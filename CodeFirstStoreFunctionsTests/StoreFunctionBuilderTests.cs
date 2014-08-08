// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
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
                        new KeyValuePair<string, EdmType>(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)) 
                    },
                    new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                    "ResultCol", "dbo", StoreFunctionKind.TableValuedFunction);

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
            Assert.True(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void Crate_creates_store_function_for_complex_type_function_import()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var complexType = ComplexType.Create("CT", "ns", DataSpace.CSpace,
                new[]
                {
                    EdmProperty.Create("Street",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String))),
                    EdmProperty.Create("ZipCode",
                        TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))),
                }, 
                null);

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] { 
                        new KeyValuePair<string, EdmType>(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)) 
                    },
                    new EdmType[] { complexType },
                    "ResultCol",
                    "dbo",
                    StoreFunctionKind.StoredProcedure);

            var storeFunction = new StoreFunctionBuilder(model, "docs", "ns").Create(functionDescriptor);

            Assert.Null(storeFunction.ReturnParameter);

            Assert.Equal(1, storeFunction.Parameters.Count);
            Assert.Equal("p1", storeFunction.Parameters[0].Name);
            Assert.Equal("nvarchar(max)", storeFunction.Parameters[0].TypeName);
            Assert.False(storeFunction.IsComposableAttribute);
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
                    new[] { new KeyValuePair<string, EdmType>("p1", enumType) },
                    new EdmType[] { enumType },
                    "ResultCol",
                    "dbo",
                    StoreFunctionKind.TableValuedFunction);

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
            Assert.True(storeFunction.IsComposableAttribute);
        }

        [Fact]
        public void StoreFunctionBuilder_uses_default_namespace_if_no_entities()
        {
            var model = new DbModelBuilder()
                    .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var functionDescriptor =
                new FunctionDescriptor(
                    "f",
                    new[] { 
                        new KeyValuePair<string, EdmType>(
                            "p1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)) 
                    },
                    new EdmType[] { PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int64) },
                    "ResultCol", "dbo", StoreFunctionKind.TableValuedFunction);

            var storeFunction = new StoreFunctionBuilder(model, "docs").Create(functionDescriptor);

            Assert.Equal("CodeFirstDatabaseSchema", storeFunction.NamespaceName);
        }
    }
}
