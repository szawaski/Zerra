// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Benchmark.EFData
{
    public sealed class EFTestTypesModel
    {
        public Guid KeyA { get; set; }
        public int KeyB { get; set; }

        public byte ByteThing { get; set; }
        public short Int16Thing { get; set; }
        public int Int32Thing { get; set; }
        public long Int64Thing { get; set; }
        public float SingleThing { get; set; }
        public double DoubleThing { get; set; }
        public decimal DecimalThing { get; set; }
        public char CharThing { get; set; }
        public DateTime DateTimeThing { get; set; }
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        public TimeSpan TimeSpanThing { get; set; }
        public DateOnly DateOnlyThing { get; set; }
        public TimeOnly TimeOnlyThing { get; set; }
        public Guid GuidThing { get; set; }

        public byte? ByteNullableThing { get; set; }
        public short? Int16NullableThing { get; set; }
        public int? Int32NullableThing { get; set; }
        public long? Int64NullableThing { get; set; }
        public float? SingleNullableThing { get; set; }
        public double? DoubleNullableThing { get; set; }
        public decimal? DecimalNullableThing { get; set; }
        public char? CharNullableThing { get; set; }
        public DateTime? DateTimeNullableThing { get; set; }
        public DateTimeOffset? DateTimeOffsetNullableThing { get; set; }
        public TimeSpan? TimeSpanNullableThing { get; set; }
        public DateOnly? DateOnlyNullableThing { get; set; }
        public TimeOnly? TimeOnlyNullableThing { get; set; }
        public Guid? GuidNullableThing { get; set; }

        public byte? ByteNullableThingNull { get; set; }
        public short? Int16NullableThingNull { get; set; }
        public int? Int32NullableThingNull { get; set; }
        public long? Int64NullableThingNull { get; set; }
        public float? SingleNullableThingNull { get; set; }
        public double? DoubleNullableThingNull { get; set; }
        public decimal? DecimalNullableThingNull { get; set; }
        public char? CharNullableThingNull { get; set; }
        public DateTime? DateTimeNullableThingNull { get; set; }
        public DateTimeOffset? DateTimeOffsetNullableThingNull { get; set; }
        public TimeSpan? TimeSpanNullableThingNull { get; set; }
        public DateOnly? DateOnlyNullableThingNull { get; set; }
        public TimeOnly? TimeOnlyNullableThingNull { get; set; }
        public Guid? GuidNullableThingNull { get; set; }

        public string? StringThing { get; set; }
        public string? StringThingNull { get; set; }

        public byte[]? BytesThing { get; set; }
        public byte[]? BytesThingNull { get; set; }

        public int? RelationAKey { get; set; }
        public EFTestRelationsModel? RelationA { get; set; }

        public ICollection<EFTestRelationsModel> RelationB { get; set; } = [];
    }
}
