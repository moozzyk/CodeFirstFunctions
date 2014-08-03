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

        public EdmFunction Create(FunctionImport functionImport)
        {
            Debug.Assert(functionImport != null, "functionImport is null");

            if (_schema == null && functionImport.DatabaseSchema == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Database schema is not defined for function '{0}'. Either set a default database schema or use the DbFunctionEx attribute with non-null DatabaseSchema value.",
                        functionImport.Name));
            }

            var returnParameters = new List<FunctionParameter>();
            if (functionImport.IsComposable)
            {
                Debug.Assert(functionImport.ReturnTypes.Length == 1, "Expected only one returnType for composable functions");

                var returnEdmType =
                    CreateReturnRowType(functionImport.ResultColumnName, functionImport.ReturnTypes[0]);

                returnParameters.Add(
                    FunctionParameter.Create(
                        "ReturnParam",
                        returnEdmType.GetCollectionType(),
                        ParameterMode.ReturnValue));
            }

            var functionPayload =
                new EdmFunctionPayload()
                {
                    Parameters = functionImport
                        .Parameters
                        .Select(
                            p => FunctionParameter.Create(
                                p.Key, 
                                GetStorePrimitiveType(
                                    p.Value.BuiltInTypeKind == BuiltInTypeKind.EnumType 
                                        ? ((EnumType)p.Value).UnderlyingType 
                                        : p.Value), 
                                ParameterMode.In)).ToArray(),

                    ReturnParameters = returnParameters,
                    IsComposable = functionImport.IsComposable,
                    Schema = functionImport.DatabaseSchema ?? _schema
                };

            return EdmFunction.Create(
                functionImport.Name,
                _namespace,
                DataSpace.SSpace,
                functionPayload,
                null);
        }

        private EdmType CreateReturnRowType(string propertyName, EdmType edmType)
        {
            if (edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                var propertyToSoreTypeUsage = FindStoreTypeUsages((EntityType)edmType);
                return
                    RowType.Create(
                        ((EntityType) edmType).Properties.Select(
                            m => EdmProperty.Create(m.Name, propertyToSoreTypeUsage[m])), null);
            }

            if (edmType.BuiltInTypeKind == BuiltInTypeKind.ComplexType)
            {
                return
                    RowType.Create(
                        ((StructuralType) edmType).Members.Select(
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
                        (ScalarPropertyMapping)entityTypeMapping.Fragments.SelectMany(f => f.PropertyMappings)
                        .FirstOrDefault(p => p.Property == property);

                    if (propertyMapping != null)
                    {
                        Debug.Assert(!propertyToStoreTypeUsage.ContainsKey(property), "Property already in dictionary");

                        propertyToStoreTypeUsage[property] = TypeUsage.Create(
                            propertyMapping.Column.TypeUsage.EdmType,
                            propertyMapping.Column.TypeUsage.Facets.Where(
                                f => f.Name != "StoreGeneratedPattern" && f.Name != "ConcurrencyMode"));

                        break;
                    }
                }
            }

            return propertyToStoreTypeUsage;
        }

        private TypeUsage GetStorePrimitiveTypeUsage(TypeUsage typeUsage)
        {
            Debug.Assert(typeUsage != null, "typeUsage is null");
            Debug.Assert(typeUsage.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType, "expected primitive type");

            return _model.ProviderManifest.GetStoreType(typeUsage);
        }
        private EdmType GetStorePrimitiveType(EdmType edmType)
        {
            Debug.Assert(edmType != null, "edmType is null");
            Debug.Assert(edmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType, "expected primitive type");

            return _model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(edmType)).EdmType;
        }
    }
}
