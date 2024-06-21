// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zerra.Serialization.Json;

namespace Zerra.Test
{
    [TestClass]
    public class JsonSerializerTest
    {
        public JsonSerializerTest()
        {
#if DEBUG
            JsonSerializerOld.Testing = true;
#endif
        }

        [TestMethod]
        public void StringMatchesNewtonsoft()
        {
            var baseModel = AllTypesModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            var model1 = JsonSerializerOld.Deserialize<AllTypesModel>(json2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = AllTypesModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel, options);

            Assert.IsTrue(json1 == json2);

            //swap serializers
            var model1 = JsonSerializerOld.Deserialize<AllTypesModel>(json2);
            var model2 = System.Text.Json.JsonSerializer.Deserialize<AllTypesModel>(json1, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypes()
        {
            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var model = JsonSerializerOld.Deserialize<AllTypesModel>(json);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringEnumAsNumbers()
        {
            var options = new JsonSerializerOptionsOld()
            {
                EnumAsNumber = true
            };

            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));
            var model = JsonSerializerOld.Deserialize<AllTypesModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringConvertNullables()
        {
            var baseModel = BasicTypesNotNullable.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var model1 = JsonSerializerOld.Deserialize<BasicTypesNullable>(json1);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model1);

            var json2 = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<BasicTypesNotNullable>(json2);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model2);
        }

        [TestMethod]
        public void StringConvertTypes()
        {
            var baseModel = AllTypesModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var model1 = JsonSerializerOld.Deserialize<AllTypesAsStringsModel>(json1);
            AllTypesModel.AreEqual(baseModel, model1);

            var json2 = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<AllTypesModel>(json2);
            AssertHelper.AreEqual(baseModel, model2);
        }

        [TestMethod]
        public void StringNumbers()
        {
            for (var i = -10; i < 10; i++)
                StringTestNumber(i);
            for (decimal i = -2; i < 2; i += 0.1m)
                StringTestNumber(i);

            StringTestNumber(Byte.MinValue);
            StringTestNumber(Byte.MaxValue);
            StringTestNumber(SByte.MinValue);
            StringTestNumber(SByte.MaxValue);

            StringTestNumber(Int16.MinValue);
            StringTestNumber(Int16.MaxValue);
            StringTestNumber(UInt16.MinValue);
            StringTestNumber(UInt16.MaxValue);

            StringTestNumber(Int32.MinValue);
            StringTestNumber(Int32.MaxValue);
            StringTestNumber(UInt32.MinValue);
            StringTestNumber(UInt32.MaxValue);

            StringTestNumber(Int64.MinValue);
            StringTestNumber(Int64.MaxValue);
            StringTestNumber(UInt64.MinValue);
            StringTestNumber(UInt64.MaxValue);

            StringTestNumber(Single.MinValue);
            StringTestNumber(Single.MaxValue);

            StringTestNumber(Double.MinValue);
            StringTestNumber(Double.MaxValue);

            StringTestNumber(Decimal.MinValue);
            StringTestNumber(Decimal.MaxValue);

            StringTestNumberAsString(Double.MinValue);
            StringTestNumberAsString(Double.MaxValue);

            StringTestNumberAsString(Decimal.MinValue);
            StringTestNumberAsString(Decimal.MaxValue);
        }
        private static void StringTestNumber(byte value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<byte>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(sbyte value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<sbyte>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(short value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<short>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(ushort value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<ushort>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(int value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<int>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(uint value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<uint>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(long value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<long>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(ulong value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<ulong>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(decimal value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<decimal>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(float value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<float>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(double value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<double>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumberAsString(double value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<string>(json);
            Assert.AreEqual(json, result);
        }
        private static void StringTestNumberAsString(decimal value)
        {
            var json = JsonSerializerOld.Serialize(value);
            var result = JsonSerializerOld.Deserialize<string>(json);
            Assert.AreEqual(json, result);
        }

        [TestMethod]
        public void StringEnumConversion()
        {
            //var model1 = new EnumConversionModel1() { Thing = EnumModel.Item2 };
            //var test1 = JsonSerializer.Serialize(model1);
            //var result1 = JsonSerializer.Deserialize<EnumConversionModel2>(test1);
            //Assert.AreEqual((int)model1.Thing, result1.Thing);

            var model2 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = 2,
                Thing3 = 3,
                Thing4 = 4
            };

            var json2 = JsonSerializerOld.Serialize(model2);
            var result2 = JsonSerializerOld.Deserialize<EnumConversionModel1>(json2);
            Assert.AreEqual(model2.Thing1, (int)result2.Thing1);
            Assert.AreEqual(model2.Thing2, (int?)result2.Thing2);
            Assert.AreEqual(model2.Thing3, (int)result2.Thing3);
            Assert.AreEqual(model2.Thing4, (int?)result2.Thing4);

            var model3 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = null,
                Thing3 = 3,
                Thing4 = null
            };

            var json3 = JsonSerializerOld.Serialize(model3);
            var result3 = JsonSerializerOld.Deserialize<EnumConversionModel1>(json3);
            Assert.AreEqual(model3.Thing1, (int)result3.Thing1);
            Assert.AreEqual(default, result3.Thing2);
            Assert.AreEqual(model3.Thing3, (int)result3.Thing3);
            Assert.AreEqual(model3.Thing4, (int?)result3.Thing4);
        }

        [TestMethod]
        public void StringPretty()
        {
            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            string jsonPretty;
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader);
                var jsonWriter = new Newtonsoft.Json.JsonTextWriter(stringWriter) { Formatting = Newtonsoft.Json.Formatting.Indented, Indentation = 4 };
                jsonWriter.WriteToken(jsonReader);
                jsonPretty = stringWriter.ToString();
            }
            var model = JsonSerializerOld.Deserialize<AllTypesModel>(jsonPretty);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringNameless()
        {
            var options = new JsonSerializerOptionsOld()
            {
                Nameless = true
            };

            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            var model = JsonSerializerOld.Deserialize<AllTypesModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptionsOld()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            var model = JsonSerializerOld.Deserialize<AllTypesModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringEmptys()
        {
            var json1 = JsonSerializerOld.Serialize<string>(null);
            Assert.AreEqual("null", json1);

            var json2 = JsonSerializerOld.Serialize<string>(String.Empty);
            Assert.AreEqual("\"\"", json2);

            var json3 = JsonSerializerOld.Serialize<object>(null);
            Assert.AreEqual("null", json3);

            var json4 = JsonSerializerOld.Serialize<object>(new object());
            Assert.AreEqual("{}", json4);

            var model1 = JsonSerializerOld.Deserialize<string>("null");
            Assert.IsNull(model1);

            var model2 = JsonSerializerOld.Deserialize<string>("");
            Assert.AreEqual("", model2);

            var model3 = JsonSerializerOld.Deserialize<string>("\"\"");
            Assert.AreEqual("", model3);

            var model4 = JsonSerializerOld.Deserialize<string>("{}");
            Assert.AreEqual("", model4);

            var model5 = JsonSerializerOld.Deserialize<object>("null");
            Assert.IsNull(model5);

            var model6 = JsonSerializerOld.Deserialize<object>("");
            Assert.IsNull(model6);

            var model7 = JsonSerializerOld.Deserialize<object>("\"\"");
            Assert.IsNull(model7);

            var model8 = JsonSerializerOld.Deserialize<object>("{}");
            Assert.IsNotNull(model8);
        }

        [TestMethod]
        public void StringEscaping()
        {
            for (var i = 0; i < (int)byte.MaxValue; i++)
            {
                var c = (char)i;
                var json = JsonSerializerOld.Serialize(c);
                var result = JsonSerializerOld.Deserialize<char>(json);
                Assert.AreEqual(c, result);

                switch (c)
                {
                    case '\\':
                    case '"':
                    case '/':
                    case '\b':
                    case '\t':
                    case '\n':
                    case '\f':
                    case '\r':
                        Assert.AreEqual(4, json.Length);
                        break;
                    default:
                        if (c < ' ')
                            Assert.AreEqual(8, json.Length);
                        break;
                }
            }
        }

        [TestMethod]
        public void StringJsonObject()
        {
            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var jsonObject = JsonSerializerOld.DeserializeJsonObject(json);

            var json2 = jsonObject.ToString();

            Assert.AreEqual(json, json2);

            var model1 = new AllTypesModel();

            model1.BooleanThing = (bool)jsonObject[nameof(AllTypesModel.BooleanThing)];
            model1.ByteThing = (byte)jsonObject[nameof(AllTypesModel.ByteThing)];
            model1.SByteThing = (sbyte)jsonObject[nameof(AllTypesModel.SByteThing)];
            model1.Int16Thing = (short)jsonObject[nameof(AllTypesModel.Int16Thing)];
            model1.UInt16Thing = (ushort)jsonObject[nameof(AllTypesModel.UInt16Thing)];
            model1.Int32Thing = (int)jsonObject[nameof(AllTypesModel.Int32Thing)];
            model1.UInt32Thing = (uint)jsonObject[nameof(AllTypesModel.UInt32Thing)];
            model1.Int64Thing = (long)jsonObject[nameof(AllTypesModel.Int64Thing)];
            model1.UInt64Thing = (ulong)jsonObject[nameof(AllTypesModel.UInt64Thing)];
            model1.SingleThing = (float)jsonObject[nameof(AllTypesModel.SingleThing)];
            model1.DoubleThing = (double)jsonObject[nameof(AllTypesModel.DoubleThing)];
            model1.DecimalThing = (decimal)jsonObject[nameof(AllTypesModel.DecimalThing)];
            model1.CharThing = (char)jsonObject[nameof(AllTypesModel.CharThing)];
            model1.DateTimeThing = (DateTime)jsonObject[nameof(AllTypesModel.DateTimeThing)];
            model1.DateTimeOffsetThing = (DateTimeOffset)jsonObject[nameof(AllTypesModel.DateTimeOffsetThing)];
            model1.TimeSpanThing = (TimeSpan)jsonObject[nameof(AllTypesModel.TimeSpanThing)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThing = (DateOnly)jsonObject[nameof(AllTypesModel.DateOnlyThing)];
            model1.TimeOnlyThing = (TimeOnly)jsonObject[nameof(AllTypesModel.TimeOnlyThing)];
#endif
            model1.GuidThing = (Guid)jsonObject[nameof(AllTypesModel.GuidThing)];

            model1.BooleanThingNullable = (bool?)jsonObject[nameof(AllTypesModel.BooleanThingNullable)];
            model1.ByteThingNullable = (byte?)jsonObject[nameof(AllTypesModel.ByteThingNullable)];
            model1.SByteThingNullable = (sbyte?)jsonObject[nameof(AllTypesModel.SByteThingNullable)];
            model1.Int16ThingNullable = (short?)jsonObject[nameof(AllTypesModel.Int16ThingNullable)];
            model1.UInt16ThingNullable = (ushort?)jsonObject[nameof(AllTypesModel.UInt16ThingNullable)];
            model1.Int32ThingNullable = (int?)jsonObject[nameof(AllTypesModel.Int32ThingNullable)];
            model1.UInt32ThingNullable = (uint?)jsonObject[nameof(AllTypesModel.UInt32ThingNullable)];
            model1.Int64ThingNullable = (long?)jsonObject[nameof(AllTypesModel.Int64ThingNullable)];
            model1.UInt64ThingNullable = (ulong?)jsonObject[nameof(AllTypesModel.UInt64ThingNullable)];
            model1.SingleThingNullable = (float?)jsonObject[nameof(AllTypesModel.SingleThingNullable)];
            model1.DoubleThingNullable = (double?)jsonObject[nameof(AllTypesModel.DoubleThingNullable)];
            model1.DecimalThingNullable = (decimal?)jsonObject[nameof(AllTypesModel.DecimalThingNullable)];
            model1.CharThingNullable = (char?)jsonObject[nameof(AllTypesModel.CharThingNullable)];
            model1.DateTimeThingNullable = (DateTime?)jsonObject[nameof(AllTypesModel.DateTimeThingNullable)];
            model1.DateTimeOffsetThingNullable = (DateTimeOffset?)jsonObject[nameof(AllTypesModel.DateTimeOffsetThingNullable)];
            model1.TimeSpanThingNullable = (TimeSpan?)jsonObject[nameof(AllTypesModel.TimeSpanThingNullable)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThingNullable = (DateOnly?)jsonObject[nameof(AllTypesModel.DateOnlyThingNullable)];
            model1.TimeOnlyThingNullable = (TimeOnly?)jsonObject[nameof(AllTypesModel.TimeOnlyThingNullable)];
#endif
            model1.GuidThingNullable = (Guid?)jsonObject[nameof(AllTypesModel.GuidThingNullable)];

            model1.BooleanThingNullableNull = (bool?)jsonObject[nameof(AllTypesModel.BooleanThingNullableNull)];
            model1.ByteThingNullableNull = (byte?)jsonObject[nameof(AllTypesModel.ByteThingNullableNull)];
            model1.SByteThingNullableNull = (sbyte?)jsonObject[nameof(AllTypesModel.SByteThingNullableNull)];
            model1.Int16ThingNullableNull = (short?)jsonObject[nameof(AllTypesModel.Int16ThingNullableNull)];
            model1.UInt16ThingNullableNull = (ushort?)jsonObject[nameof(AllTypesModel.UInt16ThingNullableNull)];
            model1.Int32ThingNullableNull = (int?)jsonObject[nameof(AllTypesModel.Int32ThingNullableNull)];
            model1.UInt32ThingNullableNull = (uint?)jsonObject[nameof(AllTypesModel.UInt32ThingNullableNull)];
            model1.Int64ThingNullableNull = (long?)jsonObject[nameof(AllTypesModel.Int64ThingNullableNull)];
            model1.UInt64ThingNullableNull = (ulong?)jsonObject[nameof(AllTypesModel.UInt64ThingNullableNull)];
            model1.SingleThingNullableNull = (float?)jsonObject[nameof(AllTypesModel.SingleThingNullableNull)];
            model1.DoubleThingNullableNull = (double?)jsonObject[nameof(AllTypesModel.DoubleThingNullableNull)];
            model1.DecimalThingNullableNull = (decimal?)jsonObject[nameof(AllTypesModel.DecimalThingNullableNull)];
            model1.CharThingNullableNull = (char?)jsonObject[nameof(AllTypesModel.CharThingNullableNull)];
            model1.DateTimeThingNullableNull = (DateTime?)jsonObject[nameof(AllTypesModel.DateTimeThingNullableNull)];
            model1.DateTimeOffsetThingNullableNull = (DateTimeOffset?)jsonObject[nameof(AllTypesModel.DateTimeOffsetThingNullableNull)];
            model1.TimeSpanThingNullableNull = (TimeSpan?)jsonObject[nameof(AllTypesModel.TimeSpanThingNullableNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThingNullableNull = (DateOnly?)jsonObject[nameof(AllTypesModel.DateOnlyThingNullableNull)];
            model1.TimeOnlyThingNullableNull = (TimeOnly?)jsonObject[nameof(AllTypesModel.TimeOnlyThingNullableNull)];
#endif
            model1.GuidThingNullableNull = (Guid?)jsonObject[nameof(AllTypesModel.GuidThingNullableNull)];

            model1.StringThing = (string)jsonObject[nameof(AllTypesModel.StringThing)];
            model1.StringThingNull = (string)jsonObject[nameof(AllTypesModel.StringThingNull)];
            model1.StringThingEmpty = (string)jsonObject[nameof(AllTypesModel.StringThingEmpty)];

            model1.EnumThing = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThing)]);
            model1.EnumThingNullable = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThingNullable)]);
            model1.EnumThingNullableNull = ((string)jsonObject[nameof(AllTypesModel.EnumThingNullableNull)]) == null ? (EnumModel?)null : EnumModel.EnumItem1;

            model1.BooleanArray = (bool[])jsonObject[nameof(AllTypesModel.BooleanArray)];
            model1.ByteArray = (byte[])jsonObject[nameof(AllTypesModel.ByteArray)];
            model1.SByteArray = (sbyte[])jsonObject[nameof(AllTypesModel.SByteArray)];
            model1.Int16Array = (short[])jsonObject[nameof(AllTypesModel.Int16Array)];
            model1.UInt16Array = (ushort[])jsonObject[nameof(AllTypesModel.UInt16Array)];
            model1.Int32Array = (int[])jsonObject[nameof(AllTypesModel.Int32Array)];
            model1.UInt32Array = (uint[])jsonObject[nameof(AllTypesModel.UInt32Array)];
            model1.Int64Array = (long[])jsonObject[nameof(AllTypesModel.Int64Array)];
            model1.UInt64Array = (ulong[])jsonObject[nameof(AllTypesModel.UInt64Array)];
            model1.SingleArray = (float[])jsonObject[nameof(AllTypesModel.SingleArray)];
            model1.DoubleArray = (double[])jsonObject[nameof(AllTypesModel.DoubleArray)];
            model1.DecimalArray = (decimal[])jsonObject[nameof(AllTypesModel.DecimalArray)];
            model1.CharArray = (char[])jsonObject[nameof(AllTypesModel.CharArray)];
            model1.DateTimeArray = (DateTime[])jsonObject[nameof(AllTypesModel.DateTimeArray)];
            model1.DateTimeOffsetArray = (DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArray)];
            model1.TimeSpanArray = (TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanArray)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArray = (DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyArray)];
            model1.TimeOnlyArray = (TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyArray)];
#endif
            model1.GuidArray = (Guid[])jsonObject[nameof(AllTypesModel.GuidArray)];

            model1.BooleanArrayEmpty = (bool[])jsonObject[nameof(AllTypesModel.BooleanArrayEmpty)];
            model1.ByteArrayEmpty = (byte[])jsonObject[nameof(AllTypesModel.ByteArrayEmpty)];
            model1.SByteArrayEmpty = (sbyte[])jsonObject[nameof(AllTypesModel.SByteArrayEmpty)];
            model1.Int16ArrayEmpty = (short[])jsonObject[nameof(AllTypesModel.Int16ArrayEmpty)];
            model1.UInt16ArrayEmpty = (ushort[])jsonObject[nameof(AllTypesModel.UInt16ArrayEmpty)];
            model1.Int32ArrayEmpty = (int[])jsonObject[nameof(AllTypesModel.Int32ArrayEmpty)];
            model1.UInt32ArrayEmpty = (uint[])jsonObject[nameof(AllTypesModel.UInt32ArrayEmpty)];
            model1.Int64ArrayEmpty = (long[])jsonObject[nameof(AllTypesModel.Int64ArrayEmpty)];
            model1.UInt64ArrayEmpty = (ulong[])jsonObject[nameof(AllTypesModel.UInt64ArrayEmpty)];
            model1.SingleArrayEmpty = (float[])jsonObject[nameof(AllTypesModel.SingleArrayEmpty)];
            model1.DoubleArrayEmpty = (double[])jsonObject[nameof(AllTypesModel.DoubleArrayEmpty)];
            model1.DecimalArrayEmpty = (decimal[])jsonObject[nameof(AllTypesModel.DecimalArrayEmpty)];
            model1.CharArrayEmpty = (char[])jsonObject[nameof(AllTypesModel.CharArrayEmpty)];
            model1.DateTimeArrayEmpty = (DateTime[])jsonObject[nameof(AllTypesModel.DateTimeArrayEmpty)];
            model1.DateTimeOffsetArrayEmpty = (DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayEmpty)];
            model1.TimeSpanArrayEmpty = (TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanArrayEmpty)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayEmpty = (DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyArrayEmpty)];
            model1.TimeOnlyArrayEmpty = (TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyArrayEmpty)];
#endif
            model1.GuidArrayEmpty = (Guid[])jsonObject[nameof(AllTypesModel.GuidArrayEmpty)];

            model1.BooleanArrayNull = (bool[])jsonObject[nameof(AllTypesModel.BooleanArrayNull)];
            model1.ByteArrayNull = (byte[])jsonObject[nameof(AllTypesModel.ByteArrayNull)];
            model1.SByteArrayNull = (sbyte[])jsonObject[nameof(AllTypesModel.SByteArrayNull)];
            model1.Int16ArrayNull = (short[])jsonObject[nameof(AllTypesModel.Int16ArrayNull)];
            model1.UInt16ArrayNull = (ushort[])jsonObject[nameof(AllTypesModel.UInt16ArrayNull)];
            model1.Int32ArrayNull = (int[])jsonObject[nameof(AllTypesModel.Int32ArrayNull)];
            model1.UInt32ArrayNull = (uint[])jsonObject[nameof(AllTypesModel.UInt32ArrayNull)];
            model1.Int64ArrayNull = (long[])jsonObject[nameof(AllTypesModel.Int64ArrayNull)];
            model1.UInt64ArrayNull = (ulong[])jsonObject[nameof(AllTypesModel.UInt64ArrayNull)];
            model1.SingleArrayNull = (float[])jsonObject[nameof(AllTypesModel.SingleArrayNull)];
            model1.DoubleArrayNull = (double[])jsonObject[nameof(AllTypesModel.DoubleArrayNull)];
            model1.DecimalArrayNull = (decimal[])jsonObject[nameof(AllTypesModel.DecimalArrayNull)];
            model1.CharArrayNull = (char[])jsonObject[nameof(AllTypesModel.CharArrayNull)];
            model1.DateTimeArrayNull = (DateTime[])jsonObject[nameof(AllTypesModel.DateTimeArrayNull)];
            model1.DateTimeOffsetArrayNull = (DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayNull)];
            model1.TimeSpanArrayNull = (TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanArrayNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNull = (DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyArrayNull)];
            model1.TimeOnlyArrayNull = (TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyArrayNull)];
#endif
            model1.GuidArrayNull = (Guid[])jsonObject[nameof(AllTypesModel.GuidArrayNull)];

