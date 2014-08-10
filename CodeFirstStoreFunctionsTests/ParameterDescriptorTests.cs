// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class ParameterDescriptorTests
    {
        [Fact]
        public void ParameterDescriptor_initialized_correctly()
        {
            var edmType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Binary);

            var parameterDescriptor = new ParameterDescriptor("param", edmType, true);

            Assert.Equal("param", parameterDescriptor.Name);
            Assert.Same(edmType, parameterDescriptor.EdmType);
            Assert.True(parameterDescriptor.IsOutParam);
        }
    }
}
