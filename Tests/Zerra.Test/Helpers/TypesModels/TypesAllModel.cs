// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesAllModel
    {
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
        public string StringThingNull { get; set; }
        public string StringThingEmpty { get; set; }

        public EnumModel EnumThing { get; set; }
        public EnumModel? EnumThingNullable { get; set; }
        public EnumModel? EnumThingNullableNull { get; set; }

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
        public string[] StringArrayEmpty { get; set; }
        public string[] StringArrayNull { get; set; }

        public EnumModel[] EnumArray { get; set; }
        public EnumModel[] EnumArrayEmpty { get; set; }
        public EnumModel[] EnumArrayNull { get; set; }

        public EnumModel?[] EnumArrayNullable { get; set; }
        public EnumModel?[] EnumArrayNullableEmpty { get; set; }
        public EnumModel?[] EnumArrayNullableNull { get; set; }

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
        public List<string> StringListTEmpty { get; set; }
        public List<string> StringListTNull { get; set; }

        public List<EnumModel> EnumListT { get; set; }
        public List<EnumModel> EnumListTEmpty { get; set; }
        public List<EnumModel> EnumListTNull { get; set; }

        public List<EnumModel?> EnumListTNullable { get; set; }
        public List<EnumModel?> EnumListTNullableEmpty { get; set; }
        public List<EnumModel?> EnumListTNullableNull { get; set; }

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
        public IList<string> StringIListTEmpty { get; set; }
        public IList<string> StringIListTNull { get; set; }

        public IList<EnumModel> EnumIListT { get; set; }
        public IList<EnumModel> EnumIListTEmpty { get; set; }
        public IList<EnumModel> EnumIListTNull { get; set; }

        public IList<EnumModel?> EnumIListTNullable { get; set; }
        public IList<EnumModel?> EnumIListTNullableEmpty { get; set; }
        public IList<EnumModel?> EnumIListTNullableNull { get; set; }

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
        public IReadOnlyList<string> StringIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> StringIReadOnlyListTNull { get; set; }

        public IReadOnlyList<EnumModel> EnumIReadOnlyListT { get; set; }
        public IReadOnlyList<EnumModel> EnumIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<EnumModel> EnumIReadOnlyListTNull { get; set; }

        public IReadOnlyList<EnumModel?> EnumIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<EnumModel?> EnumIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<EnumModel?> EnumIReadOnlyListTNullableNull { get; set; }

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
        public ICollection<string> StringICollectionTEmpty { get; set; }
        public ICollection<string> StringICollectionTNull { get; set; }

        public ICollection<EnumModel> EnumICollectionT { get; set; }
        public ICollection<EnumModel> EnumICollectionTEmpty { get; set; }
        public ICollection<EnumModel> EnumICollectionTNull { get; set; }

        public ICollection<EnumModel?> EnumICollectionTNullable { get; set; }
        public ICollection<EnumModel?> EnumICollectionTNullableEmpty { get; set; }
        public ICollection<EnumModel?> EnumICollectionTNullableNull { get; set; }

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
        public IReadOnlyCollection<string> StringIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> StringIReadOnlyCollectionTNull { get; set; }

        public IReadOnlyCollection<EnumModel> EnumIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<EnumModel> EnumIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<EnumModel> EnumIReadOnlyCollectionTNull { get; set; }

        public IReadOnlyCollection<EnumModel?> EnumIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<EnumModel?> EnumIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<EnumModel?> EnumIReadOnlyCollectionTNullableNull { get; set; }

        #endregion

        #region Objects

        public SimpleModel ClassThing { get; set; }
        public SimpleModel ClassThingNull { get; set; }

        public SimpleModel[] ClassArray { get; set; }
        public SimpleModel[] ClassArrayEmpty { get; set; }
        public SimpleModel[] ClassArrayNull { get; set; }

        public IEnumerable<SimpleModel> ClassEnumerable { get; set; }

        public List<SimpleModel> ClassList { get; set; }
        public List<SimpleModel> ClassListEmpty { get; set; }
        public List<SimpleModel> ClassListNull { get; set; }

        public IList<SimpleModel> ClassIList { get; set; }
        public IList<SimpleModel> ClassIListEmpty { get; set; }
        public IList<SimpleModel> ClassIListNull { get; set; }

        public IReadOnlyList<SimpleModel> ClassIReadOnlyList { get; set; }
        public IReadOnlyList<SimpleModel> ClassIReadOnlyListEmpty { get; set; }
        public IReadOnlyList<SimpleModel> ClassIReadOnlyListNull { get; set; }

        public ICollection<SimpleModel> ClassICollection { get; set; }
        public ICollection<SimpleModel> ClassICollectionEmpty { get; set; }
        public ICollection<SimpleModel> ClassICollectionNull { get; set; }

        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollection { get; set; }
        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollectionEmpty { get; set; }
        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollectionNull { get; set; }

        #endregion

        #region Dictionaries

        public Dictionary<int, string> DictionaryThing1 { get; set; }
        public Dictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public IDictionary<int, string> DictionaryThing3 { get; set; }
        public IDictionary<int, SimpleModel> DictionaryThing4 { get; set; }
        public IReadOnlyDictionary<int, string> DictionaryThing5 { get; set; }
        public IReadOnlyDictionary<int, SimpleModel> DictionaryThing6 { get; set; }
        public ConcurrentDictionary<int, string> DictionaryThing7 { get; set; }
        public ConcurrentDictionary<int, SimpleModel> DictionaryThing8 { get; set; }

        #endregion

        public string[][] StringArrayOfArrayThing { get; set; }

        public static TypesAllModel Create()
        {
            var model = new TypesAllModel()
            {
                #region Basic

                BooleanThing = true,
                ByteThing = 1,
                SByteThing = -2,
                Int16Thing = -3,
                UInt16Thing = 4,
                Int32Thing = -5,
                UInt32Thing = 6,
                Int64Thing = -7,
                UInt64Thing = 8,
                SingleThing = -9.1f,
                DoubleThing = -10.2,
                DecimalThing = -11.3m,
                CharThing = 'Z',
                DateTimeThing = DateTime.UtcNow.Date,
                DateTimeOffsetThing = DateTimeOffset.UtcNow.AddDays(1),
                TimeSpanThing = -(new TimeSpan(2, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond, 1)),
#if NET6_0_OR_GREATER
                DateOnlyThing = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                TimeOnlyThing = TimeOnly.FromDateTime(DateTime.UtcNow),
#endif
                GuidThing = Guid.NewGuid(),

                BooleanThingNullable = true,
                ByteThingNullable = 11,
                SByteThingNullable = -12,
                Int16ThingNullable = -13,
                UInt16ThingNullable = 14,
                Int32ThingNullable = -15,
                UInt32ThingNullable = 16,
                Int64ThingNullable = -17,
                UInt64ThingNullable = 18,
                SingleThingNullable = -19.1f,
                DoubleThingNullable = -110.2,
                DecimalThingNullable = -111.3m,
                CharThingNullable = 'X',
                DateTimeThingNullable = DateTime.UtcNow.AddMonths(1),
                DateTimeOffsetThingNullable = DateTimeOffset.UtcNow.AddMonths(1).AddDays(1),
                TimeSpanThingNullable = new TimeSpan(0, 0, 0, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond, 1),
#if NET6_0_OR_GREATER
                DateOnlyThingNullable = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                TimeOnlyThingNullable = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)),
#endif
                GuidThingNullable = Guid.NewGuid(),

                BooleanThingNullableNull = null,
                ByteThingNullableNull = null,
                SByteThingNullableNull = null,
                Int16ThingNullableNull = null,
                UInt16ThingNullableNull = null,
                Int32ThingNullableNull = null,
                UInt32ThingNullableNull = null,
                Int64ThingNullableNull = null,
                UInt64ThingNullableNull = null,
                SingleThingNullableNull = null,
                DoubleThingNullableNull = null,
                DecimalThingNullableNull = null,
                CharThingNullableNull = null,
                DateTimeThingNullableNull = null,
                DateTimeOffsetThingNullableNull = null,
                TimeSpanThingNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyThingNullableNull = null,
                TimeOnlyThingNullableNull = null,
#endif
                GuidThingNullableNull = null,

                StringThing = "Hello\r\nWorld!",
                StringThingNull = null,
                StringThingEmpty = String.Empty,

                EnumThing = EnumModel.EnumItem1,
                EnumThingNullable = EnumModel.EnumItem2,
                EnumThingNullableNull = null,

                #endregion

                #region Arrays

                BooleanArray = [true, false, true],
                ByteArray = [1, 2, 3],
                SByteArray = [4, 5, 6],
                Int16Array = [7, 8, 9],
                UInt16Array = [10, 11, 12],
                Int32Array = [13, 14, 15],
                UInt32Array = [16, 17, 18],
                Int64Array = [19, 20, 21],
                UInt64Array = [22, 23, 24],
                SingleArray = [25, 26, 27],
                DoubleArray = [28, 29, 30],
                DecimalArray = [31, 32, 33],
                CharArray = ['A', 'B', 'C'],
                DateTimeArray = [DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3)],
                DateTimeOffsetArray = [DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6)],
                TimeSpanArray = [DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay],
