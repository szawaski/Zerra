// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class MemberDetailTTests
    {
        private static MemberDetail<int> MakeIntPropertyT()
        {
            static int getter(object obj) => ((TestReflectionClass)obj).IntProperty;
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).IntProperty;
            static void setter(object obj, int val) => ((TestReflectionClass)obj).IntProperty = val;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).IntProperty = (int)val!;
            return TestReflectionDetailFactory.MakeMemberDetailT<int>(
                typeof(TestReflectionClass), nameof(TestReflectionClass.IntProperty),
                isField: false, getter, getterBoxed, setter, setterBoxed);
        }

        private static MemberDetail<string?> MakeStringPropertyT()
        {
            static string getter(object obj) => ((TestReflectionClass)obj).StringProperty;
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).StringProperty;
            static void setter(object obj, string val) => ((TestReflectionClass)obj).StringProperty = val;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).StringProperty = (string?)val;
            return TestReflectionDetailFactory.MakeMemberDetailT<string?>(
                typeof(TestReflectionClass), nameof(TestReflectionClass.StringProperty),
                isField: false, getter, getterBoxed, setter, setterBoxed);
        }

        [Fact]
        public void Constructor_SetsBaseProperties()
        {
            var member = MakeIntPropertyT();

            Assert.Equal(typeof(TestReflectionClass), member.ParentType);
            Assert.Equal(typeof(int), member.Type);
            Assert.Equal(nameof(TestReflectionClass.IntProperty), member.Name);
            Assert.False(member.IsField);
            Assert.True(member.HasGetter);
            Assert.True(member.HasSetter);
        }

        [Fact]
        public void StronglyTypedGetter_ReturnsValue()
        {
            var member = MakeIntPropertyT();
            var obj = new TestReflectionClass { IntProperty = 42 };

            var result = member.Getter!(obj);

            Assert.Equal(42, result);
        }

        [Fact]
        public void StronglyTypedSetter_SetsValue()
        {
            var member = MakeIntPropertyT();
            var obj = new TestReflectionClass();

            member.Setter!(obj, 77);

            Assert.Equal(77, obj.IntProperty);
        }

        [Fact]
        public void BoxedGetter_ReturnsBoxedValue()
        {
            var member = MakeIntPropertyT();
            var obj = new TestReflectionClass { IntProperty = 5 };

            var result = member.GetterBoxed!(obj);

            Assert.Equal(5, result);
        }

        [Fact]
        public void BoxedSetter_SetsValue()
        {
            var member = MakeIntPropertyT();
            var obj = new TestReflectionClass();

            member.SetterBoxed!(obj, 11);

            Assert.Equal(11, obj.IntProperty);
        }

        [Fact]
        public void StringProperty_GetterSetter_Work()
        {
            var member = MakeStringPropertyT();
            var obj = new TestReflectionClass();

            member.Setter!(obj, "hello");
            var result = member.Getter!(obj);

            Assert.Equal("hello", result);
        }

        [Fact]
        public void TypeDetail_ReturnsGenericTypeDetail()
        {
            var member = MakeIntPropertyT();

            var td = member.TypeDetail;

            Assert.NotNull(td);
            Assert.IsType<TypeDetail<int>>(td);
            Assert.Equal(typeof(int), td.Type);
        }

        [Fact]
        public void TypeDetail_IsCached()
        {
            var member = MakeIntPropertyT();

            var td1 = member.TypeDetail;
            var td2 = member.TypeDetail;

            Assert.Same(td1, td2);
        }

        [Fact]
        public void NullGetter_HasGetterFalse()
            {
                var member = TestReflectionDetailFactory.MakeMemberDetailT<int>(
                    typeof(TestReflectionClass), "ReadOnly",
                    isField: false, (Func<object, int>?)null, getterBoxed: null, (Action<object, int>?)null, setterBoxed: null);

            Assert.False(member.HasGetter);
            Assert.Null(member.Getter);
        }
    }
}
