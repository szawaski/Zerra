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
            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, 
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            var model1 = JsonSerializerOld.Deserialize<TypesAllModel>(json2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json1, 
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

            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel, options);

            Assert.IsTrue(json1 == json2);

            //swap serializers
            var model1 = JsonSerializerOld.Deserialize<TypesAllModel>(json2);
            var model2 = System.Text.Json.JsonSerializer.Deserialize<TypesAllModel>(json1, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypes()
        {
            var baseModel = TypesAllModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var model = JsonSerializerOld.Deserialize<TypesAllModel>(json);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringEnumAsNumbers()
        {
            var options = new JsonSerializerOptionsOld()
            {
                EnumAsNumber = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));
            var model = JsonSerializerOld.Deserialize<TypesAllModel>(json, options);
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
            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializerOld.Serialize(baseModel);
            var model1 = JsonSerializerOld.Deserialize<TypesAllAsStringsModel>(json1);
            TypesAllAsStringsModel.AreEqual(baseModel, model1);

            var json2 = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<TypesAllModel>(json2);
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
            var baseModel = TypesAllModel.Create();
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
            var model = JsonSerializerOld.Deserialize<TypesAllModel>(jsonPretty);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringNameless()
        {
            var options = new JsonSerializerOptionsOld()
            {
                Nameless = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            var model = JsonSerializerOld.Deserialize<TypesAllModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public void StringDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptionsOld()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel, options);
            var model = JsonSerializerOld.Deserialize<TypesAllModel>(json, options);
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
            var baseModel = TypesAllModel.Create();
            var json = JsonSerializerOld.Serialize(baseModel);
            var jsonObject = JsonSerializerOld.DeserializeJsonObject(json);

            var json2 = jsonObject.ToString();

            Assert.AreEqual(json, json2);

            var model1 = new TypesAllModel();

            model1.BooleanThing = (bool)jsonObject[nameof(TypesAllModel.BooleanThing)];
            model1.ByteThing = (byte)jsonObject[nameof(TypesAllModel.ByteThing)];
            model1.SByteThing = (sbyte)jsonObject[nameof(TypesAllModel.SByteThing)];
            model1.Int16Thing = (short)jsonObject[nameof(TypesAllModel.Int16Thing)];
            model1.UInt16Thing = (ushort)jsonObject[nameof(TypesAllModel.UInt16Thing)];
            model1.Int32Thing = (int)jsonObject[nameof(TypesAllModel.Int32Thing)];
            model1.UInt32Thing = (uint)jsonObject[nameof(TypesAllModel.UInt32Thing)];
            model1.Int64Thing = (long)jsonObject[nameof(TypesAllModel.Int64Thing)];
            model1.UInt64Thing = (ulong)jsonObject[nameof(TypesAllModel.UInt64Thing)];
            model1.SingleThing = (float)jsonObject[nameof(TypesAllModel.SingleThing)];
            model1.DoubleThing = (double)jsonObject[nameof(TypesAllModel.DoubleThing)];
            model1.DecimalThing = (decimal)jsonObject[nameof(TypesAllModel.DecimalThing)];
            model1.CharThing = (char)jsonObject[nameof(TypesAllModel.CharThing)];
            model1.DateTimeThing = (DateTime)jsonObject[nameof(TypesAllModel.DateTimeThing)];
            model1.DateTimeOffsetThing = (DateTimeOffset)jsonObject[nameof(TypesAllModel.DateTimeOffsetThing)];
            model1.TimeSpanThing = (TimeSpan)jsonObject[nameof(TypesAllModel.TimeSpanThing)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThing = (DateOnly)jsonObject[nameof(TypesAllModel.DateOnlyThing)];
            model1.TimeOnlyThing = (TimeOnly)jsonObject[nameof(TypesAllModel.TimeOnlyThing)];
#endif
            model1.GuidThing = (Guid)jsonObject[nameof(TypesAllModel.GuidThing)];

            model1.BooleanThingNullable = (bool?)jsonObject[nameof(TypesAllModel.BooleanThingNullable)];
            model1.ByteThingNullable = (byte?)jsonObject[nameof(TypesAllModel.ByteThingNullable)];
            model1.SByteThingNullable = (sbyte?)jsonObject[nameof(TypesAllModel.SByteThingNullable)];
            model1.Int16ThingNullable = (short?)jsonObject[nameof(TypesAllModel.Int16ThingNullable)];
            model1.UInt16ThingNullable = (ushort?)jsonObject[nameof(TypesAllModel.UInt16ThingNullable)];
            model1.Int32ThingNullable = (int?)jsonObject[nameof(TypesAllModel.Int32ThingNullable)];
            model1.UInt32ThingNullable = (uint?)jsonObject[nameof(TypesAllModel.UInt32ThingNullable)];
            model1.Int64ThingNullable = (long?)jsonObject[nameof(TypesAllModel.Int64ThingNullable)];
            model1.UInt64ThingNullable = (ulong?)jsonObject[nameof(TypesAllModel.UInt64ThingNullable)];
            model1.SingleThingNullable = (float?)jsonObject[nameof(TypesAllModel.SingleThingNullable)];
            model1.DoubleThingNullable = (double?)jsonObject[nameof(TypesAllModel.DoubleThingNullable)];
            model1.DecimalThingNullable = (decimal?)jsonObject[nameof(TypesAllModel.DecimalThingNullable)];
            model1.CharThingNullable = (char?)jsonObject[nameof(TypesAllModel.CharThingNullable)];
            model1.DateTimeThingNullable = (DateTime?)jsonObject[nameof(TypesAllModel.DateTimeThingNullable)];
            model1.DateTimeOffsetThingNullable = (DateTimeOffset?)jsonObject[nameof(TypesAllModel.DateTimeOffsetThingNullable)];
            model1.TimeSpanThingNullable = (TimeSpan?)jsonObject[nameof(TypesAllModel.TimeSpanThingNullable)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThingNullable = (DateOnly?)jsonObject[nameof(TypesAllModel.DateOnlyThingNullable)];
            model1.TimeOnlyThingNullable = (TimeOnly?)jsonObject[nameof(TypesAllModel.TimeOnlyThingNullable)];
#endif
            model1.GuidThingNullable = (Guid?)jsonObject[nameof(TypesAllModel.GuidThingNullable)];

            model1.BooleanThingNullableNull = (bool?)jsonObject[nameof(TypesAllModel.BooleanThingNullableNull)];
            model1.ByteThingNullableNull = (byte?)jsonObject[nameof(TypesAllModel.ByteThingNullableNull)];
            model1.SByteThingNullableNull = (sbyte?)jsonObject[nameof(TypesAllModel.SByteThingNullableNull)];
            model1.Int16ThingNullableNull = (short?)jsonObject[nameof(TypesAllModel.Int16ThingNullableNull)];
            model1.UInt16ThingNullableNull = (ushort?)jsonObject[nameof(TypesAllModel.UInt16ThingNullableNull)];
            model1.Int32ThingNullableNull = (int?)jsonObject[nameof(TypesAllModel.Int32ThingNullableNull)];
            model1.UInt32ThingNullableNull = (uint?)jsonObject[nameof(TypesAllModel.UInt32ThingNullableNull)];
            model1.Int64ThingNullableNull = (long?)jsonObject[nameof(TypesAllModel.Int64ThingNullableNull)];
            model1.UInt64ThingNullableNull = (ulong?)jsonObject[nameof(TypesAllModel.UInt64ThingNullableNull)];
            model1.SingleThingNullableNull = (float?)jsonObject[nameof(TypesAllModel.SingleThingNullableNull)];
            model1.DoubleThingNullableNull = (double?)jsonObject[nameof(TypesAllModel.DoubleThingNullableNull)];
            model1.DecimalThingNullableNull = (decimal?)jsonObject[nameof(TypesAllModel.DecimalThingNullableNull)];
            model1.CharThingNullableNull = (char?)jsonObject[nameof(TypesAllModel.CharThingNullableNull)];
            model1.DateTimeThingNullableNull = (DateTime?)jsonObject[nameof(TypesAllModel.DateTimeThingNullableNull)];
            model1.DateTimeOffsetThingNullableNull = (DateTimeOffset?)jsonObject[nameof(TypesAllModel.DateTimeOffsetThingNullableNull)];
            model1.TimeSpanThingNullableNull = (TimeSpan?)jsonObject[nameof(TypesAllModel.TimeSpanThingNullableNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyThingNullableNull = (DateOnly?)jsonObject[nameof(TypesAllModel.DateOnlyThingNullableNull)];
            model1.TimeOnlyThingNullableNull = (TimeOnly?)jsonObject[nameof(TypesAllModel.TimeOnlyThingNullableNull)];
#endif
            model1.GuidThingNullableNull = (Guid?)jsonObject[nameof(TypesAllModel.GuidThingNullableNull)];

            model1.StringThing = (string)jsonObject[nameof(TypesAllModel.StringThing)];
            model1.StringThingNull = (string)jsonObject[nameof(TypesAllModel.StringThingNull)];
            model1.StringThingEmpty = (string)jsonObject[nameof(TypesAllModel.StringThingEmpty)];

            model1.EnumThing = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(TypesAllModel.EnumThing)]);
            model1.EnumThingNullable = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(TypesAllModel.EnumThingNullable)]);
            model1.EnumThingNullableNull = ((string)jsonObject[nameof(TypesAllModel.EnumThingNullableNull)]) == null ? (EnumModel?)null : EnumModel.EnumItem1;

            model1.BooleanArray = (bool[])jsonObject[nameof(TypesAllModel.BooleanArray)];
            model1.ByteArray = (byte[])jsonObject[nameof(TypesAllModel.ByteArray)];
            model1.SByteArray = (sbyte[])jsonObject[nameof(TypesAllModel.SByteArray)];
            model1.Int16Array = (short[])jsonObject[nameof(TypesAllModel.Int16Array)];
            model1.UInt16Array = (ushort[])jsonObject[nameof(TypesAllModel.UInt16Array)];
            model1.Int32Array = (int[])jsonObject[nameof(TypesAllModel.Int32Array)];
            model1.UInt32Array = (uint[])jsonObject[nameof(TypesAllModel.UInt32Array)];
            model1.Int64Array = (long[])jsonObject[nameof(TypesAllModel.Int64Array)];
            model1.UInt64Array = (ulong[])jsonObject[nameof(TypesAllModel.UInt64Array)];
            model1.SingleArray = (float[])jsonObject[nameof(TypesAllModel.SingleArray)];
            model1.DoubleArray = (double[])jsonObject[nameof(TypesAllModel.DoubleArray)];
            model1.DecimalArray = (decimal[])jsonObject[nameof(TypesAllModel.DecimalArray)];
            model1.CharArray = (char[])jsonObject[nameof(TypesAllModel.CharArray)];
            model1.DateTimeArray = (DateTime[])jsonObject[nameof(TypesAllModel.DateTimeArray)];
            model1.DateTimeOffsetArray = (DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArray)];
            model1.TimeSpanArray = (TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanArray)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArray = (DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyArray)];
            model1.TimeOnlyArray = (TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyArray)];
