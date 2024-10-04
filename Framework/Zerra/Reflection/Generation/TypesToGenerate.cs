// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Zerra.Reflection.Generation
{
    public class TypesToGenerate
    {
        #region Non-Generics

        public IList IList { get; set; }
        public ICollection ICollection { get; set; }
        public IEnumerable IEnumerable { get; set; }
        public IDictionary IDictionary { get; set; }

        #endregion

        #region Basic

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

        public bool? BooleanThingNullableNull { get; set; }
        public byte? ByteThingNullableNull { get; set; }
        public sbyte? SByteThingNullableNull { get; set; }
        public short? Int16ThingNullableNull { get; set; }
        public ushort? UInt16ThingNullableNull { get; set; }
        public int? Int32ThingNullableNull { get; set; }
        public uint? UInt32ThingNullableNull { get; set; }
        public long? Int64ThingNullableNull { get; set; }
        public ulong? UInt64ThingNullableNull { get; set; }
        public float? SingleThingNullableNull { get; set; }
        public double? DoubleThingNullableNull { get; set; }
        public decimal? DecimalThingNullableNull { get; set; }
        public char? CharThingNullableNull { get; set; }
        public DateTime? DateTimeThingNullableNull { get; set; }
        public DateTimeOffset? DateTimeOffsetThingNullableNull { get; set; }
        public TimeSpan? TimeSpanThingNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly? DateOnlyThingNullableNull { get; set; }
        public TimeOnly? TimeOnlyThingNullableNull { get; set; }
#endif
        public Guid? GuidThingNullableNull { get; set; }

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

        public bool[] BooleanArrayEmpty { get; set; }
        public byte[] ByteArrayEmpty { get; set; }
        public sbyte[] SByteArrayEmpty { get; set; }
        public short[] Int16ArrayEmpty { get; set; }
        public ushort[] UInt16ArrayEmpty { get; set; }
        public int[] Int32ArrayEmpty { get; set; }
        public uint[] UInt32ArrayEmpty { get; set; }
        public long[] Int64ArrayEmpty { get; set; }
        public ulong[] UInt64ArrayEmpty { get; set; }
        public float[] SingleArrayEmpty { get; set; }
        public double[] DoubleArrayEmpty { get; set; }
        public decimal[] DecimalArrayEmpty { get; set; }
        public char[] CharArrayEmpty { get; set; }
        public DateTime[] DateTimeArrayEmpty { get; set; }
        public DateTimeOffset[] DateTimeOffsetArrayEmpty { get; set; }
        public TimeSpan[] TimeSpanArrayEmpty { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly[] DateOnlyArrayEmpty { get; set; }
        public TimeOnly[] TimeOnlyArrayEmpty { get; set; }
#endif
        public Guid[] GuidArrayEmpty { get; set; }

        public bool[] BooleanArrayNull { get; set; }
        public byte[] ByteArrayNull { get; set; }
        public sbyte[] SByteArrayNull { get; set; }
        public short[] Int16ArrayNull { get; set; }
        public ushort[] UInt16ArrayNull { get; set; }
        public int[] Int32ArrayNull { get; set; }
        public uint[] UInt32ArrayNull { get; set; }
        public long[] Int64ArrayNull { get; set; }
        public ulong[] UInt64ArrayNull { get; set; }
        public float[] SingleArrayNull { get; set; }
        public double[] DoubleArrayNull { get; set; }
        public decimal[] DecimalArrayNull { get; set; }
        public char[] CharArrayNull { get; set; }
        public DateTime[] DateTimeArrayNull { get; set; }
        public DateTimeOffset[] DateTimeOffsetArrayNull { get; set; }
        public TimeSpan[] TimeSpanArrayNull { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly[] DateOnlyArrayNull { get; set; }
        public TimeOnly[] TimeOnlyArrayNull { get; set; }
#endif
        public Guid[] GuidArrayNull { get; set; }

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

        public bool?[] BooleanArrayNullableEmpty { get; set; }
        public byte?[] ByteArrayNullableEmpty { get; set; }
        public sbyte?[] SByteArrayNullableEmpty { get; set; }
        public short?[] Int16ArrayNullableEmpty { get; set; }
        public ushort?[] UInt16ArrayNullableEmpty { get; set; }
        public int?[] Int32ArrayNullableEmpty { get; set; }
        public uint?[] UInt32ArrayNullableEmpty { get; set; }
        public long?[] Int64ArrayNullableEmpty { get; set; }
        public ulong?[] UInt64ArrayNullableEmpty { get; set; }
        public float?[] SingleArrayNullableEmpty { get; set; }
        public double?[] DoubleArrayNullableEmpty { get; set; }
        public decimal?[] DecimalArrayNullableEmpty { get; set; }
        public char?[] CharArrayNullableEmpty { get; set; }
        public DateTime?[] DateTimeArrayNullableEmpty { get; set; }
        public DateTimeOffset?[] DateTimeOffsetArrayNullableEmpty { get; set; }
        public TimeSpan?[] TimeSpanArrayNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly?[] DateOnlyArrayNullableEmpty { get; set; }
        public TimeOnly?[] TimeOnlyArrayNullableEmpty { get; set; }
#endif
        public Guid?[] GuidArrayNullableEmpty { get; set; }

        public bool?[] BooleanArrayNullableNull { get; set; }
        public byte?[] ByteArrayNullableNull { get; set; }
        public sbyte?[] SByteArrayNullableNull { get; set; }
        public short?[] Int16ArrayNullableNull { get; set; }
        public ushort?[] UInt16ArrayNullableNull { get; set; }
        public int?[] Int32ArrayNullableNull { get; set; }
        public uint?[] UInt32ArrayNullableNull { get; set; }
        public long?[] Int64ArrayNullableNull { get; set; }
        public ulong?[] UInt64ArrayNullableNull { get; set; }
        public float?[] SingleArrayNullableNull { get; set; }
        public double?[] DoubleArrayNullableNull { get; set; }
        public decimal?[] DecimalArrayNullableNull { get; set; }
        public char?[] CharArrayNullableNull { get; set; }
        public DateTime?[] DateTimeArrayNullableNull { get; set; }
        public DateTimeOffset?[] DateTimeOffsetArrayNullableNull { get; set; }
        public TimeSpan?[] TimeSpanArrayNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly?[] DateOnlyArrayNullableNull { get; set; }
        public TimeOnly?[] TimeOnlyArrayNullableNull { get; set; }
#endif
        public Guid?[] GuidArrayNullableNull { get; set; }

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

        public List<bool> BooleanListTEmpty { get; set; }
        public List<byte> ByteListTEmpty { get; set; }
        public List<sbyte> SByteListTEmpty { get; set; }
        public List<short> Int16ListTEmpty { get; set; }
        public List<ushort> UInt16ListTEmpty { get; set; }
        public List<int> Int32ListTEmpty { get; set; }
        public List<uint> UInt32ListTEmpty { get; set; }
        public List<long> Int64ListTEmpty { get; set; }
        public List<ulong> UInt64ListTEmpty { get; set; }
        public List<float> SingleListTEmpty { get; set; }
        public List<double> DoubleListTEmpty { get; set; }
        public List<decimal> DecimalListTEmpty { get; set; }
        public List<char> CharListTEmpty { get; set; }
        public List<DateTime> DateTimeListTEmpty { get; set; }
        public List<DateTimeOffset> DateTimeOffsetListTEmpty { get; set; }
        public List<TimeSpan> TimeSpanListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly> DateOnlyListTEmpty { get; set; }
        public List<TimeOnly> TimeOnlyListTEmpty { get; set; }
#endif
        public List<Guid> GuidListTEmpty { get; set; }

        public List<bool> BooleanListTNull { get; set; }
        public List<byte> ByteListTNull { get; set; }
        public List<sbyte> SByteListTNull { get; set; }
        public List<short> Int16ListTNull { get; set; }
        public List<ushort> UInt16ListTNull { get; set; }
        public List<int> Int32ListTNull { get; set; }
        public List<uint> UInt32ListTNull { get; set; }
        public List<long> Int64ListTNull { get; set; }
        public List<ulong> UInt64ListTNull { get; set; }
        public List<float> SingleListTNull { get; set; }
        public List<double> DoubleListTNull { get; set; }
        public List<decimal> DecimalListTNull { get; set; }
        public List<char> CharListTNull { get; set; }
        public List<DateTime> DateTimeListTNull { get; set; }
        public List<DateTimeOffset> DateTimeOffsetListTNull { get; set; }
        public List<TimeSpan> TimeSpanListTNull { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly> DateOnlyListTNull { get; set; }
        public List<TimeOnly> TimeOnlyListTNull { get; set; }
#endif
        public List<Guid> GuidListTNull { get; set; }

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

        public List<bool?> BooleanListTNullableEmpty { get; set; }
        public List<byte?> ByteListTNullableEmpty { get; set; }
        public List<sbyte?> SByteListTNullableEmpty { get; set; }
        public List<short?> Int16ListTNullableEmpty { get; set; }
        public List<ushort?> UInt16ListTNullableEmpty { get; set; }
        public List<int?> Int32ListTNullableEmpty { get; set; }
        public List<uint?> UInt32ListTNullableEmpty { get; set; }
        public List<long?> Int64ListTNullableEmpty { get; set; }
        public List<ulong?> UInt64ListTNullableEmpty { get; set; }
        public List<float?> SingleListTNullableEmpty { get; set; }
        public List<double?> DoubleListTNullableEmpty { get; set; }
        public List<decimal?> DecimalListTNullableEmpty { get; set; }
        public List<char?> CharListTNullableEmpty { get; set; }
        public List<DateTime?> DateTimeListTNullableEmpty { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListTNullableEmpty { get; set; }
        public List<TimeSpan?> TimeSpanListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly?> DateOnlyListTNullableEmpty { get; set; }
        public List<TimeOnly?> TimeOnlyListTNullableEmpty { get; set; }
#endif
        public List<Guid?> GuidListTNullableEmpty { get; set; }

        public List<bool?> BooleanListTNullableNull { get; set; }
        public List<byte?> ByteListTNullableNull { get; set; }
        public List<sbyte?> SByteListTNullableNull { get; set; }
        public List<short?> Int16ListTNullableNull { get; set; }
        public List<ushort?> UInt16ListTNullableNull { get; set; }
        public List<int?> Int32ListTNullableNull { get; set; }
        public List<uint?> UInt32ListTNullableNull { get; set; }
        public List<long?> Int64ListTNullableNull { get; set; }
        public List<ulong?> UInt64ListTNullableNull { get; set; }
        public List<float?> SingleListTNullableNull { get; set; }
        public List<double?> DoubleListTNullableNull { get; set; }
        public List<decimal?> DecimalListTNullableNull { get; set; }
        public List<char?> CharListTNullableNull { get; set; }
        public List<DateTime?> DateTimeListTNullableNull { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListTNullableNull { get; set; }
        public List<TimeSpan?> TimeSpanListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public List<DateOnly?> DateOnlyListTNullableNull { get; set; }
        public List<TimeOnly?> TimeOnlyListTNullableNull { get; set; }
#endif
        public List<Guid?> GuidListTNullableNull { get; set; }

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

        public IList<bool> BooleanIListTEmpty { get; set; }
        public IList<byte> ByteIListTEmpty { get; set; }
        public IList<sbyte> SByteIListTEmpty { get; set; }
        public IList<short> Int16IListTEmpty { get; set; }
        public IList<ushort> UInt16IListTEmpty { get; set; }
        public IList<int> Int32IListTEmpty { get; set; }
        public IList<uint> UInt32IListTEmpty { get; set; }
        public IList<long> Int64IListTEmpty { get; set; }
        public IList<ulong> UInt64IListTEmpty { get; set; }
        public IList<float> SingleIListTEmpty { get; set; }
        public IList<double> DoubleIListTEmpty { get; set; }
        public IList<decimal> DecimalIListTEmpty { get; set; }
        public IList<char> CharIListTEmpty { get; set; }
        public IList<DateTime> DateTimeIListTEmpty { get; set; }
        public IList<DateTimeOffset> DateTimeOffsetIListTEmpty { get; set; }
        public IList<TimeSpan> TimeSpanIListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly> DateOnlyIListTEmpty { get; set; }
        public IList<TimeOnly> TimeOnlyIListTEmpty { get; set; }
#endif
        public IList<Guid> GuidIListTEmpty { get; set; }

        public IList<bool> BooleanIListTNull { get; set; }
        public IList<byte> ByteIListTNull { get; set; }
        public IList<sbyte> SByteIListTNull { get; set; }
        public IList<short> Int16IListTNull { get; set; }
        public IList<ushort> UInt16IListTNull { get; set; }
        public IList<int> Int32IListTNull { get; set; }
        public IList<uint> UInt32IListTNull { get; set; }
        public IList<long> Int64IListTNull { get; set; }
        public IList<ulong> UInt64IListTNull { get; set; }
        public IList<float> SingleIListTNull { get; set; }
        public IList<double> DoubleIListTNull { get; set; }
        public IList<decimal> DecimalIListTNull { get; set; }
        public IList<char> CharIListTNull { get; set; }
        public IList<DateTime> DateTimeIListTNull { get; set; }
        public IList<DateTimeOffset> DateTimeOffsetIListTNull { get; set; }
        public IList<TimeSpan> TimeSpanIListTNull { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly> DateOnlyIListTNull { get; set; }
        public IList<TimeOnly> TimeOnlyIListTNull { get; set; }
#endif
        public IList<Guid> GuidIListTNull { get; set; }

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

        public IList<bool?> BooleanIListTNullableEmpty { get; set; }
        public IList<byte?> ByteIListTNullableEmpty { get; set; }
        public IList<sbyte?> SByteIListTNullableEmpty { get; set; }
        public IList<short?> Int16IListTNullableEmpty { get; set; }
        public IList<ushort?> UInt16IListTNullableEmpty { get; set; }
        public IList<int?> Int32IListTNullableEmpty { get; set; }
        public IList<uint?> UInt32IListTNullableEmpty { get; set; }
        public IList<long?> Int64IListTNullableEmpty { get; set; }
        public IList<ulong?> UInt64IListTNullableEmpty { get; set; }
        public IList<float?> SingleIListTNullableEmpty { get; set; }
        public IList<double?> DoubleIListTNullableEmpty { get; set; }
        public IList<decimal?> DecimalIListTNullableEmpty { get; set; }
        public IList<char?> CharIListTNullableEmpty { get; set; }
        public IList<DateTime?> DateTimeIListTNullableEmpty { get; set; }
        public IList<DateTimeOffset?> DateTimeOffsetIListTNullableEmpty { get; set; }
        public IList<TimeSpan?> TimeSpanIListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly?> DateOnlyIListTNullableEmpty { get; set; }
        public IList<TimeOnly?> TimeOnlyIListTNullableEmpty { get; set; }
#endif
        public IList<Guid?> GuidIListTNullableEmpty { get; set; }

        public IList<bool?> BooleanIListTNullableNull { get; set; }
        public IList<byte?> ByteIListTNullableNull { get; set; }
        public IList<sbyte?> SByteIListTNullableNull { get; set; }
        public IList<short?> Int16IListTNullableNull { get; set; }
        public IList<ushort?> UInt16IListTNullableNull { get; set; }
        public IList<int?> Int32IListTNullableNull { get; set; }
        public IList<uint?> UInt32IListTNullableNull { get; set; }
        public IList<long?> Int64IListTNullableNull { get; set; }
        public IList<ulong?> UInt64IListTNullableNull { get; set; }
        public IList<float?> SingleIListTNullableNull { get; set; }
        public IList<double?> DoubleIListTNullableNull { get; set; }
        public IList<decimal?> DecimalIListTNullableNull { get; set; }
        public IList<char?> CharIListTNullableNull { get; set; }
        public IList<DateTime?> DateTimeIListTNullableNull { get; set; }
        public IList<DateTimeOffset?> DateTimeOffsetIListTNullableNull { get; set; }
        public IList<TimeSpan?> TimeSpanIListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IList<DateOnly?> DateOnlyIListTNullableNull { get; set; }
        public IList<TimeOnly?> TimeOnlyIListTNullableNull { get; set; }
#endif
        public IList<Guid?> GuidIListTNullableNull { get; set; }

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

        public IReadOnlyList<bool> BooleanIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<byte> ByteIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<sbyte> SByteIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<short> Int16IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<ushort> UInt16IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<int> Int32IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<uint> UInt32IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<long> Int64IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<ulong> UInt64IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<float> SingleIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<double> DoubleIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<decimal> DecimalIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<char> CharIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<DateTime> DateTimeIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<DateTimeOffset> DateTimeOffsetIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<TimeSpan> TimeSpanIReadOnlyListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly> DateOnlyIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<TimeOnly> TimeOnlyIReadOnlyListTEmpty { get; set; }
#endif
        public IReadOnlyList<Guid> GuidIReadOnlyListTEmpty { get; set; }

        public IReadOnlyList<bool> BooleanIReadOnlyListTNull { get; set; }
        public IReadOnlyList<byte> ByteIReadOnlyListTNull { get; set; }
        public IReadOnlyList<sbyte> SByteIReadOnlyListTNull { get; set; }
        public IReadOnlyList<short> Int16IReadOnlyListTNull { get; set; }
        public IReadOnlyList<ushort> UInt16IReadOnlyListTNull { get; set; }
        public IReadOnlyList<int> Int32IReadOnlyListTNull { get; set; }
        public IReadOnlyList<uint> UInt32IReadOnlyListTNull { get; set; }
        public IReadOnlyList<long> Int64IReadOnlyListTNull { get; set; }
        public IReadOnlyList<ulong> UInt64IReadOnlyListTNull { get; set; }
        public IReadOnlyList<float> SingleIReadOnlyListTNull { get; set; }
        public IReadOnlyList<double> DoubleIReadOnlyListTNull { get; set; }
        public IReadOnlyList<decimal> DecimalIReadOnlyListTNull { get; set; }
        public IReadOnlyList<char> CharIReadOnlyListTNull { get; set; }
        public IReadOnlyList<DateTime> DateTimeIReadOnlyListTNull { get; set; }
        public IReadOnlyList<DateTimeOffset> DateTimeOffsetIReadOnlyListTNull { get; set; }
        public IReadOnlyList<TimeSpan> TimeSpanIReadOnlyListTNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly> DateOnlyIReadOnlyListTNull { get; set; }
        public IReadOnlyList<TimeOnly> TimeOnlyIReadOnlyListTNull { get; set; }
#endif
        public IReadOnlyList<Guid> GuidIReadOnlyListTNull { get; set; }

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

        public IReadOnlyList<bool?> BooleanIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<byte?> ByteIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<sbyte?> SByteIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<short?> Int16IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<ushort?> UInt16IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<int?> Int32IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<uint?> UInt32IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<long?> Int64IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<ulong?> UInt64IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<float?> SingleIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<double?> DoubleIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<decimal?> DecimalIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<char?> CharIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<DateTime?> DateTimeIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<DateTimeOffset?> DateTimeOffsetIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<TimeSpan?> TimeSpanIReadOnlyListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly?> DateOnlyIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<TimeOnly?> TimeOnlyIReadOnlyListTNullableEmpty { get; set; }
#endif
        public IReadOnlyList<Guid?> GuidIReadOnlyListTNullableEmpty { get; set; }

        public IReadOnlyList<bool?> BooleanIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<byte?> ByteIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<sbyte?> SByteIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<short?> Int16IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<ushort?> UInt16IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<int?> Int32IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<uint?> UInt32IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<long?> Int64IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<ulong?> UInt64IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<float?> SingleIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<double?> DoubleIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<decimal?> DecimalIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<char?> CharIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<DateTime?> DateTimeIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<DateTimeOffset?> DateTimeOffsetIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<TimeSpan?> TimeSpanIReadOnlyListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<DateOnly?> DateOnlyIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<TimeOnly?> TimeOnlyIReadOnlyListTNullableNull { get; set; }
#endif
        public IReadOnlyList<Guid?> GuidIReadOnlyListTNullableNull { get; set; }

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

        public ICollection<bool> BooleanICollectionTEmpty { get; set; }
        public ICollection<byte> ByteICollectionTEmpty { get; set; }
        public ICollection<sbyte> SByteICollectionTEmpty { get; set; }
        public ICollection<short> Int16ICollectionTEmpty { get; set; }
        public ICollection<ushort> UInt16ICollectionTEmpty { get; set; }
        public ICollection<int> Int32ICollectionTEmpty { get; set; }
        public ICollection<uint> UInt32ICollectionTEmpty { get; set; }
        public ICollection<long> Int64ICollectionTEmpty { get; set; }
        public ICollection<ulong> UInt64ICollectionTEmpty { get; set; }
        public ICollection<float> SingleICollectionTEmpty { get; set; }
        public ICollection<double> DoubleICollectionTEmpty { get; set; }
        public ICollection<decimal> DecimalICollectionTEmpty { get; set; }
        public ICollection<char> CharICollectionTEmpty { get; set; }
        public ICollection<DateTime> DateTimeICollectionTEmpty { get; set; }
        public ICollection<DateTimeOffset> DateTimeOffsetICollectionTEmpty { get; set; }
        public ICollection<TimeSpan> TimeSpanICollectionTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly> DateOnlyICollectionTEmpty { get; set; }
        public ICollection<TimeOnly> TimeOnlyICollectionTEmpty { get; set; }
#endif
        public ICollection<Guid> GuidICollectionTEmpty { get; set; }

        public ICollection<bool> BooleanICollectionTNull { get; set; }
        public ICollection<byte> ByteICollectionTNull { get; set; }
        public ICollection<sbyte> SByteICollectionTNull { get; set; }
        public ICollection<short> Int16ICollectionTNull { get; set; }
        public ICollection<ushort> UInt16ICollectionTNull { get; set; }
        public ICollection<int> Int32ICollectionTNull { get; set; }
        public ICollection<uint> UInt32ICollectionTNull { get; set; }
        public ICollection<long> Int64ICollectionTNull { get; set; }
        public ICollection<ulong> UInt64ICollectionTNull { get; set; }
        public ICollection<float> SingleICollectionTNull { get; set; }
        public ICollection<double> DoubleICollectionTNull { get; set; }
        public ICollection<decimal> DecimalICollectionTNull { get; set; }
        public ICollection<char> CharICollectionTNull { get; set; }
        public ICollection<DateTime> DateTimeICollectionTNull { get; set; }
        public ICollection<DateTimeOffset> DateTimeOffsetICollectionTNull { get; set; }
        public ICollection<TimeSpan> TimeSpanICollectionTNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly> DateOnlyICollectionTNull { get; set; }
        public ICollection<TimeOnly> TimeOnlyICollectionTNull { get; set; }
#endif
        public ICollection<Guid> GuidICollectionTNull { get; set; }

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

        public ICollection<bool?> BooleanICollectionTNullableEmpty { get; set; }
        public ICollection<byte?> ByteICollectionTNullableEmpty { get; set; }
        public ICollection<sbyte?> SByteICollectionTNullableEmpty { get; set; }
        public ICollection<short?> Int16ICollectionTNullableEmpty { get; set; }
        public ICollection<ushort?> UInt16ICollectionTNullableEmpty { get; set; }
        public ICollection<int?> Int32ICollectionTNullableEmpty { get; set; }
        public ICollection<uint?> UInt32ICollectionTNullableEmpty { get; set; }
        public ICollection<long?> Int64ICollectionTNullableEmpty { get; set; }
        public ICollection<ulong?> UInt64ICollectionTNullableEmpty { get; set; }
        public ICollection<float?> SingleICollectionTNullableEmpty { get; set; }
        public ICollection<double?> DoubleICollectionTNullableEmpty { get; set; }
        public ICollection<decimal?> DecimalICollectionTNullableEmpty { get; set; }
        public ICollection<char?> CharICollectionTNullableEmpty { get; set; }
        public ICollection<DateTime?> DateTimeICollectionTNullableEmpty { get; set; }
        public ICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullableEmpty { get; set; }
        public ICollection<TimeSpan?> TimeSpanICollectionTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly?> DateOnlyICollectionTNullableEmpty { get; set; }
        public ICollection<TimeOnly?> TimeOnlyICollectionTNullableEmpty { get; set; }
#endif
        public ICollection<Guid?> GuidICollectionTNullableEmpty { get; set; }

        public ICollection<bool?> BooleanICollectionTNullableNull { get; set; }
        public ICollection<byte?> ByteICollectionTNullableNull { get; set; }
        public ICollection<sbyte?> SByteICollectionTNullableNull { get; set; }
        public ICollection<short?> Int16ICollectionTNullableNull { get; set; }
        public ICollection<ushort?> UInt16ICollectionTNullableNull { get; set; }
        public ICollection<int?> Int32ICollectionTNullableNull { get; set; }
        public ICollection<uint?> UInt32ICollectionTNullableNull { get; set; }
        public ICollection<long?> Int64ICollectionTNullableNull { get; set; }
        public ICollection<ulong?> UInt64ICollectionTNullableNull { get; set; }
        public ICollection<float?> SingleICollectionTNullableNull { get; set; }
        public ICollection<double?> DoubleICollectionTNullableNull { get; set; }
        public ICollection<decimal?> DecimalICollectionTNullableNull { get; set; }
        public ICollection<char?> CharICollectionTNullableNull { get; set; }
        public ICollection<DateTime?> DateTimeICollectionTNullableNull { get; set; }
        public ICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullableNull { get; set; }
        public ICollection<TimeSpan?> TimeSpanICollectionTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<DateOnly?> DateOnlyICollectionTNullableNull { get; set; }
        public ICollection<TimeOnly?> TimeOnlyICollectionTNullableNull { get; set; }
#endif
        public ICollection<Guid?> GuidICollectionTNullableNull { get; set; }

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

        public IReadOnlyCollection<bool> BooleanIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<byte> ByteIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<sbyte> SByteIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<short> Int16IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<ushort> UInt16IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<int> Int32IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<uint> UInt32IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<long> Int64IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<ulong> UInt64IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<float> SingleIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<double> DoubleIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<decimal> DecimalIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<char> CharIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<DateTime> DateTimeIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<DateTimeOffset> DateTimeOffsetIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<TimeSpan> TimeSpanIReadOnlyCollectionTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly> DateOnlyIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<TimeOnly> TimeOnlyIReadOnlyCollectionTEmpty { get; set; }
#endif
        public IReadOnlyCollection<Guid> GuidIReadOnlyCollectionTEmpty { get; set; }

        public IReadOnlyCollection<bool> BooleanIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<byte> ByteIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<sbyte> SByteIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<short> Int16IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<ushort> UInt16IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<int> Int32IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<uint> UInt32IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<long> Int64IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<ulong> UInt64IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<float> SingleIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<double> DoubleIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<decimal> DecimalIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<char> CharIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<DateTime> DateTimeIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<DateTimeOffset> DateTimeOffsetIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<TimeSpan> TimeSpanIReadOnlyCollectionTNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly> DateOnlyIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<TimeOnly> TimeOnlyIReadOnlyCollectionTNull { get; set; }
#endif
        public IReadOnlyCollection<Guid> GuidIReadOnlyCollectionTNull { get; set; }

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

        public IReadOnlyCollection<bool?> BooleanIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<byte?> ByteIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<sbyte?> SByteIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<short?> Int16IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<ushort?> UInt16IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<int?> Int32IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<uint?> UInt32IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<long?> Int64IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<ulong?> UInt64IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<float?> SingleIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<double?> DoubleIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<decimal?> DecimalIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<char?> CharIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<DateTime?> DateTimeIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<DateTimeOffset?> DateTimeOffsetIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<TimeSpan?> TimeSpanIReadOnlyCollectionTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly?> DateOnlyIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<TimeOnly?> TimeOnlyIReadOnlyCollectionTNullableEmpty { get; set; }
#endif
        public IReadOnlyCollection<Guid?> GuidIReadOnlyCollectionTNullableEmpty { get; set; }

        public IReadOnlyCollection<bool?> BooleanIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<byte?> ByteIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<sbyte?> SByteIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<short?> Int16IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<ushort?> UInt16IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<int?> Int32IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<uint?> UInt32IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<long?> Int64IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<ulong?> UInt64IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<float?> SingleIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<double?> DoubleIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<decimal?> DecimalIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<char?> CharIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<DateTime?> DateTimeIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<DateTimeOffset?> DateTimeOffsetIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<TimeSpan?> TimeSpanIReadOnlyCollectionTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<DateOnly?> DateOnlyIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<TimeOnly?> TimeOnlyIReadOnlyCollectionTNullableNull { get; set; }
#endif
        public IReadOnlyCollection<Guid?> GuidIReadOnlyCollectionTNullableNull { get; set; }

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