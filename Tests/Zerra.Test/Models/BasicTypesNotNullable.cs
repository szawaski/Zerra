// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;

namespace Zerra.Test
{
    public class BasicTypesNotNullable
    {
        public bool BooleanThing { get; set; }
        public byte ByteThing { get; set; }
        public sbyte SByteThing { get; set; }
        public int Int16Thing { get; set; }
        public uint UInt16Thing { get; set; }
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

        public EnumModel EnumThing { get; set; }

        public static BasicTypesNotNullable Create()
        {
            var model = new BasicTypesNotNullable()
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
                TimeSpanThing = DateTime.UtcNow.TimeOfDay,
                DateOnlyThing = DateOnly.FromDateTime(DateTime.UtcNow),
                TimeOnlyThing = TimeOnly.FromDateTime(DateTime.UtcNow),
                GuidThing = Guid.NewGuid(),

                EnumThing = EnumModel.EnumItem1,
            };
            return model;
        }

        public static void AssertAreEqual(BasicTypesNotNullable model1, BasicTypesNotNullable model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);
            Assert.NotEqual(model1, model2);

            Assert.Equal(model1.BooleanThing, model2.BooleanThing);
            Assert.Equal(model1.ByteThing, model2.ByteThing);
            Assert.Equal(model1.SByteThing, model2.SByteThing);
            Assert.Equal(model1.Int16Thing, model2.Int16Thing);
            Assert.Equal(model1.UInt16Thing, model2.UInt16Thing);
            Assert.Equal(model1.Int32Thing, model2.Int32Thing);
            Assert.Equal(model1.UInt32Thing, model2.UInt32Thing);
            Assert.Equal(model1.Int64Thing, model2.Int64Thing);
            Assert.Equal(model1.UInt64Thing, model2.UInt64Thing);
            Assert.Equal(model1.SingleThing, model2.SingleThing);
            Assert.Equal(model1.DoubleThing, model2.DoubleThing);
            Assert.Equal(model1.DecimalThing, model2.DecimalThing);
            Assert.Equal(model1.CharThing, model2.CharThing);
            Assert.Equal(model1.DateTimeThing, model2.DateTimeThing);
            Assert.Equal(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.Equal(model1.TimeSpanThing, model2.TimeSpanThing);
            Assert.Equal(model1.GuidThing, model2.GuidThing);

            Assert.Equal(model1.EnumThing, model2.EnumThing);
        }
        public static void AssertAreEqual(BasicTypesNotNullable model1, BasicTypesNullable model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);
            Assert.NotEqual<object>(model1, model2);

            Assert.Equal(model1.BooleanThing, model2.BooleanThing.Value);
            Assert.Equal(model1.ByteThing, model2.ByteThing.Value);
            Assert.Equal(model1.SByteThing, model2.SByteThing.Value);
            Assert.Equal(model1.Int16Thing, model2.Int16Thing.Value);
            Assert.Equal(model1.UInt16Thing, model2.UInt16Thing.Value);
            Assert.Equal(model1.Int32Thing, model2.Int32Thing.Value);
            Assert.Equal(model1.UInt32Thing, model2.UInt32Thing.Value);
            Assert.Equal(model1.Int64Thing, model2.Int64Thing.Value);
            Assert.Equal(model1.UInt64Thing, model2.UInt64Thing.Value);
            Assert.Equal(model1.SingleThing, model2.SingleThing.Value);
            Assert.Equal(model1.DoubleThing, model2.DoubleThing.Value);
            Assert.Equal(model1.DecimalThing, model2.DecimalThing.Value);
            Assert.Equal(model1.CharThing, model2.CharThing.Value);
            Assert.Equal(model1.DateTimeThing, model2.DateTimeThing.Value);
            Assert.Equal(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing.Value);
            Assert.Equal(model1.TimeSpanThing, model2.TimeSpanThing.Value);
#if NET6_0_OR_GREATER
            Assert.Equal(model1.DateOnlyThing, model2.DateOnlyThing.Value);
            Assert.Equal(model1.TimeOnlyThing, model2.TimeOnlyThing.Value);
#endif
            Assert.Equal(model1.GuidThing, model2.GuidThing.Value);

            Assert.Equal(model1.EnumThing, model2.EnumThing.Value);
        }
    }
}