#if NET6_0_OR_GREATER
                DateOnlyArray = [DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))],
                TimeOnlyArray = [TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3))],
#endif
                GuidArray = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],

                BooleanArrayEmpty = [],
                ByteArrayEmpty = [],
                SByteArrayEmpty = [],
                Int16ArrayEmpty = [],
                UInt16ArrayEmpty = [],
                Int32ArrayEmpty = [],
                UInt32ArrayEmpty = [],
                Int64ArrayEmpty = [],
                UInt64ArrayEmpty = [],
                SingleArrayEmpty = [],
                DoubleArrayEmpty = [],
                DecimalArrayEmpty = [],
                CharArrayEmpty = [],
                DateTimeArrayEmpty = [],
                DateTimeOffsetArrayEmpty = [],
                TimeSpanArrayEmpty = [],
#if NET6_0_OR_GREATER
                DateOnlyArrayEmpty = [],
                TimeOnlyArrayEmpty = [],
#endif
                GuidArrayEmpty = [],

                BooleanArrayNull = null,
                ByteArrayNull = null,
                SByteArrayNull = null,
                Int16ArrayNull = null,
                UInt16ArrayNull = null,
                Int32ArrayNull = null,
                UInt32ArrayNull = null,
                Int64ArrayNull = null,
                UInt64ArrayNull = null,
                SingleArrayNull = null,
                DoubleArrayNull = null,
                DecimalArrayNull = null,
                CharArrayNull = null,
                DateTimeArrayNull = null,
                DateTimeOffsetArrayNull = null,
                TimeSpanArrayNull = null,
#if NET6_0_OR_GREATER
                DateOnlyArrayNull = [],
                TimeOnlyArrayNull = [],
#endif
                GuidArrayNull = null,

                BooleanArrayNullable = [true, null, true],
                ByteArrayNullable = [1, null, 3],
                SByteArrayNullable = [4, null, 6],
                Int16ArrayNullable = [7, null, 9],
                UInt16ArrayNullable = [10, null, 12],
                Int32ArrayNullable = [13, null, 15],
                UInt32ArrayNullable = [16, null, 18],
                Int64ArrayNullable = [19, null, 21],
                UInt64ArrayNullable = [22, null, 24],
                SingleArrayNullable = [25, null, 27],
                DoubleArrayNullable = [28, null, 30],
                DecimalArrayNullable = [31, null, 33],
                CharArrayNullable = ['A', null, 'C'],
                DateTimeArrayNullable = [DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3)],
                DateTimeOffsetArrayNullable = [DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6)],
                TimeSpanArrayNullable = [DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay],
#if NET6_0_OR_GREATER
                DateOnlyArrayNullable = [DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))],
                TimeOnlyArrayNullable = [TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3))],
#endif
                GuidArrayNullable = [Guid.NewGuid(), null, Guid.NewGuid()],

                BooleanArrayNullableEmpty = [],
                ByteArrayNullableEmpty = [],
                SByteArrayNullableEmpty = [],
                Int16ArrayNullableEmpty = [],
                UInt16ArrayNullableEmpty = [],
                Int32ArrayNullableEmpty = [],
                UInt32ArrayNullableEmpty = [],
                Int64ArrayNullableEmpty = [],
                UInt64ArrayNullableEmpty = [],
                SingleArrayNullableEmpty = [],
                DoubleArrayNullableEmpty = [],
                DecimalArrayNullableEmpty = [],
                CharArrayNullableEmpty = [],
                DateTimeArrayNullableEmpty = [],
                DateTimeOffsetArrayNullableEmpty = [],
                TimeSpanArrayNullableEmpty = [],
