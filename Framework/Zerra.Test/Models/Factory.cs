// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Test
{
    public static class Factory
    {
        private static void AssertEnumerable(IEnumerable model1, IEnumerable model2)
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
            Assert.AreNotEqual(model1, model2);

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

                EnumThing = EnumModel.Item1,
                EnumThingNullable = EnumModel.Item2,
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

                StringArray = new string[] { "Hello", "World", "People" },
                StringEmptyArray = new string[0],

                EnumArray = new EnumModel[] { EnumModel.Item1, EnumModel.Item2, EnumModel.Item3 },
                EnumArrayNullable = new EnumModel?[] { EnumModel.Item1, null, EnumModel.Item3 },

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

                EnumList = new List<EnumModel> { EnumModel.Item1, EnumModel.Item2, EnumModel.Item3 },
                EnumListNullable = new List<EnumModel?> { EnumModel.Item1, null, EnumModel.Item3 },

                ClassThing = new BasicModel { Value = 1234 },
                ClassThingNull = null,
                ClassArray = new BasicModel[] { new BasicModel { Value = 10 }, new BasicModel { Value = 11 } },
                ClassEnumerable = new List<BasicModel>() { new BasicModel { Value = 12 }, new BasicModel { Value = 13 } },
                ClassList = new List<BasicModel>() { new BasicModel { Value = 14 }, new BasicModel { Value = 15 } },

                DictionaryThing = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                StringArrayOfArrayThing = new string[][] { new string[] { "a", "b", "c" }, null, new string[] { "d", "e", "f" } }
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

            AssertEnumerable(model1.BooleanArray, model2.BooleanArray);
            AssertEnumerable(model1.ByteArray, model2.ByteArray);
            AssertEnumerable(model1.SByteArray, model2.SByteArray);
            AssertEnumerable(model1.Int16Array, model2.Int16Array);
            AssertEnumerable(model1.UInt16Array, model2.UInt16Array);
            AssertEnumerable(model1.Int32Array, model2.Int32Array);
            AssertEnumerable(model1.UInt32Array, model2.UInt32Array);
            AssertEnumerable(model1.Int64Array, model2.Int64Array);
            AssertEnumerable(model1.UInt64Array, model2.UInt64Array);
            AssertEnumerable(model1.SingleArray, model2.SingleArray);
            AssertEnumerable(model1.DoubleArray, model2.DoubleArray);
            AssertEnumerable(model1.DecimalArray, model2.DecimalArray);
            AssertEnumerable(model1.CharArray, model2.CharArray);
            AssertEnumerable(model1.DateTimeArray, model2.DateTimeArray);
            AssertEnumerable(model1.DateTimeOffsetArray, model2.DateTimeOffsetArray);
            AssertEnumerable(model1.TimeSpanArray, model2.TimeSpanArray);
            AssertEnumerable(model1.GuidArray, model2.GuidArray);

            AssertEnumerable(model1.BooleanArrayNullable, model2.BooleanArrayNullable);
            AssertEnumerable(model1.ByteArrayNullable, model2.ByteArrayNullable);
            AssertEnumerable(model1.SByteArrayNullable, model2.SByteArrayNullable);
            AssertEnumerable(model1.Int16ArrayNullable, model2.Int16ArrayNullable);
            AssertEnumerable(model1.UInt16ArrayNullable, model2.UInt16ArrayNullable);
            AssertEnumerable(model1.Int32ArrayNullable, model2.Int32ArrayNullable);
            AssertEnumerable(model1.UInt32ArrayNullable, model2.UInt32ArrayNullable);
            AssertEnumerable(model1.Int64ArrayNullable, model2.Int64ArrayNullable);
            AssertEnumerable(model1.UInt64ArrayNullable, model2.UInt64ArrayNullable);
            AssertEnumerable(model1.SingleArrayNullable, model2.SingleArrayNullable);
            AssertEnumerable(model1.DoubleArrayNullable, model2.DoubleArrayNullable);
            AssertEnumerable(model1.DecimalArrayNullable, model2.DecimalArrayNullable);
            AssertEnumerable(model1.CharArrayNullable, model2.CharArrayNullable);
            AssertEnumerable(model1.DateTimeArrayNullable, model2.DateTimeArrayNullable);
            AssertEnumerable(model1.DateTimeOffsetArrayNullable, model2.DateTimeOffsetArrayNullable);
            AssertEnumerable(model1.TimeSpanArrayNullable, model2.TimeSpanArrayNullable);
            AssertEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertEnumerable(model1.StringArray, model2.StringArray);
            AssertEnumerable(model1.StringEmptyArray, model2.StringEmptyArray);

            AssertEnumerable(model1.EnumArray, model2.EnumArray);
            AssertEnumerable(model1.EnumArrayNullable, model2.EnumArrayNullable);

            AssertEnumerable(model1.BooleanList, model2.BooleanList);
            AssertEnumerable(model1.ByteList, model2.ByteList);
            AssertEnumerable(model1.SByteList, model2.SByteList);
            AssertEnumerable(model1.Int16List, model2.Int16List);
            AssertEnumerable(model1.UInt16List, model2.UInt16List);
            AssertEnumerable(model1.Int32List, model2.Int32List);
            AssertEnumerable(model1.UInt32List, model2.UInt32List);
            AssertEnumerable(model1.Int64List, model2.Int64List);
            AssertEnumerable(model1.UInt64List, model2.UInt64List);
            AssertEnumerable(model1.SingleList, model2.SingleList);
            AssertEnumerable(model1.DoubleList, model2.DoubleList);
            AssertEnumerable(model1.DecimalList, model2.DecimalList);
            AssertEnumerable(model1.CharList, model2.CharList);
            AssertEnumerable(model1.DateTimeList, model2.DateTimeList);
            AssertEnumerable(model1.DateTimeOffsetList, model2.DateTimeOffsetList);
            AssertEnumerable(model1.TimeSpanList, model2.TimeSpanList);
            AssertEnumerable(model1.GuidList, model2.GuidList);

            AssertEnumerable(model1.BooleanListNullable, model2.BooleanListNullable);
            AssertEnumerable(model1.ByteListNullable, model2.ByteListNullable);
            AssertEnumerable(model1.SByteListNullable, model2.SByteListNullable);
            AssertEnumerable(model1.Int16ListNullable, model2.Int16ListNullable);
            AssertEnumerable(model1.UInt16ListNullable, model2.UInt16ListNullable);
            AssertEnumerable(model1.Int32ListNullable, model2.Int32ListNullable);
            AssertEnumerable(model1.UInt32ListNullable, model2.UInt32ListNullable);
            AssertEnumerable(model1.Int64ListNullable, model2.Int64ListNullable);
            AssertEnumerable(model1.UInt64ListNullable, model2.UInt64ListNullable);
            AssertEnumerable(model1.SingleListNullable, model2.SingleListNullable);
            AssertEnumerable(model1.DoubleListNullable, model2.DoubleListNullable);
            AssertEnumerable(model1.DecimalListNullable, model2.DecimalListNullable);
            AssertEnumerable(model1.CharListNullable, model2.CharListNullable);
            AssertEnumerable(model1.DateTimeListNullable, model2.DateTimeListNullable);
            AssertEnumerable(model1.DateTimeOffsetListNullable, model2.DateTimeOffsetListNullable);
            AssertEnumerable(model1.TimeSpanListNullable, model2.TimeSpanListNullable);
            AssertEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertEnumerable(model1.StringList, model2.StringList);

            AssertEnumerable(model1.EnumList, model2.EnumList);
            AssertEnumerable(model1.EnumListNullable, model2.EnumListNullable);

            if (model1.ClassThing != null)
            {
                Assert.IsNotNull(model2.ClassThing);
                Assert.AreEqual(model1.ClassThing.Value, model2.ClassThing.Value);
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
                    Assert.AreEqual(model1.ClassArray[i].Value, model2.ClassArray[i].Value);
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
                    Assert.AreEqual(items1[i].Value, items2[i].Value);
            }
            else
            {
                Assert.IsNull(model2.ClassEnumerable);
            }

            if (model1.ClassList != null)
            {
                Assert.AreEqual(model1.ClassList.Count, model2.ClassList.Count);
                for (var i = 0; i < model1.ClassList.Count; i++)
                    Assert.AreEqual(model1.ClassList[i].Value, model2.ClassList[i].Value);
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

            AssertEnumerable(model1.BooleanArray, model2.BooleanArray);
            AssertEnumerable(model1.ByteArray, model2.ByteArray);
            AssertEnumerable(model1.SByteArray, model2.SByteArray);
            AssertEnumerable(model1.Int16Array, model2.Int16Array);
            AssertEnumerable(model1.UInt16Array, model2.UInt16Array);
            AssertEnumerable(model1.Int32Array, model2.Int32Array);
            AssertEnumerable(model1.UInt32Array, model2.UInt32Array);
            AssertEnumerable(model1.Int64Array, model2.Int64Array);
            AssertEnumerable(model1.UInt64Array, model2.UInt64Array);
            AssertEnumerable(model1.SingleArray, model2.SingleArray);
            AssertEnumerable(model1.DoubleArray, model2.DoubleArray);
            AssertEnumerable(model1.DecimalArray, model2.DecimalArray);
            AssertEnumerable(model1.CharArray, model2.CharArray);
            AssertEnumerable(model1.DateTimeArray, model2.DateTimeArray);
            AssertEnumerable(model1.DateTimeOffsetArray, model2.DateTimeOffsetArray);
            AssertEnumerable(model1.TimeSpanArray, model2.TimeSpanArray);
            AssertEnumerable(model1.GuidArray, model2.GuidArray);

            AssertEnumerable(model1.BooleanArrayNullable, model2.BooleanArrayNullable);
            AssertEnumerable(model1.ByteArrayNullable, model2.ByteArrayNullable);
            AssertEnumerable(model1.SByteArrayNullable, model2.SByteArrayNullable);
            AssertEnumerable(model1.Int16ArrayNullable, model2.Int16ArrayNullable);
            AssertEnumerable(model1.UInt16ArrayNullable, model2.UInt16ArrayNullable);
            AssertEnumerable(model1.Int32ArrayNullable, model2.Int32ArrayNullable);
            AssertEnumerable(model1.UInt32ArrayNullable, model2.UInt32ArrayNullable);
            AssertEnumerable(model1.Int64ArrayNullable, model2.Int64ArrayNullable);
            AssertEnumerable(model1.UInt64ArrayNullable, model2.UInt64ArrayNullable);
            AssertEnumerable(model1.SingleArrayNullable, model2.SingleArrayNullable);
            AssertEnumerable(model1.DoubleArrayNullable, model2.DoubleArrayNullable);
            AssertEnumerable(model1.DecimalArrayNullable, model2.DecimalArrayNullable);
            AssertEnumerable(model1.CharArrayNullable, model2.CharArrayNullable);
            AssertEnumerable(model1.DateTimeArrayNullable, model2.DateTimeArrayNullable);
            AssertEnumerable(model1.DateTimeOffsetArrayNullable, model2.DateTimeOffsetArrayNullable);
            AssertEnumerable(model1.TimeSpanArrayNullable, model2.TimeSpanArrayNullable);
            AssertEnumerable(model1.GuidArrayNullable, model2.GuidArrayNullable);

            AssertEnumerable(model1.StringArray, model2.StringArray);
            AssertEnumerable(model1.StringEmptyArray, model2.StringEmptyArray);

            AssertEnumerable(model1.EnumArray, model2.EnumArray);
            AssertEnumerable(model1.EnumArrayNullable, model2.EnumArrayNullable);

            AssertEnumerable(model1.BooleanList, model2.BooleanList);
            AssertEnumerable(model1.ByteList, model2.ByteList);
            AssertEnumerable(model1.SByteList, model2.SByteList);
            AssertEnumerable(model1.Int16List, model2.Int16List);
            AssertEnumerable(model1.UInt16List, model2.UInt16List);
            AssertEnumerable(model1.Int32List, model2.Int32List);
            AssertEnumerable(model1.UInt32List, model2.UInt32List);
            AssertEnumerable(model1.Int64List, model2.Int64List);
            AssertEnumerable(model1.UInt64List, model2.UInt64List);
            AssertEnumerable(model1.SingleList, model2.SingleList);
            AssertEnumerable(model1.DoubleList, model2.DoubleList);
            AssertEnumerable(model1.DecimalList, model2.DecimalList);
            AssertEnumerable(model1.CharList, model2.CharList);
            AssertEnumerable(model1.DateTimeList, model2.DateTimeList);
            AssertEnumerable(model1.DateTimeOffsetList, model2.DateTimeOffsetList);
            AssertEnumerable(model1.TimeSpanList, model2.TimeSpanList);
            AssertEnumerable(model1.GuidList, model2.GuidList);

            AssertEnumerable(model1.BooleanListNullable, model2.BooleanListNullable);
            AssertEnumerable(model1.ByteListNullable, model2.ByteListNullable);
            AssertEnumerable(model1.SByteListNullable, model2.SByteListNullable);
            AssertEnumerable(model1.Int16ListNullable, model2.Int16ListNullable);
            AssertEnumerable(model1.UInt16ListNullable, model2.UInt16ListNullable);
            AssertEnumerable(model1.Int32ListNullable, model2.Int32ListNullable);
            AssertEnumerable(model1.UInt32ListNullable, model2.UInt32ListNullable);
            AssertEnumerable(model1.Int64ListNullable, model2.Int64ListNullable);
            AssertEnumerable(model1.UInt64ListNullable, model2.UInt64ListNullable);
            AssertEnumerable(model1.SingleListNullable, model2.SingleListNullable);
            AssertEnumerable(model1.DoubleListNullable, model2.DoubleListNullable);
            AssertEnumerable(model1.DecimalListNullable, model2.DecimalListNullable);
            AssertEnumerable(model1.CharListNullable, model2.CharListNullable);
            AssertEnumerable(model1.DateTimeListNullable, model2.DateTimeListNullable);
            AssertEnumerable(model1.DateTimeOffsetListNullable, model2.DateTimeOffsetListNullable);
            AssertEnumerable(model1.TimeSpanListNullable, model2.TimeSpanListNullable);
            AssertEnumerable(model1.GuidListNullable, model2.GuidListNullable);

            AssertEnumerable(model1.StringList, model2.StringList);

            AssertEnumerable(model1.EnumList, model2.EnumList);
            AssertEnumerable(model1.EnumListNullable, model2.EnumListNullable);

            if (model1.ClassThing != null)
            {
                Assert.IsNotNull(model2.ClassThing);
                Assert.AreEqual(model1.ClassThing.Value, model2.ClassThing.Value);
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
                    Assert.AreEqual(model1.ClassArray[i].Value, model2.ClassArray[i].Value);
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
                    Assert.AreEqual(items1[i].Value, items2[i].Value);
            }
            else
            {
                Assert.IsNull(model2.ClassEnumerable);
            }

            if (model1.ClassList != null)
            {
                Assert.AreEqual(model1.ClassList.Count, model2.ClassList.Count);
                for (var i = 0; i < model1.ClassList.Count; i++)
                    Assert.AreEqual(model1.ClassList[i].Value, model2.ClassList[i].Value);
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
            Assert.AreNotEqual(model1, model2);

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
            Assert.AreEqual(model1.DateTimeThing, DateTime.Parse(model2.DateTimeThing)); //extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.DateTimeOffsetThing, DateTimeOffset.Parse(model2.DateTimeOffsetThing));//extra zeros removed at end of fractional sectons
            Assert.AreEqual(model1.TimeSpanThing.ToString(), model2.TimeSpanThing);
            Assert.AreEqual(model1.GuidThing.ToString(), model2.GuidThing);

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
            Assert.AreEqual(model1.TimeSpanThingNullable?.ToString(), model2.TimeSpanThingNullable);
            Assert.AreEqual(model1.GuidThingNullable?.ToString(), model2.GuidThingNullable);

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

        public static TestBoxingModel GetBoxingModel()
        {
            var model = new TestBoxingModel()
            {
                BoxedThing = new BasicModel() { Value = 10 }
            };
            return model;
        }
        public static void AssertAreEqual(TestBoxingModel model1, TestBoxingModel model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

            Assert.IsNotNull(model1.BoxedThing);
            Assert.IsNotNull(model2.BoxedThing);
            var subModel1 = model1.BoxedThing as BasicModel;
            var subModel2 = model2.BoxedThing as BasicModel;
            Assert.IsNotNull(subModel1);
            Assert.IsNotNull(subModel2);
            Assert.AreEqual(subModel1.Value, subModel2.Value);
        }

        public static BasicModel[] GetArrayModel()
        {
            var model = new BasicModel[]
            {
                new BasicModel() { Value = 1 },
                new BasicModel() { Value = 2 },
                new BasicModel() { Value = 3 }
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
                Assert.AreEqual(model1[i]?.Value, model2[i]?.Value);
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
            Assert.AreNotEqual(model1, model2);

            Assert.AreEqual(model1.Value1, model2.Value1);
            Assert.AreEqual(model1.Value2, model2.Value2);
            Assert.AreEqual(model1.Value3, model2.Value3);
        }
        public static void AssertAreNotEqual(TestSerializerIndexModel1 model1, TestSerializerIndexModel2 model2)
        {
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.AreNotEqual(model1, model2);

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
    }
}
