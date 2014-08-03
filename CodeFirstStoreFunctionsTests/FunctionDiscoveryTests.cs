// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using Moq;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class FunctionDiscoveryTests
    {
        public class FindFunctionImportsTests
        {
            private class Fake
            {
                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int> PrimitiveFunctionImportComposable(int p1, string p2)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<TestEnumType> EnumFunctionImportComposable(TestEnumType p1, string p2)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int?> PrimitiveFunctionImportWithNullablePrimitiveTypes(int? p1)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<TestEnumType?> EnumFunctionImportWithNullableEnums(TestEnumType? p1)
                {
                    throw new NotImplementedException();
                }

                // should not be discovered - missing DbFunctionAttribute
                public IQueryable<int> NotAFunctionImport(int p1, string p2)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int> InvalidParamFunc(object p1)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "f")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col", ResultTypes = new Type[0])]
                public IQueryable<int> TVFWithResultTypes()
                {
                    throw new NotImplementedException();
                }


                [DbFunction("ns", "f")]
                public IQueryable<TestComplexType> FunctionImportReturningComplexTypesComposable()
                {
                    throw new NotImplementedException();
                }

                public ObjectResult<TestComplexType> StoredProcToComplexTypes()
                {
                    throw new NotImplementedException();
                }


                [DbFunctionDetails(ResultTypes = new Type[0])]
                public ObjectResult<int> EmptyResultType()
                {
                    throw new NotImplementedException();
                }

                [DbFunctionDetails(ResultTypes = new [] { typeof(int)})]
                public ObjectResult<byte> StoredProcReturnTypeAndResultTypeMismatch()
                {
                    throw new NotImplementedException();
                }

                public class Entity
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }

                public class TestComplexType
                {
                    public int Prop1 { get; set; }
                    public string Prop2 { get; set; }

                }

                public enum TestEnumType
                {
                }

                // TODO:
                // invalid return type (return type not in model)
                // parameters of invalid return type
                // missing schema
                // missing result column name for scalars
                // result column for non-scalars
                // function attribute on non IQueryable
                // out parameters
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_returning_primitive_types()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "PrimitiveFunctionImportComposable" || m.Name == "NotAFunctionImport")
                        .ToArray());

                var functionImport = 
                    new FunctionDiscovery(CreateModel(), mockType.Object)
                        .FindFunctionImports().Single();

                Assert.NotNull(functionImport);
                Assert.Equal("PrimitiveFunctionImportComposable", functionImport.Name);
                Assert.Equal(2, functionImport.Parameters.Count());
                Assert.Equal("Edm.Int32", functionImport.ReturnType.FullName);
                Assert.True(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_returning_complex_types()
            {
                var model = CreateModel();
                model.ConceptualModel.AddItem(CreateComplexType());

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "FunctionImportReturningComplexTypesComposable")
                        .ToArray());

                var functionImport =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctionImports().Single();

                Assert.NotNull(functionImport);
                Assert.Equal("FunctionImportReturningComplexTypesComposable", functionImport.Name);
                Assert.Equal(0, functionImport.Parameters.Count());
                Assert.Equal("Model.TestComplexType", functionImport.ReturnType.FullName);
                Assert.True(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_taking_and_returning_nullable_primitive_types()
            {
                var enumTypeCtor = typeof(EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType)enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, DataSpace.CSpace },
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "PrimitiveFunctionImportWithNullablePrimitiveTypes")
                        .ToArray());

                var functionImport =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctionImports().SingleOrDefault();

                Assert.NotNull(functionImport);
                Assert.Equal("PrimitiveFunctionImportWithNullablePrimitiveTypes", functionImport.Name);
                Assert.Equal(1, functionImport.Parameters.Count());
                Assert.Equal("Edm.Int32", functionImport.ReturnType.FullName);
                Assert.True(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_returning_enum_types()
            {
                var enumTypeCtor = typeof (EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType)enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] {"TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, DataSpace.CSpace}, 
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "EnumFunctionImportComposable")
                        .ToArray());

                var functionImport =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctionImports().Single();

                Assert.NotNull(functionImport);
                Assert.Equal("EnumFunctionImportComposable", functionImport.Name);
                Assert.Equal(2, functionImport.Parameters.Count());
                Assert.Equal("Model.TestEnumType", functionImport.ReturnType.FullName);
                Assert.True(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_taking_and_returning_nullable_enums()
            {
                var enumTypeCtor = typeof(EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType)enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[] { "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false, DataSpace.CSpace },
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "EnumFunctionImportWithNullableEnums")
                        .ToArray());

                var functionImport =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctionImports().SingleOrDefault();

                Assert.NotNull(functionImport);
                Assert.Equal("EnumFunctionImportWithNullableEnums", functionImport.Name);
                Assert.Equal(1, functionImport.Parameters.Count());
                Assert.Equal("Model.TestEnumType", functionImport.ReturnType.FullName);
                Assert.True(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_creates_function_imports_returning_complex_types_non_composable()
            {
                var model = CreateModel();
                model.ConceptualModel.AddItem(CreateComplexType());

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof(Fake)
                        .GetMethods()
                        .Where(m => m.Name == "StoredProcToComplexTypes")
                        .ToArray());

                var functionImport =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctionImports().Single();

                Assert.NotNull(functionImport);
                Assert.Equal("StoredProcToComplexTypes", functionImport.Name);
                Assert.Equal(0, functionImport.Parameters.Count());
                Assert.Equal("Model.TestComplexType", functionImport.ReturnType.FullName);
                Assert.False(functionImport.IsComposable);
            }

            [Fact]
            public void FindFunctionImports_throws_for_function_imports_with_invalid_parameters()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] { typeof(Fake).GetMethod("InvalidParamFunc") });

                var message = 
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                                .FindFunctionImports()
                                .ToArray()).Message;

                Assert.Contains("System.Object", message);
                Assert.Contains("p1", message);
                Assert.Contains("InvalidParamFunc", message);
            }

            [Fact]
            public void FindFunctionImports_throws_for_TVFs_with_ResultTypes()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] { typeof(Fake).GetMethod("TVFWithResultTypes") });

                var message =
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                                .FindFunctionImports()
                                .ToArray()).Message;

                Assert.Contains("DbFunctionDetailsAttribute.ResultTypes", message);
            }

            [Fact]
            public void FindFunctionImports_ignores_empty_ResultTypes_for_non_composable()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] { typeof(Fake).GetMethod("EmptyResultType") });

                var returnType = new FunctionDiscovery(CreateModel(), mockType.Object)
                                .FindFunctionImports()
                                .ToArray()[0].ReturnType;

                Assert.Contains("Edm.Int32", returnType.FullName);
            }

            [Fact]
            public void FindFunctionImports_throws_if_return_type_and_resultTypes_out_of_sync()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] { typeof(Fake).GetMethod("StoredProcReturnTypeAndResultTypeMismatch") });

                var message =
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                                .FindFunctionImports()
                                .ToArray()).Message;

                Assert.Contains("ObjectResult<T>", message);
                Assert.Contains("'StoredProcReturnTypeAndResultTypeMismatch'", message);
                Assert.Contains("'System.Int32'", message);
                Assert.Contains("'System.Byte'", message);
                Assert.Contains("DbFunctionDetailsAttribute.ResultTypes", message);
            }

            private static DbModel CreateModel()
            {
                return
                    new DbModelBuilder()
                        .Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            }

            private static ComplexType CreateComplexType()
            {
                var prop1 = EdmProperty.Create(
                    "Prop1",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)));
                var prop2 = EdmProperty.Create(
                    "Prop2",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)));

                return
                    ComplexType.Create("TestComplexType", "Model", DataSpace.CSpace, new[] { prop1, prop2 }, null);
            }

            private static EntityType CreateEntityType()
            {
                var idProperty = EdmProperty.Create(
                    "Id",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)));
                var nameProperty = EdmProperty.Create(
                    "Name",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)));

                return
                    EntityType.Create("Entity", "Model", DataSpace.CSpace, new[] {"Id"},
                        new[] {idProperty, nameProperty}, null);
            }
        }
    }
}
