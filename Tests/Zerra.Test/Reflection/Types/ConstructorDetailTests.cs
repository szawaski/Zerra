// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class ConstructorDetailTests
    {
        private static ParameterDetail[] NoParams => Array.Empty<ParameterDetail>();

        private static ConstructorDetail MakeDefaultConstructor()
        {
            static object creatorBoxed(object[] _) => new TestReflectionClass();
            Func<object?[]?, TestReflectionClass> creator = _ => new TestReflectionClass();
            return TestReflectionDetailFactory.MakeConstructorDetail(
                typeof(TestReflectionClass), NoParams,
                creator, creatorBoxed);
        }

        private static ConstructorDetail MakeIntConstructor()
        {
            var parameters = new[] { TestReflectionDetailFactory.MakeParameter(typeof(int), "value") };
            static object creatorBoxed(object[] args) => new TestReflectionClass((int)args![0]!);
            Func<object?[]?, TestReflectionClass> creator = args => new TestReflectionClass((int)args![0]!);
            return TestReflectionDetailFactory.MakeConstructorDetail(
                typeof(TestReflectionClass), parameters,
                creator, creatorBoxed);
        }

        private static ConstructorDetail MakeTwoParamConstructor()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(int), "value"),
                TestReflectionDetailFactory.MakeParameter(typeof(string), "text")
            };
            static object creatorBoxed(object[] args) => new TestReflectionClass((int)args![0]!, (string?)args[1]);
            Func<object?[]?, TestReflectionClass> creator = args => new TestReflectionClass((int)args![0]!, (string?)args[1]);
            return TestReflectionDetailFactory.MakeConstructorDetail(
                typeof(TestReflectionClass), parameters,
                creator, creatorBoxed);
        }

        [Fact]
        public void Constructor_SetsParentType()
        {
            var ctor = MakeDefaultConstructor();

            Assert.Equal(typeof(TestReflectionClass), ctor.ParentType);
        }

        [Fact]
        public void Constructor_NoParams_EmptyParameters()
        {
            var ctor = MakeDefaultConstructor();

            Assert.NotNull(ctor.Parameters);
            Assert.Empty(ctor.Parameters);
        }

        [Fact]
        public void Constructor_WithParams_ParametersSet()
        {
            var ctor = MakeIntConstructor();

            Assert.Single(ctor.Parameters);
            Assert.Equal("value", ctor.Parameters[0].Name);
            Assert.Equal(typeof(int), ctor.Parameters[0].Type);
        }

        [Fact]
        public void Constructor_TwoParams_ParametersSet()
        {
            var ctor = MakeTwoParamConstructor();

            Assert.Equal(2, ctor.Parameters.Count);
            Assert.Equal("value", ctor.Parameters[0].Name);
            Assert.Equal("text", ctor.Parameters[1].Name);
        }

        [Fact]
        public void CreatorBoxed_Default_CreatesInstance()
        {
            var ctor = MakeDefaultConstructor();

            var instance = ctor.CreatorBoxed(null);

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void CreatorBoxed_WithInt_CreatesInstanceWithValue()
        {
            var ctor = MakeIntConstructor();

            var instance = (TestReflectionClass)ctor.CreatorBoxed(new object?[] { 42 });

            Assert.Equal(42, instance.IntProperty);
        }

        [Fact]
        public void CreatorBoxed_TwoParams_CreatesInstanceWithValues()
        {
            var ctor = MakeTwoParamConstructor();

            var instance = (TestReflectionClass)ctor.CreatorBoxed(new object?[] { 10, "hello" });

            Assert.Equal(10, instance.IntProperty);
            Assert.Equal("hello", instance.StringProperty);
        }

        [Fact]
        public void Creator_Delegate_IsSet()
        {
            var ctor = MakeDefaultConstructor();

            Assert.NotNull(ctor.Creator);
        }

        [Fact]
        public void ToString_NoParams_ShowsTypeName()
        {
            var ctor = MakeDefaultConstructor();

            var str = ctor.ToString();

            Assert.Contains("TestReflectionClass", str);
        }

        [Fact]
        public void ToString_WithParams_ShowsParamTypes()
        {
            var ctor = MakeIntConstructor();

            var str = ctor.ToString();

            Assert.Contains("Int32", str);
        }

        [Fact]
        public void GetConstructorInfo_DefaultConstructor_ReturnsConstructorInfo()
        {
            var ctor = MakeDefaultConstructor();

            var ci = ctor.GetConstructorInfo;

            Assert.NotNull(ci);
            Assert.Equal(typeof(TestReflectionClass), ci.DeclaringType);
            Assert.Empty(ci.GetParameters());
        }

        [Fact]
        public void GetConstructorInfo_IntConstructor_ReturnsConstructorInfo()
        {
            var ctor = MakeIntConstructor();

            var ci = ctor.GetConstructorInfo;

            Assert.NotNull(ci);
            Assert.Single(ci.GetParameters());
            Assert.Equal(typeof(int), ci.GetParameters()[0].ParameterType);
        }
    }
}