#endif
            model1.GuidArray = (Guid[])jsonObject[nameof(TypesAllModel.GuidArray)];

            model1.BooleanArrayEmpty = (bool[])jsonObject[nameof(TypesAllModel.BooleanArrayEmpty)];
            model1.ByteArrayEmpty = (byte[])jsonObject[nameof(TypesAllModel.ByteArrayEmpty)];
            model1.SByteArrayEmpty = (sbyte[])jsonObject[nameof(TypesAllModel.SByteArrayEmpty)];
            model1.Int16ArrayEmpty = (short[])jsonObject[nameof(TypesAllModel.Int16ArrayEmpty)];
            model1.UInt16ArrayEmpty = (ushort[])jsonObject[nameof(TypesAllModel.UInt16ArrayEmpty)];
            model1.Int32ArrayEmpty = (int[])jsonObject[nameof(TypesAllModel.Int32ArrayEmpty)];
            model1.UInt32ArrayEmpty = (uint[])jsonObject[nameof(TypesAllModel.UInt32ArrayEmpty)];
            model1.Int64ArrayEmpty = (long[])jsonObject[nameof(TypesAllModel.Int64ArrayEmpty)];
            model1.UInt64ArrayEmpty = (ulong[])jsonObject[nameof(TypesAllModel.UInt64ArrayEmpty)];
            model1.SingleArrayEmpty = (float[])jsonObject[nameof(TypesAllModel.SingleArrayEmpty)];
            model1.DoubleArrayEmpty = (double[])jsonObject[nameof(TypesAllModel.DoubleArrayEmpty)];
            model1.DecimalArrayEmpty = (decimal[])jsonObject[nameof(TypesAllModel.DecimalArrayEmpty)];
            model1.CharArrayEmpty = (char[])jsonObject[nameof(TypesAllModel.CharArrayEmpty)];
            model1.DateTimeArrayEmpty = (DateTime[])jsonObject[nameof(TypesAllModel.DateTimeArrayEmpty)];
            model1.DateTimeOffsetArrayEmpty = (DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArrayEmpty)];
            model1.TimeSpanArrayEmpty = (TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanArrayEmpty)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayEmpty = (DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyArrayEmpty)];
            model1.TimeOnlyArrayEmpty = (TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyArrayEmpty)];
#endif
            model1.GuidArrayEmpty = (Guid[])jsonObject[nameof(TypesAllModel.GuidArrayEmpty)];

            model1.BooleanArrayNull = (bool[])jsonObject[nameof(TypesAllModel.BooleanArrayNull)];
            model1.ByteArrayNull = (byte[])jsonObject[nameof(TypesAllModel.ByteArrayNull)];
            model1.SByteArrayNull = (sbyte[])jsonObject[nameof(TypesAllModel.SByteArrayNull)];
            model1.Int16ArrayNull = (short[])jsonObject[nameof(TypesAllModel.Int16ArrayNull)];
            model1.UInt16ArrayNull = (ushort[])jsonObject[nameof(TypesAllModel.UInt16ArrayNull)];
            model1.Int32ArrayNull = (int[])jsonObject[nameof(TypesAllModel.Int32ArrayNull)];
            model1.UInt32ArrayNull = (uint[])jsonObject[nameof(TypesAllModel.UInt32ArrayNull)];
            model1.Int64ArrayNull = (long[])jsonObject[nameof(TypesAllModel.Int64ArrayNull)];
            model1.UInt64ArrayNull = (ulong[])jsonObject[nameof(TypesAllModel.UInt64ArrayNull)];
            model1.SingleArrayNull = (float[])jsonObject[nameof(TypesAllModel.SingleArrayNull)];
            model1.DoubleArrayNull = (double[])jsonObject[nameof(TypesAllModel.DoubleArrayNull)];
            model1.DecimalArrayNull = (decimal[])jsonObject[nameof(TypesAllModel.DecimalArrayNull)];
            model1.CharArrayNull = (char[])jsonObject[nameof(TypesAllModel.CharArrayNull)];
            model1.DateTimeArrayNull = (DateTime[])jsonObject[nameof(TypesAllModel.DateTimeArrayNull)];
            model1.DateTimeOffsetArrayNull = (DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArrayNull)];
            model1.TimeSpanArrayNull = (TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanArrayNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNull = (DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyArrayNull)];
            model1.TimeOnlyArrayNull = (TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyArrayNull)];
#endif
            model1.GuidArrayNull = (Guid[])jsonObject[nameof(TypesAllModel.GuidArrayNull)];

            model1.BooleanArrayNullable = (bool?[])jsonObject[nameof(TypesAllModel.BooleanArrayNullable)];
            model1.ByteArrayNullable = (byte?[])jsonObject[nameof(TypesAllModel.ByteArrayNullable)];
            model1.SByteArrayNullable = (sbyte?[])jsonObject[nameof(TypesAllModel.SByteArrayNullable)];
            model1.Int16ArrayNullable = (short?[])jsonObject[nameof(TypesAllModel.Int16ArrayNullable)];
            model1.UInt16ArrayNullable = (ushort?[])jsonObject[nameof(TypesAllModel.UInt16ArrayNullable)];
            model1.Int32ArrayNullable = (int?[])jsonObject[nameof(TypesAllModel.Int32ArrayNullable)];
            model1.UInt32ArrayNullable = (uint?[])jsonObject[nameof(TypesAllModel.UInt32ArrayNullable)];
            model1.Int64ArrayNullable = (long?[])jsonObject[nameof(TypesAllModel.Int64ArrayNullable)];
            model1.UInt64ArrayNullable = (ulong?[])jsonObject[nameof(TypesAllModel.UInt64ArrayNullable)];
            model1.SingleArrayNullable = (float?[])jsonObject[nameof(TypesAllModel.SingleArrayNullable)];
            model1.DoubleArrayNullable = (double?[])jsonObject[nameof(TypesAllModel.DoubleArrayNullable)];
            model1.DecimalArrayNullable = (decimal?[])jsonObject[nameof(TypesAllModel.DecimalArrayNullable)];
            model1.CharArrayNullable = (char?[])jsonObject[nameof(TypesAllModel.CharArrayNullable)];
            model1.DateTimeArrayNullable = (DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeArrayNullable)];
            model1.DateTimeOffsetArrayNullable = (DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArrayNullable)];
            model1.TimeSpanArrayNullable = (TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanArrayNullable)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullable = (DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyArrayNullable)];
            model1.TimeOnlyArrayNullable = (TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyArrayNullable)];
#endif
            model1.GuidArrayNullable = (Guid?[])jsonObject[nameof(TypesAllModel.GuidArrayNullable)];

            model1.BooleanArrayNullableEmpty = (bool?[])jsonObject[nameof(TypesAllModel.BooleanArrayNullableEmpty)];
            model1.ByteArrayNullableEmpty = (byte?[])jsonObject[nameof(TypesAllModel.ByteArrayNullableEmpty)];
            model1.SByteArrayNullableEmpty = (sbyte?[])jsonObject[nameof(TypesAllModel.SByteArrayNullableEmpty)];
            model1.Int16ArrayNullableEmpty = (short?[])jsonObject[nameof(TypesAllModel.Int16ArrayNullableEmpty)];
            model1.UInt16ArrayNullableEmpty = (ushort?[])jsonObject[nameof(TypesAllModel.UInt16ArrayNullableEmpty)];
            model1.Int32ArrayNullableEmpty = (int?[])jsonObject[nameof(TypesAllModel.Int32ArrayNullableEmpty)];
            model1.UInt32ArrayNullableEmpty = (uint?[])jsonObject[nameof(TypesAllModel.UInt32ArrayNullableEmpty)];
            model1.Int64ArrayNullableEmpty = (long?[])jsonObject[nameof(TypesAllModel.Int64ArrayNullableEmpty)];
            model1.UInt64ArrayNullableEmpty = (ulong?[])jsonObject[nameof(TypesAllModel.UInt64ArrayNullableEmpty)];
            model1.SingleArrayNullableEmpty = (float?[])jsonObject[nameof(TypesAllModel.SingleArrayNullableEmpty)];
            model1.DoubleArrayNullableEmpty = (double?[])jsonObject[nameof(TypesAllModel.DoubleArrayNullableEmpty)];
            model1.DecimalArrayNullableEmpty = (decimal?[])jsonObject[nameof(TypesAllModel.DecimalArrayNullableEmpty)];
            model1.CharArrayNullableEmpty = (char?[])jsonObject[nameof(TypesAllModel.CharArrayNullableEmpty)];
            model1.DateTimeArrayNullableEmpty = (DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeArrayNullableEmpty)];
            model1.DateTimeOffsetArrayNullableEmpty = (DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArrayNullableEmpty)];
            model1.TimeSpanArrayNullableEmpty = (TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanArrayNullableEmpty)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullableEmpty = (DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyArrayNullableEmpty)];
            model1.TimeOnlyArrayNullableEmpty = (TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyArrayNullableEmpty)];
#endif
            model1.GuidArrayNullableEmpty = (Guid?[])jsonObject[nameof(TypesAllModel.GuidArrayNullableEmpty)];

            model1.BooleanArrayNullableNull = (bool?[])jsonObject[nameof(TypesAllModel.BooleanArrayNullableNull)];
            model1.ByteArrayNullableNull = (byte?[])jsonObject[nameof(TypesAllModel.ByteArrayNullableNull)];
            model1.SByteArrayNullableNull = (sbyte?[])jsonObject[nameof(TypesAllModel.SByteArrayNullableNull)];
            model1.Int16ArrayNullableNull = (short?[])jsonObject[nameof(TypesAllModel.Int16ArrayNullableNull)];
            model1.UInt16ArrayNullableNull = (ushort?[])jsonObject[nameof(TypesAllModel.UInt16ArrayNullableNull)];
            model1.Int32ArrayNullableNull = (int?[])jsonObject[nameof(TypesAllModel.Int32ArrayNullableNull)];
            model1.UInt32ArrayNullableNull = (uint?[])jsonObject[nameof(TypesAllModel.UInt32ArrayNullableNull)];
            model1.Int64ArrayNullableNull = (long?[])jsonObject[nameof(TypesAllModel.Int64ArrayNullableNull)];
            model1.UInt64ArrayNullableNull = (ulong?[])jsonObject[nameof(TypesAllModel.UInt64ArrayNullableNull)];
            model1.SingleArrayNullableNull = (float?[])jsonObject[nameof(TypesAllModel.SingleArrayNullableNull)];
            model1.DoubleArrayNullableNull = (double?[])jsonObject[nameof(TypesAllModel.DoubleArrayNullableNull)];
            model1.DecimalArrayNullableNull = (decimal?[])jsonObject[nameof(TypesAllModel.DecimalArrayNullableNull)];
            model1.CharArrayNullableNull = (char?[])jsonObject[nameof(TypesAllModel.CharArrayNullableNull)];
            model1.DateTimeArrayNullableNull = (DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeArrayNullableNull)];
            model1.DateTimeOffsetArrayNullableNull = (DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetArrayNullableNull)];
            model1.TimeSpanArrayNullableNull = (TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanArrayNullableNull)];
