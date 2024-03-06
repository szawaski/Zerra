// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Zerra.Test
{
    public static class Factory
    {
        private static void AssertOrderedEnumerable(IEnumerable model1, IEnumerable model2)
        {
            if (model1 != null)
            {
                Assert.IsNotNull(model2);
                var count1 = 0;
                var count2 = 0;
                foreach (var item1 in model1)
                    count1++;
                foreach (var item2 in model2)
                    count2++;
                Assert.AreEqual(count1, count1);

                var enumerator1 = model1.GetEnumerator();
                var enumerator2 = model2.GetEnumerator();
                while (enumerator1.MoveNext())
                {
                    _ = enumerator2.MoveNext();
                    var item1 = enumerator1.Current;
                    var item2 = enumerator2.Current;
                    Assert.AreEqual(item1, item2);
                }
            }
            else
            {
                Assert.IsNull(model2);
            }
        }
        private static void AssertUnorderedEnumerable(IEnumerable model1, IEnumerable model2)
        {
            if (model1 != null)
            {
                Assert.IsNotNull(model2);
                var count1 = 0;
                var count2 = 0;
                foreach (var item1 in model1)
                    count1++;
                foreach (var item2 in model2)
                    count2++;
                Assert.AreEqual(count1, count1);

                foreach (var item1 in model1)
                {
                    var found = false;
                    foreach (var item2 in model2)
                    {
                        if ((item1 == null && item2 == null) || (item1 != null && item2 != null && item1.Equals(item2)))
                        {
                            found = true;
                            break;
                        }
                    }
                    Assert.IsTrue(found);
                }
            }
            else
            {
                Assert.IsNull(model2);
            }
        }

        public static CoreTypesModel GetCoreTypesModel()
        {
            var model = new CoreTypesModel()
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
                TimeSpanThingNullable = DateTime.UtcNow.AddHours(1).TimeOfDay,
#if NET6_0_OR_GREATER
                DateOnlyThingNullable = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                TimeOnlyThingNullable = TimeOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
#endif
                GuidThingNullable = Guid.NewGuid(),
            };
            return model;
        }
        public static void AssertAreEqual(CoreTypesModel model1, CoreTypesModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing, model2.ByteThing);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing.ToUniversalTime(), model2.DateTimeThing.ToUniversalTime());
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing, model2.DateOnlyThing);
            Assert.AreEqual(model1.TimeOnlyThing, model2.TimeOnlyThing);
#endif
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.BooleanThingNullable, model2.BooleanThingNullable);
            Assert.AreEqual(model1.ByteThingNullable, model2.ByteThingNullable);
            Assert.AreEqual(model1.SByteThingNullable, model2.SByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable, model2.Int16ThingNullable);
            Assert.AreEqual(model1.UInt16ThingNullable, model2.UInt16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable, model2.Int32ThingNullable);
            Assert.AreEqual(model1.UInt32ThingNullable, model2.UInt32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable, model2.Int64ThingNullable);
            Assert.AreEqual(model1.UInt64ThingNullable, model2.UInt64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable, model2.SingleThingNullable);
            Assert.AreEqual(model1.DoubleThingNullable, model2.DoubleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable, model2.DecimalThingNullable);
            Assert.AreEqual(model1.CharThingNullable, model2.CharThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable?.ToUniversalTime(), model2.DateTimeThingNullable?.ToUniversalTime());
            Assert.AreEqual(model1.DateTimeOffsetThingNullable, model2.DateTimeOffsetThingNullable);
            Assert.AreEqual(model1.TimeSpanThingNullable, model2.TimeSpanThingNullable);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullable, model2.DateOnlyThingNullable);
            Assert.AreEqual(model1.TimeOnlyThingNullable, model2.TimeOnlyThingNullable);
#endif
            Assert.AreEqual(model1.GuidThingNullable, model2.GuidThingNullable);
        }
        public static void AssertAreEqual(CoreTypesModel model1, CoreTypesAsStringsModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.BooleanThing.ToString(), model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing.ToString(), model2.ByteThing);
            Assert.AreEqual(model1.SByteThing.ToString(), model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing.ToString(), model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing.ToString(), model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing.ToString(), model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing.ToString(), model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing.ToString(), model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing.ToString(), model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing.ToString(), model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing.ToString(), model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing.ToString(), model2.DecimalThing);
            Assert.AreEqual(model1.CharThing.ToString(), model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing.ToString(), model2.DateTimeThing);
            Assert.AreEqual(model1.DateTimeOffsetThing.ToString(), model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing.ToString(), model2.TimeSpanThing);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing.ToString(), model2.DateOnlyThing);
            Assert.AreEqual(model1.TimeOnlyThing.ToString(), model2.TimeOnlyThing);
#endif
            Assert.AreEqual(model1.GuidThing.ToString(), model2.GuidThing);

            Assert.AreEqual(model1.BooleanThingNullable?.ToString(), model2.BooleanThingNullable);
            Assert.AreEqual(model1.ByteThingNullable?.ToString(), model2.ByteThingNullable);
            Assert.AreEqual(model1.SByteThingNullable?.ToString(), model2.SByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable?.ToString(), model2.Int16ThingNullable);
            Assert.AreEqual(model1.UInt16ThingNullable?.ToString(), model2.UInt16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable?.ToString(), model2.Int32ThingNullable);
            Assert.AreEqual(model1.UInt32ThingNullable?.ToString(), model2.UInt32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable?.ToString(), model2.Int64ThingNullable);
            Assert.AreEqual(model1.UInt64ThingNullable?.ToString(), model2.UInt64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable?.ToString(), model2.SingleThingNullable);
            Assert.AreEqual(model1.DoubleThingNullable?.ToString(), model2.DoubleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable?.ToString(), model2.DecimalThingNullable);
            Assert.AreEqual(model1.CharThingNullable?.ToString(), model2.CharThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable?.ToString(), model2.DateTimeThingNullable);
            Assert.AreEqual(model1.DateTimeOffsetThingNullable?.ToString(), model2.DateTimeOffsetThingNullable);
            Assert.AreEqual(model1.TimeSpanThingNullable?.ToString(), model2.TimeSpanThingNullable);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullable.ToString(), model2.DateOnlyThingNullable);
            Assert.AreEqual(model1.TimeOnlyThingNullable.ToString(), model2.TimeOnlyThingNullable);
#endif
            Assert.AreEqual(model1.GuidThingNullable?.ToString(), model2.GuidThingNullable);
        }

        public static CoreTypesAlternatingModel GetCoreTypesAlternatingModel()
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
        public static void AssertAreEqual(CoreTypesAlternatingModel model1, CoreTypesModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.ByteThingNullable, model2.ByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable, model2.Int16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable, model2.Int32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable, model2.Int64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable, model2.SingleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable, model2.DecimalThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable, model2.DateTimeThingNullable);
            Assert.AreEqual(model1.TimeSpanThingNullable, model2.TimeSpanThingNullable);
        }

        public static AllTypesModel GetAllTypesModel()
        {
            var model = new AllTypesModel()
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
                TimeSpanThing = DateTime.UtcNow.TimeOfDay,
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
                TimeSpanThingNullable = DateTime.UtcNow.AddHours(1).TimeOfDay,
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

                BooleanArray = [true, false, true],
                ByteArray = [1, 2, 3],
                SByteArray = [4, 5, 6],
                Int16Array = [7, 8, 9],
                UInt16Array = [10, 11, 12],
                Int32Array = [13, 14, 15],
                UInt32Array = [16, 17, 18],
                Int64Array = [19, 20, 21],
                UInt64Array = [22, 23, 24],
                SingleArray = [25, 26, 27],
                DoubleArray = [28, 29, 30],
                DecimalArray = [31, 32, 33],
                CharArray = ['A', 'B', 'C'],
                DateTimeArray = [DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3)],
                DateTimeOffsetArray = [DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6)],
                TimeSpanArray = [DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay],
#if NET6_0_OR_GREATER
                DateOnlyArray = [DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))],
                TimeOnlyArray = [TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3))],
#endif
                GuidArray = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],

                BooleanArrayEmpty = [],
                ByteArrayEmpty = [],
                SByteArrayEmpty = [],
                Int16ArrayEmpty = [],
                UInt16ArrayEmpty = [],
                Int32ArrayEmpty = [],
                UInt32ArrayEmpty = [],
                Int64ArrayEmpty = [],
                UInt64ArrayEmpty = [],
                SingleArrayEmpty = [],
                DoubleArrayEmpty = [],
                DecimalArrayEmpty = [],
                CharArrayEmpty = [],
                DateTimeArrayEmpty = [],
                DateTimeOffsetArrayEmpty = [],
                TimeSpanArrayEmpty = [],
#if NET6_0_OR_GREATER
                DateOnlyArrayEmpty = [],
                TimeOnlyArrayEmpty = [],
#endif
                GuidArrayEmpty = [],

                BooleanArrayNull = null,
                ByteArrayNull = null,
                SByteArrayNull = null,
                Int16ArrayNull = null,
                UInt16ArrayNull = null,
                Int32ArrayNull = null,
                UInt32ArrayNull = null,
                Int64ArrayNull = null,
                UInt64ArrayNull = null,
                SingleArrayNull = null,
                DoubleArrayNull = null,
                DecimalArrayNull = null,
                CharArrayNull = null,
                DateTimeArrayNull = null,
                DateTimeOffsetArrayNull = null,
                TimeSpanArrayNull = null,
#if NET6_0_OR_GREATER
                DateOnlyArrayNull = [],
                TimeOnlyArrayNull = [],
#endif
                GuidArrayNull = null,

                BooleanArrayNullable = [true, null, true],
                ByteArrayNullable = [1, null, 3],
                SByteArrayNullable = [4, null, 6],
                Int16ArrayNullable = [7, null, 9],
                UInt16ArrayNullable = [10, null, 12],
                Int32ArrayNullable = [13, null, 15],
                UInt32ArrayNullable = [16, null, 18],
                Int64ArrayNullable = [19, null, 21],
                UInt64ArrayNullable = [22, null, 24],
                SingleArrayNullable = [25, null, 27],
                DoubleArrayNullable = [28, null, 30],
                DecimalArrayNullable = [31, null, 33],
                CharArrayNullable = ['A', null, 'C'],
                DateTimeArrayNullable = [DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3)],
                DateTimeOffsetArrayNullable = [DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6)],
                TimeSpanArrayNullable = [DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay],
#if NET6_0_OR_GREATER
                DateOnlyArrayNullable = [DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))],
                TimeOnlyArrayNullable = [TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3))],
#endif
                GuidArrayNullable = [Guid.NewGuid(), null, Guid.NewGuid()],

                BooleanArrayNullableEmpty = [],
                ByteArrayNullableEmpty = [],
                SByteArrayNullableEmpty = [],
                Int16ArrayNullableEmpty = [],
                UInt16ArrayNullableEmpty = [],
                Int32ArrayNullableEmpty = [],
                UInt32ArrayNullableEmpty = [],
                Int64ArrayNullableEmpty = [],
                UInt64ArrayNullableEmpty = [],
                SingleArrayNullableEmpty = [],
                DoubleArrayNullableEmpty = [],
                DecimalArrayNullableEmpty = [],
                CharArrayNullableEmpty = [],
                DateTimeArrayNullableEmpty = [],
                DateTimeOffsetArrayNullableEmpty = [],
                TimeSpanArrayNullableEmpty = [],
#if NET6_0_OR_GREATER
                DateOnlyArrayNullableEmpty = [],
                TimeOnlyArrayNullableEmpty = [],
#endif
                GuidArrayNullableEmpty = [],

                BooleanArrayNullableNull = null,
                ByteArrayNullableNull = null,
                SByteArrayNullableNull = null,
                Int16ArrayNullableNull = null,
                UInt16ArrayNullableNull = null,
                Int32ArrayNullableNull = null,
                UInt32ArrayNullableNull = null,
                Int64ArrayNullableNull = null,
                UInt64ArrayNullableNull = null,
                SingleArrayNullableNull = null,
                DoubleArrayNullableNull = null,
                DecimalArrayNullableNull = null,
                CharArrayNullableNull = null,
                DateTimeArrayNullableNull = null,
                DateTimeOffsetArrayNullableNull = null,
                TimeSpanArrayNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyArrayNullableNull = null,
                TimeOnlyArrayNullableNull = null,