#if NET6_0_OR_GREATER
                DateOnlyArrayNullableEmpty = [],
                TimeOnlyArrayNullableEmpty = [],
#endif
                GuidArrayNullableEmpty = [],

                BooleanArrayNullableNull = null,
                ByteArrayNullableNull = null,
                SByteArrayNullableNull = null,
                Int16ArrayNullableNull = null,
                UInt16ArrayNullableNull = null,
                Int32ArrayNullableNull = null,
                UInt32ArrayNullableNull = null,
                Int64ArrayNullableNull = null,
                UInt64ArrayNullableNull = null,
                SingleArrayNullableNull = null,
                DoubleArrayNullableNull = null,
                DecimalArrayNullableNull = null,
                CharArrayNullableNull = null,
                DateTimeArrayNullableNull = null,
                DateTimeOffsetArrayNullableNull = null,
                TimeSpanArrayNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyArrayNullableNull = null,
                TimeOnlyArrayNullableNull = null,
#endif
                GuidArrayNullableNull = null,

                StringArray = ["Hello", "World", null, "", "People"],
                StringArrayEmpty = Array.Empty<string>(),
                StringArrayNull = null,

                EnumArray = [EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3],
                EnumArrayEmpty = [],
                EnumArrayNull = null,

                EnumArrayNullable = [EnumModel.EnumItem1, null, EnumModel.EnumItem3],
                EnumArrayNullableEmpty = [],
                EnumArrayNullableNull = null,

                #endregion

                #region ListTs

                BooleanListT = new List<bool>() { true, false, true },
                ByteListT = new List<byte>() { 1, 2, 3 },
                SByteListT = new List<sbyte>() { 4, 5, 6 },
                Int16ListT = new List<short>() { 7, 8, 9 },
                UInt16ListT = new List<ushort>() { 10, 11, 12 },
                Int32ListT = new List<int>() { 13, 14, 15 },
                UInt32ListT = new List<uint>() { 16, 17, 18 },
                Int64ListT = new List<long>() { 19, 20, 21 },
                UInt64ListT = new List<ulong>() { 22, 23, 24 },
                SingleListT = new List<float>() { 25, 26, 27 },
                DoubleListT = new List<double>() { 28, 29, 30 },
                DecimalListT = new List<decimal>() { 31, 32, 33 },
                CharListT = new List<char>() { 'A', 'B', 'C' },
                DateTimeListT = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetListT = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanListT = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyListT = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyListT = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidListT = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanListTEmpty = new List<bool>(0),
                ByteListTEmpty = new List<byte>(0),
                SByteListTEmpty = new List<sbyte>(0),
                Int16ListTEmpty = new List<short>(0),
                UInt16ListTEmpty = new List<ushort>(0),
                Int32ListTEmpty = new List<int>(0),
                UInt32ListTEmpty = new List<uint>(0),
                Int64ListTEmpty = new List<long>(0),
                UInt64ListTEmpty = new List<ulong>(0),
                SingleListTEmpty = new List<float>(0),
                DoubleListTEmpty = new List<double>(0),
                DecimalListTEmpty = new List<decimal>(0),
                CharListTEmpty = new List<char>(0),
                DateTimeListTEmpty = new List<DateTime>(0),
                DateTimeOffsetListTEmpty = new List<DateTimeOffset>(0),
                TimeSpanListTEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyListTEmpty = new List<DateOnly>(0),
                TimeOnlyListTEmpty = new List<TimeOnly>(0),
#endif
                GuidListTEmpty = new List<Guid>(0),

                BooleanListTNull = null,
                ByteListTNull = null,
                SByteListTNull = null,
                Int16ListTNull = null,
                UInt16ListTNull = null,
                Int32ListTNull = null,
                UInt32ListTNull = null,
                Int64ListTNull = null,
                UInt64ListTNull = null,
                SingleListTNull = null,
                DoubleListTNull = null,
                DecimalListTNull = null,
                CharListTNull = null,
                DateTimeListTNull = null,
                DateTimeOffsetListTNull = null,
                TimeSpanListTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyListTNull = null,
                TimeOnlyListTNull = null,
#endif
                GuidListTNull = null,

                BooleanListTNullable = new List<bool?>() { true, null, true },
                ByteListTNullable = new List<byte?>() { 1, null, 3 },
                SByteListTNullable = new List<sbyte?>() { 4, null, 6 },
                Int16ListTNullable = new List<short?>() { 7, null, 9 },
                UInt16ListTNullable = new List<ushort?>() { 10, null, 12 },
                Int32ListTNullable = new List<int?>() { 13, null, 15 },
                UInt32ListTNullable = new List<uint?>() { 16, null, 18 },
                Int64ListTNullable = new List<long?>() { 19, null, 21 },
                UInt64ListTNullable = new List<ulong?>() { 22, null, 24 },
                SingleListTNullable = new List<float?>() { 25, null, 27 },
                DoubleListTNullable = new List<double?>() { 28, null, 30 },
                DecimalListTNullable = new List<decimal?>() { 31, null, 33 },
                CharListTNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeListTNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetListTNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanListTNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyListTNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyListTNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidListTNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanListTNullableEmpty = new List<bool?>(0),
                ByteListTNullableEmpty = new List<byte?>(0),
                SByteListTNullableEmpty = new List<sbyte?>(0),
                Int16ListTNullableEmpty = new List<short?>(0),
                UInt16ListTNullableEmpty = new List<ushort?>(0),
                Int32ListTNullableEmpty = new List<int?>(0),
                UInt32ListTNullableEmpty = new List<uint?>(0),
                Int64ListTNullableEmpty = new List<long?>(0),
                UInt64ListTNullableEmpty = new List<ulong?>(0),
                SingleListTNullableEmpty = new List<float?>(0),
                DoubleListTNullableEmpty = new List<double?>(0),
                DecimalListTNullableEmpty = new List<decimal?>(0),
                CharListTNullableEmpty = new List<char?>(0),
                DateTimeListTNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetListTNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanListTNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyListTNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyListTNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidListTNullableEmpty = new List<Guid?>(0),

                BooleanListTNullableNull = null,
                ByteListTNullableNull = null,
                SByteListTNullableNull = null,
                Int16ListTNullableNull = null,
                UInt16ListTNullableNull = null,
                Int32ListTNullableNull = null,
                UInt32ListTNullableNull = null,
                Int64ListTNullableNull = null,
                UInt64ListTNullableNull = null,
                SingleListTNullableNull = null,
                DoubleListTNullableNull = null,
                DecimalListTNullableNull = null,
                CharListTNullableNull = null,
                DateTimeListTNullableNull = null,
                DateTimeOffsetListTNullableNull = null,
                TimeSpanListTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyListTNullableNull = null,
                TimeOnlyListTNullableNull = null,