#if NET6_0_OR_GREATER
            model1.DateOnlyArrayNullableNull = (DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyArrayNullableNull)];
            model1.TimeOnlyArrayNullableNull = (TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyArrayNullableNull)];
#endif
            model1.GuidArrayNullableNull = (Guid?[])jsonObject[nameof(TypesAllModel.GuidArrayNullableNull)];

            model1.StringArray = (string[])jsonObject[nameof(TypesAllModel.StringArray)];
            model1.StringArrayEmpty = (string[])jsonObject[nameof(TypesAllModel.StringArrayEmpty)];

            model1.EnumArray = ((string[])jsonObject[nameof(TypesAllModel.EnumArray)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();
            model1.EnumArrayNullable = ((string[])jsonObject[nameof(TypesAllModel.EnumArrayNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray();

            model1.BooleanListT = ((bool[])jsonObject[nameof(TypesAllModel.BooleanListT)]).ToList();
            model1.ByteListT = ((byte[])jsonObject[nameof(TypesAllModel.ByteListT)]).ToList();
            model1.SByteListT = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteListT)]).ToList();
            model1.Int16ListT = ((short[])jsonObject[nameof(TypesAllModel.Int16ListT)]).ToList();
            model1.UInt16ListT = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ListT)]).ToList();
            model1.Int32ListT = ((int[])jsonObject[nameof(TypesAllModel.Int32ListT)]).ToList();
            model1.UInt32ListT = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ListT)]).ToList();
            model1.Int64ListT = ((long[])jsonObject[nameof(TypesAllModel.Int64ListT)]).ToList();
            model1.UInt64ListT = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ListT)]).ToList();
            model1.SingleListT = ((float[])jsonObject[nameof(TypesAllModel.SingleListT)]).ToList();
            model1.DoubleListT = ((double[])jsonObject[nameof(TypesAllModel.DoubleListT)]).ToList();
            model1.DecimalListT = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalListT)]).ToList();
            model1.CharListT = ((char[])jsonObject[nameof(TypesAllModel.CharListT)]).ToList();
            model1.DateTimeListT = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeListT)]).ToList();
            model1.DateTimeOffsetListT = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListT)]).ToList();
            model1.TimeSpanListT = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListT = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyListT)]).ToList();
            model1.TimeOnlyListT = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyListT)]).ToList();
#endif
            model1.GuidListT = ((Guid[])jsonObject[nameof(TypesAllModel.GuidListT)]).ToList();

            model1.BooleanListTEmpty = ((bool[])jsonObject[nameof(TypesAllModel.BooleanListTEmpty)]).ToList();
            model1.ByteListTEmpty = ((byte[])jsonObject[nameof(TypesAllModel.ByteListTEmpty)]).ToList();
            model1.SByteListTEmpty = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteListTEmpty)]).ToList();
            model1.Int16ListTEmpty = ((short[])jsonObject[nameof(TypesAllModel.Int16ListTEmpty)]).ToList();
            model1.UInt16ListTEmpty = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ListTEmpty)]).ToList();
            model1.Int32ListTEmpty = ((int[])jsonObject[nameof(TypesAllModel.Int32ListTEmpty)]).ToList();
            model1.UInt32ListTEmpty = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ListTEmpty)]).ToList();
            model1.Int64ListTEmpty = ((long[])jsonObject[nameof(TypesAllModel.Int64ListTEmpty)]).ToList();
            model1.UInt64ListTEmpty = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ListTEmpty)]).ToList();
            model1.SingleListTEmpty = ((float[])jsonObject[nameof(TypesAllModel.SingleListTEmpty)]).ToList();
            model1.DoubleListTEmpty = ((double[])jsonObject[nameof(TypesAllModel.DoubleListTEmpty)]).ToList();
            model1.DecimalListTEmpty = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalListTEmpty)]).ToList();
            model1.CharListTEmpty = ((char[])jsonObject[nameof(TypesAllModel.CharListTEmpty)]).ToList();
            model1.DateTimeListTEmpty = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeListTEmpty)]).ToList();
            model1.DateTimeOffsetListTEmpty = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListTEmpty)]).ToList();
            model1.TimeSpanListTEmpty = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTEmpty = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyListTEmpty)]).ToList();
            model1.TimeOnlyListTEmpty = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyListTEmpty)]).ToList();
#endif
            model1.GuidListTEmpty = ((Guid[])jsonObject[nameof(TypesAllModel.GuidListTEmpty)]).ToList();

            model1.BooleanListTNull = ((bool[])jsonObject[nameof(TypesAllModel.BooleanListTNull)])?.ToList();
            model1.ByteListTNull = ((byte[])jsonObject[nameof(TypesAllModel.ByteListTNull)])?.ToList();
            model1.SByteListTNull = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteListTNull)])?.ToList();
            model1.Int16ListTNull = ((short[])jsonObject[nameof(TypesAllModel.Int16ListTNull)])?.ToList();
            model1.UInt16ListTNull = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ListTNull)])?.ToList();
            model1.Int32ListTNull = ((int[])jsonObject[nameof(TypesAllModel.Int32ListTNull)])?.ToList();
            model1.UInt32ListTNull = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ListTNull)])?.ToList();
            model1.Int64ListTNull = ((long[])jsonObject[nameof(TypesAllModel.Int64ListTNull)])?.ToList();
            model1.UInt64ListTNull = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ListTNull)])?.ToList();
            model1.SingleListTNull = ((float[])jsonObject[nameof(TypesAllModel.SingleListTNull)])?.ToList();
            model1.DoubleListTNull = ((double[])jsonObject[nameof(TypesAllModel.DoubleListTNull)])?.ToList();
            model1.DecimalListTNull = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalListTNull)])?.ToList();
            model1.CharListTNull = ((char[])jsonObject[nameof(TypesAllModel.CharListTNull)])?.ToList();
            model1.DateTimeListTNull = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeListTNull)])?.ToList();
            model1.DateTimeOffsetListTNull = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListTNull)])?.ToList();
            model1.TimeSpanListTNull = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNull = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyListTNull)])?.ToList();
            model1.TimeOnlyListTNull = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyListTNull)])?.ToList();
#endif
            model1.GuidListTNull = ((Guid[])jsonObject[nameof(TypesAllModel.GuidListTNull)])?.ToList();

            model1.BooleanListTNullable = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanListTNullable)]).ToList();
            model1.ByteListTNullable = ((byte?[])jsonObject[nameof(TypesAllModel.ByteListTNullable)]).ToList();
            model1.SByteListTNullable = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteListTNullable)]).ToList();
            model1.Int16ListTNullable = ((short?[])jsonObject[nameof(TypesAllModel.Int16ListTNullable)]).ToList();
            model1.UInt16ListTNullable = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ListTNullable)]).ToList();
            model1.Int32ListTNullable = ((int?[])jsonObject[nameof(TypesAllModel.Int32ListTNullable)]).ToList();
            model1.UInt32ListTNullable = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ListTNullable)]).ToList();
            model1.Int64ListTNullable = ((long?[])jsonObject[nameof(TypesAllModel.Int64ListTNullable)]).ToList();
            model1.UInt64ListTNullable = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ListTNullable)]).ToList();
            model1.SingleListTNullable = ((float?[])jsonObject[nameof(TypesAllModel.SingleListTNullable)]).ToList();
            model1.DoubleListTNullable = ((double?[])jsonObject[nameof(TypesAllModel.DoubleListTNullable)]).ToList();
            model1.DecimalListTNullable = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalListTNullable)]).ToList();
            model1.CharListTNullable = ((char?[])jsonObject[nameof(TypesAllModel.CharListTNullable)]).ToList();
            model1.DateTimeListTNullable = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeListTNullable)]).ToList();
            model1.DateTimeOffsetListTNullable = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListTNullable)]).ToList();
            model1.TimeSpanListTNullable = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullable = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyListTNullable)]).ToList();
            model1.TimeOnlyListTNullable = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyListTNullable)]).ToList();
#endif
            model1.GuidListTNullable = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidListTNullable)]).ToList();

            model1.BooleanListTNullableEmpty = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanListTNullableEmpty)]).ToList();
            model1.ByteListTNullableEmpty = ((byte?[])jsonObject[nameof(TypesAllModel.ByteListTNullableEmpty)]).ToList();
            model1.SByteListTNullableEmpty = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteListTNullableEmpty)]).ToList();
            model1.Int16ListTNullableEmpty = ((short?[])jsonObject[nameof(TypesAllModel.Int16ListTNullableEmpty)]).ToList();
            model1.UInt16ListTNullableEmpty = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ListTNullableEmpty)]).ToList();
            model1.Int32ListTNullableEmpty = ((int?[])jsonObject[nameof(TypesAllModel.Int32ListTNullableEmpty)]).ToList();
            model1.UInt32ListTNullableEmpty = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ListTNullableEmpty)]).ToList();
            model1.Int64ListTNullableEmpty = ((long?[])jsonObject[nameof(TypesAllModel.Int64ListTNullableEmpty)]).ToList();
            model1.UInt64ListTNullableEmpty = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ListTNullableEmpty)]).ToList();
            model1.SingleListTNullableEmpty = ((float?[])jsonObject[nameof(TypesAllModel.SingleListTNullableEmpty)]).ToList();
            model1.DoubleListTNullableEmpty = ((double?[])jsonObject[nameof(TypesAllModel.DoubleListTNullableEmpty)]).ToList();
            model1.DecimalListTNullableEmpty = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalListTNullableEmpty)]).ToList();
            model1.CharListTNullableEmpty = ((char?[])jsonObject[nameof(TypesAllModel.CharListTNullableEmpty)]).ToList();
            model1.DateTimeListTNullableEmpty = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListTNullableEmpty)]).ToList();
            model1.TimeSpanListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyListTNullableEmpty)]).ToList();
            model1.TimeOnlyListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyListTNullableEmpty)]).ToList();
