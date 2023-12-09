// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class HashSetModel
    {
        public HashSet<bool> BooleanHashSet { get; set; }
        public HashSet<byte> ByteHashSet { get; set; }
        public HashSet<sbyte> SByteHashSet { get; set; }
        public HashSet<short> Int16HashSet { get; set; }
        public HashSet<ushort> UInt16HashSet { get; set; }
        public HashSet<int> Int32HashSet { get; set; }
        public HashSet<uint> UInt32HashSet { get; set; }
        public HashSet<long> Int64HashSet { get; set; }
        public HashSet<ulong> UInt64HashSet { get; set; }
        public HashSet<float> SingleHashSet { get; set; }
        public HashSet<double> DoubleHashSet { get; set; }
        public HashSet<decimal> DecimalHashSet { get; set; }
        public HashSet<char> CharHashSet { get; set; }
        public HashSet<DateTime> DateTimeHashSet { get; set; }
        public HashSet<DateTimeOffset> DateTimeOffsetHashSet { get; set; }
        public HashSet<TimeSpan> TimeSpanHashSet { get; set; }
        public HashSet<Guid> GuidHashSet { get; set; }

        public HashSet<bool?> BooleanHashSetNullable { get; set; }
        public HashSet<byte?> ByteHashSetNullable { get; set; }
        public HashSet<sbyte?> SByteHashSetNullable { get; set; }
        public HashSet<short?> Int16HashSetNullable { get; set; }
        public HashSet<ushort?> UInt16HashSetNullable { get; set; }
        public HashSet<int?> Int32HashSetNullable { get; set; }
        public HashSet<uint?> UInt32HashSetNullable { get; set; }
        public HashSet<long?> Int64HashSetNullable { get; set; }
        public HashSet<ulong?> UInt64HashSetNullable { get; set; }
        public HashSet<float?> SingleHashSetNullable { get; set; }
        public HashSet<double?> DoubleHashSetNullable { get; set; }
        public HashSet<decimal?> DecimalHashSetNullable { get; set; }
        public HashSet<char?> CharHashSetNullable { get; set; }
        public HashSet<DateTime?> DateTimeHashSetNullable { get; set; }
        public HashSet<DateTimeOffset?> DateTimeOffsetHashSetNullable { get; set; }
        public HashSet<TimeSpan?> TimeSpanHashSetNullable { get; set; }
        public HashSet<Guid?> GuidHashSetNullable { get; set; }

        public HashSet<string> StringHashSet { get; set; }
    }
}
