// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zerra.Serialization;

namespace Zerra.Test
{
    [TestClass]
    public class JsonSerializerTest
    {
        public JsonSerializerTest()
        {
#if DEBUG
            JsonSerializer.Testing = true;
#endif
        }

        [TestMethod]
        public void StringMatchesNewtonsoft()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            var model1 = JsonSerializer.Deserialize<AllTypesModel>(json2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void StringMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = Factory.GetAllTypesModel();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel, options);

            Assert.IsTrue(json1 == json2);

            //swap serializers
            var model1 = JsonSerializer.Deserialize<AllTypesModel>(json2);
            var model2 = System.Text.Json.JsonSerializer.Deserialize<AllTypesModel>(json1, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypes()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<AllTypesModel>(json);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringEnumAsNumbers()
        {
            var options = new JsonSerializerOptions()
            {
                EnumAsNumber = true
            };

            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel, options);
            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));
            var model = JsonSerializer.Deserialize<AllTypesModel>(json, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringConvertNullables()
        {
            var baseModel = Factory.GetBasicTypesNotNullableModel();
            var json1 = JsonSerializer.Serialize(baseModel);
            var model1 = JsonSerializer.Deserialize<BasicTypesNullable>(json1);
            Factory.AssertAreEqual(baseModel, model1);

            var json2 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<BasicTypesNotNullable>(json2);
            Factory.AssertAreEqual(baseModel, model2);
        }

        [TestMethod]
        public void StringConvertTypes()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json1 = JsonSerializer.Serialize(baseModel);
            var model1 = JsonSerializer.Deserialize<AllTypesAsStringsModel>(json1);
            Factory.AssertAreEqual(baseModel, model1);

            var json2 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<AllTypesModel>(json2);
            Factory.AssertAreEqual(baseModel, model2);
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
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<byte>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(sbyte value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<sbyte>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(short value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<short>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(ushort value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<ushort>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(int value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<int>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(uint value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<uint>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(long value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<long>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(ulong value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<ulong>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(decimal value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<decimal>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(float value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<float>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumber(double value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<double>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumberAsString(double value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<string>(json);
            Assert.AreEqual(json, result);
        }
        private static void StringTestNumberAsString(decimal value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<string>(json);
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

            var json2 = JsonSerializer.Serialize(model2);
            var result2 = JsonSerializer.Deserialize<EnumConversionModel1>(json2);
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

            var json3 = JsonSerializer.Serialize(model3);
            var result3 = JsonSerializer.Deserialize<EnumConversionModel1>(json3);
            Assert.AreEqual(model3.Thing1, (int)result3.Thing1);
            Assert.AreEqual(default, result3.Thing2);
            Assert.AreEqual(model3.Thing3, (int)result3.Thing3);
            Assert.AreEqual(model3.Thing4, (int?)result3.Thing4);
        }

        [TestMethod]
        public void StringPretty()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel);
            string jsonPretty;
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader);
                var jsonWriter = new Newtonsoft.Json.JsonTextWriter(stringWriter) { Formatting = Newtonsoft.Json.Formatting.Indented, Indentation = 4 };
                jsonWriter.WriteToken(jsonReader);
                jsonPretty = stringWriter.ToString();
            }
            var model = JsonSerializer.Deserialize<AllTypesModel>(jsonPretty);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringNameless()
        {
            var options = new JsonSerializerOptions()
            {
                Nameless = true
            };

            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel, options);
            var model = JsonSerializer.Deserialize<AllTypesModel>(json, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptions()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel, options);
            var model = JsonSerializer.Deserialize<AllTypesModel>(json, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringEmptys()
        {
            var json1 = JsonSerializer.Serialize<string>(null);
            Assert.AreEqual("null", json1);

            var json2 = JsonSerializer.Serialize<string>(String.Empty);
            Assert.AreEqual("\"\"", json2);

            var json3 = JsonSerializer.Serialize<object>(null);
            Assert.AreEqual("null", json3);

            var json4 = JsonSerializer.Serialize<object>(new object());
            Assert.AreEqual("{}", json4);

            var model1 = JsonSerializer.Deserialize<string>("null");
            Assert.IsNull(model1);

            var model2 = JsonSerializer.Deserialize<string>("");
            Assert.AreEqual("", model2);

            var model3 = JsonSerializer.Deserialize<string>("\"\"");
            Assert.AreEqual("", model3);

            var model4 = JsonSerializer.Deserialize<string>("{}");
            Assert.AreEqual("", model4);

            var model5 = JsonSerializer.Deserialize<object>("null");
            Assert.IsNull(model5);

            var model6 = JsonSerializer.Deserialize<object>("");
            Assert.IsNull(model6);

            var model7 = JsonSerializer.Deserialize<object>("\"\"");
            Assert.IsNull(model7);

            var model8 = JsonSerializer.Deserialize<object>("{}");
            Assert.IsNotNull(model8);
        }

        [TestMethod]
        public void StringEscaping()
        {
            for (var i = 0; i < (int)byte.MaxValue; i++)
            {
                var c = (char)i;
                var json = JsonSerializer.Serialize(c);
                var result = JsonSerializer.Deserialize<char>(json);
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
            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel);
            var jsonObject = JsonSerializer.DeserializeJsonObject(json);

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

            model1.BooleanList = ((bool[])jsonObject[nameof(AllTypesModel.BooleanList)]).ToList();
            model1.ByteList = ((byte[])jsonObject[nameof(AllTypesModel.ByteList)]).ToList();
            model1.SByteList = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteList)]).ToList();
            model1.Int16List = ((short[])jsonObject[nameof(AllTypesModel.Int16List)]).ToList();
            model1.UInt16List = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16List)]).ToList();
            model1.Int32List = ((int[])jsonObject[nameof(AllTypesModel.Int32List)]).ToList();
            model1.UInt32List = ((uint[])jsonObject[nameof(AllTypesModel.UInt32List)]).ToList();
            model1.Int64List = ((long[])jsonObject[nameof(AllTypesModel.Int64List)]).ToList();
            model1.UInt64List = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64List)]).ToList();
            model1.SingleList = ((float[])jsonObject[nameof(AllTypesModel.SingleList)]).ToList();
            model1.DoubleList = ((double[])jsonObject[nameof(AllTypesModel.DoubleList)]).ToList();
            model1.DecimalList = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalList)]).ToList();
            model1.CharList = ((char[])jsonObject[nameof(AllTypesModel.CharList)]).ToList();
            model1.DateTimeList = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeList)]).ToList();
            model1.DateTimeOffsetList = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetList)]).ToList();
            model1.TimeSpanList = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanList)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyList = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyList)]).ToList();
            model1.TimeOnlyList = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyList)]).ToList();
