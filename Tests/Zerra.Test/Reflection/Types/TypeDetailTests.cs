// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class TypeDetailTests
    {
        private static TypeDetail MakeSimple(Type type, IReadOnlyList<Attribute>? attributes = null)
            => TestReflectionDetailFactory.MakeTypeDetail(type, attributes: attributes);

        private static TypeDetail MakeWithCreator()
        {
            static object creatorBoxed() => new TestReflectionClass();
            Func<TestReflectionClass> creator = () => new TestReflectionClass();
            return TestReflectionDetailFactory.MakeTypeDetail(
                typeof(TestReflectionClass),
                creator: creator,
                creatorBoxed: creatorBoxed);
        }

        private static TypeDetail MakeWithMembers()
        {
            static object getterBoxed(object obj) => ((TestReflectionClass)obj).IntProperty;
            static void setterBoxed(object obj, object val) => ((TestReflectionClass)obj).IntProperty = (int)val!;
            var member = TestReflectionDetailFactory.MakeMemberDetail(
                typeof(TestReflectionClass), typeof(int), nameof(TestReflectionClass.IntProperty),
                false, null, getterBoxed, null, setterBoxed);
            return TestReflectionDetailFactory.MakeTypeDetail(
                typeof(TestReflectionClass),
                members: new[] { member });
        }

        private static TypeDetail MakeWithConstructors()
        {
            static object creatorBoxed(object[] _) => new TestReflectionClass();
            Func<object?[]?, TestReflectionClass> creator = _ => new TestReflectionClass();
            var ctor = TestReflectionDetailFactory.MakeConstructorDetail(
                typeof(TestReflectionClass), Array.Empty<ParameterDetail>(), creator, creatorBoxed);
            return TestReflectionDetailFactory.MakeTypeDetail(
                typeof(TestReflectionClass),
                constructors: new[] { ctor });
        }

        private static TypeDetail MakeWithMethods()
        {
            var parameters = new[] { TestReflectionDetailFactory.MakeParameter(typeof(int), "a"), TestReflectionDetailFactory.MakeParameter(typeof(int), "b") };
            static object callerBoxed(object obj, object[] args) => ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            Func<object?, object?[]?, int> caller = (obj, args) => ((TestReflectionClass)obj!).Add((int)args![0]!, (int)args[1]!);
            var method = TestReflectionDetailFactory.MakeMethodDetail(
                typeof(TestReflectionClass), nameof(TestReflectionClass.Add), typeof(int), null, parameters, caller, callerBoxed);
            return TestReflectionDetailFactory.MakeTypeDetail(
                typeof(TestReflectionClass),
                methods: new[] { method });
        }

        [Fact]
        public void Constructor_SetsType()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.Equal(typeof(TestReflectionClass), td.Type);
        }

        [Fact]
        public void Constructor_NullCreator_HasCreatorFalse()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.False(td.HasCreator);
            Assert.Null(td.Creator);
            Assert.Null(td.CreatorBoxed);
        }

        [Fact]
        public void Constructor_WithCreator_HasCreatorTrue()
        {
            var td = MakeWithCreator();

            Assert.True(td.HasCreator);
            Assert.NotNull(td.Creator);
            Assert.NotNull(td.CreatorBoxed);
        }

        [Fact]
        public void CreatorBoxed_CreatesInstance()
        {
            var td = MakeWithCreator();

            var instance = td.CreatorBoxed!();

            Assert.NotNull(instance);
            Assert.IsType<TestReflectionClass>(instance);
        }

        [Fact]
        public void Members_WhenSupplied_ReturnsThem()
        {
            var td = MakeWithMembers();

            Assert.NotNull(td.Members);
            Assert.Single(td.Members);
            Assert.Equal(nameof(TestReflectionClass.IntProperty), td.Members[0].Name);
        }

        [Fact]
        public void Constructors_WhenSupplied_ReturnsThem()
        {
            var td = MakeWithConstructors();

            Assert.NotNull(td.Constructors);
            Assert.Single(td.Constructors);
            Assert.Empty(td.Constructors[0].Parameters);
        }

        [Fact]
        public void Methods_WhenSupplied_ReturnsThem()
        {
            var td = MakeWithMethods();

            Assert.NotNull(td.Methods);
            Assert.Single(td.Methods);
            Assert.Equal(nameof(TestReflectionClass.Add), td.Methods[0].Name);
        }

        [Fact]
        public void IsNullable_TrueWhenSet()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(int?), isNullable: true, innerType: typeof(int));

            Assert.True(td.IsNullable);
        }

        [Fact]
        public void IsNullable_FalseByDefault()
        {
            var td = MakeSimple(typeof(int));

            Assert.False(td.IsNullable);
        }

        [Fact]
        public void CoreType_SetWhenProvided()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(int), coreType: Zerra.Reflection.CoreType.Int32);

            Assert.Equal(Zerra.Reflection.CoreType.Int32, td.CoreType);
        }

        [Fact]
        public void CoreType_NullWhenNotProvided()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.Null(td.CoreType);
        }

        [Fact]
        public void EnumUnderlyingType_SetWhenEnum()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(TestReflectionEnum),
                enumUnderlyingType: Zerra.Reflection.CoreEnumType.Int32);

            Assert.Equal(Zerra.Reflection.CoreEnumType.Int32, td.EnumUnderlyingType);
        }

        [Fact]
        public void CollectionFlags_HasIEnumerable_Set()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(List<int>),
                hasIEnumerable: true,
                hasIEnumerableGeneric: true,
                hasIListGeneric: true,
                hasListGeneric: true,
                iEnumerableGenericInnerType: typeof(int));

            Assert.True(td.HasIEnumerable);
            Assert.True(td.HasIEnumerableGeneric);
            Assert.True(td.HasIListGeneric);
            Assert.True(td.HasListGeneric);
            Assert.Equal(typeof(int), td.IEnumerableGenericInnerType);
        }

        [Fact]
        public void CollectionFlags_IsIEnumerable_Set()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(IEnumerable<int>),
                isIEnumerableGeneric: true);

            Assert.True(td.IsIEnumerableGeneric);
        }

        [Fact]
        public void DictionaryFlags_Set()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(Dictionary<string, int>),
                hasIDictionaryGeneric: true,
                hasDictionaryGeneric: true,
                dictionaryInnerType: typeof(int));

            Assert.True(td.HasIDictionaryGeneric);
            Assert.True(td.HasDictionaryGeneric);
            Assert.Equal(typeof(int), td.DictionaryInnerType);
        }

        [Fact]
        public void InnerType_Set()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(int?),
                isNullable: true, innerType: typeof(int));

            Assert.Equal(typeof(int), td.InnerType);
        }

        [Fact]
        public void InnerTypes_Set()
        {
            var innerTypes = new[] { typeof(string), typeof(int) };
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(Dictionary<string, int>),
                innerTypes: innerTypes);

            Assert.Equal(2, td.InnerTypes.Count);
            Assert.Contains(typeof(string), td.InnerTypes);
            Assert.Contains(typeof(int), td.InnerTypes);
        }

        [Fact]
        public void BaseTypes_Set()
        {
            var baseTypes = new[] { typeof(TestReflectionClass) };
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(TestReflectionSubClass),
                baseTypes: baseTypes);

            Assert.Single(td.BaseTypes);
            Assert.Equal(typeof(TestReflectionClass), td.BaseTypes[0]);
        }

        [Fact]
        public void Interfaces_Set()
        {
            var interfaces = new[] { typeof(ITestReflectionInterface) };
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(TestReflectionClass),
                interfaces: interfaces);

            Assert.Single(td.Interfaces);
            Assert.Equal(typeof(ITestReflectionInterface), td.Interfaces[0]);
        }

        [Fact]
        public void Attributes_WhenProvided_AreStored()
        {
            var attrs = new Attribute[] { new TestMarkerAttribute("class") };
            var td = MakeSimple(typeof(TestReflectionClass), attrs);

            Assert.Single(td.Attributes);
            var attr = Assert.IsType<TestMarkerAttribute>(td.Attributes[0]);
            Assert.Equal("class", attr.Label);
        }

        [Fact]
        public void Attributes_WhenNull_LazilyGeneratedFromType()
        {
            // When attributes are null they get generated dynamically; the real type has [TestMarker]
            var td = MakeSimple(typeof(TestReflectionClass));

            // Just verify accessing attributes doesn't throw
            var attrs = td.Attributes;
            Assert.NotNull(attrs);
        }

        [Fact]
        public void InnerTypeDetail_NullWhenNoInnerType()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.Null(td.InnerTypeDetail);
        }

        [Fact]
        public void InnerTypeDetail_ReturnsDetailForInnerType()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(int?),
                isNullable: true, innerType: typeof(int));

            var innerDetail = td.InnerTypeDetail;

            Assert.NotNull(innerDetail);
            Assert.Equal(typeof(int), innerDetail!.Type);
        }

        [Fact]
        public void InnerTypeDetail_IsCached()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(int?),
                isNullable: true, innerType: typeof(int));

            var d1 = td.InnerTypeDetail;
            var d2 = td.InnerTypeDetail;

            Assert.Same(d1, d2);
        }

        [Fact]
        public void IEnumerableGenericInnerTypeDetail_ReturnsDetail()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(List<int>),
                hasIEnumerableGeneric: true,
                iEnumerableGenericInnerType: typeof(int));

            var innerDetail = td.IEnumerableGenericInnerTypeDetail;

            Assert.NotNull(innerDetail);
            Assert.Equal(typeof(int), innerDetail!.Type);
        }

        [Fact]
        public void DictionaryInnerTypeDetail_ReturnsDetail()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(Dictionary<string, int>),
                hasDictionaryGeneric: true,
                dictionaryInnerType: typeof(int));

            var innerDetail = td.DictionaryInnerTypeDetail;

            Assert.NotNull(innerDetail);
            Assert.Equal(typeof(int), innerDetail!.Type);
        }

        [Fact]
        public void InnerTypeDetails_ReturnsDetails()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(Dictionary<string, int>),
                innerTypes: new[] { typeof(string), typeof(int) });

            var details = td.InnerTypeDetails;

            Assert.Equal(2, details.Count);
        }

        [Fact]
        public void SpecialType_Set()
        {
            var td = TestReflectionDetailFactory.MakeTypeDetail(typeof(System.Threading.CancellationToken),
                specialType: Zerra.Reflection.SpecialType.CancellationToken);

            Assert.Equal(Zerra.Reflection.SpecialType.CancellationToken, td.SpecialType);
        }

        [Fact]
        public void AllCollectionHasFlags_DefaultFalse()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.False(td.HasIEnumerable);
            Assert.False(td.HasIEnumerableGeneric);
            Assert.False(td.HasICollection);
            Assert.False(td.HasICollectionGeneric);
            Assert.False(td.HasIReadOnlyCollectionGeneric);
            Assert.False(td.HasIList);
            Assert.False(td.HasIListGeneric);
            Assert.False(td.HasIReadOnlyListGeneric);
            Assert.False(td.HasListGeneric);
            Assert.False(td.HasISetGeneric);
            Assert.False(td.HasIReadOnlySetGeneric);
            Assert.False(td.HasHashSetGeneric);
            Assert.False(td.HasIDictionary);
            Assert.False(td.HasIDictionaryGeneric);
            Assert.False(td.HasIReadOnlyDictionaryGeneric);
            Assert.False(td.HasDictionaryGeneric);
        }

        [Fact]
        public void AllCollectionIsFlags_DefaultFalse()
        {
            var td = MakeSimple(typeof(TestReflectionClass));

            Assert.False(td.IsIEnumerable);
            Assert.False(td.IsIEnumerableGeneric);
            Assert.False(td.IsICollection);
            Assert.False(td.IsICollectionGeneric);
            Assert.False(td.IsIReadOnlyCollectionGeneric);
            Assert.False(td.IsIList);
            Assert.False(td.IsIListGeneric);
            Assert.False(td.IsIReadOnlyListGeneric);
            Assert.False(td.IsListGeneric);
            Assert.False(td.IsISetGeneric);
            Assert.False(td.IsIReadOnlySetGeneric);
            Assert.False(td.IsHashSetGeneric);
            Assert.False(td.IsIDictionary);
            Assert.False(td.IsIDictionaryGeneric);
            Assert.False(td.IsIReadOnlyDictionaryGeneric);
            Assert.False(td.IsDictionaryGeneric);
        }
    }
}