#endif
                GuidArrayNullableNull = null,

                StringArray = ["Hello", "World", "People", "", null],
                StringArrayEmpty = Array.Empty<string>(),
                StringArrayNull = null,

                EnumArray = [EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3],
                EnumArrayNullable = [EnumModel.EnumItem1, null, EnumModel.EnumItem3],

                BooleanList = new() { true, false, true },
                ByteList = new() { 1, 2, 3 },
                SByteList = new() { 4, 5, 6 },
                Int16List = new() { 7, 8, 9 },
                UInt16List = new() { 10, 11, 12 },
                Int32List = new() { 13, 14, 15 },
                UInt32List = new() { 16, 17, 18 },
                Int64List = new() { 19, 20, 21 },
                UInt64List = new() { 22, 23, 24 },
                SingleList = new() { 25, 26, 27 },
                DoubleList = new() { 28, 29, 30 },
                DecimalList = new() { 31, 32, 33 },
                CharList = new() { 'A', 'B', 'C' },
                DateTimeList = new() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetList = new() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanList = new() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyList = new() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyList = new() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidList = new() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanListEmpty = new(0),
                ByteListEmpty = new(0),
                SByteListEmpty = new(0),
                Int16ListEmpty = new(0),
                UInt16ListEmpty = new(0),
                Int32ListEmpty = new(0),
                UInt32ListEmpty = new(0),
                Int64ListEmpty = new(0),
                UInt64ListEmpty = new(0),
                SingleListEmpty = new(0),
                DoubleListEmpty = new(0),
                DecimalListEmpty = new(0),
                CharListEmpty = new(0),
                DateTimeListEmpty = new(0),
                DateTimeOffsetListEmpty = new(0),
                TimeSpanListEmpty = new(0),
#if NET6_0_OR_GREATER
                DateOnlyListEmpty = new(0),
                TimeOnlyListEmpty = new(0),
#endif
                GuidListEmpty = new(0),

                BooleanListNull = null,
                ByteListNull = null,
                SByteListNull = null,
                Int16ListNull = null,
                UInt16ListNull = null,
                Int32ListNull = null,
                UInt32ListNull = null,
                Int64ListNull = null,
                UInt64ListNull = null,
                SingleListNull = null,
                DoubleListNull = null,
                DecimalListNull = null,
                CharListNull = null,
                DateTimeListNull = null,
                DateTimeOffsetListNull = null,
                TimeSpanListNull = null,
#if NET6_0_OR_GREATER
                DateOnlyListNull = null,
                TimeOnlyListNull = null,
#endif
                GuidListNull = null,

                BooleanListNullable = new() { true, null, true },
                ByteListNullable = new() { 1, null, 3 },
                SByteListNullable = new() { 4, null, 6 },
                Int16ListNullable = new() { 7, null, 9 },
                UInt16ListNullable = new() { 10, null, 12 },
                Int32ListNullable = new() { 13, null, 15 },
                UInt32ListNullable = new() { 16, null, 18 },
                Int64ListNullable = new() { 19, null, 21 },
                UInt64ListNullable = new() { 22, null, 24 },
                SingleListNullable = new() { 25, null, 27 },
                DoubleListNullable = new() { 28, null, 30 },
                DecimalListNullable = new() { 31, null, 33 },
                CharListNullable = new() { 'A', null, 'C' },
                DateTimeListNullable = new() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetListNullable = new() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanListNullable = new() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyListNullable = new() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyListNullable = new() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidListNullable = new() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanListNullableEmpty = new(0),
                ByteListNullableEmpty = new(0),
                SByteListNullableEmpty = new(0),
                Int16ListNullableEmpty = new(0),
                UInt16ListNullableEmpty = new(0),
                Int32ListNullableEmpty = new(0),
                UInt32ListNullableEmpty = new(0),
                Int64ListNullableEmpty = new(0),
                UInt64ListNullableEmpty = new(0),
                SingleListNullableEmpty = new(0),
                DoubleListNullableEmpty = new(0),
                DecimalListNullableEmpty = new(0),
                CharListNullableEmpty = new(0),
                DateTimeListNullableEmpty = new(0),
                DateTimeOffsetListNullableEmpty = new(0),
                TimeSpanListNullableEmpty = new(0),
#if NET6_0_OR_GREATER
                DateOnlyListNullableEmpty = new(0),
                TimeOnlyListNullableEmpty = new(0),
#endif
                GuidListNullableEmpty = new(0),

                BooleanListNullableNull = null,
                ByteListNullableNull = null,
                SByteListNullableNull = null,
                Int16ListNullableNull = null,
                UInt16ListNullableNull = null,
                Int32ListNullableNull = null,
                UInt32ListNullableNull = null,
                Int64ListNullableNull = null,
                UInt64ListNullableNull = null,
                SingleListNullableNull = null,
                DoubleListNullableNull = null,
                DecimalListNullableNull = null,
                CharListNullableNull = null,
                DateTimeListNullableNull = null,
                DateTimeOffsetListNullableNull = null,
                TimeSpanListNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyListNullableNull = null,
                TimeOnlyListNullableNull = null,
#endif
                GuidListNullableNull = null,

                StringList = new() { "Hello", "World", "People" },
                StringListEmpty = new(0),
                StringListNull = null,

                EnumList = new() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumListEmpty = new(0),
                EnumListNull = null,

                EnumListNullable = new() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumListNullableEmpty = new(0),
                EnumListNullableNull = null,

                ClassThing = new BasicModel { Value1 = 1234, Value2 = "S-1234" },
                ClassThingNull = null,

                ClassArray = [new BasicModel { Value1 = 10, Value2 = "S-10" }, new BasicModel { Value1 = 11, Value2 = "S-11" }],
                ClassArrayEmpty = [],
                ClassArrayNull = null,

                ClassEnumerable = new List<BasicModel>() { new() { Value1 = 12, Value2 = "S-12" }, new() { Value1 = 13, Value2 = "S-13" } },

                ClassList = new() { new BasicModel { Value1 = 14, Value2 = "S-14" }, new BasicModel { Value1 = 15, Value2 = "S-15" } },
                ClassListEmpty = new(0),
                ClassListNull = null,

                DictionaryThing = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                StringArrayOfArrayThing = [["a", "b", "c"], null, ["d", "e", "f"], ["", null, ""]]
            };
            return model;
        }
        public static void AssertAreEqual(AllTypesModel model1, AllTypesModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing, model2.ByteThing);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing, model2.DateTimeThing);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing, model2.DateOnlyThing);
            Assert.AreEqual(model1.TimeOnlyThing, model2.TimeOnlyThing);
#endif
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.BooleanThingNullable, model2.BooleanThingNullable);
            Assert.AreEqual(model1.ByteThingNullable, model2.ByteThingNullable);
            Assert.AreEqual(model1.SByteThingNullable, model2.SByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable, model2.Int16ThingNullable);
            Assert.AreEqual(model1.UInt16ThingNullable, model2.UInt16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable, model2.Int32ThingNullable);
            Assert.AreEqual(model1.UInt32ThingNullable, model2.UInt32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable, model2.Int64ThingNullable);
            Assert.AreEqual(model1.UInt64ThingNullable, model2.UInt64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable, model2.SingleThingNullable);
            Assert.AreEqual(model1.DoubleThingNullable, model2.DoubleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable, model2.DecimalThingNullable);
            Assert.AreEqual(model1.CharThingNullable, model2.CharThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable, model2.DateTimeThingNullable);
            Assert.AreEqual(model1.DateTimeOffsetThingNullable, model2.DateTimeOffsetThingNullable);
            Assert.AreEqual(model1.TimeSpanThingNullable, model2.TimeSpanThingNullable);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullable, model2.DateOnlyThingNullable);
            Assert.AreEqual(model1.TimeOnlyThingNullable, model2.TimeOnlyThingNullable);
#endif
            Assert.AreEqual(model1.GuidThingNullable, model2.GuidThingNullable);

            Assert.IsNull(model2.BooleanThingNullableNull);
            Assert.IsNull(model2.ByteThingNullableNull);
            Assert.IsNull(model2.SByteThingNullableNull);
            Assert.IsNull(model2.Int16ThingNullableNull);
            Assert.IsNull(model2.UInt16ThingNullableNull);
            Assert.IsNull(model2.Int32ThingNullableNull);
            Assert.IsNull(model2.UInt32ThingNullableNull);
            Assert.IsNull(model2.Int64ThingNullableNull);
            Assert.IsNull(model2.UInt64ThingNullableNull);
            Assert.IsNull(model2.SingleThingNullableNull);
            Assert.IsNull(model2.DoubleThingNullableNull);
            Assert.IsNull(model2.DecimalThingNullableNull);
            Assert.IsNull(model2.CharThingNullableNull);
            Assert.IsNull(model2.DateTimeThingNullableNull);
            Assert.IsNull(model2.DateTimeOffsetThingNullableNull);
            Assert.IsNull(model2.TimeSpanThingNullableNull);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullableNull, model2.DateOnlyThingNullableNull);
            Assert.AreEqual(model1.TimeOnlyThingNullableNull, model2.TimeOnlyThingNullableNull);
#endif
            Assert.IsNull(model2.GuidThingNullableNull);

            Assert.AreEqual(model1.StringThing, model2.StringThing);

            Assert.IsNull(model1.StringThingNull);
            Assert.IsNull(model2.StringThingNull);

            Assert.AreEqual(String.Empty, model1.StringThingEmpty);
            Assert.AreEqual(String.Empty, model2.StringThingEmpty);

            Assert.AreEqual(model1.EnumThing, model2.EnumThing);
            Assert.AreEqual(model1.EnumThingNullable, model2.EnumThingNullable);
            Assert.AreEqual(model1.EnumThingNullableNull, model2.EnumThingNullableNull);

            AssertOrderedEnumerable(model1.BooleanArray, model2.BooleanArray);
            AssertOrderedEnumerable(model1.ByteArray, model2.ByteArray);
            AssertOrderedEnumerable(model1.SByteArray, model2.SByteArray);
            AssertOrderedEnumerable(model1.Int16Array, model2.Int16Array);
            AssertOrderedEnumerable(model1.UInt16Array, model2.UInt16Array);
            AssertOrderedEnumerable(model1.Int32Array, model2.Int32Array);
            AssertOrderedEnumerable(model1.UInt32Array, model2.UInt32Array);
            AssertOrderedEnumerable(model1.Int64Array, model2.Int64Array);
            AssertOrderedEnumerable(model1.UInt64Array, model2.UInt64Array);
            AssertOrderedEnumerable(model1.SingleArray, model2.SingleArray);
            AssertOrderedEnumerable(model1.DoubleArray, model2.DoubleArray);
            AssertOrderedEnumerable(model1.DecimalArray, model2.DecimalArray);
            AssertOrderedEnumerable(model1.CharArray, model2.CharArray);
            AssertOrderedEnumerable(model1.DateTimeArray, model2.DateTimeArray);
            AssertOrderedEnumerable(model1.DateTimeOffsetArray, model2.DateTimeOffsetArray);
            AssertOrderedEnumerable(model1.TimeSpanArray, model2.TimeSpanArray);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArray, model2.DateOnlyArray);
            AssertOrderedEnumerable(model1.TimeOnlyArray, model2.TimeOnlyArray);