#endif
                GuidListTNullableNull = null,

                StringListT = new List<string>() { "Hello", "World", null, "", "People" },
                StringListTEmpty = new List<string>(0),
                StringListTNull = null,

                EnumListT = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumListTEmpty = new(0),
                EnumListTNull = null,

                EnumListTNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumListTNullableEmpty = new(0),
                EnumListTNullableNull = null,

                #endregion

                #region IListTs

                BooleanIListT = new List<bool>() { true, false, true },
                ByteIListT = new List<byte>() { 1, 2, 3 },
                SByteIListT = new List<sbyte>() { 4, 5, 6 },
                Int16IListT = new List<short>() { 7, 8, 9 },
                UInt16IListT = new List<ushort>() { 10, 11, 12 },
                Int32IListT = new List<int>() { 13, 14, 15 },
                UInt32IListT = new List<uint>() { 16, 17, 18 },
                Int64IListT = new List<long>() { 19, 20, 21 },
                UInt64IListT = new List<ulong>() { 22, 23, 24 },
                SingleIListT = new List<float>() { 25, 26, 27 },
                DoubleIListT = new List<double>() { 28, 29, 30 },
                DecimalIListT = new List<decimal>() { 31, 32, 33 },
                CharIListT = new List<char>() { 'A', 'B', 'C' },
                DateTimeIListT = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListT = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListT = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListT = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListT = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListT = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIListTEmpty = new List<bool>(0),
                ByteIListTEmpty = new List<byte>(0),
                SByteIListTEmpty = new List<sbyte>(0),
                Int16IListTEmpty = new List<short>(0),
                UInt16IListTEmpty = new List<ushort>(0),
                Int32IListTEmpty = new List<int>(0),
                UInt32IListTEmpty = new List<uint>(0),
                Int64IListTEmpty = new List<long>(0),
                UInt64IListTEmpty = new List<ulong>(0),
                SingleIListTEmpty = new List<float>(0),
                DoubleIListTEmpty = new List<double>(0),
                DecimalIListTEmpty = new List<decimal>(0),
                CharIListTEmpty = new List<char>(0),
                DateTimeIListTEmpty = new List<DateTime>(0),
                DateTimeOffsetIListTEmpty = new List<DateTimeOffset>(0),
                TimeSpanIListTEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTEmpty = new List<DateOnly>(0),
                TimeOnlyIListTEmpty = new List<TimeOnly>(0),
#endif
                GuidIListTEmpty = new List<Guid>(0),

                BooleanIListTNull = null,
                ByteIListTNull = null,
                SByteIListTNull = null,
                Int16IListTNull = null,
                UInt16IListTNull = null,
                Int32IListTNull = null,
                UInt32IListTNull = null,
                Int64IListTNull = null,
                UInt64IListTNull = null,
                SingleIListTNull = null,
                DoubleIListTNull = null,
                DecimalIListTNull = null,
                CharIListTNull = null,
                DateTimeIListTNull = null,
                DateTimeOffsetIListTNull = null,
                TimeSpanIListTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListTNull = null,
                TimeOnlyIListTNull = null,
#endif
                GuidIListTNull = null,

                BooleanIListTNullable = new List<bool?>() { true, null, true },
                ByteIListTNullable = new List<byte?>() { 1, null, 3 },
                SByteIListTNullable = new List<sbyte?>() { 4, null, 6 },
                Int16IListTNullable = new List<short?>() { 7, null, 9 },
                UInt16IListTNullable = new List<ushort?>() { 10, null, 12 },
                Int32IListTNullable = new List<int?>() { 13, null, 15 },
                UInt32IListTNullable = new List<uint?>() { 16, null, 18 },
                Int64IListTNullable = new List<long?>() { 19, null, 21 },
                UInt64IListTNullable = new List<ulong?>() { 22, null, 24 },
                SingleIListTNullable = new List<float?>() { 25, null, 27 },
                DoubleIListTNullable = new List<double?>() { 28, null, 30 },
                DecimalIListTNullable = new List<decimal?>() { 31, null, 33 },
                CharIListTNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeIListTNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListTNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListTNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListTNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListTNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListTNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIListTNullableEmpty = new List<bool?>(0),
                ByteIListTNullableEmpty = new List<byte?>(0),
                SByteIListTNullableEmpty = new List<sbyte?>(0),
                Int16IListTNullableEmpty = new List<short?>(0),
                UInt16IListTNullableEmpty = new List<ushort?>(0),
                Int32IListTNullableEmpty = new List<int?>(0),
                UInt32IListTNullableEmpty = new List<uint?>(0),
                Int64IListTNullableEmpty = new List<long?>(0),
                UInt64IListTNullableEmpty = new List<ulong?>(0),
                SingleIListTNullableEmpty = new List<float?>(0),
                DoubleIListTNullableEmpty = new List<double?>(0),
                DecimalIListTNullableEmpty = new List<decimal?>(0),
                CharIListTNullableEmpty = new List<char?>(0),
                DateTimeIListTNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetIListTNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanIListTNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyIListTNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidIListTNullableEmpty = new List<Guid?>(0),

                BooleanIListTNullableNull = null,
                ByteIListTNullableNull = null,
                SByteIListTNullableNull = null,
                Int16IListTNullableNull = null,
                UInt16IListTNullableNull = null,
                Int32IListTNullableNull = null,
                UInt32IListTNullableNull = null,
                Int64IListTNullableNull = null,
                UInt64IListTNullableNull = null,
                SingleIListTNullableNull = null,
                DoubleIListTNullableNull = null,
                DecimalIListTNullableNull = null,
                CharIListTNullableNull = null,
                DateTimeIListTNullableNull = null,
                DateTimeOffsetIListTNullableNull = null,
                TimeSpanIListTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListTNullableNull = null,
                TimeOnlyIListTNullableNull = null,
