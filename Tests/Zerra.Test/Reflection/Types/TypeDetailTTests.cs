// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class TypeDetailTTests
    {
        private static TypeDetail<TestReflectionClass> MakeSimpleT()
            => TestReflectionDetailFactory.MakeTypeDetailT<TestReflectionClass>();

        private static TypeDetail<TestReflectionClass> MakeWithCreatorT()
        {
            static TestReflectionClass creator() => new TestReflectionClass();
            static object creatorBoxed() => new TestReflectionClass();
            return TestReflectionDetailFactory.MakeTypeDetailT<TestReflectionClass>(
                creator: creator, creatorBoxed: creatorBoxed);
        }

        private static TypeDetail<TestReflectionClass> MakeWithConstructorsT()
        {
            static TestReflectionClass creator(object[] _) => new TestReflectionClass();
            static object creatorBoxed(object[] _) => new TestReflectionClass();
            var ctor = TestReflectionDetailFactory.MakeConstructorDetailT(
                Array.Empty<ParameterDetail>(), creator, creatorBoxed);
            return TestReflectionDetailFactory.MakeTypeDetailT<TestReflectionClass>(
                constructors: new[] { ctor });
        }

        [Fact]
        public void Constructor_TypeIsT()
        {
            var td = MakeSimpleT();

            Assert.Equal(typeof(TestReflectionClass), td.Type);
        }

        [Fact]
        public void Constructor_NoCreator_HasCreatorFalse()
        {
            var td = MakeSimpleT();

            Assert.False(td.HasCreator);
            Assert.Null(td.Creator);
        }

        [Fact]
        public void Constructor_WithCreator_HasCreatorTrue()
        {
            var td = MakeWithCreatorT();

            Assert.True(td.HasCreator);
            Assert.NotNull(td.Creator);
        }

        [Fact]
        public void StronglyTypedCreator_ReturnsTypedInstance()
        {
            var td = MakeWithCreatorT();

            var instance = td.Creator!();

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void BoxedCreator_ReturnsInstance()
        {
            var td = MakeWithCreatorT();

            var instance = td.CreatorBoxed!();

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void Constructors_StronglyTyped_ReturnedFromProperty()
        {
            var td = MakeWithConstructorsT();

            var ctors = td.Constructors;

            Assert.NotNull(ctors);
            Assert.Single(ctors);
            Assert.IsType<ConstructorDetail<TestReflectionClass>>(ctors[0]);
        }

        [Fact]
        public void StronglyTypedConstructors_Creator_ReturnsTypedInstance()
        {
            var td = MakeWithConstructorsT();

            var instance = td.Constructors[0].Creator(null);

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void Constructors_IsCached()
        {
            var td = MakeWithConstructorsT();

            var c1 = td.Constructors;
            var c2 = td.Constructors;

            Assert.Same(c1, c2);
        }

        [Fact]
        public void Members_InheritedFromBase_WhenProvided()
        {
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).IntProperty;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).IntProperty = (int)val!;
            var member = TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), nameof(TestReflectionClass.IntProperty),
                false, null, getterBoxed, null, setterBoxed);
            var td = TestReflectionDetailFactory.MakeTypeDetailT<TestReflectionClass>(
                members: new[] { member });

            Assert.Single(td.Members);
            Assert.Equal(nameof(TestReflectionClass.IntProperty), td.Members[0].Name);
        }

        [Fact]
        public void CollectionFlags_Propagate()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetailT<List<int>>(
                hasIEnumerable: true,
                hasIEnumerableGeneric: true,
                hasIListGeneric: true,
                hasListGeneric: true,
                iEnumerableGenericInnerType: typeof(int));

            Assert.True(td.HasIEnumerable);
            Assert.True(td.HasIEnumerableGeneric);
            Assert.True(td.HasIListGeneric);
            Assert.True(td.HasListGeneric);
        }

        [Fact]
        public void IsNullable_Propagates()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetailT<TestReflectionClass>(isNullable: true);

            Assert.True(td.IsNullable);
        }
    }
}
