// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Serialization.Json.Converters.Collections.Collections;
using Zerra.Serialization.Json.Converters.Collections.Dictionaries;
using Zerra.Serialization.Json.Converters.Collections.Enumerables;
using Zerra.Serialization.Json.Converters.Collections.Lists;
using Zerra.Serialization.Json.Converters.Collections.Sets;
using Zerra.Serialization.Json.Converters.CoreTypes.Values;
using Zerra.Serialization.Json.Converters.General;
using Zerra.Serialization.Json.Converters.Special;

namespace Zerra.Serialization.Json.Converters
{
    /// <summary>
    /// Factory for creating and caching <see cref="JsonConverter"/> instances for various types.
    /// </summary>
    /// <remarks>
    /// This factory uses dynamic code generation to create converters for types that don't have explicit implementations.
    /// Converters are cached both by type and by member key to improve performance.
    /// </remarks>
    public static class JsonConverterFactory
    {
        private static readonly ConcurrentFactoryDictionary<Type, Func<JsonConverter>> creators = new();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, JsonConverter>> cache = new();

        /// <summary>
        /// Gets or creates the root <see cref="JsonConverter"/> for the specified type detail.
        /// </summary>
        /// <param name="typeDetail">The type detail describing the type to create a converter for.</param>
        /// <returns>A <see cref="JsonConverter"/> instance for the specified type.</returns>
        internal static JsonConverter CreateRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        /// <summary>
        /// Gets or creates a <see cref="JsonConverter"/> for the specified type detail and member information.
        /// </summary>
        /// <param name="typeDetail">The type detail describing the type to create a converter for.</param>
        /// <param name="memberKey">A unique key identifying the member being converted.</param>
        /// <param name="getter">An optional delegate for getting the value from an object.</param>
        /// <param name="setter">An optional delegate for setting the value on an object.</param>
        /// <returns>A <see cref="JsonConverter"/> instance configured for the specified member.</returns>
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

        /// <summary>
        /// Registers a custom converter creator for a specific type.
        /// </summary>
        /// <param name="type">The type for which to register the converter.</param>
        /// <param name="converter">A factory function that creates instances of the converter.</param>
        internal static void AddConverter(Type type, Func<JsonConverter> converter)
        {
            creators[type] = converter;
        }

        internal static Func<JsonConverter> GenerateJsonConverterCreator(TypeDetail typeDetail)
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

        internal static void RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>()
            where TDictionaryKey : notnull
        {
            var type = typeof(TType);
            var creator = FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(type.GetTypeDetail());
            _ = creators.TryAdd(type, creator);
        }

        private static Func<JsonConverter> FindCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>(TypeDetail typeDetail)
            where TDictionaryKey : notnull
        {
            if (typeDetail.CoreType.HasValue)
            {
                switch (typeDetail.CoreType.Value)
                {
                    case CoreType.Boolean: return static () => new JsonConverterBoolean();
                    case CoreType.Byte: return static () => new JsonConverterByte();
                    case CoreType.SByte: return static () => new JsonConverterSByte();
                    case CoreType.Int16: return static () => new JsonConverterInt16();
                    case CoreType.UInt16: return static () => new JsonConverterUInt16();
                    case CoreType.Int32: return static () => new JsonConverterInt32();
                    case CoreType.UInt32: return static () => new JsonConverterUInt32();
                    case CoreType.Int64: return static () => new JsonConverterInt64();
                    case CoreType.UInt64: return static () => new JsonConverterUInt64();
                    case CoreType.Single: return static () => new JsonConverterSingle();
                    case CoreType.Double: return static () => new JsonConverterDouble();
                    case CoreType.Decimal: return static () => new JsonConverterDecimal();
                    case CoreType.Char: return static () => new JsonConverterChar();
                    case CoreType.DateTime: return static () => new JsonConverterDateTime();
                    case CoreType.DateTimeOffset: return static () => new JsonConverterDateTimeOffset();
                    case CoreType.TimeSpan: return static () => new JsonConverterTimeSpan();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnly: return static () => new JsonConverterDateOnly();
                    case CoreType.TimeOnly: return static () => new JsonConverterTimeOnly();
#endif
                    case CoreType.Guid: return static () => new JsonConverterGuid();
                    case CoreType.BooleanNullable: return static () => new JsonConverterBooleanNullable();
                    case CoreType.ByteNullable: return static () => new JsonConverterByteNullable();
                    case CoreType.SByteNullable: return static () => new JsonConverterSByteNullable();
                    case CoreType.Int16Nullable: return static () => new JsonConverterInt16Nullable();
                    case CoreType.UInt16Nullable: return static () => new JsonConverterUInt16Nullable();
                    case CoreType.Int32Nullable: return static () => new JsonConverterInt32Nullable();
                    case CoreType.UInt32Nullable: return static () => new JsonConverterUInt32Nullable();
                    case CoreType.Int64Nullable: return static () => new JsonConverterInt64Nullable();
                    case CoreType.UInt64Nullable: return static () => new JsonConverterUInt64Nullable();
                    case CoreType.SingleNullable: return static () => new JsonConverterSingleNullable();
                    case CoreType.DoubleNullable: return static () => new JsonConverterDoubleNullable();
                    case CoreType.DecimalNullable: return static () => new JsonConverterDecimalNullable();
                    case CoreType.CharNullable: return static () => new JsonConverterCharNullable();
                    case CoreType.DateTimeNullable: return static () => new JsonConverterDateTimeNullable();
                    case CoreType.DateTimeOffsetNullable: return static () => new JsonConverterDateTimeOffsetNullable();
                    case CoreType.TimeSpanNullable: return static () => new JsonConverterTimeSpanNullable();
#if NET6_0_OR_GREATER
                    case CoreType.DateOnlyNullable: return static () => new JsonConverterDateOnlyNullable();
                    case CoreType.TimeOnlyNullable: return static () => new JsonConverterTimeOnlyNullable();
#endif
                    case CoreType.GuidNullable: return static () => new JsonConverterGuidNullable();
                    case CoreType.String: return static () => new JsonConverterString();
                }
            }

            if (typeDetail.HasIEnumerableGeneric && typeDetail.IEnumerableGenericInnerTypeDetail!.CoreType.HasValue)
            {
                if (typeDetail.Type.IsArray)
                {
                    if (typeDetail.IEnumerableGenericInnerTypeDetail.CoreType.Value == CoreType.Byte)
                        return static () => new JsonConverterByteArray();
                }
            }

            if (typeDetail.IsIList)
                return static () => new JsonConverterIList();
            if (typeDetail.IsIListGeneric)
                return static () => new JsonConverterIListT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyListGeneric)
                return static () => new JsonConverterIReadOnlyListT<TEnumerableType>();
            if (typeDetail.IsListGeneric)
                return static () => new JsonConverterListT<TEnumerableType>();

