// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesBasicModel
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

        public SimpleModel ClassThing { get; set; }
        public SimpleModel ClassThingNull { get; set; }

        public static TypesBasicModel Create()
        {
            var model = new TypesBasicModel()
            {
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

                ClassThing = new SimpleModel { Value1 = 1234, Value2 = "S-1234" },
                ClassThingNull = null,
            };
            return model;
        }
    }
}