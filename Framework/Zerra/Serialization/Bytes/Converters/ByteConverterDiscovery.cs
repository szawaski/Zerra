// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.Converters.Collections.Collections;
using Zerra.Serialization.Bytes.Converters.Collections.Dictionaries;
using Zerra.Serialization.Bytes.Converters.Collections.Enumerables;
using Zerra.Serialization.Bytes.Converters.Collections.Lists;
using Zerra.Serialization.Bytes.Converters.Collections.Sets;
using Zerra.Serialization.Bytes.Converters.CoreTypes.Values;
using Zerra.Serialization.Bytes.Converters.General;

namespace Zerra.Serialization.Bytes.Converters
{
    internal static class ByteConverterDiscovery
    {
        private static readonly Dictionary<string, Type> typeByInterfaceName;

        static ByteConverterDiscovery()
        {
            typeByInterfaceName = new()
            {
                #region Collections
                { "System.Collections.ICollection", typeof(ByteConverterICollection<>) },
                { "System.Collections.Generic.ICollection<T>", typeof(ByteConverterICollectionT<,>) },
                { "System.Collections.Generic.IReadOnlyCollection<T>", typeof(ByteConverterIReadOnlyCollectionT<,>) },
                #endregion

                #region Dictionaries
                { "System.Collections.Generic.Dictionary<T,T>", typeof(ByteConverterDictionaryT<,,>) },
                { "System.Collections.IDictionary", typeof(ByteConverterIDictionary<>) },
                { "System.Collections.Generic.IDictionary<T,T>", typeof(ByteConverterIDictionaryT<,,>) },
                { "System.Collections.Generic.IReadOnlyDictionary<T,T>", typeof(ByteConverterIReadOnlyDictionaryT<,,>) },
                #endregion

                #region Enumerables
                { "System.Collections.IEnumerable", typeof(ByteConverterIEnumerable<>) },
                { "System.Collections.Generic.IEnumerable<T>", typeof(ByteConverterIEnumerableT<,>) },
                #endregion

                #region Lists
                { "System.Collections.IList", typeof(ByteConverterIList<,>) },
                { "System.Collections.Generic.IList<T>", typeof(ByteConverterIListT<,>) },
                { "System.Collections.Generic.IReadOnlyList<T>", typeof(ByteConverterIReadOnlyListT<,>) },
                { "System.Collections.Generic.List<T>", typeof(ByteConverterListT<,>) },
                #endregion

                #region Sets
                { "System.Collections.Generic.HashSet<T>", typeof(ByteConverterHashSetT<,>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IReadOnlySet<T>", typeof(ByteConverterIReadOnlySetT<,>) },
#endif
                { "System.Collections.Generic.ISet<T>", typeof(ByteConverterISetT<,>) },
                #endregion

                #region CoreTypeValues
                { "System.Boolean", typeof(ByteConverterBoolean<>) },
                { "System.Nullable<System.Boolean>", typeof(ByteConverterBooleanNullable<>) },
                { "System.Byte", typeof(ByteConverterByte<>) },
                { "System.Nullable<System.Byte>", typeof(ByteConverterByteNullable<>) },
                { "System.SByte", typeof(ByteConverterSByte<>) },
                { "System.Nullable<System.SByte>", typeof(ByteConverterSByteNullable<>) },
                { "System.Int16", typeof(ByteConverterInt16<>) },
                { "System.Nullable<System.Int16>", typeof(ByteConverterInt16Nullable<>) },
                { "System.UInt16", typeof(ByteConverterUInt16<>) },
                { "System.Nullable<System.UInt16>", typeof(ByteConverterUInt16Nullable<>) },
                { "System.Int32", typeof(ByteConverterInt32<>) },
                { "System.Nullable<System.Int32>", typeof(ByteConverterInt32Nullable<>) },
                { "System.UInt32", typeof(ByteConverterUInt32<>) },
                { "System.Nullable<System.UInt32>", typeof(ByteConverterUInt32Nullable<>) },
                { "System.Int64", typeof(ByteConverterInt64<>) },
                { "System.Nullable<System.Int64>", typeof(ByteConverterInt64Nullable<>) },
                { "System.UInt64", typeof(ByteConverterUInt64<>) },
                { "System.Nullable<System.UInt64>", typeof(ByteConverterUInt64Nullable<>) },
                { "System.Single", typeof(ByteConverterSingle<>) },
                { "System.Nullable<System.Single>", typeof(ByteConverterSingleNullable<>) },
                { "System.Double", typeof(ByteConverterDouble<>) },
                { "System.Nullable<System.Double>", typeof(ByteConverterDoubleNullable<>) },
                { "System.Decimal", typeof(ByteConverterDecimal<>) },
                { "System.Nullable<System.Decimal>", typeof(ByteConverterDecimalNullable<>) },
                { "System.Char", typeof(ByteConverterChar<>) },
                { "System.Nullable<System.Char>", typeof(ByteConverterCharNullable<>) },
                { "System.DateTime", typeof(ByteConverterDateTime<>) },
                { "System.Nullable<System.DateTime>", typeof(ByteConverterDateTimeNullable<>) },
                { "System.DateTimeOffset", typeof(ByteConverterDateTimeOffset<>) },
                { "System.Nullable<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetNullable<>) },
                { "System.TimeSpan", typeof(ByteConverterTimeSpan<>) },
                { "System.Nullable<System.TimeSpan>", typeof(ByteConverterTimeSpanNullable<>) },
#if NET5_0_OR_GREATER
                { "System.DateOnly", typeof(ByteConverterDateOnly<>) },
                { "System.Nullable<System.DateOnly>", typeof(ByteConverterDateOnlyNullable<>) },
                { "System.TimeOnly", typeof(ByteConverterTimeOnly<>) },
                { "System.Nullable<System.TimeOnly>", typeof(ByteConverterTimeOnlyNullable<>) },
#endif
                { "System.Guid", typeof(ByteConverterGuid<>) },
                { "System.Nullable<System.Guid>", typeof(ByteConverterGuidNullable<>) },
                { "System.String", typeof(ByteConverterString<>) },
                #endregion

                { "System.Type", typeof(ByteConverterType<>) },
            };
        }

        public static Type? Discover(Type type)
        {
            var name = Discovery.GetNiceFullName(type);
            if (!typeByInterfaceName.TryGetValue(name, out var converterType))
                return null;
            return converterType;
        }
    }
}