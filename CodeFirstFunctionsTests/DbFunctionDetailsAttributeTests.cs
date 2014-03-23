// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstFunctions
{
    using Xunit;

    public class DbFunctionDetailsAttributeTests
    {
        [Fact]
        public void Can_set_get_schema()
        {
            var attr = new DbFunctionDetailsAttribute();

            Assert.Null(attr.DatabaseSchema);

            attr.DatabaseSchema = "dbo";

            Assert.Equal("dbo", attr.DatabaseSchema);
        }

        [Fact]
        public void Can_set_get_ResultColumnName()
        {
            var attr = new DbFunctionDetailsAttribute();

            Assert.Null(attr.ResultColumnName);

            attr.ResultColumnName = "column";

            Assert.Equal("column", attr.ResultColumnName);
        }
    }
}
