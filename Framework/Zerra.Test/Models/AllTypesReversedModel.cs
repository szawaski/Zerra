// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class AllTypesReversedModel
    {
        public string[][] StringArrayOfArrayThing { get; set; }
        public Dictionary<int, string> DictionaryThing { get; set; }

        public List<BasicModel> ClassList { get; set; }
        public IEnumerable<BasicModel> ClassEnumerable { get; set; }
        public BasicModel[] ClassArray { get; set; }
        public BasicModel ClassThingNull { get; set; }
        public BasicModel ClassThing { get; set; }

        public List<EnumModel?> EnumListNullable { get; set; }
        public List<EnumModel> EnumList { get; set; }

        public List<string> StringList { get; set; }

        public List<Guid?> GuidListNullable { get; set; }
        public List<TimeSpan?> TimeSpanListNullable { get; set; }
        public List<DateTimeOffset?> DateTimeOffsetListNullable { get; set; }
        public List<DateTime?> DateTimeListNullable { get; set; }
        public List<char?> CharListNullable { get; set; }
        public List<decimal?> DecimalListNullable { get; set; }
        public List<double?> DoubleListNullable { get; set; }
        public List<float?> SingleListNullable { get; set; }
        public List<ulong?> UInt64ListNullable { get; set; }
        public List<long?> Int64ListNullable { get; set; }
        public List<uint?> UInt32ListNullable { get; set; }
        public List<int?> Int32ListNullable { get; set; }
        public List<ushort?> UInt16ListNullable { get; set; }
        public List<short?> Int16ListNullable { get; set; }
        public List<sbyte?> SByteListNullable { get; set; }
        public List<byte?> ByteListNullable { get; set; }
        public List<bool?> BooleanListNullable { get; set; }

        public List<Guid> GuidList { get; set; }
        public List<TimeSpan> TimeSpanList { get; set; }
        public List<DateTimeOffset> DateTimeOffsetList { get; set; }
        public List<DateTime> DateTimeList { get; set; }
        public List<char> CharList { get; set; }
        public List<decimal> DecimalList { get; set; }
        public List<double> DoubleList { get; set; }
        public List<float> SingleList { get; set; }
        public List<ulong> UInt64List { get; set; }
        public List<long> Int64List { get; set; }
        public List<uint> UInt32List { get; set; }
        public List<int> Int32List { get; set; }
        public List<ushort> UInt16List { get; set; }
        public List<short> Int16List { get; set; }
        public List<sbyte> SByteList { get; set; }
        public List<byte> ByteList { get; set; }
        public List<bool> BooleanList { get; set; }

        public EnumModel?[] EnumArrayNullable { get; set; }
        public EnumModel[] EnumArray { get; set; }

        public string[] StringEmptyArray { get; set; }
        public string[] StringArray { get; set; }

        public Guid?[] GuidArrayNullable { get; set; }
        public TimeSpan?[] TimeSpanArrayNullable { get; set; }
        public DateTimeOffset?[] DateTimeOffsetArrayNullable { get; set; }
        public DateTime?[] DateTimeArrayNullable { get; set; }
        public char?[] CharArrayNullable { get; set; }
        public decimal?[] DecimalArrayNullable { get; set; }
        public double?[] DoubleArrayNullable { get; set; }
        public float?[] SingleArrayNullable { get; set; }
        public ulong?[] UInt64ArrayNullable { get; set; }
        public long?[] Int64ArrayNullable { get; set; }
        public uint?[] UInt32ArrayNullable { get; set; }
        public int?[] Int32ArrayNullable { get; set; }
        public ushort?[] UInt16ArrayNullable { get; set; }
        public short?[] Int16ArrayNullable { get; set; }
        public sbyte?[] SByteArrayNullable { get; set; }
        public byte?[] ByteArrayNullable { get; set; }
        public bool?[] BooleanArrayNullable { get; set; }

        public Guid[] GuidArray { get; set; }
        public TimeSpan[] TimeSpanArray { get; set; }
        public DateTimeOffset[] DateTimeOffsetArray { get; set; }
        public DateTime[] DateTimeArray { get; set; }
        public char[] CharArray { get; set; }
        public decimal[] DecimalArray { get; set; }
        public double[] DoubleArray { get; set; }
        public float[] SingleArray { get; set; }
        public ulong[] UInt64Array { get; set; }
        public long[] Int64Array { get; set; }
        public uint[] UInt32Array { get; set; }
        public int[] Int32Array { get; set; }
        public ushort[] UInt16Array { get; set; }
        public short[] Int16Array { get; set; }
        public sbyte[] SByteArray { get; set; }
        public byte[] ByteArray { get; set; }
        public bool[] BooleanArray { get; set; }

        public EnumModel? EnumThingNullableNull { get; set; }
        public EnumModel? EnumThingNullable { get; set; }
        public EnumModel EnumThing { get; set; }

        public string StringThingEmpty { get; set; }
        public string StringThingNull { get; set; }
        public string StringThing { get; set; }

        public Guid? GuidThingNullableNull { get; set; }
        public TimeSpan? TimeSpanThingNullableNull { get; set; }
        public DateTimeOffset? DateTimeOffsetThingNullableNull { get; set; }
        public DateTime? DateTimeThingNullableNull { get; set; }
        public char? CharThingNullableNull { get; set; }
        public decimal? DecimalThingNullableNull { get; set; }
        public double? DoubleThingNullableNull { get; set; }
        public float? SingleThingNullableNull { get; set; }
        public ulong? UInt64ThingNullableNull { get; set; }
        public long? Int64ThingNullableNull { get; set; }
        public uint? UInt32ThingNullableNull { get; set; }
        public int? Int32ThingNullableNull { get; set; }
        public ushort? UInt16ThingNullableNull { get; set; }
        public short? Int16ThingNullableNull { get; set; }
        public sbyte? SByteThingNullableNull { get; set; }
        public byte? ByteThingNullableNull { get; set; }
        public bool? BooleanThingNullableNull { get; set; }

        public Guid? GuidThingNullable { get; set; }
        public TimeSpan? TimeSpanThingNullable { get; set; }
        public DateTimeOffset? DateTimeOffsetThingNullable { get; set; }
        public DateTime? DateTimeThingNullable { get; set; }
        public char? CharThingNullable { get; set; }
        public decimal? DecimalThingNullable { get; set; }
        public double? DoubleThingNullable { get; set; }
        public float? SingleThingNullable { get; set; }
        public ulong? UInt64ThingNullable { get; set; }
        public long? Int64ThingNullable { get; set; }
        public uint? UInt32ThingNullable { get; set; }
        public int? Int32ThingNullable { get; set; }
        public ushort? UInt16ThingNullable { get; set; }
        public short? Int16ThingNullable { get; set; }
        public sbyte? SByteThingNullable { get; set; }
        public byte? ByteThingNullable { get; set; }
        public bool? BooleanThingNullable { get; set; }

        public Guid GuidThing { get; set; }
        public TimeSpan TimeSpanThing { get; set; }
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        public DateTime DateTimeThing { get; set; }
        public char CharThing { get; set; }
        public decimal DecimalThing { get; set; }
        public double DoubleThing { get; set; }
        public float SingleThing { get; set; }
        public ulong UInt64Thing { get; set; }
        public long Int64Thing { get; set; }
        public uint UInt32Thing { get; set; }
        public int Int32Thing { get; set; }
        public ushort UInt16Thing { get; set; }
        public short Int16Thing { get; set; }
        public sbyte SByteThing { get; set; }
        public byte ByteThing { get; set; }
        public bool BooleanThing { get; set; }
    }
}
