// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using Moq;
    using Moq.Protected;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public static class StaticFake
    {
        [DbFunction("ns", "ExtensionMethod")]
        public static IQueryable<int> ExtensionMethod(this IQueryable q, string param)
        {
            throw new NotImplementedException();
        }

        [DbFunction("ns", "StaticMethod")]
        public static IQueryable<int> StaticMethod(string param)
        {
            throw new NotImplementedException();
        }
    }

    public class FunctionDiscoveryTests
    {
        public class FindFunctionsTests
        {
            private class Fake
            {
                [DbFunction("ns", "PrimitiveFunctionImportComposable")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int> PrimitiveFunctionImportComposable(int p1, string p2)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "EnumFunctionImportComposable")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<TestEnumType> EnumFunctionImportComposable(TestEnumType p1, string p2)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "PrimitiveFunctionImportWithNullablePrimitiveTypes")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int?> PrimitiveFunctionImportWithNullablePrimitiveTypes(int? p1)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "EnumFunctionImportWithNullableEnums")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<TestEnumType?> EnumFunctionImportWithNullableEnums(TestEnumType? p1)
                {
                    throw new NotImplementedException();
                }

                [DbFunction("ns", "storeFuncName")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col")]
                public IQueryable<int?> FuncWithDifferentNames()
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

                [DbFunction("ns", "TVFWithResultTypes")]
                [DbFunctionDetails(DatabaseSchema = "abc", ResultColumnName = "col", ResultTypes = new Type[0])]
                public IQueryable<int> TVFWithResultTypes()
                {
                    throw new NotImplementedException();
                }


                [DbFunction("ns", "FunctionImportReturningComplexTypesComposable")]
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

                [DbFunctionDetails(ResultTypes = new[] {typeof (int)})]
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
            public void FindFunctions_creates_function_descriptors_returning_primitive_types()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "PrimitiveFunctionImportComposable" || m.Name == "NotAFunctionImport")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(CreateModel(), mockType.Object)
                        .FindFunctions().Single();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("PrimitiveFunctionImportComposable", functionDescriptor.Name);
                Assert.Equal(2, functionDescriptor.Parameters.Count());
                Assert.Equal("Edm.Int32", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_returning_complex_types()
            {
                var model = CreateModel();
                model.ConceptualModel.AddItem(CreateComplexType());

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "FunctionImportReturningComplexTypesComposable")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctions().Single();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("FunctionImportReturningComplexTypesComposable", functionDescriptor.Name);
                Assert.Equal(0, functionDescriptor.Parameters.Count());
                Assert.Equal("Model.TestComplexType", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_taking_and_returning_nullable_primitive_types()
            {
                var enumTypeCtor =
                    typeof (EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType) enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[]
                    {
                        "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false,
                        DataSpace.CSpace
                    },
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "PrimitiveFunctionImportWithNullablePrimitiveTypes")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctions().SingleOrDefault();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("PrimitiveFunctionImportWithNullablePrimitiveTypes", functionDescriptor.Name);
                Assert.Equal(1, functionDescriptor.Parameters.Count());
                Assert.Equal("Edm.Int32", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_returning_enum_types()
            {
                var enumTypeCtor =
                    typeof (EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType) enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[]
                    {
                        "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false,
                        DataSpace.CSpace
                    },
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "EnumFunctionImportComposable")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctions().Single();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("EnumFunctionImportComposable", functionDescriptor.Name);
                Assert.Equal(2, functionDescriptor.Parameters.Count());
                Assert.Equal("Model.TestEnumType", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_taking_and_returning_nullable_enums()
            {
                var enumTypeCtor =
                    typeof (EnumType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(c => c.GetParameters().Count() == 5);
                var enumType = (EnumType) enumTypeCtor.Invoke(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new object[]
                    {
                        "TestEnumType", "Model", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), false,
                        DataSpace.CSpace
                    },
                    CultureInfo.InvariantCulture);

                var model = CreateModel();
                model.ConceptualModel.AddItem(enumType);

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "EnumFunctionImportWithNullableEnums")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctions().SingleOrDefault();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("EnumFunctionImportWithNullableEnums", functionDescriptor.Name);
                Assert.Equal(1, functionDescriptor.Parameters.Count());
                Assert.Equal("Model.TestEnumType", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_returning_complex_types_non_composable()
            {
                var model = CreateModel();
                model.ConceptualModel.AddItem(CreateComplexType());

                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "StoredProcToComplexTypes")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(model, mockType.Object)
                        .FindFunctions().SingleOrDefault();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("StoredProcToComplexTypes", functionDescriptor.Name);
                Assert.Equal(0, functionDescriptor.Parameters.Count());
                Assert.Equal("Model.TestComplexType", functionDescriptor.ReturnTypes[0].FullName);
                Assert.False(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_with_custom_names()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(typeof (Fake)
                        .GetMethods()
                        .Where(m => m.Name == "FuncWithDifferentNames")
                        .ToArray());

                var functionDescriptor =
                    new FunctionDiscovery(CreateModel(), mockType.Object)
                        .FindFunctions().Single();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("storeFuncName", functionDescriptor.Name);
            }

            [Fact]
            public void FindFunctions_throws_for_function_descriptors_with_invalid_parameters()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (Fake).GetMethod("InvalidParamFunc")});

                var message =
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                            .FindFunctions()
                            .ToArray()).Message;

                Assert.Contains("System.Object", message);
                Assert.Contains("p1", message);
                Assert.Contains("InvalidParamFunc", message);
            }

            [Fact]
            public void FindFunctions_throws_for_TVFs_with_ResultTypes()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (Fake).GetMethod("TVFWithResultTypes")});

                var message =
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                            .FindFunctions()
                            .ToArray()).Message;

                Assert.Contains("DbFunctionDetailsAttribute.ResultTypes", message);
            }

            [Fact]
            public void FindFunctions_ignores_empty_ResultTypes_for_non_composable()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (Fake).GetMethod("EmptyResultType")});

                var returnType = new FunctionDiscovery(CreateModel(), mockType.Object)
                    .FindFunctions()
                    .ToArray()[0].ReturnTypes[0];

                Assert.Contains("Edm.Int32", returnType.FullName);
            }

            [Fact]
            public void FindFunctions_throws_if_return_type_and_resultTypes_out_of_sync()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (Fake).GetMethod("StoredProcReturnTypeAndResultTypeMismatch")});

                var message =
                    Assert.Throws<InvalidOperationException>(
                        () => new FunctionDiscovery(CreateModel(), mockType.Object)
                            .FindFunctions()
                            .ToArray()).Message;

                Assert.Contains("ObjectResult<T>", message);
                Assert.Contains("'StoredProcReturnTypeAndResultTypeMismatch'", message);
                Assert.Contains("'System.Int32'", message);
                Assert.Contains("'System.Byte'", message);
                Assert.Contains("DbFunctionDetailsAttribute.ResultTypes", message);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_for_extension_methods()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (StaticFake).GetMethod("ExtensionMethod")});

                mockType
                    .Protected()
                    .Setup<TypeAttributes>("GetAttributeFlagsImpl")
                    .Returns(TypeAttributes.Abstract | TypeAttributes.Sealed);

                var functionDescriptor = new FunctionDiscovery(CreateModel(), mockType.Object)
                    .FindFunctions().SingleOrDefault();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("ExtensionMethod", functionDescriptor.Name);
                Assert.Equal(1, functionDescriptor.Parameters.Count());
                Assert.Equal("param", functionDescriptor.Parameters.First().Key);
                Assert.Equal("Edm.String", functionDescriptor.Parameters.First().Value.FullName);
                Assert.Equal("Edm.Int32", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
            }

            [Fact]
            public void FindFunctions_creates_function_descriptors_for_static_methods()
            {
                var mockType = new Mock<Type>();
                mockType
                    .Setup(t => t.GetMethods(It.IsAny<BindingFlags>()))
                    .Returns(new[] {typeof (StaticFake).GetMethod("StaticMethod")});

                mockType
                    .Protected()
                    .Setup<TypeAttributes>("GetAttributeFlagsImpl")
                    .Returns(TypeAttributes.Abstract | TypeAttributes.Sealed);

                var functionDescriptor = new FunctionDiscovery(CreateModel(), mockType.Object)
                    .FindFunctions().SingleOrDefault();

                Assert.NotNull(functionDescriptor);
                Assert.Equal("StaticMethod", functionDescriptor.Name);
                Assert.Equal(1, functionDescriptor.Parameters.Count());
                Assert.Equal("param", functionDescriptor.Parameters.First().Key);
                Assert.Equal("Edm.String", functionDescriptor.Parameters.First().Value.FullName);
                Assert.Equal("Edm.Int32", functionDescriptor.ReturnTypes[0].FullName);
                Assert.True(functionDescriptor.IsComposable);
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
                    ComplexType.Create("TestComplexType", "Model", DataSpace.CSpace, new[] {prop1, prop2}, null);
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