#endif
            model1.GuidListTNullableEmpty = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidListTNullableEmpty)]).ToList();

            model1.BooleanListTNullableNull = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanListTNullableNull)])?.ToList();
            model1.ByteListTNullableNull = ((byte?[])jsonObject[nameof(TypesAllModel.ByteListTNullableNull)])?.ToList();
            model1.SByteListTNullableNull = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteListTNullableNull)])?.ToList();
            model1.Int16ListTNullableNull = ((short?[])jsonObject[nameof(TypesAllModel.Int16ListTNullableNull)])?.ToList();
            model1.UInt16ListTNullableNull = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ListTNullableNull)])?.ToList();
            model1.Int32ListTNullableNull = ((int?[])jsonObject[nameof(TypesAllModel.Int32ListTNullableNull)])?.ToList();
            model1.UInt32ListTNullableNull = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ListTNullableNull)])?.ToList();
            model1.Int64ListTNullableNull = ((long?[])jsonObject[nameof(TypesAllModel.Int64ListTNullableNull)])?.ToList();
            model1.UInt64ListTNullableNull = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ListTNullableNull)])?.ToList();
            model1.SingleListTNullableNull = ((float?[])jsonObject[nameof(TypesAllModel.SingleListTNullableNull)])?.ToList();
            model1.DoubleListTNullableNull = ((double?[])jsonObject[nameof(TypesAllModel.DoubleListTNullableNull)])?.ToList();
            model1.DecimalListTNullableNull = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalListTNullableNull)])?.ToList();
            model1.CharListTNullableNull = ((char?[])jsonObject[nameof(TypesAllModel.CharListTNullableNull)])?.ToList();
            model1.DateTimeListTNullableNull = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeListTNullableNull)])?.ToList();
            model1.DateTimeOffsetListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetListTNullableNull)])?.ToList();
            model1.TimeSpanListTNullableNull = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyListTNullableNull = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyListTNullableNull)])?.ToList();
            model1.TimeOnlyListTNullableNull = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyListTNullableNull)])?.ToList();
#endif
            model1.GuidListTNullableNull = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidListTNullableNull)])?.ToList();

            model1.StringListT = ((string[])jsonObject[nameof(TypesAllModel.StringListT)]).ToList();
            model1.StringListTEmpty = ((string[])jsonObject[nameof(TypesAllModel.StringListTEmpty)]).ToList();
            model1.StringListTNull = ((string[])jsonObject[nameof(TypesAllModel.StringListTNull)])?.ToList();

            model1.EnumListT = (((string[])jsonObject[nameof(TypesAllModel.EnumListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListTEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListTNull = (((string[])jsonObject[nameof(TypesAllModel.EnumListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumListTNullable = (((string[])jsonObject[nameof(TypesAllModel.EnumListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListTNullableEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumListTNullableNull = (((string[])jsonObject[nameof(TypesAllModel.EnumListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIListT = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIListT)]).ToList();
            model1.ByteIListT = ((byte[])jsonObject[nameof(TypesAllModel.ByteIListT)]).ToList();
            model1.SByteIListT = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIListT)]).ToList();
            model1.Int16IListT = ((short[])jsonObject[nameof(TypesAllModel.Int16IListT)]).ToList();
            model1.UInt16IListT = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IListT)]).ToList();
            model1.Int32IListT = ((int[])jsonObject[nameof(TypesAllModel.Int32IListT)]).ToList();
            model1.UInt32IListT = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IListT)]).ToList();
            model1.Int64IListT = ((long[])jsonObject[nameof(TypesAllModel.Int64IListT)]).ToList();
            model1.UInt64IListT = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IListT)]).ToList();
            model1.SingleIListT = ((float[])jsonObject[nameof(TypesAllModel.SingleIListT)]).ToList();
            model1.DoubleIListT = ((double[])jsonObject[nameof(TypesAllModel.DoubleIListT)]).ToList();
            model1.DecimalIListT = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIListT)]).ToList();
            model1.CharIListT = ((char[])jsonObject[nameof(TypesAllModel.CharIListT)]).ToList();
            model1.DateTimeIListT = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIListT)]).ToList();
            model1.DateTimeOffsetIListT = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListT)]).ToList();
            model1.TimeSpanIListT = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListT = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIListT)]).ToList();
            model1.TimeOnlyIListT = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIListT)]).ToList();
#endif
            model1.GuidIListT = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIListT)]).ToList();

            model1.BooleanIListTEmpty = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIListTEmpty)]).ToList();
            model1.ByteIListTEmpty = ((byte[])jsonObject[nameof(TypesAllModel.ByteIListTEmpty)]).ToList();
            model1.SByteIListTEmpty = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIListTEmpty)]).ToList();
            model1.Int16IListTEmpty = ((short[])jsonObject[nameof(TypesAllModel.Int16IListTEmpty)]).ToList();
            model1.UInt16IListTEmpty = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IListTEmpty)]).ToList();
            model1.Int32IListTEmpty = ((int[])jsonObject[nameof(TypesAllModel.Int32IListTEmpty)]).ToList();
            model1.UInt32IListTEmpty = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IListTEmpty)]).ToList();
            model1.Int64IListTEmpty = ((long[])jsonObject[nameof(TypesAllModel.Int64IListTEmpty)]).ToList();
            model1.UInt64IListTEmpty = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IListTEmpty)]).ToList();
            model1.SingleIListTEmpty = ((float[])jsonObject[nameof(TypesAllModel.SingleIListTEmpty)]).ToList();
            model1.DoubleIListTEmpty = ((double[])jsonObject[nameof(TypesAllModel.DoubleIListTEmpty)]).ToList();
            model1.DecimalIListTEmpty = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIListTEmpty)]).ToList();
            model1.CharIListTEmpty = ((char[])jsonObject[nameof(TypesAllModel.CharIListTEmpty)]).ToList();
            model1.DateTimeIListTEmpty = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIListTEmpty)]).ToList();
            model1.DateTimeOffsetIListTEmpty = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListTEmpty)]).ToList();
            model1.TimeSpanIListTEmpty = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTEmpty = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIListTEmpty)]).ToList();
            model1.TimeOnlyIListTEmpty = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIListTEmpty)]).ToList();
#endif
            model1.GuidIListTEmpty = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIListTEmpty)]).ToList();

            model1.BooleanIListTNull = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIListTNull)])?.ToList();
            model1.ByteIListTNull = ((byte[])jsonObject[nameof(TypesAllModel.ByteIListTNull)])?.ToList();
            model1.SByteIListTNull = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIListTNull)])?.ToList();
            model1.Int16IListTNull = ((short[])jsonObject[nameof(TypesAllModel.Int16IListTNull)])?.ToList();
            model1.UInt16IListTNull = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IListTNull)])?.ToList();
            model1.Int32IListTNull = ((int[])jsonObject[nameof(TypesAllModel.Int32IListTNull)])?.ToList();
            model1.UInt32IListTNull = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IListTNull)])?.ToList();
            model1.Int64IListTNull = ((long[])jsonObject[nameof(TypesAllModel.Int64IListTNull)])?.ToList();
            model1.UInt64IListTNull = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IListTNull)])?.ToList();
            model1.SingleIListTNull = ((float[])jsonObject[nameof(TypesAllModel.SingleIListTNull)])?.ToList();
            model1.DoubleIListTNull = ((double[])jsonObject[nameof(TypesAllModel.DoubleIListTNull)])?.ToList();
            model1.DecimalIListTNull = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIListTNull)])?.ToList();
            model1.CharIListTNull = ((char[])jsonObject[nameof(TypesAllModel.CharIListTNull)])?.ToList();
            model1.DateTimeIListTNull = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIListTNull)])?.ToList();
            model1.DateTimeOffsetIListTNull = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListTNull)])?.ToList();
            model1.TimeSpanIListTNull = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNull = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIListTNull)])?.ToList();
            model1.TimeOnlyIListTNull = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIListTNull)])?.ToList();
#endif
            model1.GuidIListTNull = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIListTNull)])?.ToList();

            model1.BooleanIListTNullable = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIListTNullable)]).ToList();
            model1.ByteIListTNullable = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIListTNullable)]).ToList();
            model1.SByteIListTNullable = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIListTNullable)]).ToList();
            model1.Int16IListTNullable = ((short?[])jsonObject[nameof(TypesAllModel.Int16IListTNullable)]).ToList();
            model1.UInt16IListTNullable = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IListTNullable)]).ToList();
            model1.Int32IListTNullable = ((int?[])jsonObject[nameof(TypesAllModel.Int32IListTNullable)]).ToList();
            model1.UInt32IListTNullable = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IListTNullable)]).ToList();
            model1.Int64IListTNullable = ((long?[])jsonObject[nameof(TypesAllModel.Int64IListTNullable)]).ToList();
            model1.UInt64IListTNullable = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IListTNullable)]).ToList();
            model1.SingleIListTNullable = ((float?[])jsonObject[nameof(TypesAllModel.SingleIListTNullable)]).ToList();
            model1.DoubleIListTNullable = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIListTNullable)]).ToList();
            model1.DecimalIListTNullable = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIListTNullable)]).ToList();
            model1.CharIListTNullable = ((char?[])jsonObject[nameof(TypesAllModel.CharIListTNullable)]).ToList();
            model1.DateTimeIListTNullable = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIListTNullable)]).ToList();
            model1.DateTimeOffsetIListTNullable = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListTNullable)]).ToList();
            model1.TimeSpanIListTNullable = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullable = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIListTNullable)]).ToList();
            model1.TimeOnlyIListTNullable = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIListTNullable)]).ToList();
#endif
            model1.GuidIListTNullable = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIListTNullable)]).ToList();

            model1.BooleanIListTNullableEmpty = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIListTNullableEmpty)]).ToList();
            model1.ByteIListTNullableEmpty = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIListTNullableEmpty)]).ToList();
            model1.SByteIListTNullableEmpty = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIListTNullableEmpty)]).ToList();
            model1.Int16IListTNullableEmpty = ((short?[])jsonObject[nameof(TypesAllModel.Int16IListTNullableEmpty)]).ToList();
            model1.UInt16IListTNullableEmpty = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IListTNullableEmpty)]).ToList();
            model1.Int32IListTNullableEmpty = ((int?[])jsonObject[nameof(TypesAllModel.Int32IListTNullableEmpty)]).ToList();
            model1.UInt32IListTNullableEmpty = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IListTNullableEmpty)]).ToList();
            model1.Int64IListTNullableEmpty = ((long?[])jsonObject[nameof(TypesAllModel.Int64IListTNullableEmpty)]).ToList();
            model1.UInt64IListTNullableEmpty = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IListTNullableEmpty)]).ToList();
            model1.SingleIListTNullableEmpty = ((float?[])jsonObject[nameof(TypesAllModel.SingleIListTNullableEmpty)]).ToList();
            model1.DoubleIListTNullableEmpty = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIListTNullableEmpty)]).ToList();
            model1.DecimalIListTNullableEmpty = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIListTNullableEmpty)]).ToList();
            model1.CharIListTNullableEmpty = ((char?[])jsonObject[nameof(TypesAllModel.CharIListTNullableEmpty)]).ToList();
            model1.DateTimeIListTNullableEmpty = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListTNullableEmpty)]).ToList();
            model1.TimeSpanIListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIListTNullableEmpty)]).ToList();
            model1.TimeOnlyIListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIListTNullableEmpty)]).ToList();
