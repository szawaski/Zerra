// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class MethodDetailTests
    {
        private static MethodDetail MakeAddMethod()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(int), "a"),
                TestReflectionDetailFactory.MakeParameter(typeof(int), "b")
            };
            static object callerBoxed(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            Func<object?, object?[]?, int> caller = (obj, args) =>
                ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            var attrs = new Attribute[] { new TestMarkerAttribute("method") };
            return TestReflectionDetailFactory.MakeMethodDetail(
                typeof(TestReflectionClass), nameof(TestReflectionClass.Add),
                typeof(int), null, parameters, caller, callerBoxed, attrs);
        }

        private static MethodDetail MakeGreetMethod()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(string), "name")
            };
            static object callerBoxed(object obj, object[] args) =>
                ((TestReflectionClass)obj!).Greet((string)args![0]!);
            Func<object?, object?[]?, string> caller = (obj, args) =>
                ((TestReflectionClass)obj!).Greet((string)args![0]!);
            return TestReflectionDetailFactory.MakeMethodDetail(
                typeof(TestReflectionClass), nameof(TestReflectionClass.Greet),
                typeof(string), null, parameters, caller, callerBoxed);
        }

        private static MethodDetail MakeStaticMethod()
        {
            var parameters = new[]
            {
                TestReflectionDetailFactory.MakeParameter(typeof(int), "x")
            };
            static object callerBoxed(object _, object[] args) =>
                TestReflectionClass.StaticMethod((int)args![0]!);
            Func<object?, object?[]?, int> caller = (_, args) =>
                TestReflectionClass.StaticMethod((int)args![0]!);
            return TestReflectionDetailFactory.MakeMethodDetail(
                typeof(TestReflectionClass), nameof(TestReflectionClass.StaticMethod),
                typeof(int), null, parameters, caller, callerBoxed, isStatic: true);
        }

        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var method = MakeAddMethod();

            Assert.Equal(typeof(TestReflectionClass), method.ParentType);
            Assert.Equal(nameof(TestReflectionClass.Add), method.Name);
            Assert.Equal(typeof(int), method.ReturnType);
            Assert.Equal(2, method.Parameters.Count);
            Assert.True(method.HasCaller);
            Assert.NotNull(method.Caller);
            Assert.NotNull(method.CallerBoxed);
            Assert.False(method.IsStatic);
            Assert.False(method.IsExplicitFromInterface);
        }

        [Fact]
        public void Constructor_GenericArguments_Empty_WhenNone()
        {
            var method = MakeAddMethod();

            Assert.NotNull(method.GenericArguments);
            Assert.Empty(method.GenericArguments);
        }

        [Fact]
        public void Constructor_StaticMethod_IsStaticTrue()
        {
            var method = MakeStaticMethod();

            Assert.True(method.IsStatic);
        }

        [Fact]
        public void CallerBoxed_InvokesMethod()
        {
            var method = MakeAddMethod();
            var obj = new TestReflectionClass();

            var result = method.CallerBoxed!(obj, new object?[] { 3, 4 });

            Assert.Equal(7, result);
        }

        [Fact]
        public void Caller_TypedDelegate_InvokesMethod()
        {
            var method = MakeAddMethod();
            var obj = new TestReflectionClass();
            var caller = (Func<object?, object?[]?, int>)method.Caller!;

            var result = caller(obj, new object?[] { 10, 20 });

            Assert.Equal(30, result);
        }

        [Fact]
        public void CallerBoxed_StaticMethod_Works()
        {
            var method = MakeStaticMethod();

            var result = method.CallerBoxed!(null, new object?[] { 5 });

            Assert.Equal(10, result);
        }

        [Fact]
        public void CallerBoxed_StringReturn_Works()
        {
            var method = MakeGreetMethod();
            var obj = new TestReflectionClass();

            var result = method.CallerBoxed!(obj, new object?[] { "World" });

            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public void Attributes_AreStored()
        {
            var method = MakeAddMethod();

            Assert.Single(method.Attributes);
            var attr = Assert.IsType<TestMarkerAttribute>(method.Attributes[0]);
            Assert.Equal("method", attr.Label);
        }

        [Fact]
        public void Attributes_EmptyByDefault()
        {
            var method = MakeGreetMethod();

            Assert.Empty(method.Attributes);
        }

        [Fact]
        public void Parameters_HaveCorrectNamesAndTypes()
        {
            var method = MakeAddMethod();

            Assert.Equal("a", method.Parameters[0].Name);
            Assert.Equal(typeof(int), method.Parameters[0].Type);
            Assert.Equal("b", method.Parameters[1].Name);
            Assert.Equal(typeof(int), method.Parameters[1].Type);
        }

        [Fact]
        public void HasCaller_False_WhenCallerIsNull()
        {
            var method = TestReflectionDetailFactory.MakeMethodDetail(
                typeof(TestReflectionClass), "Abstract",
                typeof(void), null, Array.Empty<ParameterDetail>(),
                caller: null, callerBoxed: null);

            Assert.False(method.HasCaller);
            Assert.Null(method.Caller);
            Assert.Null(method.CallerBoxed);
        }

        [Fact]
        public void ToString_ContainsMethodNameAndParams()
        {
            var method = MakeAddMethod();

            var str = method.ToString();

            Assert.Contains("Add", str);
            Assert.Contains("Int32", str);
        }

        [Fact]
        public void ReturnTypeDetail_Lazy_ReturnsCorrectType()
        {
            var method = MakeAddMethod();

            var td = method.ReturnTypeDetail;

            Assert.NotNull(td);
            Assert.Equal(typeof(int), td.Type);
        }

        [Fact]
        public void ReturnTypeDetail_IsCached()
        {
            var method = MakeAddMethod();

            var td1 = method.ReturnTypeDetail;
            var td2 = method.ReturnTypeDetail;

            Assert.Same(td1, td2);
        }

        [Fact]
        public void MethodInfo_ResolvesCorrectly()
        {
            var method = MakeAddMethod();

            var mi = method.MethodInfo;

            Assert.NotNull(mi);
            Assert.Equal(nameof(TestReflectionClass.Add), mi.Name);
        }
    }
}
