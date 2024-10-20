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

                #region CoreTypeArrays
                { "System.Boolean[]", typeof(ByteConverterBooleanArray<>) },
                { "System.Nullable<System.Boolean>[]", typeof(ByteConverterBooleanNullableArray<>) },
                { "System.Byte[]", typeof(ByteConverterByteArray<>) },
                { "System.Nullable<System.Byte>[]", typeof(ByteConverterByteNullableArray<>) },
                { "System.SByte[]", typeof(ByteConverterSByteArray<>) },
                { "System.Nullable<System.SByte>[]", typeof(ByteConverterSByteNullableArray<>) },
                { "System.Int16[]", typeof(ByteConverterInt16Array<>) },
                { "System.Nullable<System.Int16>[]", typeof(ByteConverterInt16NullableArray<>) },
                { "System.UInt16[]", typeof(ByteConverterUInt16Array<>) },
                { "System.Nullable<System.UInt16>[]", typeof(ByteConverterUInt16NullableArray<>) },
                { "System.Int32[]", typeof(ByteConverterInt32Array<>) },
                { "System.Nullable<System.Int32>[]", typeof(ByteConverterInt32NullableArray<>) },
                { "System.UInt32[]", typeof(ByteConverterUInt32Array<>) },
                { "System.Nullable<System.UInt32>[]", typeof(ByteConverterUInt32NullableArray<>) },
                { "System.Int64[]", typeof(ByteConverterInt64Array<>) },
                { "System.Nullable<System.Int64>[]", typeof(ByteConverterInt64NullableArray<>) },
                { "System.UInt64[]", typeof(ByteConverterUInt64Array<>) },
                { "System.Nullable<System.UInt64>[]", typeof(ByteConverterUInt64NullableArray<>) },
                { "System.Single[]", typeof(ByteConverterSingleArray<>) },
                { "System.Nullable<System.Single>[]", typeof(ByteConverterSingleNullableArray<>) },
                { "System.Double[]", typeof(ByteConverterDoubleArray<>) },
                { "System.Nullable<System.Double>[]", typeof(ByteConverterDoubleNullableArray<>) },
                { "System.Decimal[]", typeof(ByteConverterDecimalArray<>) },
                { "System.Nullable<System.Decimal>[]", typeof(ByteConverterDecimalNullableArray<>) },
                { "System.Char[]", typeof(ByteConverterCharArray<>) },
                { "System.Nullable<System.Char>[]", typeof(ByteConverterCharNullableArray<>) },
                { "System.DateTime[]", typeof(ByteConverterDateTimeArray<>) },
                { "System.Nullable<System.DateTime>[]", typeof(ByteConverterDateTimeNullableArray<>) },
                { "System.DateTimeOffset[]", typeof(ByteConverterDateTimeOffsetArray<>) },
                { "System.Nullable<System.DateTimeOffset>[]", typeof(ByteConverterDateTimeOffsetNullableArray<>) },
                { "System.TimeSpan[]", typeof(ByteConverterTimeSpanArray<>) },
                { "System.Nullable<System.TimeSpan>[]", typeof(ByteConverterTimeSpanNullableArray<>) },
#if NET5_0_OR_GREATER
                { "System.DateOnly[]", typeof(ByteConverterDateOnlyArray<>) },
                { "System.Nullable<System.DateOnly>[]", typeof(ByteConverterDateOnlyNullableArray<>) },
                { "System.TimeOnly[]", typeof(ByteConverterTimeOnlyArray<>) },
                { "System.Nullable<System.TimeOnly>[]", typeof(ByteConverterTimeOnlyNullableArray<>) },
