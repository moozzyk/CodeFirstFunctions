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
        public void Can_get_set_Type_property()
        {
            Assert.Same(typeof(object), new ParameterTypeAttribute { Type = typeof(object)}.Type);
        }

        [Fact]
        public void Can_get_set_StoreType_property()
        {
            Assert.Equal("abc", new ParameterTypeAttribute { StoreType = "abc" }.StoreType);
        }

    }
}