#endif
                GuidIListTNullableNull = null,

                StringIListT = new List<string>() { "Hello", "World", null, "", "People" },
                StringIListTEmpty = new List<string>(0),
                StringIListTNull = null,

                EnumIListT = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIListTEmpty = new List<EnumModel>(0),
                EnumIListTNull = null,

                EnumIListTNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIListTNullableEmpty = new List<EnumModel?>(0),
                EnumIListTNullableNull = null,

                #endregion

                #region IReadOnlyListTs

                BooleanIReadOnlyListT = new List<bool>() { true, false, true },
                ByteIReadOnlyListT = new List<byte>() { 1, 2, 3 },
                SByteIReadOnlyListT = new List<sbyte>() { 4, 5, 6 },
                Int16IReadOnlyListT = new List<short>() { 7, 8, 9 },
                UInt16IReadOnlyListT = new List<ushort>() { 10, 11, 12 },
                Int32IReadOnlyListT = new List<int>() { 13, 14, 15 },
                UInt32IReadOnlyListT = new List<uint>() { 16, 17, 18 },
                Int64IReadOnlyListT = new List<long>() { 19, 20, 21 },
                UInt64IReadOnlyListT = new List<ulong>() { 22, 23, 24 },
                SingleIReadOnlyListT = new List<float>() { 25, 26, 27 },
                DoubleIReadOnlyListT = new List<double>() { 28, 29, 30 },
                DecimalIReadOnlyListT = new List<decimal>() { 31, 32, 33 },
                CharIReadOnlyListT = new List<char>() { 'A', 'B', 'C' },
                DateTimeIReadOnlyListT = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlyListT = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlyListT = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListT = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlyListT = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlyListT = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIReadOnlyListTEmpty = new List<bool>(0),
                ByteIReadOnlyListTEmpty = new List<byte>(0),
                SByteIReadOnlyListTEmpty = new List<sbyte>(0),
                Int16IReadOnlyListTEmpty = new List<short>(0),
                UInt16IReadOnlyListTEmpty = new List<ushort>(0),
                Int32IReadOnlyListTEmpty = new List<int>(0),
                UInt32IReadOnlyListTEmpty = new List<uint>(0),
                Int64IReadOnlyListTEmpty = new List<long>(0),
                UInt64IReadOnlyListTEmpty = new List<ulong>(0),
                SingleIReadOnlyListTEmpty = new List<float>(0),
                DoubleIReadOnlyListTEmpty = new List<double>(0),
                DecimalIReadOnlyListTEmpty = new List<decimal>(0),
                CharIReadOnlyListTEmpty = new List<char>(0),
                DateTimeIReadOnlyListTEmpty = new List<DateTime>(0),
                DateTimeOffsetIReadOnlyListTEmpty = new List<DateTimeOffset>(0),
                TimeSpanIReadOnlyListTEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListTEmpty = new List<DateOnly>(0),
                TimeOnlyIReadOnlyListTEmpty = new List<TimeOnly>(0),
#endif
                GuidIReadOnlyListTEmpty = new List<Guid>(0),

                BooleanIReadOnlyListTNull = null,
                ByteIReadOnlyListTNull = null,
                SByteIReadOnlyListTNull = null,
                Int16IReadOnlyListTNull = null,
                UInt16IReadOnlyListTNull = null,
                Int32IReadOnlyListTNull = null,
                UInt32IReadOnlyListTNull = null,
                Int64IReadOnlyListTNull = null,
                UInt64IReadOnlyListTNull = null,
                SingleIReadOnlyListTNull = null,
                DoubleIReadOnlyListTNull = null,
                DecimalIReadOnlyListTNull = null,
                CharIReadOnlyListTNull = null,
                DateTimeIReadOnlyListTNull = null,
                DateTimeOffsetIReadOnlyListTNull = null,
                TimeSpanIReadOnlyListTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListTNull = null,
                TimeOnlyIReadOnlyListTNull = null,
#endif
                GuidIReadOnlyListTNull = null,

                BooleanIReadOnlyListTNullable = new List<bool?>() { true, null, true },
                ByteIReadOnlyListTNullable = new List<byte?>() { 1, null, 3 },
                SByteIReadOnlyListTNullable = new List<sbyte?>() { 4, null, 6 },
                Int16IReadOnlyListTNullable = new List<short?>() { 7, null, 9 },
                UInt16IReadOnlyListTNullable = new List<ushort?>() { 10, null, 12 },
                Int32IReadOnlyListTNullable = new List<int?>() { 13, null, 15 },
                UInt32IReadOnlyListTNullable = new List<uint?>() { 16, null, 18 },
                Int64IReadOnlyListTNullable = new List<long?>() { 19, null, 21 },
                UInt64IReadOnlyListTNullable = new List<ulong?>() { 22, null, 24 },
                SingleIReadOnlyListTNullable = new List<float?>() { 25, null, 27 },
                DoubleIReadOnlyListTNullable = new List<double?>() { 28, null, 30 },
                DecimalIReadOnlyListTNullable = new List<decimal?>() { 31, null, 33 },
                CharIReadOnlyListTNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeIReadOnlyListTNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlyListTNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlyListTNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListTNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlyListTNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlyListTNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIReadOnlyListTNullableEmpty = new List<bool?>(0),
                ByteIReadOnlyListTNullableEmpty = new List<byte?>(0),
                SByteIReadOnlyListTNullableEmpty = new List<sbyte?>(0),
                Int16IReadOnlyListTNullableEmpty = new List<short?>(0),
                UInt16IReadOnlyListTNullableEmpty = new List<ushort?>(0),
                Int32IReadOnlyListTNullableEmpty = new List<int?>(0),
                UInt32IReadOnlyListTNullableEmpty = new List<uint?>(0),
                Int64IReadOnlyListTNullableEmpty = new List<long?>(0),
                UInt64IReadOnlyListTNullableEmpty = new List<ulong?>(0),
                SingleIReadOnlyListTNullableEmpty = new List<float?>(0),
                DoubleIReadOnlyListTNullableEmpty = new List<double?>(0),
                DecimalIReadOnlyListTNullableEmpty = new List<decimal?>(0),
                CharIReadOnlyListTNullableEmpty = new List<char?>(0),
                DateTimeIReadOnlyListTNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetIReadOnlyListTNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanIReadOnlyListTNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListTNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyIReadOnlyListTNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidIReadOnlyListTNullableEmpty = new List<Guid?>(0),

                BooleanIReadOnlyListTNullableNull = null,
                ByteIReadOnlyListTNullableNull = null,
                SByteIReadOnlyListTNullableNull = null,
                Int16IReadOnlyListTNullableNull = null,
                UInt16IReadOnlyListTNullableNull = null,
                Int32IReadOnlyListTNullableNull = null,
                UInt32IReadOnlyListTNullableNull = null,
                Int64IReadOnlyListTNullableNull = null,
                UInt64IReadOnlyListTNullableNull = null,
                SingleIReadOnlyListTNullableNull = null,
                DoubleIReadOnlyListTNullableNull = null,
                DecimalIReadOnlyListTNullableNull = null,
                CharIReadOnlyListTNullableNull = null,
                DateTimeIReadOnlyListTNullableNull = null,
                DateTimeOffsetIReadOnlyListTNullableNull = null,
                TimeSpanIReadOnlyListTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyListTNullableNull = null,
                TimeOnlyIReadOnlyListTNullableNull = null,
