// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.Converters.Collections;
using Zerra.Serialization.Bytes.Converters.Collections.Collections;
using Zerra.Serialization.Bytes.Converters.Collections.Dictionaries;
using Zerra.Serialization.Bytes.Converters.Collections.Enumerables;
using Zerra.Serialization.Bytes.Converters.Collections.Lists;
using Zerra.Serialization.Bytes.Converters.Collections.Sets;
using Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays;
using Zerra.Serialization.Bytes.Converters.CoreTypes.HashSetTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.ICollectionTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.IListTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyCollectionTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyListTs;
#if NET5_0_OR_GREATER
using Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlySetTs;
#endif
using Zerra.Serialization.Bytes.Converters.CoreTypes.ISetTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs;
using Zerra.Serialization.Bytes.Converters.CoreTypes.Values;
using Zerra.Serialization.Bytes.Converters.General;
using Zerra.Serialization.Bytes.Converters.Special;

namespace Zerra.Serialization.Bytes.Converters
{
    /// <summary>
    /// Factory for creating and caching <see cref="ByteConverter"/> instances for various types.
    /// </summary>
    /// <remarks>
    /// This factory uses dynamic code generation to create converters for types that don't have explicit implementations.
    /// Converters are cached both by type and by member key to improve performance.
    /// </remarks>
    public static class ByteConverterFactory
    {
        private static readonly ConcurrentFactoryDictionary<Type, Func<ByteConverter>> creators = new();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, ByteConverter>> cache = new();
        private static ByteConverterTypeRequired? cacheByteConverterTypeInfo;