#endif
            AssertOrderedEnumerable(model1.GuidArray, model2.GuidArray);

            AssertOrderedEnumerable(model1.BooleanArrayEmpty, model2.BooleanArrayEmpty);
            AssertOrderedEnumerable(model1.ByteArrayEmpty, model2.ByteArrayEmpty);
            AssertOrderedEnumerable(model1.SByteArrayEmpty, model2.SByteArrayEmpty);
            AssertOrderedEnumerable(model1.Int16ArrayEmpty, model2.Int16ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt16ArrayEmpty, model2.UInt16ArrayEmpty);
            AssertOrderedEnumerable(model1.Int32ArrayEmpty, model2.Int32ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt32ArrayEmpty, model2.UInt32ArrayEmpty);
            AssertOrderedEnumerable(model1.Int64ArrayEmpty, model2.Int64ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt64ArrayEmpty, model2.UInt64ArrayEmpty);
            AssertOrderedEnumerable(model1.SingleArrayEmpty, model2.SingleArrayEmpty);
            AssertOrderedEnumerable(model1.DoubleArrayEmpty, model2.DoubleArrayEmpty);
            AssertOrderedEnumerable(model1.DecimalArrayEmpty, model2.DecimalArrayEmpty);
            AssertOrderedEnumerable(model1.CharArrayEmpty, model2.CharArrayEmpty);
            AssertOrderedEnumerable(model1.DateTimeArrayEmpty, model2.DateTimeArrayEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayEmpty, model2.DateTimeOffsetArrayEmpty);
            AssertOrderedEnumerable(model1.TimeSpanArrayEmpty, model2.TimeSpanArrayEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayEmpty, model2.DateOnlyArrayEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyArrayEmpty, model2.TimeOnlyArrayEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidArrayEmpty, model2.GuidArrayEmpty);

            AssertOrderedEnumerable(model1.BooleanArrayNull, model2.BooleanArrayNull);
            AssertOrderedEnumerable(model1.ByteArrayNull, model2.ByteArrayNull);
            AssertOrderedEnumerable(model1.SByteArrayNull, model2.SByteArrayNull);
            AssertOrderedEnumerable(model1.Int16ArrayNull, model2.Int16ArrayNull);
            AssertOrderedEnumerable(model1.UInt16ArrayNull, model2.UInt16ArrayNull);
            AssertOrderedEnumerable(model1.Int32ArrayNull, model2.Int32ArrayNull);
            AssertOrderedEnumerable(model1.UInt32ArrayNull, model2.UInt32ArrayNull);
            AssertOrderedEnumerable(model1.Int64ArrayNull, model2.Int64ArrayNull);
            AssertOrderedEnumerable(model1.UInt64ArrayNull, model2.UInt64ArrayNull);
            AssertOrderedEnumerable(model1.SingleArrayNull, model2.SingleArrayNull);
            AssertOrderedEnumerable(model1.DoubleArrayNull, model2.DoubleArrayNull);
            AssertOrderedEnumerable(model1.DecimalArrayNull, model2.DecimalArrayNull);
            AssertOrderedEnumerable(model1.CharArrayNull, model2.CharArrayNull);
            AssertOrderedEnumerable(model1.DateTimeArrayNull, model2.DateTimeArrayNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNull, model2.DateTimeOffsetArrayNull);
            AssertOrderedEnumerable(model1.TimeSpanArrayNull, model2.TimeSpanArrayNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNull, model2.DateOnlyArrayNull);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNull, model2.TimeOnlyArrayNull);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNull, model2.GuidArrayNull);

            AssertOrderedEnumerable(model1.BooleanArrayNullable, model2.BooleanArrayNullable);
            AssertOrderedEnumerable(model1.ByteArrayNullable, model2.ByteArrayNullable);
            AssertOrderedEnumerable(model1.SByteArrayNullable, model2.SByteArrayNullable);
            AssertOrderedEnumerable(model1.Int16ArrayNullable, model2.Int16ArrayNullable);
            AssertOrderedEnumerable(model1.UInt16ArrayNullable, model2.UInt16ArrayNullable);
            AssertOrderedEnumerable(model1.Int32ArrayNullable, model2.Int32ArrayNullable);
            AssertOrderedEnumerable(model1.UInt32ArrayNullable, model2.UInt32ArrayNullable);
            AssertOrderedEnumerable(model1.Int64ArrayNullable, model2.Int64ArrayNullable);
            AssertOrderedEnumerable(model1.UInt64ArrayNullable, model2.UInt64ArrayNullable);
            AssertOrderedEnumerable(model1.SingleArrayNullable, model2.SingleArrayNullable);
            AssertOrderedEnumerable(model1.DoubleArrayNullable, model2.DoubleArrayNullable);
            AssertOrderedEnumerable(model1.DecimalArrayNullable, model2.DecimalArrayNullable);
            AssertOrderedEnumerable(model1.CharArrayNullable, model2.CharArrayNullable);
            AssertOrderedEnumerable(model1.DateTimeArrayNullable, model2.DateTimeArrayNullable);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullable, model2.DateTimeOffsetArrayNullable);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullable, model2.TimeSpanArrayNullable);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullable, model2.DateOnlyArrayNullable);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullable, model2.TimeOnlyArrayNullable);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertOrderedEnumerable(model1.BooleanArrayNullableEmpty, model2.BooleanArrayNullableEmpty);
            AssertOrderedEnumerable(model1.ByteArrayNullableEmpty, model2.ByteArrayNullableEmpty);
            AssertOrderedEnumerable(model1.SByteArrayNullableEmpty, model2.SByteArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int16ArrayNullableEmpty, model2.Int16ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt16ArrayNullableEmpty, model2.UInt16ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int32ArrayNullableEmpty, model2.Int32ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt32ArrayNullableEmpty, model2.UInt32ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int64ArrayNullableEmpty, model2.Int64ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt64ArrayNullableEmpty, model2.UInt64ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.SingleArrayNullableEmpty, model2.SingleArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DoubleArrayNullableEmpty, model2.DoubleArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DecimalArrayNullableEmpty, model2.DecimalArrayNullableEmpty);
            AssertOrderedEnumerable(model1.CharArrayNullableEmpty, model2.CharArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeArrayNullableEmpty, model2.DateTimeArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullableEmpty, model2.DateTimeOffsetArrayNullableEmpty);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullableEmpty, model2.TimeSpanArrayNullableEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullableEmpty, model2.DateOnlyArrayNullableEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullableEmpty, model2.TimeOnlyArrayNullableEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullableEmpty, model2.GuidArrayNullableEmpty);

            AssertOrderedEnumerable(model1.BooleanArrayNullableNull, model2.BooleanArrayNullableNull);
            AssertOrderedEnumerable(model1.ByteArrayNullableNull, model2.ByteArrayNullableNull);
            AssertOrderedEnumerable(model1.SByteArrayNullableNull, model2.SByteArrayNullableNull);
            AssertOrderedEnumerable(model1.Int16ArrayNullableNull, model2.Int16ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt16ArrayNullableNull, model2.UInt16ArrayNullableNull);
            AssertOrderedEnumerable(model1.Int32ArrayNullableNull, model2.Int32ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt32ArrayNullableNull, model2.UInt32ArrayNullableNull);
            AssertOrderedEnumerable(model1.Int64ArrayNullableNull, model2.Int64ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt64ArrayNullableNull, model2.UInt64ArrayNullableNull);
            AssertOrderedEnumerable(model1.SingleArrayNullableNull, model2.SingleArrayNullableNull);
            AssertOrderedEnumerable(model1.DoubleArrayNullableNull, model2.DoubleArrayNullableNull);
            AssertOrderedEnumerable(model1.DecimalArrayNullableNull, model2.DecimalArrayNullableNull);
            AssertOrderedEnumerable(model1.CharArrayNullableNull, model2.CharArrayNullableNull);
            AssertOrderedEnumerable(model1.DateTimeArrayNullableNull, model2.DateTimeArrayNullableNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullableNull, model2.DateTimeOffsetArrayNullableNull);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullableNull, model2.TimeSpanArrayNullableNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullableNull, model2.DateOnlyArrayNullableNull);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullableNull, model2.TimeOnlyArrayNullableNull);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullableNull, model2.GuidArrayNullableNull);

            AssertOrderedEnumerable(model1.StringArray, model2.StringArray);
            AssertOrderedEnumerable(model1.StringArrayEmpty, model2.StringArrayEmpty);
            AssertOrderedEnumerable(model1.StringArrayNull, model2.StringArrayNull);

            AssertOrderedEnumerable(model1.EnumArray, model2.EnumArray);
            AssertOrderedEnumerable(model1.EnumArrayNullable, model2.EnumArrayNullable);

            AssertOrderedEnumerable(model1.BooleanList, model2.BooleanList);
            AssertOrderedEnumerable(model1.ByteList, model2.ByteList);
            AssertOrderedEnumerable(model1.SByteList, model2.SByteList);
            AssertOrderedEnumerable(model1.Int16List, model2.Int16List);
            AssertOrderedEnumerable(model1.UInt16List, model2.UInt16List);
            AssertOrderedEnumerable(model1.Int32List, model2.Int32List);
            AssertOrderedEnumerable(model1.UInt32List, model2.UInt32List);
            AssertOrderedEnumerable(model1.Int64List, model2.Int64List);
            AssertOrderedEnumerable(model1.UInt64List, model2.UInt64List);
            AssertOrderedEnumerable(model1.SingleList, model2.SingleList);
            AssertOrderedEnumerable(model1.DoubleList, model2.DoubleList);
            AssertOrderedEnumerable(model1.DecimalList, model2.DecimalList);
            AssertOrderedEnumerable(model1.CharList, model2.CharList);
            AssertOrderedEnumerable(model1.DateTimeList, model2.DateTimeList);
            AssertOrderedEnumerable(model1.DateTimeOffsetList, model2.DateTimeOffsetList);
            AssertOrderedEnumerable(model1.TimeSpanList, model2.TimeSpanList);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyList, model2.DateOnlyList);
            AssertOrderedEnumerable(model1.TimeOnlyList, model2.TimeOnlyList);
#endif
            AssertOrderedEnumerable(model1.GuidList, model2.GuidList);

            AssertOrderedEnumerable(model1.BooleanListEmpty, model2.BooleanListEmpty);
            AssertOrderedEnumerable(model1.ByteListEmpty, model2.ByteListEmpty);
            AssertOrderedEnumerable(model1.SByteListEmpty, model2.SByteListEmpty);
            AssertOrderedEnumerable(model1.Int16ListEmpty, model2.Int16ListEmpty);
            AssertOrderedEnumerable(model1.UInt16ListEmpty, model2.UInt16ListEmpty);
            AssertOrderedEnumerable(model1.Int32ListEmpty, model2.Int32ListEmpty);
            AssertOrderedEnumerable(model1.UInt32ListEmpty, model2.UInt32ListEmpty);
            AssertOrderedEnumerable(model1.Int64ListEmpty, model2.Int64ListEmpty);
            AssertOrderedEnumerable(model1.UInt64ListEmpty, model2.UInt64ListEmpty);
            AssertOrderedEnumerable(model1.SingleListEmpty, model2.SingleListEmpty);
            AssertOrderedEnumerable(model1.DoubleListEmpty, model2.DoubleListEmpty);
            AssertOrderedEnumerable(model1.DecimalListEmpty, model2.DecimalListEmpty);
            AssertOrderedEnumerable(model1.CharListEmpty, model2.CharListEmpty);
            AssertOrderedEnumerable(model1.DateTimeListEmpty, model2.DateTimeListEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetListEmpty, model2.DateTimeOffsetListEmpty);
            AssertOrderedEnumerable(model1.TimeSpanListEmpty, model2.TimeSpanListEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListEmpty, model2.DateOnlyListEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyListEmpty, model2.TimeOnlyListEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidListEmpty, model2.GuidListEmpty);

            AssertOrderedEnumerable(model1.BooleanListNull, model2.BooleanListNull);
            AssertOrderedEnumerable(model1.ByteListNull, model2.ByteListNull);
            AssertOrderedEnumerable(model1.SByteListNull, model2.SByteListNull);
            AssertOrderedEnumerable(model1.Int16ListNull, model2.Int16ListNull);
            AssertOrderedEnumerable(model1.UInt16ListNull, model2.UInt16ListNull);
            AssertOrderedEnumerable(model1.Int32ListNull, model2.Int32ListNull);
            AssertOrderedEnumerable(model1.UInt32ListNull, model2.UInt32ListNull);
            AssertOrderedEnumerable(model1.Int64ListNull, model2.Int64ListNull);
            AssertOrderedEnumerable(model1.UInt64ListNull, model2.UInt64ListNull);
            AssertOrderedEnumerable(model1.SingleListNull, model2.SingleListNull);
            AssertOrderedEnumerable(model1.DoubleListNull, model2.DoubleListNull);
            AssertOrderedEnumerable(model1.DecimalListNull, model2.DecimalListNull);
            AssertOrderedEnumerable(model1.CharListNull, model2.CharListNull);
            AssertOrderedEnumerable(model1.DateTimeListNull, model2.DateTimeListNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNull, model2.DateTimeOffsetListNull);
            AssertOrderedEnumerable(model1.TimeSpanListNull, model2.TimeSpanListNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNull, model2.DateOnlyListNull);
            AssertOrderedEnumerable(model1.TimeOnlyListNull, model2.TimeOnlyListNull);
