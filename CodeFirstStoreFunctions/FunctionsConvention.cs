// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Diagnostics;
    using System.Linq;

    public class FunctionsConvention<T> : IStoreModelConvention<EntityContainer>
        where T : DbContext
    {
        private readonly string _defaultSchema;

        public FunctionsConvention(string defaultSchema)
        {
            _defaultSchema = defaultSchema;
        }

        public void Apply(EntityContainer item, DbModel model)
        {
            var functionImports = new FunctionDiscovery(model, typeof (T)).FindFunctionImports();
            var storeFunctionBuilder = new StoreFunctionBuilder(model, _defaultSchema);

            foreach (var functionImport in functionImports)
            {
                var functionImportDefinition = CreateFunctionImport(model, functionImport);
                var storeFunctionDefinition = storeFunctionBuilder.Create(functionImport);
                model.ConceptualModel.Container.AddFunctionImport(functionImportDefinition);
                model.StoreModel.AddItem(storeFunctionDefinition);

                if (functionImportDefinition.IsComposableAttribute)
                {
                    model.ConceptualToStoreMapping.AddFunctionImportMapping(
                        new FunctionImportMappingComposable(
                            functionImportDefinition,
                            storeFunctionDefinition,
                            new FunctionImportResultMapping(),
                            model.ConceptualToStoreMapping));
                }
                else
                {
                    model.ConceptualToStoreMapping.AddFunctionImportMapping(
                        new FunctionImportMappingNonComposable(
                            functionImportDefinition,
                            storeFunctionDefinition,
                            new FunctionImportResultMapping[0],
                            model.ConceptualToStoreMapping));
                }
            }

            // TODO: scalar functions?, model defined functions?, multiple result sets?
        }

        private EdmFunction CreateFunctionImport(DbModel model, FunctionImport functionImport)
        {
            List<EntitySet> entitySets = null;
            if (functionImport.ReturnType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                // TODO: derived types?
				var types = Tools.GetTypeHierarchy(functionImport.ReturnType);
                entitySets =
                    model.ConceptualModel.Container.EntitySets.Where(s => types.Contains(s.ElementType))
                        .ToList();

                if (entitySets.Count == 0)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "The model does not contain EntitySet for the '{0}' entity type.",
                            functionImport.ReturnType.Name));
                }

                Debug.Assert(entitySets.Count == 1, "Invalid model (MEST)");
            }

            var functionPayload =
                new EdmFunctionPayload
                {
                    Parameters =
                        functionImport
                            .Parameters
                            .Select(p => FunctionParameter.Create(p.Key, p.Value, ParameterMode.In))
                            .ToList(),
                    ReturnParameters = new[]
                    {
                        FunctionParameter.Create(
                            "ReturnParam",
                            functionImport.ReturnType.GetCollectionType(),
                            ParameterMode.ReturnValue)
                    },
                    IsComposable = functionImport.IsComposable,
                    IsFunctionImport = true,
                    EntitySets = entitySets
                };

            return EdmFunction.Create(
                functionImport.Name,
                model.ConceptualModel.Container.Name,
                DataSpace.CSpace,
                functionPayload,
                null);
        }
    }
}