#endif
            model1.GuidIListTNullableEmpty = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIListTNullableEmpty)]).ToList();

            model1.BooleanIListTNullableNull = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIListTNullableNull)])?.ToList();
            model1.ByteIListTNullableNull = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIListTNullableNull)])?.ToList();
            model1.SByteIListTNullableNull = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIListTNullableNull)])?.ToList();
            model1.Int16IListTNullableNull = ((short?[])jsonObject[nameof(TypesAllModel.Int16IListTNullableNull)])?.ToList();
            model1.UInt16IListTNullableNull = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IListTNullableNull)])?.ToList();
            model1.Int32IListTNullableNull = ((int?[])jsonObject[nameof(TypesAllModel.Int32IListTNullableNull)])?.ToList();
            model1.UInt32IListTNullableNull = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IListTNullableNull)])?.ToList();
            model1.Int64IListTNullableNull = ((long?[])jsonObject[nameof(TypesAllModel.Int64IListTNullableNull)])?.ToList();
            model1.UInt64IListTNullableNull = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IListTNullableNull)])?.ToList();
            model1.SingleIListTNullableNull = ((float?[])jsonObject[nameof(TypesAllModel.SingleIListTNullableNull)])?.ToList();
            model1.DoubleIListTNullableNull = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIListTNullableNull)])?.ToList();
            model1.DecimalIListTNullableNull = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIListTNullableNull)])?.ToList();
            model1.CharIListTNullableNull = ((char?[])jsonObject[nameof(TypesAllModel.CharIListTNullableNull)])?.ToList();
            model1.DateTimeIListTNullableNull = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIListTNullableNull)])?.ToList();
            model1.DateTimeOffsetIListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIListTNullableNull)])?.ToList();
            model1.TimeSpanIListTNullableNull = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIListTNullableNull = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIListTNullableNull)])?.ToList();
            model1.TimeOnlyIListTNullableNull = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIListTNullableNull)])?.ToList();
#endif
            model1.GuidIListTNullableNull = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIListTNullableNull)])?.ToList();

            model1.StringIListT = ((string[])jsonObject[nameof(TypesAllModel.StringIListT)]).ToList();
            model1.StringIListTEmpty = ((string[])jsonObject[nameof(TypesAllModel.StringIListTEmpty)]).ToList();
            model1.StringIListTNull = ((string[])jsonObject[nameof(TypesAllModel.StringIListTNull)])?.ToList();

            model1.EnumIListT = (((string[])jsonObject[nameof(TypesAllModel.EnumIListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIListTEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIListTNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIListTNullable = (((string[])jsonObject[nameof(TypesAllModel.EnumIListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIListTNullableEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIListTNullableNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIReadOnlyListT = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListT)]).ToList();
            model1.ByteIReadOnlyListT = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListT)]).ToList();
            model1.SByteIReadOnlyListT = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListT)]).ToList();
            model1.Int16IReadOnlyListT = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListT)]).ToList();
            model1.UInt16IReadOnlyListT = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListT)]).ToList();
            model1.Int32IReadOnlyListT = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListT)]).ToList();
            model1.UInt32IReadOnlyListT = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListT)]).ToList();
            model1.Int64IReadOnlyListT = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListT)]).ToList();
            model1.UInt64IReadOnlyListT = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListT)]).ToList();
            model1.SingleIReadOnlyListT = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListT)]).ToList();
            model1.DoubleIReadOnlyListT = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListT)]).ToList();
            model1.DecimalIReadOnlyListT = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListT)]).ToList();
            model1.CharIReadOnlyListT = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListT)]).ToList();
            model1.DateTimeIReadOnlyListT = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListT)]).ToList();
            model1.DateTimeOffsetIReadOnlyListT = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListT)]).ToList();
            model1.TimeSpanIReadOnlyListT = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListT = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListT)]).ToList();
            model1.TimeOnlyIReadOnlyListT = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListT)]).ToList();
#endif
            model1.GuidIReadOnlyListT = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListT)]).ToList();

            model1.BooleanIReadOnlyListTEmpty = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListTEmpty)]).ToList();
            model1.ByteIReadOnlyListTEmpty = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListTEmpty)]).ToList();
            model1.SByteIReadOnlyListTEmpty = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListTEmpty)]).ToList();
            model1.Int16IReadOnlyListTEmpty = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListTEmpty)]).ToList();
            model1.UInt16IReadOnlyListTEmpty = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListTEmpty)]).ToList();
            model1.Int32IReadOnlyListTEmpty = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListTEmpty)]).ToList();
            model1.UInt32IReadOnlyListTEmpty = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListTEmpty)]).ToList();
            model1.Int64IReadOnlyListTEmpty = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListTEmpty)]).ToList();
            model1.UInt64IReadOnlyListTEmpty = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListTEmpty)]).ToList();
            model1.SingleIReadOnlyListTEmpty = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListTEmpty)]).ToList();
            model1.DoubleIReadOnlyListTEmpty = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListTEmpty)]).ToList();
            model1.DecimalIReadOnlyListTEmpty = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListTEmpty)]).ToList();
            model1.CharIReadOnlyListTEmpty = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListTEmpty)]).ToList();
            model1.DateTimeIReadOnlyListTEmpty = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListTEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTEmpty = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListTEmpty)]).ToList();
            model1.TimeSpanIReadOnlyListTEmpty = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTEmpty = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListTEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyListTEmpty = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListTEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyListTEmpty = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListTEmpty)]).ToList();

            model1.BooleanIReadOnlyListTNull = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListTNull)])?.ToList();
            model1.ByteIReadOnlyListTNull = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListTNull)])?.ToList();
            model1.SByteIReadOnlyListTNull = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListTNull)])?.ToList();
            model1.Int16IReadOnlyListTNull = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListTNull)])?.ToList();
            model1.UInt16IReadOnlyListTNull = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListTNull)])?.ToList();
            model1.Int32IReadOnlyListTNull = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListTNull)])?.ToList();
            model1.UInt32IReadOnlyListTNull = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListTNull)])?.ToList();
            model1.Int64IReadOnlyListTNull = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListTNull)])?.ToList();
            model1.UInt64IReadOnlyListTNull = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListTNull)])?.ToList();
            model1.SingleIReadOnlyListTNull = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListTNull)])?.ToList();
            model1.DoubleIReadOnlyListTNull = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListTNull)])?.ToList();
            model1.DecimalIReadOnlyListTNull = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListTNull)])?.ToList();
            model1.CharIReadOnlyListTNull = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListTNull)])?.ToList();
            model1.DateTimeIReadOnlyListTNull = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListTNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyListTNull = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListTNull)])?.ToList();
            model1.TimeSpanIReadOnlyListTNull = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNull = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListTNull)])?.ToList();
            model1.TimeOnlyIReadOnlyListTNull = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListTNull)])?.ToList();
#endif
            model1.GuidIReadOnlyListTNull = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListTNull)])?.ToList();

            model1.BooleanIReadOnlyListTNullable = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListTNullable)]).ToList();
            model1.ByteIReadOnlyListTNullable = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListTNullable)]).ToList();
            model1.SByteIReadOnlyListTNullable = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListTNullable)]).ToList();
            model1.Int16IReadOnlyListTNullable = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListTNullable)]).ToList();
            model1.UInt16IReadOnlyListTNullable = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListTNullable)]).ToList();
            model1.Int32IReadOnlyListTNullable = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListTNullable)]).ToList();
            model1.UInt32IReadOnlyListTNullable = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListTNullable)]).ToList();
            model1.Int64IReadOnlyListTNullable = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListTNullable)]).ToList();
            model1.UInt64IReadOnlyListTNullable = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListTNullable)]).ToList();
            model1.SingleIReadOnlyListTNullable = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListTNullable)]).ToList();
            model1.DoubleIReadOnlyListTNullable = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListTNullable)]).ToList();
            model1.DecimalIReadOnlyListTNullable = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListTNullable)]).ToList();
            model1.CharIReadOnlyListTNullable = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListTNullable)]).ToList();
            model1.DateTimeIReadOnlyListTNullable = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListTNullable)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTNullable = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListTNullable)]).ToList();
            model1.TimeSpanIReadOnlyListTNullable = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullable = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListTNullable)]).ToList();
            model1.TimeOnlyIReadOnlyListTNullable = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListTNullable)]).ToList();
#endif
            model1.GuidIReadOnlyListTNullable = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListTNullable)]).ToList();

            model1.BooleanIReadOnlyListTNullableEmpty = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListTNullableEmpty)]).ToList();
            model1.ByteIReadOnlyListTNullableEmpty = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListTNullableEmpty)]).ToList();
            model1.SByteIReadOnlyListTNullableEmpty = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListTNullableEmpty)]).ToList();
            model1.Int16IReadOnlyListTNullableEmpty = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt16IReadOnlyListTNullableEmpty = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListTNullableEmpty)]).ToList();
            model1.Int32IReadOnlyListTNullableEmpty = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt32IReadOnlyListTNullableEmpty = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListTNullableEmpty)]).ToList();
            model1.Int64IReadOnlyListTNullableEmpty = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListTNullableEmpty)]).ToList();
            model1.UInt64IReadOnlyListTNullableEmpty = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListTNullableEmpty)]).ToList();
            model1.SingleIReadOnlyListTNullableEmpty = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListTNullableEmpty)]).ToList();
            model1.DoubleIReadOnlyListTNullableEmpty = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListTNullableEmpty)]).ToList();
            model1.DecimalIReadOnlyListTNullableEmpty = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListTNullableEmpty)]).ToList();
            model1.CharIReadOnlyListTNullableEmpty = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListTNullableEmpty)]).ToList();
            model1.DateTimeIReadOnlyListTNullableEmpty = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyListTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListTNullableEmpty)]).ToList();
            model1.TimeSpanIReadOnlyListTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullableEmpty = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListTNullableEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyListTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListTNullableEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyListTNullableEmpty = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListTNullableEmpty)]).ToList();

            model1.BooleanIReadOnlyListTNullableNull = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyListTNullableNull)])?.ToList();
            model1.ByteIReadOnlyListTNullableNull = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyListTNullableNull)])?.ToList();
            model1.SByteIReadOnlyListTNullableNull = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyListTNullableNull)])?.ToList();
            model1.Int16IReadOnlyListTNullableNull = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt16IReadOnlyListTNullableNull = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyListTNullableNull)])?.ToList();
            model1.Int32IReadOnlyListTNullableNull = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt32IReadOnlyListTNullableNull = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyListTNullableNull)])?.ToList();
            model1.Int64IReadOnlyListTNullableNull = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyListTNullableNull)])?.ToList();
            model1.UInt64IReadOnlyListTNullableNull = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyListTNullableNull)])?.ToList();
            model1.SingleIReadOnlyListTNullableNull = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyListTNullableNull)])?.ToList();
            model1.DoubleIReadOnlyListTNullableNull = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyListTNullableNull)])?.ToList();
            model1.DecimalIReadOnlyListTNullableNull = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyListTNullableNull)])?.ToList();
            model1.CharIReadOnlyListTNullableNull = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyListTNullableNull)])?.ToList();
            model1.DateTimeIReadOnlyListTNullableNull = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyListTNullableNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyListTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyListTNullableNull)])?.ToList();
            model1.TimeSpanIReadOnlyListTNullableNull = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyListTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyListTNullableNull = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyListTNullableNull)])?.ToList();
            model1.TimeOnlyIReadOnlyListTNullableNull = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyListTNullableNull)])?.ToList();
