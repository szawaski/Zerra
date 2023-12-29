// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class AllTypesModel
    {
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
        public Guid? GuidThingNullableNull { get; set; }

        public string StringThing { get; set; }
        public string StringThingNull { get; set; }
        public string StringThingEmpty { get; set; }

        public EnumModel EnumThing { get; set; }
        public EnumModel? EnumThingNullable { get; set; }
        public EnumModel? EnumThingNullableNull { get; set; }

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
        public Guid?[] GuidArrayNullableNull { get; set; }

        public string[] StringArray { get; set; }
        public string[] StringArrayEmpty { get; set; }
        public string[] StringArrayNull{ get; set; }

        public EnumModel[] EnumArray { get; set; }
        public EnumModel?[] EnumArrayNullable { get; set; }

        public List<bool> BooleanList { get; set; }
        public List<byte> ByteList { get; set; }
        public List<sbyte> SByteList { get; set; }
        public List<short> Int16List { get; set; }
        public List<ushort> UInt16List { get; set; }
        public List<int> Int32List { get; set; }
        public List<uint> UInt32List { get; set; }
        public List<long> Int64List { get; set; }
        public List<ulong> UInt64List { get; set; }
        public List<float> SingleList { get; set; }
        public List<double> DoubleList { get; set; }
        public List<decimal> DecimalList { get; set; }
        public List<char> CharList { get; set; }
        public List<DateTime> DateTimeList { get; set; }
        public List<DateTimeOffset> DateTimeOffsetList { get; set; }
        public List<TimeSpan> TimeSpanList { get; set; }
        public List<Guid> GuidList { get; set; }

        public List<bool> BooleanListEmpty { get; set; }
        public List<byte> ByteListEmpty { get; set; }
        public List<sbyte> SByteListEmpty { get; set; }
        public List<short> Int16ListEmpty { get; set; }
        public List<ushort> UInt16ListEmpty { get; set; }
        public List<int> Int32ListEmpty { get; set; }
        public List<uint> UInt32ListEmpty { get; set; }
        public List<long> Int64ListEmpty { get; set; }
        public List<ulong> UInt64ListEmpty { get; set; }
        public List<float> SingleListEmpty { get; set; }
        public List<double> DoubleListEmpty { get; set; }
        public List<decimal> DecimalListEmpty { get; set; }
        public List<char> CharListEmpty { get; set; }
        public List<DateTime> DateTimeListEmpty { get; set; }
        public List<DateTimeOffset> DateTimeOffsetListEmpty { get; set; }
        public List<TimeSpan> TimeSpanListEmpty { get; set; }
        public List<Guid> GuidListEmpty { get; set; }

        public List<bool> BooleanListNull { get; set; }
        public List<byte> ByteListNull { get; set; }
        public List<sbyte> SByteListNull { get; set; }
        public List<short> Int16ListNull { get; set; }
        public List<ushort> UInt16ListNull { get; set; }
        public List<int> Int32ListNull { get; set; }
        public List<uint> UInt32ListNull { get; set; }
        public List<long> Int64ListNull { get; set; }
        public List<ulong> UInt64ListNull { get; set; }
        public List<float> SingleListNull { get; set; }
        public List<double> DoubleListNull { get; set; }
        public List<decimal> DecimalListNull { get; set; }
        public List<char> CharListNull { get; set; }
        public List<DateTime> DateTimeListNull { get; set; }
        public List<DateTimeOffset> DateTimeOffsetListNull { get; set; }
        public List<TimeSpan> TimeSpanListNull { get; set; }
        public List<Guid> GuidListNull { get; set; }

        public List<bool?> BooleanListNullable { get; set; }
        public List<byte?> ByteListNullable { get; set; }
        public List<sbyte?> SByteListNullable { get; set; }
        public List<short?> Int16ListNullable { get; set; }
        public List<ushort?> UInt16ListNullable { get; set; }
        public List<int?> Int32ListNullable { get; set; }
        public List<uint?> UInt32ListNullable { get; set; }
        public List<long?> Int64ListNullable { get; set; }
        public List<ulong?> UInt64ListNullable { get; set; }
        public List<float?> SingleListNullable { get; set; }
        public List<double?> DoubleListNullable { get; set; }
        public List<decimal?> DecimalListNullable { get; set; }
        public List<char?> CharListNullable { get; set; }
        public List<DateTime?> DateTimeListNullable { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListNullable { get; set; }
        public List<TimeSpan?> TimeSpanListNullable { get; set; }
        public List<Guid?> GuidListNullable { get; set; }

        public List<bool?> BooleanListNullableEmpty { get; set; }
        public List<byte?> ByteListNullableEmpty { get; set; }
        public List<sbyte?> SByteListNullableEmpty { get; set; }
        public List<short?> Int16ListNullableEmpty { get; set; }
        public List<ushort?> UInt16ListNullableEmpty { get; set; }
        public List<int?> Int32ListNullableEmpty { get; set; }
        public List<uint?> UInt32ListNullableEmpty { get; set; }
        public List<long?> Int64ListNullableEmpty { get; set; }
        public List<ulong?> UInt64ListNullableEmpty { get; set; }
        public List<float?> SingleListNullableEmpty { get; set; }
        public List<double?> DoubleListNullableEmpty { get; set; }
        public List<decimal?> DecimalListNullableEmpty { get; set; }
        public List<char?> CharListNullableEmpty { get; set; }
        public List<DateTime?> DateTimeListNullableEmpty { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListNullableEmpty { get; set; }
        public List<TimeSpan?> TimeSpanListNullableEmpty { get; set; }
        public List<Guid?> GuidListNullableEmpty { get; set; }

        public List<bool?> BooleanListNullableNull { get; set; }
        public List<byte?> ByteListNullableNull { get; set; }
        public List<sbyte?> SByteListNullableNull { get; set; }
        public List<short?> Int16ListNullableNull { get; set; }
        public List<ushort?> UInt16ListNullableNull { get; set; }
        public List<int?> Int32ListNullableNull { get; set; }
        public List<uint?> UInt32ListNullableNull { get; set; }
        public List<long?> Int64ListNullableNull { get; set; }
        public List<ulong?> UInt64ListNullableNull { get; set; }
        public List<float?> SingleListNullableNull { get; set; }
        public List<double?> DoubleListNullableNull { get; set; }
        public List<decimal?> DecimalListNullableNull { get; set; }
        public List<char?> CharListNullableNull { get; set; }
        public List<DateTime?> DateTimeListNullableNull { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListNullableNull { get; set; }
        public List<TimeSpan?> TimeSpanListNullableNull { get; set; }
        public List<Guid?> GuidListNullableNull { get; set; }

        public List<string> StringList { get; set; }
        public List<string> StringListEmpty { get; set; }
        public List<string> StringListNull { get; set; }

        public List<EnumModel> EnumList { get; set; }
        public List<EnumModel> EnumListEmpty { get; set; }
        public List<EnumModel> EnumListNull { get; set; }

        public List<EnumModel?> EnumListNullable { get; set; }
        public List<EnumModel?> EnumListNullableEmpty { get; set; }
        public List<EnumModel?> EnumListNullableNull { get; set; }

        public BasicModel ClassThing { get; set; }
        public BasicModel ClassThingNull { get; set; }

        public BasicModel[] ClassArray { get; set; }
        public BasicModel[] ClassArrayEmpty { get; set; }
        public BasicModel[] ClassArrayNull { get; set; }

        public IEnumerable<BasicModel> ClassEnumerable { get; set; }

        public List<BasicModel> ClassList { get; set; }
        public List<BasicModel> ClassListEmpty { get; set; }
        public List<BasicModel> ClassListNull { get; set; }

        public IReadOnlyDictionary<int, string> DictionaryThing { get; set; }
        public string[][] StringArrayOfArrayThing { get; set; }
    }
}