        /// <summary>
        /// Gets or creates the root <see cref="ByteConverter"/> for the specified type detail.
        /// </summary>
        /// <param name="typeDetail">The type detail describing the type to create a converter for.</param>
        /// <returns>A <see cref="ByteConverter"/> instance for the specified type.</returns>
        internal static ByteConverter GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        /// <summary>
        /// Gets or creates a <see cref="ByteConverter"/> for the specified type detail and member information.
        /// </summary>
        /// <param name="typeDetail">The type detail describing the type to create a converter for.</param>
        /// <param name="memberKey">A unique key identifying the member being converted.</param>
        /// <param name="getter">An optional delegate for getting the value from an object.</param>
        /// <param name="setter">An optional delegate for setting the value on an object.</param>
        /// <returns>A <see cref="ByteConverter"/> instance configured for the specified member.</returns>
        public static ByteConverter Get(TypeDetail typeDetail, string memberKey, Delegate? getter, Delegate? setter)
        {
            var cache2 = cache.GetOrAdd(typeDetail.Type, static () => new());
            var converter = cache2.GetOrAdd(memberKey, typeDetail, memberKey, getter, setter, static (typeDetail, memberKey, getter, setter) =>
            {
                var creator = creators.GetOrAdd(typeDetail.Type, typeDetail, GenerateByteConverterCreator);
                var newConverter = creator();
                newConverter.Setup(memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        /// <summary>
        /// Registers a custom converter creator for a specific type.
        /// </summary>
        /// <param name="type">The type for which to register the converter.</param>
        /// <param name="converter">A factory function that creates instances of the converter.</param>
        internal static void AddConverter(Type type, Func<ByteConverter> converter)
        {
            creators[type] = converter;
        }

        /// <summary>
        /// Gets or creates a drain converter that discards bytes without processing them.
        /// </summary>
        /// <returns>A <see cref="ByteConverter"/> instance for draining bytes.</returns>
        internal static ByteConverter GetDrainBytes()
        {
            if (cacheByteConverterTypeInfo is null)
            {
                lock (creators)
                {
                    if (cacheByteConverterTypeInfo is null)
                    {
                        var newConverter = new ByteConverterTypeRequired();
                        newConverter.Setup("Drain", null, null);
                        cacheByteConverterTypeInfo = newConverter;
                    }
                }
            }
            return cacheByteConverterTypeInfo;
        }

        internal static Func<ByteConverter> GenerateByteConverterCreator(TypeDetail typeDetail)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new InvalidOperationException($"ByteConverter type not found for {typeDetail.Type.Name} and dynamic code generation is not supported in this build configuration");

            var type = typeDetail.Type;
            var enumerableType = typeDetail.IEnumerableGenericInnerType ?? typeof(object);
            var dictionaryKeyType = typeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(0) ?? typeof(object);
            var dictionaryValueType = typeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(1) ?? typeof(object);

            var findCreatorMethodDefinition = typeof(ByteConverterFactory).GetMethod(nameof(FindCreator), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var findCreatorMethod = findCreatorMethodDefinition.MakeGenericMethod(type, enumerableType, dictionaryKeyType, dictionaryValueType);
            var creator = (Func<ByteConverter>)findCreatorMethod.Invoke(null, [typeDetail])!;
            return creator;
        }

        internal static void RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>()
            where TDictionaryKey : notnull
        {
            var type = typeof(TType);
            var creator = FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(type.GetTypeDetail());
            _ = creators.TryAdd(type, creator);
        }

        private static Func<ByteConverter> FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(TypeDetail typeDetail)
            where TDictionaryKey : notnull
        {
            if (typeDetail.CoreType.HasValue)
            {
                switch (typeDetail.CoreType.Value)
                {
                    case CoreType.Boolean: return static () => new ByteConverterBoolean();
                    case CoreType.Byte: return static () => new ByteConverterByte();
                    case CoreType.SByte: return static () => new ByteConverterSByte();
                    case CoreType.Int16: return static () => new ByteConverterInt16();
                    case CoreType.UInt16: return static () => new ByteConverterUInt16();
                    case CoreType.Int32: return static () => new ByteConverterInt32();
                    case CoreType.UInt32: return static () => new ByteConverterUInt32();
                    case CoreType.Int64: return static () => new ByteConverterInt64();
                    case CoreType.UInt64: return static () => new ByteConverterUInt64();
                    case CoreType.Single: return static () => new ByteConverterSingle();
                    case CoreType.Double: return static () => new ByteConverterDouble();
                    case CoreType.Decimal: return static () => new ByteConverterDecimal();
                    case CoreType.Char: return static () => new ByteConverterChar();
                    case CoreType.DateTime: return static () => new ByteConverterDateTime();
                    case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffset();
                    case CoreType.TimeSpan: return static () => new ByteConverterTimeSpan();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnly: return static () => new ByteConverterDateOnly();
                    case CoreType.TimeOnly: return static () => new ByteConverterTimeOnly();
#endif
                    case CoreType.Guid: return static () => new ByteConverterGuid();
                    case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullable();
                    case CoreType.ByteNullable: return static () => new ByteConverterByteNullable();
                    case CoreType.SByteNullable: return static () => new ByteConverterSByteNullable();
                    case CoreType.Int16Nullable: return static () => new ByteConverterInt16Nullable();
                    case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16Nullable();
                    case CoreType.Int32Nullable: return static () => new ByteConverterInt32Nullable();
                    case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32Nullable();
                    case CoreType.Int64Nullable: return static () => new ByteConverterInt64Nullable();
                    case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64Nullable();
                    case CoreType.SingleNullable: return static () => new ByteConverterSingleNullable();
                    case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullable();
                    case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullable();
                    case CoreType.CharNullable: return static () => new ByteConverterCharNullable();
                    case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullable();
                    case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullable();
                    case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullable();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullable();
                    case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullable();
#endif
                    case CoreType.GuidNullable: return static () => new ByteConverterGuidNullable();
                    case CoreType.String: return static () => new ByteConverterString();
                }
            }

            if (typeDetail.HasIEnumerableGeneric && typeDetail.IEnumerableGenericInnerTypeDetail!.CoreType.HasValue)
            {
                if (typeDetail.Type.IsArray)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanArray();
                        case CoreType.Byte: return static () => new ByteConverterByteArray();
                        case CoreType.SByte: return static () => new ByteConverterSByteArray();
                        case CoreType.Int16: return static () => new ByteConverterInt16Array();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16Array();
                        case CoreType.Int32: return static () => new ByteConverterInt32Array();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32Array();
                        case CoreType.Int64: return static () => new ByteConverterInt64Array();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64Array();
                        case CoreType.Single: return static () => new ByteConverterSingleArray();
                        case CoreType.Double: return static () => new ByteConverterDoubleArray();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalArray();
                        case CoreType.Char: return static () => new ByteConverterCharArray();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeArray();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetArray();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanArray();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyArray();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyArray();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidArray();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableArray();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableArray();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableArray();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableArray();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableArray();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableArray();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableArray();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableArray();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableArray();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableArray();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableArray();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableArray();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableArray();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableArray();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableArray();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableArray();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableArray();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableArray();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableArray();
                            //case CoreType.String: return static () => new ByteConverterStringArray();
                    }
                }

                if (typeDetail.IsListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanList();
                        case CoreType.Byte: return static () => new ByteConverterByteList();
                        case CoreType.SByte: return static () => new ByteConverterSByteList();
                        case CoreType.Int16: return static () => new ByteConverterInt16List();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16List();
                        case CoreType.Int32: return static () => new ByteConverterInt32List();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32List();
                        case CoreType.Int64: return static () => new ByteConverterInt64List();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64List();
                        case CoreType.Single: return static () => new ByteConverterSingleList();
                        case CoreType.Double: return static () => new ByteConverterDoubleList();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalList();
                        case CoreType.Char: return static () => new ByteConverterCharList();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeList();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetList();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyList();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyList();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidList();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableList();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableList();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableList();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableList();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableList();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableList();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableList();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableList();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableList();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableList();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableList();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableList();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableList();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableList();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableList();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableList();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableList();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableList();
                            //case CoreType.String: return static () => new ByteConverterStringList();
                    }
                }

                if (typeDetail.IsIListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanIList();
                        case CoreType.Byte: return static () => new ByteConverterByteIList();
                        case CoreType.SByte: return static () => new ByteConverterSByteIList();
                        case CoreType.Int16: return static () => new ByteConverterInt16IList();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16IList();
                        case CoreType.Int32: return static () => new ByteConverterInt32IList();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32IList();
                        case CoreType.Int64: return static () => new ByteConverterInt64IList();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64IList();
                        case CoreType.Single: return static () => new ByteConverterSingleIList();
                        case CoreType.Double: return static () => new ByteConverterDoubleIList();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalIList();
                        case CoreType.Char: return static () => new ByteConverterCharIList();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeIList();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetIList();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanIList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyIList();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyIList();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidIList();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableIList();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableIList();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableIList();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableIList();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableIList();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableIList();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableIList();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableIList();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableIList();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableIList();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableIList();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableIList();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableIList();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableIList();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableIList();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableIList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableIList();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableIList();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableIList();
                            //case CoreType.String: return static () => new ByteConverterStringIList();
                    }
                }

                if (typeDetail.IsIReadOnlyListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanIReadOnlyList();
                        case CoreType.Byte: return static () => new ByteConverterByteIReadOnlyList();
                        case CoreType.SByte: return static () => new ByteConverterSByteIReadOnlyList();
                        case CoreType.Int16: return static () => new ByteConverterInt16IReadOnlyList();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16IReadOnlyList();
                        case CoreType.Int32: return static () => new ByteConverterInt32IReadOnlyList();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32IReadOnlyList();
                        case CoreType.Int64: return static () => new ByteConverterInt64IReadOnlyList();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64IReadOnlyList();
                        case CoreType.Single: return static () => new ByteConverterSingleIReadOnlyList();
                        case CoreType.Double: return static () => new ByteConverterDoubleIReadOnlyList();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalIReadOnlyList();
                        case CoreType.Char: return static () => new ByteConverterCharIReadOnlyList();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeIReadOnlyList();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetIReadOnlyList();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanIReadOnlyList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyIReadOnlyList();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyIReadOnlyList();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidIReadOnlyList();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableIReadOnlyList();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableIReadOnlyList();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableIReadOnlyList();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableIReadOnlyList();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableIReadOnlyList();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableIReadOnlyList();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableIReadOnlyList();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableIReadOnlyList();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableIReadOnlyList();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableIReadOnlyList();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableIReadOnlyList();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableIReadOnlyList();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableIReadOnlyList();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableIReadOnlyList();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableIReadOnlyList();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableIReadOnlyList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableIReadOnlyList();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableIReadOnlyList();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableIReadOnlyList();
                            //case CoreType.String: return static () => new ByteConverterStringIReadOnlyList();
                    }
                }

                if (typeDetail.IsICollectionGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanICollection();
                        case CoreType.Byte: return static () => new ByteConverterByteICollection();
                        case CoreType.SByte: return static () => new ByteConverterSByteICollection();
                        case CoreType.Int16: return static () => new ByteConverterInt16ICollection();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16ICollection();
                        case CoreType.Int32: return static () => new ByteConverterInt32ICollection();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32ICollection();
                        case CoreType.Int64: return static () => new ByteConverterInt64ICollection();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64ICollection();
                        case CoreType.Single: return static () => new ByteConverterSingleICollection();
                        case CoreType.Double: return static () => new ByteConverterDoubleICollection();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalICollection();
                        case CoreType.Char: return static () => new ByteConverterCharICollection();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeICollection();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetICollection();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanICollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyICollection();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyICollection();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidICollection();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableICollection();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableICollection();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableICollection();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableICollection();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableICollection();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableICollection();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableICollection();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableICollection();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableICollection();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableICollection();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableICollection();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableICollection();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableICollection();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableICollection();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableICollection();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableICollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableICollection();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableICollection();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableICollection();
                            //case CoreType.String: return static () => new ByteConverterStringICollection();
                    }
                }