#endif
            model1.GuidList = ((Guid[])jsonObject[nameof(AllTypesModel.GuidList)]).ToList();

            model1.BooleanListEmpty = ((bool[])jsonObject[nameof(AllTypesModel.BooleanListEmpty)]).ToList();
            model1.ByteListEmpty = ((byte[])jsonObject[nameof(AllTypesModel.ByteListEmpty)]).ToList();
            model1.SByteListEmpty = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteListEmpty)]).ToList();
            model1.Int16ListEmpty = ((short[])jsonObject[nameof(AllTypesModel.Int16ListEmpty)]).ToList();
            model1.UInt16ListEmpty = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ListEmpty)]).ToList();
            model1.Int32ListEmpty = ((int[])jsonObject[nameof(AllTypesModel.Int32ListEmpty)]).ToList();
            model1.UInt32ListEmpty = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ListEmpty)]).ToList();
            model1.Int64ListEmpty = ((long[])jsonObject[nameof(AllTypesModel.Int64ListEmpty)]).ToList();
            model1.UInt64ListEmpty = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ListEmpty)]).ToList();
            model1.SingleListEmpty = ((float[])jsonObject[nameof(AllTypesModel.SingleListEmpty)]).ToList();
            model1.DoubleListEmpty = ((double[])jsonObject[nameof(AllTypesModel.DoubleListEmpty)]).ToList();
            model1.DecimalListEmpty = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalListEmpty)]).ToList();
            model1.CharListEmpty = ((char[])jsonObject[nameof(AllTypesModel.CharListEmpty)]).ToList();
            model1.DateTimeListEmpty = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeListEmpty)]).ToList();
            model1.DateTimeOffsetListEmpty = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListEmpty)]).ToList();
            model1.TimeSpanListEmpty = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanListEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListEmpty = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyListEmpty)]).ToList();
            model1.TimeOnlyListEmpty = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyListEmpty)]).ToList();
#endif
            model1.GuidListEmpty = ((Guid[])jsonObject[nameof(AllTypesModel.GuidListEmpty)]).ToList();

            model1.BooleanListNull = ((bool[])jsonObject[nameof(AllTypesModel.BooleanListNull)])?.ToList();
            model1.ByteListNull = ((byte[])jsonObject[nameof(AllTypesModel.ByteListNull)])?.ToList();
            model1.SByteListNull = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteListNull)])?.ToList();
            model1.Int16ListNull = ((short[])jsonObject[nameof(AllTypesModel.Int16ListNull)])?.ToList();
            model1.UInt16ListNull = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16ListNull)])?.ToList();
            model1.Int32ListNull = ((int[])jsonObject[nameof(AllTypesModel.Int32ListNull)])?.ToList();
            model1.UInt32ListNull = ((uint[])jsonObject[nameof(AllTypesModel.UInt32ListNull)])?.ToList();
            model1.Int64ListNull = ((long[])jsonObject[nameof(AllTypesModel.Int64ListNull)])?.ToList();
            model1.UInt64ListNull = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64ListNull)])?.ToList();
            model1.SingleListNull = ((float[])jsonObject[nameof(AllTypesModel.SingleListNull)])?.ToList();
            model1.DoubleListNull = ((double[])jsonObject[nameof(AllTypesModel.DoubleListNull)])?.ToList();
            model1.DecimalListNull = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalListNull)])?.ToList();
            model1.CharListNull = ((char[])jsonObject[nameof(AllTypesModel.CharListNull)])?.ToList();
            model1.DateTimeListNull = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeListNull)])?.ToList();
            model1.DateTimeOffsetListNull = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListNull)])?.ToList();
            model1.TimeSpanListNull = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanListNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListNull = ((DateOnly[])jsonObject[nameof(AllTypesModel.DateOnlyListNull)])?.ToList();
            model1.TimeOnlyListNull = ((TimeOnly[])jsonObject[nameof(AllTypesModel.TimeOnlyListNull)])?.ToList();
#endif
            model1.GuidListNull = ((Guid[])jsonObject[nameof(AllTypesModel.GuidListNull)])?.ToList();

            model1.BooleanListNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListNullable)]).ToList();
            model1.ByteListNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListNullable)]).ToList();
            model1.SByteListNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListNullable)]).ToList();
            model1.Int16ListNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListNullable)]).ToList();
            model1.UInt16ListNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListNullable)]).ToList();
            model1.Int32ListNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListNullable)]).ToList();
            model1.UInt32ListNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListNullable)]).ToList();
            model1.Int64ListNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListNullable)]).ToList();
            model1.UInt64ListNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListNullable)]).ToList();
            model1.SingleListNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleListNullable)]).ToList();
            model1.DoubleListNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListNullable)]).ToList();
            model1.DecimalListNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListNullable)]).ToList();
            model1.CharListNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharListNullable)]).ToList();
            model1.DateTimeListNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListNullable)]).ToList();
            model1.DateTimeOffsetListNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListNullable)]).ToList();
            model1.TimeSpanListNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListNullable = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListNullable)]).ToList();
            model1.TimeOnlyListNullable = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListNullable)]).ToList();
