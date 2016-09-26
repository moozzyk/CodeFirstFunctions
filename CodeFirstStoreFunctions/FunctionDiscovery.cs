// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal class FunctionDiscovery
    {
        private readonly DbModel _model;
        private readonly Type _type;

        public FunctionDiscovery(DbModel model, Type type)
        {
            Debug.Assert(model != null, "model is null");
            Debug.Assert(type != null, "type is null");

            _model = model;
            _type = type;
        }

        public IEnumerable<FunctionDescriptor> FindFunctions()
        {
            const BindingFlags bindingFlags =
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.InvokeMethod |
                BindingFlags.Static | BindingFlags.Instance;

            foreach (var method in _type.GetMethods(bindingFlags))
            {
                var functionDescriptor = CreateFunctionDescriptor(method);
                if (functionDescriptor != null)
                {
                    yield return functionDescriptor;
                }
            }
        }

        private FunctionDescriptor CreateFunctionDescriptor(MethodInfo method)
        {
            var functionAttribute = (DbFunctionAttribute)Attribute.GetCustomAttribute(method, typeof(DbFunctionAttribute));
            var returnGenericTypeDefinition = method.ReturnType.IsGenericType
                ? method.ReturnType.GetGenericTypeDefinition()
                : null;
            
            if(functionAttribute != null ||                             // TVF, scalar UDF or StoreProc
               returnGenericTypeDefinition == typeof (ObjectResult<>))  // StoredProc without DbFunction attribute
            {
                var functionDetailsAttr = 
                    Attribute.GetCustomAttribute(method, typeof(DbFunctionDetailsAttribute)) as DbFunctionDetailsAttribute;

                var storeFunctionKind =
                    returnGenericTypeDefinition == typeof (IQueryable<>)
                        ? StoreFunctionKind.TableValuedFunction
                        : returnGenericTypeDefinition == typeof (ObjectResult<>)
                            ? StoreFunctionKind.StoredProcedure
                            : StoreFunctionKind.ScalarUserDefinedFunction;

                if (storeFunctionKind == StoreFunctionKind.ScalarUserDefinedFunction && 
                    (functionAttribute == null || functionAttribute.NamespaceName != "CodeFirstDatabaseSchema"))
                {
                    throw new InvalidOperationException("Scalar store functions must be decorated with the 'DbFunction' attribute with the 'CodeFirstDatabaseSchema' namespace.");
                }

                var unwrapperReturnType =
                    storeFunctionKind == StoreFunctionKind.ScalarUserDefinedFunction
                        ? method.ReturnType
                        : method.ReturnType.GetGenericArguments()[0];

                return new FunctionDescriptor(
                    (functionAttribute != null ? functionAttribute.FunctionName : null) ?? method.Name,
                    GetParameters(method, storeFunctionKind),
                    GetReturnTypes(method.Name, unwrapperReturnType, functionDetailsAttr, storeFunctionKind),
                    functionDetailsAttr != null ? functionDetailsAttr.ResultColumnName : null,
                    functionDetailsAttr != null ? functionDetailsAttr.DatabaseSchema : null,
                    storeFunctionKind,
                    functionDetailsAttr != null ? GetBuiltInOption(functionDetailsAttr.IsBuiltIn) : null);
            }

            return null;
        }

        private IEnumerable<ParameterDescriptor> GetParameters(MethodInfo method, StoreFunctionKind storeFunctionKind)
        {
            Debug.Assert(method != null, "method is null");

            foreach (var parameter in method.GetParameters())
            {
                if (method.IsDefined(typeof(ExtensionAttribute), false) && parameter.Position == 0)
                {
                    continue;
                }

                if (parameter.IsOut || parameter.ParameterType.IsByRef)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "The parameter '{0}' is an out or ref parameter. To map Input/Output database parameters use the 'ObjectParameter' as the parameter type.",
                            parameter.Name));
                }

                var paramTypeAttribute =
                    (ParameterTypeAttribute)Attribute.GetCustomAttribute(parameter, typeof(ParameterTypeAttribute));

                var parameterType = parameter.ParameterType;

                var isObjectParameter = parameter.ParameterType == typeof (ObjectParameter);

                if (isObjectParameter)
                {
                    if (paramTypeAttribute == null)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "Cannot infer type for parameter '{0}'. All ObjectParameter parameters must be decorated with the ParameterTypeAttribute.",
                                parameter.Name));    
                    }

                    parameterType = paramTypeAttribute.Type;
                }

                var unwrappedParameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

                var parameterEdmType =
                    unwrappedParameterType.IsEnum
                        ? FindEnumType(unwrappedParameterType)
                        : GetEdmPrimitiveTypeForClrType(unwrappedParameterType);

                if (parameterEdmType == null)
                {
                    throw
                        new InvalidOperationException(
                            string.Format(
                                "The type '{0}' of the parameter '{1}' of function '{2}' is invalid. Parameters can only be of a type that can be converted to an Edm scalar type",
                                unwrappedParameterType.FullName, parameter.Name, method.Name));
                }

                if (storeFunctionKind == StoreFunctionKind.ScalarUserDefinedFunction &&
                    parameterEdmType.BuiltInTypeKind != BuiltInTypeKind.PrimitiveType)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "The parameter '{0}' is of the '{1}' type which is not an Edm primitive type. Types of parameters of store scalar functions must be Edm primitive types.",
                            parameter.Name, parameterEdmType));
                }

                yield return new ParameterDescriptor(parameter.Name, parameterEdmType, 
                    paramTypeAttribute != null ? paramTypeAttribute.StoreType : null, isObjectParameter);
            }
        }

        private EdmType[] GetReturnTypes(string methodName, Type methodReturnType,
            DbFunctionDetailsAttribute functionDetailsAttribute, StoreFunctionKind storeFunctionKind)
        {
            Debug.Assert(methodReturnType != null, "methodReturnType is null");

            var resultTypes = functionDetailsAttribute != null ? functionDetailsAttribute.ResultTypes : null;

            if (storeFunctionKind != StoreFunctionKind.StoredProcedure && resultTypes != null)
            {
                throw new InvalidOperationException(
                    "The DbFunctionDetailsAttribute.ResultTypes property should be used only for stored procedures returning multiple resultsets and must be null for composable function imports.");
            }

            resultTypes = resultTypes == null || resultTypes.Length == 0 ? null : resultTypes;

            if (resultTypes != null && resultTypes[0] != methodReturnType)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "The ObjectResult<T> item type returned by the method '{0}' is '{1}' but the first type specified in the `DbFunctionDetailsAttribute.ResultTypes` is '{2}'. The ObjectResult<T> item type must match the first type from the `DbFunctionDetailsAttribute.ResultTypes` array.",
                        methodName, methodReturnType.FullName, resultTypes[0].FullName));
            }

            var edmResultTypes = (resultTypes ?? new[] {methodReturnType}).Select(GetReturnEdmItemType).ToArray();

            if (storeFunctionKind == StoreFunctionKind.ScalarUserDefinedFunction &&
                edmResultTypes[0].BuiltInTypeKind != BuiltInTypeKind.PrimitiveType)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "The type '{0}' returned by the method '{1}' cannot be mapped to an Edm primitive type. Scalar user defined functions have to return types that can be mapped to Edm primitive types.",
                        methodReturnType.FullName, methodName));
            }

            return edmResultTypes;
        }

        private EdmType GetReturnEdmItemType(Type type)
        {
            var unwrappedType = Nullable.GetUnderlyingType(type) ?? type;

            var edmType = GetEdmPrimitiveTypeForClrType(unwrappedType);
            if (edmType != null)
            {
                return edmType;
            }

            if (unwrappedType.IsEnum)
            {
                if ((edmType = FindEnumType(unwrappedType)) != null)
                {
                    return edmType;
                }
            }
            else
            {
                if ((edmType = FindStructuralType(unwrappedType)) != null)
                {
                    return edmType;
                }                
            }

            throw new InvalidOperationException(string.Format("No EdmType found for type '{0}'.", type.FullName));
        }

        private static EdmType GetEdmPrimitiveTypeForClrType(Type clrType)
        {
            return PrimitiveType
                .GetEdmPrimitiveTypes()
                .FirstOrDefault(t => t.ClrEquivalentType == clrType);
        }

        private EdmType FindStructuralType(Type type)
        {
            return
                ((IEnumerable<StructuralType>)_model.ConceptualModel.EntityTypes)
                    .Concat(_model.ConceptualModel.ComplexTypes)
                    .FirstOrDefault(t => t.Name == type.Name);
        }

        private EdmType FindEnumType(Type type)
        {
            return _model.ConceptualModel.EnumTypes.FirstOrDefault(t => t.Name == type.Name);
        }

        private bool? GetBuiltInOption(BuiltInOptions builtInOption)
        {
            if (builtInOption == BuiltInOptions.BuiltIn)
                return true;
            else if (builtInOption == BuiltInOptions.NotBuiltIn)
                return false;
            else if (builtInOption == BuiltInOptions.Unspecified)
                return null;
            else
                throw new ArgumentException("invalid built in option");
        }
    }
}
