// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository.Test
{
    [Entity("TestTypes")]
    public abstract class BaseTestTypesModel<T> where T : BaseTestRelationsModel, new()
    {
        [Identity]
        public Guid KeyA { get; set; }
        [Identity(true)]
        public int KeyB { get; set; }

        public byte ByteThing { get; set; }
        public short Int16Thing { get; set; }
        public int Int32Thing { get; set; }
        public long Int64Thing { get; set; }
        public float SingleThing { get; set; }
        public double DoubleThing { get; set; }
        public decimal DecimalThing { get; set; }
        public char CharThing { get; set; }
        [StoreProperties(true, 6)]
        public DateTime DateTimeThing { get; set; }
        [StoreProperties(true, 6)]
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        [StoreProperties(true, 6)]
        public TimeSpan TimeSpanThing { get; set; }
        public Guid GuidThing { get; set; }

        public byte? ByteNullableThing { get; set; }
        public short? Int16NullableThing { get; set; }
        public int? Int32NullableThing { get; set; }
        public long? Int64NullableThing { get; set; }
        public float? SingleNullableThing { get; set; }
        public double? DoubleNullableThing { get; set; }
        public decimal? DecimalNullableThing { get; set; }
        public char? CharNullableThing { get; set; }
        [StoreProperties(false, 6)]
        public DateTime? DateTimeNullableThing { get; set; }
        [StoreProperties(false, 6)]
        public DateTimeOffset? DateTimeOffsetNullableThing { get; set; }
        [StoreProperties(false, 6)]
        public TimeSpan? TimeSpanNullableThing { get; set; }
        public Guid? GuidNullableThing { get; set; }

        public byte? ByteNullableThingNull { get; set; }
        public short? Int16NullableThingNull { get; set; }
        public int? Int32NullableThingNull { get; set; }
        public long? Int64NullableThingNull { get; set; }
        public float? SingleNullableThingNull { get; set; }
        public double? DoubleNullableThingNull { get; set; }
        public decimal? DecimalNullableThingNull { get; set; }
        public char? CharNullableThingNull { get; set; }
        [StoreProperties(false, 6)]
        public DateTime? DateTimeNullableThingNull { get; set; }
        [StoreProperties(false, 6)]
        public DateTimeOffset? DateTimeOffsetNullableThingNull { get; set; }
        [StoreProperties(false, 6)]
        public TimeSpan? TimeSpanNullableThingNull { get; set; }
        public Guid? GuidNullableThingNull { get; set; }

        public string StringThing { get; set; }
        public string StringThingNull { get; set; }

        public byte[] BytesThing { get; set; }
        [StoreProperties(false)]
        public byte[] BytesThingNull { get; set; }

        public Guid? RelationAKey { get; set; }

        [Relation("RelationAKey")]
        public T RelationA { get; set; }

        [Relation("RelationKey")]
        public T[] RelationB { get; set; }
    }
}