                if (typeDetail.IsIReadOnlyCollectionGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanIReadOnlyCollection();
                        case CoreType.Byte: return static () => new ByteConverterByteIReadOnlyCollection();
                        case CoreType.SByte: return static () => new ByteConverterSByteIReadOnlyCollection();
                        case CoreType.Int16: return static () => new ByteConverterInt16IReadOnlyCollection();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16IReadOnlyCollection();
                        case CoreType.Int32: return static () => new ByteConverterInt32IReadOnlyCollection();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32IReadOnlyCollection();
                        case CoreType.Int64: return static () => new ByteConverterInt64IReadOnlyCollection();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64IReadOnlyCollection();
                        case CoreType.Single: return static () => new ByteConverterSingleIReadOnlyCollection();
                        case CoreType.Double: return static () => new ByteConverterDoubleIReadOnlyCollection();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalIReadOnlyCollection();
                        case CoreType.Char: return static () => new ByteConverterCharIReadOnlyCollection();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeIReadOnlyCollection();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetIReadOnlyCollection();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanIReadOnlyCollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyIReadOnlyCollection();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyIReadOnlyCollection();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidIReadOnlyCollection();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableIReadOnlyCollection();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableIReadOnlyCollection();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableIReadOnlyCollection();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableIReadOnlyCollection();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableIReadOnlyCollection();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableIReadOnlyCollection();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableIReadOnlyCollection();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableIReadOnlyCollection();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableIReadOnlyCollection();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableIReadOnlyCollection();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableIReadOnlyCollection();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableIReadOnlyCollection();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableIReadOnlyCollection();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableIReadOnlyCollection();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableIReadOnlyCollection();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableIReadOnlyCollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableIReadOnlyCollection();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableIReadOnlyCollection();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableIReadOnlyCollection();
                            //case CoreType.String: return static () => new ByteConverterStringIReadOnlyCollection();
                    }
                }

