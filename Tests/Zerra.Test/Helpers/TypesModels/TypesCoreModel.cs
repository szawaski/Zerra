// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesCoreModel
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

        public string? StringThing { get; set; }

        public static TypesCoreModel Create()
        {
            var model = new TypesCoreModel()
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
                DateTimeThing = DateTime.UtcNow,
                DateTimeOffsetThing = DateTimeOffset.UtcNow.AddDays(1),
                TimeSpanThing = -(new TimeSpan(2, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond, 1)),
#if NET6_0_OR_GREATER
                DateOnlyThing = DateOnly.FromDateTime(DateTime.UtcNow),
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
                TimeOnlyThingNullable = TimeOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
#endif
                GuidThingNullable = Guid.NewGuid(),

                StringThing = "abcdefghijklmnopqrstuvwxy\r\n\tz"
            };
            return model;
        }

        public static void AssertAreEqual(TypesCoreModel model1, TypesCoreAsStringsModel model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);
            Assert.NotEqual<object>(model1, model2);

            Assert.Equal(model1.BooleanThing.ToString(), model2.BooleanThing);
            Assert.Equal(model1.ByteThing.ToString(), model2.ByteThing);
            Assert.Equal(model1.SByteThing.ToString(), model2.SByteThing);
            Assert.Equal(model1.Int16Thing.ToString(), model2.Int16Thing);
            Assert.Equal(model1.UInt16Thing.ToString(), model2.UInt16Thing);
            Assert.Equal(model1.Int32Thing.ToString(), model2.Int32Thing);
            Assert.Equal(model1.UInt32Thing.ToString(), model2.UInt32Thing);
            Assert.Equal(model1.Int64Thing.ToString(), model2.Int64Thing);
            Assert.Equal(model1.UInt64Thing.ToString(), model2.UInt64Thing);
            Assert.Equal(model1.SingleThing.ToString(), model2.SingleThing);
            Assert.Equal(model1.DoubleThing.ToString(), model2.DoubleThing);
            Assert.Equal(model1.DecimalThing.ToString(), model2.DecimalThing);
            Assert.Equal(model1.CharThing.ToString(), model2.CharThing);
            Assert.Equal(model1.DateTimeThing.ToString(), model2.DateTimeThing);
            Assert.Equal(model1.DateTimeOffsetThing.ToString(), model2.DateTimeOffsetThing);
            Assert.Equal(model1.TimeSpanThing.ToString(), model2.TimeSpanThing);
#if NET6_0_OR_GREATER
            Assert.Equal(model1.DateOnlyThing.ToString(), model2.DateOnlyThing);
            Assert.Equal(model1.TimeOnlyThing.ToString(), model2.TimeOnlyThing);
#endif
            Assert.Equal(model1.GuidThing.ToString(), model2.GuidThing);

            Assert.Equal(model1.BooleanThingNullable?.ToString(), model2.BooleanThingNullable);
            Assert.Equal(model1.ByteThingNullable?.ToString(), model2.ByteThingNullable);
            Assert.Equal(model1.SByteThingNullable?.ToString(), model2.SByteThingNullable);
            Assert.Equal(model1.Int16ThingNullable?.ToString(), model2.Int16ThingNullable);
            Assert.Equal(model1.UInt16ThingNullable?.ToString(), model2.UInt16ThingNullable);
            Assert.Equal(model1.Int32ThingNullable?.ToString(), model2.Int32ThingNullable);
            Assert.Equal(model1.UInt32ThingNullable?.ToString(), model2.UInt32ThingNullable);
            Assert.Equal(model1.Int64ThingNullable?.ToString(), model2.Int64ThingNullable);
            Assert.Equal(model1.UInt64ThingNullable?.ToString(), model2.UInt64ThingNullable);
            Assert.Equal(model1.SingleThingNullable?.ToString(), model2.SingleThingNullable);
            Assert.Equal(model1.DoubleThingNullable?.ToString(), model2.DoubleThingNullable);
            Assert.Equal(model1.DecimalThingNullable?.ToString(), model2.DecimalThingNullable);
            Assert.Equal(model1.CharThingNullable?.ToString(), model2.CharThingNullable);
            Assert.Equal(model1.DateTimeThingNullable?.ToString(), model2.DateTimeThingNullable);
            Assert.Equal(model1.DateTimeOffsetThingNullable?.ToString(), model2.DateTimeOffsetThingNullable);
            Assert.Equal(model1.TimeSpanThingNullable?.ToString(), model2.TimeSpanThingNullable);
#if NET6_0_OR_GREATER
            Assert.Equal(model1.DateOnlyThingNullable.ToString(), model2.DateOnlyThingNullable);
            Assert.Equal(model1.TimeOnlyThingNullable.ToString(), model2.TimeOnlyThingNullable);
#endif
            Assert.Equal(model1.GuidThingNullable?.ToString(), model2.GuidThingNullable);
        }
    }
}