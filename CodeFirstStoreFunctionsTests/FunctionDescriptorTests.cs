// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class FunctionDescriptorTests
    {
        [Fact]
        public void FunctionDescriptor_initialized_correctly()
        {
            var edmType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Boolean);
            var parameters =
                new[] 
                { 
                    new KeyValuePair<string, EdmType>("p1", edmType),
                    new KeyValuePair<string, EdmType>("p2", edmType),
                };

            var functionDescriptor = 
                new FunctionDescriptor("Func", parameters, new EdmType[] { edmType }, "result", "dbo", isComposable: true);

            Assert.Equal("Func", functionDescriptor.Name);
            Assert.Same(edmType, functionDescriptor.ReturnTypes[0]);
            Assert.Equal(parameters, functionDescriptor.Parameters);
            Assert.Equal("result", functionDescriptor.ResultColumnName);
            Assert.Equal("dbo", functionDescriptor.DatabaseSchema);
            Assert.True(functionDescriptor.IsComposable);
        }
    }
}