            if (typeDetail.IsISetGeneric)
                return static () => new JsonConverterISetT<TEnumerableType>();
            if (typeDetail.IsIReadOnlySetGeneric)
                return static () => new JsonConverterIReadOnlySetT<TEnumerableType>();
            if (typeDetail.IsHashSetGeneric)
                return static () => new JsonConverterHashSetT<TEnumerableType>();

            if (typeDetail.IsICollection)
                return static () => new JsonConverterICollection();
            if (typeDetail.IsICollectionGeneric)
                return static () => new JsonConverterICollectionT<TEnumerableType>();
            if (typeDetail.IsIReadOnlyCollectionGeneric)
                return static () => new JsonConverterIReadOnlyCollectionT<TEnumerableType>();

            if (typeDetail.IsIDictionary)
                return static () => new JsonConverterIDictionary();
            if (typeDetail.IsIDictionaryGeneric)
                return static () => new JsonConverterIDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsIReadOnlyDictionaryGeneric)
                return static () => new JsonConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>();
            if (typeDetail.IsDictionaryGeneric)
                return static () => new JsonConverterDictionaryT<TDictionaryKey, TDictionaryValue>();

            if (typeDetail.IsIEnumerable)
                return static () => new JsonConverterIEnumerable();
            if (typeDetail.IsIEnumerableGeneric)
                return static () => new JsonConverterIEnumerableT<TEnumerableType>();

            if (typeDetail.Type.FullName == "System.Type")
                return static () => new JsonConverterType();
            if (typeDetail.Type.FullName == "System.Threading.CancellationToken")
                return static () => new JsonConverterCancellationToken();
            if (typeDetail.IsNullable && typeDetail.InnerType!.FullName == "System.Threading.CancellationToken")
                return static () => new JsonConverterCancellationTokenNullable();

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No JsonConverter found to support {typeDetail.Type.Name}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType!.IsEnum)
                return static () => new JsonConverterEnum<TType>();

            //Array
            if (typeDetail.Type.IsArray)
                return static () => new JsonConverterArrayT<TEnumerableType>();

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return static () => new JsonConverterIListTOfT<TType, TEnumerableType>();

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return static () => new JsonConverterIListOfT<TType>();

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return static () => new JsonConverterISetTOfT<TType, TEnumerableType>();

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return static () => new JsonConverterIDictionaryTOfT<TType, TDictionaryKey, TDictionaryValue>();

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return static () => new JsonConverterIDictionaryOfT<TType>();

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return static () => new JsonConverterICollectionTOfT<TType, TEnumerableType>();

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return static () => new JsonConverterIEnumerableTOfT<TType, TEnumerableType>();

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return static () => new JsonConverterIEnumerableOfT<TType>();

            //Object
            return static () => new JsonConverterObject<TType>();
        }
    }
}