            model1.BooleanArrayNullable = (bool?[])jsonObject[nameof(AllTypesModel.BooleanArrayNullable)];
            model1.ByteArrayNullable = (byte?[])jsonObject[nameof(AllTypesModel.ByteArrayNullable)];
            model1.SByteArrayNullable = (sbyte?[])jsonObject[nameof(AllTypesModel.SByteArrayNullable)];
            model1.Int16ArrayNullable = (short?[])jsonObject[nameof(AllTypesModel.Int16ArrayNullable)];
            model1.UInt16ArrayNullable = (ushort?[])jsonObject[nameof(AllTypesModel.UInt16ArrayNullable)];
            model1.Int32ArrayNullable = (int?[])jsonObject[nameof(AllTypesModel.Int32ArrayNullable)];
            model1.UInt32ArrayNullable = (uint?[])jsonObject[nameof(AllTypesModel.UInt32ArrayNullable)];
            model1.Int64ArrayNullable = (long?[])jsonObject[nameof(AllTypesModel.Int64ArrayNullable)];
            model1.UInt64ArrayNullable = (ulong?[])jsonObject[nameof(AllTypesModel.UInt64ArrayNullable)];
            model1.SingleArrayNullable = (float?[])jsonObject[nameof(AllTypesModel.SingleArrayNullable)];
            model1.DoubleArrayNullable = (double?[])jsonObject[nameof(AllTypesModel.DoubleArrayNullable)];
            model1.DecimalArrayNullable = (decimal?[])jsonObject[nameof(AllTypesModel.DecimalArrayNullable)];
            model1.CharArrayNullable = (char?[])jsonObject[nameof(AllTypesModel.CharArrayNullable)];
            model1.DateTimeArrayNullable = (DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeArrayNullable)];
            model1.DateTimeOffsetArrayNullable = (DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayNullable)];
            model1.TimeSpanArrayNullable = (TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanArrayNullable)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullable = (DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyArrayNullable)];
            model1.TimeOnlyArrayNullable = (TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyArrayNullable)];
