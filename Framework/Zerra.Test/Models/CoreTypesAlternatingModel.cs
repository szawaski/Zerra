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
    }
}