#endif
            model1.GuidListNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListNullable)]).ToList();

            model1.BooleanListNullableEmpty = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListNullableEmpty)]).ToList();
            model1.ByteListNullableEmpty = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListNullableEmpty)]).ToList();
            model1.SByteListNullableEmpty = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListNullableEmpty)]).ToList();
            model1.Int16ListNullableEmpty = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListNullableEmpty)]).ToList();
            model1.UInt16ListNullableEmpty = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListNullableEmpty)]).ToList();
            model1.Int32ListNullableEmpty = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListNullableEmpty)]).ToList();
            model1.UInt32ListNullableEmpty = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListNullableEmpty)]).ToList();
            model1.Int64ListNullableEmpty = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListNullableEmpty)]).ToList();
            model1.UInt64ListNullableEmpty = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListNullableEmpty)]).ToList();
            model1.SingleListNullableEmpty = ((float?[])jsonObject[nameof(AllTypesModel.SingleListNullableEmpty)]).ToList();
            model1.DoubleListNullableEmpty = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListNullableEmpty)]).ToList();
            model1.DecimalListNullableEmpty = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListNullableEmpty)]).ToList();
            model1.CharListNullableEmpty = ((char?[])jsonObject[nameof(AllTypesModel.CharListNullableEmpty)]).ToList();
            model1.DateTimeListNullableEmpty = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListNullableEmpty)]).ToList();
            model1.DateTimeOffsetListNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListNullableEmpty)]).ToList();
            model1.TimeSpanListNullableEmpty = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListNullableEmpty = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListNullableEmpty)]).ToList();
            model1.TimeOnlyListNullableEmpty = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListNullableEmpty)]).ToList();
#endif
            model1.GuidListNullableEmpty = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListNullableEmpty)]).ToList();

            model1.BooleanListNullableNull = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListNullableNull)])?.ToList();
            model1.ByteListNullableNull = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListNullableNull)])?.ToList();
            model1.SByteListNullableNull = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListNullableNull)])?.ToList();
            model1.Int16ListNullableNull = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListNullableNull)])?.ToList();
            model1.UInt16ListNullableNull = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListNullableNull)])?.ToList();
            model1.Int32ListNullableNull = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListNullableNull)])?.ToList();
            model1.UInt32ListNullableNull = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListNullableNull)])?.ToList();
            model1.Int64ListNullableNull = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListNullableNull)])?.ToList();
            model1.UInt64ListNullableNull = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListNullableNull)])?.ToList();
            model1.SingleListNullableNull = ((float?[])jsonObject[nameof(AllTypesModel.SingleListNullableNull)])?.ToList();
            model1.DoubleListNullableNull = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListNullableNull)])?.ToList();
            model1.DecimalListNullableNull = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListNullableNull)])?.ToList();
            model1.CharListNullableNull = ((char?[])jsonObject[nameof(AllTypesModel.CharListNullableNull)])?.ToList();
            model1.DateTimeListNullableNull = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListNullableNull)])?.ToList();
            model1.DateTimeOffsetListNullableNull = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListNullableNull)])?.ToList();
            model1.TimeSpanListNullableNull = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListNullableNull = ((DateOnly?[])jsonObject[nameof(AllTypesModel.DateOnlyListNullableNull)])?.ToList();
            model1.TimeOnlyListNullableNull = ((TimeOnly?[])jsonObject[nameof(AllTypesModel.TimeOnlyListNullableNull)])?.ToList();
