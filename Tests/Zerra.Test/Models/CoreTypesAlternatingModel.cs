// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Test
{
    public class CoreTypesAlternatingModel
    {
        public bool BooleanThing { get; set; }
        public sbyte SByteThing { get; set; }
        public ushort UInt16Thing { get; set; }
        public uint UInt32Thing { get; set; }
        public ulong UInt64Thing { get; set; }
        public double DoubleThing { get; set; }
        public char CharThing { get; set; }
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        public Guid GuidThing { get; set; }

        public byte? ByteThingNullable { get; set; }
        public short? Int16ThingNullable { get; set; }
        public int? Int32ThingNullable { get; set; }
        public long? Int64ThingNullable { get; set; }
        public float? SingleThingNullable { get; set; }
        public decimal? DecimalThingNullable { get; set; }
        public DateTime? DateTimeThingNullable { get; set; }
        public TimeSpan? TimeSpanThingNullable { get; set; }

        public static CoreTypesAlternatingModel Create()
        {
            var model = new CoreTypesAlternatingModel()
            {
                BooleanThing = true,
                SByteThing = -2,
                UInt16Thing = 4,
                UInt32Thing = 6,
                UInt64Thing = 8,
                DoubleThing = -10.2,
                CharThing = 'Z',
                DateTimeOffsetThing = DateTimeOffset.UtcNow.AddDays(1),
                GuidThing = Guid.NewGuid(),

                ByteThingNullable = 11,
                Int16ThingNullable = -13,
                Int32ThingNullable = -15,
                Int64ThingNullable = -17,
                SingleThingNullable = -19.1f,
                DecimalThingNullable = -111.3m,
                DateTimeThingNullable = DateTime.UtcNow.AddMonths(1),
                TimeSpanThingNullable = DateTime.UtcNow.AddHours(1).TimeOfDay,
            };
            return model;
        }
    }
}