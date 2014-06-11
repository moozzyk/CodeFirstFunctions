// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class FunctionImportTests
    {
        [Fact]
        public void FunctionImportInitialized()
        {
            var edmType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Boolean);
            var parameters =
                new[] 
                { 
                    new KeyValuePair<string, EdmType>("p1", edmType),
                    new KeyValuePair<string, EdmType>("p2", edmType),
                };

            var functionImport = 
                new FunctionImport("Func", parameters, new EdmType[] { edmType }, "result", "dbo", null, isComposable: true);

            Assert.Equal("Func", functionImport.Name);
            Assert.Same(edmType, functionImport.ReturnTypes[0]);
            Assert.Equal(parameters, functionImport.Parameters);
            Assert.Equal("result", functionImport.ResultColumnName);
            Assert.Equal("dbo", functionImport.DatabaseSchema);
            Assert.True(functionImport.IsComposable);
        }
    }
}
