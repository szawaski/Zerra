// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Zerra.Reflection.Compiletime
{
    internal class TypesToGenerate
    {
        public object Object { get; set; }

        #region Generics

        public IList<object> IListMakeUnbounded { get; set; }
        public ICollection<object> ICollectionMakeUnbounded { get; set; }
        public IEnumerable<object> IEnumerableMakeUnbounded { get; set; }

        #endregion

        #region Non-Generics

        public IList IList { get; set; }
        public ICollection ICollection { get; set; }
        public IEnumerable IEnumerable { get; set; }
        public IDictionary IDictionary { get; set; }

        #endregion

        #region CoreTypes

        public bool BooleanThing { get; set; }
        public byte ByteThing { get; set; }
        public sbyte SByteThing { get; set; }
        public short Int16Thing { get; set; }
        public ushort UInt16Thing { get; set; }
        public int Int32Thing { get; set; }
        public uint UInt32Thing { get; set; }
        public long Int64Thing { get; set; }
        public ulong UInt64Thing { get; set; }
        public float SingleThing { get; set; }
        public double DoubleThing { get; set; }
        public decimal DecimalThing { get; set; }
        public char CharThing { get; set; }
        public DateTime DateTimeThing { get; set; }
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        public TimeSpan TimeSpanThing { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly DateOnlyThing { get; set; }
        public TimeOnly TimeOnlyThing { get; set; }
#endif
        public Guid GuidThing { get; set; }

        public bool? BooleanThingNullable { get; set; }
        public byte? ByteThingNullable { get; set; }
        public sbyte? SByteThingNullable { get; set; }
        public short? Int16ThingNullable { get; set; }
        public ushort? UInt16ThingNullable { get; set; }
        public int? Int32ThingNullable { get; set; }
        public uint? UInt32ThingNullable { get; set; }
        public long? Int64ThingNullable { get; set; }
        public ulong? UInt64ThingNullable { get; set; }
        public float? SingleThingNullable { get; set; }
        public double? DoubleThingNullable { get; set; }
        public decimal? DecimalThingNullable { get; set; }
        public char? CharThingNullable { get; set; }
        public DateTime? DateTimeThingNullable { get; set; }
        public DateTimeOffset? DateTimeOffsetThingNullable { get; set; }
        public TimeSpan? TimeSpanThingNullable { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly? DateOnlyThingNullable { get; set; }
        public TimeOnly? TimeOnlyThingNullable { get; set; }
#endif
        public Guid? GuidThingNullable { get; set; }

        public string StringThing { get; set; }

        #endregion

        #region Arrays

        public bool[] BooleanArray { get; set; }
        public byte[] ByteArray { get; set; }
        public sbyte[] SByteArray { get; set; }
        public short[] Int16Array { get; set; }
        public ushort[] UInt16Array { get; set; }
        public int[] Int32Array { get; set; }
        public uint[] UInt32Array { get; set; }
        public long[] Int64Array { get; set; }
        public ulong[] UInt64Array { get; set; }
        public float[] SingleArray { get; set; }
        public double[] DoubleArray { get; set; }
        public decimal[] DecimalArray { get; set; }
        public char[] CharArray { get; set; }
        public DateTime[] DateTimeArray { get; set; }
        public DateTimeOffset[] DateTimeOffsetArray { get; set; }
        public TimeSpan[] TimeSpanArray { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly[] DateOnlyArray { get; set; }
        public TimeOnly[] TimeOnlyArray { get; set; }
#endif
        public Guid[] GuidArray { get; set; }      

        public bool?[] BooleanArrayNullable { get; set; }
        public byte?[] ByteArrayNullable { get; set; }
        public sbyte?[] SByteArrayNullable { get; set; }
        public short?[] Int16ArrayNullable { get; set; }
        public ushort?[] UInt16ArrayNullable { get; set; }
        public int?[] Int32ArrayNullable { get; set; }
        public uint?[] UInt32ArrayNullable { get; set; }
        public long?[] Int64ArrayNullable { get; set; }
        public ulong?[] UInt64ArrayNullable { get; set; }
        public float?[] SingleArrayNullable { get; set; }
        public double?[] DoubleArrayNullable { get; set; }
        public decimal?[] DecimalArrayNullable { get; set; }
        public char?[] CharArrayNullable { get; set; }
        public DateTime?[] DateTimeArrayNullable { get; set; }
        public DateTimeOffset?[] DateTimeOffsetArrayNullable { get; set; }
        public TimeSpan?[] TimeSpanArrayNullable { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly?[] DateOnlyArrayNullable { get; set; }
        public TimeOnly?[] TimeOnlyArrayNullable { get; set; }
#endif
        public Guid?[] GuidArrayNullable { get; set; }

        public string[] StringArray { get; set; }

        #endregion

        #region ListTs

        public List<bool> BooleanListT { get; set; }
        public List<byte> ByteListT { get; set; }
        public List<sbyte> SByteListT { get; set; }
        public List<short> Int16ListT { get; set; }
        public List<ushort> UInt16ListT { get; set; }
        public List<int> Int32ListT { get; set; }
        public List<uint> UInt32ListT { get; set; }
        public List<long> Int64ListT { get; set; }
        public List<ulong> UInt64ListT { get; set; }
        public List<float> SingleListT { get; set; }
        public List<double> DoubleListT { get; set; }
        public List<decimal> DecimalListT { get; set; }
        public List<char> CharListT { get; set; }
        public List<DateTime> DateTimeListT { get; set; }
        public List<DateTimeOffset> DateTimeOffsetListT { get; set; }
        public List<TimeSpan> TimeSpanListT { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly> DateOnlyListT { get; set; }
        public List<TimeOnly> TimeOnlyListT { get; set; }
#endif
        public List<Guid> GuidListT { get; set; }

        public List<bool?> BooleanListTNullable { get; set; }
        public List<byte?> ByteListTNullable { get; set; }
        public List<sbyte?> SByteListTNullable { get; set; }
        public List<short?> Int16ListTNullable { get; set; }
        public List<ushort?> UInt16ListTNullable { get; set; }
        public List<int?> Int32ListTNullable { get; set; }
        public List<uint?> UInt32ListTNullable { get; set; }
        public List<long?> Int64ListTNullable { get; set; }
        public List<ulong?> UInt64ListTNullable { get; set; }
        public List<float?> SingleListTNullable { get; set; }
        public List<double?> DoubleListTNullable { get; set; }
        public List<decimal?> DecimalListTNullable { get; set; }
        public List<char?> CharListTNullable { get; set; }
        public List<DateTime?> DateTimeListTNullable { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListTNullable { get; set; }
        public List<TimeSpan?> TimeSpanListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly?> DateOnlyListTNullable { get; set; }
        public List<TimeOnly?> TimeOnlyListTNullable { get; set; }
#endif
        public List<Guid?> GuidListTNullable { get; set; }

        public List<string> StringListT { get; set; }

        #endregion

        #region IListTs

        public IList<bool> BooleanIListT { get; set; }
        public IList<byte> ByteIListT { get; set; }
        public IList<sbyte> SByteIListT { get; set; }
        public IList<short> Int16IListT { get; set; }
        public IList<ushort> UInt16IListT { get; set; }
        public IList<int> Int32IListT { get; set; }
        public IList<uint> UInt32IListT { get; set; }
        public IList<long> Int64IListT { get; set; }
        public IList<ulong> UInt64IListT { get; set; }
        public IList<float> SingleIListT { get; set; }
        public IList<double> DoubleIListT { get; set; }
        public IList<decimal> DecimalIListT { get; set; }
        public IList<char> CharIListT { get; set; }
        public IList<DateTime> DateTimeIListT { get; set; }
        public IList<DateTimeOffset> DateTimeOffsetIListT { get; set; }
        public IList<TimeSpan> TimeSpanIListT { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly> DateOnlyIListT { get; set; }
        public IList<TimeOnly> TimeOnlyIListT { get; set; }
#endif
        public IList<Guid> GuidIListT { get; set; }

        public IList<bool?> BooleanIListTNullable { get; set; }
        public IList<byte?> ByteIListTNullable { get; set; }
        public IList<sbyte?> SByteIListTNullable { get; set; }
        public IList<short?> Int16IListTNullable { get; set; }
        public IList<ushort?> UInt16IListTNullable { get; set; }
        public IList<int?> Int32IListTNullable { get; set; }
        public IList<uint?> UInt32IListTNullable { get; set; }
        public IList<long?> Int64IListTNullable { get; set; }
        public IList<ulong?> UInt64IListTNullable { get; set; }
        public IList<float?> SingleIListTNullable { get; set; }
        public IList<double?> DoubleIListTNullable { get; set; }
        public IList<decimal?> DecimalIListTNullable { get; set; }
        public IList<char?> CharIListTNullable { get; set; }
        public IList<DateTime?> DateTimeIListTNullable { get; set; }
        public IList<DateTimeOffset?> DateTimeOffsetIListTNullable { get; set; }
        public IList<TimeSpan?> TimeSpanIListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly?> DateOnlyIListTNullable { get; set; }
        public IList<TimeOnly?> TimeOnlyIListTNullable { get; set; }
#endif
        public IList<Guid?> GuidIListTNullable { get; set; }

        public IList<string> StringIListT { get; set; }

        #endregion

        #region IReadOnlyListTs

        public IReadOnlyList<bool> BooleanIReadOnlyListT { get; set; }
        public IReadOnlyList<byte> ByteIReadOnlyListT { get; set; }
        public IReadOnlyList<sbyte> SByteIReadOnlyListT { get; set; }
        public IReadOnlyList<short> Int16IReadOnlyListT { get; set; }
        public IReadOnlyList<ushort> UInt16IReadOnlyListT { get; set; }
        public IReadOnlyList<int> Int32IReadOnlyListT { get; set; }
        public IReadOnlyList<uint> UInt32IReadOnlyListT { get; set; }
        public IReadOnlyList<long> Int64IReadOnlyListT { get; set; }
        public IReadOnlyList<ulong> UInt64IReadOnlyListT { get; set; }
        public IReadOnlyList<float> SingleIReadOnlyListT { get; set; }
        public IReadOnlyList<double> DoubleIReadOnlyListT { get; set; }
        public IReadOnlyList<decimal> DecimalIReadOnlyListT { get; set; }
        public IReadOnlyList<char> CharIReadOnlyListT { get; set; }
        public IReadOnlyList<DateTime> DateTimeIReadOnlyListT { get; set; }
        public IReadOnlyList<DateTimeOffset> DateTimeOffsetIReadOnlyListT { get; set; }
        public IReadOnlyList<TimeSpan> TimeSpanIReadOnlyListT { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly> DateOnlyIReadOnlyListT { get; set; }
        public IReadOnlyList<TimeOnly> TimeOnlyIReadOnlyListT { get; set; }
#endif
        public IReadOnlyList<Guid> GuidIReadOnlyListT { get; set; }

        public IReadOnlyList<bool?> BooleanIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<byte?> ByteIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<sbyte?> SByteIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<short?> Int16IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<ushort?> UInt16IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<int?> Int32IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<uint?> UInt32IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<long?> Int64IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<ulong?> UInt64IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<float?> SingleIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<double?> DoubleIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<decimal?> DecimalIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<char?> CharIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<DateTime?> DateTimeIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<DateTimeOffset?> DateTimeOffsetIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<TimeSpan?> TimeSpanIReadOnlyListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly?> DateOnlyIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<TimeOnly?> TimeOnlyIReadOnlyListTNullable { get; set; }
#endif
        public IReadOnlyList<Guid?> GuidIReadOnlyListTNullable { get; set; }

        public IReadOnlyList<string> StringIReadOnlyListT { get; set; }

        #endregion

        #region ICollectionTs

        public ICollection<bool> BooleanICollectionT { get; set; }
        public ICollection<byte> ByteICollectionT { get; set; }
        public ICollection<sbyte> SByteICollectionT { get; set; }
        public ICollection<short> Int16ICollectionT { get; set; }
        public ICollection<ushort> UInt16ICollectionT { get; set; }
        public ICollection<int> Int32ICollectionT { get; set; }
        public ICollection<uint> UInt32ICollectionT { get; set; }
        public ICollection<long> Int64ICollectionT { get; set; }
        public ICollection<ulong> UInt64ICollectionT { get; set; }
        public ICollection<float> SingleICollectionT { get; set; }
        public ICollection<double> DoubleICollectionT { get; set; }
        public ICollection<decimal> DecimalICollectionT { get; set; }
        public ICollection<char> CharICollectionT { get; set; }
        public ICollection<DateTime> DateTimeICollectionT { get; set; }
        public ICollection<DateTimeOffset> DateTimeOffsetICollectionT { get; set; }
        public ICollection<TimeSpan> TimeSpanICollectionT { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly> DateOnlyICollectionT { get; set; }
        public ICollection<TimeOnly> TimeOnlyICollectionT { get; set; }
#endif
        public ICollection<Guid> GuidICollectionT { get; set; }

        public ICollection<bool?> BooleanICollectionTNullable { get; set; }
        public ICollection<byte?> ByteICollectionTNullable { get; set; }
        public ICollection<sbyte?> SByteICollectionTNullable { get; set; }
        public ICollection<short?> Int16ICollectionTNullable { get; set; }
        public ICollection<ushort?> UInt16ICollectionTNullable { get; set; }
        public ICollection<int?> Int32ICollectionTNullable { get; set; }
        public ICollection<uint?> UInt32ICollectionTNullable { get; set; }
        public ICollection<long?> Int64ICollectionTNullable { get; set; }
        public ICollection<ulong?> UInt64ICollectionTNullable { get; set; }
        public ICollection<float?> SingleICollectionTNullable { get; set; }
        public ICollection<double?> DoubleICollectionTNullable { get; set; }
        public ICollection<decimal?> DecimalICollectionTNullable { get; set; }
        public ICollection<char?> CharICollectionTNullable { get; set; }
        public ICollection<DateTime?> DateTimeICollectionTNullable { get; set; }
        public ICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullable { get; set; }
        public ICollection<TimeSpan?> TimeSpanICollectionTNullable { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly?> DateOnlyICollectionTNullable { get; set; }
        public ICollection<TimeOnly?> TimeOnlyICollectionTNullable { get; set; }
#endif
        public ICollection<Guid?> GuidICollectionTNullable { get; set; }

        public ICollection<string> StringICollectionT { get; set; }

        #endregion

        #region IReadOnlyCollectionTs

        public IReadOnlyCollection<bool> BooleanIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<byte> ByteIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<sbyte> SByteIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<short> Int16IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<ushort> UInt16IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<int> Int32IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<uint> UInt32IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<long> Int64IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<ulong> UInt64IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<float> SingleIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<double> DoubleIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<decimal> DecimalIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<char> CharIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<DateTime> DateTimeIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<DateTimeOffset> DateTimeOffsetIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<TimeSpan> TimeSpanIReadOnlyCollectionT { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly> DateOnlyIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<TimeOnly> TimeOnlyIReadOnlyCollectionT { get; set; }
#endif
        public IReadOnlyCollection<Guid> GuidIReadOnlyCollectionT { get; set; }

        public IReadOnlyCollection<bool?> BooleanIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<byte?> ByteIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<sbyte?> SByteIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<short?> Int16IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<ushort?> UInt16IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<int?> Int32IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<uint?> UInt32IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<long?> Int64IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<ulong?> UInt64IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<float?> SingleIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<double?> DoubleIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<decimal?> DecimalIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<char?> CharIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<DateTime?> DateTimeIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<DateTimeOffset?> DateTimeOffsetIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<TimeSpan?> TimeSpanIReadOnlyCollectionTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly?> DateOnlyIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<TimeOnly?> TimeOnlyIReadOnlyCollectionTNullable { get; set; }
#endif
        public IReadOnlyCollection<Guid?> GuidIReadOnlyCollectionTNullable { get; set; }        

        public IReadOnlyCollection<string> StringIReadOnlyCollectionT { get; set; }

        #endregion

        #region  HashSet

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
#if NET6_0_OR_GREATER
        public HashSet<DateOnly> DateOnlyHashSet { get; set; }
        public HashSet<TimeOnly> TimeOnlyHashSet { get; set; }
#endif
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
#if NET6_0_OR_GREATER
        public HashSet<DateOnly?> DateOnlyHashSetNullable { get; set; }
        public HashSet<TimeOnly?> TimeOnlyHashSetNullable { get; set; }
#endif
        public HashSet<Guid?> GuidHashSetNullable { get; set; }

        public HashSet<string> StringHashSet { get; set; }

        #endregion

        #region ISetT

        public ISet<bool> BooleanISet { get; set; }
        public ISet<byte> ByteISet { get; set; }
        public ISet<sbyte> SByteISet { get; set; }
        public ISet<short> Int16ISet { get; set; }
        public ISet<ushort> UInt16ISet { get; set; }
        public ISet<int> Int32ISet { get; set; }
        public ISet<uint> UInt32ISet { get; set; }
        public ISet<long> Int64ISet { get; set; }
        public ISet<ulong> UInt64ISet { get; set; }
        public ISet<float> SingleISet { get; set; }
        public ISet<double> DoubleISet { get; set; }
        public ISet<decimal> DecimalISet { get; set; }
        public ISet<char> CharISet { get; set; }
        public ISet<DateTime> DateTimeISet { get; set; }
        public ISet<DateTimeOffset> DateTimeOffsetISet { get; set; }
        public ISet<TimeSpan> TimeSpanISet { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly> DateOnlyISet { get; set; }
        public ISet<TimeOnly> TimeOnlyISet { get; set; }
#endif
        public ISet<Guid> GuidISet { get; set; }

        public ISet<bool> BooleanISetEmpty { get; set; }
        public ISet<byte> ByteISetEmpty { get; set; }
        public ISet<sbyte> SByteISetEmpty { get; set; }
        public ISet<short> Int16ISetEmpty { get; set; }
        public ISet<ushort> UInt16ISetEmpty { get; set; }
        public ISet<int> Int32ISetEmpty { get; set; }
        public ISet<uint> UInt32ISetEmpty { get; set; }
        public ISet<long> Int64ISetEmpty { get; set; }
        public ISet<ulong> UInt64ISetEmpty { get; set; }
        public ISet<float> SingleISetEmpty { get; set; }
        public ISet<double> DoubleISetEmpty { get; set; }
        public ISet<decimal> DecimalISetEmpty { get; set; }
        public ISet<char> CharISetEmpty { get; set; }
        public ISet<DateTime> DateTimeISetEmpty { get; set; }
        public ISet<DateTimeOffset> DateTimeOffsetISetEmpty { get; set; }
        public ISet<TimeSpan> TimeSpanISetEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly> DateOnlyISetEmpty { get; set; }
        public ISet<TimeOnly> TimeOnlyISetEmpty { get; set; }
#endif
        public ISet<Guid> GuidISetEmpty { get; set; }

        public ISet<bool> BooleanISetNull { get; set; }
        public ISet<byte> ByteISetNull { get; set; }
        public ISet<sbyte> SByteISetNull { get; set; }
        public ISet<short> Int16ISetNull { get; set; }
        public ISet<ushort> UInt16ISetNull { get; set; }
        public ISet<int> Int32ISetNull { get; set; }
        public ISet<uint> UInt32ISetNull { get; set; }
        public ISet<long> Int64ISetNull { get; set; }
        public ISet<ulong> UInt64ISetNull { get; set; }
        public ISet<float> SingleISetNull { get; set; }
        public ISet<double> DoubleISetNull { get; set; }
        public ISet<decimal> DecimalISetNull { get; set; }
        public ISet<char> CharISetNull { get; set; }
        public ISet<DateTime> DateTimeISetNull { get; set; }
        public ISet<DateTimeOffset> DateTimeOffsetISetNull { get; set; }
        public ISet<TimeSpan> TimeSpanISetNull { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly> DateOnlyISetNull { get; set; }
        public ISet<TimeOnly> TimeOnlyISetNull { get; set; }
#endif
        public ISet<Guid> GuidISetNull { get; set; }

        public ISet<bool?> BooleanISetNullable { get; set; }
        public ISet<byte?> ByteISetNullable { get; set; }
        public ISet<sbyte?> SByteISetNullable { get; set; }
        public ISet<short?> Int16ISetNullable { get; set; }
        public ISet<ushort?> UInt16ISetNullable { get; set; }
        public ISet<int?> Int32ISetNullable { get; set; }
        public ISet<uint?> UInt32ISetNullable { get; set; }
        public ISet<long?> Int64ISetNullable { get; set; }
        public ISet<ulong?> UInt64ISetNullable { get; set; }
        public ISet<float?> SingleISetNullable { get; set; }
        public ISet<double?> DoubleISetNullable { get; set; }
        public ISet<decimal?> DecimalISetNullable { get; set; }
        public ISet<char?> CharISetNullable { get; set; }
        public ISet<DateTime?> DateTimeISetNullable { get; set; }
        public ISet<DateTimeOffset?> DateTimeOffsetISetNullable { get; set; }
        public ISet<TimeSpan?> TimeSpanISetNullable { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly?> DateOnlyISetNullable { get; set; }
        public ISet<TimeOnly?> TimeOnlyISetNullable { get; set; }
#endif
        public ISet<Guid?> GuidISetNullable { get; set; }

        public ISet<Guid> GuidISetString { get; set; }

        #endregion

        #region IReadOnlySet

#if NET5_0_OR_GREATER

        public IReadOnlySet<bool> BooleanIReadOnlySet { get; set; }
        public IReadOnlySet<byte> ByteIReadOnlySet { get; set; }
        public IReadOnlySet<sbyte> SByteIReadOnlySet { get; set; }
        public IReadOnlySet<short> Int16IReadOnlySet { get; set; }
        public IReadOnlySet<ushort> UInt16IReadOnlySet { get; set; }
        public IReadOnlySet<int> Int32IReadOnlySet { get; set; }
        public IReadOnlySet<uint> UInt32IReadOnlySet { get; set; }
        public IReadOnlySet<long> Int64IReadOnlySet { get; set; }
        public IReadOnlySet<ulong> UInt64IReadOnlySet { get; set; }
        public IReadOnlySet<float> SingleIReadOnlySet { get; set; }
        public IReadOnlySet<double> DoubleIReadOnlySet { get; set; }
        public IReadOnlySet<decimal> DecimalIReadOnlySet { get; set; }
        public IReadOnlySet<char> CharIReadOnlySet { get; set; }
        public IReadOnlySet<DateTime> DateTimeIReadOnlySet { get; set; }
        public IReadOnlySet<DateTimeOffset> DateTimeOffsetIReadOnlySet { get; set; }
        public IReadOnlySet<TimeSpan> TimeSpanIReadOnlySet { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly> DateOnlyIReadOnlySet { get; set; }
        public IReadOnlySet<TimeOnly> TimeOnlyIReadOnlySet { get; set; }
#endif
        public IReadOnlySet<Guid> GuidIReadOnlySet { get; set; }

        public IReadOnlySet<bool?> BooleanIReadOnlySetNullable { get; set; }
        public IReadOnlySet<byte?> ByteIReadOnlySetNullable { get; set; }
        public IReadOnlySet<sbyte?> SByteIReadOnlySetNullable { get; set; }
        public IReadOnlySet<short?> Int16IReadOnlySetNullable { get; set; }
        public IReadOnlySet<ushort?> UInt16IReadOnlySetNullable { get; set; }
        public IReadOnlySet<int?> Int32IReadOnlySetNullable { get; set; }
        public IReadOnlySet<uint?> UInt32IReadOnlySetNullable { get; set; }
        public IReadOnlySet<long?> Int64IReadOnlySetNullable { get; set; }
        public IReadOnlySet<ulong?> UInt64IReadOnlySetNullable { get; set; }
        public IReadOnlySet<float?> SingleIReadOnlySetNullable { get; set; }
        public IReadOnlySet<double?> DoubleIReadOnlySetNullable { get; set; }
        public IReadOnlySet<decimal?> DecimalIReadOnlySetNullable { get; set; }
        public IReadOnlySet<char?> CharIReadOnlySetNullable { get; set; }
        public IReadOnlySet<DateTime?> DateTimeIReadOnlySetNullable { get; set; }
        public IReadOnlySet<DateTimeOffset?> DateTimeOffsetIReadOnlySetNullable { get; set; }
        public IReadOnlySet<TimeSpan?> TimeSpanIReadOnlySetNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly?> DateOnlyIReadOnlySetNullable { get; set; }
        public IReadOnlySet<TimeOnly?> TimeOnlyIReadOnlySetNullable { get; set; }
#endif
        public IReadOnlySet<string> StringIReadOnlySetNullable { get; set; }

#endif

        #endregion

        #region IEnumerableT

        public IEnumerable<bool> BooleanIEnumerableT { get; set; }
        public IEnumerable<byte> ByteIEnumerableT { get; set; }
        public IEnumerable<sbyte> SByteIEnumerableT { get; set; }
        public IEnumerable<short> Int16IEnumerableT { get; set; }
        public IEnumerable<ushort> UInt16IEnumerableT { get; set; }
        public IEnumerable<int> Int32IEnumerableT { get; set; }
        public IEnumerable<uint> UInt32IEnumerableT { get; set; }
        public IEnumerable<long> Int64IEnumerableT { get; set; }
        public IEnumerable<ulong> UInt64IEnumerableT { get; set; }
        public IEnumerable<float> SingleIEnumerableT { get; set; }
        public IEnumerable<double> DoubleIEnumerableT { get; set; }
        public IEnumerable<decimal> DecimalIEnumerableT { get; set; }
        public IEnumerable<char> CharIEnumerableT { get; set; }
        public IEnumerable<DateTime> DateTimeIEnumerableT { get; set; }
        public IEnumerable<DateTimeOffset> DateTimeOffsetIEnumerableT { get; set; }
        public IEnumerable<TimeSpan> TimeSpanIEnumerableT { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable<DateOnly> DateOnlyIEnumerableT { get; set; }
        public IEnumerable<TimeOnly> TimeOnlyIEnumerableT { get; set; }
#endif
        public IEnumerable<Guid> GuidIEnumerableT { get; set; }

        public IEnumerable<bool?> BooleanIEnumerableTNullable { get; set; }
        public IEnumerable<byte?> ByteIEnumerableTNullable { get; set; }
        public IEnumerable<sbyte?> SByteIEnumerableTNullable { get; set; }
        public IEnumerable<short?> Int16IEnumerableTNullable { get; set; }
        public IEnumerable<ushort?> UInt16IEnumerableTNullable { get; set; }
        public IEnumerable<int?> Int32IEnumerableTNullable { get; set; }
        public IEnumerable<uint?> UInt32IEnumerableTNullable { get; set; }
        public IEnumerable<long?> Int64IEnumerableTNullable { get; set; }
        public IEnumerable<ulong?> UInt64IEnumerableTNullable { get; set; }
        public IEnumerable<float?> SingleIEnumerableTNullable { get; set; }
        public IEnumerable<double?> DoubleIEnumerableTNullable { get; set; }
        public IEnumerable<decimal?> DecimalIEnumerableTNullable { get; set; }
        public IEnumerable<char?> CharIEnumerableTNullable { get; set; }
        public IEnumerable<DateTime?> DateTimeIEnumerableTNullable { get; set; }
        public IEnumerable<DateTimeOffset?> DateTimeOffsetIEnumerableTNullable { get; set; }
        public IEnumerable<TimeSpan?> TimeSpanIEnumerableTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable<DateOnly?> DateOnlyIEnumerableTNullable { get; set; }
        public IEnumerable<TimeOnly?> TimeOnlyIEnumerableTNullable { get; set; }
#endif
        public IEnumerable<Guid?> GuidIEnumerableTNullable { get; set; }

        public IEnumerable<string> StringIEnumerableT { get; set; }

        #endregion

    }
}