// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace CodeFirstStoreFunctions
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

        [Fact]
        public void Can_get_set_ResultTypes()
        {
            var resultTypes = new Type[0];
            var attr = new DbFunctionDetailsAttribute();

            Assert.Null(attr.ResultTypes);

            attr.ResultTypes = resultTypes;

            Assert.Same(resultTypes, attr.ResultTypes);
        }

        [Fact]
        public void Is_BuiltIn_false_by_default_and_not_marked_as_set()
        {
            var attr = new DbFunctionDetailsAttribute();
            Assert.False(attr.IsBuiltIn);
            Assert.False(attr.IsBuiltInPropertySet);
        }

        [Fact]
        public void Is_BuiltIn_false_when_set_to_false_and_marked_as_set()
        {
            var attr = new DbFunctionDetailsAttribute();
            attr.IsBuiltIn = false;
            Assert.False(attr.IsBuiltIn);
            Assert.True(attr.IsBuiltInPropertySet);
        }

        [Fact]
        public void Is_BuiltIn_true_when_set_to_true_and_marked_as_set()
        {
            var attr = new DbFunctionDetailsAttribute();
            attr.IsBuiltIn = true;
            Assert.True(attr.IsBuiltIn);
            Assert.True(attr.IsBuiltInPropertySet);
        }
    }
}