#endif
            model1.GuidIReadOnlyListTNullableNull = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyListTNullableNull)])?.ToList();

            model1.StringIReadOnlyListT = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyListT)]).ToList();
            model1.StringIReadOnlyListTEmpty = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyListTEmpty)]).ToList();
            model1.StringIReadOnlyListTNull = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyListTNull)])?.ToList();

            model1.EnumIReadOnlyListT = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyListTEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyListTNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIReadOnlyListTNullable = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyListTNullableEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyListTNullableNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyListTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanICollectionT = ((bool[])jsonObject[nameof(TypesAllModel.BooleanICollectionT)]).ToList();
            model1.ByteICollectionT = ((byte[])jsonObject[nameof(TypesAllModel.ByteICollectionT)]).ToList();
            model1.SByteICollectionT = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteICollectionT)]).ToList();
            model1.Int16ICollectionT = ((short[])jsonObject[nameof(TypesAllModel.Int16ICollectionT)]).ToList();
            model1.UInt16ICollectionT = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ICollectionT)]).ToList();
            model1.Int32ICollectionT = ((int[])jsonObject[nameof(TypesAllModel.Int32ICollectionT)]).ToList();
            model1.UInt32ICollectionT = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ICollectionT)]).ToList();
            model1.Int64ICollectionT = ((long[])jsonObject[nameof(TypesAllModel.Int64ICollectionT)]).ToList();
            model1.UInt64ICollectionT = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ICollectionT)]).ToList();
            model1.SingleICollectionT = ((float[])jsonObject[nameof(TypesAllModel.SingleICollectionT)]).ToList();
            model1.DoubleICollectionT = ((double[])jsonObject[nameof(TypesAllModel.DoubleICollectionT)]).ToList();
            model1.DecimalICollectionT = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalICollectionT)]).ToList();
            model1.CharICollectionT = ((char[])jsonObject[nameof(TypesAllModel.CharICollectionT)]).ToList();
            model1.DateTimeICollectionT = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeICollectionT)]).ToList();
            model1.DateTimeOffsetICollectionT = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionT)]).ToList();
            model1.TimeSpanICollectionT = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionT = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionT)]).ToList();
            model1.TimeOnlyICollectionT = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionT)]).ToList();
#endif
            model1.GuidICollectionT = ((Guid[])jsonObject[nameof(TypesAllModel.GuidICollectionT)]).ToList();

            model1.BooleanICollectionTEmpty = ((bool[])jsonObject[nameof(TypesAllModel.BooleanICollectionTEmpty)]).ToList();
            model1.ByteICollectionTEmpty = ((byte[])jsonObject[nameof(TypesAllModel.ByteICollectionTEmpty)]).ToList();
            model1.SByteICollectionTEmpty = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteICollectionTEmpty)]).ToList();
            model1.Int16ICollectionTEmpty = ((short[])jsonObject[nameof(TypesAllModel.Int16ICollectionTEmpty)]).ToList();
            model1.UInt16ICollectionTEmpty = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ICollectionTEmpty)]).ToList();
            model1.Int32ICollectionTEmpty = ((int[])jsonObject[nameof(TypesAllModel.Int32ICollectionTEmpty)]).ToList();
            model1.UInt32ICollectionTEmpty = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ICollectionTEmpty)]).ToList();
            model1.Int64ICollectionTEmpty = ((long[])jsonObject[nameof(TypesAllModel.Int64ICollectionTEmpty)]).ToList();
            model1.UInt64ICollectionTEmpty = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ICollectionTEmpty)]).ToList();
            model1.SingleICollectionTEmpty = ((float[])jsonObject[nameof(TypesAllModel.SingleICollectionTEmpty)]).ToList();
            model1.DoubleICollectionTEmpty = ((double[])jsonObject[nameof(TypesAllModel.DoubleICollectionTEmpty)]).ToList();
            model1.DecimalICollectionTEmpty = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalICollectionTEmpty)]).ToList();
            model1.CharICollectionTEmpty = ((char[])jsonObject[nameof(TypesAllModel.CharICollectionTEmpty)]).ToList();
            model1.DateTimeICollectionTEmpty = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeICollectionTEmpty)]).ToList();
            model1.DateTimeOffsetICollectionTEmpty = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionTEmpty)]).ToList();
            model1.TimeSpanICollectionTEmpty = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTEmpty = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionTEmpty)]).ToList();
            model1.TimeOnlyICollectionTEmpty = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionTEmpty)]).ToList();
#endif
            model1.GuidICollectionTEmpty = ((Guid[])jsonObject[nameof(TypesAllModel.GuidICollectionTEmpty)]).ToList();

            model1.BooleanICollectionTNull = ((bool[])jsonObject[nameof(TypesAllModel.BooleanICollectionTNull)])?.ToList();
            model1.ByteICollectionTNull = ((byte[])jsonObject[nameof(TypesAllModel.ByteICollectionTNull)])?.ToList();
            model1.SByteICollectionTNull = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteICollectionTNull)])?.ToList();
            model1.Int16ICollectionTNull = ((short[])jsonObject[nameof(TypesAllModel.Int16ICollectionTNull)])?.ToList();
            model1.UInt16ICollectionTNull = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16ICollectionTNull)])?.ToList();
            model1.Int32ICollectionTNull = ((int[])jsonObject[nameof(TypesAllModel.Int32ICollectionTNull)])?.ToList();
            model1.UInt32ICollectionTNull = ((uint[])jsonObject[nameof(TypesAllModel.UInt32ICollectionTNull)])?.ToList();
            model1.Int64ICollectionTNull = ((long[])jsonObject[nameof(TypesAllModel.Int64ICollectionTNull)])?.ToList();
            model1.UInt64ICollectionTNull = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64ICollectionTNull)])?.ToList();
            model1.SingleICollectionTNull = ((float[])jsonObject[nameof(TypesAllModel.SingleICollectionTNull)])?.ToList();
            model1.DoubleICollectionTNull = ((double[])jsonObject[nameof(TypesAllModel.DoubleICollectionTNull)])?.ToList();
            model1.DecimalICollectionTNull = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalICollectionTNull)])?.ToList();
            model1.CharICollectionTNull = ((char[])jsonObject[nameof(TypesAllModel.CharICollectionTNull)])?.ToList();
            model1.DateTimeICollectionTNull = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeICollectionTNull)])?.ToList();
            model1.DateTimeOffsetICollectionTNull = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionTNull)])?.ToList();
            model1.TimeSpanICollectionTNull = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNull = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionTNull)])?.ToList();
            model1.TimeOnlyICollectionTNull = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionTNull)])?.ToList();
#endif
            model1.GuidICollectionTNull = ((Guid[])jsonObject[nameof(TypesAllModel.GuidICollectionTNull)])?.ToList();

            model1.BooleanICollectionTNullable = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanICollectionTNullable)]).ToList();
            model1.ByteICollectionTNullable = ((byte?[])jsonObject[nameof(TypesAllModel.ByteICollectionTNullable)]).ToList();
            model1.SByteICollectionTNullable = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteICollectionTNullable)]).ToList();
            model1.Int16ICollectionTNullable = ((short?[])jsonObject[nameof(TypesAllModel.Int16ICollectionTNullable)]).ToList();
            model1.UInt16ICollectionTNullable = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ICollectionTNullable)]).ToList();
            model1.Int32ICollectionTNullable = ((int?[])jsonObject[nameof(TypesAllModel.Int32ICollectionTNullable)]).ToList();
            model1.UInt32ICollectionTNullable = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ICollectionTNullable)]).ToList();
            model1.Int64ICollectionTNullable = ((long?[])jsonObject[nameof(TypesAllModel.Int64ICollectionTNullable)]).ToList();
            model1.UInt64ICollectionTNullable = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ICollectionTNullable)]).ToList();
            model1.SingleICollectionTNullable = ((float?[])jsonObject[nameof(TypesAllModel.SingleICollectionTNullable)]).ToList();
            model1.DoubleICollectionTNullable = ((double?[])jsonObject[nameof(TypesAllModel.DoubleICollectionTNullable)]).ToList();
            model1.DecimalICollectionTNullable = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalICollectionTNullable)]).ToList();
            model1.CharICollectionTNullable = ((char?[])jsonObject[nameof(TypesAllModel.CharICollectionTNullable)]).ToList();
            model1.DateTimeICollectionTNullable = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeICollectionTNullable)]).ToList();
            model1.DateTimeOffsetICollectionTNullable = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionTNullable)]).ToList();
            model1.TimeSpanICollectionTNullable = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullable = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionTNullable)]).ToList();
            model1.TimeOnlyICollectionTNullable = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionTNullable)]).ToList();
#endif
            model1.GuidICollectionTNullable = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidICollectionTNullable)]).ToList();

            model1.BooleanICollectionTNullableEmpty = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanICollectionTNullableEmpty)]).ToList();
            model1.ByteICollectionTNullableEmpty = ((byte?[])jsonObject[nameof(TypesAllModel.ByteICollectionTNullableEmpty)]).ToList();
            model1.SByteICollectionTNullableEmpty = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteICollectionTNullableEmpty)]).ToList();
            model1.Int16ICollectionTNullableEmpty = ((short?[])jsonObject[nameof(TypesAllModel.Int16ICollectionTNullableEmpty)]).ToList();
            model1.UInt16ICollectionTNullableEmpty = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ICollectionTNullableEmpty)]).ToList();
            model1.Int32ICollectionTNullableEmpty = ((int?[])jsonObject[nameof(TypesAllModel.Int32ICollectionTNullableEmpty)]).ToList();
            model1.UInt32ICollectionTNullableEmpty = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ICollectionTNullableEmpty)]).ToList();
            model1.Int64ICollectionTNullableEmpty = ((long?[])jsonObject[nameof(TypesAllModel.Int64ICollectionTNullableEmpty)]).ToList();
            model1.UInt64ICollectionTNullableEmpty = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ICollectionTNullableEmpty)]).ToList();
            model1.SingleICollectionTNullableEmpty = ((float?[])jsonObject[nameof(TypesAllModel.SingleICollectionTNullableEmpty)]).ToList();
            model1.DoubleICollectionTNullableEmpty = ((double?[])jsonObject[nameof(TypesAllModel.DoubleICollectionTNullableEmpty)]).ToList();
            model1.DecimalICollectionTNullableEmpty = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalICollectionTNullableEmpty)]).ToList();
            model1.CharICollectionTNullableEmpty = ((char?[])jsonObject[nameof(TypesAllModel.CharICollectionTNullableEmpty)]).ToList();
            model1.DateTimeICollectionTNullableEmpty = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeICollectionTNullableEmpty)]).ToList();
            model1.DateTimeOffsetICollectionTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionTNullableEmpty)]).ToList();
            model1.TimeSpanICollectionTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullableEmpty = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionTNullableEmpty)]).ToList();
            model1.TimeOnlyICollectionTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionTNullableEmpty)]).ToList();