#endif
            model1.GuidArrayNullable = (Guid?[])jsonObject[nameof(AllTypesModel.GuidArrayNullable)];

            model1.BooleanArrayNullableEmpty = (bool?[])jsonObject[nameof(AllTypesModel.BooleanArrayNullableEmpty)];
            model1.ByteArrayNullableEmpty = (byte?[])jsonObject[nameof(AllTypesModel.ByteArrayNullableEmpty)];
            model1.SByteArrayNullableEmpty = (sbyte?[])jsonObject[nameof(AllTypesModel.SByteArrayNullableEmpty)];
            model1.Int16ArrayNullableEmpty = (short?[])jsonObject[nameof(AllTypesModel.Int16ArrayNullableEmpty)];
            model1.UInt16ArrayNullableEmpty = (ushort?[])jsonObject[nameof(AllTypesModel.UInt16ArrayNullableEmpty)];
            model1.Int32ArrayNullableEmpty = (int?[])jsonObject[nameof(AllTypesModel.Int32ArrayNullableEmpty)];
            model1.UInt32ArrayNullableEmpty = (uint?[])jsonObject[nameof(AllTypesModel.UInt32ArrayNullableEmpty)];
            model1.Int64ArrayNullableEmpty = (long?[])jsonObject[nameof(AllTypesModel.Int64ArrayNullableEmpty)];
            model1.UInt64ArrayNullableEmpty = (ulong?[])jsonObject[nameof(AllTypesModel.UInt64ArrayNullableEmpty)];
            model1.SingleArrayNullableEmpty = (float?[])jsonObject[nameof(AllTypesModel.SingleArrayNullableEmpty)];
            model1.DoubleArrayNullableEmpty = (double?[])jsonObject[nameof(AllTypesModel.DoubleArrayNullableEmpty)];
            model1.DecimalArrayNullableEmpty = (decimal?[])jsonObject[nameof(AllTypesModel.DecimalArrayNullableEmpty)];
            model1.CharArrayNullableEmpty = (char?[])jsonObject[nameof(AllTypesModel.CharArrayNullableEmpty)];
            model1.DateTimeArrayNullableEmpty = (DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeArrayNullableEmpty)];
            model1.DateTimeOffsetArrayNullableEmpty = (DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayNullableEmpty)];
            model1.TimeSpanArrayNullableEmpty = (TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanArrayNullableEmpty)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullableEmpty = (DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyArrayNullableEmpty)];
            model1.TimeOnlyArrayNullableEmpty = (TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyArrayNullableEmpty)];
#endif
            model1.GuidArrayNullableEmpty = (Guid?[])jsonObject[nameof(AllTypesModel.GuidArrayNullableEmpty)];

            model1.BooleanArrayNullableNull = (bool?[])jsonObject[nameof(AllTypesModel.BooleanArrayNullableNull)];
            model1.ByteArrayNullableNull = (byte?[])jsonObject[nameof(AllTypesModel.ByteArrayNullableNull)];
            model1.SByteArrayNullableNull = (sbyte?[])jsonObject[nameof(AllTypesModel.SByteArrayNullableNull)];
            model1.Int16ArrayNullableNull = (short?[])jsonObject[nameof(AllTypesModel.Int16ArrayNullableNull)];
            model1.UInt16ArrayNullableNull = (ushort?[])jsonObject[nameof(AllTypesModel.UInt16ArrayNullableNull)];
            model1.Int32ArrayNullableNull = (int?[])jsonObject[nameof(AllTypesModel.Int32ArrayNullableNull)];
            model1.UInt32ArrayNullableNull = (uint?[])jsonObject[nameof(AllTypesModel.UInt32ArrayNullableNull)];
            model1.Int64ArrayNullableNull = (long?[])jsonObject[nameof(AllTypesModel.Int64ArrayNullableNull)];
            model1.UInt64ArrayNullableNull = (ulong?[])jsonObject[nameof(AllTypesModel.UInt64ArrayNullableNull)];
            model1.SingleArrayNullableNull = (float?[])jsonObject[nameof(AllTypesModel.SingleArrayNullableNull)];
            model1.DoubleArrayNullableNull = (double?[])jsonObject[nameof(AllTypesModel.DoubleArrayNullableNull)];
            model1.DecimalArrayNullableNull = (decimal?[])jsonObject[nameof(AllTypesModel.DecimalArrayNullableNull)];
            model1.CharArrayNullableNull = (char?[])jsonObject[nameof(AllTypesModel.CharArrayNullableNull)];
            model1.DateTimeArrayNullableNull = (DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeArrayNullableNull)];
            model1.DateTimeOffsetArrayNullableNull = (DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayNullableNull)];
            model1.TimeSpanArrayNullableNull = (TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanArrayNullableNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullableNull = (DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyArrayNullableNull)];
            model1.TimeOnlyArrayNullableNull = (TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyArrayNullableNull)];
#endif
            model1.GuidArrayNullableNull = (Guid?[])jsonObject[nameof(AllTypesModel.GuidArrayNullableNull)];

            model1.StringArray = (string[])jsonObject[nameof(AllTypesModel.StringArray)];
            model1.StringArrayEmpty = (string[])jsonObject[nameof(AllTypesModel.StringArrayEmpty)];

            model1.EnumArray = ((string[])jsonObject[nameof(AllTypesModel.EnumArray)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();
            model1.EnumArrayNullable = ((string[])jsonObject[nameof(AllTypesModel.EnumArrayNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();

            model1.BooleanListT = ((bool[])jsonObject[nameof(AllTypesModel.BooleanListT)]).ToList();
            model1.ByteListT = ((byte[])jsonObject[nameof(AllTypesModel.ByteListT)]).ToList();
            model1.SByteListT = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteListT)]).ToList();
            model1.Int16ListT = ((short[])jsonObject[nameof(AllTypesModel.Int16ListT)]).ToList();
            model1.UInt16ListT = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ListT)]).ToList();
            model1.Int32ListT = ((int[])jsonObject[nameof(AllTypesModel.Int32ListT)]).ToList();
            model1.UInt32ListT = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ListT)]).ToList();
            model1.Int64ListT = ((long[])jsonObject[nameof(AllTypesModel.Int64ListT)]).ToList();
            model1.UInt64ListT = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ListT)]).ToList();
            model1.SingleListT = ((float[])jsonObject[nameof(AllTypesModel.SingleListT)]).ToList();
            model1.DoubleListT = ((double[])jsonObject[nameof(AllTypesModel.DoubleListT)]).ToList();
            model1.DecimalListT = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalListT)]).ToList();
            model1.CharListT = ((char[])jsonObject[nameof(AllTypesModel.CharListT)]).ToList();
            model1.DateTimeListT = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeListT)]).ToList();
            model1.DateTimeOffsetListT = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListT)]).ToList();
            model1.TimeSpanListT = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListT = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyListT)]).ToList();
            model1.TimeOnlyListT = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyListT)]).ToList();
#endif
            model1.GuidListT = ((Guid[])jsonObject[nameof(AllTypesModel.GuidListT)]).ToList();

            model1.BooleanListTEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanListTEmpty)]).ToList();
            model1.ByteListTEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteListTEmpty)]).ToList();
            model1.SByteListTEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteListTEmpty)]).ToList();
            model1.Int16ListTEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16ListTEmpty)]).ToList();
            model1.UInt16ListTEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ListTEmpty)]).ToList();
            model1.Int32ListTEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32ListTEmpty)]).ToList();
            model1.UInt32ListTEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ListTEmpty)]).ToList();
            model1.Int64ListTEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64ListTEmpty)]).ToList();
            model1.UInt64ListTEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ListTEmpty)]).ToList();
            model1.SingleListTEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleListTEmpty)]).ToList();
            model1.DoubleListTEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleListTEmpty)]).ToList();
            model1.DecimalListTEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalListTEmpty)]).ToList();
            model1.CharListTEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharListTEmpty)]).ToList();
            model1.DateTimeListTEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeListTEmpty)]).ToList();
            model1.DateTimeOffsetListTEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListTEmpty)]).ToList();
            model1.TimeSpanListTEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyListTEmpty)]).ToList();
            model1.TimeOnlyListTEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyListTEmpty)]).ToList();
#endif
            model1.GuidListTEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidListTEmpty)]).ToList();

            model1.BooleanListTNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanListTNull)])?.ToList();
            model1.ByteListTNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteListTNull)])?.ToList();
            model1.SByteListTNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteListTNull)])?.ToList();
            model1.Int16ListTNull = ((short[])jsonObject[nameof(AllTypesModel.Int16ListTNull)])?.ToList();
            model1.UInt16ListTNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ListTNull)])?.ToList();
            model1.Int32ListTNull = ((int[])jsonObject[nameof(AllTypesModel.Int32ListTNull)])?.ToList();
            model1.UInt32ListTNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ListTNull)])?.ToList();
            model1.Int64ListTNull = ((long[])jsonObject[nameof(AllTypesModel.Int64ListTNull)])?.ToList();
            model1.UInt64ListTNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ListTNull)])?.ToList();
            model1.SingleListTNull = ((float[])jsonObject[nameof(AllTypesModel.SingleListTNull)])?.ToList();
            model1.DoubleListTNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleListTNull)])?.ToList();
            model1.DecimalListTNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalListTNull)])?.ToList();
            model1.CharListTNull = ((char[])jsonObject[nameof(AllTypesModel.CharListTNull)])?.ToList();
            model1.DateTimeListTNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeListTNull)])?.ToList();
            model1.DateTimeOffsetListTNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListTNull)])?.ToList();
            model1.TimeSpanListTNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyListTNull)])?.ToList();
            model1.TimeOnlyListTNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyListTNull)])?.ToList();
#endif
            model1.GuidListTNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidListTNull)])?.ToList();

            model1.BooleanListTNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListTNullable)]).ToList();
            model1.ByteListTNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListTNullable)]).ToList();
            model1.SByteListTNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListTNullable)]).ToList();
            model1.Int16ListTNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListTNullable)]).ToList();
            model1.UInt16ListTNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListTNullable)]).ToList();
            model1.Int32ListTNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListTNullable)]).ToList();
            model1.UInt32ListTNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListTNullable)]).ToList();
            model1.Int64ListTNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListTNullable)]).ToList();
            model1.UInt64ListTNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListTNullable)]).ToList();
            model1.SingleListTNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleListTNullable)]).ToList();
            model1.DoubleListTNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListTNullable)]).ToList();
            model1.DecimalListTNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListTNullable)]).ToList();
            model1.CharListTNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharListTNullable)]).ToList();
            model1.DateTimeListTNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListTNullable)]).ToList();
            model1.DateTimeOffsetListTNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListTNullable)]).ToList();
            model1.TimeSpanListTNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListTNullable)]).ToList();
            model1.TimeOnlyListTNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListTNullable)]).ToList();
#endif
            model1.GuidListTNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListTNullable)]).ToList();

            model1.BooleanListTNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListTNullableEmpty)]).ToList();
            model1.ByteListTNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListTNullableEmpty)]).ToList();
            model1.SByteListTNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListTNullableEmpty)]).ToList();
            model1.Int16ListTNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListTNullableEmpty)]).ToList();
            model1.UInt16ListTNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListTNullableEmpty)]).ToList();
            model1.Int32ListTNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListTNullableEmpty)]).ToList();
            model1.UInt32ListTNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListTNullableEmpty)]).ToList();
            model1.Int64ListTNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListTNullableEmpty)]).ToList();
            model1.UInt64ListTNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListTNullableEmpty)]).ToList();
            model1.SingleListTNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleListTNullableEmpty)]).ToList();
            model1.DoubleListTNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListTNullableEmpty)]).ToList();
            model1.DecimalListTNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListTNullableEmpty)]).ToList();
            model1.CharListTNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharListTNullableEmpty)]).ToList();
            model1.DateTimeListTNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListTNullableEmpty)]).ToList();
            model1.TimeSpanListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListTNullableEmpty)]).ToList();
            model1.TimeOnlyListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListTNullableEmpty)]).ToList();
