// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class MemberDetailTests
    {
        private static MemberDetail MakeIntProperty()
        {
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).IntProperty;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).IntProperty = (int)val!;
            Func<object, int> getter = obj => ((TestReflectionClass)obj).IntProperty;
            Action<object, int> setter = (obj, val) => ((TestReflectionClass)obj).IntProperty = val;
            return TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), nameof(TestReflectionClass.IntProperty),
                isField: false, getter, getterBoxed, setter, setterBoxed);
        }

        private static MemberDetail MakePublicField()
        {
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).PublicField;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).PublicField = (int)val!;
            Func<object, int> getter = obj => ((TestReflectionClass)obj).PublicField;
            Action<object, int> setter = (obj, val) => ((TestReflectionClass)obj).PublicField = val;
            var attrs = new Attribute[] { new TestMarkerAttribute("field") };
            return TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), nameof(TestReflectionClass.PublicField),
                isField: true, getter, getterBoxed, setter, setterBoxed, attrs);
        }

        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var member = MakeIntProperty();

            Assert.Equal(typeof(TestReflectionClass), member.ParentType);
            Assert.Equal(typeof(int), member.Type);
            Assert.Equal(nameof(TestReflectionClass.IntProperty), member.Name);
            Assert.False(member.IsField);
            Assert.True(member.HasGetter);
            Assert.True(member.HasSetter);
            Assert.True(member.IsBacked);
            Assert.False(member.IsStatic);
            Assert.False(member.IsExplicitFromInterface);
        }

        [Fact]
        public void Constructor_Field_IsFieldTrue()
        {
            var member = MakePublicField();

            Assert.True(member.IsField);
        }

        [Fact]
        public void Constructor_NoGetterOrSetter_HasGetterAndSetterFalse()
        {
            var member = TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), "SomeProp",
                isField: false, getter: null, getterBoxed: null, setter: null, setterBoxed: null);

            Assert.False(member.HasGetter);
            Assert.False(member.HasSetter);
            Assert.Null(member.Getter);
            Assert.Null(member.Setter);
            Assert.Null(member.GetterBoxed);
            Assert.Null(member.SetterBoxed);
        }

        [Fact]
        public void GetterBoxed_ReturnsValue()
        {
            var member = MakeIntProperty();
            var obj = new TestReflectionClass { IntProperty = 42 };

            var result = member.GetterBoxed!(obj);

            Assert.Equal(42, result);
        }

        [Fact]
        public void SetterBoxed_SetsValue()
        {
            var member = MakeIntProperty();
            var obj = new TestReflectionClass();

            member.SetterBoxed!(obj, 99);

            Assert.Equal(99, obj.IntProperty);
        }

        [Fact]
        public void Getter_TypedDelegate_ReturnsValue()
        {
            var member = MakeIntProperty();
            var obj = new TestReflectionClass { IntProperty = 7 };

            var getter = (Func<object, int>)member.Getter!;
            var result = getter(obj);

            Assert.Equal(7, result);
        }

        [Fact]
        public void Setter_TypedDelegate_SetsValue()
        {
            var member = MakeIntProperty();
            var obj = new TestReflectionClass();

            var setter = (Action<object, int>)member.Setter!;
            setter(obj, 55);

            Assert.Equal(55, obj.IntProperty);
        }

        [Fact]
        public void Attributes_AreStored()
        {
            var member = MakePublicField();

            Assert.NotNull(member.Attributes);
            Assert.Single(member.Attributes);
            var attr = Assert.IsType<TestMarkerAttribute>(member.Attributes[0]);
            Assert.Equal("field", attr.Label);
        }

        [Fact]
        public void Attributes_EmptyByDefault()
        {
            var member = MakeIntProperty();

            Assert.NotNull(member.Attributes);
            Assert.Empty(member.Attributes);
        }

        [Fact]
        public void TypeDetail_Lazy_ReturnsTypeDetailForMemberType()
        {
            var member = MakeIntProperty();

            var td = member.TypeDetail;

            Assert.NotNull(td);
            Assert.Equal(typeof(int), td.Type);
        }

        [Fact]
        public void TypeDetail_IsCached()
        {
            var member = MakeIntProperty();

            var td1 = member.TypeDetail;
            var td2 = member.TypeDetail;

            Assert.Same(td1, td2);
        }

        [Fact]
        public void MemberInfo_ResolvesPropertyInfo()
        {
            var member = MakeIntProperty();

            var mi = member.MemberInfo;

            Assert.NotNull(mi);
            Assert.Equal(nameof(TestReflectionClass.IntProperty), mi.Name);
        }

        [Fact]
        public void MemberInfo_ResolvesFieldInfo()
        {
            var member = MakePublicField();

            var mi = member.MemberInfo;

            Assert.NotNull(mi);
            Assert.Equal(nameof(TestReflectionClass.PublicField), mi.Name);
        }

        [Fact]
        public void IsStatic_True_WhenSpecified()
        {
            var member = TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), nameof(TestReflectionClass.StaticField),
                isField: true, getter: null, getterBoxed: null, setter: null, setterBoxed: null,
                isStatic: true);

            Assert.True(member.IsStatic);
        }
    }
}