#endif
            model1.GuidICollectionTNullableEmpty = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidICollectionTNullableEmpty)]).ToList();

            model1.BooleanICollectionTNullableNull = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanICollectionTNullableNull)])?.ToList();
            model1.ByteICollectionTNullableNull = ((byte?[])jsonObject[nameof(TypesAllModel.ByteICollectionTNullableNull)])?.ToList();
            model1.SByteICollectionTNullableNull = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteICollectionTNullableNull)])?.ToList();
            model1.Int16ICollectionTNullableNull = ((short?[])jsonObject[nameof(TypesAllModel.Int16ICollectionTNullableNull)])?.ToList();
            model1.UInt16ICollectionTNullableNull = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16ICollectionTNullableNull)])?.ToList();
            model1.Int32ICollectionTNullableNull = ((int?[])jsonObject[nameof(TypesAllModel.Int32ICollectionTNullableNull)])?.ToList();
            model1.UInt32ICollectionTNullableNull = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32ICollectionTNullableNull)])?.ToList();
            model1.Int64ICollectionTNullableNull = ((long?[])jsonObject[nameof(TypesAllModel.Int64ICollectionTNullableNull)])?.ToList();
            model1.UInt64ICollectionTNullableNull = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64ICollectionTNullableNull)])?.ToList();
            model1.SingleICollectionTNullableNull = ((float?[])jsonObject[nameof(TypesAllModel.SingleICollectionTNullableNull)])?.ToList();
            model1.DoubleICollectionTNullableNull = ((double?[])jsonObject[nameof(TypesAllModel.DoubleICollectionTNullableNull)])?.ToList();
            model1.DecimalICollectionTNullableNull = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalICollectionTNullableNull)])?.ToList();
            model1.CharICollectionTNullableNull = ((char?[])jsonObject[nameof(TypesAllModel.CharICollectionTNullableNull)])?.ToList();
            model1.DateTimeICollectionTNullableNull = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeICollectionTNullableNull)])?.ToList();
            model1.DateTimeOffsetICollectionTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetICollectionTNullableNull)])?.ToList();
            model1.TimeSpanICollectionTNullableNull = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanICollectionTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyICollectionTNullableNull = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyICollectionTNullableNull)])?.ToList();
            model1.TimeOnlyICollectionTNullableNull = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyICollectionTNullableNull)])?.ToList();