#endif
            model1.GuidListTNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListTNullableEmpty)]).ToList();

            model1.BooleanListTNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListTNullableNull)])?.ToList();
            model1.ByteListTNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListTNullableNull)])?.ToList();
            model1.SByteListTNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListTNullableNull)])?.ToList();
            model1.Int16ListTNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListTNullableNull)])?.ToList();
            model1.UInt16ListTNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListTNullableNull)])?.ToList();
            model1.Int32ListTNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListTNullableNull)])?.ToList();
            model1.UInt32ListTNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListTNullableNull)])?.ToList();
            model1.Int64ListTNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListTNullableNull)])?.ToList();
            model1.UInt64ListTNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListTNullableNull)])?.ToList();
            model1.SingleListTNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleListTNullableNull)])?.ToList();
            model1.DoubleListTNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListTNullableNull)])?.ToList();
            model1.DecimalListTNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListTNullableNull)])?.ToList();
            model1.CharListTNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharListTNullableNull)])?.ToList();
            model1.DateTimeListTNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListTNullableNull)])?.ToList();
            model1.DateTimeOffsetListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListTNullableNull)])?.ToList();
            model1.TimeSpanListTNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListTNullableNull)])?.ToList();
            model1.TimeOnlyListTNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListTNullableNull)])?.ToList();
#endif
            model1.GuidListTNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListTNullableNull)])?.ToList();

            model1.StringListT = ((string[])jsonObject[nameof(AllTypesModel.StringListT)]).ToList();
            model1.StringListTEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringListTEmpty)]).ToList();
            model1.StringListTNull = ((string[])jsonObject[nameof(AllTypesModel.StringListTNull)])?.ToList();

            model1.EnumListT = (((string[])jsonObject[nameof(AllTypesModel.EnumListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListTEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListTNull = (((string[])jsonObject[nameof(AllTypesModel.EnumListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumListTNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListTNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListTNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIListT = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIListT)]).ToList();
            model1.ByteIListT = ((byte[])jsonObject[nameof(AllTypesModel.ByteIListT)]).ToList();
            model1.SByteIListT = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIListT)]).ToList();
            model1.Int16IListT = ((short[])jsonObject[nameof(AllTypesModel.Int16IListT)]).ToList();
            model1.UInt16IListT = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IListT)]).ToList();
            model1.Int32IListT = ((int[])jsonObject[nameof(AllTypesModel.Int32IListT)]).ToList();
            model1.UInt32IListT = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IListT)]).ToList();
            model1.Int64IListT = ((long[])jsonObject[nameof(AllTypesModel.Int64IListT)]).ToList();
            model1.UInt64IListT = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IListT)]).ToList();
            model1.SingleIListT = ((float[])jsonObject[nameof(AllTypesModel.SingleIListT)]).ToList();
            model1.DoubleIListT = ((double[])jsonObject[nameof(AllTypesModel.DoubleIListT)]).ToList();
            model1.DecimalIListT = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIListT)]).ToList();
            model1.CharIListT = ((char[])jsonObject[nameof(AllTypesModel.CharIListT)]).ToList();
            model1.DateTimeIListT = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIListT)]).ToList();
            model1.DateTimeOffsetIListT = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListT)]).ToList();
            model1.TimeSpanIListT = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListT = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIListT)]).ToList();
            model1.TimeOnlyIListT = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIListT)]).ToList();
#endif
            model1.GuidIListT = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIListT)]).ToList();

            model1.BooleanIListTEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIListTEmpty)]).ToList();
            model1.ByteIListTEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteIListTEmpty)]).ToList();
            model1.SByteIListTEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIListTEmpty)]).ToList();
            model1.Int16IListTEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16IListTEmpty)]).ToList();
            model1.UInt16IListTEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IListTEmpty)]).ToList();
            model1.Int32IListTEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32IListTEmpty)]).ToList();
            model1.UInt32IListTEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IListTEmpty)]).ToList();
            model1.Int64IListTEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64IListTEmpty)]).ToList();
            model1.UInt64IListTEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IListTEmpty)]).ToList();
            model1.SingleIListTEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleIListTEmpty)]).ToList();
            model1.DoubleIListTEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleIListTEmpty)]).ToList();
            model1.DecimalIListTEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIListTEmpty)]).ToList();
            model1.CharIListTEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharIListTEmpty)]).ToList();
            model1.DateTimeIListTEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIListTEmpty)]).ToList();
            model1.DateTimeOffsetIListTEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListTEmpty)]).ToList();
            model1.TimeSpanIListTEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIListTEmpty)]).ToList();
            model1.TimeOnlyIListTEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIListTEmpty)]).ToList();
#endif
            model1.GuidIListTEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIListTEmpty)]).ToList();

            model1.BooleanIListTNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIListTNull)])?.ToList();
            model1.ByteIListTNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteIListTNull)])?.ToList();
            model1.SByteIListTNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIListTNull)])?.ToList();
            model1.Int16IListTNull = ((short[])jsonObject[nameof(AllTypesModel.Int16IListTNull)])?.ToList();
            model1.UInt16IListTNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IListTNull)])?.ToList();
            model1.Int32IListTNull = ((int[])jsonObject[nameof(AllTypesModel.Int32IListTNull)])?.ToList();
            model1.UInt32IListTNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IListTNull)])?.ToList();
            model1.Int64IListTNull = ((long[])jsonObject[nameof(AllTypesModel.Int64IListTNull)])?.ToList();
            model1.UInt64IListTNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IListTNull)])?.ToList();
            model1.SingleIListTNull = ((float[])jsonObject[nameof(AllTypesModel.SingleIListTNull)])?.ToList();
            model1.DoubleIListTNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleIListTNull)])?.ToList();
            model1.DecimalIListTNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIListTNull)])?.ToList();
            model1.CharIListTNull = ((char[])jsonObject[nameof(AllTypesModel.CharIListTNull)])?.ToList();
            model1.DateTimeIListTNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIListTNull)])?.ToList();
            model1.DateTimeOffsetIListTNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListTNull)])?.ToList();
            model1.TimeSpanIListTNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIListTNull)])?.ToList();
            model1.TimeOnlyIListTNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIListTNull)])?.ToList();
#endif
            model1.GuidIListTNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIListTNull)])?.ToList();

            model1.BooleanIListTNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIListTNullable)]).ToList();
            model1.ByteIListTNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIListTNullable)]).ToList();
            model1.SByteIListTNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIListTNullable)]).ToList();
            model1.Int16IListTNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16IListTNullable)]).ToList();
            model1.UInt16IListTNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IListTNullable)]).ToList();
            model1.Int32IListTNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32IListTNullable)]).ToList();
            model1.UInt32IListTNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IListTNullable)]).ToList();
            model1.Int64IListTNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64IListTNullable)]).ToList();
            model1.UInt64IListTNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IListTNullable)]).ToList();
            model1.SingleIListTNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleIListTNullable)]).ToList();
            model1.DoubleIListTNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIListTNullable)]).ToList();
            model1.DecimalIListTNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIListTNullable)]).ToList();
            model1.CharIListTNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharIListTNullable)]).ToList();
            model1.DateTimeIListTNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIListTNullable)]).ToList();
            model1.DateTimeOffsetIListTNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListTNullable)]).ToList();
            model1.TimeSpanIListTNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIListTNullable)]).ToList();
            model1.TimeOnlyIListTNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIListTNullable)]).ToList();
#endif
            model1.GuidIListTNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIListTNullable)]).ToList();

            model1.BooleanIListTNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIListTNullableEmpty)]).ToList();
            model1.ByteIListTNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIListTNullableEmpty)]).ToList();
            model1.SByteIListTNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIListTNullableEmpty)]).ToList();
            model1.Int16IListTNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16IListTNullableEmpty)]).ToList();
            model1.UInt16IListTNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IListTNullableEmpty)]).ToList();
            model1.Int32IListTNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32IListTNullableEmpty)]).ToList();
            model1.UInt32IListTNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IListTNullableEmpty)]).ToList();
            model1.Int64IListTNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64IListTNullableEmpty)]).ToList();
            model1.UInt64IListTNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IListTNullableEmpty)]).ToList();
            model1.SingleIListTNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleIListTNullableEmpty)]).ToList();
            model1.DoubleIListTNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIListTNullableEmpty)]).ToList();
            model1.DecimalIListTNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIListTNullableEmpty)]).ToList();
            model1.CharIListTNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharIListTNullableEmpty)]).ToList();
            model1.DateTimeIListTNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListTNullableEmpty)]).ToList();
            model1.TimeSpanIListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIListTNullableEmpty)]).ToList();
            model1.TimeOnlyIListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIListTNullableEmpty)]).ToList();
#endif
            model1.GuidIListTNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIListTNullableEmpty)]).ToList();

            model1.BooleanIListTNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIListTNullableNull)])?.ToList();
            model1.ByteIListTNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIListTNullableNull)])?.ToList();
            model1.SByteIListTNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIListTNullableNull)])?.ToList();
            model1.Int16IListTNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16IListTNullableNull)])?.ToList();
            model1.UInt16IListTNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IListTNullableNull)])?.ToList();
            model1.Int32IListTNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32IListTNullableNull)])?.ToList();
            model1.UInt32IListTNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IListTNullableNull)])?.ToList();
            model1.Int64IListTNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64IListTNullableNull)])?.ToList();
            model1.UInt64IListTNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IListTNullableNull)])?.ToList();
            model1.SingleIListTNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleIListTNullableNull)])?.ToList();
            model1.DoubleIListTNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIListTNullableNull)])?.ToList();
            model1.DecimalIListTNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIListTNullableNull)])?.ToList();
            model1.CharIListTNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharIListTNullableNull)])?.ToList();
            model1.DateTimeIListTNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIListTNullableNull)])?.ToList();
            model1.DateTimeOffsetIListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIListTNullableNull)])?.ToList();
            model1.TimeSpanIListTNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIListTNullableNull)])?.ToList();
            model1.TimeOnlyIListTNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIListTNullableNull)])?.ToList();
#endif
            model1.GuidIListTNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIListTNullableNull)])?.ToList();

            model1.StringIListT = ((string[])jsonObject[nameof(AllTypesModel.StringIListT)]).ToList();
            model1.StringIListTEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringIListTEmpty)]).ToList();
            model1.StringIListTNull = ((string[])jsonObject[nameof(AllTypesModel.StringIListTNull)])?.ToList();

            model1.EnumIListT = (((string[])jsonObject[nameof(AllTypesModel.EnumIListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIListTEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIListTNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIListTNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumIListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIListTNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIListTNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIReadOnlyListT = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListT)]).ToList();
            model1.ByteIReadOnlyListT = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListT)]).ToList();
            model1.SByteIReadOnlyListT = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListT)]).ToList();
            model1.Int16IReadOnlyListT = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListT)]).ToList();
            model1.UInt16IReadOnlyListT = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListT)]).ToList();
            model1.Int32IReadOnlyListT = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListT)]).ToList();
            model1.UInt32IReadOnlyListT = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListT)]).ToList();
            model1.Int64IReadOnlyListT = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListT)]).ToList();
            model1.UInt64IReadOnlyListT = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListT)]).ToList();
            model1.SingleIReadOnlyListT = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListT)]).ToList();
            model1.DoubleIReadOnlyListT = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListT)]).ToList();
            model1.DecimalIReadOnlyListT = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListT)]).ToList();
            model1.CharIReadOnlyListT = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListT)]).ToList();
            model1.DateTimeIReadOnlyListT = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListT)]).ToList();
            model1.DateTimeOffsetIReadOnlyListT = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListT)]).ToList();
            model1.TimeSpanIReadOnlyListT = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListT = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListT)]).ToList();
            model1.TimeOnlyIReadOnlyListT = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListT)]).ToList();
