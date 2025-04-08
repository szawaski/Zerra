// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
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
    internal static class JsonConverterDiscovery
    {
        private static readonly Dictionary<string, Type> typeByInterfaceName;

        static JsonConverterDiscovery()
        {
            typeByInterfaceName = new()
            {
                #region Collections
                { "System.Collections.ICollection", typeof(JsonConverterICollection<>) },
                { "System.Collections.Generic.ICollection<T>", typeof(JsonConverterICollectionT<,>) },
                { "System.Collections.Generic.IReadOnlyCollection<T>", typeof(JsonConverterIReadOnlyCollectionT<,>) },
                #endregion

                #region Dictionaries
                { "System.Collections.Generic.Dictionary<T,T>", typeof(JsonConverterDictionaryT<,,>) },
                { "System.Collections.IDictionary", typeof(JsonConverterIDictionary<>) },
                { "System.Collections.Generic.IDictionary<T,T>", typeof(JsonConverterIDictionaryT<,,>) },
                { "System.Collections.Generic.IReadOnlyDictionary<T,T>", typeof(JsonConverterIReadOnlyDictionaryT<,,>) },
                #endregion

                #region Enumerables
                { "System.Collections.IEnumerable", typeof(JsonConverterIEnumerable<>) },
                { "System.Collections.Generic.IEnumerable<T>", typeof(JsonConverterIEnumerableT<,>) },
                #endregion

                #region Lists
                { "System.Collections.IList", typeof(JsonConverterIList<>) },
                { "System.Collections.Generic.IList<T>", typeof(JsonConverterIListT<,>) },
                { "System.Collections.Generic.IReadOnlyList<T>", typeof(JsonConverterIReadOnlyListT<,>) },
                { "System.Collections.Generic.List<T>", typeof(JsonConverterListT<,>) },
                #endregion

                #region Sets
                { "System.Collections.Generic.HashSet<T>", typeof(JsonConverterHashSetT<,>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IReadOnlySet<T>", typeof(JsonConverterIReadOnlySetT<,>) },
#endif
                { "System.Collections.Generic.ISet<T>", typeof(JsonConverterISetT<,>) },
                #endregion

                #region CoreTypeValues
                { "System.Boolean", typeof(JsonConverterBoolean<>) },
                { "System.Nullable<System.Boolean>", typeof(JsonConverterBooleanNullable<>) },
                { "System.Byte", typeof(JsonConverterByte<>) },
                { "System.Nullable<System.Byte>", typeof(JsonConverterByteNullable<>) },
                { "System.SByte", typeof(JsonConverterSByte<>) },
                { "System.Nullable<System.SByte>", typeof(JsonConverterSByteNullable<>) },
                { "System.Int16", typeof(JsonConverterInt16<>) },
                { "System.Nullable<System.Int16>", typeof(JsonConverterInt16Nullable<>) },
                { "System.UInt16", typeof(JsonConverterUInt16<>) },
                { "System.Nullable<System.UInt16>", typeof(JsonConverterUInt16Nullable<>) },
                { "System.Int32", typeof(JsonConverterInt32<>) },
                { "System.Nullable<System.Int32>", typeof(JsonConverterInt32Nullable<>) },
                { "System.UInt32", typeof(JsonConverterUInt32<>) },
                { "System.Nullable<System.UInt32>", typeof(JsonConverterUInt32Nullable<>) },
                { "System.Int64", typeof(JsonConverterInt64<>) },
                { "System.Nullable<System.Int64>", typeof(JsonConverterInt64Nullable<>) },
                { "System.UInt64", typeof(JsonConverterUInt64<>) },
                { "System.Nullable<System.UInt64>", typeof(JsonConverterUInt64Nullable<>) },
                { "System.Single", typeof(JsonConverterSingle<>) },
                { "System.Nullable<System.Single>", typeof(JsonConverterSingleNullable<>) },
                { "System.Double", typeof(JsonConverterDouble<>) },
                { "System.Nullable<System.Double>", typeof(JsonConverterDoubleNullable<>) },
                { "System.Decimal", typeof(JsonConverterDecimal<>) },
                { "System.Nullable<System.Decimal>", typeof(JsonConverterDecimalNullable<>) },
                { "System.Char", typeof(JsonConverterChar<>) },
                { "System.Nullable<System.Char>", typeof(JsonConverterCharNullable<>) },
                { "System.DateTime", typeof(JsonConverterDateTime<>) },
                { "System.Nullable<System.DateTime>", typeof(JsonConverterDateTimeNullable<>) },
                { "System.DateTimeOffset", typeof(JsonConverterDateTimeOffset<>) },
                { "System.Nullable<System.DateTimeOffset>", typeof(JsonConverterDateTimeOffsetNullable<>) },
                { "System.TimeSpan", typeof(JsonConverterTimeSpan<>) },
                { "System.Nullable<System.TimeSpan>", typeof(JsonConverterTimeSpanNullable<>) },
#if NET5_0_OR_GREATER
                { "System.DateOnly", typeof(JsonConverterDateOnly<>) },
                { "System.Nullable<System.DateOnly>", typeof(JsonConverterDateOnlyNullable<>) },
                { "System.TimeOnly", typeof(JsonConverterTimeOnly<>) },
                { "System.Nullable<System.TimeOnly>", typeof(JsonConverterTimeOnlyNullable<>) },
#endif
                { "System.Guid", typeof(JsonConverterGuid<>) },
                { "System.Nullable<System.Guid>", typeof(JsonConverterGuidNullable<>) },
                { "System.String", typeof(JsonConverterString<>) },
                #endregion

                { "System.Type", typeof(JsonConverterType<>) },

                { "System.Byte[]", typeof(JsonConverterByteArray<>) },
                { "System.Threading.CancellationToken", typeof(JsonConverterCancellationToken<>) },
                { "System.Nullable<System.Threading.CancellationToken>", typeof(JsonConverterCancellationTokenNullable<>) },
            };

        }

        public static Type? Discover(Type type)
        {
            var name = Discovery.GetNiceFullName(type);
            if (!typeByInterfaceName.TryGetValue(name, out var converterType))
            {
                if (!type.IsGenericType || type.Name == "Nullable`1")
                    return null;
                name = Discovery.MakeNiceNameGeneric(name);
                if (!typeByInterfaceName.TryGetValue(name, out converterType))
                    return null;
            }
            return converterType;
        }

        private static readonly string converterBaseTypeName = typeof(JsonConverter<,>).Name;
        public static void AddConverter(Type converterType, Type valueType)
        { 
            if (!converterType.IsGenericType)
                throw new ArgumentException("Cannot add converter because it must be a genertic type");
            if (converterType.GetGenericArguments().Length > 4)
                throw new ArgumentException("Cannot add converter because it has more than four generic arguments");

            var baseType = converterType.BaseType;
            if (baseType is null)
                throw new ArgumentException($"Cannot add converter because the must inherit {nameof(JsonConverter)}<T>");
            while (baseType.Name == converterBaseTypeName)
            {
                if (baseType.BaseType is null)
                    throw new ArgumentException($"Cannot add converter because the Converter must inherit {nameof(JsonConverter)}<T>");
                baseType = baseType.BaseType;
            }

            var name = Discovery.GetNiceFullName(valueType);
            typeByInterfaceName[name] = converterType;
        }
    }
}