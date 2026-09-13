// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    [Entity("TestTypes")]
    public sealed class TestTypesModel
    {
        [Identity(false)]
        public Guid KeyA { get; set; } = Guid.NewGuid();
        //[Identity(true)]
        public int KeyB { get; set; }

        public bool BooleanThing { get; set; }
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
        //no precision set, the store's default precision still keeps fractional seconds
        public DateTime DateTimeDefaultPrecisionThing { get; set; }
        [StoreProperties(true, 6)]
        public DateTimeOffset DateTimeOffsetThing { get; set; }
        [StoreProperties(true, 6)]
        public TimeSpan TimeSpanThing { get; set; }
        [StoreProperties(true, 6)]
        public DateOnly DateOnlyThing { get; set; }
        [StoreProperties(true, 6)]
        public TimeOnly TimeOnlyThing { get; set; }
        public Guid GuidThing { get; set; }

        public bool? BooleanNullableThing { get; set; }
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
        [StoreProperties(false, 6)]
        public DateOnly? DateOnlyNullableThing { get; set; }
        [StoreProperties(false, 6)]
        public TimeOnly? TimeOnlyNullableThing { get; set; }
        public Guid? GuidNullableThing { get; set; }

        public bool? BooleanNullableThingNull { get; set; }
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
        [StoreProperties(false, 6)]
        public DateOnly? DateOnlyNullableThingNull { get; set; }
        [StoreProperties(false, 6)]
        public TimeOnly? TimeOnlyNullableThingNull { get; set; }
        public Guid? GuidNullableThingNull { get; set; }

        public string StringThing { get; set; }
        public string StringThingNull { get; set; }
        [StoreProperties(true, 128)]
        public string StringLengthThing { get; set; }
        [StoreProperties(false, 64)]
        public string StringLengthThingNull { get; set; }

        public byte[] BytesThing { get; set; }
        [StoreProperties(false)]
        public byte[] BytesThingNull { get; set; }

        public int? RelationAKey { get; set; }

        [Relation(nameof(RelationAKey))]
        public TestRelationsModel RelationA { get; set; }

        [Relation(nameof(TestRelationsModel.RelationBKey))]
        public TestRelationsModel[] RelationB { get; set; }

        public static TestTypesModel Create()
        {
            var model = new TestTypesModel()
            {
                BooleanThing = true,
                ByteThing = 1,
                Int16Thing = -3,
                Int32Thing = -5,
                Int64Thing = -7,
                SingleThing = -9.1f,
                DoubleThing = -10.2,
                DecimalThing = -11.3m,
                CharThing = 'Z',
                DateTimeThing = DateTime.Now,
                DateTimeDefaultPrecisionThing = new DateTime(2024, 5, 6, 7, 8, 9, 456, DateTimeKind.Utc),
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                TimeSpanThing = DateTime.Now.TimeOfDay,
                DateOnlyThing = DateOnly.FromDateTime(DateTime.Now),
                TimeOnlyThing = TimeOnly.FromDateTime(DateTime.Now),
                GuidThing = Guid.NewGuid(),

                BooleanNullableThing = false,
                ByteNullableThing = 11,
                Int16NullableThing = -13,
                Int32NullableThing = -15,
                Int64NullableThing = -17,
                SingleNullableThing = -19.1f,
                DoubleNullableThing = -110.2,
                DecimalNullableThing = -111.3m,
                CharNullableThing = 'X',
                DateTimeNullableThing = DateTime.Now.AddMonths(1),
                DateTimeOffsetNullableThing = DateTimeOffset.Now.AddMonths(1).AddDays(1),
                TimeSpanNullableThing = DateTime.Now.AddHours(1).TimeOfDay,
                DateOnlyNullableThing = DateOnly.FromDateTime(DateTime.Now),
                TimeOnlyNullableThing = TimeOnly.FromDateTime(DateTime.Now),
                GuidNullableThing = Guid.NewGuid(),

                BooleanNullableThingNull = null,
                ByteNullableThingNull = null,
                Int16NullableThingNull = null,
                Int32NullableThingNull = null,
                Int64NullableThingNull = null,
                SingleNullableThingNull = null,
                DoubleNullableThingNull = null,
                DecimalNullableThingNull = null,
                CharNullableThingNull = null,
                DateTimeNullableThingNull = null,
                DateTimeOffsetNullableThingNull = null,
                TimeSpanNullableThingNull = null,
                DateOnlyNullableThingNull = null,
                TimeOnlyNullableThingNull = null,
                GuidNullableThingNull = null,

                StringThing = "Hello\r\nWorld!",
                StringThingNull = null,
                StringLengthThing = "Bounded",
                StringLengthThingNull = null,

                BytesThing = [1, 2, 3],
                BytesThingNull = null,
            };
            return model;
        }
    }
}
