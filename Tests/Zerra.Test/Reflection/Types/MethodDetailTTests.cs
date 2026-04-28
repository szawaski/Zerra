// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class MethodDetailTTests
    {
        private static MethodDetail<int> MakeAddMethodT()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(int), "a"),
                TestReflectionDetailFactory.MakeParameter(typeof(int), "b")
            };
            static int caller(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            static object callerBoxed(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            return TestReflectionDetailFactory.MakeMethodDetailT<int>(
                typeof(TestReflectionClass), nameof(TestReflectionClass.Add),
                null, parameters, caller, callerBoxed);
        }

        private static MethodDetail<string> MakeGreetMethodT()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(string), "name")
            };
            static string caller(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Greet((string)args![0]!);
            static object callerBoxed(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Greet((string)args![0]!);
            return TestReflectionDetailFactory.MakeMethodDetailT<string>(
                typeof(TestReflectionClass), nameof(TestReflectionClass.Greet),
                null, parameters, caller, callerBoxed);
        }

        [Fact]
        public void Constructor_SetsBaseProperties()
        {
            var method = MakeAddMethodT();

            Assert.Equal(typeof(TestReflectionClass), method.ParentType);
            Assert.Equal(nameof(TestReflectionClass.Add), method.Name);
            Assert.Equal(typeof(int), method.ReturnType);
            Assert.Equal(2, method.Parameters.Count);
            Assert.True(method.HasCaller);
        }

        [Fact]
        public void StronglyTypedCaller_ReturnsTypedResult()
        {
            var method = MakeAddMethodT();
            var obj = new TestReflectionClass();

            var result = method.Caller(obj, new object?[] { 6, 7 });

            Assert.Equal(13, result);
        }

        [Fact]
        public void BoxedCaller_ReturnsBoxedResult()
        {
            var method = MakeAddMethodT();
            var obj = new TestReflectionClass();

            var result = method.CallerBoxed!(obj, new object?[] { 2, 3 });

            Assert.Equal(5, result);
        }

        [Fact]
        public void StringReturn_TypedCaller_Works()
        {
            var method = MakeGreetMethodT();
            var obj = new TestReflectionClass();

            var result = method.Caller(obj, new object?[] { "Alice" });

            Assert.Equal("Hello, Alice!", result);
        }
    }
}