#endif
            model1.GuidICollectionTNullableNull = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidICollectionTNullableNull)])?.ToList();

            model1.StringICollectionT = ((string[])jsonObject[nameof(TypesAllModel.StringICollectionT)]).ToList();
            model1.StringICollectionTEmpty = ((string[])jsonObject[nameof(TypesAllModel.StringICollectionTEmpty)]).ToList();
            model1.StringICollectionTNull = ((string[])jsonObject[nameof(TypesAllModel.StringICollectionTNull)])?.ToList();

            model1.EnumICollectionT = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumICollectionTEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumICollectionTNull = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumICollectionTNullable = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumICollectionTNullableEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumICollectionTNullableNull = (((string[])jsonObject[nameof(TypesAllModel.EnumICollectionTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();

            model1.BooleanIReadOnlyCollectionT = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionT)]).ToList();
            model1.ByteIReadOnlyCollectionT = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionT)]).ToList();
            model1.SByteIReadOnlyCollectionT = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionT)]).ToList();
            model1.Int16IReadOnlyCollectionT = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionT)]).ToList();
            model1.UInt16IReadOnlyCollectionT = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionT)]).ToList();
            model1.Int32IReadOnlyCollectionT = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionT)]).ToList();
            model1.UInt32IReadOnlyCollectionT = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionT)]).ToList();
            model1.Int64IReadOnlyCollectionT = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionT)]).ToList();
            model1.UInt64IReadOnlyCollectionT = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionT)]).ToList();
            model1.SingleIReadOnlyCollectionT = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionT)]).ToList();
            model1.DoubleIReadOnlyCollectionT = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionT)]).ToList();
            model1.DecimalIReadOnlyCollectionT = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionT)]).ToList();
            model1.CharIReadOnlyCollectionT = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionT)]).ToList();
            model1.DateTimeIReadOnlyCollectionT = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionT)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionT = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionT)]).ToList();
            model1.TimeSpanIReadOnlyCollectionT = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionT)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionT = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionT)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionT = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionT)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionT = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionT)]).ToList();

            model1.BooleanIReadOnlyCollectionTEmpty = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionTEmpty)]).ToList();
            model1.ByteIReadOnlyCollectionTEmpty = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionTEmpty)]).ToList();
            model1.SByteIReadOnlyCollectionTEmpty = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionTEmpty)]).ToList();
            model1.Int16IReadOnlyCollectionTEmpty = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt16IReadOnlyCollectionTEmpty = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionTEmpty)]).ToList();
            model1.Int32IReadOnlyCollectionTEmpty = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt32IReadOnlyCollectionTEmpty = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionTEmpty)]).ToList();
            model1.Int64IReadOnlyCollectionTEmpty = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionTEmpty)]).ToList();
            model1.UInt64IReadOnlyCollectionTEmpty = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionTEmpty)]).ToList();
            model1.SingleIReadOnlyCollectionTEmpty = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionTEmpty)]).ToList();
            model1.DoubleIReadOnlyCollectionTEmpty = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionTEmpty)]).ToList();
            model1.DecimalIReadOnlyCollectionTEmpty = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionTEmpty)]).ToList();
            model1.CharIReadOnlyCollectionTEmpty = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionTEmpty)]).ToList();
            model1.DateTimeIReadOnlyCollectionTEmpty = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionTEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTEmpty = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionTEmpty)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTEmpty = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionTEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTEmpty = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionTEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTEmpty = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionTEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTEmpty = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionTEmpty)]).ToList();

            model1.BooleanIReadOnlyCollectionTNull = ((bool[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionTNull)])?.ToList();
            model1.ByteIReadOnlyCollectionTNull = ((byte[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionTNull)])?.ToList();
            model1.SByteIReadOnlyCollectionTNull = ((sbyte[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionTNull)])?.ToList();
            model1.Int16IReadOnlyCollectionTNull = ((short[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt16IReadOnlyCollectionTNull = ((ushort[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionTNull)])?.ToList();
            model1.Int32IReadOnlyCollectionTNull = ((int[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt32IReadOnlyCollectionTNull = ((uint[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionTNull)])?.ToList();
            model1.Int64IReadOnlyCollectionTNull = ((long[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionTNull)])?.ToList();
            model1.UInt64IReadOnlyCollectionTNull = ((ulong[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionTNull)])?.ToList();
            model1.SingleIReadOnlyCollectionTNull = ((float[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionTNull)])?.ToList();
            model1.DoubleIReadOnlyCollectionTNull = ((double[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionTNull)])?.ToList();
            model1.DecimalIReadOnlyCollectionTNull = ((decimal[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionTNull)])?.ToList();
            model1.CharIReadOnlyCollectionTNull = ((char[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionTNull)])?.ToList();
            model1.DateTimeIReadOnlyCollectionTNull = ((DateTime[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionTNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNull = ((DateTimeOffset[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionTNull)])?.ToList();
            model1.TimeSpanIReadOnlyCollectionTNull = ((TimeSpan[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionTNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNull = ((DateOnly[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionTNull)])?.ToList();
            model1.TimeOnlyIReadOnlyCollectionTNull = ((TimeOnly[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionTNull)])?.ToList();
#endif
            model1.GuidIReadOnlyCollectionTNull = ((Guid[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionTNull)])?.ToList();

            model1.BooleanIReadOnlyCollectionTNullable = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionTNullable)]).ToList();
            model1.ByteIReadOnlyCollectionTNullable = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionTNullable)]).ToList();
            model1.SByteIReadOnlyCollectionTNullable = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionTNullable)]).ToList();
            model1.Int16IReadOnlyCollectionTNullable = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt16IReadOnlyCollectionTNullable = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionTNullable)]).ToList();
            model1.Int32IReadOnlyCollectionTNullable = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt32IReadOnlyCollectionTNullable = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionTNullable)]).ToList();
            model1.Int64IReadOnlyCollectionTNullable = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionTNullable)]).ToList();
            model1.UInt64IReadOnlyCollectionTNullable = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionTNullable)]).ToList();
            model1.SingleIReadOnlyCollectionTNullable = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionTNullable)]).ToList();
            model1.DoubleIReadOnlyCollectionTNullable = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionTNullable)]).ToList();
            model1.DecimalIReadOnlyCollectionTNullable = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionTNullable)]).ToList();
            model1.CharIReadOnlyCollectionTNullable = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionTNullable)]).ToList();
            model1.DateTimeIReadOnlyCollectionTNullable = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionTNullable)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullable = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionTNullable)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTNullable = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionTNullable)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullable = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionTNullable)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullable = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionTNullable)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullable = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionTNullable)]).ToList();

            model1.BooleanIReadOnlyCollectionTNullableEmpty = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.ByteIReadOnlyCollectionTNullableEmpty = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.SByteIReadOnlyCollectionTNullableEmpty = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int16IReadOnlyCollectionTNullableEmpty = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt16IReadOnlyCollectionTNullableEmpty = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int32IReadOnlyCollectionTNullableEmpty = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt32IReadOnlyCollectionTNullableEmpty = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.Int64IReadOnlyCollectionTNullableEmpty = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.UInt64IReadOnlyCollectionTNullableEmpty = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.SingleIReadOnlyCollectionTNullableEmpty = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DoubleIReadOnlyCollectionTNullableEmpty = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DecimalIReadOnlyCollectionTNullableEmpty = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.CharIReadOnlyCollectionTNullableEmpty = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DateTimeIReadOnlyCollectionTNullableEmpty = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullableEmpty = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.TimeSpanIReadOnlyCollectionTNullableEmpty = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionTNullableEmpty)]).ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullableEmpty = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionTNullableEmpty)]).ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullableEmpty = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionTNullableEmpty)]).ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullableEmpty = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionTNullableEmpty)]).ToList();

            model1.BooleanIReadOnlyCollectionTNullableNull = ((bool?[])jsonObject[nameof(TypesAllModel.BooleanIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.ByteIReadOnlyCollectionTNullableNull = ((byte?[])jsonObject[nameof(TypesAllModel.ByteIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.SByteIReadOnlyCollectionTNullableNull = ((sbyte?[])jsonObject[nameof(TypesAllModel.SByteIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int16IReadOnlyCollectionTNullableNull = ((short?[])jsonObject[nameof(TypesAllModel.Int16IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt16IReadOnlyCollectionTNullableNull = ((ushort?[])jsonObject[nameof(TypesAllModel.UInt16IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int32IReadOnlyCollectionTNullableNull = ((int?[])jsonObject[nameof(TypesAllModel.Int32IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt32IReadOnlyCollectionTNullableNull = ((uint?[])jsonObject[nameof(TypesAllModel.UInt32IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.Int64IReadOnlyCollectionTNullableNull = ((long?[])jsonObject[nameof(TypesAllModel.Int64IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.UInt64IReadOnlyCollectionTNullableNull = ((ulong?[])jsonObject[nameof(TypesAllModel.UInt64IReadOnlyCollectionTNullableNull)])?.ToList();
            model1.SingleIReadOnlyCollectionTNullableNull = ((float?[])jsonObject[nameof(TypesAllModel.SingleIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DoubleIReadOnlyCollectionTNullableNull = ((double?[])jsonObject[nameof(TypesAllModel.DoubleIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DecimalIReadOnlyCollectionTNullableNull = ((decimal?[])jsonObject[nameof(TypesAllModel.DecimalIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.CharIReadOnlyCollectionTNullableNull = ((char?[])jsonObject[nameof(TypesAllModel.CharIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DateTimeIReadOnlyCollectionTNullableNull = ((DateTime?[])jsonObject[nameof(TypesAllModel.DateTimeIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.DateTimeOffsetIReadOnlyCollectionTNullableNull = ((DateTimeOffset?[])jsonObject[nameof(TypesAllModel.DateTimeOffsetIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.TimeSpanIReadOnlyCollectionTNullableNull = ((TimeSpan?[])jsonObject[nameof(TypesAllModel.TimeSpanIReadOnlyCollectionTNullableNull)])?.ToList();
#if NET6_0_OR_GREATER
            model1.DateOnlyIReadOnlyCollectionTNullableNull = ((DateOnly?[])jsonObject[nameof(TypesAllModel.DateOnlyIReadOnlyCollectionTNullableNull)])?.ToList();
            model1.TimeOnlyIReadOnlyCollectionTNullableNull = ((TimeOnly?[])jsonObject[nameof(TypesAllModel.TimeOnlyIReadOnlyCollectionTNullableNull)])?.ToList();
#endif
            model1.GuidIReadOnlyCollectionTNullableNull = ((Guid?[])jsonObject[nameof(TypesAllModel.GuidIReadOnlyCollectionTNullableNull)])?.ToList();

            model1.StringIReadOnlyCollectionT = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyCollectionT)]).ToList();
            model1.StringIReadOnlyCollectionTEmpty = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyCollectionTEmpty)]).ToList();
            model1.StringIReadOnlyCollectionTNull = ((string[])jsonObject[nameof(TypesAllModel.StringIReadOnlyCollectionTNull)])?.ToList();

            model1.EnumIReadOnlyCollectionT = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionT)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyCollectionTEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionTEmpty)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumIReadOnlyCollectionTNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionTNull)])?.Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList())?.ToList();

            model1.EnumIReadOnlyCollectionTNullable = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionTNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyCollectionTNullableEmpty = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionTNullableEmpty)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();
            model1.EnumIReadOnlyCollectionTNullableNull = (((string[])jsonObject[nameof(TypesAllModel.EnumIReadOnlyCollectionTNullableNull)])?.Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray())?.ToList();


            var classThingJsonObject = jsonObject[nameof(TypesAllModel.ClassThing)];
            if (!classThingJsonObject.IsNull)
            {
                model1.ClassThing = new SimpleModel();
                model1.ClassThing.Value1 = (int)classThingJsonObject["Value1"];
                model1.ClassThing.Value2 = (string)classThingJsonObject["Value2"];
            }

            var classThingNullJsonObject = jsonObject[nameof(TypesAllModel.ClassThingNull)];
            if (!classThingNullJsonObject.IsNull)
            {
                model1.ClassThingNull = new SimpleModel();
                model1.ClassThingNull.Value1 = (int)classThingNullJsonObject["Value1"];
                model1.ClassThingNull.Value2 = (string)classThingNullJsonObject["Value2"];
            }


            var classArrayJsonObject = jsonObject[nameof(TypesAllModel.ClassArray)];
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

            var classArrayEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassArrayEmpty)];
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

            var classEnumerableJsonObject = jsonObject[nameof(TypesAllModel.ClassEnumerable)];
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

            var classListJsonObject = jsonObject[nameof(TypesAllModel.ClassList)];
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

            var classListEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassListEmpty)];
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

            var classIListJsonObject = jsonObject[nameof(TypesAllModel.ClassIList)];
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

            var classIListEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassIListEmpty)];
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

            var classIReadOnlyListJsonObject = jsonObject[nameof(TypesAllModel.ClassIReadOnlyList)];
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

            var classIReadOnlyListEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassIReadOnlyListEmpty)];
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

            var classICollectionJsonObject = jsonObject[nameof(TypesAllModel.ClassICollection)];
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

            var classICollectionEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassICollectionEmpty)];
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

            var classIReadOnlyCollectionJsonObject = jsonObject[nameof(TypesAllModel.ClassIReadOnlyCollection)];
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

            var classIReadOnlyCollectionEmptyJsonObject = jsonObject[nameof(TypesAllModel.ClassIReadOnlyCollectionEmpty)];
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

            var dictionaryThingJsonObject1 = jsonObject[nameof(TypesAllModel.DictionaryThing1)];
            model1.DictionaryThing1 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject1["1"]),
                    new(2, (string)dictionaryThingJsonObject1["2"]),
                    new(3, (string)dictionaryThingJsonObject1["3"]),
                    new(4, (string)dictionaryThingJsonObject1["4"]),
                }
            );

            var dictionaryThingJsonObject2 = jsonObject[nameof(TypesAllModel.DictionaryThing2)];
            model1.DictionaryThing2 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject2["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject2["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject2["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject2["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject3 = jsonObject[nameof(TypesAllModel.DictionaryThing3)];
            model1.DictionaryThing3 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject3["1"]),
                    new(2, (string)dictionaryThingJsonObject3["2"]),
                    new(3, (string)dictionaryThingJsonObject3["3"]),
                    new(4, (string)dictionaryThingJsonObject3["4"]),
                }
            );

            var dictionaryThingJsonObject4 = jsonObject[nameof(TypesAllModel.DictionaryThing4)];
            model1.DictionaryThing4 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject4["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject4["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject4["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject4["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject5 = jsonObject[nameof(TypesAllModel.DictionaryThing5)];
            model1.DictionaryThing5 = new Dictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject5["1"]),
                    new(2, (string)dictionaryThingJsonObject5["2"]),
                    new(3, (string)dictionaryThingJsonObject5["3"]),
                    new(4, (string)dictionaryThingJsonObject5["4"]),
                }
            );

            var dictionaryThingJsonObject6 = jsonObject[nameof(TypesAllModel.DictionaryThing6)];
            model1.DictionaryThing6 = new Dictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject6["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject6["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject6["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject6["4"].Bind<SimpleModel>()),
                }
            );

            var dictionaryThingJsonObject7 = jsonObject[nameof(TypesAllModel.DictionaryThing7)];
            model1.DictionaryThing7 = new ConcurrentDictionary<int, string>(
                new KeyValuePair<int, string>[]
                {
                    new(1, (string)dictionaryThingJsonObject7["1"]),
                    new(2, (string)dictionaryThingJsonObject7["2"]),
                    new(3, (string)dictionaryThingJsonObject7["3"]),
                    new(4, (string)dictionaryThingJsonObject7["4"]),
                }
            );

            var dictionaryThingJsonObject8 = jsonObject[nameof(TypesAllModel.DictionaryThing8)];
            model1.DictionaryThing8 = new ConcurrentDictionary<int, SimpleModel>(
                new KeyValuePair<int, SimpleModel>[]
                {
                    new(1, dictionaryThingJsonObject8["1"].Bind<SimpleModel>()),
                    new(2, dictionaryThingJsonObject8["2"].Bind<SimpleModel>()),
                    new(3, dictionaryThingJsonObject8["3"].Bind<SimpleModel>()),
                    new(4, dictionaryThingJsonObject8["4"].Bind<SimpleModel>()),
                }
            );

            var stringArrayOfArrayThingJsonObject = jsonObject[nameof(TypesAllModel.StringArrayOfArrayThing)];
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

            var model2 = jsonObject.Bind<TypesAllModel>();
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
            var baseModel = TypesAllModel.Create();
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
            var model1 = TypesCoreAlternatingModel.Create();
            var json1 = JsonSerializerOld.Serialize(model1);
            var result1 = JsonSerializerOld.Deserialize<TypesCoreModel>(json1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = TypesCoreModel.Create();
            var json2 = JsonSerializerOld.Serialize(model2);
            var result2 = JsonSerializerOld.Deserialize<TypesCoreAlternatingModel>(json2);
            AssertHelper.AreEqual(result2, model2);
        }

        [TestMethod]
        public void StringLargeModel()
        {
            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            var json = JsonSerializerOld.Serialize(models);
            var result = JsonSerializerOld.Deserialize<TypesAllModel[]>(json);

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
            var model1 = TypesHashSetModel.Create();
            var json = JsonSerializerOld.Serialize(model1);
            var model2 = JsonSerializerOld.Deserialize<TypesHashSetModel>(json);
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
            var baseModel = TypesAllModel.Create();

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
            var model1 = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json1, 
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

            var baseModel = TypesAllModel.Create();

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
            var model1 = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream3);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream4, options);

            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamEnumAsNumber()
        {
            var options = new JsonSerializerOptionsOld()
            {
                EnumAsNumber = true
            };

            var baseModel = TypesAllModel.Create();

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
            var model = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream, options);
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
            var baseModel = TypesAllModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializerOld.DeserializeAsync<TypesAllAsStringsModel>(stream1);
            TypesAllAsStringsModel.AreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream2);
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
            var baseModel = TypesAllModel.Create();
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
            var model = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream2);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamNameless()
        {
            var options = new JsonSerializerOptionsOld()
            {
                Nameless = true
            };

            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
        public async Task StreamDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptionsOld()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializerOld.DeserializeAsync<TypesAllModel>(stream, options);
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
            var baseModel = TypesAllModel.Create();

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
            var model1 = TypesCoreAlternatingModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var result1 = await JsonSerializerOld.DeserializeAsync<TypesCoreModel>(stream1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = TypesCoreModel.Create();
            using var stream2 = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializerOld.DeserializeAsync<TypesCoreAlternatingModel>(stream2);
            AssertHelper.AreEqual(result2, model2);
        }

        [TestMethod]
        public async Task StreamLargeModel()
        {
#if DEBUG
            JsonSerializerOld.Testing = false;
#endif

            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, models);
            stream.Position = 0;
            var result = await JsonSerializerOld.DeserializeAsync<TypesAllModel[]>(stream);

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
            var model1 = TypesHashSetModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializerOld.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializerOld.DeserializeAsync<TypesHashSetModel>(stream);
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
