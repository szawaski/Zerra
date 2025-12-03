// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Serialization.Json.Converters.Collections.Collections;
using Zerra.Serialization.Json.Converters.Collections.Dictionaries;
using Zerra.Serialization.Json.Converters.Collections.Enumerables;
using Zerra.Serialization.Json.Converters.Collections.Lists;
using Zerra.Serialization.Json.Converters.Collections.Sets;
using Zerra.Serialization.Json.Converters.CoreTypes.Values;
using Zerra.Serialization.Json.Converters.General;
using Zerra.Serialization.Json.Converters.Special;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

namespace Zerra.Serialization.Json.Converters
{
    public static class JsonConverterFactory
    {
        private static readonly ConcurrentFactoryDictionary<Type, Func<JsonConverter>> creators = new();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, JsonConverter>> cache = new();

        internal static JsonConverter CreateRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        public static JsonConverter Get(TypeDetail typeDetail, string memberKey, Delegate? getter, Delegate? setter)
        {
            var cache2 = cache.GetOrAdd(typeDetail.Type, static () => new());
            var converter = cache2.GetOrAdd(memberKey, typeDetail, memberKey, getter, setter, static (typeDetail, memberKey, getter, setter) =>
            {
                var creator = creators.GetOrAdd(typeDetail.Type, typeDetail, GenerateJsonConverterCreator);
                var newConverter = creator();
                newConverter.Setup(memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        public static Func<JsonConverter> GenerateJsonConverterCreator(TypeDetail typeDetail)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new InvalidOperationException($"JsonConverter type not found for {typeDetail.Type.Name} and dynamic code generation is not supported in this build configuration");

            var type = typeDetail.Type;
            var enumerableType = typeDetail.IEnumerableGenericInnerType ?? typeof(object);
            var dictionaryKeyType = typeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(0) ?? typeof(object);
            var dictionaryValueType = typeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(1) ?? typeof(object);

            var findCreatorMethodDefinition = typeof(JsonConverterFactory).GetMethod(nameof(FindCreator), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var findCreatorMethod = findCreatorMethodDefinition.MakeGenericMethod(type, enumerableType, dictionaryKeyType, dictionaryValueType);
            var creator = (Func<JsonConverter>)findCreatorMethod.Invoke(null, [typeDetail])!;
            return creator;
        }

        public static void RegisterCreator<TType>(Type type) => RegisterCreator<TType, object, object, object>(type);

        public static void RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(Type type)
            where TDictionaryKey : notnull
        {
            var creator = FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(type.GetTypeDetail());
            _ = creators.TryAdd(type, creator);
        }

        private static Func<JsonConverter> FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(TypeDetail typeDetail)
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
                throw new NotSupportedException($"No JsonConverter found to support {typeDetail.Type.GetNiceName()}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType!.IsEnum)
                return () => new JsonConverterEnum<TType>();

            //Array
            if (typeDetail.Type.IsArray)
                return () => new JsonConverterArrayT<TEnumerableType>();

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return () => new JsonConverterIListTOfT<TType, TEnumerableType>();

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return () => new JsonConverterIListOfT<TType>();

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return () => new JsonConverterISetTOfT<TType, TEnumerableType>();

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return () => new JsonConverterIDictionaryTOfT<TType, TDictionaryKey, TDictionaryValue>();

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return () => new JsonConverterIDictionaryOfT<TType>();

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return () => new JsonConverterICollectionTOfT<TType, TEnumerableType>();

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return () => new JsonConverterIEnumerableTOfT<TType, TEnumerableType>();

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return () => new JsonConverterIEnumerableOfT<TType>();

            //Object
            return () => new JsonConverterObject<TType>();
        }

        private static Func<JsonConverter>? FindCreatorTypeLookup<TEnumerableType, TDictionaryKey, TDictionaryValue>(string name)
            where TDictionaryKey : notnull
        {
            return name switch
            {
                #region Collections
                "System.Collections.ICollection" => () => new JsonConverterICollection(),
                "System.Collections.Generic.ICollection<T>" => () => new JsonConverterICollectionT<TEnumerableType>(),
                "System.Collections.Generic.IReadOnlyCollection<T>" => () => new JsonConverterIReadOnlyCollectionT<TEnumerableType>(),
                #endregion

                #region Dictionaries
                "System.Collections.Generic.Dictionary<T,T>" => () => new JsonConverterDictionaryT<TDictionaryKey, TDictionaryValue>(),
                "System.Collections.IDictionary" => () => new JsonConverterIDictionary(),
                "System.Collections.Generic.IDictionary<T,T>" => () => new JsonConverterIDictionaryT<TDictionaryKey, TDictionaryValue>(),
                "System.Collections.Generic.IReadOnlyDictionary<T,T>" => () => new JsonConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>(),
                #endregion

                #region Enumerables
                "System.Collections.IEnumerable" => () => new JsonConverterIEnumerable(),
                "System.Collections.Generic.IEnumerable<T>" => () => new JsonConverterIEnumerableT<TEnumerableType>(),
                #endregion

                #region Lists
                "System.Collections.IList" => () => new JsonConverterIList(),
                "System.Collections.Generic.IList<T>" => () => new JsonConverterIListT<TEnumerableType>(),
                "System.Collections.Generic.IReadOnlyList<T>" => () => new JsonConverterIReadOnlyListT<TEnumerableType>(),
                "System.Collections.Generic.List<T>" => () => new JsonConverterListT<TEnumerableType>(),
                #endregion

                #region Sets
                "System.Collections.Generic.HashSet<T>" => () => new JsonConverterHashSetT<TEnumerableType>(),
#if NET5_0_OR_GREATER
                "System.Collections.Generic.IReadOnlySet<T>" => () => new JsonConverterIReadOnlySetT<TEnumerableType>(),
#endif
                "System.Collections.Generic.ISet<T>" => () => new JsonConverterISetT<TEnumerableType>(),
                #endregion

                #region CoreTypeValues
                "System.Boolean" => () => new JsonConverterBoolean(),
                "System.Nullable<System.Boolean>" => () => new JsonConverterBooleanNullable(),
                "System.Byte" => () => new JsonConverterByte(),
                "System.Nullable<System.Byte>" => () => new JsonConverterByteNullable(),
                "System.SByte" => () => new JsonConverterSByte(),
                "System.Nullable<System.SByte>" => () => new JsonConverterSByteNullable(),
                "System.Int16" => () => new JsonConverterInt16(),
                "System.Nullable<System.Int16>" => () => new JsonConverterInt16Nullable(),
                "System.UInt16" => () => new JsonConverterUInt16(),
                "System.Nullable<System.UInt16>" => () => new JsonConverterUInt16Nullable(),
                "System.Int32" => () => new JsonConverterInt32(),
                "System.Nullable<System.Int32>" => () => new JsonConverterInt32Nullable(),
                "System.UInt32" => () => new JsonConverterUInt32(),
                "System.Nullable<System.UInt32>" => () => new JsonConverterUInt32Nullable(),
                "System.Int64" => () => new JsonConverterInt64(),
                "System.Nullable<System.Int64>" => () => new JsonConverterInt64Nullable(),
                "System.UInt64" => () => new JsonConverterUInt64(),
                "System.Nullable<System.UInt64>" => () => new JsonConverterUInt64Nullable(),
                "System.Single" => () => new JsonConverterSingle(),
                "System.Nullable<System.Single>" => () => new JsonConverterSingleNullable(),
                "System.Double" => () => new JsonConverterDouble(),
                "System.Nullable<System.Double>" => () => new JsonConverterDoubleNullable(),
                "System.Decimal" => () => new JsonConverterDecimal(),
                "System.Nullable<System.Decimal>" => () => new JsonConverterDecimalNullable(),
                "System.Char" => () => new JsonConverterChar(),
                "System.Nullable<System.Char>" => () => new JsonConverterCharNullable(),
                "System.DateTime" => () => new JsonConverterDateTime(),
                "System.Nullable<System.DateTime>" => () => new JsonConverterDateTimeNullable(),
                "System.DateTimeOffset" => () => new JsonConverterDateTimeOffset(),
                "System.Nullable<System.DateTimeOffset>" => () => new JsonConverterDateTimeOffsetNullable(),
                "System.TimeSpan" => () => new JsonConverterTimeSpan(),
                "System.Nullable<System.TimeSpan>" => () => new JsonConverterTimeSpanNullable(),
#if NET5_0_OR_GREATER
                "System.DateOnly" => () => new JsonConverterDateOnly(),
                "System.Nullable<System.DateOnly>" => () => new JsonConverterDateOnlyNullable(),
                "System.TimeOnly" => () => new JsonConverterTimeOnly(),
                "System.Nullable<System.TimeOnly>" => () => new JsonConverterTimeOnlyNullable(),
#endif
                "System.Guid" => () => new JsonConverterGuid(),
                "System.Nullable<System.Guid>" => () => new JsonConverterGuidNullable(),
                "System.String" => () => new JsonConverterString(),
                #endregion

                "System.Type" => () => new JsonConverterType(),

                "System.Byte[]" => () => new JsonConverterByteArray(),
                "System.Threading.CancellationToken" => () => new JsonConverterCancellationToken(),
                "System.Nullable<System.Threading.CancellationToken>" => () => new JsonConverterCancellationTokenNullable(),
                _ => null,
            };
        }

        internal static void AddConverter(Type converterType, Func<JsonConverter> converter)
        {
            creators[converterType] = converter;
        }
    }
}