#endif
                GuidIReadOnlyListTNullableNull = null,

                StringIReadOnlyListT = new List<string>() { "Hello", "World", null, "", "People" },
                StringIReadOnlyListTEmpty = new List<string>(0),
                StringIReadOnlyListTNull = null,

                EnumIReadOnlyListT = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIReadOnlyListTEmpty = new List<EnumModel>(0),
                EnumIReadOnlyListTNull = null,

                EnumIReadOnlyListTNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIReadOnlyListTNullableEmpty = new List<EnumModel?>(0),
                EnumIReadOnlyListTNullableNull = null,

                #endregion

                #region ICollectionTs

                BooleanICollectionT = new List<bool>() { true, false, true },
                ByteICollectionT = new List<byte>() { 1, 2, 3 },
                SByteICollectionT = new List<sbyte>() { 4, 5, 6 },
                Int16ICollectionT = new List<short>() { 7, 8, 9 },
                UInt16ICollectionT = new List<ushort>() { 10, 11, 12 },
                Int32ICollectionT = new List<int>() { 13, 14, 15 },
                UInt32ICollectionT = new List<uint>() { 16, 17, 18 },
                Int64ICollectionT = new List<long>() { 19, 20, 21 },
                UInt64ICollectionT = new List<ulong>() { 22, 23, 24 },
                SingleICollectionT = new List<float>() { 25, 26, 27 },
                DoubleICollectionT = new List<double>() { 28, 29, 30 },
                DecimalICollectionT = new List<decimal>() { 31, 32, 33 },
                CharICollectionT = new List<char>() { 'A', 'B', 'C' },
                DateTimeICollectionT = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollectionT = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollectionT = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollectionT = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollectionT = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollectionT = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanICollectionTEmpty = new List<bool>(0),
                ByteICollectionTEmpty = new List<byte>(0),
                SByteICollectionTEmpty = new List<sbyte>(0),
                Int16ICollectionTEmpty = new List<short>(0),
                UInt16ICollectionTEmpty = new List<ushort>(0),
                Int32ICollectionTEmpty = new List<int>(0),
                UInt32ICollectionTEmpty = new List<uint>(0),
                Int64ICollectionTEmpty = new List<long>(0),
                UInt64ICollectionTEmpty = new List<ulong>(0),
                SingleICollectionTEmpty = new List<float>(0),
                DoubleICollectionTEmpty = new List<double>(0),
                DecimalICollectionTEmpty = new List<decimal>(0),
                CharICollectionTEmpty = new List<char>(0),
                DateTimeICollectionTEmpty = new List<DateTime>(0),
                DateTimeOffsetICollectionTEmpty = new List<DateTimeOffset>(0),
                TimeSpanICollectionTEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionTEmpty = new List<DateOnly>(0),
                TimeOnlyICollectionTEmpty = new List<TimeOnly>(0),
#endif
                GuidICollectionTEmpty = new List<Guid>(0),

                BooleanICollectionTNull = null,
                ByteICollectionTNull = null,
                SByteICollectionTNull = null,
                Int16ICollectionTNull = null,
                UInt16ICollectionTNull = null,
                Int32ICollectionTNull = null,
                UInt32ICollectionTNull = null,
                Int64ICollectionTNull = null,
                UInt64ICollectionTNull = null,
                SingleICollectionTNull = null,
                DoubleICollectionTNull = null,
                DecimalICollectionTNull = null,
                CharICollectionTNull = null,
                DateTimeICollectionTNull = null,
                DateTimeOffsetICollectionTNull = null,
                TimeSpanICollectionTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNull = null,
                TimeOnlyICollectionTNull = null,
