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
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.ICollection>", typeof(ByteConverterICollection<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.ICollection<T>>", typeof(ByteConverterICollectionT<,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IReadOnlyCollection<T>>", typeof(ByteConverterIReadOnlyCollectionT<,>) },
                #endregion

                #region Dictionaries
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.Dictionary<T,T>>", typeof(ByteConverterDictionaryT<,,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.IDictionary>", typeof(ByteConverterIDictionary<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IDictionary<T,T>>", typeof(ByteConverterIDictionaryT<,,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IReadOnlyDictionary<T,T>>", typeof(ByteConverterIReadOnlyDictionaryT<,,>) },
                #endregion

                #region Enumerables
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.IEnumerable>", typeof(ByteConverterIEnumerable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IEnumerable<T>>", typeof(ByteConverterIEnumerableT<,>) },
                #endregion

                #region Lists
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.IList>", typeof(ByteConverterIList<,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IList<T>>", typeof(ByteConverterIListT<,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IReadOnlyList<T>>", typeof(ByteConverterIReadOnlyListT<,>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.List<T>>", typeof(ByteConverterListT<,>) },
                #endregion

                #region Sets
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.HashSet<T>>", typeof(ByteConverterHashSetT<,>) },
#if NET5_0_OR_GREATER
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.IReadOnlySet<T>>", typeof(ByteConverterIReadOnlySetT<,>) },
#endif
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Collections.Generic.ISet<T>>", typeof(ByteConverterISetT<,>) },
                #endregion

                #region CoreTypeValues
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Boolean>", typeof(ByteConverterBoolean<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Byte>", typeof(ByteConverterByte<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.SByte>", typeof(ByteConverterSByte<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Int16>", typeof(ByteConverterInt16<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Int16>>", typeof(ByteConverterInt16Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.UInt16>", typeof(ByteConverterUInt16<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Int32>", typeof(ByteConverterInt32<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Int32>>", typeof(ByteConverterInt32Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.UInt32>", typeof(ByteConverterUInt32<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Int64>", typeof(ByteConverterInt64<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Int64>>", typeof(ByteConverterInt64Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.UInt64>", typeof(ByteConverterUInt64<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64Nullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Single>", typeof(ByteConverterSingle<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Double>", typeof(ByteConverterDouble<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Decimal>", typeof(ByteConverterDecimal<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Char>", typeof(ByteConverterChar<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Char>>", typeof(ByteConverterCharNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.DateTime>", typeof(ByteConverterDateTime<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffset<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.TimeSpan>", typeof(ByteConverterTimeSpan<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullable<>) },
#if NET5_0_OR_GREATER
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.DateOnly>", typeof(ByteConverterDateOnly<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.TimeOnly>", typeof(ByteConverterTimeOnly<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullable<>) },
#endif
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Guid>", typeof(ByteConverterGuid<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullable<>) },
                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.String>", typeof(ByteConverterString<>) },
                #endregion

                { "Zerra.Serialization.Bytes.Converters.IByteConverterHandles<System.Type>", typeof(ByteConverterType<>) },
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