                if (typeDetail.IsHashSetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanHashSet();
                        case CoreType.Byte: return static () => new ByteConverterByteHashSet();
                        case CoreType.SByte: return static () => new ByteConverterSByteHashSet();
                        case CoreType.Int16: return static () => new ByteConverterInt16HashSet();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16HashSet();
                        case CoreType.Int32: return static () => new ByteConverterInt32HashSet();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32HashSet();
                        case CoreType.Int64: return static () => new ByteConverterInt64HashSet();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64HashSet();
                        case CoreType.Single: return static () => new ByteConverterSingleHashSet();
                        case CoreType.Double: return static () => new ByteConverterDoubleHashSet();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalHashSet();
                        case CoreType.Char: return static () => new ByteConverterCharHashSet();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeHashSet();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetHashSet();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanHashSet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyHashSet();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyHashSet();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidHashSet();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableHashSet();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableHashSet();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableHashSet();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableHashSet();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableHashSet();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableHashSet();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableHashSet();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableHashSet();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableHashSet();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableHashSet();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableHashSet();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableHashSet();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableHashSet();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableHashSet();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableHashSet();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableHashSet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableHashSet();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableHashSet();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableHashSet();
                            //case CoreType.String: return static () => new ByteConverterStringHashSet();
                    }
                }

                if (typeDetail.IsISetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanISet();
                        case CoreType.Byte: return static () => new ByteConverterByteISet();
                        case CoreType.SByte: return static () => new ByteConverterSByteISet();
                        case CoreType.Int16: return static () => new ByteConverterInt16ISet();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16ISet();
                        case CoreType.Int32: return static () => new ByteConverterInt32ISet();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32ISet();
                        case CoreType.Int64: return static () => new ByteConverterInt64ISet();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64ISet();
                        case CoreType.Single: return static () => new ByteConverterSingleISet();
                        case CoreType.Double: return static () => new ByteConverterDoubleISet();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalISet();
                        case CoreType.Char: return static () => new ByteConverterCharISet();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeISet();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetISet();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanISet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyISet();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyISet();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidISet();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableISet();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableISet();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableISet();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableISet();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableISet();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableISet();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableISet();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableISet();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableISet();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableISet();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableISet();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableISet();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableISet();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableISet();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableISet();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableISet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableISet();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableISet();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableISet();
                            //case CoreType.String: return static () => new ByteConverterStringISet();
                    }
                }

                if (typeDetail.IsIReadOnlySetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return static () => new ByteConverterBooleanIReadOnlySet();
                        case CoreType.Byte: return static () => new ByteConverterByteIReadOnlySet();
                        case CoreType.SByte: return static () => new ByteConverterSByteIReadOnlySet();
                        case CoreType.Int16: return static () => new ByteConverterInt16IReadOnlySet();
                        case CoreType.UInt16: return static () => new ByteConverterUInt16IReadOnlySet();
                        case CoreType.Int32: return static () => new ByteConverterInt32IReadOnlySet();
                        case CoreType.UInt32: return static () => new ByteConverterUInt32IReadOnlySet();
                        case CoreType.Int64: return static () => new ByteConverterInt64IReadOnlySet();
                        case CoreType.UInt64: return static () => new ByteConverterUInt64IReadOnlySet();
                        case CoreType.Single: return static () => new ByteConverterSingleIReadOnlySet();
                        case CoreType.Double: return static () => new ByteConverterDoubleIReadOnlySet();
                        case CoreType.Decimal: return static () => new ByteConverterDecimalIReadOnlySet();
                        case CoreType.Char: return static () => new ByteConverterCharIReadOnlySet();
                        case CoreType.DateTime: return static () => new ByteConverterDateTimeIReadOnlySet();
                        case CoreType.DateTimeOffset: return static () => new ByteConverterDateTimeOffsetIReadOnlySet();
                        case CoreType.TimeSpan: return static () => new ByteConverterTimeSpanIReadOnlySet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return static () => new ByteConverterDateOnlyIReadOnlySet();
                        case CoreType.TimeOnly: return static () => new ByteConverterTimeOnlyIReadOnlySet();
#endif
                        case CoreType.Guid: return static () => new ByteConverterGuidIReadOnlySet();
                        case CoreType.BooleanNullable: return static () => new ByteConverterBooleanNullableIReadOnlySet();
                        case CoreType.ByteNullable: return static () => new ByteConverterByteNullableIReadOnlySet();
                        case CoreType.SByteNullable: return static () => new ByteConverterSByteNullableIReadOnlySet();
                        case CoreType.Int16Nullable: return static () => new ByteConverterInt16NullableIReadOnlySet();
                        case CoreType.UInt16Nullable: return static () => new ByteConverterUInt16NullableIReadOnlySet();
                        case CoreType.Int32Nullable: return static () => new ByteConverterInt32NullableIReadOnlySet();
                        case CoreType.UInt32Nullable: return static () => new ByteConverterUInt32NullableIReadOnlySet();
                        case CoreType.Int64Nullable: return static () => new ByteConverterInt64NullableIReadOnlySet();
                        case CoreType.UInt64Nullable: return static () => new ByteConverterUInt64NullableIReadOnlySet();
                        case CoreType.SingleNullable: return static () => new ByteConverterSingleNullableIReadOnlySet();
                        case CoreType.DoubleNullable: return static () => new ByteConverterDoubleNullableIReadOnlySet();
                        case CoreType.DecimalNullable: return static () => new ByteConverterDecimalNullableIReadOnlySet();
                        case CoreType.CharNullable: return static () => new ByteConverterCharNullableIReadOnlySet();
                        case CoreType.DateTimeNullable: return static () => new ByteConverterDateTimeNullableIReadOnlySet();
                        case CoreType.DateTimeOffsetNullable: return static () => new ByteConverterDateTimeOffsetNullableIReadOnlySet();
                        case CoreType.TimeSpanNullable: return static () => new ByteConverterTimeSpanNullableIReadOnlySet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return static () => new ByteConverterDateOnlyNullableIReadOnlySet();
                        case CoreType.TimeOnlyNullable: return static () => new ByteConverterTimeOnlyNullableIReadOnlySet();
#endif
                        case CoreType.GuidNullable: return static () => new ByteConverterGuidNullableIReadOnlySet();
                            //case CoreType.String: return static () => new ByteConverterStringIReadOnlySet();
                    }
                }
            }

            if (typeDetail.IsIList)
                return static () => new ByteConverterIList();
            if (typeDetail.IsIListGeneric)
                return static () => new ByteConverterIListT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyListGeneric)
                return static () => new ByteConverterIReadOnlyListT<TEnumerableType>();
            if (typeDetail.IsListGeneric)
                return static () => new ByteConverterListT<TEnumerableType>();

            if (typeDetail.IsISetGeneric)
                return static () => new ByteConverterISetT<TEnumerableType>();
            if (typeDetail.IsIReadOnlySetGeneric)
                return static () => new ByteConverterIReadOnlySetT<TEnumerableType>();
            if (typeDetail.IsHashSetGeneric)
                return static () => new ByteConverterHashSetT<TEnumerableType>();

            if (typeDetail.IsICollection)
                return static () => new ByteConverterICollection();
            if (typeDetail.IsICollectionGeneric)
                return static () => new ByteConverterICollectionT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyCollectionGeneric)
                return static () => new ByteConverterIReadOnlyCollectionT<TEnumerableType>();

            if (typeDetail.IsIDictionary)
                return static () => new ByteConverterIDictionary();
            if (typeDetail.IsIDictionaryGeneric)
                return static () => new ByteConverterIDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsIReadOnlyDictionaryGeneric)
                return static () => new ByteConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsDictionaryGeneric)
                return static () => new ByteConverterDictionaryT<TDictionaryKey, TDictionaryValue>();

            if (typeDetail.IsIEnumerable)
                return static () => new ByteConverterIEnumerable();
            if (typeDetail.IsIEnumerableGeneric)
                return static () => new ByteConverterIEnumerableT<TEnumerableType>();

            if (typeDetail.Type.FullName == "System.Type")
                return static () => new ByteConverterType();
            if (typeDetail.Type.FullName == "System.Threading.CancellationToken")
                return static () => new ByteConverterCancellationToken();
            if (typeDetail.IsNullable && typeDetail.InnerType!.FullName == "System.Threading.CancellationToken")
                return static () => new ByteConverterCancellationTokenNullable();

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No ByteConverter found to support {typeDetail.Type.Name}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType!.IsEnum)
                return static () => new ByteConverterEnum<TType>();

            //Array
            if (typeDetail.Type.IsArray)
                return static () => new ByteConverterArrayT<TEnumerableType>();

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return static () => new ByteConverterIListTOfT<TType, TEnumerableType>();

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return static () => new ByteConverterIListOfT<TType>();

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return static () => new ByteConverterISetTOfT<TType, TEnumerableType>();

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return static () => new ByteConverterIDictionaryTOfT<TType, TDictionaryKey, TDictionaryValue>();

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return static () => new ByteConverterIDictionaryOfT<TType>();

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return static () => new ByteConverterICollectionTOfT<TType, TEnumerableType>();

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return static () => new ByteConverterIEnumerableTOfT<TType, TEnumerableType>();

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return static () => new ByteConverterIEnumerableOfT<TType>();

            //Object
            return static () => new ByteConverterObject<TType>();
        }
    }
}