#endif
            AssertOrderedEnumerable(model1.GuidListNull, model2.GuidListNull);

            AssertOrderedEnumerable(model1.BooleanListNullable, model2.BooleanListNullable);
            AssertOrderedEnumerable(model1.ByteListNullable, model2.ByteListNullable);
            AssertOrderedEnumerable(model1.SByteListNullable, model2.SByteListNullable);
            AssertOrderedEnumerable(model1.Int16ListNullable, model2.Int16ListNullable);
            AssertOrderedEnumerable(model1.UInt16ListNullable, model2.UInt16ListNullable);
            AssertOrderedEnumerable(model1.Int32ListNullable, model2.Int32ListNullable);
            AssertOrderedEnumerable(model1.UInt32ListNullable, model2.UInt32ListNullable);
            AssertOrderedEnumerable(model1.Int64ListNullable, model2.Int64ListNullable);
            AssertOrderedEnumerable(model1.UInt64ListNullable, model2.UInt64ListNullable);
            AssertOrderedEnumerable(model1.SingleListNullable, model2.SingleListNullable);
            AssertOrderedEnumerable(model1.DoubleListNullable, model2.DoubleListNullable);
            AssertOrderedEnumerable(model1.DecimalListNullable, model2.DecimalListNullable);
            AssertOrderedEnumerable(model1.CharListNullable, model2.CharListNullable);
            AssertOrderedEnumerable(model1.DateTimeListNullable, model2.DateTimeListNullable);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullable, model2.DateTimeOffsetListNullable);
            AssertOrderedEnumerable(model1.TimeSpanListNullable, model2.TimeSpanListNullable);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullable, model2.DateOnlyListNullable);
            AssertOrderedEnumerable(model1.TimeOnlyListNullable, model2.TimeOnlyListNullable);
#endif
            AssertOrderedEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertOrderedEnumerable(model1.BooleanListNullableEmpty, model2.BooleanListNullableEmpty);
            AssertOrderedEnumerable(model1.ByteListNullableEmpty, model2.ByteListNullableEmpty);
            AssertOrderedEnumerable(model1.SByteListNullableEmpty, model2.SByteListNullableEmpty);
            AssertOrderedEnumerable(model1.Int16ListNullableEmpty, model2.Int16ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt16ListNullableEmpty, model2.UInt16ListNullableEmpty);
            AssertOrderedEnumerable(model1.Int32ListNullableEmpty, model2.Int32ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt32ListNullableEmpty, model2.UInt32ListNullableEmpty);
            AssertOrderedEnumerable(model1.Int64ListNullableEmpty, model2.Int64ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt64ListNullableEmpty, model2.UInt64ListNullableEmpty);
            AssertOrderedEnumerable(model1.SingleListNullableEmpty, model2.SingleListNullableEmpty);
            AssertOrderedEnumerable(model1.DoubleListNullableEmpty, model2.DoubleListNullableEmpty);
            AssertOrderedEnumerable(model1.DecimalListNullableEmpty, model2.DecimalListNullableEmpty);
            AssertOrderedEnumerable(model1.CharListNullableEmpty, model2.CharListNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeListNullableEmpty, model2.DateTimeListNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullableEmpty, model2.DateTimeOffsetListNullableEmpty);
            AssertOrderedEnumerable(model1.TimeSpanListNullableEmpty, model2.TimeSpanListNullableEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullableEmpty, model2.DateOnlyListNullableEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyListNullableEmpty, model2.TimeOnlyListNullableEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidListNullableEmpty, model2.GuidListNullableEmpty);

            AssertOrderedEnumerable(model1.BooleanListNullableNull, model2.BooleanListNullableNull);
            AssertOrderedEnumerable(model1.ByteListNullableNull, model2.ByteListNullableNull);
            AssertOrderedEnumerable(model1.SByteListNullableNull, model2.SByteListNullableNull);
            AssertOrderedEnumerable(model1.Int16ListNullableNull, model2.Int16ListNullableNull);
            AssertOrderedEnumerable(model1.UInt16ListNullableNull, model2.UInt16ListNullableNull);
            AssertOrderedEnumerable(model1.Int32ListNullableNull, model2.Int32ListNullableNull);
            AssertOrderedEnumerable(model1.UInt32ListNullableNull, model2.UInt32ListNullableNull);
            AssertOrderedEnumerable(model1.Int64ListNullableNull, model2.Int64ListNullableNull);
            AssertOrderedEnumerable(model1.UInt64ListNullableNull, model2.UInt64ListNullableNull);
            AssertOrderedEnumerable(model1.SingleListNullableNull, model2.SingleListNullableNull);
            AssertOrderedEnumerable(model1.DoubleListNullableNull, model2.DoubleListNullableNull);
            AssertOrderedEnumerable(model1.DecimalListNullableNull, model2.DecimalListNullableNull);
            AssertOrderedEnumerable(model1.CharListNullableNull, model2.CharListNullableNull);
            AssertOrderedEnumerable(model1.DateTimeListNullableNull, model2.DateTimeListNullableNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullableNull, model2.DateTimeOffsetListNullableNull);
            AssertOrderedEnumerable(model1.TimeSpanListNullableNull, model2.TimeSpanListNullableNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullableNull, model2.DateOnlyListNullableNull);
            AssertOrderedEnumerable(model1.TimeOnlyListNullableNull, model2.TimeOnlyListNullableNull);