#endif
                GuidICollectionTNull = null,

                BooleanICollectionTNullable = new List<bool?>() { true, null, true },
                ByteICollectionTNullable = new List<byte?>() { 1, null, 3 },
                SByteICollectionTNullable = new List<sbyte?>() { 4, null, 6 },
                Int16ICollectionTNullable = new List<short?>() { 7, null, 9 },
                UInt16ICollectionTNullable = new List<ushort?>() { 10, null, 12 },
                Int32ICollectionTNullable = new List<int?>() { 13, null, 15 },
                UInt32ICollectionTNullable = new List<uint?>() { 16, null, 18 },
                Int64ICollectionTNullable = new List<long?>() { 19, null, 21 },
                UInt64ICollectionTNullable = new List<ulong?>() { 22, null, 24 },
                SingleICollectionTNullable = new List<float?>() { 25, null, 27 },
                DoubleICollectionTNullable = new List<double?>() { 28, null, 30 },
                DecimalICollectionTNullable = new List<decimal?>() { 31, null, 33 },
                CharICollectionTNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeICollectionTNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollectionTNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollectionTNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollectionTNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollectionTNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanICollectionTNullableEmpty = new List<bool?>(0),
                ByteICollectionTNullableEmpty = new List<byte?>(0),
                SByteICollectionTNullableEmpty = new List<sbyte?>(0),
                Int16ICollectionTNullableEmpty = new List<short?>(0),
                UInt16ICollectionTNullableEmpty = new List<ushort?>(0),
                Int32ICollectionTNullableEmpty = new List<int?>(0),
                UInt32ICollectionTNullableEmpty = new List<uint?>(0),
                Int64ICollectionTNullableEmpty = new List<long?>(0),
                UInt64ICollectionTNullableEmpty = new List<ulong?>(0),
                SingleICollectionTNullableEmpty = new List<float?>(0),
                DoubleICollectionTNullableEmpty = new List<double?>(0),
                DecimalICollectionTNullableEmpty = new List<decimal?>(0),
                CharICollectionTNullableEmpty = new List<char?>(0),
                DateTimeICollectionTNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetICollectionTNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanICollectionTNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyICollectionTNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidICollectionTNullableEmpty = new List<Guid?>(0),

                BooleanICollectionTNullableNull = null,
                ByteICollectionTNullableNull = null,
                SByteICollectionTNullableNull = null,
                Int16ICollectionTNullableNull = null,
                UInt16ICollectionTNullableNull = null,
                Int32ICollectionTNullableNull = null,
                UInt32ICollectionTNullableNull = null,
                Int64ICollectionTNullableNull = null,
                UInt64ICollectionTNullableNull = null,
                SingleICollectionTNullableNull = null,
                DoubleICollectionTNullableNull = null,
                DecimalICollectionTNullableNull = null,
                CharICollectionTNullableNull = null,
                DateTimeICollectionTNullableNull = null,
                DateTimeOffsetICollectionTNullableNull = null,
                TimeSpanICollectionTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullableNull = null,
                TimeOnlyICollectionTNullableNull = null,
#endif
                GuidICollectionTNullableNull = null,

                StringICollectionT = new List<string>() { "Hello", "World", null, "", "People" },
                StringICollectionTEmpty = new List<string>(0),
                StringICollectionTNull = null,

                EnumICollectionT = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumICollectionTEmpty = new List<EnumModel>(0),
                EnumICollectionTNull = null,

                EnumICollectionTNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumICollectionTNullableEmpty = new List<EnumModel?>(0),
                EnumICollectionTNullableNull = null,

                #endregion

                #region IReadOnlyCollectionTs

                BooleanIReadOnlyCollectionT = new List<bool>() { true, false, true },
                ByteIReadOnlyCollectionT = new List<byte>() { 1, 2, 3 },
                SByteIReadOnlyCollectionT = new List<sbyte>() { 4, 5, 6 },
                Int16IReadOnlyCollectionT = new List<short>() { 7, 8, 9 },
                UInt16IReadOnlyCollectionT = new List<ushort>() { 10, 11, 12 },
                Int32IReadOnlyCollectionT = new List<int>() { 13, 14, 15 },
                UInt32IReadOnlyCollectionT = new List<uint>() { 16, 17, 18 },
                Int64IReadOnlyCollectionT = new List<long>() { 19, 20, 21 },
                UInt64IReadOnlyCollectionT = new List<ulong>() { 22, 23, 24 },
                SingleIReadOnlyCollectionT = new List<float>() { 25, 26, 27 },
                DoubleIReadOnlyCollectionT = new List<double>() { 28, 29, 30 },
                DecimalIReadOnlyCollectionT = new List<decimal>() { 31, 32, 33 },
                CharIReadOnlyCollectionT = new List<char>() { 'A', 'B', 'C' },
                DateTimeIReadOnlyCollectionT = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlyCollectionT = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlyCollectionT = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionT = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlyCollectionT = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlyCollectionT = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIReadOnlyCollectionTEmpty = new List<bool>(0),
                ByteIReadOnlyCollectionTEmpty = new List<byte>(0),
                SByteIReadOnlyCollectionTEmpty = new List<sbyte>(0),
                Int16IReadOnlyCollectionTEmpty = new List<short>(0),
                UInt16IReadOnlyCollectionTEmpty = new List<ushort>(0),
                Int32IReadOnlyCollectionTEmpty = new List<int>(0),
                UInt32IReadOnlyCollectionTEmpty = new List<uint>(0),
                Int64IReadOnlyCollectionTEmpty = new List<long>(0),
                UInt64IReadOnlyCollectionTEmpty = new List<ulong>(0),
                SingleIReadOnlyCollectionTEmpty = new List<float>(0),
                DoubleIReadOnlyCollectionTEmpty = new List<double>(0),
                DecimalIReadOnlyCollectionTEmpty = new List<decimal>(0),
                CharIReadOnlyCollectionTEmpty = new List<char>(0),
                DateTimeIReadOnlyCollectionTEmpty = new List<DateTime>(0),
                DateTimeOffsetIReadOnlyCollectionTEmpty = new List<DateTimeOffset>(0),
                TimeSpanIReadOnlyCollectionTEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionTEmpty = new List<DateOnly>(0),
                TimeOnlyIReadOnlyCollectionTEmpty = new List<TimeOnly>(0),
#endif
                GuidIReadOnlyCollectionTEmpty = new List<Guid>(0),

                BooleanIReadOnlyCollectionTNull = null,
                ByteIReadOnlyCollectionTNull = null,
                SByteIReadOnlyCollectionTNull = null,
                Int16IReadOnlyCollectionTNull = null,
                UInt16IReadOnlyCollectionTNull = null,
                Int32IReadOnlyCollectionTNull = null,
                UInt32IReadOnlyCollectionTNull = null,
                Int64IReadOnlyCollectionTNull = null,
                UInt64IReadOnlyCollectionTNull = null,
                SingleIReadOnlyCollectionTNull = null,
                DoubleIReadOnlyCollectionTNull = null,
                DecimalIReadOnlyCollectionTNull = null,
                CharIReadOnlyCollectionTNull = null,
                DateTimeIReadOnlyCollectionTNull = null,
                DateTimeOffsetIReadOnlyCollectionTNull = null,
                TimeSpanIReadOnlyCollectionTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionTNull = null,
                TimeOnlyIReadOnlyCollectionTNull = null,
