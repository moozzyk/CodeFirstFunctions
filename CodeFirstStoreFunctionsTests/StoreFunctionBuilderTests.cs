// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class StoreFunctionBuilderTests
    {
        [Fact]
        public void Crate_creates_store_function_for_primitive_function_import()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] {
                        new ParameterDescriptor(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, false),
                    },
                    new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                    "ResultCol", "dbo", StoreFunctionKind.TableValuedFunction, isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Equal(
                BuiltInTypeKind.CollectionType,
                storeFunction.ReturnParameter.TypeUsage.EdmType.BuiltInTypeKind);

            var collectionItemType =
                (RowType)((CollectionType)storeFunction.ReturnParameter.TypeUsage.EdmType).TypeUsage.EdmType;

            Assert.Equal(1, collectionItemType.Properties.Count);
            Assert.Equal("ResultCol", collectionItemType.Properties[0].Name);
            Assert.Equal("bigint", collectionItemType.Properties[0].TypeUsage.EdmType.Name);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("nvarchar(max)", storeFunction.Parameters[0].TypeName);
            Assert.Equal(ParameterMode.In, storeFunction.Parameters[0].Mode);
            Assert.True(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void Crate_creates_store_function_for_complex_type_function_import()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var enumType = EnumType.Create("TestEnum", "TestNs",PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, new EnumMember[]{EnumMember.Create("foo", 1, null)}, null);

            var complexType = ComplexType.Create("CT", "ns", DataSpace.CSpace,
                new[]
                {
                    EdmProperty.Create("Street",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String))),
                    EdmProperty.Create("ZipCode",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))),
                    EdmProperty.Create("MyEnum", TypeUsage.CreateDefaultTypeUsage(enumType))
                },
                null);

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[]
                    {new ParameterDescriptor("p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, false)},
                    new EdmType[] {complexType},
                    "ResultCol",
                    "dbo",
                    StoreFunctionKind.StoredProcedure,
                    isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Null(storeFunction.ReturnParameter);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("nvarchar(max)", storeFunction.Parameters[0].TypeName);
            Assert.Equal(ParameterMode.In, storeFunction.Parameters[0].Mode);
            Assert.False(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void Crate_creates_store_function_for_complex_type_withEnum_in_TableValuedFunction()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var enumType = EnumType.Create("TestEnum", "TestNs", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, new EnumMember[] { EnumMember.Create("foo", 1, null) }, null);

            var complexType = ComplexType.Create("CT", "ns", DataSpace.CSpace,
                new[]
                {
                    EdmProperty.Create("Street",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String))),
                    EdmProperty.Create("ZipCode",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))),
                    EdmProperty.Create("MyEnum", TypeUsage.CreateDefaultTypeUsage(enumType))
                },
                null);

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[]
                    {new ParameterDescriptor("p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, false)},
                    new EdmType[] { complexType },
                    "ResultCol",
                    "dbo",
                    StoreFunctionKind.TableValuedFunction,
                    isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Equal(
                BuiltInTypeKind.CollectionType,
                storeFunction.ReturnParameter.TypeUsage.EdmType.BuiltInTypeKind);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("nvarchar(max)", storeFunction.Parameters[0].TypeName);
            Assert.Equal(ParameterMode.In, storeFunction.Parameters[0].Mode);
            Assert.True(storeFunction.IsComposableAttribute);
        }


        [Fact]
        public void Crate_creates_store_function_for_enum_type_function_import()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var enumTypeCtor = typeof(EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(c => c.GetParameters().Count() == 5);
            var enumType = (EnumType)enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, DataSpace.CSpace },
                CultureInfo.InvariantCulture);

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] { new ParameterDescriptor("p1", enumType, null, false) },
                    new EdmType[] { enumType },
                    "ResultCol",
                    "dbo",
                    StoreFunctionKind.TableValuedFunction,
                    isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Equal(
                BuiltInTypeKind.CollectionType,
                storeFunction.ReturnParameter.TypeUsage.EdmType.BuiltInTypeKind);

            var collectionItemType =
                (RowType)((CollectionType)storeFunction.ReturnParameter.TypeUsage.EdmType).TypeUsage.EdmType;

            Assert.Equal(1, collectionItemType.Properties.Count);
            Assert.Equal("ResultCol", collectionItemType.Properties[0].Name);
            Assert.Equal("int", collectionItemType.Properties[0].TypeUsage.EdmType.Name);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("int", storeFunction.Parameters[0].TypeName);
            Assert.Equal(ParameterMode.In, storeFunction.Parameters[0].Mode);
            Assert.True(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void Crate_can_create_out_params()
        {
            var model = new DbModelBuilder()
                .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] {
                        new ParameterDescriptor(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, true),
                    },
                    new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                    "ResultCol", "dbo", StoreFunctionKind.StoredProcedure, isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("nvarchar(max)", storeFunction.Parameters[0].TypeName);
            Assert.Equal(ParameterMode.InOut, storeFunction.Parameters[0].Mode);
            Assert.False(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void StoreFunctionBuilder_uses_default_namespace_if_no_entities()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[]
                    {new ParameterDescriptor("p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, false)},
                    new EdmType[] {PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64)},
                    "ResultCol", "dbo", StoreFunctionKind.TableValuedFunction, isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs").Create(functionDescriptor);

            Assert.Equal("CodeFirstDatabaseSchema", storeFunction.NamespaceName);
        }

        [Fact]
        public void Can_specify_store_type_for_parameters()
        {
            var model = new DbModelBuilder()
                .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] {
                        new ParameterDescriptor(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), "xml", true),
                    },
                    new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                    "ResultCol", "dbo", StoreFunctionKind.StoredProcedure, isBuiltIn: null);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("xml", storeFunction.Parameters[0].TypeName);
        }

        [Fact]
        public void Exception_thrown_if_provided_store_type_invalid()
        {
            var model = new DbModelBuilder()
                .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] {
                        new ParameterDescriptor(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), "json", true),
                    },
                    new EdmType[] {PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64)},
                    "ResultCol", "dbo", StoreFunctionKind.StoredProcedure, isBuiltIn: null);

            Assert.Contains("'json'",
                Assert.Throws<InvalidOperationException>(() =>
                    new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor)).Message);
        }

        [Fact]
        public void Builtin_attribute_set_correctly()
        {
            var model = new DbModelBuilder()
                .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            foreach (var isBuiltIn in new bool?[] { null, true, false })
            {
                var functionDescriptor =
                    new FunctionDescriptor(
                        "f",
                        new[] { new ParameterDescriptor("p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), null, false) },
                        new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                        "ResultCol", "dbo", StoreFunctionKind.TableValuedFunction, isBuiltIn);

                Assert.Equal(new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor).BuiltInAttribute,
                    isBuiltIn == true);
            }
        }
    }
}