#endif
            AssertOrderedEnumerable(model1.GuidListNullableNull, model2.GuidListNullableNull);

            AssertOrderedEnumerable(model1.StringList, model2.StringList);
            AssertOrderedEnumerable(model1.StringListEmpty, model2.StringListEmpty);
            AssertOrderedEnumerable(model1.StringListNull, model2.StringListNull);

            AssertOrderedEnumerable(model1.EnumList, model2.EnumList);
            AssertOrderedEnumerable(model1.EnumListEmpty, model2.EnumListEmpty);
            AssertOrderedEnumerable(model1.EnumListNull, model2.EnumListNull);

            AssertOrderedEnumerable(model1.EnumListNullable, model2.EnumListNullable);
            AssertOrderedEnumerable(model1.EnumListNullableEmpty, model2.EnumListNullableEmpty);
            AssertOrderedEnumerable(model1.EnumListNullableNull, model2.EnumListNullableNull);

            if (model1.ClassThing != null)
            {
                Assert.IsNotNull(model2.ClassThing);
                Assert.AreEqual(model1.ClassThing.Value1, model2.ClassThing.Value1);
                Assert.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);
            }
            else
            {
                Assert.IsNull(model2.ClassThing);
            }

            Assert.IsNull(model1.ClassThingNull);
            Assert.IsNull(model2.ClassThingNull);

            if (model1.ClassArray != null)
            {
                Assert.IsNotNull(model2.ClassArray);
                Assert.AreEqual(model1.ClassArray.Length, model2.ClassArray.Length);
                for (var i = 0; i < model1.ClassArray.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArray[i].Value1, model2.ClassArray[i].Value1);
                    Assert.AreEqual(model1.ClassArray[i].Value2, model2.ClassArray[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArray);
            }

            if (model1.ClassArrayEmpty != null)
            {
                Assert.IsNotNull(model2.ClassArrayEmpty);
                Assert.AreEqual(model1.ClassArrayEmpty.Length, model2.ClassArrayEmpty.Length);
                for (var i = 0; i < model1.ClassArrayEmpty.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArrayEmpty[i].Value1, model2.ClassArrayEmpty[i].Value1);
                    Assert.AreEqual(model1.ClassArrayEmpty[i].Value2, model2.ClassArrayEmpty[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArrayEmpty);
            }

            if (model1.ClassArrayNull != null)
            {
                Assert.IsNotNull(model2.ClassArrayNull);
                Assert.AreEqual(model1.ClassArrayNull.Length, model2.ClassArrayNull.Length);
                for (var i = 0; i < model1.ClassArrayNull.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArrayNull[i].Value1, model2.ClassArrayNull[i].Value1);
                    Assert.AreEqual(model1.ClassArrayNull[i].Value2, model2.ClassArrayNull[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArrayNull);
            }

            if (model1.ClassEnumerable != null)
            {
                Assert.IsNotNull(model2.ClassEnumerable);
                var items1 = new List<BasicModel>();
                foreach (var item in model1.ClassEnumerable)
                    items1.Add(item);
                var items2 = new List<BasicModel>();
                foreach (var item in model2.ClassEnumerable)
                    items2.Add(item);

                Assert.AreEqual(items1.Count, items2.Count);
                for (var i = 0; i < items1.Count; i++)
                {
                    Assert.AreEqual(items1[i].Value1, items2[i].Value1);
                    Assert.AreEqual(items1[i].Value2, items2[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassEnumerable);
            }

            if (model1.ClassList != null)
            {
                Assert.AreEqual(model1.ClassList.Count, model2.ClassList.Count);
                for (var i = 0; i < model1.ClassList.Count; i++)
                {
                    Assert.AreEqual(model1.ClassList[i].Value1, model2.ClassList[i].Value1);
                    Assert.AreEqual(model1.ClassList[i].Value2, model2.ClassList[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassList);
            }

            if (model1.ClassListEmpty != null)
            {
                Assert.AreEqual(model1.ClassListEmpty.Count, model2.ClassListEmpty.Count);
                for (var i = 0; i < model1.ClassListEmpty.Count; i++)
                {
                    Assert.AreEqual(model1.ClassListEmpty[i].Value1, model2.ClassListEmpty[i].Value1);
                    Assert.AreEqual(model1.ClassListEmpty[i].Value2, model2.ClassListEmpty[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassListEmpty);
            }

            if (model1.ClassListNull != null)
            {
                Assert.AreEqual(model1.ClassListNull.Count, model2.ClassListNull.Count);
                for (var i = 0; i < model1.ClassListNull.Count; i++)
                {
                    Assert.AreEqual(model1.ClassListNull[i].Value1, model2.ClassListNull[i].Value1);
                    Assert.AreEqual(model1.ClassListNull[i].Value2, model2.ClassListNull[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassListNull);
            }

            if (model1.DictionaryThing != null)
            {
                Assert.IsNotNull(model2.DictionaryThing);
                Assert.AreEqual(model1.DictionaryThing.Count, model2.DictionaryThing.Count);
                foreach (var key in model1.DictionaryThing.Keys)
                {
                    Assert.IsTrue(model2.DictionaryThing.ContainsKey(key));
                    Assert.AreEqual(model1.DictionaryThing[key], model2.DictionaryThing[key]);
                }
            }
            else
            {
                Assert.IsNull(model2.DictionaryThing);
            }

            if (model1.StringArrayOfArrayThing != null)
            {
                Assert.IsNotNull(model2.StringArrayOfArrayThing);
                Assert.AreEqual(model1.StringArrayOfArrayThing.Length, model2.StringArrayOfArrayThing.Length);
                for (var i = 0; i < model1.StringArrayOfArrayThing.Length; i++)
                {
                    if (model1.StringArrayOfArrayThing[i] == null)
                    {
                        Assert.IsNull(model2.StringArrayOfArrayThing[i]);
                    }
                    else
                    {
                        Assert.AreEqual(model1.StringArrayOfArrayThing[i].Length, model2.StringArrayOfArrayThing[i].Length);
                        for (var j = 0; j < model1.StringArrayOfArrayThing[i].Length; j++)
                        {
                            Assert.AreEqual(model1.StringArrayOfArrayThing[i][j], model2.StringArrayOfArrayThing[i][j]);
                        }
                    }
                }
            }
            else
            {
                Assert.IsNull(model2.StringArrayOfArrayThing);
            }
        }
        public static void AssertAreEqual(AllTypesModel model1, AllTypesReversedModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing, model2.ByteThing);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing, model2.DateTimeThing);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing, model2.DateOnlyThing);
            Assert.AreEqual(model1.TimeOnlyThing, model2.TimeOnlyThing);
#endif
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.BooleanThingNullable, model2.BooleanThingNullable);
            Assert.AreEqual(model1.ByteThingNullable, model2.ByteThingNullable);
            Assert.AreEqual(model1.SByteThingNullable, model2.SByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable, model2.Int16ThingNullable);
            Assert.AreEqual(model1.UInt16ThingNullable, model2.UInt16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable, model2.Int32ThingNullable);
            Assert.AreEqual(model1.UInt32ThingNullable, model2.UInt32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable, model2.Int64ThingNullable);
            Assert.AreEqual(model1.UInt64ThingNullable, model2.UInt64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable, model2.SingleThingNullable);
            Assert.AreEqual(model1.DoubleThingNullable, model2.DoubleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable, model2.DecimalThingNullable);
            Assert.AreEqual(model1.CharThingNullable, model2.CharThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable, model2.DateTimeThingNullable);
            Assert.AreEqual(model1.DateTimeOffsetThingNullable, model2.DateTimeOffsetThingNullable);
            Assert.AreEqual(model1.TimeSpanThingNullable, model2.TimeSpanThingNullable);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullable, model2.DateOnlyThingNullable);
            Assert.AreEqual(model1.TimeOnlyThingNullable, model2.TimeOnlyThingNullable);
#endif
            Assert.AreEqual(model1.GuidThingNullable, model2.GuidThingNullable);

            Assert.IsNull(model2.BooleanThingNullableNull);
            Assert.IsNull(model2.ByteThingNullableNull);
            Assert.IsNull(model2.SByteThingNullableNull);
            Assert.IsNull(model2.Int16ThingNullableNull);
            Assert.IsNull(model2.UInt16ThingNullableNull);
            Assert.IsNull(model2.Int32ThingNullableNull);
            Assert.IsNull(model2.UInt32ThingNullableNull);
            Assert.IsNull(model2.Int64ThingNullableNull);
            Assert.IsNull(model2.UInt64ThingNullableNull);
            Assert.IsNull(model2.SingleThingNullableNull);
            Assert.IsNull(model2.DoubleThingNullableNull);
            Assert.IsNull(model2.DecimalThingNullableNull);
            Assert.IsNull(model2.CharThingNullableNull);
            Assert.IsNull(model2.DateTimeThingNullableNull);
            Assert.IsNull(model2.DateTimeOffsetThingNullableNull);
            Assert.IsNull(model2.TimeSpanThingNullableNull);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullableNull, model2.DateOnlyThingNullableNull);
            Assert.AreEqual(model1.TimeOnlyThingNullableNull, model2.TimeOnlyThingNullableNull);
#endif
            Assert.IsNull(model2.GuidThingNullableNull);

            Assert.AreEqual(model1.StringThing, model2.StringThing);

            Assert.IsNull(model1.StringThingNull);
            Assert.IsNull(model2.StringThingNull);

            Assert.AreEqual(String.Empty, model1.StringThingEmpty);
            Assert.AreEqual(String.Empty, model2.StringThingEmpty);

            Assert.AreEqual(model1.EnumThing, model2.EnumThing);
            Assert.AreEqual(model1.EnumThingNullable, model2.EnumThingNullable);
            Assert.AreEqual(model1.EnumThingNullableNull, model2.EnumThingNullableNull);

            AssertOrderedEnumerable(model1.BooleanArray, model2.BooleanArray);
            AssertOrderedEnumerable(model1.ByteArray, model2.ByteArray);
            AssertOrderedEnumerable(model1.SByteArray, model2.SByteArray);
            AssertOrderedEnumerable(model1.Int16Array, model2.Int16Array);
            AssertOrderedEnumerable(model1.UInt16Array, model2.UInt16Array);
            AssertOrderedEnumerable(model1.Int32Array, model2.Int32Array);
            AssertOrderedEnumerable(model1.UInt32Array, model2.UInt32Array);
            AssertOrderedEnumerable(model1.Int64Array, model2.Int64Array);
            AssertOrderedEnumerable(model1.UInt64Array, model2.UInt64Array);
            AssertOrderedEnumerable(model1.SingleArray, model2.SingleArray);
            AssertOrderedEnumerable(model1.DoubleArray, model2.DoubleArray);
            AssertOrderedEnumerable(model1.DecimalArray, model2.DecimalArray);
            AssertOrderedEnumerable(model1.CharArray, model2.CharArray);
            AssertOrderedEnumerable(model1.DateTimeArray, model2.DateTimeArray);
            AssertOrderedEnumerable(model1.DateTimeOffsetArray, model2.DateTimeOffsetArray);
            AssertOrderedEnumerable(model1.TimeSpanArray, model2.TimeSpanArray);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArray, model2.DateOnlyArray);
            AssertOrderedEnumerable(model1.TimeOnlyArray, model2.TimeOnlyArray);
#endif
            AssertOrderedEnumerable(model1.GuidArray, model2.GuidArray);

            AssertOrderedEnumerable(model1.BooleanArrayEmpty, model2.BooleanArrayEmpty);
            AssertOrderedEnumerable(model1.ByteArrayEmpty, model2.ByteArrayEmpty);
            AssertOrderedEnumerable(model1.SByteArrayEmpty, model2.SByteArrayEmpty);
            AssertOrderedEnumerable(model1.Int16ArrayEmpty, model2.Int16ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt16ArrayEmpty, model2.UInt16ArrayEmpty);
            AssertOrderedEnumerable(model1.Int32ArrayEmpty, model2.Int32ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt32ArrayEmpty, model2.UInt32ArrayEmpty);
            AssertOrderedEnumerable(model1.Int64ArrayEmpty, model2.Int64ArrayEmpty);
            AssertOrderedEnumerable(model1.UInt64ArrayEmpty, model2.UInt64ArrayEmpty);
            AssertOrderedEnumerable(model1.SingleArrayEmpty, model2.SingleArrayEmpty);
            AssertOrderedEnumerable(model1.DoubleArrayEmpty, model2.DoubleArrayEmpty);
            AssertOrderedEnumerable(model1.DecimalArrayEmpty, model2.DecimalArrayEmpty);
            AssertOrderedEnumerable(model1.CharArrayEmpty, model2.CharArrayEmpty);
            AssertOrderedEnumerable(model1.DateTimeArrayEmpty, model2.DateTimeArrayEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayEmpty, model2.DateTimeOffsetArrayEmpty);
            AssertOrderedEnumerable(model1.TimeSpanArrayEmpty, model2.TimeSpanArrayEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayEmpty, model2.DateOnlyArrayEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyArrayEmpty, model2.TimeOnlyArrayEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidArrayEmpty, model2.GuidArrayEmpty);

            AssertOrderedEnumerable(model1.BooleanArrayNull, model2.BooleanArrayNull);
            AssertOrderedEnumerable(model1.ByteArrayNull, model2.ByteArrayNull);
            AssertOrderedEnumerable(model1.SByteArrayNull, model2.SByteArrayNull);
            AssertOrderedEnumerable(model1.Int16ArrayNull, model2.Int16ArrayNull);
            AssertOrderedEnumerable(model1.UInt16ArrayNull, model2.UInt16ArrayNull);
            AssertOrderedEnumerable(model1.Int32ArrayNull, model2.Int32ArrayNull);
            AssertOrderedEnumerable(model1.UInt32ArrayNull, model2.UInt32ArrayNull);
            AssertOrderedEnumerable(model1.Int64ArrayNull, model2.Int64ArrayNull);
            AssertOrderedEnumerable(model1.UInt64ArrayNull, model2.UInt64ArrayNull);
            AssertOrderedEnumerable(model1.SingleArrayNull, model2.SingleArrayNull);
            AssertOrderedEnumerable(model1.DoubleArrayNull, model2.DoubleArrayNull);
            AssertOrderedEnumerable(model1.DecimalArrayNull, model2.DecimalArrayNull);
            AssertOrderedEnumerable(model1.CharArrayNull, model2.CharArrayNull);
            AssertOrderedEnumerable(model1.DateTimeArrayNull, model2.DateTimeArrayNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNull, model2.DateTimeOffsetArrayNull);
            AssertOrderedEnumerable(model1.TimeSpanArrayNull, model2.TimeSpanArrayNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNull, model2.DateOnlyArrayNull);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNull, model2.TimeOnlyArrayNull);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNull, model2.GuidArrayNull);

            AssertOrderedEnumerable(model1.BooleanArrayNullable, model2.BooleanArrayNullable);
            AssertOrderedEnumerable(model1.ByteArrayNullable, model2.ByteArrayNullable);
            AssertOrderedEnumerable(model1.SByteArrayNullable, model2.SByteArrayNullable);
            AssertOrderedEnumerable(model1.Int16ArrayNullable, model2.Int16ArrayNullable);
            AssertOrderedEnumerable(model1.UInt16ArrayNullable, model2.UInt16ArrayNullable);
            AssertOrderedEnumerable(model1.Int32ArrayNullable, model2.Int32ArrayNullable);
            AssertOrderedEnumerable(model1.UInt32ArrayNullable, model2.UInt32ArrayNullable);
            AssertOrderedEnumerable(model1.Int64ArrayNullable, model2.Int64ArrayNullable);
            AssertOrderedEnumerable(model1.UInt64ArrayNullable, model2.UInt64ArrayNullable);
            AssertOrderedEnumerable(model1.SingleArrayNullable, model2.SingleArrayNullable);
            AssertOrderedEnumerable(model1.DoubleArrayNullable, model2.DoubleArrayNullable);
            AssertOrderedEnumerable(model1.DecimalArrayNullable, model2.DecimalArrayNullable);
            AssertOrderedEnumerable(model1.CharArrayNullable, model2.CharArrayNullable);
            AssertOrderedEnumerable(model1.DateTimeArrayNullable, model2.DateTimeArrayNullable);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullable, model2.DateTimeOffsetArrayNullable);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullable, model2.TimeSpanArrayNullable);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullable, model2.DateOnlyArrayNullable);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullable, model2.TimeOnlyArrayNullable);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertOrderedEnumerable(model1.BooleanArrayNullableEmpty, model2.BooleanArrayNullableEmpty);
            AssertOrderedEnumerable(model1.ByteArrayNullableEmpty, model2.ByteArrayNullableEmpty);
            AssertOrderedEnumerable(model1.SByteArrayNullableEmpty, model2.SByteArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int16ArrayNullableEmpty, model2.Int16ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt16ArrayNullableEmpty, model2.UInt16ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int32ArrayNullableEmpty, model2.Int32ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt32ArrayNullableEmpty, model2.UInt32ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.Int64ArrayNullableEmpty, model2.Int64ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.UInt64ArrayNullableEmpty, model2.UInt64ArrayNullableEmpty);
            AssertOrderedEnumerable(model1.SingleArrayNullableEmpty, model2.SingleArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DoubleArrayNullableEmpty, model2.DoubleArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DecimalArrayNullableEmpty, model2.DecimalArrayNullableEmpty);
            AssertOrderedEnumerable(model1.CharArrayNullableEmpty, model2.CharArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeArrayNullableEmpty, model2.DateTimeArrayNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullableEmpty, model2.DateTimeOffsetArrayNullableEmpty);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullableEmpty, model2.TimeSpanArrayNullableEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullableEmpty, model2.DateOnlyArrayNullableEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullableEmpty, model2.TimeOnlyArrayNullableEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullableEmpty, model2.GuidArrayNullableEmpty);

            AssertOrderedEnumerable(model1.BooleanArrayNullableNull, model2.BooleanArrayNullableNull);
            AssertOrderedEnumerable(model1.ByteArrayNullableNull, model2.ByteArrayNullableNull);
            AssertOrderedEnumerable(model1.SByteArrayNullableNull, model2.SByteArrayNullableNull);
            AssertOrderedEnumerable(model1.Int16ArrayNullableNull, model2.Int16ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt16ArrayNullableNull, model2.UInt16ArrayNullableNull);
            AssertOrderedEnumerable(model1.Int32ArrayNullableNull, model2.Int32ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt32ArrayNullableNull, model2.UInt32ArrayNullableNull);
            AssertOrderedEnumerable(model1.Int64ArrayNullableNull, model2.Int64ArrayNullableNull);
            AssertOrderedEnumerable(model1.UInt64ArrayNullableNull, model2.UInt64ArrayNullableNull);
            AssertOrderedEnumerable(model1.SingleArrayNullableNull, model2.SingleArrayNullableNull);
            AssertOrderedEnumerable(model1.DoubleArrayNullableNull, model2.DoubleArrayNullableNull);
            AssertOrderedEnumerable(model1.DecimalArrayNullableNull, model2.DecimalArrayNullableNull);
            AssertOrderedEnumerable(model1.CharArrayNullableNull, model2.CharArrayNullableNull);
            AssertOrderedEnumerable(model1.DateTimeArrayNullableNull, model2.DateTimeArrayNullableNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetArrayNullableNull, model2.DateTimeOffsetArrayNullableNull);
            AssertOrderedEnumerable(model1.TimeSpanArrayNullableNull, model2.TimeSpanArrayNullableNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyArrayNullableNull, model2.DateOnlyArrayNullableNull);
            AssertOrderedEnumerable(model1.TimeOnlyArrayNullableNull, model2.TimeOnlyArrayNullableNull);
