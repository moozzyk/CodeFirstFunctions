// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics;
    using System.Linq;

    internal class StoreFunctionBuilder
    {
        private readonly DbModel _model;
        private readonly string _schema;
        private readonly string _namespace;

        public StoreFunctionBuilder(DbModel model, string schema, string @namespace = null)
        {
            Debug.Assert(model != null, "model is null");

            _model = model;
            _schema = schema;

            // CodeFirstDatabaseSchema is what EF CodeFirst model builder uses for store model
            _namespace = @namespace ?? "CodeFirstDatabaseSchema";
        }

        public EdmFunction Create(FunctionDescriptor functionDescriptor)
        {
            Debug.Assert(functionDescriptor != null, "functionDescriptor is null");

            if (_schema == null && functionDescriptor.DatabaseSchema == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Database schema is not defined for function '{0}'. Either set a default database schema or use the DbFunctionEx attribute with non-null DatabaseSchema value.",
                        functionDescriptor.Name));
            }

            var functionPayload =
                new EdmFunctionPayload
                {
                    Parameters = functionDescriptor
                        .Parameters
                        .Select(
                            p => FunctionParameter.Create(
                                p.Name,
                                GetStorePrimitiveType(p),
                                p.IsOutParam
                                    ? ParameterMode.InOut
                                    : ParameterMode.In)).ToArray(),

                    ReturnParameters = CreateFunctionReturnParameters(functionDescriptor),
                    IsComposable = functionDescriptor.StoreFunctionKind != StoreFunctionKind.StoredProcedure,
                    Schema = functionDescriptor.DatabaseSchema ?? _schema,
                    IsBuiltIn = functionDescriptor.IsBuiltIn
                };

            return EdmFunction.Create(
                functionDescriptor.Name,
                _namespace,
                DataSpace.SSpace,
                functionPayload,
                null);
        }

        private List<FunctionParameter> CreateFunctionReturnParameters(FunctionDescriptor functionDescriptor)
        {
            var returnParameters = new List<FunctionParameter>();

            EdmType returnEdmType = null;
            switch (functionDescriptor.StoreFunctionKind)
            {
                case StoreFunctionKind.TableValuedFunction:
                    Debug.Assert(functionDescriptor.ReturnTypes.Length == 1, "Expected only one returnType for composable functions");
                    returnEdmType =
                        CreateReturnRowType(functionDescriptor.ResultColumnName, functionDescriptor.ReturnTypes[0])
                            .GetCollectionType();
                    break;
                case StoreFunctionKind.ScalarUserDefinedFunction:
                    var returnPrimtiveType = functionDescriptor.ReturnTypes[0].BuiltInTypeKind == BuiltInTypeKind.EnumType
                        ? ((EnumType) functionDescriptor.ReturnTypes[0]).UnderlyingType
                        : functionDescriptor.ReturnTypes[0];
                    returnEdmType = GetStorePrimitiveType(returnPrimtiveType);
                    break;
            }

            if (returnEdmType != null)
            {
                returnParameters.Add(
                    FunctionParameter.Create(
                        "ReturnParam",
                        returnEdmType,
                        ParameterMode.ReturnValue));
            }

            return returnParameters;
        }

        private EdmType CreateReturnRowType(string propertyName, EdmType edmType)
        {
            if (edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                var propertyToStoreTypeUsage = FindStoreTypeUsages((EntityType)edmType);
                var properties = new List<EdmProperty>();
                AddEntityTypeProperties((EntityType)edmType, properties);

                return
                    RowType.Create(
                       properties.Select(
                            m => EdmProperty.Create(m.Name, propertyToStoreTypeUsage[m])), null);
            }

            if (edmType.BuiltInTypeKind == BuiltInTypeKind.ComplexType)
            {
                return
                    RowType.Create(
                        ((StructuralType)edmType).Members.Select(
                            m => EdmProperty.Create(m.Name, GetStorePrimitiveTypeUsage(m.TypeUsage))), null);
            }

            if (edmType.BuiltInTypeKind == BuiltInTypeKind.EnumType)
            {
                return RowType.Create(new[]
                {
                    EdmProperty.Create(propertyName, GetStorePrimitiveTypeUsage(TypeUsage.CreateDefaultTypeUsage(((EnumType)edmType).UnderlyingType)))
                }, null);
            }

            return
                RowType.Create(
                    new[]
                    {
                        EdmProperty.Create(propertyName, GetStorePrimitiveTypeUsage(TypeUsage.CreateDefaultTypeUsage(edmType)))
                    }, null);
        }

        private void AddEntityTypeProperties(EntityType entityType, List<EdmProperty> properties)
        {
            foreach(var p in entityType.Properties)
            {
                if (p.IsComplexType)
                {
                    AddComplexTypeProperties(p.ComplexType, properties);
                }
                else
                {
                    properties.Add(p);
                }
            }
        }

        private void AddComplexTypeProperties(ComplexType complexType, List<EdmProperty> properties)
        {
            foreach (var p in complexType.Properties)
            {
                if (p.IsComplexType)
                {
                    throw new NotImplementedException("Nested complex types are not supported.");
                }

                properties.Add(p);
            }
        }

        private Dictionary<EdmProperty, TypeUsage> FindStoreTypeUsages(EntityType entityType)
        {
            Debug.Assert(entityType != null, "entityType == null");

            var propertyToStoreTypeUsage = new Dictionary<EdmProperty, TypeUsage>();

            var types = Tools.GetTypeHierarchy(entityType);
            var entityTypeMappings =
                _model.ConceptualToStoreMapping.EntitySetMappings
                    .SelectMany(s => s.EntityTypeMappings)
                    .Where(t => types.Contains(t.EntityType))
                        .ToArray();

            foreach (var property in entityType.Properties)
            {
                foreach (var entityTypeMapping in entityTypeMappings)
                {
                    var propertyMapping =
                        entityTypeMapping.Fragments.SelectMany(f => f.PropertyMappings)
                        .FirstOrDefault(p => p.Property == property);

                    var scalarPropertyMapping = propertyMapping as ScalarPropertyMapping;
                    if (scalarPropertyMapping != null)
                    {
                        Debug.Assert(!propertyToStoreTypeUsage.ContainsKey(property), "Property already in dictionary");

                        propertyToStoreTypeUsage[property] = TypeUsage.Create(
                            scalarPropertyMapping.Column.TypeUsage.EdmType,
                            scalarPropertyMapping.Column.TypeUsage.Facets.Where(
                                f => f.Name != "StoreGeneratedPattern" && f.Name != "ConcurrencyMode"));

                        break;
                    }

                    var complexPropertyMapping = propertyMapping as ComplexPropertyMapping;
                    if (complexPropertyMapping != null)
                    {
                        AddComplexPropertyMapping(complexPropertyMapping, propertyToStoreTypeUsage);
                        break;
                    }
                }
            }

            return propertyToStoreTypeUsage;
        }

        private void AddComplexPropertyMapping(ComplexPropertyMapping complexPropertyMapping,
            Dictionary<EdmProperty, TypeUsage> propertyToStoreTypeUsage)
        {
            var complexType = complexPropertyMapping.TypeMappings.First().ComplexType;
            foreach (var property in complexType.Properties)
            {
                var propertyMapping = complexPropertyMapping.TypeMappings.First()
                    .PropertyMappings.FirstOrDefault(p => p.Property == property);

                var scalarPropertyMapping = propertyMapping as ScalarPropertyMapping;
                if (scalarPropertyMapping != null && !propertyToStoreTypeUsage.ContainsKey(property))
                {
                    propertyToStoreTypeUsage[property] = TypeUsage.Create(
                        scalarPropertyMapping.Column.TypeUsage.EdmType,
                        scalarPropertyMapping.Column.TypeUsage.Facets.Where(
                            f => f.Name != "StoreGeneratedPattern" && f.Name != "ConcurrencyMode"));
                }
                else
                {
                    throw new NotImplementedException("Nested complex types not supported.");
                }
            }
        }

        private TypeUsage GetStorePrimitiveTypeUsage(TypeUsage typeUsage)
        {
            Debug.Assert(typeUsage != null, "typeUsage is null");
            Debug.Assert(typeUsage.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType, "expected primitive type");

            return _model.ProviderManifest.GetStoreType(typeUsage);
        }

        private EdmType GetStorePrimitiveType(ParameterDescriptor parameterDescriptor)
        {
            if (parameterDescriptor.StoreType != null)
            {
                var type =
                    _model.ProviderManifest.GetStoreTypes()
                        .SingleOrDefault(t => t.Name == parameterDescriptor.StoreType);

                if (type == null)
                {
                    throw new InvalidOperationException(string.Format(
                        "No store EdmType with the name '{0}' could be found.", parameterDescriptor.StoreType));
                }

                return type;
            }

            return GetStorePrimitiveType(
                parameterDescriptor.EdmType.BuiltInTypeKind == BuiltInTypeKind.EnumType
                    ? ((EnumType) parameterDescriptor.EdmType).UnderlyingType
                    : parameterDescriptor.EdmType);
        }

        private EdmType GetStorePrimitiveType(EdmType edmType)
        {
            Debug.Assert(edmType != null, "edmType is null");
            Debug.Assert(edmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType, "expected primitive type");

            return _model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(edmType)).EdmType;
        }
    }
}
