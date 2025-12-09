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
    public static class ByteConverterFactory
    {
        private static readonly ConcurrentFactoryDictionary<Type, Func<ByteConverter>> creators = new();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, ByteConverter>> cache = new();
        private static ByteConverterTypeRequired? cacheByteConverterTypeInfo;

        internal static ByteConverter GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

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

        internal static void AddConverter(Type converterType, Func<ByteConverter> converter)
        {
            creators[converterType] = converter;
        }

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
                    case CoreType.Boolean: return () => new ByteConverterBoolean();
                    case CoreType.Byte: return () => new ByteConverterByte();
                    case CoreType.SByte: return () => new ByteConverterSByte();
                    case CoreType.Int16: return () => new ByteConverterInt16();
                    case CoreType.UInt16: return () => new ByteConverterUInt16();
                    case CoreType.Int32: return () => new ByteConverterInt32();
                    case CoreType.UInt32: return () => new ByteConverterUInt32();
                    case CoreType.Int64: return () => new ByteConverterInt64();
                    case CoreType.UInt64: return () => new ByteConverterUInt64();
                    case CoreType.Single: return () => new ByteConverterSingle();
                    case CoreType.Double: return () => new ByteConverterDouble();
                    case CoreType.Decimal: return () => new ByteConverterDecimal();
                    case CoreType.Char: return () => new ByteConverterChar();
                    case CoreType.DateTime: return () => new ByteConverterDateTime();
                    case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffset();
                    case CoreType.TimeSpan: return () => new ByteConverterTimeSpan();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnly: return () => new ByteConverterDateOnly();
                    case CoreType.TimeOnly: return () => new ByteConverterTimeOnly();
#endif
                    case CoreType.Guid: return () => new ByteConverterGuid();
                    case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullable();
                    case CoreType.ByteNullable: return () => new ByteConverterByteNullable();
                    case CoreType.SByteNullable: return () => new ByteConverterSByteNullable();
                    case CoreType.Int16Nullable: return () => new ByteConverterInt16Nullable();
                    case CoreType.UInt16Nullable: return () => new ByteConverterUInt16Nullable();
                    case CoreType.Int32Nullable: return () => new ByteConverterInt32Nullable();
                    case CoreType.UInt32Nullable: return () => new ByteConverterUInt32Nullable();
                    case CoreType.Int64Nullable: return () => new ByteConverterInt64Nullable();
                    case CoreType.UInt64Nullable: return () => new ByteConverterUInt64Nullable();
                    case CoreType.SingleNullable: return () => new ByteConverterSingleNullable();
                    case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullable();
                    case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullable();
                    case CoreType.CharNullable: return () => new ByteConverterCharNullable();
                    case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullable();
                    case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullable();
                    case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullable();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullable();
                    case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullable();
#endif
                    case CoreType.GuidNullable: return () => new ByteConverterGuidNullable();
                    case CoreType.String: return () => new ByteConverterString();
                }
            }

            if (typeDetail.HasIEnumerableGeneric && typeDetail.IEnumerableGenericInnerTypeDetail!.CoreType.HasValue)
            {
                if (typeDetail.Type.IsArray)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanArray();
                        case CoreType.Byte: return () => new ByteConverterByteArray();
                        case CoreType.SByte: return () => new ByteConverterSByteArray();
                        case CoreType.Int16: return () => new ByteConverterInt16Array();
                        case CoreType.UInt16: return () => new ByteConverterUInt16Array();
                        case CoreType.Int32: return () => new ByteConverterInt32Array();
                        case CoreType.UInt32: return () => new ByteConverterUInt32Array();
                        case CoreType.Int64: return () => new ByteConverterInt64Array();
                        case CoreType.UInt64: return () => new ByteConverterUInt64Array();
                        case CoreType.Single: return () => new ByteConverterSingleArray();
                        case CoreType.Double: return () => new ByteConverterDoubleArray();
                        case CoreType.Decimal: return () => new ByteConverterDecimalArray();
                        case CoreType.Char: return () => new ByteConverterCharArray();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeArray();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetArray();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanArray();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyArray();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyArray();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidArray();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableArray();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableArray();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableArray();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableArray();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableArray();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableArray();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableArray();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableArray();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableArray();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableArray();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableArray();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableArray();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableArray();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableArray();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableArray();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableArray();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableArray();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableArray();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableArray();
                            //case CoreType.String: return () => new ByteConverterStringArray();
                    }
                }

                if (typeDetail.IsListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanList();
                        case CoreType.Byte: return () => new ByteConverterByteList();
                        case CoreType.SByte: return () => new ByteConverterSByteList();
                        case CoreType.Int16: return () => new ByteConverterInt16List();
                        case CoreType.UInt16: return () => new ByteConverterUInt16List();
                        case CoreType.Int32: return () => new ByteConverterInt32List();
                        case CoreType.UInt32: return () => new ByteConverterUInt32List();
                        case CoreType.Int64: return () => new ByteConverterInt64List();
                        case CoreType.UInt64: return () => new ByteConverterUInt64List();
                        case CoreType.Single: return () => new ByteConverterSingleList();
                        case CoreType.Double: return () => new ByteConverterDoubleList();
                        case CoreType.Decimal: return () => new ByteConverterDecimalList();
                        case CoreType.Char: return () => new ByteConverterCharList();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeList();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetList();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyList();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyList();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidList();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableList();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableList();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableList();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableList();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableList();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableList();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableList();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableList();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableList();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableList();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableList();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableList();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableList();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableList();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableList();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableList();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableList();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableList();
                            //case CoreType.String: return () => new ByteConverterStringList();
                    }
                }

                if (typeDetail.IsIListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanIList();
                        case CoreType.Byte: return () => new ByteConverterByteIList();
                        case CoreType.SByte: return () => new ByteConverterSByteIList();
                        case CoreType.Int16: return () => new ByteConverterInt16IList();
                        case CoreType.UInt16: return () => new ByteConverterUInt16IList();
                        case CoreType.Int32: return () => new ByteConverterInt32IList();
                        case CoreType.UInt32: return () => new ByteConverterUInt32IList();
                        case CoreType.Int64: return () => new ByteConverterInt64IList();
                        case CoreType.UInt64: return () => new ByteConverterUInt64IList();
                        case CoreType.Single: return () => new ByteConverterSingleIList();
                        case CoreType.Double: return () => new ByteConverterDoubleIList();
                        case CoreType.Decimal: return () => new ByteConverterDecimalIList();
                        case CoreType.Char: return () => new ByteConverterCharIList();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeIList();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetIList();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanIList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyIList();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyIList();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidIList();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableIList();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableIList();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableIList();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableIList();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableIList();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableIList();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableIList();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableIList();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableIList();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableIList();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableIList();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableIList();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableIList();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableIList();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableIList();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableIList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableIList();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableIList();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableIList();
                            //case CoreType.String: return () => new ByteConverterStringIList();
                    }
                }

                if (typeDetail.IsIReadOnlyListGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanIReadOnlyList();
                        case CoreType.Byte: return () => new ByteConverterByteIReadOnlyList();
                        case CoreType.SByte: return () => new ByteConverterSByteIReadOnlyList();
                        case CoreType.Int16: return () => new ByteConverterInt16IReadOnlyList();
                        case CoreType.UInt16: return () => new ByteConverterUInt16IReadOnlyList();
                        case CoreType.Int32: return () => new ByteConverterInt32IReadOnlyList();
                        case CoreType.UInt32: return () => new ByteConverterUInt32IReadOnlyList();
                        case CoreType.Int64: return () => new ByteConverterInt64IReadOnlyList();
                        case CoreType.UInt64: return () => new ByteConverterUInt64IReadOnlyList();
                        case CoreType.Single: return () => new ByteConverterSingleIReadOnlyList();
                        case CoreType.Double: return () => new ByteConverterDoubleIReadOnlyList();
                        case CoreType.Decimal: return () => new ByteConverterDecimalIReadOnlyList();
                        case CoreType.Char: return () => new ByteConverterCharIReadOnlyList();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeIReadOnlyList();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetIReadOnlyList();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanIReadOnlyList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyIReadOnlyList();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyIReadOnlyList();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidIReadOnlyList();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableIReadOnlyList();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableIReadOnlyList();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableIReadOnlyList();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableIReadOnlyList();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableIReadOnlyList();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableIReadOnlyList();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableIReadOnlyList();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableIReadOnlyList();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableIReadOnlyList();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableIReadOnlyList();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableIReadOnlyList();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableIReadOnlyList();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableIReadOnlyList();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableIReadOnlyList();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableIReadOnlyList();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableIReadOnlyList();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableIReadOnlyList();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableIReadOnlyList();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableIReadOnlyList();
                            //case CoreType.String: return () => new ByteConverterStringIReadOnlyList();
                    }
                }

                if (typeDetail.IsICollectionGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanICollection();
                        case CoreType.Byte: return () => new ByteConverterByteICollection();
                        case CoreType.SByte: return () => new ByteConverterSByteICollection();
                        case CoreType.Int16: return () => new ByteConverterInt16ICollection();
                        case CoreType.UInt16: return () => new ByteConverterUInt16ICollection();
                        case CoreType.Int32: return () => new ByteConverterInt32ICollection();
                        case CoreType.UInt32: return () => new ByteConverterUInt32ICollection();
                        case CoreType.Int64: return () => new ByteConverterInt64ICollection();
                        case CoreType.UInt64: return () => new ByteConverterUInt64ICollection();
                        case CoreType.Single: return () => new ByteConverterSingleICollection();
                        case CoreType.Double: return () => new ByteConverterDoubleICollection();
                        case CoreType.Decimal: return () => new ByteConverterDecimalICollection();
                        case CoreType.Char: return () => new ByteConverterCharICollection();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeICollection();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetICollection();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanICollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyICollection();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyICollection();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidICollection();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableICollection();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableICollection();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableICollection();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableICollection();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableICollection();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableICollection();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableICollection();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableICollection();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableICollection();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableICollection();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableICollection();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableICollection();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableICollection();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableICollection();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableICollection();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableICollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableICollection();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableICollection();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableICollection();
                            //case CoreType.String: return () => new ByteConverterStringICollection();
                    }
                }

                if (typeDetail.IsIReadOnlyCollectionGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanIReadOnlyCollection();
                        case CoreType.Byte: return () => new ByteConverterByteIReadOnlyCollection();
                        case CoreType.SByte: return () => new ByteConverterSByteIReadOnlyCollection();
                        case CoreType.Int16: return () => new ByteConverterInt16IReadOnlyCollection();
                        case CoreType.UInt16: return () => new ByteConverterUInt16IReadOnlyCollection();
                        case CoreType.Int32: return () => new ByteConverterInt32IReadOnlyCollection();
                        case CoreType.UInt32: return () => new ByteConverterUInt32IReadOnlyCollection();
                        case CoreType.Int64: return () => new ByteConverterInt64IReadOnlyCollection();
                        case CoreType.UInt64: return () => new ByteConverterUInt64IReadOnlyCollection();
                        case CoreType.Single: return () => new ByteConverterSingleIReadOnlyCollection();
                        case CoreType.Double: return () => new ByteConverterDoubleIReadOnlyCollection();
                        case CoreType.Decimal: return () => new ByteConverterDecimalIReadOnlyCollection();
                        case CoreType.Char: return () => new ByteConverterCharIReadOnlyCollection();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeIReadOnlyCollection();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetIReadOnlyCollection();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanIReadOnlyCollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyIReadOnlyCollection();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyIReadOnlyCollection();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidIReadOnlyCollection();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableIReadOnlyCollection();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableIReadOnlyCollection();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableIReadOnlyCollection();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableIReadOnlyCollection();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableIReadOnlyCollection();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableIReadOnlyCollection();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableIReadOnlyCollection();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableIReadOnlyCollection();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableIReadOnlyCollection();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableIReadOnlyCollection();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableIReadOnlyCollection();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableIReadOnlyCollection();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableIReadOnlyCollection();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableIReadOnlyCollection();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableIReadOnlyCollection();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableIReadOnlyCollection();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableIReadOnlyCollection();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableIReadOnlyCollection();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableIReadOnlyCollection();
                            //case CoreType.String: return () => new ByteConverterStringIReadOnlyCollection();
                    }
                }

                if (typeDetail.IsHashSetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanHashSet();
                        case CoreType.Byte: return () => new ByteConverterByteHashSet();
                        case CoreType.SByte: return () => new ByteConverterSByteHashSet();
                        case CoreType.Int16: return () => new ByteConverterInt16HashSet();
                        case CoreType.UInt16: return () => new ByteConverterUInt16HashSet();
                        case CoreType.Int32: return () => new ByteConverterInt32HashSet();
                        case CoreType.UInt32: return () => new ByteConverterUInt32HashSet();
                        case CoreType.Int64: return () => new ByteConverterInt64HashSet();
                        case CoreType.UInt64: return () => new ByteConverterUInt64HashSet();
                        case CoreType.Single: return () => new ByteConverterSingleHashSet();
                        case CoreType.Double: return () => new ByteConverterDoubleHashSet();
                        case CoreType.Decimal: return () => new ByteConverterDecimalHashSet();
                        case CoreType.Char: return () => new ByteConverterCharHashSet();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeHashSet();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetHashSet();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanHashSet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyHashSet();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyHashSet();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidHashSet();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableHashSet();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableHashSet();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableHashSet();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableHashSet();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableHashSet();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableHashSet();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableHashSet();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableHashSet();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableHashSet();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableHashSet();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableHashSet();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableHashSet();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableHashSet();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableHashSet();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableHashSet();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableHashSet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableHashSet();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableHashSet();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableHashSet();
                            //case CoreType.String: return () => new ByteConverterStringHashSet();
                    }
                }

                if (typeDetail.IsISetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanISet();
                        case CoreType.Byte: return () => new ByteConverterByteISet();
                        case CoreType.SByte: return () => new ByteConverterSByteISet();
                        case CoreType.Int16: return () => new ByteConverterInt16ISet();
                        case CoreType.UInt16: return () => new ByteConverterUInt16ISet();
                        case CoreType.Int32: return () => new ByteConverterInt32ISet();
                        case CoreType.UInt32: return () => new ByteConverterUInt32ISet();
                        case CoreType.Int64: return () => new ByteConverterInt64ISet();
                        case CoreType.UInt64: return () => new ByteConverterUInt64ISet();
                        case CoreType.Single: return () => new ByteConverterSingleISet();
                        case CoreType.Double: return () => new ByteConverterDoubleISet();
                        case CoreType.Decimal: return () => new ByteConverterDecimalISet();
                        case CoreType.Char: return () => new ByteConverterCharISet();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeISet();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetISet();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanISet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyISet();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyISet();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidISet();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableISet();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableISet();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableISet();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableISet();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableISet();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableISet();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableISet();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableISet();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableISet();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableISet();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableISet();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableISet();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableISet();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableISet();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableISet();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableISet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableISet();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableISet();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableISet();
                            //case CoreType.String: return () => new ByteConverterStringISet();
                    }
                }

                if (typeDetail.IsIReadOnlySetGeneric)
                {
                    switch (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value)
                    {
                        case CoreType.Boolean: return () => new ByteConverterBooleanIReadOnlySet();
                        case CoreType.Byte: return () => new ByteConverterByteIReadOnlySet();
                        case CoreType.SByte: return () => new ByteConverterSByteIReadOnlySet();
                        case CoreType.Int16: return () => new ByteConverterInt16IReadOnlySet();
                        case CoreType.UInt16: return () => new ByteConverterUInt16IReadOnlySet();
                        case CoreType.Int32: return () => new ByteConverterInt32IReadOnlySet();
                        case CoreType.UInt32: return () => new ByteConverterUInt32IReadOnlySet();
                        case CoreType.Int64: return () => new ByteConverterInt64IReadOnlySet();
                        case CoreType.UInt64: return () => new ByteConverterUInt64IReadOnlySet();
                        case CoreType.Single: return () => new ByteConverterSingleIReadOnlySet();
                        case CoreType.Double: return () => new ByteConverterDoubleIReadOnlySet();
                        case CoreType.Decimal: return () => new ByteConverterDecimalIReadOnlySet();
                        case CoreType.Char: return () => new ByteConverterCharIReadOnlySet();
                        case CoreType.DateTime: return () => new ByteConverterDateTimeIReadOnlySet();
                        case CoreType.DateTimeOffset: return () => new ByteConverterDateTimeOffsetIReadOnlySet();
                        case CoreType.TimeSpan: return () => new ByteConverterTimeSpanIReadOnlySet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnly: return () => new ByteConverterDateOnlyIReadOnlySet();
                        case CoreType.TimeOnly: return () => new ByteConverterTimeOnlyIReadOnlySet();
#endif
                        case CoreType.Guid: return () => new ByteConverterGuidIReadOnlySet();
                        case CoreType.BooleanNullable: return () => new ByteConverterBooleanNullableIReadOnlySet();
                        case CoreType.ByteNullable: return () => new ByteConverterByteNullableIReadOnlySet();
                        case CoreType.SByteNullable: return () => new ByteConverterSByteNullableIReadOnlySet();
                        case CoreType.Int16Nullable: return () => new ByteConverterInt16NullableIReadOnlySet();
                        case CoreType.UInt16Nullable: return () => new ByteConverterUInt16NullableIReadOnlySet();
                        case CoreType.Int32Nullable: return () => new ByteConverterInt32NullableIReadOnlySet();
                        case CoreType.UInt32Nullable: return () => new ByteConverterUInt32NullableIReadOnlySet();
                        case CoreType.Int64Nullable: return () => new ByteConverterInt64NullableIReadOnlySet();
                        case CoreType.UInt64Nullable: return () => new ByteConverterUInt64NullableIReadOnlySet();
                        case CoreType.SingleNullable: return () => new ByteConverterSingleNullableIReadOnlySet();
                        case CoreType.DoubleNullable: return () => new ByteConverterDoubleNullableIReadOnlySet();
                        case CoreType.DecimalNullable: return () => new ByteConverterDecimalNullableIReadOnlySet();
                        case CoreType.CharNullable: return () => new ByteConverterCharNullableIReadOnlySet();
                        case CoreType.DateTimeNullable: return () => new ByteConverterDateTimeNullableIReadOnlySet();
                        case CoreType.DateTimeOffsetNullable: return () => new ByteConverterDateTimeOffsetNullableIReadOnlySet();
                        case CoreType.TimeSpanNullable: return () => new ByteConverterTimeSpanNullableIReadOnlySet();
#if NET6_0_OR_GREATER
                        case CoreType.DateOnlyNullable: return () => new ByteConverterDateOnlyNullableIReadOnlySet();
                        case CoreType.TimeOnlyNullable: return () => new ByteConverterTimeOnlyNullableIReadOnlySet();
#endif
                        case CoreType.GuidNullable: return () => new ByteConverterGuidNullableIReadOnlySet();
                            //case CoreType.String: return () => new ByteConverterStringIReadOnlySet();
                    }
                }
            }

            if (typeDetail.IsIList)
                return () => new ByteConverterIList();
            if (typeDetail.IsIListGeneric)
                return () => new ByteConverterIListT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyListGeneric)
                return () => new ByteConverterIReadOnlyListT<TEnumerableType>();
            if (typeDetail.IsListGeneric)
                return () => new ByteConverterListT<TEnumerableType>();

            if (typeDetail.IsISetGeneric)
                return () => new ByteConverterISetT<TEnumerableType>();
            if (typeDetail.IsIReadOnlySetGeneric)
                return () => new ByteConverterIReadOnlySetT<TEnumerableType>();
            if (typeDetail.IsHashSetGeneric)
                return () => new ByteConverterHashSetT<TEnumerableType>();

            if (typeDetail.IsICollection)
                return () => new ByteConverterICollection();
            if (typeDetail.IsICollectionGeneric)
                return () => new ByteConverterICollectionT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyCollectionGeneric)
                return () => new ByteConverterIReadOnlyCollectionT<TEnumerableType>();

            if (typeDetail.IsIDictionary)
                return () => new ByteConverterIDictionary();
            if (typeDetail.IsIDictionaryGeneric)
                return () => new ByteConverterIDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsIReadOnlyDictionaryGeneric)
                return () => new ByteConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsDictionaryGeneric)
                return () => new ByteConverterDictionaryT<TDictionaryKey, TDictionaryValue>();

            if (typeDetail.IsIEnumerable)
                return () => new ByteConverterIEnumerable();
            if (typeDetail.IsIEnumerableGeneric)
                return () => new ByteConverterIEnumerableT<TEnumerableType>();

            if (typeDetail.Type.FullName == "System.Type")
                return () => new ByteConverterType();
            if (typeDetail.Type.FullName == "System.Threading.CancellationToken")
                return () => new ByteConverterCancellationToken();
            if (typeDetail.IsNullable && typeDetail.InnerType!.FullName == "System.Threading.CancellationToken")
                return () => new ByteConverterCancellationTokenNullable();

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No ByteConverter found to support {typeDetail.Type.Name}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType!.IsEnum)
                return () => new ByteConverterEnum<TType>();

            //Array
            if (typeDetail.Type.IsArray)
                return () => new ByteConverterArrayT<TEnumerableType>();

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return () => new ByteConverterIListTOfT<TType, TEnumerableType>();

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return () => new ByteConverterIListOfT<TType>();

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return () => new ByteConverterISetTOfT<TType, TEnumerableType>();

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return () => new ByteConverterIDictionaryTOfT<TType, TDictionaryKey, TDictionaryValue>();

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return () => new ByteConverterIDictionaryOfT<TType>();

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return () => new ByteConverterICollectionTOfT<TType, TEnumerableType>();

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return () => new ByteConverterIEnumerableTOfT<TType, TEnumerableType>();

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return () => new ByteConverterIEnumerableOfT<TType>();

            //Object
            return () => new ByteConverterObject<TType>();
        }
    }
}