#endif
            AssertOrderedEnumerable(model1.GuidArrayNullableNull, model2.GuidArrayNullableNull);

            AssertOrderedEnumerable(model1.StringArray, model2.StringArray);
            AssertOrderedEnumerable(model1.StringArrayEmpty, model2.StringArrayEmpty);
            AssertOrderedEnumerable(model1.StringArrayNull, model2.StringArrayNull);

            AssertOrderedEnumerable(model1.EnumArray, model2.EnumArray);
            AssertOrderedEnumerable(model1.EnumArrayNullable, model2.EnumArrayNullable);

            AssertOrderedEnumerable(model1.BooleanList, model2.BooleanList);
            AssertOrderedEnumerable(model1.ByteList, model2.ByteList);
            AssertOrderedEnumerable(model1.SByteList, model2.SByteList);
            AssertOrderedEnumerable(model1.Int16List, model2.Int16List);
            AssertOrderedEnumerable(model1.UInt16List, model2.UInt16List);
            AssertOrderedEnumerable(model1.Int32List, model2.Int32List);
            AssertOrderedEnumerable(model1.UInt32List, model2.UInt32List);
            AssertOrderedEnumerable(model1.Int64List, model2.Int64List);
            AssertOrderedEnumerable(model1.UInt64List, model2.UInt64List);
            AssertOrderedEnumerable(model1.SingleList, model2.SingleList);
            AssertOrderedEnumerable(model1.DoubleList, model2.DoubleList);
            AssertOrderedEnumerable(model1.DecimalList, model2.DecimalList);
            AssertOrderedEnumerable(model1.CharList, model2.CharList);
            AssertOrderedEnumerable(model1.DateTimeList, model2.DateTimeList);
            AssertOrderedEnumerable(model1.DateTimeOffsetList, model2.DateTimeOffsetList);
            AssertOrderedEnumerable(model1.TimeSpanList, model2.TimeSpanList);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyList, model2.DateOnlyList);
            AssertOrderedEnumerable(model1.TimeOnlyList, model2.TimeOnlyList);
#endif
            AssertOrderedEnumerable(model1.GuidList, model2.GuidList);

            AssertOrderedEnumerable(model1.BooleanListEmpty, model2.BooleanListEmpty);
            AssertOrderedEnumerable(model1.ByteListEmpty, model2.ByteListEmpty);
            AssertOrderedEnumerable(model1.SByteListEmpty, model2.SByteListEmpty);
            AssertOrderedEnumerable(model1.Int16ListEmpty, model2.Int16ListEmpty);
            AssertOrderedEnumerable(model1.UInt16ListEmpty, model2.UInt16ListEmpty);
            AssertOrderedEnumerable(model1.Int32ListEmpty, model2.Int32ListEmpty);
            AssertOrderedEnumerable(model1.UInt32ListEmpty, model2.UInt32ListEmpty);
            AssertOrderedEnumerable(model1.Int64ListEmpty, model2.Int64ListEmpty);
            AssertOrderedEnumerable(model1.UInt64ListEmpty, model2.UInt64ListEmpty);
            AssertOrderedEnumerable(model1.SingleListEmpty, model2.SingleListEmpty);
            AssertOrderedEnumerable(model1.DoubleListEmpty, model2.DoubleListEmpty);
            AssertOrderedEnumerable(model1.DecimalListEmpty, model2.DecimalListEmpty);
            AssertOrderedEnumerable(model1.CharListEmpty, model2.CharListEmpty);
            AssertOrderedEnumerable(model1.DateTimeListEmpty, model2.DateTimeListEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetListEmpty, model2.DateTimeOffsetListEmpty);
            AssertOrderedEnumerable(model1.TimeSpanListEmpty, model2.TimeSpanListEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListEmpty, model2.DateOnlyListEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyListEmpty, model2.TimeOnlyListEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidListEmpty, model2.GuidListEmpty);

            AssertOrderedEnumerable(model1.BooleanListNull, model2.BooleanListNull);
            AssertOrderedEnumerable(model1.ByteListNull, model2.ByteListNull);
            AssertOrderedEnumerable(model1.SByteListNull, model2.SByteListNull);
            AssertOrderedEnumerable(model1.Int16ListNull, model2.Int16ListNull);
            AssertOrderedEnumerable(model1.UInt16ListNull, model2.UInt16ListNull);
            AssertOrderedEnumerable(model1.Int32ListNull, model2.Int32ListNull);
            AssertOrderedEnumerable(model1.UInt32ListNull, model2.UInt32ListNull);
            AssertOrderedEnumerable(model1.Int64ListNull, model2.Int64ListNull);
            AssertOrderedEnumerable(model1.UInt64ListNull, model2.UInt64ListNull);
            AssertOrderedEnumerable(model1.SingleListNull, model2.SingleListNull);
            AssertOrderedEnumerable(model1.DoubleListNull, model2.DoubleListNull);
            AssertOrderedEnumerable(model1.DecimalListNull, model2.DecimalListNull);
            AssertOrderedEnumerable(model1.CharListNull, model2.CharListNull);
            AssertOrderedEnumerable(model1.DateTimeListNull, model2.DateTimeListNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNull, model2.DateTimeOffsetListNull);
            AssertOrderedEnumerable(model1.TimeSpanListNull, model2.TimeSpanListNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNull, model2.DateOnlyListNull);
            AssertOrderedEnumerable(model1.TimeOnlyListNull, model2.TimeOnlyListNull);
#endif
            AssertOrderedEnumerable(model1.GuidListNull, model2.GuidListNull);

            AssertOrderedEnumerable(model1.BooleanListNullable, model2.BooleanListNullable);
            AssertOrderedEnumerable(model1.ByteListNullable, model2.ByteListNullable);
            AssertOrderedEnumerable(model1.SByteListNullable, model2.SByteListNullable);
            AssertOrderedEnumerable(model1.Int16ListNullable, model2.Int16ListNullable);
            AssertOrderedEnumerable(model1.UInt16ListNullable, model2.UInt16ListNullable);
            AssertOrderedEnumerable(model1.Int32ListNullable, model2.Int32ListNullable);
            AssertOrderedEnumerable(model1.UInt32ListNullable, model2.UInt32ListNullable);
            AssertOrderedEnumerable(model1.Int64ListNullable, model2.Int64ListNullable);
            AssertOrderedEnumerable(model1.UInt64ListNullable, model2.UInt64ListNullable);
            AssertOrderedEnumerable(model1.SingleListNullable, model2.SingleListNullable);
            AssertOrderedEnumerable(model1.DoubleListNullable, model2.DoubleListNullable);
            AssertOrderedEnumerable(model1.DecimalListNullable, model2.DecimalListNullable);
            AssertOrderedEnumerable(model1.CharListNullable, model2.CharListNullable);
            AssertOrderedEnumerable(model1.DateTimeListNullable, model2.DateTimeListNullable);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullable, model2.DateTimeOffsetListNullable);
            AssertOrderedEnumerable(model1.TimeSpanListNullable, model2.TimeSpanListNullable);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullable, model2.DateOnlyListNullable);
            AssertOrderedEnumerable(model1.TimeOnlyListNullable, model2.TimeOnlyListNullable);
#endif
            AssertOrderedEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertOrderedEnumerable(model1.BooleanListNullableEmpty, model2.BooleanListNullableEmpty);
            AssertOrderedEnumerable(model1.ByteListNullableEmpty, model2.ByteListNullableEmpty);
            AssertOrderedEnumerable(model1.SByteListNullableEmpty, model2.SByteListNullableEmpty);
            AssertOrderedEnumerable(model1.Int16ListNullableEmpty, model2.Int16ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt16ListNullableEmpty, model2.UInt16ListNullableEmpty);
            AssertOrderedEnumerable(model1.Int32ListNullableEmpty, model2.Int32ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt32ListNullableEmpty, model2.UInt32ListNullableEmpty);
            AssertOrderedEnumerable(model1.Int64ListNullableEmpty, model2.Int64ListNullableEmpty);
            AssertOrderedEnumerable(model1.UInt64ListNullableEmpty, model2.UInt64ListNullableEmpty);
            AssertOrderedEnumerable(model1.SingleListNullableEmpty, model2.SingleListNullableEmpty);
            AssertOrderedEnumerable(model1.DoubleListNullableEmpty, model2.DoubleListNullableEmpty);
            AssertOrderedEnumerable(model1.DecimalListNullableEmpty, model2.DecimalListNullableEmpty);
            AssertOrderedEnumerable(model1.CharListNullableEmpty, model2.CharListNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeListNullableEmpty, model2.DateTimeListNullableEmpty);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullableEmpty, model2.DateTimeOffsetListNullableEmpty);
            AssertOrderedEnumerable(model1.TimeSpanListNullableEmpty, model2.TimeSpanListNullableEmpty);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullableEmpty, model2.DateOnlyListNullableEmpty);
            AssertOrderedEnumerable(model1.TimeOnlyListNullableEmpty, model2.TimeOnlyListNullableEmpty);
#endif
            AssertOrderedEnumerable(model1.GuidListNullableEmpty, model2.GuidListNullableEmpty);

            AssertOrderedEnumerable(model1.BooleanListNullableNull, model2.BooleanListNullableNull);
            AssertOrderedEnumerable(model1.ByteListNullableNull, model2.ByteListNullableNull);
            AssertOrderedEnumerable(model1.SByteListNullableNull, model2.SByteListNullableNull);
            AssertOrderedEnumerable(model1.Int16ListNullableNull, model2.Int16ListNullableNull);
            AssertOrderedEnumerable(model1.UInt16ListNullableNull, model2.UInt16ListNullableNull);
            AssertOrderedEnumerable(model1.Int32ListNullableNull, model2.Int32ListNullableNull);
            AssertOrderedEnumerable(model1.UInt32ListNullableNull, model2.UInt32ListNullableNull);
            AssertOrderedEnumerable(model1.Int64ListNullableNull, model2.Int64ListNullableNull);
            AssertOrderedEnumerable(model1.UInt64ListNullableNull, model2.UInt64ListNullableNull);
            AssertOrderedEnumerable(model1.SingleListNullableNull, model2.SingleListNullableNull);
            AssertOrderedEnumerable(model1.DoubleListNullableNull, model2.DoubleListNullableNull);
            AssertOrderedEnumerable(model1.DecimalListNullableNull, model2.DecimalListNullableNull);
            AssertOrderedEnumerable(model1.CharListNullableNull, model2.CharListNullableNull);
            AssertOrderedEnumerable(model1.DateTimeListNullableNull, model2.DateTimeListNullableNull);
            AssertOrderedEnumerable(model1.DateTimeOffsetListNullableNull, model2.DateTimeOffsetListNullableNull);
            AssertOrderedEnumerable(model1.TimeSpanListNullableNull, model2.TimeSpanListNullableNull);
#if NET6_0_OR_GREATER
            AssertOrderedEnumerable(model1.DateOnlyListNullableNull, model2.DateOnlyListNullableNull);
            AssertOrderedEnumerable(model1.TimeOnlyListNullableNull, model2.TimeOnlyListNullableNull);