#endif
            model1.GuidIReadOnlyListT = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListT)]).ToList();

            model1.BooleanIReadOnlyListTEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListTEmpty)]).ToList();
            model1.ByteIReadOnlyListTEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListTEmpty)]).ToList();
            model1.SByteIReadOnlyListTEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListTEmpty)]).ToList();
            model1.Int16IReadOnlyListTEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListTEmpty)]).ToList();
            model1.UInt16IReadOnlyListTEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListTEmpty)]).ToList();
            model1.Int32IReadOnlyListTEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListTEmpty)]).ToList();
            model1.UInt32IReadOnlyListTEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListTEmpty)]).ToList();
            model1.Int64IReadOnlyListTEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListTEmpty)]).ToList();
            model1.UInt64IReadOnlyListTEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListTEmpty)]).ToList();
            model1.SingleIReadOnlyListTEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListTEmpty)]).ToList();
            model1.DoubleIReadOnlyListTEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListTEmpty)]).ToList();
            model1.DecimalIReadOnlyListTEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListTEmpty)]).ToList();
            model1.CharIReadOnlyListTEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListTEmpty)]).ToList();
            model1.DateTimeIReadOnlyListTEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListTEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListTEmpty)]).ToList();
            model1.TimeSpanIReadOnlyListTEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListTEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyListTEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListTEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyListTEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListTEmpty)]).ToList();

            model1.BooleanIReadOnlyListTNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListTNull)])?.ToList();
            model1.ByteIReadOnlyListTNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListTNull)])?.ToList();
            model1.SByteIReadOnlyListTNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListTNull)])?.ToList();
            model1.Int16IReadOnlyListTNull = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListTNull)])?.ToList();
            model1.UInt16IReadOnlyListTNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListTNull)])?.ToList();
            model1.Int32IReadOnlyListTNull = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListTNull)])?.ToList();
            model1.UInt32IReadOnlyListTNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListTNull)])?.ToList();
            model1.Int64IReadOnlyListTNull = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListTNull)])?.ToList();
            model1.UInt64IReadOnlyListTNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListTNull)])?.ToList();
            model1.SingleIReadOnlyListTNull = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListTNull)])?.ToList();
            model1.DoubleIReadOnlyListTNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListTNull)])?.ToList();
            model1.DecimalIReadOnlyListTNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListTNull)])?.ToList();
            model1.CharIReadOnlyListTNull = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListTNull)])?.ToList();
            model1.DateTimeIReadOnlyListTNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListTNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyListTNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListTNull)])?.ToList();
            model1.TimeSpanIReadOnlyListTNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListTNull)])?.ToList();
            model1.TimeOnlyIReadOnlyListTNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListTNull)])?.ToList();
#endif
            model1.GuidIReadOnlyListTNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListTNull)])?.ToList();

            model1.BooleanIReadOnlyListTNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListTNullable)]).ToList();
            model1.ByteIReadOnlyListTNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListTNullable)]).ToList();
            model1.SByteIReadOnlyListTNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListTNullable)]).ToList();
            model1.Int16IReadOnlyListTNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListTNullable)]).ToList();
            model1.UInt16IReadOnlyListTNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListTNullable)]).ToList();
            model1.Int32IReadOnlyListTNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListTNullable)]).ToList();
            model1.UInt32IReadOnlyListTNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListTNullable)]).ToList();
            model1.Int64IReadOnlyListTNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListTNullable)]).ToList();
            model1.UInt64IReadOnlyListTNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListTNullable)]).ToList();
            model1.SingleIReadOnlyListTNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListTNullable)]).ToList();
            model1.DoubleIReadOnlyListTNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListTNullable)]).ToList();
            model1.DecimalIReadOnlyListTNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListTNullable)]).ToList();
            model1.CharIReadOnlyListTNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListTNullable)]).ToList();
            model1.DateTimeIReadOnlyListTNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListTNullable)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListTNullable)]).ToList();
            model1.TimeSpanIReadOnlyListTNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListTNullable)]).ToList();
            model1.TimeOnlyIReadOnlyListTNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListTNullable)]).ToList();
#endif
            model1.GuidIReadOnlyListTNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListTNullable)]).ToList();

            model1.BooleanIReadOnlyListTNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListTNullableEmpty)]).ToList();
            model1.ByteIReadOnlyListTNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListTNullableEmpty)]).ToList();
            model1.SByteIReadOnlyListTNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListTNullableEmpty)]).ToList();
            model1.Int16IReadOnlyListTNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt16IReadOnlyListTNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListTNullableEmpty)]).ToList();
            model1.Int32IReadOnlyListTNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt32IReadOnlyListTNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListTNullableEmpty)]).ToList();
            model1.Int64IReadOnlyListTNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt64IReadOnlyListTNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListTNullableEmpty)]).ToList();
            model1.SingleIReadOnlyListTNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListTNullableEmpty)]).ToList();
            model1.DoubleIReadOnlyListTNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListTNullableEmpty)]).ToList();
            model1.DecimalIReadOnlyListTNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListTNullableEmpty)]).ToList();
            model1.CharIReadOnlyListTNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListTNullableEmpty)]).ToList();
            model1.DateTimeIReadOnlyListTNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListTNullableEmpty)]).ToList();
            model1.TimeSpanIReadOnlyListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListTNullableEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListTNullableEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyListTNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListTNullableEmpty)]).ToList();

            model1.BooleanIReadOnlyListTNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyListTNullableNull)])?.ToList();
            model1.ByteIReadOnlyListTNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyListTNullableNull)])?.ToList();
            model1.SByteIReadOnlyListTNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyListTNullableNull)])?.ToList();
            model1.Int16IReadOnlyListTNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt16IReadOnlyListTNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyListTNullableNull)])?.ToList();
            model1.Int32IReadOnlyListTNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt32IReadOnlyListTNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyListTNullableNull)])?.ToList();
            model1.Int64IReadOnlyListTNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt64IReadOnlyListTNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyListTNullableNull)])?.ToList();
            model1.SingleIReadOnlyListTNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyListTNullableNull)])?.ToList();
            model1.DoubleIReadOnlyListTNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyListTNullableNull)])?.ToList();
            model1.DecimalIReadOnlyListTNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyListTNullableNull)])?.ToList();
            model1.CharIReadOnlyListTNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyListTNullableNull)])?.ToList();
            model1.DateTimeIReadOnlyListTNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyListTNullableNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyListTNullableNull)])?.ToList();
            model1.TimeSpanIReadOnlyListTNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyListTNullableNull)])?.ToList();
            model1.TimeOnlyIReadOnlyListTNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyListTNullableNull)])?.ToList();
#endif
            model1.GuidIReadOnlyListTNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyListTNullableNull)])?.ToList();

            model1.StringIReadOnlyListT = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyListT)]).ToList();
            model1.StringIReadOnlyListTEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyListTEmpty)]).ToList();
            model1.StringIReadOnlyListTNull = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyListTNull)])?.ToList();

            model1.EnumIReadOnlyListT = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyListTEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyListTNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIReadOnlyListTNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyListTNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyListTNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanICollectionT = ((bool[])jsonObject[nameof(AllTypesModel.BooleanICollectionT)]).ToList();
            model1.ByteICollectionT = ((byte[])jsonObject[nameof(AllTypesModel.ByteICollectionT)]).ToList();
            model1.SByteICollectionT = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteICollectionT)]).ToList();
            model1.Int16ICollectionT = ((short[])jsonObject[nameof(AllTypesModel.Int16ICollectionT)]).ToList();
            model1.UInt16ICollectionT = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ICollectionT)]).ToList();
            model1.Int32ICollectionT = ((int[])jsonObject[nameof(AllTypesModel.Int32ICollectionT)]).ToList();
            model1.UInt32ICollectionT = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ICollectionT)]).ToList();
            model1.Int64ICollectionT = ((long[])jsonObject[nameof(AllTypesModel.Int64ICollectionT)]).ToList();
            model1.UInt64ICollectionT = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ICollectionT)]).ToList();
            model1.SingleICollectionT = ((float[])jsonObject[nameof(AllTypesModel.SingleICollectionT)]).ToList();
            model1.DoubleICollectionT = ((double[])jsonObject[nameof(AllTypesModel.DoubleICollectionT)]).ToList();
            model1.DecimalICollectionT = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalICollectionT)]).ToList();
            model1.CharICollectionT = ((char[])jsonObject[nameof(AllTypesModel.CharICollectionT)]).ToList();
            model1.DateTimeICollectionT = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeICollectionT)]).ToList();
            model1.DateTimeOffsetICollectionT = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionT)]).ToList();
            model1.TimeSpanICollectionT = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionT = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionT)]).ToList();
            model1.TimeOnlyICollectionT = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionT)]).ToList();
#endif
            model1.GuidICollectionT = ((Guid[])jsonObject[nameof(AllTypesModel.GuidICollectionT)]).ToList();

            model1.BooleanICollectionTEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanICollectionTEmpty)]).ToList();
            model1.ByteICollectionTEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteICollectionTEmpty)]).ToList();
            model1.SByteICollectionTEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteICollectionTEmpty)]).ToList();
            model1.Int16ICollectionTEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16ICollectionTEmpty)]).ToList();
            model1.UInt16ICollectionTEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ICollectionTEmpty)]).ToList();
            model1.Int32ICollectionTEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32ICollectionTEmpty)]).ToList();
            model1.UInt32ICollectionTEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ICollectionTEmpty)]).ToList();
            model1.Int64ICollectionTEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64ICollectionTEmpty)]).ToList();
            model1.UInt64ICollectionTEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ICollectionTEmpty)]).ToList();
            model1.SingleICollectionTEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleICollectionTEmpty)]).ToList();
            model1.DoubleICollectionTEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleICollectionTEmpty)]).ToList();
            model1.DecimalICollectionTEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalICollectionTEmpty)]).ToList();
            model1.CharICollectionTEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharICollectionTEmpty)]).ToList();
            model1.DateTimeICollectionTEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeICollectionTEmpty)]).ToList();
            model1.DateTimeOffsetICollectionTEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionTEmpty)]).ToList();
            model1.TimeSpanICollectionTEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionTEmpty)]).ToList();
            model1.TimeOnlyICollectionTEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionTEmpty)]).ToList();
#endif
            model1.GuidICollectionTEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidICollectionTEmpty)]).ToList();

            model1.BooleanICollectionTNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanICollectionTNull)])?.ToList();
            model1.ByteICollectionTNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteICollectionTNull)])?.ToList();
            model1.SByteICollectionTNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteICollectionTNull)])?.ToList();
            model1.Int16ICollectionTNull = ((short[])jsonObject[nameof(AllTypesModel.Int16ICollectionTNull)])?.ToList();
            model1.UInt16ICollectionTNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ICollectionTNull)])?.ToList();
            model1.Int32ICollectionTNull = ((int[])jsonObject[nameof(AllTypesModel.Int32ICollectionTNull)])?.ToList();
            model1.UInt32ICollectionTNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ICollectionTNull)])?.ToList();
            model1.Int64ICollectionTNull = ((long[])jsonObject[nameof(AllTypesModel.Int64ICollectionTNull)])?.ToList();
            model1.UInt64ICollectionTNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ICollectionTNull)])?.ToList();
            model1.SingleICollectionTNull = ((float[])jsonObject[nameof(AllTypesModel.SingleICollectionTNull)])?.ToList();
            model1.DoubleICollectionTNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleICollectionTNull)])?.ToList();
            model1.DecimalICollectionTNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalICollectionTNull)])?.ToList();
            model1.CharICollectionTNull = ((char[])jsonObject[nameof(AllTypesModel.CharICollectionTNull)])?.ToList();
            model1.DateTimeICollectionTNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeICollectionTNull)])?.ToList();
            model1.DateTimeOffsetICollectionTNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionTNull)])?.ToList();
            model1.TimeSpanICollectionTNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionTNull)])?.ToList();
            model1.TimeOnlyICollectionTNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionTNull)])?.ToList();
