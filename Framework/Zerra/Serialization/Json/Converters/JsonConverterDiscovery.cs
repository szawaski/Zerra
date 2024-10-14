// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Serialization.Json.Converters.Collections.Collections;
using Zerra.Serialization.Json.Converters.Collections.Dictionaries;
using Zerra.Serialization.Json.Converters.Collections.Enumerables;
using Zerra.Serialization.Json.Converters.Collections.Lists;
using Zerra.Serialization.Json.Converters.Collections.Sets;
using Zerra.Serialization.Json.Converters.CoreTypes.Values;
using Zerra.Serialization.Json.Converters.General;

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
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Boolean>", typeof(JsonConverterBoolean<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Boolean>>", typeof(JsonConverterBooleanNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Byte>", typeof(JsonConverterByte<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Byte>>", typeof(JsonConverterByteNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.SByte>", typeof(JsonConverterSByte<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.SByte>>", typeof(JsonConverterSByteNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Int16>", typeof(JsonConverterInt16<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Int16>>", typeof(JsonConverterInt16Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.UInt16>", typeof(JsonConverterUInt16<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.UInt16>>", typeof(JsonConverterUInt16Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Int32>", typeof(JsonConverterInt32<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Int32>>", typeof(JsonConverterInt32Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.UInt32>", typeof(JsonConverterUInt32<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.UInt32>>", typeof(JsonConverterUInt32Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Int64>", typeof(JsonConverterInt64<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Int64>>", typeof(JsonConverterInt64Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.UInt64>", typeof(JsonConverterUInt64<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.UInt64>>", typeof(JsonConverterUInt64Nullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Single>", typeof(JsonConverterSingle<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Single>>", typeof(JsonConverterSingleNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Double>", typeof(JsonConverterDouble<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Double>>", typeof(JsonConverterDoubleNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Decimal>", typeof(JsonConverterDecimal<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Decimal>>", typeof(JsonConverterDecimalNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Char>", typeof(JsonConverterChar<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Char>>", typeof(JsonConverterCharNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTime>", typeof(JsonConverterDateTime<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.DateTime>>", typeof(JsonConverterDateTimeNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateTimeOffset>", typeof(JsonConverterDateTimeOffset<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.DateTimeOffset>>", typeof(JsonConverterDateTimeOffsetNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeSpan>", typeof(JsonConverterTimeSpan<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.TimeSpan>>", typeof(JsonConverterTimeSpanNullable<>) },
#if NET5_0_OR_GREATER
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.DateOnly>", typeof(JsonConverterDateOnly<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.DateOnly>>", typeof(JsonConverterDateOnlyNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.TimeOnly>", typeof(JsonConverterTimeOnly<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.TimeOnly>>", typeof(JsonConverterTimeOnlyNullable<>) },
#endif
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Guid>", typeof(JsonConverterGuid<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Nullable<System.Guid>>", typeof(JsonConverterGuidNullable<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.String>", typeof(JsonConverterString<>) },
                #endregion

                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Byte[]>", typeof(JsonConverterByteArray<>) },
                { "Zerra.Serialization.Json.Converters.IJsonConverterHandles<System.Type>", typeof(JsonConverterType<>) },
            };

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