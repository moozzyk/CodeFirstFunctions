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

        public IEnumerable<FunctionImport> FindFunctionImports()
        {
            foreach (var method in _type.GetMethods())
            {
                var functionImport = CreateFunctionImport(method);
                if (functionImport != null)
                {
                    yield return functionImport;
                }
            }
        }

        private FunctionImport CreateFunctionImport(MethodInfo method)
        {
            var functionAttribute = Attribute.GetCustomAttribute(method, typeof(DbFunctionAttribute));
            var returnGenericTypeDefinition = method.ReturnType.IsGenericType
                ? method.ReturnType.GetGenericTypeDefinition()
                : null;
            
            if((returnGenericTypeDefinition == typeof (IQueryable<>) && functionAttribute != null) ||  //TVF
               returnGenericTypeDefinition == typeof (ObjectResult<>))                                 // StoredProc
            {
                var functionDetailsAttr = 
                    Attribute.GetCustomAttribute(method, typeof(DbFunctionDetailsAttribute)) as DbFunctionDetailsAttribute;
                
                return new FunctionImport(
                    method.Name, 
                    GetParameters(method),
                    GetReturnEdmItemType(method.ReturnType.GetGenericArguments()[0]),
                    functionDetailsAttr != null ? functionDetailsAttr.ResultColumnName : null,
                    functionDetailsAttr != null ? functionDetailsAttr.DatabaseSchema : null,
                    isComposable: returnGenericTypeDefinition == typeof(IQueryable<>));
            }

            return null;
        }

        private IEnumerable<KeyValuePair<string, EdmType>> GetParameters(MethodInfo method)
        {
            Debug.Assert(method != null, "method is null");

            // TODO: Output parameters?
            foreach (var parameter in method.GetParameters())
            {
                var unwrappedParameterType = 
                    Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

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

                yield return new KeyValuePair<string, EdmType>(parameter.Name, parameterEdmType);
            }
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
    }
}