#endif
            model1.GuidICollectionTNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidICollectionTNull)])?.ToList();

            model1.BooleanICollectionTNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanICollectionTNullable)]).ToList();
            model1.ByteICollectionTNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteICollectionTNullable)]).ToList();
            model1.SByteICollectionTNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteICollectionTNullable)]).ToList();
            model1.Int16ICollectionTNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16ICollectionTNullable)]).ToList();
            model1.UInt16ICollectionTNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ICollectionTNullable)]).ToList();
            model1.Int32ICollectionTNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32ICollectionTNullable)]).ToList();
            model1.UInt32ICollectionTNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ICollectionTNullable)]).ToList();
            model1.Int64ICollectionTNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64ICollectionTNullable)]).ToList();
            model1.UInt64ICollectionTNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ICollectionTNullable)]).ToList();
            model1.SingleICollectionTNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleICollectionTNullable)]).ToList();
            model1.DoubleICollectionTNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleICollectionTNullable)]).ToList();
            model1.DecimalICollectionTNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalICollectionTNullable)]).ToList();
            model1.CharICollectionTNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharICollectionTNullable)]).ToList();
            model1.DateTimeICollectionTNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeICollectionTNullable)]).ToList();
            model1.DateTimeOffsetICollectionTNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionTNullable)]).ToList();
            model1.TimeSpanICollectionTNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionTNullable)]).ToList();
            model1.TimeOnlyICollectionTNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionTNullable)]).ToList();
#endif
            model1.GuidICollectionTNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidICollectionTNullable)]).ToList();

            model1.BooleanICollectionTNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanICollectionTNullableEmpty)]).ToList();
            model1.ByteICollectionTNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteICollectionTNullableEmpty)]).ToList();
            model1.SByteICollectionTNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteICollectionTNullableEmpty)]).ToList();
            model1.Int16ICollectionTNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16ICollectionTNullableEmpty)]).ToList();
            model1.UInt16ICollectionTNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ICollectionTNullableEmpty)]).ToList();
            model1.Int32ICollectionTNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32ICollectionTNullableEmpty)]).ToList();
            model1.UInt32ICollectionTNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ICollectionTNullableEmpty)]).ToList();
            model1.Int64ICollectionTNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64ICollectionTNullableEmpty)]).ToList();
            model1.UInt64ICollectionTNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ICollectionTNullableEmpty)]).ToList();
            model1.SingleICollectionTNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleICollectionTNullableEmpty)]).ToList();
            model1.DoubleICollectionTNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleICollectionTNullableEmpty)]).ToList();
            model1.DecimalICollectionTNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalICollectionTNullableEmpty)]).ToList();
            model1.CharICollectionTNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharICollectionTNullableEmpty)]).ToList();
            model1.DateTimeICollectionTNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeICollectionTNullableEmpty)]).ToList();
            model1.DateTimeOffsetICollectionTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionTNullableEmpty)]).ToList();
            model1.TimeSpanICollectionTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionTNullableEmpty)]).ToList();
            model1.TimeOnlyICollectionTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionTNullableEmpty)]).ToList();
#endif
            model1.GuidICollectionTNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidICollectionTNullableEmpty)]).ToList();

            model1.BooleanICollectionTNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanICollectionTNullableNull)])?.ToList();
            model1.ByteICollectionTNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteICollectionTNullableNull)])?.ToList();
            model1.SByteICollectionTNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteICollectionTNullableNull)])?.ToList();
            model1.Int16ICollectionTNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16ICollectionTNullableNull)])?.ToList();
            model1.UInt16ICollectionTNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ICollectionTNullableNull)])?.ToList();
            model1.Int32ICollectionTNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32ICollectionTNullableNull)])?.ToList();
            model1.UInt32ICollectionTNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ICollectionTNullableNull)])?.ToList();
            model1.Int64ICollectionTNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64ICollectionTNullableNull)])?.ToList();
            model1.UInt64ICollectionTNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ICollectionTNullableNull)])?.ToList();
            model1.SingleICollectionTNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleICollectionTNullableNull)])?.ToList();
            model1.DoubleICollectionTNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleICollectionTNullableNull)])?.ToList();
            model1.DecimalICollectionTNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalICollectionTNullableNull)])?.ToList();
            model1.CharICollectionTNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharICollectionTNullableNull)])?.ToList();
            model1.DateTimeICollectionTNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeICollectionTNullableNull)])?.ToList();
            model1.DateTimeOffsetICollectionTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetICollectionTNullableNull)])?.ToList();
            model1.TimeSpanICollectionTNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanICollectionTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyICollectionTNullableNull)])?.ToList();
            model1.TimeOnlyICollectionTNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyICollectionTNullableNull)])?.ToList();
#endif
            model1.GuidICollectionTNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidICollectionTNullableNull)])?.ToList();

            model1.StringICollectionT = ((string[])jsonObject[nameof(AllTypesModel.StringICollectionT)]).ToList();
            model1.StringICollectionTEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringICollectionTEmpty)]).ToList();
            model1.StringICollectionTNull = ((string[])jsonObject[nameof(AllTypesModel.StringICollectionTNull)])?.ToList();

            model1.EnumICollectionT = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumICollectionTEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumICollectionTNull = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumICollectionTNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumICollectionTNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumICollectionTNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumICollectionTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIReadOnlyCollectionT = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionT)]).ToList();
            model1.ByteIReadOnlyCollectionT = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionT)]).ToList();
            model1.SByteIReadOnlyCollectionT = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionT)]).ToList();
            model1.Int16IReadOnlyCollectionT = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionT)]).ToList();
            model1.UInt16IReadOnlyCollectionT = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionT)]).ToList();
            model1.Int32IReadOnlyCollectionT = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionT)]).ToList();
            model1.UInt32IReadOnlyCollectionT = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionT)]).ToList();
            model1.Int64IReadOnlyCollectionT = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionT)]).ToList();
            model1.UInt64IReadOnlyCollectionT = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionT)]).ToList();
            model1.SingleIReadOnlyCollectionT = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionT)]).ToList();
            model1.DoubleIReadOnlyCollectionT = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionT)]).ToList();
            model1.DecimalIReadOnlyCollectionT = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionT)]).ToList();
            model1.CharIReadOnlyCollectionT = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionT)]).ToList();
            model1.DateTimeIReadOnlyCollectionT = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionT)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionT = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionT)]).ToList();
            model1.TimeSpanIReadOnlyCollectionT = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionT = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionT)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionT = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionT)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionT = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionT)]).ToList();

            model1.BooleanIReadOnlyCollectionTEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionTEmpty)]).ToList();
            model1.ByteIReadOnlyCollectionTEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionTEmpty)]).ToList();
            model1.SByteIReadOnlyCollectionTEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionTEmpty)]).ToList();
            model1.Int16IReadOnlyCollectionTEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt16IReadOnlyCollectionTEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionTEmpty)]).ToList();
            model1.Int32IReadOnlyCollectionTEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt32IReadOnlyCollectionTEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionTEmpty)]).ToList();
            model1.Int64IReadOnlyCollectionTEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt64IReadOnlyCollectionTEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionTEmpty)]).ToList();
            model1.SingleIReadOnlyCollectionTEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionTEmpty)]).ToList();
            model1.DoubleIReadOnlyCollectionTEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionTEmpty)]).ToList();
            model1.DecimalIReadOnlyCollectionTEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionTEmpty)]).ToList();
            model1.CharIReadOnlyCollectionTEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionTEmpty)]).ToList();
            model1.DateTimeIReadOnlyCollectionTEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionTEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionTEmpty)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionTEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionTEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionTEmpty)]).ToList();

            model1.BooleanIReadOnlyCollectionTNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionTNull)])?.ToList();
            model1.ByteIReadOnlyCollectionTNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionTNull)])?.ToList();
            model1.SByteIReadOnlyCollectionTNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionTNull)])?.ToList();
            model1.Int16IReadOnlyCollectionTNull = ((short[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt16IReadOnlyCollectionTNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionTNull)])?.ToList();
            model1.Int32IReadOnlyCollectionTNull = ((int[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt32IReadOnlyCollectionTNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionTNull)])?.ToList();
            model1.Int64IReadOnlyCollectionTNull = ((long[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt64IReadOnlyCollectionTNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionTNull)])?.ToList();
            model1.SingleIReadOnlyCollectionTNull = ((float[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionTNull)])?.ToList();
            model1.DoubleIReadOnlyCollectionTNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionTNull)])?.ToList();
            model1.DecimalIReadOnlyCollectionTNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionTNull)])?.ToList();
            model1.CharIReadOnlyCollectionTNull = ((char[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionTNull)])?.ToList();
            model1.DateTimeIReadOnlyCollectionTNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionTNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionTNull)])?.ToList();
            model1.TimeSpanIReadOnlyCollectionTNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionTNull)])?.ToList();
            model1.TimeOnlyIReadOnlyCollectionTNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionTNull)])?.ToList();
