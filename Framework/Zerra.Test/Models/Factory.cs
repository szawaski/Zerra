// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

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
                DateTimeThing = DateTime.Now,
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                TimeSpanThing = DateTime.Now.TimeOfDay,
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
                DateTimeThingNullable = DateTime.Now.AddMonths(1),
                DateTimeOffsetThingNullable = DateTimeOffset.Now.AddMonths(1).AddDays(1),
                TimeSpanThingNullable = DateTime.Now.AddHours(1).TimeOfDay,
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
            Assert.AreEqual(model1.DateTimeThing, model2.DateTimeThing);
            Assert.AreEqual(model1.DateTimeOffsetThing, model2.DateTimeOffsetThing);
            Assert.AreEqual(model1.TimeSpanThing, model2.TimeSpanThing);
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
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                GuidThing = Guid.NewGuid(),

                ByteThingNullable = 11,
                Int16ThingNullable = -13,
                Int32ThingNullable = -15,
                Int64ThingNullable = -17,
                SingleThingNullable = -19.1f,
                DecimalThingNullable = -111.3m,
                DateTimeThingNullable = DateTime.Now.AddMonths(1),
                TimeSpanThingNullable = DateTime.Now.AddHours(1).TimeOfDay,
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
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                TimeSpanThing = DateTime.Now.TimeOfDay,
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
                DateTimeThingNullable = DateTime.Now.AddMonths(1),
                DateTimeOffsetThingNullable = DateTimeOffset.Now.AddMonths(1).AddDays(1),
                TimeSpanThingNullable = DateTime.Now.AddHours(1).TimeOfDay,
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
                GuidThingNullableNull = null,

                StringThing = "Hello\r\nWorld!",
                StringThingNull = null,
                StringThingEmpty = String.Empty,

                EnumThing = EnumModel.EnumItem1,
                EnumThingNullable = EnumModel.EnumItem2,
                EnumThingNullableNull = null,

                BooleanArray = new bool[] { true, false, true },
                ByteArray = new byte[] { 1, 2, 3 },
                SByteArray = new sbyte[] { 4, 5, 6 },
                Int16Array = new short[] { 7, 8, 9 },
                UInt16Array = new ushort[] { 10, 11, 12 },
                Int32Array = new int[] { 13, 14, 15 },
                UInt32Array = new uint[] { 16, 17, 18 },
                Int64Array = new long[] { 19, 20, 21 },
                UInt64Array = new ulong[] { 22, 23, 24 },
                SingleArray = new float[] { 25, 26, 27 },
                DoubleArray = new double[] { 28, 29, 30 },
                DecimalArray = new decimal[] { 31, 32, 33 },
                CharArray = new char[] { 'A', 'B', 'C' },
                DateTimeArray = new DateTime[] { DateTime.Now.AddMonths(1), DateTime.Now.AddMonths(2), DateTime.Now.AddMonths(3) },
                DateTimeOffsetArray = new DateTimeOffset[] { DateTimeOffset.Now.AddMonths(4), DateTimeOffset.Now.AddMonths(5), DateTimeOffset.Now.AddMonths(6) },
                TimeSpanArray = new TimeSpan[] { DateTime.Now.AddHours(1).TimeOfDay, DateTime.Now.AddHours(2).TimeOfDay, DateTime.Now.AddHours(3).TimeOfDay },
                GuidArray = new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanArrayNullable = new bool?[] { true, null, true },
                ByteArrayNullable = new byte?[] { 1, null, 3 },
                SByteArrayNullable = new sbyte?[] { 4, null, 6 },
                Int16ArrayNullable = new short?[] { 7, null, 9 },
                UInt16ArrayNullable = new ushort?[] { 10, null, 12 },
                Int32ArrayNullable = new int?[] { 13, null, 15 },
                UInt32ArrayNullable = new uint?[] { 16, null, 18 },
                Int64ArrayNullable = new long?[] { 19, null, 21 },
                UInt64ArrayNullable = new ulong?[] { 22, null, 24 },
                SingleArrayNullable = new float?[] { 25, null, 27 },
                DoubleArrayNullable = new double?[] { 28, null, 30 },
                DecimalArrayNullable = new decimal?[] { 31, null, 33 },
                CharArrayNullable = new char?[] { 'A', null, 'C' },
                DateTimeArrayNullable = new DateTime?[] { DateTime.Now.AddMonths(1), null, DateTime.Now.AddMonths(3) },
                DateTimeOffsetArrayNullable = new DateTimeOffset?[] { DateTimeOffset.Now.AddMonths(4), null, DateTimeOffset.Now.AddMonths(6) },
                TimeSpanArrayNullable = new TimeSpan?[] { DateTime.Now.AddHours(1).TimeOfDay, null, DateTime.Now.AddHours(3).TimeOfDay },
                GuidArrayNullable = new Guid?[] { Guid.NewGuid(), null, Guid.NewGuid() },

                StringArray = new string[] { "Hello", "World", "People", "", null },
                StringEmptyArray = Array.Empty<string>(),

                EnumArray = new EnumModel[] { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumArrayNullable = new EnumModel?[] { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },

                BooleanList = new List<bool> { true, false, true },
                ByteList = new List<byte> { 1, 2, 3 },
                SByteList = new List<sbyte> { 4, 5, 6 },
                Int16List = new List<short> { 7, 8, 9 },
                UInt16List = new List<ushort> { 10, 11, 12 },
                Int32List = new List<int> { 13, 14, 15 },
                UInt32List = new List<uint> { 16, 17, 18 },
                Int64List = new List<long> { 19, 20, 21 },
                UInt64List = new List<ulong> { 22, 23, 24 },
                SingleList = new List<float> { 25, 26, 27 },
                DoubleList = new List<double> { 28, 29, 30 },
                DecimalList = new List<decimal> { 31, 32, 33 },
                CharList = new List<char> { 'A', 'B', 'C' },
                DateTimeList = new List<DateTime> { DateTime.Now.AddMonths(1), DateTime.Now.AddMonths(2), DateTime.Now.AddMonths(3) },
                DateTimeOffsetList = new List<DateTimeOffset> { DateTimeOffset.Now.AddMonths(4), DateTimeOffset.Now.AddMonths(5), DateTimeOffset.Now.AddMonths(6) },
                TimeSpanList = new List<TimeSpan> { DateTime.Now.AddHours(1).TimeOfDay, DateTime.Now.AddHours(2).TimeOfDay, DateTime.Now.AddHours(3).TimeOfDay },
                GuidList = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanListNullable = new List<bool?> { true, null, true },
                ByteListNullable = new List<byte?> { 1, null, 3 },
                SByteListNullable = new List<sbyte?> { 4, null, 6 },
                Int16ListNullable = new List<short?> { 7, null, 9 },
                UInt16ListNullable = new List<ushort?> { 10, null, 12 },
                Int32ListNullable = new List<int?> { 13, null, 15 },
                UInt32ListNullable = new List<uint?> { 16, null, 18 },
                Int64ListNullable = new List<long?> { 19, null, 21 },
                UInt64ListNullable = new List<ulong?> { 22, null, 24 },
                SingleListNullable = new List<float?> { 25, null, 27 },
                DoubleListNullable = new List<double?> { 28, null, 30 },
                DecimalListNullable = new List<decimal?> { 31, null, 33 },
                CharListNullable = new List<char?> { 'A', null, 'C' },
                DateTimeListNullable = new List<DateTime?> { DateTime.Now.AddMonths(1), null, DateTime.Now.AddMonths(3) },
                DateTimeOffsetListNullable = new List<DateTimeOffset?> { DateTimeOffset.Now.AddMonths(4), null, DateTimeOffset.Now.AddMonths(6) },
                TimeSpanListNullable = new List<TimeSpan?> { DateTime.Now.AddHours(1).TimeOfDay, null, DateTime.Now.AddHours(3).TimeOfDay },
                GuidListNullable = new List<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringList = new List<string> { "Hello", "World", "People" },

                EnumList = new List<EnumModel> { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumListNullable = new List<EnumModel?> { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },

                ClassThing = new BasicModel { Value1 = 1234, Value2 = "S-1234" },
                ClassThingNull = null,
                ClassArray = new BasicModel[] { new BasicModel { Value1 = 10, Value2 = "S-10" }, new BasicModel { Value1 = 11, Value2 = "S-11" } },
                ClassEnumerable = new List<BasicModel>() { new BasicModel { Value1 = 12, Value2 = "S-12" }, new BasicModel { Value1 = 13, Value2 = "S-13" } },
                ClassList = new List<BasicModel>() { new BasicModel { Value1 = 14, Value2 = "S-14" }, new BasicModel { Value1 = 15, Value2 = "S-15" } },

                DictionaryThing = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                StringArrayOfArrayThing = new string[][] { new string[] { "a", "b", "c" }, null, new string[] { "d", "e", "f" }, new string[] { "", null, "" } }
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
            Assert.AreEqual(model1.GuidThingNullable, model2.GuidThingNullable);


            Assert.IsNull(model1.BooleanThingNullableNull);
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
            Assert.IsNull(model1.GuidThingNullableNull);

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
            AssertOrderedEnumerable(model1.GuidArray, model2.GuidArray);

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
            AssertOrderedEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertOrderedEnumerable(model1.StringArray, model2.StringArray);
            AssertOrderedEnumerable(model1.StringEmptyArray, model2.StringEmptyArray);

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
            AssertOrderedEnumerable(model1.GuidList, model2.GuidList);

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
            AssertOrderedEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertOrderedEnumerable(model1.StringList, model2.StringList);

            AssertOrderedEnumerable(model1.EnumList, model2.EnumList);
            AssertOrderedEnumerable(model1.EnumListNullable, model2.EnumListNullable);

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
            Assert.AreEqual(model1.GuidThingNullable, model2.GuidThingNullable);


            Assert.IsNull(model1.BooleanThingNullableNull);
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
            Assert.IsNull(model1.GuidThingNullableNull);

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
            AssertOrderedEnumerable(model1.GuidArray, model2.GuidArray);

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
            AssertOrderedEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertOrderedEnumerable(model1.StringArray, model2.StringArray);
            AssertOrderedEnumerable(model1.StringEmptyArray, model2.StringEmptyArray);

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
            AssertOrderedEnumerable(model1.GuidList, model2.GuidList);

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
            AssertOrderedEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertOrderedEnumerable(model1.StringList, model2.StringList);

            AssertOrderedEnumerable(model1.EnumList, model2.EnumList);
            AssertOrderedEnumerable(model1.EnumListNullable, model2.EnumListNullable);

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
            Assert.AreEqual(model1.DateTimeThingNullable, DateTime.Parse(model2.DateTimeThingNullable));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.DateTimeOffsetThingNullable, DateTimeOffset.Parse(model2.DateTimeOffsetThingNullable));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeSpanThingNullable, TimeSpan.Parse(model2.TimeSpanThingNullable));
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
            Assert.IsNull(model1.GuidThingNullableNull);

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
            Assert.IsNull(model2.GuidThingNullableNull);

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
                DateTimeThing = DateTime.Now,
                DateTimeOffsetThing = DateTimeOffset.Now.AddDays(1),
                TimeSpanThing = DateTime.Now.TimeOfDay,
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
                new BasicModel() { Value1 = 1, Value2 = "S-1" },
                new BasicModel() { Value1 = 2, Value2 = "S-2" },
                new BasicModel() { Value1 = 3, Value2 = "S-3" }
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
                DateTimeHashSet = new HashSet<DateTime> { DateTime.Now.AddMonths(1), DateTime.Now.AddMonths(2), DateTime.Now.AddMonths(3) },
                DateTimeOffsetHashSet = new HashSet<DateTimeOffset> { DateTimeOffset.Now.AddMonths(4), DateTimeOffset.Now.AddMonths(5), DateTimeOffset.Now.AddMonths(6) },
                TimeSpanHashSet = new HashSet<TimeSpan> { DateTime.Now.AddHours(1).TimeOfDay, DateTime.Now.AddHours(2).TimeOfDay, DateTime.Now.AddHours(3).TimeOfDay },
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
                DateTimeHashSetNullable = new HashSet<DateTime?> { DateTime.Now.AddMonths(1), null, DateTime.Now.AddMonths(3) },
                DateTimeOffsetHashSetNullable = new HashSet<DateTimeOffset?> { DateTimeOffset.Now.AddMonths(4), null, DateTimeOffset.Now.AddMonths(6) },
                TimeSpanHashSetNullable = new HashSet<TimeSpan?> { DateTime.Now.AddHours(1).TimeOfDay, null, DateTime.Now.AddHours(3).TimeOfDay },
                GuidHashSetNullable = new HashSet<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringHashSet = new HashSet<string> { "Hello", "World", "People" },
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
            AssertUnorderedEnumerable(model1.GuidHashSetNullable, model2.GuidHashSetNullable);

            AssertUnorderedEnumerable(model1.StringHashSet, model2.StringHashSet);
        }
    }
}