#endif
            AssertOrderedEnumerable(model1.GuidListNullableNull, model2.GuidListNullableNull);

            AssertOrderedEnumerable(model1.StringList, model2.StringList);
            AssertOrderedEnumerable(model1.StringListEmpty, model2.StringListEmpty);
            AssertOrderedEnumerable(model1.StringListNull, model2.StringListNull);

            AssertOrderedEnumerable(model1.EnumList, model2.EnumList);
            AssertOrderedEnumerable(model1.EnumListEmpty, model2.EnumListEmpty);
            AssertOrderedEnumerable(model1.EnumListNull, model2.EnumListNull);

            AssertOrderedEnumerable(model1.EnumListNullable, model2.EnumListNullable);
            AssertOrderedEnumerable(model1.EnumListNullableEmpty, model2.EnumListNullableEmpty);
            AssertOrderedEnumerable(model1.EnumListNullableNull, model2.EnumListNullableNull);

            if (model1.ClassThing != null)
            {
                Assert.IsNotNull(model2.ClassThing);
                Assert.AreEqual(model1.ClassThing.Value1, model2.ClassThing.Value1);
                Assert.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);
            }
            else
            {
                Assert.IsNull(model2.ClassThing);
            }

            Assert.IsNull(model1.ClassThingNull);
            Assert.IsNull(model2.ClassThingNull);

            if (model1.ClassArray != null)
            {
                Assert.IsNotNull(model2.ClassArray);
                Assert.AreEqual(model1.ClassArray.Length, model2.ClassArray.Length);
                for (var i = 0; i < model1.ClassArray.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArray[i].Value1, model2.ClassArray[i].Value1);
                    Assert.AreEqual(model1.ClassArray[i].Value2, model2.ClassArray[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArray);
            }

            if (model1.ClassArrayEmpty != null)
            {
                Assert.IsNotNull(model2.ClassArrayEmpty);
                Assert.AreEqual(model1.ClassArrayEmpty.Length, model2.ClassArrayEmpty.Length);
                for (var i = 0; i < model1.ClassArrayEmpty.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArrayEmpty[i].Value1, model2.ClassArrayEmpty[i].Value1);
                    Assert.AreEqual(model1.ClassArrayEmpty[i].Value2, model2.ClassArrayEmpty[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArrayEmpty);
            }

            if (model1.ClassArrayNull != null)
            {
                Assert.IsNotNull(model2.ClassArrayNull);
                Assert.AreEqual(model1.ClassArrayNull.Length, model2.ClassArrayNull.Length);
                for (var i = 0; i < model1.ClassArrayNull.Length; i++)
                {
                    Assert.AreEqual(model1.ClassArrayNull[i].Value1, model2.ClassArrayNull[i].Value1);
                    Assert.AreEqual(model1.ClassArrayNull[i].Value2, model2.ClassArrayNull[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassArrayNull);
            }

            if (model1.ClassEnumerable != null)
            {
                Assert.IsNotNull(model2.ClassEnumerable);
                var items1 = new List<BasicModel>();
                foreach (var item in model1.ClassEnumerable)
                    items1.Add(item);
                var items2 = new List<BasicModel>();
                foreach (var item in model2.ClassEnumerable)
                    items2.Add(item);

                Assert.AreEqual(items1.Count, items2.Count);
                for (var i = 0; i < items1.Count; i++)
                {
                    Assert.AreEqual(items1[i].Value1, items2[i].Value1);
                    Assert.AreEqual(items1[i].Value2, items2[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassEnumerable);
            }

            if (model1.ClassList != null)
            {
                Assert.AreEqual(model1.ClassList.Count, model2.ClassList.Count);
                for (var i = 0; i < model1.ClassList.Count; i++)
                {
                    Assert.AreEqual(model1.ClassList[i].Value1, model2.ClassList[i].Value1);
                    Assert.AreEqual(model1.ClassList[i].Value2, model2.ClassList[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassList);
            }

            if (model1.ClassListEmpty != null)
            {
                Assert.AreEqual(model1.ClassListEmpty.Count, model2.ClassListEmpty.Count);
                for (var i = 0; i < model1.ClassListEmpty.Count; i++)
                {
                    Assert.AreEqual(model1.ClassListEmpty[i].Value1, model2.ClassListEmpty[i].Value1);
                    Assert.AreEqual(model1.ClassListEmpty[i].Value2, model2.ClassListEmpty[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassListEmpty);
            }

            if (model1.ClassListNull != null)
            {
                Assert.AreEqual(model1.ClassListNull.Count, model2.ClassListNull.Count);
                for (var i = 0; i < model1.ClassListNull.Count; i++)
                {
                    Assert.AreEqual(model1.ClassListNull[i].Value1, model2.ClassListNull[i].Value1);
                    Assert.AreEqual(model1.ClassListNull[i].Value2, model2.ClassListNull[i].Value2);
                }
            }
            else
            {
                Assert.IsNull(model2.ClassListNull);
            }

            if (model1.DictionaryThing != null)
            {
                Assert.IsNotNull(model2.DictionaryThing);
                Assert.AreEqual(model1.DictionaryThing.Count, model2.DictionaryThing.Count);
                foreach (var key in model1.DictionaryThing.Keys)
                {
                    Assert.IsTrue(model2.DictionaryThing.ContainsKey(key));
                    Assert.AreEqual(model1.DictionaryThing[key], model2.DictionaryThing[key]);
                }
            }
            else
            {
                Assert.IsNull(model2.DictionaryThing);
            }

            if (model1.StringArrayOfArrayThing != null)
            {
                Assert.IsNotNull(model2.StringArrayOfArrayThing);
                Assert.AreEqual(model1.StringArrayOfArrayThing.Length, model2.StringArrayOfArrayThing.Length);
                for (var i = 0; i < model1.StringArrayOfArrayThing.Length; i++)
                {
                    if (model1.StringArrayOfArrayThing[i] == null)
                    {
                        Assert.IsNull(model2.StringArrayOfArrayThing[i]);
                    }
                    else
                    {
                        Assert.AreEqual(model1.StringArrayOfArrayThing[i].Length, model2.StringArrayOfArrayThing[i].Length);
                        for (var j = 0; j < model1.StringArrayOfArrayThing[i].Length; j++)
                        {
                            Assert.AreEqual(model1.StringArrayOfArrayThing[i][j], model2.StringArrayOfArrayThing[i][j]);
                        }
                    }
                }
            }
            else
            {
                Assert.IsNull(model2.StringArrayOfArrayThing);
            }
        }
        public static void AssertAreEqual(AllTypesModel model1, AllTypesAsStringsModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.BooleanThing.ToString().ToLower(), model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing.ToString(), model2.ByteThing);
            Assert.AreEqual(model1.SByteThing.ToString(), model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing.ToString(), model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing.ToString(), model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing.ToString(), model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing.ToString(), model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing.ToString(), model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing.ToString(), model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing.ToString(), model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing.ToString(), model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing.ToString(), model2.DecimalThing);
            Assert.AreEqual(model1.CharThing.ToString(), model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing, DateTime.Parse(model2.DateTimeThing, null, DateTimeStyles.RoundtripKind)); //extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.DateTimeOffsetThing, DateTimeOffset.Parse(model2.DateTimeOffsetThing, null, DateTimeStyles.RoundtripKind));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeSpanThing, TimeSpan.Parse(model2.TimeSpanThing));
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing, DateOnly.Parse(model2.DateOnlyThing)); //extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeOnlyThing, TimeOnly.Parse(model2.TimeOnlyThing));
#endif
            Assert.AreEqual(model1.GuidThing, Guid.Parse(model2.GuidThing));

            Assert.AreEqual(model1.BooleanThingNullable?.ToString().ToLower(), model2.BooleanThingNullable);
            Assert.AreEqual(model1.ByteThingNullable?.ToString(), model2.ByteThingNullable);
            Assert.AreEqual(model1.SByteThingNullable?.ToString(), model2.SByteThingNullable);
            Assert.AreEqual(model1.Int16ThingNullable?.ToString(), model2.Int16ThingNullable);
            Assert.AreEqual(model1.UInt16ThingNullable?.ToString(), model2.UInt16ThingNullable);
            Assert.AreEqual(model1.Int32ThingNullable?.ToString(), model2.Int32ThingNullable);
            Assert.AreEqual(model1.UInt32ThingNullable?.ToString(), model2.UInt32ThingNullable);
            Assert.AreEqual(model1.Int64ThingNullable?.ToString(), model2.Int64ThingNullable);
            Assert.AreEqual(model1.UInt64ThingNullable?.ToString(), model2.UInt64ThingNullable);
            Assert.AreEqual(model1.SingleThingNullable?.ToString(), model2.SingleThingNullable);
            Assert.AreEqual(model1.DoubleThingNullable?.ToString(), model2.DoubleThingNullable);
            Assert.AreEqual(model1.DecimalThingNullable?.ToString(), model2.DecimalThingNullable);
            Assert.AreEqual(model1.CharThingNullable?.ToString(), model2.CharThingNullable);
            Assert.AreEqual(model1.DateTimeThingNullable, DateTime.Parse(model2.DateTimeThingNullable, null, DateTimeStyles.RoundtripKind));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.DateTimeOffsetThingNullable, DateTimeOffset.Parse(model2.DateTimeOffsetThingNullable));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeSpanThingNullable, TimeSpan.Parse(model2.TimeSpanThingNullable));
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThingNullable, DateOnly.Parse(model2.DateOnlyThingNullable)); //extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeOnlyThingNullable, TimeOnly.Parse(model2.TimeOnlyThingNullable));
#endif
            Assert.AreEqual(model1.GuidThingNullable, Guid.Parse(model2.GuidThingNullable));

            Assert.IsNull(model1.ByteThingNullableNull);
            Assert.IsNull(model1.SByteThingNullableNull);
            Assert.IsNull(model1.Int16ThingNullableNull);
            Assert.IsNull(model1.UInt16ThingNullableNull);
            Assert.IsNull(model1.Int32ThingNullableNull);
            Assert.IsNull(model1.UInt32ThingNullableNull);
            Assert.IsNull(model1.Int64ThingNullableNull);
            Assert.IsNull(model1.UInt64ThingNullableNull);
            Assert.IsNull(model1.SingleThingNullableNull);
            Assert.IsNull(model1.DoubleThingNullableNull);
            Assert.IsNull(model1.DecimalThingNullableNull);
            Assert.IsNull(model1.CharThingNullableNull);
            Assert.IsNull(model1.DateTimeThingNullableNull);
            Assert.IsNull(model1.DateTimeOffsetThingNullableNull);
            Assert.IsNull(model1.TimeSpanThingNullableNull);
#if NET6_0_OR_GREATER
            Assert.IsNull(model1.DateOnlyThingNullableNull);
            Assert.IsNull(model1.TimeOnlyThingNullableNull);
#endif
            Assert.IsNull(model1.GuidThingNullableNull);

            Assert.AreEqual(model1.EnumThing.ToString(), model2.EnumThing);
            Assert.AreEqual(model1.EnumThingNullable?.ToString(), model2.EnumThingNullable);
            Assert.AreEqual(model1.EnumThingNullableNull?.ToString(), model2.EnumThingNullableNull);

            Assert.AreEqual(model1.StringThing, model2.StringThing);

            Assert.IsNull(model1.StringThingNull);
            Assert.IsNull(model2.StringThingNull);

            Assert.AreEqual(String.Empty, model1.StringThingEmpty);
            Assert.AreEqual(String.Empty, model2.StringThingEmpty);
        }

        public static BasicTypesNotNullable GetBasicTypesNotNullableModel()
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
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing);
            Assert.AreEqual(model1.ByteThing, model2.ByteThing);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing);
            Assert.AreEqual(model1.CharThing, model2.CharThing);
            Assert.AreEqual(model1.DateTimeThing, model2.DateTimeThing);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing);
            Assert.AreEqual(model1.GuidThing, model2.GuidThing);

            Assert.AreEqual(model1.EnumThing, model2.EnumThing);
        }
        public static void AssertAreEqual(BasicTypesNotNullable model1, BasicTypesNullable model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.BooleanThing, model2.BooleanThing.Value);
            Assert.AreEqual(model1.ByteThing, model2.ByteThing.Value);
            Assert.AreEqual(model1.SByteThing, model2.SByteThing.Value);
            Assert.AreEqual(model1.Int16Thing, model2.Int16Thing.Value);
            Assert.AreEqual(model1.UInt16Thing, model2.UInt16Thing.Value);
            Assert.AreEqual(model1.Int32Thing, model2.Int32Thing.Value);
            Assert.AreEqual(model1.UInt32Thing, model2.UInt32Thing.Value);
            Assert.AreEqual(model1.Int64Thing, model2.Int64Thing.Value);
            Assert.AreEqual(model1.UInt64Thing, model2.UInt64Thing.Value);
            Assert.AreEqual(model1.SingleThing, model2.SingleThing.Value);
            Assert.AreEqual(model1.DoubleThing, model2.DoubleThing.Value);
            Assert.AreEqual(model1.DecimalThing, model2.DecimalThing.Value);
            Assert.AreEqual(model1.CharThing, model2.CharThing.Value);
            Assert.AreEqual(model1.DateTimeThing, model2.DateTimeThing.Value);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing.Value);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing.Value);
