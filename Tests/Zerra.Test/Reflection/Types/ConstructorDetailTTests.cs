// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class ConstructorDetailTTests
    {
        private static ConstructorDetail<TestReflectionClass> MakeDefaultConstructorT()
        {
            static TestReflectionClass creator(object[] _) => new TestReflectionClass();
            static object creatorBoxed(object[] _) => new TestReflectionClass();
            return TestReflectionDetailFactory.MakeConstructorDetailT(
                Array.Empty<ParameterDetail>(), creator, creatorBoxed);
        }

        private static ConstructorDetail<TestReflectionClass> MakeIntConstructorT()
        {
            var parameters = new[] { TestReflectionDetailFactory.MakeParameter(typeof(int), "value") };
            static TestReflectionClass creator(object[] args) => new TestReflectionClass((int)args![0]!);
            static object creatorBoxed(object[] args) => new TestReflectionClass((int)args![0]!);
            return TestReflectionDetailFactory.MakeConstructorDetailT(parameters, creator, creatorBoxed);
        }

        [Fact]
        public void Constructor_ParentTypeIsT()
        {
            var ctor = MakeDefaultConstructorT();

            Assert.Equal(typeof(TestReflectionClass), ctor.ParentType);
        }

        [Fact]
        public void StronglyTypedCreator_Default_ReturnsTypedInstance()
        {
            var ctor = MakeDefaultConstructorT();

            var instance = ctor.Creator(null);

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void StronglyTypedCreator_WithInt_ReturnsTypedInstance()
        {
            var ctor = MakeIntConstructorT();

            var instance = ctor.Creator(new object?[] { 99 });

            Assert.Equal(99, instance.IntProperty);
        }

        [Fact]
        public void BoxedCreator_ReturnsObject()
        {
            var ctor = MakeDefaultConstructorT();

            var obj = ctor.CreatorBoxed(null);

            Assert.NotNull(obj);
            Assert.IsType<TestReflectionClass>(obj);
        }

        [Fact]
        public void Parameters_AreSet()
        {
            var ctor = MakeIntConstructorT();

            Assert.Single(ctor.Parameters);
            Assert.Equal("value", ctor.Parameters[0].Name);
        }

        [Fact]
        public void Creator_IsStronglyTyped_NotDelegate()
        {
            var ctor = MakeDefaultConstructorT();

            Assert.NotNull(ctor.Creator);
            var result = ctor.Creator(null);
            Assert.IsType<TestReflectionClass>(result);
        }
    }
}