#endif
            model1.GuidIReadOnlyCollectionTNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionTNull)])?.ToList();

            model1.BooleanIReadOnlyCollectionTNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionTNullable)]).ToList();
            model1.ByteIReadOnlyCollectionTNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionTNullable)]).ToList();
            model1.SByteIReadOnlyCollectionTNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionTNullable)]).ToList();
            model1.Int16IReadOnlyCollectionTNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt16IReadOnlyCollectionTNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionTNullable)]).ToList();
            model1.Int32IReadOnlyCollectionTNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt32IReadOnlyCollectionTNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionTNullable)]).ToList();
            model1.Int64IReadOnlyCollectionTNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt64IReadOnlyCollectionTNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionTNullable)]).ToList();
            model1.SingleIReadOnlyCollectionTNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionTNullable)]).ToList();
            model1.DoubleIReadOnlyCollectionTNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionTNullable)]).ToList();
            model1.DecimalIReadOnlyCollectionTNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionTNullable)]).ToList();
            model1.CharIReadOnlyCollectionTNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionTNullable)]).ToList();
            model1.DateTimeIReadOnlyCollectionTNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionTNullable)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionTNullable)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionTNullable)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionTNullable)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionTNullable)]).ToList();

            model1.BooleanIReadOnlyCollectionTNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.ByteIReadOnlyCollectionTNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.SByteIReadOnlyCollectionTNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int16IReadOnlyCollectionTNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt16IReadOnlyCollectionTNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int32IReadOnlyCollectionTNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt32IReadOnlyCollectionTNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int64IReadOnlyCollectionTNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt64IReadOnlyCollectionTNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.SingleIReadOnlyCollectionTNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DoubleIReadOnlyCollectionTNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DecimalIReadOnlyCollectionTNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.CharIReadOnlyCollectionTNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DateTimeIReadOnlyCollectionTNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionTNullableEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionTNullableEmpty)]).ToList();

            model1.BooleanIReadOnlyCollectionTNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.ByteIReadOnlyCollectionTNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.SByteIReadOnlyCollectionTNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int16IReadOnlyCollectionTNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt16IReadOnlyCollectionTNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int32IReadOnlyCollectionTNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt32IReadOnlyCollectionTNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int64IReadOnlyCollectionTNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt64IReadOnlyCollectionTNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.SingleIReadOnlyCollectionTNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DoubleIReadOnlyCollectionTNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DecimalIReadOnlyCollectionTNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.CharIReadOnlyCollectionTNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DateTimeIReadOnlyCollectionTNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.TimeSpanIReadOnlyCollectionTNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanIReadOnlyCollectionTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyIReadOnlyCollectionTNullableNull)])?.ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidIReadOnlyCollectionTNullableNull)])?.ToList();

            model1.StringIReadOnlyCollectionT = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyCollectionT)]).ToList();
            model1.StringIReadOnlyCollectionTEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyCollectionTEmpty)]).ToList();
            model1.StringIReadOnlyCollectionTNull = ((string[])jsonObject[nameof(AllTypesModel.StringIReadOnlyCollectionTNull)])?.ToList();

            model1.EnumIReadOnlyCollectionT = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyCollectionTEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyCollectionTNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIReadOnlyCollectionTNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyCollectionTNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyCollectionTNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumIReadOnlyCollectionTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();


            var classThingJsonObject = jsonObject[nameof(AllTypesModel.ClassThing)];
            if (!classThingJsonObject.IsNull)
            {
                model1.ClassThing = new SimpleModel();
                model1.ClassThing.Value1 = (int)classThingJsonObject["Value1"];
                model1.ClassThing.Value2 = (string)classThingJsonObject["Value2"];
            }

            var classThingNullJsonObject = jsonObject[nameof(AllTypesModel.ClassThingNull)];
            if (!classThingNullJsonObject.IsNull)
            {
                model1.ClassThingNull = new SimpleModel();
                model1.ClassThingNull.Value1 = (int)classThingNullJsonObject["Value1"];
                model1.ClassThingNull.Value2 = (string)classThingNullJsonObject["Value2"];
            }


            var classArrayJsonObject = jsonObject[nameof(AllTypesModel.ClassArray)];
            var classArray = new List<SimpleModel>();
            foreach (var item in classArrayJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classArray.Add(obj);
                }
                else
                {
                    classArray.Add(null);
                }
            }
            model1.ClassArray = classArray.ToArray();

            var classArrayEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassArrayEmpty)];
            var classArrayEmpty = new List<SimpleModel>();
            foreach (var item in classArrayEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classArrayEmpty.Add(obj);
                }
                else
                {
                    classArrayEmpty.Add(null);
                }
            }
            model1.ClassArrayEmpty = classArrayEmpty.ToArray();

            var classEnumerableJsonObject = jsonObject[nameof(AllTypesModel.ClassEnumerable)];
            var classEnumerable = new List<SimpleModel>();
            foreach (var item in classEnumerableJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classEnumerable.Add(obj);
                }
                else
                {
                    classEnumerable.Add(null);
                }
            }
            model1.ClassEnumerable = classEnumerable;

            var classListJsonObject = jsonObject[nameof(AllTypesModel.ClassList)];
            var classList = new List<SimpleModel>();
            foreach (var item in classListJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classList.Add(obj);
                }
                else
                {
                    classList.Add(null);
                }
            }
            model1.ClassList = classList;

            var classListEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassListEmpty)];
            var classListEmpty = new List<SimpleModel>();
            foreach (var item in classListEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classListEmpty.Add(obj);
                }
                else
                {
                    classListEmpty.Add(null);
                }
            }
            model1.ClassListEmpty = classListEmpty;

            var classIListJsonObject = jsonObject[nameof(AllTypesModel.ClassIList)];
            var classIList = new List<SimpleModel>();
            foreach (var item in classIListJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIList.Add(obj);
                }
                else
                {
                    classIList.Add(null);
                }
            }
            model1.ClassIList = classIList;

            var classIListEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassIListEmpty)];
            var classIListEmpty = new List<SimpleModel>();
            foreach (var item in classIListEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIListEmpty.Add(obj);
                }
                else
                {
                    classIListEmpty.Add(null);
                }
            }
            model1.ClassIListEmpty = classIListEmpty;

            var classIReadOnlyListJsonObject = jsonObject[nameof(AllTypesModel.ClassIReadOnlyList)];
            var classIReadOnlyList = new List<SimpleModel>();
            foreach (var item in classIReadOnlyListJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIReadOnlyList.Add(obj);
                }
                else
                {
                    classIReadOnlyList.Add(null);
                }
            }
            model1.ClassIReadOnlyList = classIReadOnlyList;

            var classIReadOnlyListEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassIReadOnlyListEmpty)];
            var classIReadOnlyListEmpty = new List<SimpleModel>();
            foreach (var item in classIReadOnlyListEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIReadOnlyListEmpty.Add(obj);
                }
                else
                {
                    classIReadOnlyListEmpty.Add(null);
                }
            }
            model1.ClassIReadOnlyListEmpty = classIReadOnlyListEmpty;

            var classICollectionJsonObject = jsonObject[nameof(AllTypesModel.ClassICollection)];
            var classICollection = new List<SimpleModel>();
            foreach (var item in classICollectionJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classICollection.Add(obj);
                }
                else
                {
                    classICollection.Add(null);
                }
            }
            model1.ClassICollection = classICollection;

            var classICollectionEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassICollectionEmpty)];
            var classICollectionEmpty = new List<SimpleModel>();
            foreach (var item in classICollectionEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classICollectionEmpty.Add(obj);
                }
                else
                {
                    classICollectionEmpty.Add(null);
                }
            }
            model1.ClassICollectionEmpty = classICollectionEmpty;

            var classIReadOnlyCollectionJsonObject = jsonObject[nameof(AllTypesModel.ClassIReadOnlyCollection)];
            var classIReadOnlyCollection = new List<SimpleModel>();
            foreach (var item in classIReadOnlyCollectionJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIReadOnlyCollection.Add(obj);
                }
                else
                {
                    classIReadOnlyCollection.Add(null);
                }
            }
            model1.ClassIReadOnlyCollection = classIReadOnlyCollection;

            var classIReadOnlyCollectionEmptyJsonObject = jsonObject[nameof(AllTypesModel.ClassIReadOnlyCollectionEmpty)];
            var classIReadOnlyCollectionEmpty = new List<SimpleModel>();
            foreach (var item in classIReadOnlyCollectionEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new SimpleModel();
                    obj.Value1 = (int)item["Value1"];
                    obj.Value2 = (string)item["Value2"];
                    classIReadOnlyCollectionEmpty.Add(obj);
                }
                else
                {
                    classIReadOnlyCollectionEmpty.Add(null);
                }
            }
            model1.ClassIReadOnlyCollectionEmpty = classIReadOnlyCollectionEmpty;

            var dictionaryThingJsonObject1 = jsonObject[nameof(AllTypesModel.DictionaryThing1)];
            model1.DictionaryThing1 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject1["1"]),
                    new(2, (string)dictionaryThingJsonObject1["2"]),
                    new(3, (string)dictionaryThingJsonObject1["3"]),
                    new(4, (string)dictionaryThingJsonObject1["4"]),
                }
            );

            var dictionaryThingJsonObject2 = jsonObject[nameof(AllTypesModel.DictionaryThing2)];
            model1.DictionaryThing2 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject2["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject2["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject2["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject2["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject3 = jsonObject[nameof(AllTypesModel.DictionaryThing3)];
            model1.DictionaryThing3 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject3["1"]),
                    new(2, (string)dictionaryThingJsonObject3["2"]),
                    new(3, (string)dictionaryThingJsonObject3["3"]),
                    new(4, (string)dictionaryThingJsonObject3["4"]),
                }
            );

            var dictionaryThingJsonObject4 = jsonObject[nameof(AllTypesModel.DictionaryThing4)];
            model1.DictionaryThing4 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject4["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject4["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject4["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject4["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject5 = jsonObject[nameof(AllTypesModel.DictionaryThing5)];
            model1.DictionaryThing5 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject5["1"]),
                    new(2, (string)dictionaryThingJsonObject5["2"]),
                    new(3, (string)dictionaryThingJsonObject5["3"]),
                    new(4, (string)dictionaryThingJsonObject5["4"]),
                }
            );

            var dictionaryThingJsonObject6 = jsonObject[nameof(AllTypesModel.DictionaryThing6)];
            model1.DictionaryThing6 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject6["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject6["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject6["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject6["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject7 = jsonObject[nameof(AllTypesModel.DictionaryThing7)];
            model1.DictionaryThing7 = new ConcurrentDictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject7["1"]),
                    new(2, (string)dictionaryThingJsonObject7["2"]),
                    new(3, (string)dictionaryThingJsonObject7["3"]),
                    new(4, (string)dictionaryThingJsonObject7["4"]),
                }
            );

            var dictionaryThingJsonObject8 = jsonObject[nameof(AllTypesModel.DictionaryThing8)];
            model1.DictionaryThing8 = new ConcurrentDictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject8["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject8["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject8["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject8["4"].Bind<SimpleModel>()),
                }
            );

            var stringArrayOfArrayThingJsonObject = jsonObject[nameof(AllTypesModel.StringArrayOfArrayThing)];
            var stringList = new List<string[]>();
            foreach (var item in stringArrayOfArrayThingJsonObject)
            {
                if (item.IsNull)
                {
                    stringList.Add(null);
                    continue;
                }
                var stringSubList = new List<string>();
                foreach (var sub in item)
                {
                    stringSubList.Add((string)sub);
                }
                stringList.Add(stringSubList.ToArray());
            }
            model1.StringArrayOfArrayThing = stringList.ToArray();

            AssertHelper.AreEqual(baseModel, model1);

            var model2 = jsonObject.Bind<AllTypesModel>();
            AssertHelper.AreEqual(baseModel, model2);
        }

        [TestMethod]
        public void StringExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            var bytes = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<Exception>(bytes);
            Assert.AreEqual(model1.Message, model2.Message);
        }

        [TestMethod]
        public void StringInterface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            var json = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<ITestInterface>(json);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public void StringEmptyModel()
        {
            var baseModel = AllTypesModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var model = JsonSerializerOld.Deserialize<EmptyModel>(json);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public void StringGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();
            var typeDetail = Zerra.Reflection.TypeAnalyzer.GetTypeDetail(typeof(GetsSetsModel));

            var json = JsonSerializerOld.Serialize(baseModel);

            var model = JsonSerializerOld.Deserialize<GetsSetsModel>(baseModelJson);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public void StringReducedModel()
        {
            var model1 = CoreTypesAlternatingModel.Create();
            var json1 = JsonSerializerOld.Serialize(model1);
            var result1 = JsonSerializerOld.Deserialize<CoreTypesModel>(json1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = CoreTypesModel.Create();
            var json2 = JsonSerializerOld.Serialize(model2);
            var result2 = JsonSerializerOld.Deserialize<CoreTypesAlternatingModel>(json2);
            AssertHelper.AreEqual(result2, model2);
        }

        [TestMethod]
        public void StringLargeModel()
        {
            var models = new List<AllTypesModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(AllTypesModel.Create());

            var json = JsonSerializerOld.Serialize(models);
            var result = JsonSerializerOld.Deserialize<AllTypesModel[]>(json);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [TestMethod]
        public void StringBoxing()
        {
            var baseModel = TestBoxingModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var model = JsonSerializerOld.Deserialize<TestBoxingModel>(json);
        }

        [TestMethod]
        public void StringHashSet()
        {
            var model1 = HashSetModel.Create();
            var json = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<HashSetModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var json = JsonSerializerOld.Serialize(baseModel);
            var model = JsonSerializerOld.Deserialize<RecordModel>(json);
            //Assert.IsNotNull(model);
            //Assert.AreEqual(baseModel.Property1, model.Property1);
            //Assert.AreEqual(baseModel.Property2, model.Property2);
            //Assert.AreEqual(baseModel.Property3, model.Property3);
        }

        [TestMethod]
        public void StringPropertyName()
        {
            var baseModel = new JsonNameTestModel()
            {
                _1_Property = 5,
                property2 = 7,
                _3_Property = new SimpleModel()
                {
                    Value1 = 10,
                    Value2 = "11"
                }
            };

            var json = JsonSerializerOld.Serialize(baseModel);

            Assert.IsTrue(json.Contains("\"1property\""));
            Assert.IsTrue(json.Contains("\"property2\""));
            Assert.IsTrue(json.Contains("\"3property\""));

            json.Replace("\"property2\"", "\"PROPERTY2\"");

            var model = JsonSerializerOld.Deserialize<JsonNameTestModel>(json);
            Assert.AreEqual(baseModel._1_Property, model._1_Property);
            Assert.AreEqual(baseModel.property2, model.property2);
            Assert.IsNotNull(model._3_Property);
            Assert.AreEqual(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.AreEqual(baseModel._3_Property.Value2, model._3_Property.Value2);
        }

        [TestMethod]
        public async Task StreamMatchesNewtonsoft()
        {
            var baseModel = AllTypesModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync();

            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = AllTypesModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync();

            using var stream2 = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream2, baseModel, options);
            stream1.Position = 0;
            using var sr2 = new StreamReader(stream1, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync();

            Assert.IsTrue(json1 == json2);

            //swap serializers
            using var stream3 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream3);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<AllTypesModel>(stream4, options);

            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var baseModel = AllTypesModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamEnumAsNumber()
        {
            var options = new JsonSerializerOptionsOld()
            {
                EnumAsNumber = true
            };

            var baseModel = AllTypesModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel, options);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamConvertNullables()
        {
            var baseModel = BasicTypesNotNullable.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializerOld.DeserializeAsync<BasicTypesNullable>(stream1);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<BasicTypesNotNullable>(stream2);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model2);
        }

        [TestMethod]
        public async Task StreamConvertTypes()
        {
            var baseModel = AllTypesModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializerOld.DeserializeAsync<AllTypesAsStringsModel>(stream1);
            AllTypesModel.AreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream2);
            AssertHelper.AreEqual(baseModel, model2);
        }

        [TestMethod]
        public async Task StreamNumbers()
        {
            for (var i = -10; i < 10; i++)
                await StreamTestNumber(i);
            for (decimal i = -2; i < 2; i += 0.1m)
                await StreamTestNumber(i);

            await StreamTestNumber(Byte.MinValue);
            await StreamTestNumber(Byte.MaxValue);
            await StreamTestNumber(SByte.MinValue);
            await StreamTestNumber(SByte.MaxValue);

            await StreamTestNumber(Int16.MinValue);
            await StreamTestNumber(Int16.MaxValue);
            await StreamTestNumber(UInt16.MinValue);
            await StreamTestNumber(UInt16.MaxValue);

            await StreamTestNumber(Int32.MinValue);
            await StreamTestNumber(Int32.MaxValue);
            await StreamTestNumber(UInt32.MinValue);
            await StreamTestNumber(UInt32.MaxValue);

            await StreamTestNumber(Int64.MinValue);
            await StreamTestNumber(Int64.MaxValue);
            await StreamTestNumber(UInt64.MinValue);
            await StreamTestNumber(UInt64.MaxValue);

            await StreamTestNumber(Single.MinValue);
            await StreamTestNumber(Single.MaxValue);

            await StreamTestNumber(Double.MinValue);
            await StreamTestNumber(Double.MaxValue);

            await StreamTestNumber(Decimal.MinValue);
            await StreamTestNumber(Decimal.MaxValue);

            await StreamTestNumberAsStream(Double.MinValue);
            await StreamTestNumberAsStream(Double.MaxValue);

            await StreamTestNumberAsStream(Decimal.MinValue);
            await StreamTestNumberAsStream(Decimal.MaxValue);
        }
        private static async Task StreamTestNumber(byte value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<byte>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(sbyte value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<sbyte>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(short value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<short>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(ushort value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<ushort>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(int value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<int>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(uint value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<uint>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(long value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<long>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(ulong value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<ulong>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(decimal value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<decimal>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(float value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<float>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(double value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<double>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumberAsStream(double value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<string>(stream);
            Assert.AreEqual(json, result);
        }
        private static async Task StreamTestNumberAsStream(decimal value)
        {
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<string>(stream);
            Assert.AreEqual(json, result);
        }

        [TestMethod]
        public async Task StreamEnumConversion()
        {
            //var model1 = new EnumConversionModel1() { Thing = EnumModel.Item2 };
            //var test1 = JsonSerializer.Serialize(model1);
            //var result1 = JsonSerializer.Deserialize<EnumConversionModel2>(test1);
            //Assert.AreEqual((int)model1.Thing, result1.Thing);

            var model2 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = 2,
                Thing3 = 3,
                Thing4 = 4
            };

            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializerOld.DeserializeAsync<EnumConversionModel1>(stream2);
            Assert.AreEqual(model2.Thing1, (int)result2.Thing1);
            Assert.AreEqual(model2.Thing2, (int?)result2.Thing2);
            Assert.AreEqual(model2.Thing3, (int)result2.Thing3);
            Assert.AreEqual(model2.Thing4, (int?)result2.Thing4);

            var model3 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = null,
                Thing3 = 3,
                Thing4 = null
            };

            using var stream3 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream3, model3);
            stream3.Position = 0;
            var result3 = await JsonSerializerOld.DeserializeAsync<EnumConversionModel1>(stream3);
            Assert.AreEqual(model3.Thing1, (int)result3.Thing1);
            Assert.AreEqual(default, result3.Thing2);
            Assert.AreEqual(model3.Thing3, (int)result3.Thing3);
            Assert.AreEqual(model3.Thing4, (int?)result3.Thing4);
        }

        [TestMethod]
        public async Task StreamPretty()
        {
            var baseModel = AllTypesModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            stream1.Position = 0;
            var json = await sr1.ReadToEndAsync();

            string jsonPretty;
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader);
                var jsonWriter = new Newtonsoft.Json.JsonTextWriter(stringWriter) { Formatting = Newtonsoft.Json.Formatting.Indented, Indentation = 4 };
                jsonWriter.WriteToken(jsonReader);
                jsonPretty = stringWriter.ToString();
            }

            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(jsonPretty));
            var model = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream2);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamNameless()
        {
            var options = new JsonSerializerOptionsOld()
            {
                Nameless = true
            };

            var baseModel = AllTypesModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptionsOld()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = AllTypesModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<AllTypesModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamEmptys()
        {
            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync<string>(stream1, null);
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            stream1.Position = 0;
            var json1 = await sr1.ReadToEndAsync();
            Assert.AreEqual("null", json1);

            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync<string>(stream2, String.Empty);
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            stream2.Position = 0;
            var json2 = await sr2.ReadToEndAsync();
            Assert.AreEqual("\"\"", json2);

            using var stream3 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync<object>(stream3, null);
            using var sr3 = new StreamReader(stream3, Encoding.UTF8);
            stream3.Position = 0;
            var json3 = await sr3.ReadToEndAsync();
            Assert.AreEqual("null", json3);

            using var stream4 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync<object>(stream4, new object());
            using var sr4 = new StreamReader(stream4, Encoding.UTF8);
            stream4.Position = 0;
            var json4 = await sr4.ReadToEndAsync();
            Assert.AreEqual("{}", json4);

            var model1 = await JsonSerializerOld.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.IsNull(model1);

            var model2 = await JsonSerializerOld.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.AreEqual("", model2);

            var model3 = await JsonSerializerOld.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.AreEqual("", model3);

            var model4 = await JsonSerializerOld.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.AreEqual("", model4);

            var model5 = await JsonSerializerOld.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.IsNull(model5);

            var model6 = await JsonSerializerOld.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.IsNull(model6);

            var model7 = await JsonSerializerOld.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.IsNull(model7);

            var model8 = await JsonSerializerOld.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.IsNotNull(model8);
        }

        [TestMethod]
        public async Task StreamEscaping()
        {
            for (var i = 0; i < (int)byte.MaxValue; i++)
            {
                var c = (char)i;
                using var stream = new MemoryStream();
                await JsonSerializerOld.SerializeAsync(stream, c);
                using var sr = new StreamReader(stream, Encoding.UTF8);
                stream.Position = 0;
                var json = await sr.ReadToEndAsync();
                stream.Position = 0;
                var result = await JsonSerializerOld.DeserializeAsync<char>(stream);
                Assert.AreEqual(c, result);

                switch (c)
                {
                    case '\\':
                    case '"':
                    case '/':
                    case '\b':
                    case '\t':
                    case '\n':
                    case '\f':
                    case '\r':
                        Assert.AreEqual(4, json.Length);
                        break;
                    default:
                        if (c < ' ')
                            Assert.AreEqual(8, json.Length);
                        break;
                }
            }
        }

        [TestMethod]
        public async Task StreamExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<Exception>(stream);
            Assert.AreEqual(model1.Message, model2.Message);
        }

        [TestMethod]
        public async Task StreamInterface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<ITestInterface>(stream);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public async Task StreamEmptyModel()
        {
            var baseModel = AllTypesModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<EmptyModel>(stream);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public async Task StreamGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();
            var typeDetail = Zerra.Reflection.TypeAnalyzer.GetTypeDetail(typeof(GetsSetsModel));

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<GetsSetsModel>(stream);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public async Task StreamReducedModel()
        {
            var model1 = CoreTypesAlternatingModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var result1 = await JsonSerializerOld.DeserializeAsync<CoreTypesModel>(stream1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = CoreTypesModel.Create();
            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializerOld.DeserializeAsync<CoreTypesAlternatingModel>(stream2);
            AssertHelper.AreEqual(result2, model2);
        }

        [TestMethod]
        public async Task StreamLargeModel()
        {
#if DEBUG
            JsonSerializerOld.Testing = false;
#endif

            var models = new List<AllTypesModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(AllTypesModel.Create());

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, models);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<AllTypesModel[]>(stream);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);

#if DEBUG
            JsonSerializerOld.Testing = true;
#endif
        }

        [TestMethod]
        public async Task StreamRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<RecordModel>(stream);
            //Assert.IsNotNull(model);
            //Assert.AreEqual(baseModel.Property1, model.Property1);
            //Assert.AreEqual(baseModel.Property2, model.Property2);
            //Assert.AreEqual(baseModel.Property3, model.Property3);
        }

        [TestMethod]
        public async Task StreamHashSet()
        {
            var model1 = HashSetModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<HashSetModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamBoxing()
        {
            var baseModel = TestBoxingModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<TestBoxingModel>(stream);
        }

        [TestMethod]
        public async Task StreamPropertyName()
        {
            var baseModel = new JsonNameTestModel()
            {
                _1_Property = 5,
                property2 = 7,
                _3_Property = new SimpleModel()
                {
                    Value1 = 10,
                    Value2 = "11"
                }
            };

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            Assert.IsTrue(json.Contains("\"1property\""));
            Assert.IsTrue(json.Contains("\"property2\""));
            Assert.IsTrue(json.Contains("\"3property\""));

            json.Replace("\"property2\"", "\"PROPERTY2\"");

            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializerOld.DeserializeAsync<JsonNameTestModel>(stream2);
            Assert.AreEqual(baseModel._1_Property, model._1_Property);
            Assert.AreEqual(baseModel.property2, model.property2);
            Assert.IsNotNull(model._3_Property);
            Assert.AreEqual(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.AreEqual(baseModel._3_Property.Value2, model._3_Property.Value2);
        }
    }
}