#if NET6_0_OR_GREATER
            Assert.AreEqual(model1.DateOnlyThing, model2.DateOnlyThing.Value);
            Assert.AreEqual(model1.TimeOnlyThing, model2.TimeOnlyThing.Value);
#endif
            Assert.AreEqual(model1.GuidThing, model2.GuidThing.Value);

            Assert.AreEqual(model1.EnumThing, model2.EnumThing.Value);
        }

        public static TestBoxingModel GetBoxingModel()
        {
            var model = new TestBoxingModel()
            {
                BoxedInterfaceThing = new BasicModel() { Value1 = 10, Value2 = "S-10" },
                BoxedObjectThing = new BasicModel() { Value1 = 11, Value2 = "S-11" }
            };
            return model;
        }
        public static void AssertAreEqual(TestBoxingModel model1, TestBoxingModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.IsNotNull(model1.BoxedInterfaceThing);
            Assert.IsNotNull(model2.BoxedInterfaceThing);
            var interfaceModel1 = model1.BoxedInterfaceThing as BasicModel;
            var interfaceModel2 = model2.BoxedInterfaceThing as BasicModel;
            Assert.IsNotNull(interfaceModel1);
            Assert.IsNotNull(interfaceModel2);
            Assert.AreEqual(interfaceModel1.Value1, interfaceModel2.Value1);
            Assert.AreEqual(interfaceModel1.Value2, interfaceModel2.Value2);

            Assert.IsNotNull(model1.BoxedObjectThing);
            Assert.IsNotNull(model2.BoxedObjectThing);
            var objectModel1 = model1.BoxedObjectThing as BasicModel;
            var objectModel2 = model2.BoxedObjectThing as BasicModel;
            Assert.IsNotNull(objectModel1);
            Assert.IsNotNull(objectModel2);
            Assert.AreEqual(objectModel1.Value1, objectModel2.Value1);
            Assert.AreEqual(objectModel1.Value2, objectModel2.Value2);
        }

        public static BasicModel[] GetArrayModel()
        {
            var model = new BasicModel[]
            {
                new() { Value1 = 1, Value2 = "S-1" },
                new() { Value1 = 2, Value2 = "S-2" },
                new() { Value1 = 3, Value2 = "S-3" }
            };
            return model;
        }
        public static void AssertAreEqual(BasicModel[] model1, BasicModel[] model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.Length, model2.Length);
            for (var i = 0; i < model1.Length; i++)
            {
                Assert.AreEqual(model1[i]?.Value1, model2[i]?.Value1);
                Assert.AreEqual(model1[i]?.Value2, model2[i]?.Value2);
            }
        }

        public static TestSerializerIndexModel1 GetSerializerIndexModel()
        {
            return new TestSerializerIndexModel1()
            {
                Value1 = 11,
                Value2 = 22,
                Value3 = 33
            };
        }
        public static void AssertAreEqual(TestSerializerIndexModel1 model1, TestSerializerIndexModel2 model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreEqual(model1.Value1, model2.Value1);
            Assert.AreEqual(model1.Value2, model2.Value2);
            Assert.AreEqual(model1.Value3, model2.Value3);
        }
        public static void AssertAreNotEqual(TestSerializerIndexModel1 model1, TestSerializerIndexModel2 model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual<object>(model1, model2);

            Assert.AreNotEqual(model1.Value1, model2.Value1);
            Assert.AreNotEqual(model1.Value2, model2.Value2);
            Assert.AreNotEqual(model1.Value3, model2.Value3);
        }

        public static TestSerializerLongIndexModel GetSerializerLongIndexModel()
        {
            return new TestSerializerLongIndexModel()
            {
                Value1 = 11,
                Value2 = 22,
                Value3 = 33
            };
        }
        public static void AssertAreEqual(TestSerializerLongIndexModel model1, TestSerializerLongIndexModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.Value1, model2.Value1);
            Assert.AreEqual(model1.Value2, model2.Value2);
            Assert.AreEqual(model1.Value3, model2.Value3);
        }

        public static HashSetModel GetHashSetModel()
        {
            var model = new HashSetModel()
            {
                BooleanHashSet = new HashSet<bool> { true, false, true },
                ByteHashSet = new HashSet<byte> { 1, 2, 3 },
                SByteHashSet = new HashSet<sbyte> { 4, 5, 6 },
                Int16HashSet = new HashSet<short> { 7, 8, 9 },
                UInt16HashSet = new HashSet<ushort> { 10, 11, 12 },
                Int32HashSet = new HashSet<int> { 13, 14, 15 },
                UInt32HashSet = new HashSet<uint> { 16, 17, 18 },
                Int64HashSet = new HashSet<long> { 19, 20, 21 },
                UInt64HashSet = new HashSet<ulong> { 22, 23, 24 },
                SingleHashSet = new HashSet<float> { 25, 26, 27 },
                DoubleHashSet = new HashSet<double> { 28, 29, 30 },
                DecimalHashSet = new HashSet<decimal> { 31, 32, 33 },
                CharHashSet = new HashSet<char> { 'A', 'B', 'C' },
                DateTimeHashSet = new HashSet<DateTime> { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSet = new HashSet<DateTimeOffset> { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSet = new HashSet<TimeSpan> { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSet = new HashSet<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSet = new HashSet<TimeOnly> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSet = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanHashSetNullable = new HashSet<bool?> { true, null, true },
                ByteHashSetNullable = new HashSet<byte?> { 1, null, 3 },
                SByteHashSetNullable = new HashSet<sbyte?> { 4, null, 6 },
                Int16HashSetNullable = new HashSet<short?> { 7, null, 9 },
                UInt16HashSetNullable = new HashSet<ushort?> { 10, null, 12 },
                Int32HashSetNullable = new HashSet<int?> { 13, null, 15 },
                UInt32HashSetNullable = new HashSet<uint?> { 16, null, 18 },
                Int64HashSetNullable = new HashSet<long?> { 19, null, 21 },
                UInt64HashSetNullable = new HashSet<ulong?> { 22, null, 24 },
                SingleHashSetNullable = new HashSet<float?> { 25, null, 27 },
                DoubleHashSetNullable = new HashSet<double?> { 28, null, 30 },
                DecimalHashSetNullable = new HashSet<decimal?> { 31, null, 33 },
                CharHashSetNullable = new HashSet<char?> { 'A', null, 'C' },
                DateTimeHashSetNullable = new HashSet<DateTime?> { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSetNullable = new HashSet<DateTimeOffset?> { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSetNullable = new HashSet<TimeSpan?> { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSetNullable = new HashSet<DateOnly?> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSetNullable = new HashSet<TimeOnly?> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSetNullable = new HashSet<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringHashSet = new HashSet<string> { "Hello", "World", "People" },

                ClassHashSet = new HashSet<BasicModel> { new() { Value1 = 1, Value2 = "1" }, new() { Value1 = 2, Value2 = "2" }, null, new() { Value1 = 3, Value2 = "3" } }
            };

            return model;
        }
        public static void AssertAreEqual(HashSetModel model1, HashSetModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            AssertUnorderedEnumerable(model1.BooleanHashSet, model2.BooleanHashSet);
            AssertUnorderedEnumerable(model1.ByteHashSet, model2.ByteHashSet);
            AssertUnorderedEnumerable(model1.SByteHashSet, model2.SByteHashSet);
            AssertUnorderedEnumerable(model1.Int16HashSet, model2.Int16HashSet);
            AssertUnorderedEnumerable(model1.UInt16HashSet, model2.UInt16HashSet);
            AssertUnorderedEnumerable(model1.Int32HashSet, model2.Int32HashSet);
            AssertUnorderedEnumerable(model1.UInt32HashSet, model2.UInt32HashSet);
            AssertUnorderedEnumerable(model1.Int64HashSet, model2.Int64HashSet);
            AssertUnorderedEnumerable(model1.UInt64HashSet, model2.UInt64HashSet);
            AssertUnorderedEnumerable(model1.SingleHashSet, model2.SingleHashSet);
            AssertUnorderedEnumerable(model1.DoubleHashSet, model2.DoubleHashSet);
            AssertUnorderedEnumerable(model1.DecimalHashSet, model2.DecimalHashSet);
            AssertUnorderedEnumerable(model1.CharHashSet, model2.CharHashSet);
            AssertUnorderedEnumerable(model1.DateTimeHashSet, model2.DateTimeHashSet);
            AssertUnorderedEnumerable(model1.DateTimeOffsetHashSet, model2.DateTimeOffsetHashSet);
            AssertUnorderedEnumerable(model1.TimeSpanHashSet, model2.TimeSpanHashSet);
#if NET6_0_OR_GREATER
            AssertUnorderedEnumerable(model1.DateOnlyHashSet, model2.DateOnlyHashSet);
            AssertUnorderedEnumerable(model1.TimeOnlyHashSet, model2.TimeOnlyHashSet);
#endif
            AssertUnorderedEnumerable(model1.GuidHashSet, model2.GuidHashSet);

            AssertUnorderedEnumerable(model1.BooleanHashSetNullable, model2.BooleanHashSetNullable);
            AssertUnorderedEnumerable(model1.ByteHashSetNullable, model2.ByteHashSetNullable);
            AssertUnorderedEnumerable(model1.SByteHashSetNullable, model2.SByteHashSetNullable);
            AssertUnorderedEnumerable(model1.Int16HashSetNullable, model2.Int16HashSetNullable);
            AssertUnorderedEnumerable(model1.UInt16HashSetNullable, model2.UInt16HashSetNullable);
            AssertUnorderedEnumerable(model1.Int32HashSetNullable, model2.Int32HashSetNullable);
            AssertUnorderedEnumerable(model1.UInt32HashSetNullable, model2.UInt32HashSetNullable);
            AssertUnorderedEnumerable(model1.Int64HashSetNullable, model2.Int64HashSetNullable);
            AssertUnorderedEnumerable(model1.UInt64HashSetNullable, model2.UInt64HashSetNullable);
            AssertUnorderedEnumerable(model1.SingleHashSetNullable, model2.SingleHashSetNullable);
            AssertUnorderedEnumerable(model1.DoubleHashSetNullable, model2.DoubleHashSetNullable);
            AssertUnorderedEnumerable(model1.DecimalHashSetNullable, model2.DecimalHashSetNullable);
            AssertUnorderedEnumerable(model1.CharHashSetNullable, model2.CharHashSetNullable);
            AssertUnorderedEnumerable(model1.DateTimeHashSetNullable, model2.DateTimeHashSetNullable);
            AssertUnorderedEnumerable(model1.DateTimeOffsetHashSetNullable, model2.DateTimeOffsetHashSetNullable);
            AssertUnorderedEnumerable(model1.TimeSpanHashSetNullable, model2.TimeSpanHashSetNullable);
#if NET6_0_OR_GREATER
            AssertUnorderedEnumerable(model1.DateOnlyHashSetNullable, model2.DateOnlyHashSetNullable);
            AssertUnorderedEnumerable(model1.TimeOnlyHashSetNullable, model2.TimeOnlyHashSetNullable);
#endif
            AssertUnorderedEnumerable(model1.GuidHashSetNullable, model2.GuidHashSetNullable);

            AssertUnorderedEnumerable(model1.StringHashSet, model2.StringHashSet);

            if (model1.ClassHashSet != null)
            {
                Assert.AreEqual(model1.ClassHashSet.Count, model2.ClassHashSet.Count);
                foreach(var item1 in model1.ClassHashSet)
                {
                    if (item1 == null)
                    {
                        Assert.IsTrue(model2.ClassHashSet.Any(x => x == null));
                    }
                    else
                    {
                        var item2 = model2.ClassHashSet.FirstOrDefault(x => x != null && x.Value1 == item1.Value1 && x.Value2 == item1.Value2);
                        Assert.IsNotNull(item2);
                    }
                }
            }
            else
            {
                Assert.IsNull(model2.ClassHashSet);
            }
        }
    }
}
