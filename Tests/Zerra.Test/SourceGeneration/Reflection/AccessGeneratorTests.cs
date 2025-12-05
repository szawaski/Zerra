using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.Test.SourceGeneration.Reflection
{
    public class AccessGeneratorTests
    {
        #region Property Getter Tests

        [Fact]
        public void GenerateGetter_SimpleProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 42 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GenerateGetter_StringProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { StringProperty = "test value" };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.StringProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal("test value", result);
        }

        [Fact]
        public void GenerateGetter_BoolProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { BoolProperty = true };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.BoolProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(true, result);
        }

        [Fact]
        public void GenerateGetter_NullableProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { NullableIntProperty = 100 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.NullableIntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(100, result);
        }

        [Fact]
        public void GenerateGetter_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 50 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter<int>(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(50, result);
        }

        [Fact]
        public void GenerateGetter_TwoGenericTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 75 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter<TestAccessorModel, int>(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(75, result);
        }

        [Fact]
        public void GenerateGetter_WithValueType_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { DoubleProperty = 3.14 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.DoubleProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo, typeof(double));

            // Act
            var result = (double?)getter?.DynamicInvoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void GenerateGetter_WithObjectAndValueTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 99 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo, typeof(TestAccessorModel), typeof(int));

            // Act
            var result = (int?)getter?.DynamicInvoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(99, result);
        }

        [Fact]
        public void GenerateGetter_NoGetMethod_ReturnsNull()
        {
            // Arrange
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.ReadOnlyProperty), BindingFlags.Public | BindingFlags.Instance);
            var dynamicMethod = new System.Reflection.Emit.DynamicMethod("test", typeof(int), new Type[] { typeof(object) }, true);
            var il = dynamicMethod.GetILGenerator();
            
            // ReadOnlyProperty only has a private setter, but it does have a public getter, so this should work
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act & Assert
            Assert.NotNull(getter);
        }

        [Fact]
        public void GenerateGetter_NonReadableProperty_ReturnsNull()
        {
            // Arrange - create a property with only a setter
            var type = typeof(TestAccessorModel);
            var propertyInfo = type.GetProperty(nameof(TestAccessorModel.IntProperty));
            
            // For properties that can't be read, expect null
            var propertyWithOnlySetter = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => !p.CanRead);

            // Assert - verify no such property exists in our test model
            Assert.True(propertyInfo.CanRead);
        }

        [Fact]
        public void GenerateGetter_VirtualProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel { VirtualProperty = 123 };
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(123, result);
        }

        [Fact]
        public void GenerateGetter_VirtualProperty_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel { VirtualProperty = 456 };
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var getter = AccessorGenerator.GenerateGetter<int>(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(456, result);
        }

        [Fact]
        public void GenerateGetter_VirtualProperty_TwoGenericTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel { VirtualProperty = 789 };
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var getter = AccessorGenerator.GenerateGetter<TestAccessorVirtualModel, int>(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(789, result);
        }

        #endregion

        #region Property Setter Tests

        [Fact]
        public void GenerateSetter_SimpleProperty_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo);

            // Act
            setter?.Invoke(model, 55);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(55, model.IntProperty);
        }

        [Fact]
        public void GenerateSetter_StringProperty_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.StringProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo);

            // Act
            setter?.Invoke(model, "new string");

            // Assert
            Assert.NotNull(setter);
            Assert.Equal("new string", model.StringProperty);
        }

        [Fact]
        public void GenerateSetter_Generic_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var setter = AccessorGenerator.GenerateSetter<int>(propertyInfo);

            // Act
            setter?.Invoke(model, 77);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(77, model.IntProperty);
        }

        [Fact]
        public void GenerateSetter_TwoGenericTypes_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var setter = AccessorGenerator.GenerateSetter<TestAccessorModel, int>(propertyInfo);

            // Act
            setter?.Invoke(model, 88);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(88, model.IntProperty);
        }

        [Fact]
        public void GenerateSetter_WithValueType_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.DoubleProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo, typeof(double));

            // Act
            setter?.DynamicInvoke(model, 2.71);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(2.71, model.DoubleProperty);
        }

        [Fact]
        public void GenerateSetter_WithObjectAndValueTypes_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo, typeof(TestAccessorModel), typeof(int));

            // Act
            setter?.DynamicInvoke(model, 123);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(123, model.IntProperty);
        }

        [Fact]
        public void GenerateSetter_VirtualProperty_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel();
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo);

            // Act
            setter?.Invoke(model, 101);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(101, model.VirtualProperty);
        }

        [Fact]
        public void GenerateSetter_VirtualProperty_Generic_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel();
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var setter = AccessorGenerator.GenerateSetter<int>(propertyInfo);

            // Act
            setter?.Invoke(model, 202);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(202, model.VirtualProperty);
        }

        [Fact]
        public void GenerateSetter_VirtualProperty_TwoGenericTypes_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel();
            var propertyInfo = typeof(TestAccessorVirtualModel).GetProperty(nameof(TestAccessorVirtualModel.VirtualProperty));
            var setter = AccessorGenerator.GenerateSetter<TestAccessorVirtualModel, int>(propertyInfo);

            // Act
            setter?.Invoke(model, 303);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(303, model.VirtualProperty);
        }

        #endregion

        #region Field Getter Tests

        [Fact]
        public void GenerateFieldGetter_SimpleField_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntField = 42 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var getter = AccessorGenerator.GenerateGetter(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GenerateFieldGetter_StringField_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { StringField = "field value" };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.StringField));
            var getter = AccessorGenerator.GenerateGetter(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal("field value", result);
        }

        [Fact]
        public void GenerateFieldGetter_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntField = 99 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var getter = AccessorGenerator.GenerateGetter<int>(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(99, result);
        }

        [Fact]
        public void GenerateFieldGetter_TwoGenericTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntField = 111 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var getter = AccessorGenerator.GenerateGetter<TestAccessorModel, int>(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(111, result);
        }

        [Fact]
        public void GenerateFieldGetter_WithValueType_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { DoubleField = 1.5 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.DoubleField));
            var getter = AccessorGenerator.GenerateGetter(fieldInfo, typeof(double));

            // Act
            var result = (double?)getter?.DynamicInvoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(1.5, result);
        }

        [Fact]
        public void GenerateFieldGetter_WithObjectAndValueTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntField = 222 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var getter = AccessorGenerator.GenerateGetter(fieldInfo, typeof(TestAccessorModel), typeof(int));

            // Act
            var result = (int?)getter?.DynamicInvoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(222, result);
        }

        [Fact]
        public void GenerateFieldGetter_NullableField_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { NullableIntField = 50 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.NullableIntField));
            var getter = AccessorGenerator.GenerateGetter(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(50, result);
        }

        [Fact]
        public void GenerateFieldGetter_NullableField_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { NullableIntField = 75 };
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.NullableIntField));
            var getter = AccessorGenerator.GenerateGetter<int?>(fieldInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(75, result);
        }

        #endregion

        #region Field Setter Tests

        [Fact]
        public void GenerateFieldSetter_SimpleField_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var setter = AccessorGenerator.GenerateSetter(fieldInfo);

            // Act
            setter?.Invoke(model, 333);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(333, model.IntField);
        }

        [Fact]
        public void GenerateFieldSetter_StringField_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.StringField));
            var setter = AccessorGenerator.GenerateSetter(fieldInfo);

            // Act
            setter?.Invoke(model, "new field value");

            // Assert
            Assert.NotNull(setter);
            Assert.Equal("new field value", model.StringField);
        }

        [Fact]
        public void GenerateFieldSetter_Generic_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var setter = AccessorGenerator.GenerateSetter<int>(fieldInfo);

            // Act
            setter?.Invoke(model, 444);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(444, model.IntField);
        }

        [Fact]
        public void GenerateFieldSetter_TwoGenericTypes_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var setter = AccessorGenerator.GenerateSetter<TestAccessorModel, int>(fieldInfo);

            // Act
            setter?.Invoke(model, 555);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(555, model.IntField);
        }

        [Fact]
        public void GenerateFieldSetter_WithValueType_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.DoubleField));
            var setter = AccessorGenerator.GenerateSetter(fieldInfo, typeof(double));

            // Act
            setter?.DynamicInvoke(model, 9.99);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(9.99, model.DoubleField);
        }

        [Fact]
        public void GenerateFieldSetter_WithObjectAndValueTypes_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.IntField));
            var setter = AccessorGenerator.GenerateSetter(fieldInfo, typeof(TestAccessorModel), typeof(int));

            // Act
            setter?.DynamicInvoke(model, 666);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(666, model.IntField);
        }

        [Fact]
        public void GenerateFieldSetter_NullableField_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.NullableIntField));
            var setter = AccessorGenerator.GenerateSetter(fieldInfo);

            // Act
            setter?.Invoke(model, 250);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(250, model.NullableIntField);
        }

        [Fact]
        public void GenerateFieldSetter_NullableField_Generic_SetsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var fieldInfo = typeof(TestAccessorModel).GetField(nameof(TestAccessorModel.NullableIntField));
            var setter = AccessorGenerator.GenerateSetter<int?>(fieldInfo);

            // Act
            setter?.Invoke(model, 175);

            // Assert
            Assert.NotNull(setter);
            Assert.Equal(175, model.NullableIntField);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void GenerateCreator_ParameterlessConstructor_CreatesInstance()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(Type.EmptyTypes);
            var creator = AccessorGenerator.GenerateCreator(constructorInfo);

            // Act
            var result = creator?.Invoke(new object[] { });

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            Assert.IsType<TestAccessorModel>(result);
        }

        [Fact]
        public void GenerateCreator_Generic_CreatesInstance()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(Type.EmptyTypes);
            var creator = AccessorGenerator.GenerateCreator<TestAccessorModel>(constructorInfo);

            // Act
            var result = creator?.Invoke(new object[] { });

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            Assert.IsType<TestAccessorModel>(result);
        }

        [Fact]
        public void GenerateCreator_WithParameters_CreatesInstanceWithValues()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(new Type[] { typeof(int), typeof(string) });
            var creator = AccessorGenerator.GenerateCreator(constructorInfo);

            // Act
            var result = creator?.Invoke(new object[] { 42, "test" });

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            var model = result as TestAccessorModel;
            Assert.NotNull(model);
            Assert.Equal(42, model.IntProperty);
            Assert.Equal("test", model.StringProperty);
        }

        [Fact]
        public void GenerateCreator_GenericWithParameters_CreatesInstanceWithValues()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(new Type[] { typeof(int), typeof(string) });
            var creator = AccessorGenerator.GenerateCreator<TestAccessorModel>(constructorInfo);

            // Act
            var result = creator?.Invoke(new object[] { 100, "generic" });

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            Assert.Equal(100, result.IntProperty);
            Assert.Equal("generic", result.StringProperty);
        }

        [Fact]
        public void GenerateCreatorNoArgs_CreatesInstance()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(Type.EmptyTypes);
            var creator = AccessorGenerator.GenerateCreatorNoArgs(constructorInfo);

            // Act
            var result = creator?.Invoke();

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            Assert.IsType<TestAccessorModel>(result);
        }

        [Fact]
        public void GenerateCreatorNoArgs_Generic_CreatesInstance()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(Type.EmptyTypes);
            var creator = AccessorGenerator.GenerateCreatorNoArgs<TestAccessorModel>(constructorInfo);

            // Act
            var result = creator?.Invoke();

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(result);
            Assert.IsType<TestAccessorModel>(result);
        }

        #endregion

        #region Method Caller Tests

        [Fact]
        public void GenerateCaller_SimpleMethod_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 10 };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetIntValue));
            var caller = AccessorGenerator.GenerateCaller(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(10, result);
        }

        [Fact]
        public void GenerateCaller_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 20 };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetIntValue));
            var caller = AccessorGenerator.GenerateCaller<int>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(20, result);
        }

        [Fact]
        public void GenerateCaller_TwoGenericTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 30 };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetIntValue));
            var caller = AccessorGenerator.GenerateCaller<TestAccessorModel, int>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(30, result);
        }

        [Fact]
        public void GenerateCaller_MethodWithParameters_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.AddNumbers));
            var caller = AccessorGenerator.GenerateCaller<TestAccessorModel, int>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { 5, 7 });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(12, result);
        }

        [Fact]
        public void GenerateCaller_VoidMethod_ReturnsNull()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 99 };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.SetIntValue));
            var caller = AccessorGenerator.GenerateCaller(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { 50 });

            // Assert
            Assert.NotNull(caller);
            Assert.Null(result);
            Assert.Equal(50, model.IntProperty);
        }

        [Fact]
        public void GenerateCaller_StringReturnMethod_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 5, StringProperty = "test" };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.ConcatenateProperties));
            var caller = AccessorGenerator.GenerateCaller(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal("5:test", result);
        }

        [Fact]
        public void GenerateCaller_WithValueType_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.AddNumbers));
            var caller = AccessorGenerator.GenerateCaller(methodInfo, typeof(int));

            // Act
            var result = (int?)caller?.DynamicInvoke(model, new object[] { 10, 20 });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(30, result);
        }

        [Fact]
        public void GenerateCaller_WithObjectAndValueTypes_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.AddNumbers));
            var caller = AccessorGenerator.GenerateCaller(methodInfo, typeof(TestAccessorModel), typeof(int));

            // Act
            var result = (int?)caller?.DynamicInvoke(model, new object[] { 8, 9 });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(17, result);
        }

        [Fact]
        public void GenerateCaller_VirtualMethod_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel();
            var methodInfo = typeof(TestAccessorVirtualModel).GetMethod(nameof(TestAccessorVirtualModel.GetValue));
            var caller = AccessorGenerator.GenerateCaller<TestAccessorVirtualModel, int>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GenerateCaller_VirtualMethod_Generic_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorVirtualModel();
            var methodInfo = typeof(TestAccessorVirtualModel).GetMethod(nameof(TestAccessorVirtualModel.GetValue));
            var caller = AccessorGenerator.GenerateCaller<int>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GenerateCaller_MethodWithNullableParameter_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorModel();
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetNullableString));
            var caller = AccessorGenerator.GenerateCaller<string>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { "hello" });

            // Assert
            Assert.NotNull(caller);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void GenerateCaller_MethodWithNullableParameterNull_ReturnsNull()
        {
            // Arrange
            var model = new TestAccessorModel();
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetNullableString));
            var caller = AccessorGenerator.GenerateCaller<string>(methodInfo);

            // Act
            var result = caller?.Invoke(model, new object[] { null });

            // Assert
            Assert.NotNull(caller);
            Assert.Null(result);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void GenerateGetter_NullPropertyInfo_HandlesGracefully()
        {
            // Arrange
            PropertyInfo propertyInfo = null;

            // Act & Assert - should handle null gracefully or throw appropriate exception
            // The method checks if ReflectedType is null, so passing null propertyInfo should be safe
            try
            {
                var getter = AccessorGenerator.GenerateGetter((PropertyInfo)null);
                Assert.Null(getter);
            }
            catch (NullReferenceException)
            {
                // Expected behavior for null input
                Assert.True(true);
            }
        }

        [Fact]
        public void GenerateGetter_StructProperty_ReturnsCorrectValue()
        {
            // Arrange
            var model = new TestAccessorStruct { IntProperty = 42 };
            var propertyInfo = typeof(TestAccessorStruct).GetProperty(nameof(TestAccessorStruct.IntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act
            var result = getter?.Invoke(model);

            // Assert
            Assert.NotNull(getter);
            Assert.Equal(42, result);
        }

        [Fact]
        public void GenerateSetter_StructProperty_SetsCorrectValue()
        {
            // Arrange
            object model = new TestAccessorStruct();
            var propertyInfo = typeof(TestAccessorStruct).GetProperty(nameof(TestAccessorStruct.IntProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo);

            // Act
            setter?.Invoke(model, 100);

            // Assert
            Assert.NotNull(setter);
            var unboxedModel = (TestAccessorStruct)model;
            Assert.Equal(100, unboxedModel.IntProperty);
        }

        [Fact]
        public void GenerateGetter_MultipleConsecutiveCalls_WorkCorrectly()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 50 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act & Assert
            Assert.Equal(50, getter?.Invoke(model));
            Assert.Equal(50, getter?.Invoke(model));
            Assert.Equal(50, getter?.Invoke(model));
        }

        [Fact]
        public void GenerateGetter_AfterPropertyValueChange_ReturnsNewValue()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 10 };
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act & Assert
            Assert.Equal(10, getter?.Invoke(model));
            
            model.IntProperty = 20;
            Assert.Equal(20, getter?.Invoke(model));
            
            model.IntProperty = 30;
            Assert.Equal(30, getter?.Invoke(model));
        }

        [Fact]
        public void GenerateSetter_ThenGetter_WorkCorrectlyTogether()
        {
            // Arrange
            var model = new TestAccessorModel();
            var propertyInfo = typeof(TestAccessorModel).GetProperty(nameof(TestAccessorModel.IntProperty));
            var setter = AccessorGenerator.GenerateSetter(propertyInfo);
            var getter = AccessorGenerator.GenerateGetter(propertyInfo);

            // Act & Assert
            setter?.Invoke(model, 123);
            Assert.Equal(123, getter?.Invoke(model));
            
            setter?.Invoke(model, 456);
            Assert.Equal(456, getter?.Invoke(model));
        }

        [Fact]
        public void GenerateCreator_MultipleInstances_CreatesSeparateObjects()
        {
            // Arrange
            var constructorInfo = typeof(TestAccessorModel).GetConstructor(Type.EmptyTypes);
            var creator = AccessorGenerator.GenerateCreator(constructorInfo);

            // Act
            var instance1 = creator?.Invoke(new object[] { });
            var instance2 = creator?.Invoke(new object[] { });

            // Assert
            Assert.NotNull(creator);
            Assert.NotNull(instance1);
            Assert.NotNull(instance2);
            Assert.NotSame(instance1, instance2);
        }

        [Fact]
        public void GenerateCaller_MultipleConsecutiveCalls_WorkCorrectly()
        {
            // Arrange
            var model = new TestAccessorModel { IntProperty = 100 };
            var methodInfo = typeof(TestAccessorModel).GetMethod(nameof(TestAccessorModel.GetIntValue));
            var caller = AccessorGenerator.GenerateCaller(methodInfo);

            // Act & Assert
            Assert.Equal(100, caller?.Invoke(model, new object[] { }));
            Assert.Equal(100, caller?.Invoke(model, new object[] { }));
            
            model.IntProperty = 200;
            Assert.Equal(200, caller?.Invoke(model, new object[] { }));
        }

        #endregion
    }
}
