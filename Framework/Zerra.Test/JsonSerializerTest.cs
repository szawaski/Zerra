// Copyright © KaKush LLC
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
        [TestMethod]
        public void StringMatchesStandards()
        {
            var baseModel = Factory.GetAllTypesModel();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, new Newtonsoft.Json.Converters.StringEnumConverter());
            //swap serializers
            var model1 = JsonSerializer.Deserialize<AllTypesModel>(json2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, new Newtonsoft.Json.Converters.StringEnumConverter());
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

            var model2 = new EnumConversionModel2() { Thing = 3 };
            var test2 = JsonSerializer.Serialize(model2);
            var result2 = JsonSerializer.Deserialize<EnumConversionModel1>(test2);
            Assert.AreEqual(model2.Thing, (int)result2.Thing);
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
            var baseModel = Factory.GetAllTypesModel();
            var json = JsonSerializer.SerializeNameless(baseModel);
            var model = JsonSerializer.DeserializeNameless<AllTypesModel>(json);
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

            var json4 = JsonSerializer.Serialize<object>(String.Empty);
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
            for (var i = 0; i < 255; i++)
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
            model1.GuidThingNullableNull = (Guid?)jsonObject[nameof(AllTypesModel.GuidThingNullableNull)];

            model1.StringThing = (string)jsonObject[nameof(AllTypesModel.StringThing)];
            model1.StringThingNull = (string)jsonObject[nameof(AllTypesModel.StringThingNull)];
            model1.StringThingEmpty = (string)jsonObject[nameof(AllTypesModel.StringThingEmpty)];

            model1.EnumThing = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThing)]);
            model1.EnumThingNullable = (EnumModel)Enum.Parse(typeof(EnumModel), (string)jsonObject[nameof(AllTypesModel.EnumThingNullable)]);
            model1.EnumThingNullableNull = ((string)jsonObject[nameof(AllTypesModel.EnumThingNullableNull)]) == null ? (EnumModel?)null : EnumModel.Item1;

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
            model1.GuidArray = (Guid[])jsonObject[nameof(AllTypesModel.GuidArray)];

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
            model1.GuidArrayNullable = (Guid?[])jsonObject[nameof(AllTypesModel.GuidArrayNullable)];

            model1.StringArray = (string[])jsonObject[nameof(AllTypesModel.StringArray)];
            model1.StringEmptyArray = (string[])jsonObject[nameof(AllTypesModel.StringEmptyArray)];

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
            model1.GuidList = ((Guid[])jsonObject[nameof(AllTypesModel.GuidList)]).ToList();

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
            model1.GuidListNullable = ((Guid?[])jsonObject[nameof(AllTypesModel.GuidListNullable)]).ToList();

            model1.StringList = ((string[])jsonObject[nameof(AllTypesModel.StringList)]).ToList();

            model1.EnumList = (((string[])jsonObject[nameof(AllTypesModel.EnumList)]).Select(x => (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToList()).ToList();
            model1.EnumListNullable = (((string[])jsonObject[nameof(AllTypesModel.EnumListNullable)]).Select(x => x == null ? (EnumModel?)null : (EnumModel)Enum.Parse(typeof(EnumModel), x)).ToArray()).ToList();

            var classThingJsonObject = jsonObject[nameof(AllTypesModel.ClassThing)];
            if (!classThingJsonObject.IsNull)
            {
                model1.ClassThing = new BasicModel();
                model1.ClassThing.Value = (int)classThingJsonObject["Value"];
            }

            var classThingNullJsonObject = jsonObject[nameof(AllTypesModel.ClassThingNull)];
            if (!classThingNullJsonObject.IsNull)
            {
                model1.ClassThingNull = new BasicModel();
                model1.ClassThingNull.Value = (int)classThingNullJsonObject["Value"];
            }


            var classArrayJsonObject = jsonObject[nameof(AllTypesModel.ClassArray)];
            var classArray = new List<BasicModel>();
            foreach (var item in classArrayJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
                    obj.Value = (int)item["Value"];
                    classArray.Add(obj);
                }
                else
                {
                    classArray.Add(null);
                }
            }
            model1.ClassArray = classArray.ToArray();

            var classEnumerableJsonObject = jsonObject[nameof(AllTypesModel.ClassEnumerable)];
            var classEnumerable = new List<BasicModel>();
            foreach (var item in classEnumerableJsonObject)
            {
                if (!item.IsNull)
                {
                    var obj = new BasicModel();
                    obj.Value = (int)item["Value"];
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
                    obj.Value = (int)item["Value"];
                    classList.Add(obj);
                }
                else
                {
                    classList.Add(null);
                }
            }
            model1.ClassList = classList;

            var dictionaryThingJsonObject = jsonObject[nameof(AllTypesModel.DictionaryThing)];
            model1.DictionaryThing = new Dictionary<int, string>();
            model1.DictionaryThing.Add(1, (string)dictionaryThingJsonObject["1"]);
            model1.DictionaryThing.Add(2, (string)dictionaryThingJsonObject["2"]);
            model1.DictionaryThing.Add(3, (string)dictionaryThingJsonObject["3"]);
            model1.DictionaryThing.Add(4, (string)dictionaryThingJsonObject["4"]);

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
        public async Task StreamMatchesStandards()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync();

            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel, new Newtonsoft.Json.Converters.StringEnumConverter());

            //swap serializers
            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AllTypesModel>(json1, new Newtonsoft.Json.Converters.StringEnumConverter());
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<AllTypesModel>(stream);
            Factory.AssertAreEqual(baseModel, model);
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

            var model2 = new EnumConversionModel2() { Thing = 3 };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model2);
            stream.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream);
            Assert.AreEqual(model2.Thing, (int)result2.Thing);
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
            var baseModel = Factory.GetAllTypesModel();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeNamelessAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeNamelessAsync<AllTypesModel>(stream);
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
            await JsonSerializer.SerializeAsync<object>(stream4, String.Empty);
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
            for (var i = 0; i < 255; i++)
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
    }
}