#endif
                GuidIReadOnlyCollectionTNull = null,

                BooleanIReadOnlyCollectionTNullable = new List<bool?>() { true, null, true },
                ByteIReadOnlyCollectionTNullable = new List<byte?>() { 1, null, 3 },
                SByteIReadOnlyCollectionTNullable = new List<sbyte?>() { 4, null, 6 },
                Int16IReadOnlyCollectionTNullable = new List<short?>() { 7, null, 9 },
                UInt16IReadOnlyCollectionTNullable = new List<ushort?>() { 10, null, 12 },
                Int32IReadOnlyCollectionTNullable = new List<int?>() { 13, null, 15 },
                UInt32IReadOnlyCollectionTNullable = new List<uint?>() { 16, null, 18 },
                Int64IReadOnlyCollectionTNullable = new List<long?>() { 19, null, 21 },
                UInt64IReadOnlyCollectionTNullable = new List<ulong?>() { 22, null, 24 },
                SingleIReadOnlyCollectionTNullable = new List<float?>() { 25, null, 27 },
                DoubleIReadOnlyCollectionTNullable = new List<double?>() { 28, null, 30 },
                DecimalIReadOnlyCollectionTNullable = new List<decimal?>() { 31, null, 33 },
                CharIReadOnlyCollectionTNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeIReadOnlyCollectionTNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlyCollectionTNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlyCollectionTNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionTNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlyCollectionTNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlyCollectionTNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIReadOnlyCollectionTNullableEmpty = new List<bool?>(0),
                ByteIReadOnlyCollectionTNullableEmpty = new List<byte?>(0),
                SByteIReadOnlyCollectionTNullableEmpty = new List<sbyte?>(0),
                Int16IReadOnlyCollectionTNullableEmpty = new List<short?>(0),
                UInt16IReadOnlyCollectionTNullableEmpty = new List<ushort?>(0),
                Int32IReadOnlyCollectionTNullableEmpty = new List<int?>(0),
                UInt32IReadOnlyCollectionTNullableEmpty = new List<uint?>(0),
                Int64IReadOnlyCollectionTNullableEmpty = new List<long?>(0),
                UInt64IReadOnlyCollectionTNullableEmpty = new List<ulong?>(0),
                SingleIReadOnlyCollectionTNullableEmpty = new List<float?>(0),
                DoubleIReadOnlyCollectionTNullableEmpty = new List<double?>(0),
                DecimalIReadOnlyCollectionTNullableEmpty = new List<decimal?>(0),
                CharIReadOnlyCollectionTNullableEmpty = new List<char?>(0),
                DateTimeIReadOnlyCollectionTNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetIReadOnlyCollectionTNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanIReadOnlyCollectionTNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionTNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyIReadOnlyCollectionTNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidIReadOnlyCollectionTNullableEmpty = new List<Guid?>(0),

                BooleanIReadOnlyCollectionTNullableNull = null,
                ByteIReadOnlyCollectionTNullableNull = null,
                SByteIReadOnlyCollectionTNullableNull = null,
                Int16IReadOnlyCollectionTNullableNull = null,
                UInt16IReadOnlyCollectionTNullableNull = null,
                Int32IReadOnlyCollectionTNullableNull = null,
                UInt32IReadOnlyCollectionTNullableNull = null,
                Int64IReadOnlyCollectionTNullableNull = null,
                UInt64IReadOnlyCollectionTNullableNull = null,
                SingleIReadOnlyCollectionTNullableNull = null,
                DoubleIReadOnlyCollectionTNullableNull = null,
                DecimalIReadOnlyCollectionTNullableNull = null,
                CharIReadOnlyCollectionTNullableNull = null,
                DateTimeIReadOnlyCollectionTNullableNull = null,
                DateTimeOffsetIReadOnlyCollectionTNullableNull = null,
                TimeSpanIReadOnlyCollectionTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlyCollectionTNullableNull = null,
                TimeOnlyIReadOnlyCollectionTNullableNull = null,
#endif
                GuidIReadOnlyCollectionTNullableNull = null,

                StringIReadOnlyCollectionT = new List<string>() { "Hello", "World", null, "", "People" },
                StringIReadOnlyCollectionTEmpty = new List<string>(0),
                StringIReadOnlyCollectionTNull = null,

                EnumIReadOnlyCollectionT = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIReadOnlyCollectionTEmpty = new List<EnumModel>(0),
                EnumIReadOnlyCollectionTNull = null,

                EnumIReadOnlyCollectionTNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIReadOnlyCollectionTNullableEmpty = new List<EnumModel?>(0),
                EnumIReadOnlyCollectionTNullableNull = null,

                #endregion

                #region Objects

                ClassThing = new SimpleModel { Value1 = 1234, Value2 = "S-1234" },
                ClassThingNull = null,

                ClassArray = [new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" }],
                ClassArrayEmpty = [],
                ClassArrayNull = null,

                ClassEnumerable = new List<SimpleModel>() { new() { Value1 = 12, Value2 = "S-12" }, new() { Value1 = 13, Value2 = "S-13" } },

                ClassList = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
                ClassListEmpty = new List<SimpleModel>(0),
                ClassListNull = null,

                ClassIList = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
                ClassIListEmpty = new List<SimpleModel>(0),
                ClassIListNull = null,

                ClassIReadOnlyList = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
                ClassIReadOnlyListEmpty = new List<SimpleModel>(0),
                ClassIReadOnlyListNull = null,

                ClassICollection = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
                ClassICollectionEmpty = new List<SimpleModel>(0),
                ClassICollectionNull = null,

                ClassIReadOnlyCollection = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
                ClassIReadOnlyCollectionEmpty = new List<SimpleModel>(0),
                ClassIReadOnlyCollectionNull = null,

                #endregion

                #region Dictionaries

                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing3 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing4 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing5 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing6 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing7 = new ConcurrentDictionary<int, string>(new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } }),
                DictionaryThing8 = new ConcurrentDictionary<int, SimpleModel>(new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } }),

                #endregion

                StringArrayOfArrayThing = [["a", "b", "c"], null, ["d", "e", "f"], ["", null, ""]]
            };
            return model;
        }
    }
}