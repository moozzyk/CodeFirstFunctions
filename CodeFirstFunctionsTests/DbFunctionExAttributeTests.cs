// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstFunctions
{
    using Xunit;

    public class DbFunctionExAttributeTests
    {
        [Fact]
        public void Ctor_sets_namespace_and_name()
        {
            var attr = new DbFunctionExAttribute("ns", "f");

            Assert.Equal("ns", attr.NamespaceName);
            Assert.Equal("f", attr.FunctionName);
        }

        [Fact]
        public void Can_set_get_schema()
        {
            var attr = new DbFunctionExAttribute("ns", "f");

            Assert.Null(attr.DatabaseSchema);

            attr.DatabaseSchema = "dbo";

            Assert.Equal("dbo", attr.DatabaseSchema);
        }

        [Fact]
        public void Can_set_get_ResultColumnName()
        {
            var attr = new DbFunctionExAttribute("ns", "f");

            Assert.Null(attr.ResultColumnName);

            attr.ResultColumnName = "column";

            Assert.Equal("column", attr.ResultColumnName);
        }
    }
}
