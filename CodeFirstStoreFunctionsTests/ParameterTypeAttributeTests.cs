// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace CodeFirstStoreFunctions
{
    using Xunit;

    public class ParameterTypeAttributeTests
    {
        [Fact]
        public void ParameterTypeAttribute_initialized_correctly()
        {
            Assert.Same(typeof(object), new ParameterTypeAttribute(typeof(object)).Type);
        }

        [Fact]
        public void ParameterTypeAttribute_throws_for_null_type()
        {
            Assert.Equal("type", 
                Assert.Throws<ArgumentNullException>(
                    () => new ParameterTypeAttribute(null)).ParamName);
        }
    }
}