#endif
            model1.GuidListNullableNull = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListNullableNull)])?.ToList();

            model1.StringList = ((string[])jsonObject[nameof(AllTypesModel.StringList)]).ToList();
            model1.StringListEmpty = ((string[])jsonObject[nameof(AllTypesModel.StringListEmpty)]).ToList();
            model1.StringListNull = ((string[])jsonObject[nameof(AllTypesModel.StringListNull)])?.ToList();

            model1.EnumList = (((string[])jsonObject[nameof(AllTypesModel.EnumList)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumListEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListNull = (((string[])jsonObject[nameof(AllTypesModel.EnumListNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumListNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumListNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListNullableEmpty = (((string[])jsonObject[nameof(AllTypesModel.EnumListNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListNullableNull = (((string[])jsonObject[nameof(AllTypesModel.EnumListNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            var classThingJsonObject = jsonObject[nameof(AllTypesModel.ClassThing)];
            if (!classThingJsonObject.IsNull)
            {
                model1.ClassThing = new BasicModel();
                model1.ClassThing.Value1 = (int)classThingJsonObject["Value1"];
                model1.ClassThing.Value2 = (string)classThingJsonObject["Value2"];
            }

            var classThingNullJsonObject = jsonObject[nameof(AllTypesModel.ClassThingNull)];
            if (!classThingNullJsonObject.IsNull)
            {
                model1.ClassThingNull = new BasicModel();
                model1.ClassThingNull.Value1 = (int)classThingNullJsonObject["Value1"];
                model1.ClassThingNull.Value2 = (string)classThingNullJsonObject["Value2"];
            }


            var classArrayJsonObject = jsonObject[nameof(AllTypesModel.ClassArray)];
            var classArray = new List<BasicModel>();
            foreach (var item in classArrayJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
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
            var classArrayEmpty = new List<BasicModel>();
            foreach (var item in classArrayEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
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
            var classEnumerable = new List<BasicModel>();
            foreach (var item in classEnumerableJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
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
            var classList = new List<BasicModel>();
            foreach (var item in classListJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
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
            var classListEmpty = new List<BasicModel>();
            foreach (var item in classListEmptyJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
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
            model1.DictionaryThing2 = new Dictionary<int, BasicModel>(
                new KeyValuePair<int, BasicModel>[]
                {
                    new(1, dictionaryThingJsonObject2["1"].Bind<BasicModel>()),
                    new(2, dictionaryThingJsonObject2["2"].Bind<BasicModel>()),
                    new(3, dictionaryThingJsonObject2["3"].Bind<BasicModel>()),
                    new(4, dictionaryThingJsonObject2["4"].Bind<BasicModel>()),
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

            Factory.AssertAreEqual(baseModel, model1);

            var model2 = jsonObject.Bind<AllTypesModel>();
            Factory.AssertAreEqual(baseModel, model2);
        }

        [TestMethod]
        public void StringExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            var bytes = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<Exception>(bytes);
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
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<ITestInterface>(json);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public void StringEmptyModel()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<EmptyModel>(json);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public void StringGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();
            var typeDetail = Zerra.Reflection.TypeAnalyzer.GetTypeDetail(typeof(GetsSetsModel));

            var json = JsonSerializer.Serialize(baseModel);

            var model = JsonSerializer.Deserialize<GetsSetsModel>(baseModelJson);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public void StringReducedModel()
        {
            var model1 = Factory.GetCoreTypesAlternatingModel();
            var json1 = JsonSerializer.Serialize(model1);
            var result1 = JsonSerializer.Deserialize<CoreTypesModel>(json1);
            Factory.AssertAreEqual(model1, result1);

            var model2 = Factory.GetCoreTypesModel();
            var json2 = JsonSerializer.Serialize(model2);
            var result2 = JsonSerializer.Deserialize<CoreTypesAlternatingModel>(json2);
            Factory.AssertAreEqual(result2, model2);
        }

        [TestMethod]
        public void StringLargeModel()
        {
            var models = new List<AllTypesModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(Factory.GetAllTypesModel());

            var json = JsonSerializer.Serialize(models);
            var result = JsonSerializer.Deserialize<AllTypesModel[]>(json);

            for (var i = 0; i < models.Count; i++)
                Factory.AssertAreEqual(models[i], result[i]);
        }

        [TestMethod]
        public void StringBoxing()
        {
            var baseModel = Factory.GetBoxingModel();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<TestBoxingModel>(json);
        }

        [TestMethod]
        public void StringHashSet()
        {
            var model1 = Factory.GetHashSetModel();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<HashSetModel>(json);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void StringRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<RecordModel>(json);
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
                _3_Property = new BasicModel()
                {
                    Value1 = 10,
                    Value2 = "11"
                }
            };

            var json = JsonSerializer.Serialize(baseModel);

            Assert.IsTrue(json.Contains("\"1property\""));
            Assert.IsTrue(json.Contains("\"property2\""));
            Assert.IsTrue(json.Contains("\"3property\""));

            json.Replace("\"property2\"", "\"PROPERTY2\"");

            var model = JsonSerializer.Deserialize<JsonNameTestModel>(json);
            Assert.AreEqual(baseModel._1_Property, model._1_Property);
            Assert.AreEqual(baseModel.property2, model.property2);
            Assert.IsNotNull(model._3_Property);
            Assert.AreEqual(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.AreEqual(baseModel._3_Property.Value2, model._3_Property.Value2);
        }

        [TestMethod]
        public async Task StreamMatchesNewtonsoft()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync();

            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = Factory.GetAllTypesModel();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);
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
            var model1 = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream3);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<AllTypesModel>(stream4, options);

            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamEnumAsNumber()
        {
            var options = new JsonSerializerOptions()
            {
                EnumAsNumber = true
            };

            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamConvertNullables()
        {
            var baseModel = Factory.GetBasicTypesNotNullableModel();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<BasicTypesNullable>(stream1);
            Factory.AssertAreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<BasicTypesNotNullable>(stream2);
            Factory.AssertAreEqual(baseModel, model2);
        }

        [TestMethod]
        public async Task StreamConvertTypes()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<AllTypesAsStringsModel>(stream1);
            Factory.AssertAreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream2);
            Factory.AssertAreEqual(baseModel, model2);
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
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<byte>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(sbyte value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<sbyte>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(short value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<short>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(ushort value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<ushort>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(int value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<int>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(uint value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<uint>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(long value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<long>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(ulong value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<ulong>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(decimal value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<decimal>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(float value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<float>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumber(double value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<double>(stream);
            Assert.AreEqual(value, result);
        }
        private static async Task StreamTestNumberAsStream(double value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<string>(stream);
            Assert.AreEqual(json, result);
        }
        private static async Task StreamTestNumberAsStream(decimal value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<string>(stream);
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
            await JsonSerializer.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream2);
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
            await JsonSerializer.SerializeAsync(stream3, model3);
            stream3.Position = 0;
            var result3 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream3);
            Assert.AreEqual(model3.Thing1, (int)result3.Thing1);
            Assert.AreEqual(default, result3.Thing2);
            Assert.AreEqual(model3.Thing3, (int)result3.Thing3);
            Assert.AreEqual(model3.Thing4, (int?)result3.Thing4);
        }

        [TestMethod]
        public async Task StreamPretty()
        {
            var baseModel = Factory.GetAllTypesModel();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);
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
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream2);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamNameless()
        {
            var options = new JsonSerializerOptions()
            {
                Nameless = true
            };

            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptions()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream, options);
            Factory.AssertAreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamEmptys()
        {
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream1, null);
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            stream1.Position = 0;
            var json1 = await sr1.ReadToEndAsync();
            Assert.AreEqual("null", json1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream2, String.Empty);
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            stream2.Position = 0;
            var json2 = await sr2.ReadToEndAsync();
            Assert.AreEqual("\"\"", json2);

            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream3, null);
            using var sr3 = new StreamReader(stream3, Encoding.UTF8);
            stream3.Position = 0;
            var json3 = await sr3.ReadToEndAsync();
            Assert.AreEqual("null", json3);

            using var stream4 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream4, new object());
            using var sr4 = new StreamReader(stream4, Encoding.UTF8);
            stream4.Position = 0;
            var json4 = await sr4.ReadToEndAsync();
            Assert.AreEqual("{}", json4);

            var model1 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.IsNull(model1);

            var model2 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.AreEqual("", model2);

            var model3 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.AreEqual("", model3);

            var model4 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.AreEqual("", model4);

            var model5 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.IsNull(model5);

            var model6 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.IsNull(model6);

            var model7 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.IsNull(model7);

            var model8 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.IsNotNull(model8);
        }

        [TestMethod]
        public async Task StreamEscaping()
        {
            for (var i = 0; i < (int)byte.MaxValue; i++)
            {
                var c = (char)i;
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, c);
                using var sr = new StreamReader(stream, Encoding.UTF8);
                stream.Position = 0;
                var json = await sr.ReadToEndAsync();
                stream.Position = 0;
                var result = await JsonSerializer.DeserializeAsync<char>(stream);
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

        //[TestMethod]
        //public async Task StreamJsonObject()
        //{
        //    var baseModel = Factory.GetAllTypesModel();
        //    var json = JsonSerializer.Serialize(baseModel);
        //    var jsonObject = JsonSerializer.DeserializeJsonObject(json);

        //    var json2 = jsonObject.ToString();

        //    Assert.AreEqual(json, json2);

        //    var model1 = new AllTypesModel();

        //    model1.BooleanThing = (bool)jsonObject[nameof(AllTypesModel.BooleanThing)];
        //    model1.ByteThing = (byte)jsonObject[nameof(AllTypesModel.ByteThing)];
        //    model1.SByteThing = (sbyte)jsonObject[nameof(AllTypesModel.SByteThing)];
        //    model1.Int16Thing = (short)jsonObject[nameof(AllTypesModel.Int16Thing)];
        //    model1.UInt16Thing = (ushort)jsonObject[nameof(AllTypesModel.UInt16Thing)];
        //    model1.Int32Thing = (int)jsonObject[nameof(AllTypesModel.Int32Thing)];
        //    model1.UInt32Thing = (uint)jsonObject[nameof(AllTypesModel.UInt32Thing)];
        //    model1.Int64Thing = (long)jsonObject[nameof(AllTypesModel.Int64Thing)];
        //    model1.UInt64Thing = (ulong)jsonObject[nameof(AllTypesModel.UInt64Thing)];
        //    model1.SingleThing = (float)jsonObject[nameof(AllTypesModel.SingleThing)];
        //    model1.DoubleThing = (double)jsonObject[nameof(AllTypesModel.DoubleThing)];
        //    model1.DecimalThing = (decimal)jsonObject[nameof(AllTypesModel.DecimalThing)];
        //    model1.CharThing = (char)jsonObject[nameof(AllTypesModel.CharThing)];
        //    model1.DateTimeThing = (DateTime)jsonObject[nameof(AllTypesModel.DateTimeThing)];
        //    model1.DateTimeOffsetThing = (DateTimeOffset)jsonObject[nameof(AllTypesModel.DateTimeOffsetThing)];
        //    model1.TimeSpanThing = (TimeSpan)jsonObject[nameof(AllTypesModel.TimeSpanThing)];
        //    model1.GuidThing = (Guid)jsonObject[nameof(AllTypesModel.GuidThing)];

        //    model1.BooleanThingNullable = (bool?)jsonObject[nameof(AllTypesModel.BooleanThingNullable)];
        //    model1.ByteThingNullable = (byte?)jsonObject[nameof(AllTypesModel.ByteThingNullable)];
        //    model1.SByteThingNullable = (sbyte?)jsonObject[nameof(AllTypesModel.SByteThingNullable)];
        //    model1.Int16ThingNullable = (short?)jsonObject[nameof(AllTypesModel.Int16ThingNullable)];
        //    model1.UInt16ThingNullable = (ushort?)jsonObject[nameof(AllTypesModel.UInt16ThingNullable)];
        //    model1.Int32ThingNullable = (int?)jsonObject[nameof(AllTypesModel.Int32ThingNullable)];
        //    model1.UInt32ThingNullable = (uint?)jsonObject[nameof(AllTypesModel.UInt32ThingNullable)];
        //    model1.Int64ThingNullable = (long?)jsonObject[nameof(AllTypesModel.Int64ThingNullable)];
        //    model1.UInt64ThingNullable = (ulong?)jsonObject[nameof(AllTypesModel.UInt64ThingNullable)];
        //    model1.SingleThingNullable = (float?)jsonObject[nameof(AllTypesModel.SingleThingNullable)];
        //    model1.DoubleThingNullable = (double?)jsonObject[nameof(AllTypesModel.DoubleThingNullable)];
        //    model1.DecimalThingNullable = (decimal?)jsonObject[nameof(AllTypesModel.DecimalThingNullable)];
        //    model1.CharThingNullable = (char?)jsonObject[nameof(AllTypesModel.CharThingNullable)];
        //    model1.DateTimeThingNullable = (DateTime?)jsonObject[nameof(AllTypesModel.DateTimeThingNullable)];
        //    model1.DateTimeOffsetThingNullable = (DateTimeOffset?)jsonObject[nameof(AllTypesModel.DateTimeOffsetThingNullable)];
        //    model1.TimeSpanThingNullable = (TimeSpan?)jsonObject[nameof(AllTypesModel.TimeSpanThingNullable)];
        //    model1.GuidThingNullable = (Guid?)jsonObject[nameof(AllTypesModel.GuidThingNullable)];

        //    model1.BooleanThingNullableNull = (bool?)jsonObject[nameof(AllTypesModel.BooleanThingNullableNull)];
        //    model1.ByteThingNullableNull = (byte?)jsonObject[nameof(AllTypesModel.ByteThingNullableNull)];
        //    model1.SByteThingNullableNull = (sbyte?)jsonObject[nameof(AllTypesModel.SByteThingNullableNull)];
        //    model1.Int16ThingNullableNull = (short?)jsonObject[nameof(AllTypesModel.Int16ThingNullableNull)];
        //    model1.UInt16ThingNullableNull = (ushort?)jsonObject[nameof(AllTypesModel.UInt16ThingNullableNull)];
        //    model1.Int32ThingNullableNull = (int?)jsonObject[nameof(AllTypesModel.Int32ThingNullableNull)];
        //    model1.UInt32ThingNullableNull = (uint?)jsonObject[nameof(AllTypesModel.UInt32ThingNullableNull)];
        //    model1.Int64ThingNullableNull = (long?)jsonObject[nameof(AllTypesModel.Int64ThingNullableNull)];
        //    model1.UInt64ThingNullableNull = (ulong?)jsonObject[nameof(AllTypesModel.UInt64ThingNullableNull)];
        //    model1.SingleThingNullableNull = (float?)jsonObject[nameof(AllTypesModel.SingleThingNullableNull)];
        //    model1.DoubleThingNullableNull = (double?)jsonObject[nameof(AllTypesModel.DoubleThingNullableNull)];
        //    model1.DecimalThingNullableNull = (decimal?)jsonObject[nameof(AllTypesModel.DecimalThingNullableNull)];
        //    model1.CharThingNullableNull = (char?)jsonObject[nameof(AllTypesModel.CharThingNullableNull)];
        //    model1.DateTimeThingNullableNull = (DateTime?)jsonObject[nameof(AllTypesModel.DateTimeThingNullableNull)];
        //    model1.DateTimeOffsetThingNullableNull = (DateTimeOffset?)jsonObject[nameof(AllTypesModel.DateTimeOffsetThingNullableNull)];
        //    model1.TimeSpanThingNullableNull = (TimeSpan?)jsonObject[nameof(AllTypesModel.TimeSpanThingNullableNull)];
        //    model1.GuidThingNullableNull = (Guid?)jsonObject[nameof(AllTypesModel.GuidThingNullableNull)];

        //    model1.StringThing = (string)jsonObject[nameof(AllTypesModel.StringThing)];
        //    model1.StringThingNull = (string)jsonObject[nameof(AllTypesModel.StringThingNull)];
        //    model1.StringThingEmpty = (string)jsonObject[nameof(AllTypesModel.StringThingEmpty)];

        //    model1.EnumThing = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThing)]);
        //    model1.EnumThingNullable = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThingNullable)]);
        //    model1.EnumThingNullableNull = ((string)jsonObject[nameof(AllTypesModel.EnumThingNullableNull)]) == null ? (EnumModel?)null : EnumModel.Item1;

        //    model1.BooleanArray = (bool[])jsonObject[nameof(AllTypesModel.BooleanArray)];
        //    model1.ByteArray = (byte[])jsonObject[nameof(AllTypesModel.ByteArray)];
        //    model1.SByteArray = (sbyte[])jsonObject[nameof(AllTypesModel.SByteArray)];
        //    model1.Int16Array = (short[])jsonObject[nameof(AllTypesModel.Int16Array)];
        //    model1.UInt16Array = (ushort[])jsonObject[nameof(AllTypesModel.UInt16Array)];
        //    model1.Int32Array = (int[])jsonObject[nameof(AllTypesModel.Int32Array)];
        //    model1.UInt32Array = (uint[])jsonObject[nameof(AllTypesModel.UInt32Array)];
        //    model1.Int64Array = (long[])jsonObject[nameof(AllTypesModel.Int64Array)];
        //    model1.UInt64Array = (ulong[])jsonObject[nameof(AllTypesModel.UInt64Array)];
        //    model1.SingleArray = (float[])jsonObject[nameof(AllTypesModel.SingleArray)];
        //    model1.DoubleArray = (double[])jsonObject[nameof(AllTypesModel.DoubleArray)];
        //    model1.DecimalArray = (decimal[])jsonObject[nameof(AllTypesModel.DecimalArray)];
        //    model1.CharArray = (char[])jsonObject[nameof(AllTypesModel.CharArray)];
        //    model1.DateTimeArray = (DateTime[])jsonObject[nameof(AllTypesModel.DateTimeArray)];
        //    model1.DateTimeOffsetArray = (DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArray)];
        //    model1.TimeSpanArray = (TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanArray)];
        //    model1.GuidArray = (Guid[])jsonObject[nameof(AllTypesModel.GuidArray)];

        //    model1.BooleanArrayNullable = (bool?[])jsonObject[nameof(AllTypesModel.BooleanArrayNullable)];
        //    model1.ByteArrayNullable = (byte?[])jsonObject[nameof(AllTypesModel.ByteArrayNullable)];
        //    model1.SByteArrayNullable = (sbyte?[])jsonObject[nameof(AllTypesModel.SByteArrayNullable)];
        //    model1.Int16ArrayNullable = (short?[])jsonObject[nameof(AllTypesModel.Int16ArrayNullable)];
        //    model1.UInt16ArrayNullable = (ushort?[])jsonObject[nameof(AllTypesModel.UInt16ArrayNullable)];
        //    model1.Int32ArrayNullable = (int?[])jsonObject[nameof(AllTypesModel.Int32ArrayNullable)];
        //    model1.UInt32ArrayNullable = (uint?[])jsonObject[nameof(AllTypesModel.UInt32ArrayNullable)];
        //    model1.Int64ArrayNullable = (long?[])jsonObject[nameof(AllTypesModel.Int64ArrayNullable)];
        //    model1.UInt64ArrayNullable = (ulong?[])jsonObject[nameof(AllTypesModel.UInt64ArrayNullable)];
        //    model1.SingleArrayNullable = (float?[])jsonObject[nameof(AllTypesModel.SingleArrayNullable)];
        //    model1.DoubleArrayNullable = (double?[])jsonObject[nameof(AllTypesModel.DoubleArrayNullable)];
        //    model1.DecimalArrayNullable = (decimal?[])jsonObject[nameof(AllTypesModel.DecimalArrayNullable)];
        //    model1.CharArrayNullable = (char?[])jsonObject[nameof(AllTypesModel.CharArrayNullable)];
        //    model1.DateTimeArrayNullable = (DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeArrayNullable)];
        //    model1.DateTimeOffsetArrayNullable = (DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetArrayNullable)];
        //    model1.TimeSpanArrayNullable = (TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanArrayNullable)];
        //    model1.GuidArrayNullable = (Guid?[])jsonObject[nameof(AllTypesModel.GuidArrayNullable)];

        //    model1.StringArray = (string[])jsonObject[nameof(AllTypesModel.StringArray)];
        //    model1.StringEmptyArray = (string[])jsonObject[nameof(AllTypesModel.StringEmptyArray)];

        //    model1.EnumArray = ((string[])jsonObject[nameof(AllTypesModel.EnumArray)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();
        //    model1.EnumArrayNullable = ((string[])jsonObject[nameof(AllTypesModel.EnumArrayNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();

        //    model1.BooleanList = ((bool[])jsonObject[nameof(AllTypesModel.BooleanList)]).ToList();
        //    model1.ByteList = ((byte[])jsonObject[nameof(AllTypesModel.ByteList)]).ToList();
        //    model1.SByteList = ((sbyte[])jsonObject[nameof(AllTypesModel.SByteList)]).ToList();
        //    model1.Int16List = ((short[])jsonObject[nameof(AllTypesModel.Int16List)]).ToList();
        //    model1.UInt16List = ((ushort[])jsonObject[nameof(AllTypesModel.UInt16List)]).ToList();
        //    model1.Int32List = ((int[])jsonObject[nameof(AllTypesModel.Int32List)]).ToList();
        //    model1.UInt32List = ((uint[])jsonObject[nameof(AllTypesModel.UInt32List)]).ToList();
        //    model1.Int64List = ((long[])jsonObject[nameof(AllTypesModel.Int64List)]).ToList();
        //    model1.UInt64List = ((ulong[])jsonObject[nameof(AllTypesModel.UInt64List)]).ToList();
        //    model1.SingleList = ((float[])jsonObject[nameof(AllTypesModel.SingleList)]).ToList();
        //    model1.DoubleList = ((double[])jsonObject[nameof(AllTypesModel.DoubleList)]).ToList();
        //    model1.DecimalList = ((decimal[])jsonObject[nameof(AllTypesModel.DecimalList)]).ToList();
        //    model1.CharList = ((char[])jsonObject[nameof(AllTypesModel.CharList)]).ToList();
        //    model1.DateTimeList = ((DateTime[])jsonObject[nameof(AllTypesModel.DateTimeList)]).ToList();
        //    model1.DateTimeOffsetList = ((DateTimeOffset[])jsonObject[nameof(AllTypesModel.DateTimeOffsetList)]).ToList();
        //    model1.TimeSpanList = ((TimeSpan[])jsonObject[nameof(AllTypesModel.TimeSpanList)]).ToList();
        //    model1.GuidList = ((Guid[])jsonObject[nameof(AllTypesModel.GuidList)]).ToList();

        //    model1.BooleanListNullable = ((bool?[])jsonObject[nameof(AllTypesModel.BooleanListNullable)]).ToList();
        //    model1.ByteListNullable = ((byte?[])jsonObject[nameof(AllTypesModel.ByteListNullable)]).ToList();
        //    model1.SByteListNullable = ((sbyte?[])jsonObject[nameof(AllTypesModel.SByteListNullable)]).ToList();
        //    model1.Int16ListNullable = ((short?[])jsonObject[nameof(AllTypesModel.Int16ListNullable)]).ToList();
        //    model1.UInt16ListNullable = ((ushort?[])jsonObject[nameof(AllTypesModel.UInt16ListNullable)]).ToList();
        //    model1.Int32ListNullable = ((int?[])jsonObject[nameof(AllTypesModel.Int32ListNullable)]).ToList();
        //    model1.UInt32ListNullable = ((uint?[])jsonObject[nameof(AllTypesModel.UInt32ListNullable)]).ToList();
        //    model1.Int64ListNullable = ((long?[])jsonObject[nameof(AllTypesModel.Int64ListNullable)]).ToList();
        //    model1.UInt64ListNullable = ((ulong?[])jsonObject[nameof(AllTypesModel.UInt64ListNullable)]).ToList();
        //    model1.SingleListNullable = ((float?[])jsonObject[nameof(AllTypesModel.SingleListNullable)]).ToList();
        //    model1.DoubleListNullable = ((double?[])jsonObject[nameof(AllTypesModel.DoubleListNullable)]).ToList();
        //    model1.DecimalListNullable = ((decimal?[])jsonObject[nameof(AllTypesModel.DecimalListNullable)]).ToList();
        //    model1.CharListNullable = ((char?[])jsonObject[nameof(AllTypesModel.CharListNullable)]).ToList();
        //    model1.DateTimeListNullable = ((DateTime?[])jsonObject[nameof(AllTypesModel.DateTimeListNullable)]).ToList();
        //    model1.DateTimeOffsetListNullable = ((DateTimeOffset?[])jsonObject[nameof(AllTypesModel.DateTimeOffsetListNullable)]).ToList();
        //    model1.TimeSpanListNullable = ((TimeSpan?[])jsonObject[nameof(AllTypesModel.TimeSpanListNullable)]).ToList();
        //    model1.GuidListNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListNullable)]).ToList();

        //    model1.StringList = ((string[])jsonObject[nameof(AllTypesModel.StringList)]).ToList();

        //    model1.EnumList = (((string[])jsonObject[nameof(AllTypesModel.EnumList)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
        //    model1.EnumListNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumListNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();

        //    var classThingJsonObject = jsonObject[nameof(AllTypesModel.ClassThing)];
        //    if (!classThingJsonObject.IsNull)
        //    {
        //        model1.ClassThing = new BasicModel();
        //        model1.ClassThing.Value = (int)classThingJsonObject["Value"];
        //    }

        //    var classThingNullJsonObject = jsonObject[nameof(AllTypesModel.ClassThingNull)];
        //    if (!classThingNullJsonObject.IsNull)
        //    {
        //        model1.ClassThingNull = new BasicModel();
        //        model1.ClassThingNull.Value = (int)classThingNullJsonObject["Value"];
        //    }


        //    var classArrayJsonObject = jsonObject[nameof(AllTypesModel.ClassArray)];
        //    var classArray = new List<BasicModel>();
        //    foreach (var item in classArrayJsonObject)
        //    {
        //        if (!item.IsNull)
        //        {
        //            var obj = new BasicModel();
        //            obj.Value = (int)item["Value"];
        //            classArray.Add(obj);
        //        }
        //        else
        //        {
        //            classArray.Add(null);
        //        }
        //    }
        //    model1.ClassArray = classArray.ToArray();

        //    var classEnumerableJsonObject = jsonObject[nameof(AllTypesModel.ClassEnumerable)];
        //    var classEnumerable = new List<BasicModel>();
        //    foreach (var item in classEnumerableJsonObject)
        //    {
        //        if (!item.IsNull)
        //        {
        //            var obj = new BasicModel();
        //            obj.Value = (int)item["Value"];
        //            classEnumerable.Add(obj);
        //        }
        //        else
        //        {
        //            classEnumerable.Add(null);
        //        }
        //    }
        //    model1.ClassEnumerable = classEnumerable;

        //    var classListJsonObject = jsonObject[nameof(AllTypesModel.ClassList)];
        //    var classList = new List<BasicModel>();
        //    foreach (var item in classListJsonObject)
        //    {
        //        if (!item.IsNull)
        //        {
        //            var obj = new BasicModel();
        //            obj.Value = (int)item["Value"];
        //            classList.Add(obj);
        //        }
        //        else
        //        {
        //            classList.Add(null);
        //        }
        //    }
        //    model1.ClassList = classList;

        //    var dictionaryThingJsonObject = jsonObject[nameof(AllTypesModel.DictionaryThing)];
        //    model1.DictionaryThing = new Dictionary<int, string>();
        //    model1.DictionaryThing.Add(1, (string)dictionaryThingJsonObject["1"]);
        //    model1.DictionaryThing.Add(2, (string)dictionaryThingJsonObject["2"]);
        //    model1.DictionaryThing.Add(3, (string)dictionaryThingJsonObject["3"]);
        //    model1.DictionaryThing.Add(4, (string)dictionaryThingJsonObject["4"]);

        //    var stringArrayOfArrayThingJsonObject = jsonObject[nameof(AllTypesModel.StringArrayOfArrayThing)];
        //    var stringList = new List<string[]>();
        //    foreach (var item in stringArrayOfArrayThingJsonObject)
        //    {
        //        if (item.IsNull)
        //        {
        //            stringList.Add(null);
        //            continue;
        //        }
        //        var stringSubList = new List<string>();
        //        foreach (var sub in item)
        //        {
        //            stringSubList.Add((string)sub);
        //        }
        //        stringList.Add(stringSubList.ToArray());
        //    }
        //    model1.StringArrayOfArrayThing = stringList.ToArray();

        //    Factory.AssertAreEqual(baseModel, model1);

        //    var model2 = jsonObject.Bind<AllTypesModel>();
        //    Factory.AssertAreEqual(baseModel, model2);
        //}

        [TestMethod]
        public async Task StreamExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<Exception>(stream);
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
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<ITestInterface>(stream);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public async Task StreamEmptyModel()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<EmptyModel>(stream);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public async Task StreamGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();
            var typeDetail = Zerra.Reflection.TypeAnalyzer.GetTypeDetail(typeof(GetsSetsModel));

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<GetsSetsModel>(stream);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public async Task StreamReducedModel()
        {
            var model1 = Factory.GetCoreTypesAlternatingModel();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var result1 = await JsonSerializer.DeserializeAsync<CoreTypesModel>(stream1);
            Factory.AssertAreEqual(model1, result1);

            var model2 = Factory.GetCoreTypesModel();
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<CoreTypesAlternatingModel>(stream2);
            Factory.AssertAreEqual(result2, model2);
        }

        [TestMethod]
        public async Task StreamLargeModel()
        {
#if DEBUG
            JsonSerializer.Testing = false;
#endif

            var models = new List<AllTypesModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(Factory.GetAllTypesModel());

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, models);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<AllTypesModel[]>(stream);

            for (var i = 0; i < models.Count; i++)
                Factory.AssertAreEqual(models[i], result[i]);

#if DEBUG
            JsonSerializer.Testing = true;
#endif
        }

        [TestMethod]
        public async Task StreamRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<RecordModel>(stream);
            //Assert.IsNotNull(model);
            //Assert.AreEqual(baseModel.Property1, model.Property1);
            //Assert.AreEqual(baseModel.Property2, model.Property2);
            //Assert.AreEqual(baseModel.Property3, model.Property3);
        }

        [TestMethod]
        public async Task StreamHashSet()
        {
            var model1 = Factory.GetHashSetModel();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<HashSetModel>(stream);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamBoxing()
        {
            var baseModel = Factory.GetBoxingModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TestBoxingModel>(stream);
        }

        [TestMethod]
        public async Task StreamPropertyName()
        {
            var baseModel = new JsonNameTestModel()
            {
                _1_Property = 5,
                property2 = 7,
                _3_Property = new BasicModel()
                {
                    Value1 = 10,
                    Value2 = "11"
                }
            };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            Assert.IsTrue(json.Contains("\"1property\""));
            Assert.IsTrue(json.Contains("\"property2\""));
            Assert.IsTrue(json.Contains("\"3property\""));

            json.Replace("\"property2\"", "\"PROPERTY2\"");

            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializer.DeserializeAsync<JsonNameTestModel>(stream2);
            Assert.AreEqual(baseModel._1_Property, model._1_Property);
            Assert.AreEqual(baseModel.property2, model.property2);
            Assert.IsNotNull(model._3_Property);
            Assert.AreEqual(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.AreEqual(baseModel._3_Property.Value2, model._3_Property.Value2);
        }
    }
}