#endif
                { "System.Guid[]", typeof(ByteConverterGuidArray<>) },
                { "System.Nullable<System.Guid>[]", typeof(ByteConverterGuidNullableArray<>) },
                //{ "System.String[]", typeof(ByteConverterStringArray<>) },
                #endregion

                #region CoreTypeLists
                { "System.Collections.Generic.List<System.Boolean>", typeof(ByteConverterBooleanList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableList<>) },
                { "System.Collections.Generic.List<System.Byte>", typeof(ByteConverterByteList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableList<>) },
                { "System.Collections.Generic.List<System.SByte>", typeof(ByteConverterSByteList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableList<>) },
                { "System.Collections.Generic.List<System.Int16>", typeof(ByteConverterInt16List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableList<>) },
                { "System.Collections.Generic.List<System.UInt16>", typeof(ByteConverterUInt16List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableList<>) },
                { "System.Collections.Generic.List<System.Int32>", typeof(ByteConverterInt32List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableList<>) },
                { "System.Collections.Generic.List<System.UInt32>", typeof(ByteConverterUInt32List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableList<>) },
                { "System.Collections.Generic.List<System.Int64>", typeof(ByteConverterInt64List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableList<>) },
                { "System.Collections.Generic.List<System.UInt64>", typeof(ByteConverterUInt64List<>) },
                { "System.Collections.Generic.List<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableList<>) },
                { "System.Collections.Generic.List<System.Single>", typeof(ByteConverterSingleList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableList<>) },
                { "System.Collections.Generic.List<System.Double>", typeof(ByteConverterDoubleList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableList<>) },
                { "System.Collections.Generic.List<System.Decimal>", typeof(ByteConverterDecimalList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableList<>) },
                { "System.Collections.Generic.List<System.Char>", typeof(ByteConverterCharList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableList<>) },
                { "System.Collections.Generic.List<System.DateTime>", typeof(ByteConverterDateTimeList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableList<>) },
                { "System.Collections.Generic.List<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableList<>) },
                { "System.Collections.Generic.List<System.TimeSpan>", typeof(ByteConverterTimeSpanList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableList<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.List<System.DateOnly>", typeof(ByteConverterDateOnlyList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableList<>) },
                { "System.Collections.Generic.List<System.TimeOnly>", typeof(ByteConverterTimeOnlyList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableList<>) },
#endif
                { "System.Collections.Generic.List<System.Guid>", typeof(ByteConverterGuidList<>) },
                { "System.Collections.Generic.List<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableList<>) },
                //{ "System.Collections.Generic.List<System.String>", typeof(ByteConverterStringList<>) },
                #endregion

                #region CoreTypeILists
                { "System.Collections.Generic.IList<System.Boolean>", typeof(ByteConverterBooleanIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableIList<>) },
                { "System.Collections.Generic.IList<System.Byte>", typeof(ByteConverterByteIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableIList<>) },
                { "System.Collections.Generic.IList<System.SByte>", typeof(ByteConverterSByteIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableIList<>) },
                { "System.Collections.Generic.IList<System.Int16>", typeof(ByteConverterInt16IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableIList<>) },
                { "System.Collections.Generic.IList<System.UInt16>", typeof(ByteConverterUInt16IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableIList<>) },
                { "System.Collections.Generic.IList<System.Int32>", typeof(ByteConverterInt32IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableIList<>) },
                { "System.Collections.Generic.IList<System.UInt32>", typeof(ByteConverterUInt32IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableIList<>) },
                { "System.Collections.Generic.IList<System.Int64>", typeof(ByteConverterInt64IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableIList<>) },
                { "System.Collections.Generic.IList<System.UInt64>", typeof(ByteConverterUInt64IList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableIList<>) },
                { "System.Collections.Generic.IList<System.Single>", typeof(ByteConverterSingleIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableIList<>) },
                { "System.Collections.Generic.IList<System.Double>", typeof(ByteConverterDoubleIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableIList<>) },
                { "System.Collections.Generic.IList<System.Decimal>", typeof(ByteConverterDecimalIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableIList<>) },
                { "System.Collections.Generic.IList<System.Char>", typeof(ByteConverterCharIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableIList<>) },
                { "System.Collections.Generic.IList<System.DateTime>", typeof(ByteConverterDateTimeIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableIList<>) },
                { "System.Collections.Generic.IList<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableIList<>) },
                { "System.Collections.Generic.IList<System.TimeSpan>", typeof(ByteConverterTimeSpanIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableIList<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IList<System.DateOnly>", typeof(ByteConverterDateOnlyIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableIList<>) },
                { "System.Collections.Generic.IList<System.TimeOnly>", typeof(ByteConverterTimeOnlyIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableIList<>) },
#endif
                { "System.Collections.Generic.IList<System.Guid>", typeof(ByteConverterGuidIList<>) },
                { "System.Collections.Generic.IList<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableIList<>) },
                //{ "System.Collections.Generic.IList<System.String>", typeof(ByteConverterStringIList<>) },
                #endregion

                #region CoreTypeIReadOnlyLists
                { "System.Collections.Generic.IReadOnlyList<System.Boolean>", typeof(ByteConverterBooleanIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Byte>", typeof(ByteConverterByteIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.SByte>", typeof(ByteConverterSByteIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Int16>", typeof(ByteConverterInt16IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.UInt16>", typeof(ByteConverterUInt16IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Int32>", typeof(ByteConverterInt32IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.UInt32>", typeof(ByteConverterUInt32IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Int64>", typeof(ByteConverterInt64IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.UInt64>", typeof(ByteConverterUInt64IReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Single>", typeof(ByteConverterSingleIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Double>", typeof(ByteConverterDoubleIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Decimal>", typeof(ByteConverterDecimalIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Char>", typeof(ByteConverterCharIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.DateTime>", typeof(ByteConverterDateTimeIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.TimeSpan>", typeof(ByteConverterTimeSpanIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableIReadOnlyList<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IReadOnlyList<System.DateOnly>", typeof(ByteConverterDateOnlyIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.TimeOnly>", typeof(ByteConverterTimeOnlyIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableIReadOnlyList<>) },
#endif
                { "System.Collections.Generic.IReadOnlyList<System.Guid>", typeof(ByteConverterGuidIReadOnlyList<>) },
                { "System.Collections.Generic.IReadOnlyList<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableIReadOnlyList<>) },
                //{ "System.Collections.Generic.IReadOnlyList<System.String>", typeof(ByteConverterStringIReadOnlyList<>) },
                #endregion

                #region CoreTypeICollections
                { "System.Collections.Generic.ICollection<System.Boolean>", typeof(ByteConverterBooleanICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Byte>", typeof(ByteConverterByteICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.SByte>", typeof(ByteConverterSByteICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Int16>", typeof(ByteConverterInt16ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.UInt16>", typeof(ByteConverterUInt16ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Int32>", typeof(ByteConverterInt32ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.UInt32>", typeof(ByteConverterUInt32ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Int64>", typeof(ByteConverterInt64ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.UInt64>", typeof(ByteConverterUInt64ICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Single>", typeof(ByteConverterSingleICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Double>", typeof(ByteConverterDoubleICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Decimal>", typeof(ByteConverterDecimalICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.Char>", typeof(ByteConverterCharICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.DateTime>", typeof(ByteConverterDateTimeICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.TimeSpan>", typeof(ByteConverterTimeSpanICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableICollection<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.ICollection<System.DateOnly>", typeof(ByteConverterDateOnlyICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableICollection<>) },
                { "System.Collections.Generic.ICollection<System.TimeOnly>", typeof(ByteConverterTimeOnlyICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableICollection<>) },
#endif
                { "System.Collections.Generic.ICollection<System.Guid>", typeof(ByteConverterGuidICollection<>) },
                { "System.Collections.Generic.ICollection<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableICollection<>) },
                //{ "System.Collections.Generic.ICollection<System.String>", typeof(ByteConverterStringICollection<>) },
                #endregion

                #region CoreTypeIReadOnlyCollections
                { "System.Collections.Generic.IReadOnlyCollection<System.Boolean>", typeof(ByteConverterBooleanIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Byte>", typeof(ByteConverterByteIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.SByte>", typeof(ByteConverterSByteIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Int16>", typeof(ByteConverterInt16IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.UInt16>", typeof(ByteConverterUInt16IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Int32>", typeof(ByteConverterInt32IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.UInt32>", typeof(ByteConverterUInt32IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Int64>", typeof(ByteConverterInt64IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.UInt64>", typeof(ByteConverterUInt64IReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Single>", typeof(ByteConverterSingleIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Double>", typeof(ByteConverterDoubleIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Decimal>", typeof(ByteConverterDecimalIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Char>", typeof(ByteConverterCharIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.DateTime>", typeof(ByteConverterDateTimeIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.TimeSpan>", typeof(ByteConverterTimeSpanIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableIReadOnlyCollection<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IReadOnlyCollection<System.DateOnly>", typeof(ByteConverterDateOnlyIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.TimeOnly>", typeof(ByteConverterTimeOnlyIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableIReadOnlyCollection<>) },
#endif
                { "System.Collections.Generic.IReadOnlyCollection<System.Guid>", typeof(ByteConverterGuidIReadOnlyCollection<>) },
                { "System.Collections.Generic.IReadOnlyCollection<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableIReadOnlyCollection<>) },
                //{ "System.Collections.Generic.IReadOnlyCollection<System.String>", typeof(ByteConverterStringIReadOnlyCollection<>) },
                #endregion

                #region CoreTypeHashSets
                { "System.Collections.Generic.HashSet<System.Boolean>", typeof(ByteConverterBooleanHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Byte>", typeof(ByteConverterByteHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.SByte>", typeof(ByteConverterSByteHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Int16>", typeof(ByteConverterInt16HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.UInt16>", typeof(ByteConverterUInt16HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Int32>", typeof(ByteConverterInt32HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.UInt32>", typeof(ByteConverterUInt32HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Int64>", typeof(ByteConverterInt64HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.UInt64>", typeof(ByteConverterUInt64HashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Single>", typeof(ByteConverterSingleHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Double>", typeof(ByteConverterDoubleHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Decimal>", typeof(ByteConverterDecimalHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Char>", typeof(ByteConverterCharHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.DateTime>", typeof(ByteConverterDateTimeHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.TimeSpan>", typeof(ByteConverterTimeSpanHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableHashSet<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.HashSet<System.DateOnly>", typeof(ByteConverterDateOnlyHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableHashSet<>) },
                { "System.Collections.Generic.HashSet<System.TimeOnly>", typeof(ByteConverterTimeOnlyHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableHashSet<>) },
#endif
                { "System.Collections.Generic.HashSet<System.Guid>", typeof(ByteConverterGuidHashSet<>) },
                { "System.Collections.Generic.HashSet<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableHashSet<>) },
                //{ "System.Collections.Generic.HashSet<System.String>", typeof(ByteConverterStringHashSet<>) },
                #endregion

                #region CoreTypeISets
                { "System.Collections.Generic.ISet<System.Boolean>", typeof(ByteConverterBooleanISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableISet<>) },
                { "System.Collections.Generic.ISet<System.Byte>", typeof(ByteConverterByteISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableISet<>) },
                { "System.Collections.Generic.ISet<System.SByte>", typeof(ByteConverterSByteISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableISet<>) },
                { "System.Collections.Generic.ISet<System.Int16>", typeof(ByteConverterInt16ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableISet<>) },
                { "System.Collections.Generic.ISet<System.UInt16>", typeof(ByteConverterUInt16ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableISet<>) },
                { "System.Collections.Generic.ISet<System.Int32>", typeof(ByteConverterInt32ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableISet<>) },
                { "System.Collections.Generic.ISet<System.UInt32>", typeof(ByteConverterUInt32ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableISet<>) },
                { "System.Collections.Generic.ISet<System.Int64>", typeof(ByteConverterInt64ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableISet<>) },
                { "System.Collections.Generic.ISet<System.UInt64>", typeof(ByteConverterUInt64ISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableISet<>) },
                { "System.Collections.Generic.ISet<System.Single>", typeof(ByteConverterSingleISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableISet<>) },
                { "System.Collections.Generic.ISet<System.Double>", typeof(ByteConverterDoubleISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableISet<>) },
                { "System.Collections.Generic.ISet<System.Decimal>", typeof(ByteConverterDecimalISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableISet<>) },
                { "System.Collections.Generic.ISet<System.Char>", typeof(ByteConverterCharISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableISet<>) },
                { "System.Collections.Generic.ISet<System.DateTime>", typeof(ByteConverterDateTimeISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableISet<>) },
                { "System.Collections.Generic.ISet<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableISet<>) },
                { "System.Collections.Generic.ISet<System.TimeSpan>", typeof(ByteConverterTimeSpanISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableISet<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.ISet<System.DateOnly>", typeof(ByteConverterDateOnlyISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableISet<>) },
                { "System.Collections.Generic.ISet<System.TimeOnly>", typeof(ByteConverterTimeOnlyISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableISet<>) },
#endif
                { "System.Collections.Generic.ISet<System.Guid>", typeof(ByteConverterGuidISet<>) },
                { "System.Collections.Generic.ISet<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableISet<>) },
                //{ "System.Collections.Generic.ISet<System.String>", typeof(ByteConverterStringISet<>) },
                #endregion

                
#if NET5_0_OR_GREATER
                #region CoreTypeIReadOnlySets
                { "System.Collections.Generic.IReadOnlySet<System.Boolean>", typeof(ByteConverterBooleanIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Boolean>>", typeof(ByteConverterBooleanNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Byte>", typeof(ByteConverterByteIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Byte>>", typeof(ByteConverterByteNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.SByte>", typeof(ByteConverterSByteIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.SByte>>", typeof(ByteConverterSByteNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Int16>", typeof(ByteConverterInt16IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int16>>", typeof(ByteConverterInt16NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.UInt16>", typeof(ByteConverterUInt16IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt16>>", typeof(ByteConverterUInt16NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Int32>", typeof(ByteConverterInt32IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int32>>", typeof(ByteConverterInt32NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.UInt32>", typeof(ByteConverterUInt32IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt32>>", typeof(ByteConverterUInt32NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Int64>", typeof(ByteConverterInt64IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Int64>>", typeof(ByteConverterInt64NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.UInt64>", typeof(ByteConverterUInt64IReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.UInt64>>", typeof(ByteConverterUInt64NullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Single>", typeof(ByteConverterSingleIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Single>>", typeof(ByteConverterSingleNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Double>", typeof(ByteConverterDoubleIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Double>>", typeof(ByteConverterDoubleNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Decimal>", typeof(ByteConverterDecimalIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Decimal>>", typeof(ByteConverterDecimalNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Char>", typeof(ByteConverterCharIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Char>>", typeof(ByteConverterCharNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.DateTime>", typeof(ByteConverterDateTimeIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateTime>>", typeof(ByteConverterDateTimeNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.DateTimeOffset>", typeof(ByteConverterDateTimeOffsetIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateTimeOffset>>", typeof(ByteConverterDateTimeOffsetNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.TimeSpan>", typeof(ByteConverterTimeSpanIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.TimeSpan>>", typeof(ByteConverterTimeSpanNullableIReadOnlySet<>) },
#if NET5_0_OR_GREATER
                { "System.Collections.Generic.IReadOnlySet<System.DateOnly>", typeof(ByteConverterDateOnlyIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.DateOnly>>", typeof(ByteConverterDateOnlyNullableIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.TimeOnly>", typeof(ByteConverterTimeOnlyIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.TimeOnly>>", typeof(ByteConverterTimeOnlyNullableIReadOnlySet<>) },
#endif
                { "System.Collections.Generic.IReadOnlySet<System.Guid>", typeof(ByteConverterGuidIReadOnlySet<>) },
                { "System.Collections.Generic.IReadOnlySet<System.Nullable<System.Guid>>", typeof(ByteConverterGuidNullableIReadOnlySet<>) },
                //{ "System.Collections.Generic.IReadOnlySet<System.String>", typeof(ByteConverterStringIReadOnlySet<>) },
                #endregion
#endif

                { "System.Type", typeof(ByteConverterType<>) },
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
    }
}