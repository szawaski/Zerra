// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
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
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

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

        public static Func<ByteConverter> GenerateByteConverterCreator(TypeDetail typeDetail)
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

        public static void RegisterCreator<TType>(Type type) => RegisterCreator<TType, object, object, object>(type);

        public static void RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(Type type)
            where TDictionaryKey : notnull
        {
            var creator = FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(type.GetTypeDetail());
            _ = creators.TryAdd(type, creator);
        }

        private static Func<ByteConverter> FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(TypeDetail typeDetail)
            where TDictionaryKey : notnull
        {
            var name = TypeHelper.GetNiceFullName(typeDetail.Type);
            var creator = FindCreatorTypeLookup<TEnumerableType, TDictionaryKey, TDictionaryValue>(name);
            if (creator == null && typeDetail.Type.IsGenericType && typeDetail.Type.Name != "Nullable`1")
            {
                name = TypeHelper.MakeNiceNameGeneric(name);
                creator = FindCreatorTypeLookup<TEnumerableType, TDictionaryKey, TDictionaryValue>(name);
            }

            if (creator != null)
                return creator;

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No ByteConverter found to support {typeDetail.Type.GetNiceName()}");

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

        private static Func<ByteConverter>? FindCreatorTypeLookup<TEnumerableType, TDictionaryKey, TDictionaryValue>(string name)
            where TDictionaryKey : notnull
        {
            return name switch
            {
                #region Collections
                "System.Collections.ICollection" => () => new ByteConverterICollection(),
                "System.Collections.Generic.ICollection<T>" => () => new ByteConverterICollectionT<TEnumerableType>(),
                "System.Collections.Generic.IReadOnlyCollection<T>" => () => new ByteConverterIReadOnlyCollectionT<TEnumerableType>(),
                #endregion

                #region Dictionaries
                "System.Collections.Generic.Dictionary<T,T>" => () => new ByteConverterDictionaryT<TDictionaryKey, TDictionaryValue>(),
                "System.Collections.IDictionary" => () => new ByteConverterIDictionary(),
                "System.Collections.Generic.IDictionary<T,T>" => () => new ByteConverterIDictionaryT<TDictionaryKey, TDictionaryValue>(),
                "System.Collections.Generic.IReadOnlyDictionary<T,T>" => () => new ByteConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>(),
                #endregion

                #region Enumerables
                "System.Collections.IEnumerable" => () => new ByteConverterIEnumerable(),
                "System.Collections.Generic.IEnumerable<T>" => () => new ByteConverterIEnumerableT<TEnumerableType>(),
                #endregion

                #region Lists
                "System.Collections.IList" => () => new ByteConverterIList<TEnumerableType>(),
                "System.Collections.Generic.IList<T>" => () => new ByteConverterIListT<TEnumerableType>(),
                "System.Collections.Generic.IReadOnlyList<T>" => () => new ByteConverterIReadOnlyListT<TEnumerableType>(),
                "System.Collections.Generic.List<T>" => () => new ByteConverterListT<TEnumerableType>(),
                #endregion

                #region Sets
                "System.Collections.Generic.HashSet<T>" => () => new ByteConverterHashSetT<TEnumerableType>(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IReadOnlySet<T>" => () => new ByteConverterIReadOnlySetT<TEnumerableType>(),
#endif
                "System.Collections.Generic.ISet<T>" => () => new ByteConverterISetT<TEnumerableType>(),
                #endregion

                #region CoreTypeValues
                "System.Boolean" => () => new ByteConverterBoolean(),
                "System.Nullable<System.Boolean>" => () => new ByteConverterBooleanNullable(),
                "System.Byte" => () => new ByteConverterByte(),
                "System.Nullable<System.Byte>" => () => new ByteConverterByteNullable(),
                "System.SByte" => () => new ByteConverterSByte(),
                "System.Nullable<System.SByte>" => () => new ByteConverterSByteNullable(),
                "System.Int16" => () => new ByteConverterInt16(),
                "System.Nullable<System.Int16>" => () => new ByteConverterInt16Nullable(),
                "System.UInt16" => () => new ByteConverterUInt16(),
                "System.Nullable<System.UInt16>" => () => new ByteConverterUInt16Nullable(),
                "System.Int32" => () => new ByteConverterInt32(),
                "System.Nullable<System.Int32>" => () => new ByteConverterInt32Nullable(),
                "System.UInt32" => () => new ByteConverterUInt32(),
                "System.Nullable<System.UInt32>" => () => new ByteConverterUInt32Nullable(),
                "System.Int64" => () => new ByteConverterInt64(),
                "System.Nullable<System.Int64>" => () => new ByteConverterInt64Nullable(),
                "System.UInt64" => () => new ByteConverterUInt64(),
                "System.Nullable<System.UInt64>" => () => new ByteConverterUInt64Nullable(),
                "System.Single" => () => new ByteConverterSingle(),
                "System.Nullable<System.Single>" => () => new ByteConverterSingleNullable(),
                "System.Double" => () => new ByteConverterDouble(),
                "System.Nullable<System.Double>" => () => new ByteConverterDoubleNullable(),
                "System.Decimal" => () => new ByteConverterDecimal(),
                "System.Nullable<System.Decimal>" => () => new ByteConverterDecimalNullable(),
                "System.Char" => () => new ByteConverterChar(),
                "System.Nullable<System.Char>" => () => new ByteConverterCharNullable(),
                "System.DateTime" => () => new ByteConverterDateTime(),
                "System.Nullable<System.DateTime>" => () => new ByteConverterDateTimeNullable(),
                "System.DateTimeOffset" => () => new ByteConverterDateTimeOffset(),
                "System.Nullable<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetNullable(),
                "System.TimeSpan" => () => new ByteConverterTimeSpan(),
                "System.Nullable<System.TimeSpan>" => () => new ByteConverterTimeSpanNullable(),
#if NET5_0_OR_GREATER
                "System.DateOnly" => () => new ByteConverterDateOnly(),
                "System.Nullable<System.DateOnly>" => () => new ByteConverterDateOnlyNullable(),
                "System.TimeOnly" => () => new ByteConverterTimeOnly(),
                "System.Nullable<System.TimeOnly>" => () => new ByteConverterTimeOnlyNullable(),
#endif
                "System.Guid" => () => new ByteConverterGuid(),
                "System.Nullable<System.Guid>" => () => new ByteConverterGuidNullable(),
                "System.String" => () => new ByteConverterString(),
                #endregion

                #region CoreTypeArrays
                "System.Boolean[]" => () => new ByteConverterBooleanArray(),
                "System.Nullable<System.Boolean>[]" => () => new ByteConverterBooleanNullableArray(),
                "System.Byte[]" => () => new ByteConverterByteArray(),
                "System.Nullable<System.Byte>[]" => () => new ByteConverterByteNullableArray(),
                "System.SByte[]" => () => new ByteConverterSByteArray(),
                "System.Nullable<System.SByte>[]" => () => new ByteConverterSByteNullableArray(),
                "System.Int16[]" => () => new ByteConverterInt16Array(),
                "System.Nullable<System.Int16>[]" => () => new ByteConverterInt16NullableArray(),
                "System.UInt16[]" => () => new ByteConverterUInt16Array(),
                "System.Nullable<System.UInt16>[]" => () => new ByteConverterUInt16NullableArray(),
                "System.Int32[]" => () => new ByteConverterInt32Array(),
                "System.Nullable<System.Int32>[]" => () => new ByteConverterInt32NullableArray(),
                "System.UInt32[]" => () => new ByteConverterUInt32Array(),
                "System.Nullable<System.UInt32>[]" => () => new ByteConverterUInt32NullableArray(),
                "System.Int64[]" => () => new ByteConverterInt64Array(),
                "System.Nullable<System.Int64>[]" => () => new ByteConverterInt64NullableArray(),
                "System.UInt64[]" => () => new ByteConverterUInt64Array(),
                "System.Nullable<System.UInt64>[]" => () => new ByteConverterUInt64NullableArray(),
                "System.Single[]" => () => new ByteConverterSingleArray(),
                "System.Nullable<System.Single>[]" => () => new ByteConverterSingleNullableArray(),
                "System.Double[]" => () => new ByteConverterDoubleArray(),
                "System.Nullable<System.Double>[]" => () => new ByteConverterDoubleNullableArray(),
                "System.Decimal[]" => () => new ByteConverterDecimalArray(),
                "System.Nullable<System.Decimal>[]" => () => new ByteConverterDecimalNullableArray(),
                "System.Char[]" => () => new ByteConverterCharArray(),
                "System.Nullable<System.Char>[]" => () => new ByteConverterCharNullableArray(),
                "System.DateTime[]" => () => new ByteConverterDateTimeArray(),
                "System.Nullable<System.DateTime>[]" => () => new ByteConverterDateTimeNullableArray(),
                "System.DateTimeOffset[]" => () => new ByteConverterDateTimeOffsetArray(),
                "System.Nullable<System.DateTimeOffset>[]" => () => new ByteConverterDateTimeOffsetNullableArray(),
                "System.TimeSpan[]" => () => new ByteConverterTimeSpanArray(),
                "System.Nullable<System.TimeSpan>[]" => () => new ByteConverterTimeSpanNullableArray(),
#if NET5_0_OR_GREATER
                "System.DateOnly[]" => () => new ByteConverterDateOnlyArray(),
                "System.Nullable<System.DateOnly>[]" => () => new ByteConverterDateOnlyNullableArray(),
                "System.TimeOnly[]" => () => new ByteConverterTimeOnlyArray(),
                "System.Nullable<System.TimeOnly>[]" => () => new ByteConverterTimeOnlyNullableArray(),
#endif
                "System.Guid[]" => () => new ByteConverterGuidArray(),
                "System.Nullable<System.Guid>[]" => () => new ByteConverterGuidNullableArray(),
                //case "System.String[]": return () => new ByteConverterStringArray() ;
                #endregion

                #region CoreTypeLists
                "System.Collections.Generic.List<System.Boolean>" => () => new ByteConverterBooleanList(),
                "System.Collections.Generic.List<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableList(),
                "System.Collections.Generic.List<System.Byte>" => () => new ByteConverterByteList(),
                "System.Collections.Generic.List<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableList(),
                "System.Collections.Generic.List<System.SByte>" => () => new ByteConverterSByteList(),
                "System.Collections.Generic.List<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableList(),
                "System.Collections.Generic.List<System.Int16>" => () => new ByteConverterInt16List(),
                "System.Collections.Generic.List<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableList(),
                "System.Collections.Generic.List<System.UInt16>" => () => new ByteConverterUInt16List(),
                "System.Collections.Generic.List<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableList(),
                "System.Collections.Generic.List<System.Int32>" => () => new ByteConverterInt32List(),
                "System.Collections.Generic.List<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableList(),
                "System.Collections.Generic.List<System.UInt32>" => () => new ByteConverterUInt32List(),
                "System.Collections.Generic.List<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableList(),
                "System.Collections.Generic.List<System.Int64>" => () => new ByteConverterInt64List(),
                "System.Collections.Generic.List<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableList(),
                "System.Collections.Generic.List<System.UInt64>" => () => new ByteConverterUInt64List(),
                "System.Collections.Generic.List<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableList(),
                "System.Collections.Generic.List<System.Single>" => () => new ByteConverterSingleList(),
                "System.Collections.Generic.List<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableList(),
                "System.Collections.Generic.List<System.Double>" => () => new ByteConverterDoubleList(),
                "System.Collections.Generic.List<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableList(),
                "System.Collections.Generic.List<System.Decimal>" => () => new ByteConverterDecimalList(),
                "System.Collections.Generic.List<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableList(),
                "System.Collections.Generic.List<System.Char>" => () => new ByteConverterCharList(),
                "System.Collections.Generic.List<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableList(),
                "System.Collections.Generic.List<System.DateTime>" => () => new ByteConverterDateTimeList(),
                "System.Collections.Generic.List<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableList(),
                "System.Collections.Generic.List<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetList(),
                "System.Collections.Generic.List<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableList(),
                "System.Collections.Generic.List<System.TimeSpan>" => () => new ByteConverterTimeSpanList(),
                "System.Collections.Generic.List<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableList(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.List<System.DateOnly>" => () => new ByteConverterDateOnlyList(),
                "System.Collections.Generic.List<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableList(),
                "System.Collections.Generic.List<System.TimeOnly>" => () => new ByteConverterTimeOnlyList(),
                "System.Collections.Generic.List<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableList(),
#endif
                "System.Collections.Generic.List<System.Guid>" => () => new ByteConverterGuidList(),
                "System.Collections.Generic.List<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableList(),
                //case "System.Collections.Generic.List<System.String>": return () => new ByteConverterStringList() ;
                #endregion

                #region CoreTypeILists
                "System.Collections.Generic.IList<System.Boolean>" => () => new ByteConverterBooleanIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableIList(),
                "System.Collections.Generic.IList<System.Byte>" => () => new ByteConverterByteIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableIList(),
                "System.Collections.Generic.IList<System.SByte>" => () => new ByteConverterSByteIList(),
                "System.Collections.Generic.IList<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableIList(),
                "System.Collections.Generic.IList<System.Int16>" => () => new ByteConverterInt16IList(),
                "System.Collections.Generic.IList<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableIList(),
                "System.Collections.Generic.IList<System.UInt16>" => () => new ByteConverterUInt16IList(),
                "System.Collections.Generic.IList<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableIList(),
                "System.Collections.Generic.IList<System.Int32>" => () => new ByteConverterInt32IList(),
                "System.Collections.Generic.IList<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableIList(),
                "System.Collections.Generic.IList<System.UInt32>" => () => new ByteConverterUInt32IList(),
                "System.Collections.Generic.IList<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableIList(),
                "System.Collections.Generic.IList<System.Int64>" => () => new ByteConverterInt64IList(),
                "System.Collections.Generic.IList<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableIList(),
                "System.Collections.Generic.IList<System.UInt64>" => () => new ByteConverterUInt64IList(),
                "System.Collections.Generic.IList<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableIList(),
                "System.Collections.Generic.IList<System.Single>" => () => new ByteConverterSingleIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableIList(),
                "System.Collections.Generic.IList<System.Double>" => () => new ByteConverterDoubleIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableIList(),
                "System.Collections.Generic.IList<System.Decimal>" => () => new ByteConverterDecimalIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableIList(),
                "System.Collections.Generic.IList<System.Char>" => () => new ByteConverterCharIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableIList(),
                "System.Collections.Generic.IList<System.DateTime>" => () => new ByteConverterDateTimeIList(),
                "System.Collections.Generic.IList<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableIList(),
                "System.Collections.Generic.IList<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetIList(),
                "System.Collections.Generic.IList<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableIList(),
                "System.Collections.Generic.IList<System.TimeSpan>" => () => new ByteConverterTimeSpanIList(),
                "System.Collections.Generic.IList<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableIList(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IList<System.DateOnly>" => () => new ByteConverterDateOnlyIList(),
                "System.Collections.Generic.IList<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableIList(),
                "System.Collections.Generic.IList<System.TimeOnly>" => () => new ByteConverterTimeOnlyIList(),
                "System.Collections.Generic.IList<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableIList(),
#endif
                "System.Collections.Generic.IList<System.Guid>" => () => new ByteConverterGuidIList(),
                "System.Collections.Generic.IList<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableIList(),
                //case "System.Collections.Generic.IList<System.String>": return () => new ByteConverterStringIList() ;
                #endregion

                #region CoreTypeIReadOnlyLists
                "System.Collections.Generic.IReadOnlyList<System.Boolean>" => () => new ByteConverterBooleanIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Byte>" => () => new ByteConverterByteIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.SByte>" => () => new ByteConverterSByteIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Int16>" => () => new ByteConverterInt16IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.UInt16>" => () => new ByteConverterUInt16IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Int32>" => () => new ByteConverterInt32IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.UInt32>" => () => new ByteConverterUInt32IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Int64>" => () => new ByteConverterInt64IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.UInt64>" => () => new ByteConverterUInt64IReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Single>" => () => new ByteConverterSingleIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Double>" => () => new ByteConverterDoubleIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Decimal>" => () => new ByteConverterDecimalIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Char>" => () => new ByteConverterCharIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.DateTime>" => () => new ByteConverterDateTimeIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.TimeSpan>" => () => new ByteConverterTimeSpanIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableIReadOnlyList(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IReadOnlyList<System.DateOnly>" => () => new ByteConverterDateOnlyIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.TimeOnly>" => () => new ByteConverterTimeOnlyIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableIReadOnlyList(),
#endif
                "System.Collections.Generic.IReadOnlyList<System.Guid>" => () => new ByteConverterGuidIReadOnlyList(),
                "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableIReadOnlyList(),
                //case "System.Collections.Generic.IReadOnlyList<System.String>": return () => new ByteConverterStringIReadOnlyList() ;
                #endregion

                #region CoreTypeICollections
                "System.Collections.Generic.ICollection<System.Boolean>" => () => new ByteConverterBooleanICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableICollection(),
                "System.Collections.Generic.ICollection<System.Byte>" => () => new ByteConverterByteICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableICollection(),
                "System.Collections.Generic.ICollection<System.SByte>" => () => new ByteConverterSByteICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableICollection(),
                "System.Collections.Generic.ICollection<System.Int16>" => () => new ByteConverterInt16ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableICollection(),
                "System.Collections.Generic.ICollection<System.UInt16>" => () => new ByteConverterUInt16ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableICollection(),
                "System.Collections.Generic.ICollection<System.Int32>" => () => new ByteConverterInt32ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableICollection(),
                "System.Collections.Generic.ICollection<System.UInt32>" => () => new ByteConverterUInt32ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableICollection(),
                "System.Collections.Generic.ICollection<System.Int64>" => () => new ByteConverterInt64ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableICollection(),
                "System.Collections.Generic.ICollection<System.UInt64>" => () => new ByteConverterUInt64ICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableICollection(),
                "System.Collections.Generic.ICollection<System.Single>" => () => new ByteConverterSingleICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableICollection(),
                "System.Collections.Generic.ICollection<System.Double>" => () => new ByteConverterDoubleICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableICollection(),
                "System.Collections.Generic.ICollection<System.Decimal>" => () => new ByteConverterDecimalICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableICollection(),
                "System.Collections.Generic.ICollection<System.Char>" => () => new ByteConverterCharICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableICollection(),
                "System.Collections.Generic.ICollection<System.DateTime>" => () => new ByteConverterDateTimeICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableICollection(),
                "System.Collections.Generic.ICollection<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableICollection(),
                "System.Collections.Generic.ICollection<System.TimeSpan>" => () => new ByteConverterTimeSpanICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableICollection(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.ICollection<System.DateOnly>" => () => new ByteConverterDateOnlyICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableICollection(),
                "System.Collections.Generic.ICollection<System.TimeOnly>" => () => new ByteConverterTimeOnlyICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableICollection(),
#endif
                "System.Collections.Generic.ICollection<System.Guid>" => () => new ByteConverterGuidICollection(),
                "System.Collections.Generic.ICollection<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableICollection(),
                //case "System.Collections.Generic.ICollection<System.String>": return () => new ByteConverterStringICollection() ;
                #endregion

                #region CoreTypeIReadOnlyCollections
                "System.Collections.Generic.IReadOnlyCollection<System.Boolean>" => () => new ByteConverterBooleanIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Byte>" => () => new ByteConverterByteIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.SByte>" => () => new ByteConverterSByteIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Int16>" => () => new ByteConverterInt16IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.UInt16>" => () => new ByteConverterUInt16IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Int32>" => () => new ByteConverterInt32IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.UInt32>" => () => new ByteConverterUInt32IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Int64>" => () => new ByteConverterInt64IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.UInt64>" => () => new ByteConverterUInt64IReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Single>" => () => new ByteConverterSingleIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Double>" => () => new ByteConverterDoubleIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Decimal>" => () => new ByteConverterDecimalIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Char>" => () => new ByteConverterCharIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.DateTime>" => () => new ByteConverterDateTimeIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.TimeSpan>" => () => new ByteConverterTimeSpanIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableIReadOnlyCollection(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IReadOnlyCollection<System.DateOnly>" => () => new ByteConverterDateOnlyIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.TimeOnly>" => () => new ByteConverterTimeOnlyIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableIReadOnlyCollection(),
#endif
                "System.Collections.Generic.IReadOnlyCollection<System.Guid>" => () => new ByteConverterGuidIReadOnlyCollection(),
                "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableIReadOnlyCollection(),
                //case "System.Collections.Generic.IReadOnlyCollection<System.String>": return () => new ByteConverterStringIReadOnlyCollection() ;
                #endregion

                #region CoreTypeHashSets
                "System.Collections.Generic.HashSet<System.Boolean>" => () => new ByteConverterBooleanHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableHashSet(),
                "System.Collections.Generic.HashSet<System.Byte>" => () => new ByteConverterByteHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableHashSet(),
                "System.Collections.Generic.HashSet<System.SByte>" => () => new ByteConverterSByteHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableHashSet(),
                "System.Collections.Generic.HashSet<System.Int16>" => () => new ByteConverterInt16HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableHashSet(),
                "System.Collections.Generic.HashSet<System.UInt16>" => () => new ByteConverterUInt16HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableHashSet(),
                "System.Collections.Generic.HashSet<System.Int32>" => () => new ByteConverterInt32HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableHashSet(),
                "System.Collections.Generic.HashSet<System.UInt32>" => () => new ByteConverterUInt32HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableHashSet(),
                "System.Collections.Generic.HashSet<System.Int64>" => () => new ByteConverterInt64HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableHashSet(),
                "System.Collections.Generic.HashSet<System.UInt64>" => () => new ByteConverterUInt64HashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableHashSet(),
                "System.Collections.Generic.HashSet<System.Single>" => () => new ByteConverterSingleHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableHashSet(),
                "System.Collections.Generic.HashSet<System.Double>" => () => new ByteConverterDoubleHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableHashSet(),
                "System.Collections.Generic.HashSet<System.Decimal>" => () => new ByteConverterDecimalHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableHashSet(),
                "System.Collections.Generic.HashSet<System.Char>" => () => new ByteConverterCharHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableHashSet(),
                "System.Collections.Generic.HashSet<System.DateTime>" => () => new ByteConverterDateTimeHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableHashSet(),
                "System.Collections.Generic.HashSet<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableHashSet(),
                "System.Collections.Generic.HashSet<System.TimeSpan>" => () => new ByteConverterTimeSpanHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableHashSet(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.HashSet<System.DateOnly>" => () => new ByteConverterDateOnlyHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableHashSet(),
                "System.Collections.Generic.HashSet<System.TimeOnly>" => () => new ByteConverterTimeOnlyHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableHashSet(),
#endif
                "System.Collections.Generic.HashSet<System.Guid>" => () => new ByteConverterGuidHashSet(),
                "System.Collections.Generic.HashSet<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableHashSet(),
                //case "System.Collections.Generic.HashSet<System.String>": return () => new ByteConverterStringHashSet() ;
                #endregion

                #region CoreTypeISets
                "System.Collections.Generic.ISet<System.Boolean>" => () => new ByteConverterBooleanISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableISet(),
                "System.Collections.Generic.ISet<System.Byte>" => () => new ByteConverterByteISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableISet(),
                "System.Collections.Generic.ISet<System.SByte>" => () => new ByteConverterSByteISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableISet(),
                "System.Collections.Generic.ISet<System.Int16>" => () => new ByteConverterInt16ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableISet(),
                "System.Collections.Generic.ISet<System.UInt16>" => () => new ByteConverterUInt16ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableISet(),
                "System.Collections.Generic.ISet<System.Int32>" => () => new ByteConverterInt32ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableISet(),
                "System.Collections.Generic.ISet<System.UInt32>" => () => new ByteConverterUInt32ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableISet(),
                "System.Collections.Generic.ISet<System.Int64>" => () => new ByteConverterInt64ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableISet(),
                "System.Collections.Generic.ISet<System.UInt64>" => () => new ByteConverterUInt64ISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableISet(),
                "System.Collections.Generic.ISet<System.Single>" => () => new ByteConverterSingleISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableISet(),
                "System.Collections.Generic.ISet<System.Double>" => () => new ByteConverterDoubleISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableISet(),
                "System.Collections.Generic.ISet<System.Decimal>" => () => new ByteConverterDecimalISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableISet(),
                "System.Collections.Generic.ISet<System.Char>" => () => new ByteConverterCharISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableISet(),
                "System.Collections.Generic.ISet<System.DateTime>" => () => new ByteConverterDateTimeISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableISet(),
                "System.Collections.Generic.ISet<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableISet(),
                "System.Collections.Generic.ISet<System.TimeSpan>" => () => new ByteConverterTimeSpanISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableISet(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.ISet<System.DateOnly>" => () => new ByteConverterDateOnlyISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableISet(),
                "System.Collections.Generic.ISet<System.TimeOnly>" => () => new ByteConverterTimeOnlyISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableISet(),
#endif
                "System.Collections.Generic.ISet<System.Guid>" => () => new ByteConverterGuidISet(),
                "System.Collections.Generic.ISet<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableISet(),
                //case "System.Collections.Generic.ISet<System.String>": return () => new ByteConverterStringISet() ;
                #endregion

#if NET5_0_OR_GREATER
                #region CoreTypeIReadOnlySets
                "System.Collections.Generic.IReadOnlySet<System.Boolean>" => () => new ByteConverterBooleanIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Boolean>>" => () => new ByteConverterBooleanNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Byte>" => () => new ByteConverterByteIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Byte>>" => () => new ByteConverterByteNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.SByte>" => () => new ByteConverterSByteIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.SByte>>" => () => new ByteConverterSByteNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Int16>" => () => new ByteConverterInt16IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int16>>" => () => new ByteConverterInt16NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.UInt16>" => () => new ByteConverterUInt16IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt16>>" => () => new ByteConverterUInt16NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Int32>" => () => new ByteConverterInt32IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int32>>" => () => new ByteConverterInt32NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.UInt32>" => () => new ByteConverterUInt32IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt32>>" => () => new ByteConverterUInt32NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Int64>" => () => new ByteConverterInt64IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int64>>" => () => new ByteConverterInt64NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.UInt64>" => () => new ByteConverterUInt64IReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt64>>" => () => new ByteConverterUInt64NullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Single>" => () => new ByteConverterSingleIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Single>>" => () => new ByteConverterSingleNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Double>" => () => new ByteConverterDoubleIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Double>>" => () => new ByteConverterDoubleNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Decimal>" => () => new ByteConverterDecimalIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Decimal>>" => () => new ByteConverterDecimalNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Char>" => () => new ByteConverterCharIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Char>>" => () => new ByteConverterCharNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.DateTime>" => () => new ByteConverterDateTimeIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateTime>>" => () => new ByteConverterDateTimeNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.DateTimeOffset>" => () => new ByteConverterDateTimeOffsetIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateTimeOffset>>" => () => new ByteConverterDateTimeOffsetNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.TimeSpan>" => () => new ByteConverterTimeSpanIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.TimeSpan>>" => () => new ByteConverterTimeSpanNullableIReadOnlySet(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IReadOnlySet<System.DateOnly>" => () => new ByteConverterDateOnlyIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateOnly>>" => () => new ByteConverterDateOnlyNullableIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.TimeOnly>" => () => new ByteConverterTimeOnlyIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.TimeOnly>>" => () => new ByteConverterTimeOnlyNullableIReadOnlySet(),
#endif
                "System.Collections.Generic.IReadOnlySet<System.Guid>" => () => new ByteConverterGuidIReadOnlySet(),
                "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Guid>>" => () => new ByteConverterGuidNullableIReadOnlySet(),
                //case "System.Collections.Generic.IReadOnlySet<System.String>": return () => new ByteConverterStringIReadOnlySet() ;
                #endregion
#endif

                "System.Type" => () => new ByteConverterType(),

                "System.Threading.CancellationToken" => () => new ByteConverterCancellationToken(),
                "System.Nullable<System.Threading.CancellationToken>" => () => new ByteConverterCancellationTokenNullable(),

                _ => null,
            };
        }

        internal static void AddConverter(Type converterType, Func<ByteConverter> converter)
        {
            creators[converterType] = converter;
        }
    }
}