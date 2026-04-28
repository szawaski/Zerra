// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test.Reflection.Types
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class TestMarkerAttribute : Attribute
    {
        public string Label { get; }
        public TestMarkerAttribute(string label) { Label = label; }
    }

    internal interface ITestReflectionInterface
    {
        int InterfaceProperty { get; set; }
        string InterfaceMethod(string input);
    }

    [TestMarker("class")]
    internal class TestReflectionClass : ITestReflectionInterface
    {
        public static int StaticField = 99;

        [TestMarker("field")]
        public int PublicField;

        [TestMarker("property")]
        public string? StringProperty { get; set; }

        public int IntProperty { get; set; }

        public int? NullableIntProperty { get; set; }

        public int InterfaceProperty { get; set; }

        private readonly int _readonlyField;

        public TestReflectionClass()
        {
            _readonlyField = 0;
        }

        public TestReflectionClass(int value)
        {
            _readonlyField = value;
            IntProperty = value;
        }

        public TestReflectionClass(int value, string? text)
        {
            _readonlyField = value;
            IntProperty = value;
            StringProperty = text;
        }

        [TestMarker("method")]
        public int Add(int a, int b) => a + b;

        public string Greet(string name) => $"Hello, {name}!";

        public string InterfaceMethod(string input) => input.ToUpperInvariant();

        public static int StaticMethod(int x) => x * 2;

        public List<T> WrapInList<T>(T item) => new List<T> { item };

        public int GetReadonly() => _readonlyField;
    }

    internal class TestReflectionSubClass : TestReflectionClass
    {
        public double DoubleProperty { get; set; }

        public TestReflectionSubClass() : base() { }
        public TestReflectionSubClass(int value) : base(value) { }
    }

    internal class TestGenericClass<T>
    {
        public T? Value { get; set; }

        public TestGenericClass() { }
        public TestGenericClass(T value) { Value = value; }

        public T? GetValue() => Value;
    }

    internal enum TestReflectionEnum
    {
        None = 0,
        One = 1,
        Two = 2
    }

    /// <summary>
    /// Helper to construct detail objects from test classes without using TypeAnalyzer.
    /// </summary>
    internal static class TestReflectionDetailFactory
    {
        public static Zerra.Reflection.ParameterDetail MakeParameter(Type type, string name)
            => new(type, name);

        public static Zerra.Reflection.MemberDetail MakeMemberDetail(
            Type parentType,
            Type memberType,
            string name,
            bool isField,
            Delegate? getter,
            Func<object, object?>? getterBoxed,
            Delegate? setter,
            Action<object, object?>? setterBoxed,
            IReadOnlyList<Attribute>? attributes = null,
            bool isBacked = true,
            bool isStatic = false,
            bool isExplicitFromInterface = false)
            => new(parentType, memberType, name, isField, getter, getterBoxed, setter, setterBoxed,
                attributes ?? Array.Empty<Attribute>(), isBacked, isStatic, isExplicitFromInterface);

        public static Zerra.Reflection.MemberDetail<T> MakeMemberDetailT<T>(
            Type parentType,
            string name,
            bool isField,
            Func<object, T?>? getter,
            Func<object, object?>? getterBoxed,
            Action<object, T?>? setter,
            Action<object, object?>? setterBoxed,
            IReadOnlyList<Attribute>? attributes = null,
            bool isBacked = true,
            bool isStatic = false,
            bool isExplicitFromInterface = false)
            => new(parentType, name, isField, getter, getterBoxed, setter, setterBoxed,
                attributes ?? Array.Empty<Attribute>(), isBacked, isStatic, isExplicitFromInterface);

        public static Zerra.Reflection.ConstructorDetail MakeConstructorDetail(
            Type parentType,
            IReadOnlyList<Zerra.Reflection.ParameterDetail> parameters,
            Delegate creator,
            Func<object?[]?, object> creatorBoxed)
            => new(parentType, parameters, creator, creatorBoxed);

        public static Zerra.Reflection.ConstructorDetail<T> MakeConstructorDetailT<T>(
            IReadOnlyList<Zerra.Reflection.ParameterDetail> parameters,
            Func<object?[]?, T> creator,
            Func<object?[]?, object> creatorBoxed)
            => new(parameters, creator, creatorBoxed);

        public static Zerra.Reflection.MethodDetail MakeMethodDetail(
            Type parentType,
            string name,
            Type returnType,
            IReadOnlyList<Type>? genericArguments,
            IReadOnlyList<Zerra.Reflection.ParameterDetail> parameters,
            Delegate? caller,
            Func<object?, object?[]?, object?>? callerBoxed,
            IReadOnlyList<Attribute>? attributes = null,
            bool isStatic = false,
            bool isExplicitFromInterface = false)
            => new(parentType, name, returnType, genericArguments ?? Array.Empty<Type>(), parameters,
                caller, callerBoxed, attributes ?? Array.Empty<Attribute>(), isStatic, isExplicitFromInterface);

        public static Zerra.Reflection.MethodDetail<T> MakeMethodDetailT<T>(
            Type parentType,
            string name,
            IReadOnlyList<Type>? genericArguments,
            IReadOnlyList<Zerra.Reflection.ParameterDetail> parameters,
            Func<object?, object?[]?, T> caller,
            Func<object?, object?[]?, object?> callerBoxed,
            IReadOnlyList<Attribute>? attributes = null,
            bool isStatic = false,
            bool isExplicitFromInterface = false)
            => new(parentType, name, genericArguments ?? Array.Empty<Type>(), parameters,
                caller, callerBoxed, attributes ?? Array.Empty<Attribute>(), isStatic, isExplicitFromInterface);

        public static Zerra.Reflection.TypeDetail MakeTypeDetail(
            Type type,
            IReadOnlyList<Zerra.Reflection.MemberDetail>? members = null,
            IReadOnlyList<Zerra.Reflection.ConstructorDetail>? constructors = null,
            IReadOnlyList<Zerra.Reflection.MethodDetail>? methods = null,
            Delegate? creator = null,
            Func<object>? creatorBoxed = null,
            bool isNullable = false,
            Zerra.Reflection.CoreType? coreType = null,
            Zerra.Reflection.SpecialType? specialType = null,
            Zerra.Reflection.CoreEnumType? enumUnderlyingType = null,
            bool hasIEnumerable = false,
            bool hasIEnumerableGeneric = false,
            bool hasICollection = false,
            bool hasICollectionGeneric = false,
            bool hasIReadOnlyCollectionGeneric = false,
            bool hasIList = false,
            bool hasIListGeneric = false,
            bool hasIReadOnlyListGeneric = false,
            bool hasListGeneric = false,
            bool hasISetGeneric = false,
            bool hasIReadOnlySetGeneric = false,
            bool hasHashSetGeneric = false,
            bool hasIDictionary = false,
            bool hasIDictionaryGeneric = false,
            bool hasIReadOnlyDictionaryGeneric = false,
            bool hasDictionaryGeneric = false,
            bool isIEnumerable = false,
            bool isIEnumerableGeneric = false,
            bool isICollection = false,
            bool isICollectionGeneric = false,
            bool isIReadOnlyCollectionGeneric = false,
            bool isIList = false,
            bool isIListGeneric = false,
            bool isIReadOnlyListGeneric = false,
            bool isListGeneric = false,
            bool isISetGeneric = false,
            bool isIReadOnlySetGeneric = false,
            bool isHashSetGeneric = false,
            bool isIDictionary = false,
            bool isIDictionaryGeneric = false,
            bool isIReadOnlyDictionaryGeneric = false,
            bool isDictionaryGeneric = false,
            Type? innerType = null,
            Type? iEnumerableGenericInnerType = null,
            Type? dictionaryInnerType = null,
            IReadOnlyList<Type>? innerTypes = null,
            IReadOnlyList<Type>? baseTypes = null,
            IReadOnlyList<Type>? interfaces = null,
            IReadOnlyList<Attribute>? attributes = null)
            => new(type, members, constructors, methods, creator, creatorBoxed,
                isNullable, coreType, specialType, enumUnderlyingType,
                hasIEnumerable, hasIEnumerableGeneric, hasICollection, hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric, hasIList, hasIListGeneric, hasIReadOnlyListGeneric,
                hasListGeneric, hasISetGeneric, hasIReadOnlySetGeneric, hasHashSetGeneric,
                hasIDictionary, hasIDictionaryGeneric, hasIReadOnlyDictionaryGeneric, hasDictionaryGeneric,
                isIEnumerable, isIEnumerableGeneric, isICollection, isICollectionGeneric,
                isIReadOnlyCollectionGeneric, isIList, isIListGeneric, isIReadOnlyListGeneric,
                isListGeneric, isISetGeneric, isIReadOnlySetGeneric, isHashSetGeneric,
                isIDictionary, isIDictionaryGeneric, isIReadOnlyDictionaryGeneric, isDictionaryGeneric,
                innerType, iEnumerableGenericInnerType, dictionaryInnerType,
                innerTypes ?? Array.Empty<Type>(),
                baseTypes ?? Array.Empty<Type>(),
                interfaces ?? Array.Empty<Type>(),
                attributes);

        public static Zerra.Reflection.TypeDetail<T> MakeTypeDetailT<T>(
            IReadOnlyList<Zerra.Reflection.MemberDetail>? members = null,
            IReadOnlyList<Zerra.Reflection.ConstructorDetail<T>>? constructors = null,
            IReadOnlyList<Zerra.Reflection.MethodDetail>? methods = null,
            Func<T>? creator = null,
            Func<object>? creatorBoxed = null,
            bool isNullable = false,
            Zerra.Reflection.CoreType? coreType = null,
            Zerra.Reflection.SpecialType? specialType = null,
            Zerra.Reflection.CoreEnumType? enumUnderlyingType = null,
            bool hasIEnumerable = false,
            bool hasIEnumerableGeneric = false,
            bool hasICollection = false,
            bool hasICollectionGeneric = false,
            bool hasIReadOnlyCollectionGeneric = false,
            bool hasIList = false,
            bool hasIListGeneric = false,
            bool hasIReadOnlyListGeneric = false,
            bool hasListGeneric = false,
            bool hasISetGeneric = false,
            bool hasIReadOnlySetGeneric = false,
            bool hasHashSetGeneric = false,
            bool hasIDictionary = false,
            bool hasIDictionaryGeneric = false,
            bool hasIReadOnlyDictionaryGeneric = false,
            bool hasDictionaryGeneric = false,
            bool isIEnumerable = false,
            bool isIEnumerableGeneric = false,
            bool isICollection = false,
            bool isICollectionGeneric = false,
            bool isIReadOnlyCollectionGeneric = false,
            bool isIList = false,
            bool isIListGeneric = false,
            bool isIReadOnlyListGeneric = false,
            bool isListGeneric = false,
            bool isISetGeneric = false,
            bool isIReadOnlySetGeneric = false,
            bool isHashSetGeneric = false,
            bool isIDictionary = false,
            bool isIDictionaryGeneric = false,
            bool isIReadOnlyDictionaryGeneric = false,
            bool isDictionaryGeneric = false,
            Type? innerType = null,
            Type? iEnumerableGenericInnerType = null,
            Type? dictionaryInnerType = null,
            IReadOnlyList<Type>? innerTypes = null,
            IReadOnlyList<Type>? baseTypes = null,
            IReadOnlyList<Type>? interfaces = null,
            IReadOnlyList<Attribute>? attributes = null)
            => new(members, constructors, methods, creator, creatorBoxed,
                isNullable, coreType, specialType, enumUnderlyingType,
                hasIEnumerable, hasIEnumerableGeneric, hasICollection, hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric, hasIList, hasIListGeneric, hasIReadOnlyListGeneric,
                hasListGeneric, hasISetGeneric, hasIReadOnlySetGeneric, hasHashSetGeneric,
                hasIDictionary, hasIDictionaryGeneric, hasIReadOnlyDictionaryGeneric, hasDictionaryGeneric,
                isIEnumerable, isIEnumerableGeneric, isICollection, isICollectionGeneric,
                isIReadOnlyCollectionGeneric, isIList, isIListGeneric, isIReadOnlyListGeneric,
                isListGeneric, isISetGeneric, isIReadOnlySetGeneric, isHashSetGeneric,
                isIDictionary, isIDictionaryGeneric, isIReadOnlyDictionaryGeneric, isDictionaryGeneric,
                innerType, iEnumerableGenericInnerType, dictionaryInnerType,
                innerTypes ?? Array.Empty<Type>(),
                baseTypes ?? Array.Empty<Type>(),
                interfaces ?? Array.Empty<Type>(),
                attributes);
    }
}
