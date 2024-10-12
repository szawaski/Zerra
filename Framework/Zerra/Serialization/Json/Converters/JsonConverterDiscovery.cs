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
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.ICollection>", typeof(JsonConverterICollection<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.ICollection<T>>", typeof(JsonConverterICollectionT<,>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IReadOnlyCollection<T>>", typeof(JsonConverterIReadOnlyCollectionT<,>) },
                #endregion

                #region Dictionaries
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.Dictionary<T,T>>", typeof(JsonConverterDictionaryT<,,>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.IDictionary>", typeof(JsonConverterIDictionary<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IDictionary<T,T>>", typeof(JsonConverterIDictionaryT<,,>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IReadOnlyDictionary<T,T>>", typeof(JsonConverterIReadOnlyDictionaryT<,,>) },
                #endregion

                #region Enumerables
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.IEnumerable>", typeof(JsonConverterIEnumerable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IEnumerable<T>>", typeof(JsonConverterIEnumerableT<,>) },
                #endregion

                #region Lists
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.IList>", typeof(JsonConverterIList<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IList<T>>", typeof(JsonConverterIListT<,>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IReadOnlyList<T>>", typeof(JsonConverterIReadOnlyListT<,>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.List<T>>", typeof(JsonConverterListT<,>) },
                #endregion

                #region Sets
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.HashSet<T>>", typeof(JsonConverterHashSetT<,>) },
#if NET5_0_OR_GREATER
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.IReadOnlySet<T>>", typeof(JsonConverterIReadOnlySetT<,>) },
#endif
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Collections.Generic.ISet<T>>", typeof(JsonConverterISetT<,>) },
                #endregion

                #region CoreTypeValues
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<bool>", typeof(JsonConverterBoolean<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<bool?>", typeof(JsonConverterBooleanNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<byte>", typeof(JsonConverterByte<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<byte?>", typeof(JsonConverterByteNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<sbyte>", typeof(JsonConverterSByte<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<sbyte?>", typeof(JsonConverterSByteNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<short>", typeof(JsonConverterInt16<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<short?>", typeof(JsonConverterInt16Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<ushort>", typeof(JsonConverterUInt16<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<ushort?>", typeof(JsonConverterUInt16Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<int>", typeof(JsonConverterInt32<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<int?>", typeof(JsonConverterInt32Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<uint>", typeof(JsonConverterUInt32<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<uint?>", typeof(JsonConverterUInt32Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<long>", typeof(JsonConverterInt64<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<long?>", typeof(JsonConverterInt64Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<ulong>", typeof(JsonConverterUInt64<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<ulong?>", typeof(JsonConverterUInt64Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<float>", typeof(JsonConverterSingle<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<float?>", typeof(JsonConverterSingleNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<double>", typeof(JsonConverterDouble<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<double?>", typeof(JsonConverterDoubleNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<decimal>", typeof(JsonConverterDecimal<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<decimal?>", typeof(JsonConverterDecimalNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<char>", typeof(JsonConverterChar<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<char?>", typeof(JsonConverterCharNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTime>", typeof(JsonConverterDateTime<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTime?>", typeof(JsonConverterDateTimeNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTimeOffset>", typeof(JsonConverterDateTimeOffset<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTimeOffset?>", typeof(JsonConverterDateTimeOffsetNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeSpan>", typeof(JsonConverterTimeSpan<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeSpan?>", typeof(JsonConverterTimeSpanNullable<>) },
#if NET5_0_OR_GREATER
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateOnly>", typeof(JsonConverterDateOnly<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateOnly?>", typeof(JsonConverterDateOnlyNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeOnly>", typeof(JsonConverterTimeOnly<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeOnly?>", typeof(JsonConverterTimeOnlyNullable<>) },
#endif
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Guid>", typeof(JsonConverterGuid<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Guid?>", typeof(JsonConverterGuidNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.String>", typeof(JsonConverterString<>) }
            };
            #endregion
        }

        public static Type? Discover(Type interfaceType)
        {
            var name = Discovery.GetNiceFullName(interfaceType);
            if (!typeByInterfaceName.TryGetValue(name, out var type))
                return null;
            return type;
        }
    }
}