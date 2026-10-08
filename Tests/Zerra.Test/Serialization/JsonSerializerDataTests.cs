// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Reflection;
using System.Text;
using Xunit;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
    public class JsonSerializerDataTests
    {
        public JsonSerializerDataTests()
        {
#if DEBUG
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
            Zerra.Serialization.Json.IO.JsonWriter.Testing = true;
#endif
        }

        [Fact]
        public void StringMatchesNewtonsoft()
        {
            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel,
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            var model1 = JsonSerializer.Deserialize<TypesAllModel>(json2);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json1,
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel, options);

            Assert.True(json1 == json2);

            //swap serializers
            var model1 = JsonSerializer.Deserialize<TypesAllModel>(json2);
            var model2 = System.Text.Json.JsonSerializer.Deserialize<TypesAllModel>(json1, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesBasicModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesArray()
        {
            var model1 = TypesArrayModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesArrayModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesListT()
        {
            var model1 = TypesListTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesListTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyListTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIList()
        {
            var model1 = TypesIListModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIListOfT()
        {
            var model1 = TypesIListOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesHashSetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesISetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesISetTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlySetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyCollectionTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIEnumerableTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
        }

        [Fact]
        public void StringTypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIEnumerableModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIEnumerableOfT()
        {
            var model1 = TypesIEnumerableOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
        }

        [Fact]
        public void StringTypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesDictionaryTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIDictionaryTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIDictionaryTOfT()
        {
            var model1 = TypesIDictionaryTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIDictionaryTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyDictionaryTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        //[Fact]
        //public void StringTypesIDictionary()
        //{
        //    var model1 = TypesIDictionaryModel.Create();
        //    var json = JsonSerializer.Serialize(model1);
        //    var model2 = JsonSerializer.Deserialize<TypesIDictionaryModel>(json);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        //[Fact]
        //public void StringTypesIDictionaryOfT()
        //{
        //    var model1 = TypesIDictionaryOfTModel.Create();
        //    var json = JsonSerializer.Serialize(model1);
        //    var model2 = JsonSerializer.Deserialize<TypesIDictionaryOfTModel>(json);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        [Fact]
        public void StringTypesCustomCollections()
        {
            var model1 = TypesCustomCollectionsModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesCustomCollectionsModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesOther()
        {
            var model1 = TypesOtherModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesOtherModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesCore()
        {
            var model1 = TypesCoreModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesCoreModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringTypesAll()
        {
            var model1 = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesAllModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringEnumAsNumbers()
        {
            var options = new JsonSerializerOptions()
            {
                EnumAsNumber = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel, options);
            Assert.DoesNotContain(EnumModel.EnumItem0.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem1.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem2.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem3.EnumName(), json);
            var model = JsonSerializer.Deserialize<TypesAllModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public void StringConvertNullables()
        {
            var baseModel = BasicTypesNotNullable.Create();
            var json1 = JsonSerializer.Serialize(baseModel);
            var model1 = JsonSerializer.Deserialize<BasicTypesNullable>(json1);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model1);

            var json2 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<BasicTypesNotNullable>(json2);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model2);
        }

        [Fact]
        public void StringConvertTypes()
        {
            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializer.Serialize(baseModel);
            var model1 = JsonSerializer.Deserialize<TypesAllAsStringsModel>(json1);
            TypesAllAsStringsModel.AreEqual(baseModel, model1);

            var json2 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesAllModel>(json2);
            AssertHelper.AreEqual(baseModel, model2);
        }

        [Fact]
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
        private static void StringTestNumber<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<T>(json);
            Assert.Equal(value, result);
        }
        private static void StringTestNumberAsString<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<string>(json);
            Assert.Equal(json, result);
        }

        [Fact]
        public void StringEnumConversion()
        {
            //var model1 = new EnumConversionModel1() { Thing = EnumModel.Item2 };
            //var test1 = JsonSerializer.Serialize(model1);
            //var result1 = JsonSerializer.Deserialize<EnumConversionModel2>(test1);
            //Assert.Equal((int)model1.Thing, result1.Thing);

            var model2 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = 2,
                Thing3 = 3,
                Thing4 = 4
            };

            var json2 = JsonSerializer.Serialize(model2);
            var result2 = JsonSerializer.Deserialize<EnumConversionModel1>(json2);
            Assert.Equal(model2.Thing1, (int)result2.Thing1);
            Assert.Equal(model2.Thing2, (int?)result2.Thing2);
            Assert.Equal(model2.Thing3, (int)result2.Thing3);
            Assert.Equal(model2.Thing4, (int?)result2.Thing4);

            var model3 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = null,
                Thing3 = 3,
                Thing4 = null
            };

            var json3 = JsonSerializer.Serialize(model3);
            var result3 = JsonSerializer.Deserialize<EnumConversionModel1>(json3);
            Assert.Equal(model3.Thing1, (int)result3.Thing1);
            Assert.Equal(default, result3.Thing2);
            Assert.Equal(model3.Thing3, (int)result3.Thing3);
            Assert.Equal(model3.Thing4, (int?)result3.Thing4);
        }

        [Fact]
        public void StringPretty()
        {
            var baseModel = TypesAllModel.Create();
            var json = System.Text.Json.JsonSerializer.Serialize(baseModel, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            var model = JsonSerializer.Deserialize<TypesAllModel>(json);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public void StringNameless()
        {
            var options = new JsonSerializerOptions()
            {
                Nameless = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel, options);
            var model = JsonSerializer.Deserialize<TypesAllModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public void StringDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptions()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel, options);
            var model = JsonSerializer.Deserialize<TypesAllModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public void StringEmptys()
        {
            var json1 = JsonSerializer.Serialize<string>(null);
            Assert.Equal("null", json1);

            var json2 = JsonSerializer.Serialize<string>(String.Empty);
            Assert.Equal("\"\"", json2);

            var json3 = JsonSerializer.Serialize<object>(null);
            Assert.Equal("null", json3);

            var json4 = JsonSerializer.Serialize<object>(new object());
            Assert.Equal("{}", json4);

            var model1 = JsonSerializer.Deserialize<string>("null");
            Assert.Null(model1);

            var model2 = JsonSerializer.Deserialize<string>("");
            Assert.Equal(String.Empty, model2);

            var model3 = JsonSerializer.Deserialize<string>("\"\"");
            Assert.Equal(String.Empty, model3);

            var model4 = JsonSerializer.Deserialize<string>("{}");
            Assert.Null(model4);

            var model5 = JsonSerializer.Deserialize<object>("null");
            Assert.Null(model5);

            var model6 = JsonSerializer.Deserialize<object>("");
            Assert.Null(model6);

            var model7 = JsonSerializer.Deserialize<object>("\"\"");
            Assert.Equal(String.Empty, model7);

            var model8 = JsonSerializer.Deserialize<object>("{}");
            Assert.NotNull(model8);

            var model9 = JsonSerializer.Deserialize<int>("");
            Assert.Equal(0, model9);

            var model10 = JsonSerializer.Deserialize<int?>("");
            Assert.Null(model10);

            StringEmptysNumbers<byte>();
            StringEmptysNumbers<sbyte>();
            StringEmptysNumbers<short>();
            StringEmptysNumbers<ushort>();
            StringEmptysNumbers<int>();
            StringEmptysNumbers<uint>();
            StringEmptysNumbers<long>();
            StringEmptysNumbers<ulong>();
            StringEmptysNumbers<float>();
            StringEmptysNumbers<double>();
            StringEmptysNumbers<decimal>();
        }
        private static void StringEmptysNumbers<T>()
            where T : unmanaged
        {
            var model11 = JsonSerializer.Deserialize<T>("\"\"");
            Assert.Equal(default, model11);

            var model12 = JsonSerializer.Deserialize<T?>("\"\"");
            Assert.Null(model12);
        }

        [Fact]
        public void StringDateTimeTypes()
        {
            var dateUtc = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Utc);
            var json = JsonSerializer.Serialize(dateUtc);
            var dateUtc2 = JsonSerializer.Deserialize<DateTime>(json);
            Assert.Equal(dateUtc, dateUtc2);
            Assert.Equal(DateTimeKind.Utc, dateUtc2.Kind);

            var dateLocal = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Local);
            json = JsonSerializer.Serialize(dateLocal);
            var dateLocal2 = JsonSerializer.Deserialize<DateTime>(json);
            var dateLocalUtc = dateLocal.ToUniversalTime();
            Assert.Equal(dateLocalUtc, dateLocal2);
            Assert.Equal(DateTimeKind.Utc, dateLocal2.Kind);

            var dateUnspecified = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Unspecified);
            json = JsonSerializer.Serialize(dateUnspecified);
            var dateUnspecified2 = JsonSerializer.Deserialize<DateTime>(json);
            Assert.Equal(dateUnspecified, dateUnspecified2);
            Assert.Equal(DateTimeKind.Utc, dateUnspecified2.Kind);
        }

        [Fact]
        public void StringEscaping()
        {
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;
                var json = JsonSerializer.Serialize(c);
                var utf8Valid = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json));
                Assert.Equal(json, utf8Valid); //not encoding surrogates would fail
                var result = JsonSerializer.Deserialize<char>(json);
                Assert.Equal(c, result);

                switch (c)
                {
                    case '\\':
                    case '"':
                    case '\b':
                    case '\t':
                    case '\n':
                    case '\f':
                    case '\r':
                        Assert.Equal(4, json.Length);
                        break;
                    default:
                        if (c < ' ')
                            Assert.Equal(8, json.Length);
                        break;
                }

                var str = new string([c]);
                json = JsonSerializer.Serialize(str);
                utf8Valid = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json));
                Assert.Equal(json, utf8Valid); //not encoding surrogates would fail
                var resultStr = JsonSerializer.Deserialize<string>(json);
                Assert.Equal(str, resultStr);

                var strPadded = $"aa{c}bb";
                json = JsonSerializer.Serialize(strPadded);
                utf8Valid = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json));
                Assert.Equal(json, utf8Valid); //not encoding surrogates would fail
                var resultStrPadded = JsonSerializer.Deserialize<string>(json);
                Assert.Equal(strPadded, resultStrPadded);
            }

            //deserialize will include all unicode escapes, some serialize differently
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;

                var charsLower = $"\"\\u{i:x4}\"";
                var charsUpper = $"\"\\u{i:X4}\"";
                var charsPadded = $"\"aa\\u{i:X4}bb\"";

                var result = JsonSerializer.Deserialize<char>(charsLower);
                Assert.Equal(c, result);

                result = JsonSerializer.Deserialize<char>(charsUpper);
                Assert.Equal(c, result);

                var resultStr = JsonSerializer.Deserialize<string>(charsPadded);
                Assert.Equal($"aa{c}bb", resultStr);
            }
        }

        [Fact]
        public void StringExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<Exception>(json);
            Assert.Equal(model1.Message, model2.Message);
        }

        [Fact]
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

            Assert.Equal(5, model2.Property1);
            Assert.Equal(6, model2.Property2);
        }

        [Fact]
        public void StringEmptyModel()
        {
            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<EmptyModel>(json);
            Assert.NotNull(model);
        }

        [Fact]
        public void StringGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();

            var json = JsonSerializer.Serialize(baseModel);

            var model = JsonSerializer.Deserialize<GetsSetsModel>(baseModelJson);
            Assert.NotNull(model);
        }

        [Fact]
        public void StringDrainModel()
        {
            var model1 = TypesCoreAlternatingModel.Create();
            var json1 = JsonSerializer.Serialize(model1);
            var result1 = JsonSerializer.Deserialize<TypesCoreModel>(json1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = TypesCoreModel.Create();
            var json2 = JsonSerializer.Serialize(model2);
            var result2 = JsonSerializer.Deserialize<TypesCoreAlternatingModel>(json2);
            AssertHelper.AreEqual(result2, model2);
        }

        [Fact]
        public void StringLargeModel()
        {
            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            var json = JsonSerializer.Serialize(models);
            var result = JsonSerializer.Deserialize<TypesAllModel[]>(json);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [Fact]
        public void StringBoxing()
        {
            var baseModel = TestBoxingModel.Create();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<TestBoxingModel>(json);
        }

        [Fact]
        public void StringHashSet()
        {
            var model1 = TypesHashSetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesHashSetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<RecordModel>(json);
            Assert.NotNull(model);
            Assert.Equal(baseModel.Property1, model.Property1);
            Assert.Equal(baseModel.Property2, model.Property2);
            Assert.Equal(baseModel.Property3, model.Property3);
        }

        [Fact]
        public void StringPropertyNameAttribute()
        {
            var baseModel = JsonPropertyNameAttributeTestModel.Create();

            var json = JsonSerializer.Serialize(baseModel);

            Assert.Contains("\"1property\"", json);
            Assert.Contains("\"property2\"", json);
            Assert.Contains("\"3property\"", json);

            _ = json.Replace("\"property2\"", "\"PROPERTY2\"");

            var model = JsonSerializer.Deserialize<JsonPropertyNameAttributeTestModel>(json);
            Assert.Equal(baseModel._1_Property, model._1_Property);
            Assert.Equal(baseModel.property2, model.property2);
            Assert.NotNull(model._3_Property);
            Assert.Equal(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.Equal(baseModel._3_Property.Value2, model._3_Property.Value2);
        }

        [Fact]
        public void StringIgnoreAttribute()
        {
            var baseModel = JsonIgnoreAttributeTestModel.Create();

            var json = JsonSerializer.Serialize(baseModel);
            Assert.Contains("\"Property1\"", json);
            Assert.DoesNotContain("\"Property2\"", json);
            Assert.Contains("\"Property3\"", json);
            Assert.DoesNotContain("\"Property4\"", json);
            Assert.Contains("\"Property5a\"", json);
            Assert.DoesNotContain("\"Property5b\"", json);
            Assert.Contains("\"Property6a\"", json);
            Assert.DoesNotContain("\"Property6b\"", json);

            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<JsonIgnoreAttributeTestModel>(json2);
            Assert.Equal(baseModel.Property1, model.Property1);
            Assert.Equal(0, model.Property2);
            Assert.Equal(0, model.Property3);
            Assert.Equal(baseModel.Property4, model.Property4);
            Assert.Equal(baseModel.Property5a, model.Property5a);
            Assert.Equal(baseModel.Property5b, model.Property5b);
            Assert.Equal(baseModel.Property6a, model.Property6a);
            Assert.Equal(baseModel.Property6b, model.Property6b);
        }

        [Fact]
        public void StringGraph()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.Int32Thing,
                x => x.ClassThing.Value2
            );

            var model1 = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(model1, null, graph);
            var model2 = JsonSerializer.Deserialize<TypesAllModel>(json);
            AssertHelper.AreEqual(model1.Int32Thing, model2.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.NotNull(model2.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);

            var json2 = JsonSerializer.Serialize(model1);
            var model3 = JsonSerializer.Deserialize<TypesAllModel>(json2, null, graph);
            AssertHelper.AreEqual(model1.Int32Thing, model3.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model3.Int64Thing);
            Assert.NotNull(model3.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model3.ClassThing.Value2);
        }

        [Fact]
        public void StringInstanceGraph()
        {
            var graph = new Graph<TypesAllModel>(true);

            var model1a = TypesAllModel.Create();
            graph.AddInstanceGraph(model1a, new Graph<TypesAllModel>(
                x => x.Int32Thing,
                x => x.ClassThing.Value2
            ));

            var model1b = TypesAllModel.Create();
            graph.AddInstanceGraph(model1b, new Graph<TypesAllModel>(
                x => x.Int64Thing
            ));

            var jsona = JsonSerializer.Serialize(model1a, null, graph);
            var model2a = JsonSerializer.Deserialize<TypesAllModel>(jsona);
            AssertHelper.AreEqual(model1a.Int32Thing, model2a.Int32Thing);
            AssertHelper.AreNotEqual(model1a.Int64Thing, model2a.Int64Thing);
            Assert.NotNull(model2a.ClassThing);
            AssertHelper.AreEqual(model1a.ClassThing.Value2, model2a.ClassThing.Value2);

            var jsonb = JsonSerializer.Serialize(model1b, null, graph);
            var model2b = JsonSerializer.Deserialize<TypesAllModel>(jsonb);
            AssertHelper.AreNotEqual(model1b.Int32Thing, model2b.Int32Thing);
            AssertHelper.AreEqual(model1b.Int64Thing, model2b.Int64Thing);
            Assert.Null(model2b.ClassThing);

            var json2 = JsonSerializer.Serialize(model1a);
            var model3 = JsonSerializer.Deserialize<TypesAllModel>(json2, null, graph);
            AssertHelper.AreEqual(model1a, model3);
        }

        [Fact]
        public void StringJsonObject()
        {
            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel);
            var jsonObject = JsonSerializer.DeserializeJsonObject(json);

            var json2 = jsonObject.ToString();

            Assert.Equal(json, json2);

            //var model1 = jsonObject.Bind<TypesAllModel>();

            //AssertHelper.AreEqual(baseModel, model1);
        }

        [Fact]
        public void StringType()
        {
            var model1 = TypeModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypeModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void StringSeekArrayLengthEncoding()
        {
            var model = new string[]
            {
                "abcdefg]hijkl\"mnopqrstuvwxyz",
                "abcdefg\"hijklmnop}qrstuvwxyz",
                "abc{defghijklmn\"opq{rst}uvwxyz",
                "abcde[fg\"hijklmnop[qrs]tuvwxyz",
                "\"\"\"\"\"\"\"\"\"\"\"\"\""
            };

            var json = JsonSerializer.Serialize(model);
            var result = JsonSerializer.Deserialize<string[]>(json);

            Assert.Equal(model.Length, result.Length);
            for (var i = 0; i < model.Length; i++)
                Assert.Equal(model[i], result[i]);
        }

        [Fact]
        public void StringIgnoreCase()
        {
            var options = new JsonSerializerOptions()
            {
                IgnoreCase = true
            };

            var model = new SimpleModel()
            {
                Value1 = 5,
                Value2 = "123456789"
            };

            var json = JsonSerializer.Serialize(model);

            var jsonUpper = json.ToUpper();
            var result1 = JsonSerializer.Deserialize<SimpleModel>(jsonUpper, options);
            Assert.Equal(model.Value1, result1.Value1);
            Assert.Equal(model.Value2, result1.Value2);

            var jsonLower = json.ToUpper();
            var result2 = JsonSerializer.Deserialize<SimpleModel>(jsonLower, options);
            Assert.Equal(model.Value1, result2.Value1);
            Assert.Equal(model.Value2, result2.Value2);
        }

        [Fact]
        public void StringCustomType()
        {
            JsonSerializer.AddConverter(typeof(CustomType), () => new CustomTypeJsonConverter());

            var model1 = CustomTypeModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<CustomTypeModel>(json);
            Assert.NotNull(model2);
            Assert.NotNull(model2.Value);
            Assert.Equal(model1.Value.Things1, model2.Value.Things1);
            Assert.Equal(model1.Value.Things2, model2.Value.Things2);
        }

        [Fact]
        public void StringCancellationToken()
        {
            var model1 = CancellationToken.None;
            var json1 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<CancellationToken>(json1);
            AssertHelper.AreEqual(model1, model2);

            CancellationToken? model3 = CancellationToken.None;
            var json2 = JsonSerializer.Serialize(model3);
            var model4 = JsonSerializer.Deserialize<CancellationToken?>(json2);
            AssertHelper.AreEqual(model3, model4);

            CancellationToken? model5 = null;
            var json3 = JsonSerializer.Serialize(model5);
            var model6 = JsonSerializer.Deserialize<CancellationToken?>(json3);
            AssertHelper.AreEqual(model5, model6);
        }

        [Fact]
        public void StringConstructorParameters()
        {
            var model1 = new TestSerializerConstructor("Five", 5);
            var json1 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TestSerializerConstructor>(json1);
            Assert.NotNull(model2);
            Assert.Equal(model1._Value1, model2._Value1);
            Assert.Equal(model1.value2, model2.value2);
        }

        [Fact]
        public void StringPatch()
        {
            var model1 = TypesBasicModel.Create();
            var json = JsonSerializer.Serialize(model1);
            (var model2, var graph) = JsonSerializer.DeserializePatch<TypesAllModel>(json);

            var validMembers = typeof(TypesBasicModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToHashSet();
            foreach (var member in typeof(TypesAllModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (validMembers.Contains(member.Name))
                    Assert.True(graph.HasMember(member.Name));
                else
                    Assert.False(graph.HasMember(member.Name));
                if (member.Name == nameof(TypesBasicModel.ClassThing))
                {
                    var childGraph = graph.GetChildGraph(member.Name);
                    foreach (var childMember in typeof(SimpleModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        Assert.True(childGraph.HasMember(childMember.Name));
                }
            }
        }

        [Fact]
        public void StringPatchDictionary()
        {
            var model1 = new Dictionary<string, string>()
            {
                { "One", "Uno" },
                { "Two", "Dos" }
            };
            var json = JsonSerializer.Serialize(model1);
            (var model2, var graph) = JsonSerializer.DeserializePatch<Dictionary<string, string>>(json);
            Assert.True(graph.HasMember("One"));
            Assert.True(graph.HasMember("Two"));
        }

        [Fact]
        public void StringRequired()
        {
            var model1 = new TestSerializerRequired { Value1 = 1, Value2 = 2, Value3 = 3 };
            var bytes1 = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TestSerializerRequired>(bytes1);
            Assert.NotNull(model2);
            Assert.Equal(model1.Value1, model2.Value1);
            Assert.Equal(model1.Value2, model2.Value2);
            Assert.Equal(model1.Value3, model2.Value3);
        }

        [Fact]
        public void StringRequiredMissing()
        {
            var model = JsonSerializer.Deserialize<TestSerializerRequired>(@"{""Value1"":1,""Value3"":3}");
            Assert.NotNull(model);
            Assert.Equal(1, model.Value1);
            Assert.Equal(0, model.Value2);
            Assert.Equal(3, model.Value3);

            var empty = JsonSerializer.Deserialize<TestSerializerRequired>("{}");
            Assert.NotNull(empty);
            Assert.Equal(0, empty.Value1);
            Assert.Equal(0, empty.Value2);
            Assert.Equal(0, empty.Value3);
        }

        [Fact]
        public void StringDrainWithCreatedParent()
        {
            var json = @"{""key"":{""value"":""True""}}";
            var model = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            Assert.NotNull(model);
            Assert.Null(model["key"]);
        }

        [Fact]
        public async Task StreamMatchesNewtonsoft()
        {
            var baseModel = TypesAllModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync(TestContext.Current.CancellationToken);

            var json2 = Newtonsoft.Json.JsonConvert.SerializeObject(baseModel,
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());

            //swap serializers
            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, null, TestContext.Current.CancellationToken);
            var model2 = Newtonsoft.Json.JsonConvert.DeserializeObject<TypesAllModel>(json1,
                new Newtonsoft.Json.Converters.StringEnumConverter(),
                new NewtonsoftDateOnlyConverter(),
                new NewtonsoftTimeOnlyConverter());
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = TypesAllModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync(TestContext.Current.CancellationToken);

            using var stream2 = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream2, baseModel, options, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync(TestContext.Current.CancellationToken);

            Assert.True(json1 == json2);

            //swap serializers
            using var stream3 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream3, null, null, TestContext.Current.CancellationToken);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream4, options, TestContext.Current.CancellationToken);

            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypes()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesBasicModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesArray()
        {
            var model1 = TypesArrayModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesArrayModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesListT()
        {
            var model1 = TypesListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesListTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTOfTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyListTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIList()
        {
            var model1 = TypesIListModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListOfT()
        {
            var model1 = TypesIListOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListOfTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTOfTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlySetTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTOfTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyCollectionTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task StreamTypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableOfT()
        {
            var model1 = TypesIEnumerableOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task StreamTypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesDictionaryTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIDictionaryTOfT()
        {
            var model1 = TypesIDictionaryTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTOfTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyDictionaryTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        //[Fact]
        //public async Task StreamTypesIDictionary()
        //{
        //    var model1 = TypesIDictionaryModel.Create();
        //    using var stream = new MemoryStream();
        //    await JsonSerializer.SerializeAsync(stream, model1);
        //    stream.Position = 0;
        //    var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryModel>(stream);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        //[Fact]
        //public async Task StreamTypesIDictionaryOfT()
        //{
        //    var model1 = TypesIDictionaryOfTModel.Create();
        //    using var stream = new MemoryStream();
        //    await JsonSerializer.SerializeAsync(stream, model1);
        //    stream.Position = 0;
        //    var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryOfTModel>(stream);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        [Fact]
        public async Task StreamTypesCustomCollections()
        {
            var model1 = TypesCustomCollectionsModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesCustomCollectionsModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesOther()
        {
            var model1 = TypesOtherModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesOtherModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesCore()
        {
            var model1 = TypesCoreModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);

            var json = Encoding.UTF8.GetString(stream.ToArray());

            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesCoreModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesAll()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamEnumAsNumber()
        {
            var options = new JsonSerializerOptions()
            {
                EnumAsNumber = true
            };

            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            Assert.DoesNotContain(EnumModel.EnumItem0.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem1.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem2.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem3.EnumName(), json);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamConvertNullables()
        {
            var baseModel = BasicTypesNotNullable.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel, null, null, TestContext.Current.CancellationToken);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<BasicTypesNullable>(stream1, null, null, TestContext.Current.CancellationToken);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1, null, null, TestContext.Current.CancellationToken);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<BasicTypesNotNullable>(stream2, null, null, TestContext.Current.CancellationToken);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model2);
        }

        [Fact]
        public async Task StreamConvertTypes()
        {
            var baseModel = TypesAllModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel, null, null, TestContext.Current.CancellationToken);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllAsStringsModel>(stream1, null, null, TestContext.Current.CancellationToken);
            TypesAllAsStringsModel.AreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1, null, null, TestContext.Current.CancellationToken);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(baseModel, model2);
        }

        [Fact]
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
        private static async Task StreamTestNumber<T>(T value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<T>(stream);
            Assert.Equal(value, result);
        }
        private static async Task StreamTestNumberAsStream<T>(T value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            stream.Position = 0;
            var json = await sr.ReadToEndAsync();
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<string>(stream);
            Assert.Equal(json, result);
        }

        [Fact]
        public async Task StreamEnumConversion()
        {
            //var model1 = new EnumConversionModel1() { Thing = EnumModel.Item2 };
            //var test1 = JsonSerializer.Serialize(model1);
            //var result1 = JsonSerializer.Deserialize<EnumConversionModel2>(test1);
            //Assert.Equal((int)model1.Thing, result1.Thing);

            var model2 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = 2,
                Thing3 = 3,
                Thing4 = 4
            };

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model2, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream2, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(model2.Thing1, (int)result2.Thing1);
            Assert.Equal(model2.Thing2, (int?)result2.Thing2);
            Assert.Equal(model2.Thing3, (int)result2.Thing3);
            Assert.Equal(model2.Thing4, (int?)result2.Thing4);

            var model3 = new EnumConversionModel2()
            {
                Thing1 = 1,
                Thing2 = null,
                Thing3 = 3,
                Thing4 = null
            };

            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, model3, null, null, TestContext.Current.CancellationToken);
            stream3.Position = 0;
            var result3 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream3, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(model3.Thing1, (int)result3.Thing1);
            Assert.Equal(default, result3.Thing2);
            Assert.Equal(model3.Thing3, (int)result3.Thing3);
            Assert.Equal(model3.Thing4, (int?)result3.Thing4);
        }

        [Fact]
        public async Task StreamPretty()
        {
            var baseModel = TypesAllModel.Create();
            var json = System.Text.Json.JsonSerializer.Serialize(baseModel, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamNameless()
        {
            var options = new JsonSerializerOptions()
            {
                Nameless = true
            };

            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamDoNotWriteNullProperties()
        {
            var options = new JsonSerializerOptions()
            {
                DoNotWriteNullProperties = true
            };

            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, options, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamEmptys()
        {
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream1, null, null, null, TestContext.Current.CancellationToken);
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            stream1.Position = 0;
            var json1 = await sr1.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal("null", json1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream2, String.Empty, null, null, TestContext.Current.CancellationToken);
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            stream2.Position = 0;
            var json2 = await sr2.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal("\"\"", json2);

            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream3, null, null, null, TestContext.Current.CancellationToken);
            using var sr3 = new StreamReader(stream3, Encoding.UTF8);
            stream3.Position = 0;
            var json3 = await sr3.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal("null", json3);

            using var stream4 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream4, new object(), null, null, TestContext.Current.CancellationToken);
            using var sr4 = new StreamReader(stream4, Encoding.UTF8);
            stream4.Position = 0;
            var json4 = await sr4.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal("{}", json4);

            var model1 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("null")), null, null, TestContext.Current.CancellationToken);
            Assert.Null(model1);

            var model2 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("")), null, null, TestContext.Current.CancellationToken);
            Assert.Equal(String.Empty, model2);

            var model3 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")), null, null, TestContext.Current.CancellationToken);
            Assert.Equal(String.Empty, model3);

            var model4 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("{}")), null, null, TestContext.Current.CancellationToken);
            Assert.Null(model4);

            var model5 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("null")), null, null, TestContext.Current.CancellationToken);
            Assert.Null(model5);

            var model6 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("")), null, null, TestContext.Current.CancellationToken);
            Assert.Null(model6);

            var model7 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")), null, null, TestContext.Current.CancellationToken);
            Assert.Equal(String.Empty, model7);

            var model8 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("{}")), null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model8);

            var model9 = await JsonSerializer.DeserializeAsync<int>(new MemoryStream(Encoding.UTF8.GetBytes("")), null, null, TestContext.Current.CancellationToken);
            Assert.Equal(0, model9);

            var model10 = await JsonSerializer.DeserializeAsync<int?>(new MemoryStream(Encoding.UTF8.GetBytes("")), null, null, TestContext.Current.CancellationToken);
            Assert.Null(model10);

            await StreamEmptysNumbers<byte>();
            await StreamEmptysNumbers<sbyte>();
            await StreamEmptysNumbers<short>();
            await StreamEmptysNumbers<ushort>();
            await StreamEmptysNumbers<int>();
            await StreamEmptysNumbers<uint>();
            await StreamEmptysNumbers<long>();
            await StreamEmptysNumbers<ulong>();
            await StreamEmptysNumbers<float>();
            await StreamEmptysNumbers<double>();
            await StreamEmptysNumbers<decimal>();
        }
        private static async Task StreamEmptysNumbers<T>()
            where T : unmanaged
        {
            var model11 = await JsonSerializer.DeserializeAsync<T>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.Equal(default, model11);

            var model12 = await JsonSerializer.DeserializeAsync<T?>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.Null(model12);
        }

        [Fact]
        public async Task StreamDateTimeTypes()
        {
            var dateUtc = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Utc);
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, dateUtc, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var dateUtc2 = await JsonSerializer.DeserializeAsync<DateTime>(stream1, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(dateUtc, dateUtc2);
            Assert.Equal(DateTimeKind.Utc, dateUtc2.Kind);

            var dateLocal = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Local);
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, dateLocal, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var dateLocal2 = await JsonSerializer.DeserializeAsync<DateTime>(stream2, null, null, TestContext.Current.CancellationToken);
            var dateLocalUtc = dateLocal.ToUniversalTime();
            Assert.Equal(dateLocalUtc, dateLocal2);
            Assert.Equal(DateTimeKind.Utc, dateLocal2.Kind);

            var dateUnspecified = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Unspecified);
            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, dateUnspecified, null, null, TestContext.Current.CancellationToken);
            stream3.Position = 0;
            var dateUnspecified2 = await JsonSerializer.DeserializeAsync<DateTime>(stream3, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(dateUnspecified, dateUnspecified2);
            Assert.Equal(DateTimeKind.Utc, dateUnspecified2.Kind);
        }

        [Fact]
        public async Task StreamEscaping()
        {
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;

                var checker = Encoding.UTF8.GetBytes([c]);
                var check = Encoding.UTF8.GetString(checker);

                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, c, null, null, TestContext.Current.CancellationToken);
                using var sr = new StreamReader(stream, Encoding.UTF8);
                stream.Position = 0;
                var json = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);
                stream.Position = 0;
                var result = await JsonSerializer.DeserializeAsync<char>(stream, null, null, TestContext.Current.CancellationToken);
                Assert.Equal(c, result);

                switch (c)
                {
                    case '\\':
                    case '"':
                    case '\b':
                    case '\t':
                    case '\n':
                    case '\f':
                    case '\r':
                        Assert.Equal(4, json.Length);
                        break;
                    default:
                        if (c < ' ')
                            Assert.Equal(8, json.Length);
                        break;
                }

                var str = new string([c]);
                using var streamStr = new MemoryStream();
                await JsonSerializer.SerializeAsync(streamStr, str, null, null, TestContext.Current.CancellationToken);
                using var srStr = new StreamReader(streamStr, Encoding.UTF8);
                streamStr.Position = 0;
                json = await srStr.ReadToEndAsync(TestContext.Current.CancellationToken);
                streamStr.Position = 0;
                var resultStr = await JsonSerializer.DeserializeAsync<string>(streamStr, null, null, TestContext.Current.CancellationToken);
                Assert.Equal(str, resultStr);

                var strPadded = $"aa{c}bb";
                using var streamStrPadded = new MemoryStream();
                await JsonSerializer.SerializeAsync(streamStrPadded, strPadded, null, null, TestContext.Current.CancellationToken);
                using var srStrPadded = new StreamReader(streamStrPadded, Encoding.UTF8);
                streamStrPadded.Position = 0;
                json = await srStrPadded.ReadToEndAsync(TestContext.Current.CancellationToken);
                streamStrPadded.Position = 0;
                var resultStrPadded = await JsonSerializer.DeserializeAsync<string>(streamStrPadded, null, null, TestContext.Current.CancellationToken);
                Assert.Equal(strPadded, resultStrPadded);
            }

            //deserialize will include all unicode escapes, some serialize differently
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;
                using var charsLowerStream = new MemoryStream(Encoding.UTF8.GetBytes($"\"\\u{i:x4}\""));
                using var charsUpperStream = new MemoryStream(Encoding.UTF8.GetBytes($"\"\\u{i:X4}\""));
                using var charsPaddedStream = new MemoryStream(Encoding.UTF8.GetBytes($"\"aa\\u{i:X4}bb\""));

                var result = await JsonSerializer.DeserializeAsync<char>(charsLowerStream, null, null, TestContext.Current.CancellationToken);
                Assert.Equal(c, result);

                result = await JsonSerializer.DeserializeAsync<char>(charsUpperStream, null, null, TestContext.Current.CancellationToken);
                Assert.Equal(c, result);

                var resultStr = await JsonSerializer.DeserializeAsync<string>(charsPaddedStream, null, null, TestContext.Current.CancellationToken);
                Assert.Equal($"aa{c}bb", resultStr);
            }
        }

        [Fact]
        public async Task StreamExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<Exception>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(model1.Message, model2.Message);
        }

        [Fact]
        public async Task StreamInterface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<ITestInterface>(stream, null, null, TestContext.Current.CancellationToken);

            Assert.Equal(5, model2.Property1);
            Assert.Equal(6, model2.Property2);
        }

        [Fact]
        public async Task StreamEmptyModel()
        {
            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<EmptyModel>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task StreamGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<GetsSetsModel>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task StreamDrainModel()
        {
            var model1 = TypesCoreAlternatingModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var result1 = await JsonSerializer.DeserializeAsync<TypesCoreModel>(stream1, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, result1);

            var model2 = TypesCoreModel.Create();
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model2, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<TypesCoreAlternatingModel>(stream2, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(result2, model2);
        }

        [Fact]
        public async Task StreamLargeModel()
        {
            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, models, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<TypesAllModel[]>(stream, null, null, TestContext.Current.CancellationToken);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [Fact]
        public async Task StreamRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<RecordModel>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model);
            Assert.Equal(baseModel.Property1, model.Property1);
            Assert.Equal(baseModel.Property2, model.Property2);
            Assert.Equal(baseModel.Property3, model.Property3);
        }

        [Fact]
        public async Task StreamHashSet()
        {
            var model1 = TypesHashSetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamBoxing()
        {
            var baseModel = TestBoxingModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TestBoxingModel>(stream, null, null, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task StreamPropertyNameAttribute()
        {
            var baseModel = JsonPropertyNameAttributeTestModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

            Assert.Contains("\"1property\"", json);
            Assert.Contains("\"property2\"", json);
            Assert.Contains("\"3property\"", json);

            _ = json.Replace("\"property2\"", "\"PROPERTY2\"");

            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializer.DeserializeAsync<JsonPropertyNameAttributeTestModel>(stream2, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(baseModel._1_Property, model._1_Property);
            Assert.Equal(baseModel.property2, model.property2);
            Assert.NotNull(model._3_Property);
            Assert.Equal(baseModel._3_Property.Value1, model._3_Property.Value1);
            Assert.Equal(baseModel._3_Property.Value2, model._3_Property.Value2);
        }

        [Fact]
        public async Task StreamIgnoreAttribute()
        {
            var baseModel = JsonIgnoreAttributeTestModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

            Assert.Contains("\"Property1\"", json);
            Assert.DoesNotContain("\"Property2\"", json);
            Assert.Contains("\"Property3\"", json);
            Assert.DoesNotContain("\"Property4\"", json);
            Assert.Contains("\"Property5a\"", json);
            Assert.DoesNotContain("\"Property5b\"", json);
            Assert.Contains("\"Property6a\"", json);
            Assert.DoesNotContain("\"Property6b\"", json);

            using var stream2 = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream2, baseModel, cancellationToken: TestContext.Current.CancellationToken);
            stream2.Position = 0;
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync(TestContext.Current.CancellationToken);

            var model = JsonSerializer.Deserialize<JsonIgnoreAttributeTestModel>(json2);
            Assert.Equal(baseModel.Property1, model.Property1);
            Assert.Equal(0, model.Property2);
            Assert.Equal(0, model.Property3);
            Assert.Equal(baseModel.Property4, model.Property4);
            Assert.Equal(baseModel.Property5a, model.Property5a);
            Assert.Equal(baseModel.Property5b, model.Property5b);
            Assert.Equal(baseModel.Property6a, model.Property6a);
            Assert.Equal(baseModel.Property6b, model.Property6b);
        }

        [Fact]
        public async Task StreamGraph()
        {
            var graph = new Graph<TypesAllModel>(
                x => x.Int32Thing,
                x => x.ClassThing.Value2
            );

            var model1 = TypesAllModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1, null, graph, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1.Int32Thing, model2.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.NotNull(model2.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1.Int32Thing, model3.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model3.Int64Thing);
            Assert.NotNull(model3.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model3.ClassThing.Value2);
        }

        [Fact]
        public async Task StreamInstanceGraph()
        {
            var graph = new Graph<TypesAllModel>(true);

            var model1a = TypesAllModel.Create();
            graph.AddInstanceGraph(model1a, new Graph<TypesAllModel>(
                x => x.Int32Thing,
                x => x.ClassThing.Value2
            ));

            var model1b = TypesAllModel.Create();
            graph.AddInstanceGraph(model1b, new Graph<TypesAllModel>(
                x => x.Int64Thing
            ));

            using var stream1a = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1a, model1a, null, graph, TestContext.Current.CancellationToken);
            stream1a.Position = 0;
            var model2a = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1a, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1a.Int32Thing, model2a.Int32Thing);
            AssertHelper.AreNotEqual(model1a.Int64Thing, model2a.Int64Thing);
            Assert.NotNull(model2a.ClassThing);
            AssertHelper.AreEqual(model1a.ClassThing.Value2, model2a.ClassThing.Value2);

            using var stream1b = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1b, model1b, null, graph, TestContext.Current.CancellationToken);
            stream1b.Position = 0;
            var model2b = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1b, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreNotEqual(model1b.Int32Thing, model2b.Int32Thing);
            AssertHelper.AreEqual(model1b.Int64Thing, model2b.Int64Thing);
            Assert.Null(model2b.ClassThing);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1a, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1a, model3);
        }

        [Fact]
        public async Task StreamJsonObject()
        {
            var baseModel = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var json = Encoding.UTF8.GetString(stream.ToArray());
            stream.Position = 0;
            var jsonObject = await JsonSerializer.DeserializeJsonObjectAsync(stream, null, null, TestContext.Current.CancellationToken);

            var json2 = jsonObject.ToString();

            Assert.Equal(json, json2);

            //var model1 = jsonObject.Bind<TypesAllModel>();

            //AssertHelper.AreEqual(baseModel, model1);
        }

        [Fact]
        public async Task StreamType()
        {
            var model1 = TypeModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypeModel>(stream, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamSeekArrayLengthEncoding()
        {
            var model = new string[]
            {
                "abcdefg]hijkl\"mnopqrstuvwxyz",
                "abcdefg\"hijklmnop}qrstuvwxyz",
                "abc{defghijklmn\"opq{rst}uvwxyz",
                "abcde[fg\"hijklmnop[qrs]tuvwxyz",
                "\"\"\"\"\"\"\"\"\"\"\"\"\""
            };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<string[]>(stream, null, null, TestContext.Current.CancellationToken);

            Assert.Equal(model.Length, result.Length);
            for (var i = 0; i < model.Length; i++)
                Assert.Equal(model[i], result[i]);
        }

        [Fact]
        public async Task StreamIgnoreCase()
        {
            var options = new JsonSerializerOptions()
            {
                IgnoreCase = true
            };

            var model = new SimpleModel()
            {
                Value1 = 5,
                Value2 = "123456789"
            };

            var json = JsonSerializer.Serialize(model);

            using var streamUpper = new MemoryStream(Encoding.UTF8.GetBytes(json.ToUpper()));
            var result1 = await JsonSerializer.DeserializeAsync<SimpleModel>(streamUpper, options, null, TestContext.Current.CancellationToken);
            Assert.Equal(model.Value1, result1.Value1);
            Assert.Equal(model.Value2, result1.Value2);

            using var streamLower = new MemoryStream(Encoding.UTF8.GetBytes(json.ToUpper()));
            var result2 = await JsonSerializer.DeserializeAsync<SimpleModel>(streamLower, options, null, TestContext.Current.CancellationToken);
            Assert.Equal(model.Value1, result2.Value1);
            Assert.Equal(model.Value2, result2.Value2);
        }

        [Fact]
        public async Task StreamCustomType()
        {
            JsonSerializer.AddConverter(typeof(CustomType), () => new CustomTypeJsonConverter());

            var model1 = CustomTypeModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<CustomTypeModel>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model2);
            Assert.NotNull(model2.Value);
            Assert.Equal(model1.Value.Things1, model2.Value.Things1);
            Assert.Equal(model1.Value.Things2, model2.Value.Things2);
        }

        [Fact]
        public async Task StreamCancellationToken()
        {
            var model1 = CancellationToken.None;
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<CancellationToken>(stream1, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model1, model2);

            CancellationToken? model3 = CancellationToken.None;
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model3, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var model4 = await JsonSerializer.DeserializeAsync<CancellationToken?>(stream2, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model3, model4);

            CancellationToken? model5 = null;
            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, model5, null, null, TestContext.Current.CancellationToken);
            stream3.Position = 0;
            var model6 = await JsonSerializer.DeserializeAsync<CancellationToken?>(stream3, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model5, model6);
        }

        [Fact]
        public async Task StreamConstructorParameters()
        {
            var model1 = new TestSerializerConstructor("Five", 5);
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TestSerializerConstructor>(stream1, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model2);
            Assert.Equal(model1._Value1, model2._Value1);
            Assert.Equal(model1.value2, model2.value2);
        }

        [Fact]
        public async Task StreamPatch()
        {
            var model1 = TypesBasicModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            (var model2, var graph) = await JsonSerializer.DeserializePatchAsync<TypesAllModel>(stream1, null, null, TestContext.Current.CancellationToken);

            var validMembers = typeof(TypesBasicModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToHashSet();
            foreach (var member in typeof(TypesAllModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (validMembers.Contains(member.Name))
                    Assert.True(graph.HasMember(member.Name));
                else
                    Assert.False(graph.HasMember(member.Name));
                if (member.Name == nameof(TypesBasicModel.ClassThing))
                {
                    var childGraph = graph.GetChildGraph(member.Name);
                    foreach (var childMember in typeof(SimpleModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        Assert.True(childGraph.HasMember(childMember.Name));
                }
            }
        }

        [Fact]
        public async Task StreamPatchDictionary()
        {
            var model1 = new Dictionary<string, string>()
            {
                { "One", "Uno" },
                { "Two", "Dos" }
            };
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            (var model2, var graph) = await JsonSerializer.DeserializePatchAsync<Dictionary<string, string>>(stream1, null, null, TestContext.Current.CancellationToken);
            Assert.True(graph.HasMember("One"));
            Assert.True(graph.HasMember("Two"));
        }

        [Fact]
        public async Task StreamRequired()
        {
            var model1 = new TestSerializerRequired { Value1 = 1, Value2 = 2, Value3 = 3 };
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1, null, null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TestSerializerRequired>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model2);
            Assert.Equal(model1.Value1, model2.Value1);
            Assert.Equal(model1.Value2, model2.Value2);
            Assert.Equal(model1.Value3, model2.Value3);
        }

        [Fact]
        public async Task StreamDrainWithCreatedParent()
        {
            var json = @"{""key"":{""value"":""True""}}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, null, null, TestContext.Current.CancellationToken);
            Assert.NotNull(model);
            Assert.Null(model["key"]);
        }

        [Fact]
        public async Task LargeValueToExpandBuffer()
        {
            var model = new string('x', 10000);

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model, null, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var result1 = await JsonSerializer.DeserializeAsync<string>(stream1, null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model, null, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync(stream2, typeof(string), null, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result2);

            using var stream3 = new MemoryStream();
            JsonSerializer.Serialize(stream3, model);
            stream3.Position = 0;
            var result3 = JsonSerializer.Deserialize<string>(stream3);
            AssertHelper.AreEqual(model, result3);

            using var stream4 = new MemoryStream();
            JsonSerializer.Serialize(stream4, model);
            stream4.Position = 0;
            var result4 = JsonSerializer.Deserialize(stream4, typeof(string));
            AssertHelper.AreEqual(model, result4);
        }

        [Fact]
        public void GraphMembers()
        {
            var model1 = new Graph<GraphModel>(x => x.Prop1, x => x.Class.Value1);
            model1.RemoveMember(nameof(GraphModel.Prop2));

            //a graph is written as its signature
            var json = JsonSerializer.Serialize(model1);
            Assert.Equal($"\"{model1.Signature}\"", json);

            var model2 = JsonSerializer.Deserialize<Graph<GraphModel>>(json);

            Assert.NotNull(model2);
            Assert.Equal(model1, model2);
            Assert.True(model2.HasMember(nameof(GraphModel.Prop1)));
            Assert.False(model2.HasMember(nameof(GraphModel.Prop2)));
            var childGraph = model2.GetChildGraph(nameof(GraphModel.Class));
            Assert.NotNull(childGraph);
            Assert.True(childGraph.HasMember(nameof(SimpleModel.Value1)));
        }

        [Fact]
        public void GraphAllMembers()
        {
            var model1 = new Graph(true);

            var json = JsonSerializer.Serialize(model1);
            Assert.Equal("\"A:\"", json);

            var model2 = JsonSerializer.Deserialize<Graph>(json);

            Assert.NotNull(model2);
            Assert.Equal(model1, model2);
            Assert.True(model2.IncludeAllMembers);
        }

        [Fact]
        public void GraphNull()
        {
            Graph? model1 = null;

            var json = JsonSerializer.Serialize(model1);
            Assert.Equal("null", json);

            Assert.Null(JsonSerializer.Deserialize<Graph>(json));
        }

        [Fact]
        public void TypeMismatch_CoreTypes()
        {
            string[] none = [];
            string[] fraction = ["5.5"];
            string[] unsignedNumber = ["-5", "5.5"];
            string[] text = ["\"xyz\"", "\"\""];
            string[] enumValue = ["-5", "5.5", "\"xyz\"", "\"\""];
            string[] base64 = ["\"xyz\""];

            AssertTypeMismatch<bool>(none);
            AssertTypeMismatch<byte>(unsignedNumber);
            AssertTypeMismatch<sbyte>(fraction);
            AssertTypeMismatch<short>(fraction);
            AssertTypeMismatch<ushort>(unsignedNumber);
            AssertTypeMismatch<int>(fraction);
            AssertTypeMismatch<uint>(unsignedNumber);
            AssertTypeMismatch<long>(fraction);
            AssertTypeMismatch<ulong>(unsignedNumber);
            AssertTypeMismatch<float>(none);
            AssertTypeMismatch<double>(none);
            AssertTypeMismatch<decimal>(none);
            AssertTypeMismatch<char>(text);
            AssertTypeMismatch<DateTime>(text);
            AssertTypeMismatch<DateTimeOffset>(text);
            AssertTypeMismatch<TimeSpan>(text);
            AssertTypeMismatch<DateOnly>(text);
            AssertTypeMismatch<TimeOnly>(text);
            AssertTypeMismatch<Guid>(text);
            AssertTypeMismatch<string>(none);

            AssertTypeMismatch<bool?>(none);
            AssertTypeMismatch<byte?>(unsignedNumber);
            AssertTypeMismatch<sbyte?>(fraction);
            AssertTypeMismatch<short?>(fraction);
            AssertTypeMismatch<ushort?>(unsignedNumber);
            AssertTypeMismatch<int?>(fraction);
            AssertTypeMismatch<uint?>(unsignedNumber);
            AssertTypeMismatch<long?>(fraction);
            AssertTypeMismatch<ulong?>(unsignedNumber);
            AssertTypeMismatch<float?>(none);
            AssertTypeMismatch<double?>(none);
            AssertTypeMismatch<decimal?>(none);
            AssertTypeMismatch<char?>(text);
            AssertTypeMismatch<DateTime?>(text);
            AssertTypeMismatch<DateTimeOffset?>(text);
            AssertTypeMismatch<TimeSpan?>(text);
            AssertTypeMismatch<DateOnly?>(text);
            AssertTypeMismatch<TimeOnly?>(text);
            AssertTypeMismatch<Guid?>(text);
            AssertTypeMismatch<EnumModel>(enumValue);
            AssertTypeMismatch<EnumModel?>(enumValue);

            AssertTypeMismatch<Type>(text, false);
            AssertTypeMismatch<byte[]>(base64, false);
            AssertTypeMismatch<CancellationToken>(none, false);
            AssertTypeMismatch<CancellationToken?>(none, false);
        }

        private static void AssertTypeMismatch<T>(string[] formatErrors, bool strictMismatches = true)
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            string[] tokens = ["null", "5", "-5", "5.5", "true", "false", "\"xyz\"", "\"\"", "{\"a\":[1,{\"b\":\"c\"}],\"d\":null}", "[1,\"two\",{\"three\":[3]},[],null,true]"];

            foreach (var token in tokens)
            {
                var json = $"{{\"Value\":{token},\"After\":7}}";
                if (formatErrors.Contains(token))
                {
                    _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<MismatchModel<T>>(json));
                    _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<MismatchModel<T>>(Encoding.UTF8.GetBytes(json)));
                    continue;
                }
                Assert.Equal(7, JsonSerializer.Deserialize<MismatchModel<T>>(json)!.After);
                Assert.Equal(7, JsonSerializer.Deserialize<MismatchModel<T>>(Encoding.UTF8.GetBytes(json))!.After);
            }

            if (!strictMismatches)
                return;

            string[] mismatches = typeof(T) == typeof(string) ? tokens[8..] : Nullable.GetUnderlyingType(typeof(T)) is not null ? [tokens[6], .. tokens[8..]] : tokens[6..];
            foreach (var token in mismatches)
            {
                var json = $"{{\"Value\":{token},\"After\":7}}";
                Assert.True(Record.Exception(() => JsonSerializer.Deserialize<MismatchModel<T>>(json, strict)) is not null, $"{typeof(T).Name} {token} chars");
                Assert.True(Record.Exception(() => JsonSerializer.Deserialize<MismatchModel<T>>(Encoding.UTF8.GetBytes(json), strict)) is not null, $"{typeof(T).Name} {token} bytes");
            }

            if (default(T) is null)
            {
                var json = "{\"Value\":null,\"After\":7}";
                Assert.Null(JsonSerializer.Deserialize<MismatchModel<T>>(json, strict)!.Value);
                Assert.Null(JsonSerializer.Deserialize<MismatchModel<T>>(Encoding.UTF8.GetBytes(json), strict)!.Value);
            }
        }

        [Theory]
        [InlineData("\"a\"", 'a')]
        [InlineData("\"\\n\"", '\n')]
        [InlineData("\"\\\"\"", '"')]
        [InlineData("\"\\u00e9\"", 'é')]
        [InlineData("\"é\"", 'é')]
        [InlineData("5", '5')]
        public void Char_Decodes(string json, char expected)
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            var bytes = Encoding.UTF8.GetBytes(json);

            Assert.Equal(expected, JsonSerializer.Deserialize<char>(json));
            Assert.Equal(expected, JsonSerializer.Deserialize<char>(bytes));
            Assert.Equal(expected, JsonSerializer.Deserialize<char?>(json));
            Assert.Equal(expected, JsonSerializer.Deserialize<char?>(bytes));

            if (json[0] == '"')
            {
                Assert.Equal(expected, JsonSerializer.Deserialize<char>(json, strict));
                Assert.Equal(expected, JsonSerializer.Deserialize<char>(bytes, strict));
                Assert.Equal(expected, JsonSerializer.Deserialize<char?>(json, strict));
                Assert.Equal(expected, JsonSerializer.Deserialize<char?>(bytes, strict));
            }
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"ab\"")]
        [InlineData("\"😀\"")]
        public void Char_InvalidString(string json)
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            var bytes = Encoding.UTF8.GetBytes(json);
            foreach (var options in new[] { null, strict })
            {
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<char>(json, options));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<char>(bytes, options));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<char?>(json, options));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<char?>(bytes, options));
            }
        }

        [Theory]
        [InlineData("55")]
        [InlineData("true")]
        public void Char_Mismatch(string json)
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            var bytes = Encoding.UTF8.GetBytes(json);

            Assert.Equal(default, JsonSerializer.Deserialize<char>(json));
            Assert.Equal(default, JsonSerializer.Deserialize<char>(bytes));
            Assert.Null(JsonSerializer.Deserialize<char?>(json));
            Assert.Null(JsonSerializer.Deserialize<char?>(bytes));

            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<char>(json, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<char>(bytes, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<char?>(json, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<char?>(bytes, strict));
        }

        [Fact]
        public async Task StreamLargerThanBuffer_AllTypes()
        {
            await AssertStreamRoundTrip(Enumerable.Range(0, 8).Select(_ => TypesAllModel.Create()).ToArray());
            await AssertStreamRoundTrip(SpecialTypesModel.CreateArray(2000));
        }

        private static async Task AssertStreamRoundTrip<T>(T value)
        {
            var token = TestContext.Current.CancellationToken;
            var expected = JsonSerializer.Serialize(value);
            Assert.True(expected.Length > 3 * 16 * 1024, typeof(T).Name);

            using (var stream = new MemoryStream())
            {
                JsonSerializer.Serialize(stream, value);
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }
            using (var stream = new MemoryStream())
            {
                JsonSerializer.Serialize(stream, (object?)value, typeof(T));
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }
            using (var stream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(stream, value, cancellationToken: token);
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }
            using (var stream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(stream, (object?)value, typeof(T), cancellationToken: token);
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }

            var bytes = Encoding.UTF8.GetBytes(expected);
            var reread = JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(expected));
            Assert.Equal(reread, JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(new MemoryStream(bytes))));
            Assert.Equal(reread, JsonSerializer.Serialize((T?)JsonSerializer.Deserialize(new MemoryStream(bytes), typeof(T))));
            Assert.Equal(reread, JsonSerializer.Serialize(await JsonSerializer.DeserializeAsync<T>(new MemoryStream(bytes), cancellationToken: token)));
            Assert.Equal(reread, JsonSerializer.Serialize((T?)await JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(T), cancellationToken: token)));
        }

        [Fact]
        public void ByteArray_ReadsEscapedBase64()
        {
            var value = Enumerable.Range(0, 300).Select(x => (byte)(x * 37)).ToArray();
            var plain = System.Text.Json.JsonSerializer.Serialize(value);
            Assert.Contains("+", plain);
            Assert.Contains("/", plain);
            var json = plain.Replace("+", "\\u002B").Replace("/", "\\/");

            Assert.Equal(value, JsonSerializer.Deserialize<byte[]>(plain));
            Assert.Equal(value, JsonSerializer.Deserialize<byte[]>(Encoding.UTF8.GetBytes(plain)));
            Assert.Equal(value, JsonSerializer.Deserialize<byte[]>(json));
            Assert.Equal(value, JsonSerializer.Deserialize<byte[]>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(value, JsonSerializer.Deserialize<byte[]>(JsonSerializer.Serialize(value)));
        }

        [Theory]
        [InlineData("\"not base64!\"")]
        [InlineData("\"=\"")]
        [InlineData("\"QUJD=\"")]
        public void ByteArray_Invalid(string json)
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<byte[]>(json));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<byte[]>(Encoding.UTF8.GetBytes(json)));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<byte[]>(json, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<byte[]>(Encoding.UTF8.GetBytes(json), strict));
        }

        [Theory]
        [InlineData("2024-01-0")]
        [InlineData("2024-01-01T10:00:00.0000000+00:00:00:00")]
        [InlineData("x024-01-01")]
        [InlineData("2024x01-01")]
        [InlineData("2024-x1-01")]
        [InlineData("2024-01x01")]
        [InlineData("2024-01-x1")]
        [InlineData("2024-01-01T10")]
        [InlineData("2024-01-01x10:00:00")]
        [InlineData("2024-01-01Tx0:00:00")]
        [InlineData("2024-01-01T10x00:00")]
        [InlineData("2024-01-01T10:x0:00")]
        [InlineData("2024-01-01T10:00x00")]
        [InlineData("2024-01-01T10:00:x0")]
        [InlineData("2024-01-01T10:00:00.")]
        [InlineData("2024-01-01T10:00:00.x")]
        [InlineData("2024-01-01T10:00:00.1x")]
        [InlineData("2024-01-01T10:00:00Zx")]
        [InlineData("2024-01-01T10:00:00.1Zx")]
        [InlineData("2024-01-01T10:00:00x")]
        [InlineData("2024-01-01T10:00:00+05")]
        [InlineData("2024-01-01T10:00:00+x5:00")]
        [InlineData("2024-01-01T10:00:00+05x00")]
        [InlineData("2024-01-01T10:00:00+05:x0")]
        [InlineData("2024-01-01T10:00:00.1+05:00x")]
        [InlineData("2024-13-01")]
        [InlineData("2024-02-30")]
        [InlineData("2024-01-01T25:00:00")]
        [InlineData("2024-01-01T10:60:00")]
        [InlineData("2024-01-01T10:00:00+15:00")]
        [InlineData("2024-00-01")]
        [InlineData("0000-01-01")]
        [InlineData("0001-01-01T00:00:00+01:00")]
        [InlineData("9999-12-31T23:59:59-01:00")]
        public void Dates_Invalid(string text)
        {
            var json = $"\"{text}\"";
            var bytes = Encoding.UTF8.GetBytes(json);
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };

            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateTime>(json));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateTime>(bytes));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateTimeOffset>(json));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateTimeOffset>(bytes));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateOnly>(json));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateOnly>(bytes));

            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<DateTime>(json, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<DateTime>(bytes, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<DateTimeOffset>(json, strict));
            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<DateTimeOffset>(bytes, strict));
        }

        [Theory]
        [InlineData("2024-01-02")]
        [InlineData("2024-01-02T03:04:05")]
        [InlineData("2024-01-02T03:04:05Z")]
        [InlineData("2024-01-02T03:04:05.1Z")]
        [InlineData("2024-01-02T03:04:05.12")]
        [InlineData("2024-01-02T03:04:05.1234567")]
        [InlineData("2024-01-02T03:04:05.12345678")]
        [InlineData("2024-01-02T03:04:05.1234567Z")]
        [InlineData("2024-01-02T03:04:05+05:30")]
        [InlineData("2024-01-02T03:04:05-08:00")]
        [InlineData("2024-01-02T03:04:05.123-08:00")]
        [InlineData("2024-01-02T03:04:05.1234567890123456+05:30")]
        public void Dates_Valid(string text)
        {
            var json = $"\"{text}\"";
            var bytes = Encoding.UTF8.GetBytes(json);
            var hasOffset = text.Length > 19 && (text.EndsWith('Z') || text[^6] is '+' or '-');
            var expected = hasOffset
                ? DateTimeOffset.Parse(text, CultureInfo.InvariantCulture)
                : new DateTimeOffset(DateTime.Parse(text, CultureInfo.InvariantCulture), TimeSpan.Zero);
            var expectedTicks = expected.Ticks - expected.Ticks % 10;

            Assert.Equal(expected.UtcDateTime.Ticks - expected.UtcDateTime.Ticks % 10, JsonSerializer.Deserialize<DateTime>(json).Ticks - JsonSerializer.Deserialize<DateTime>(json).Ticks % 10);
            Assert.Equal(expected.UtcDateTime.Ticks - expected.UtcDateTime.Ticks % 10, JsonSerializer.Deserialize<DateTime>(bytes).Ticks - JsonSerializer.Deserialize<DateTime>(bytes).Ticks % 10);
            Assert.Equal(expected.Offset, JsonSerializer.Deserialize<DateTimeOffset>(json).Offset);
            Assert.Equal(expected.Offset, JsonSerializer.Deserialize<DateTimeOffset>(bytes).Offset);
            Assert.Equal(expectedTicks, JsonSerializer.Deserialize<DateTimeOffset>(json).Ticks - JsonSerializer.Deserialize<DateTimeOffset>(json).Ticks % 10);
            Assert.Equal(expectedTicks, JsonSerializer.Deserialize<DateTimeOffset>(bytes).Ticks - JsonSerializer.Deserialize<DateTimeOffset>(bytes).Ticks % 10);
        }

        [Fact]
        public void AttributeCombinations()
        {
            var model = new JsonAttributeCombinationsModel()
            {
                NameThenIgnore = 1,
                IgnoreThenName = 2,
                NameThenWhenNull = null,
                StjName = 3,
                StjIgnore = 4,
                StjWhenNull = null,
                StjNever = 5,
                StjWhenDefault = 0,
                Kept = 7,
                NonSerializedField = 6
            };
            Assert.Equal("{\"stj\":3,\"StjNever\":5,\"Kept\":7}", JsonSerializer.Serialize(model));

            model.NameThenWhenNull = "n";
            model.StjWhenNull = "s";
            model.StjWhenDefault = 8;
            Assert.Equal("{\"named\":\"n\",\"stj\":3,\"StjWhenNull\":\"s\",\"StjNever\":5,\"StjWhenDefault\":8,\"Kept\":7}", JsonSerializer.Serialize(model));

            const string json = "{\"renamed\":9,\"x\":9,\"NameThenIgnore\":9,\"IgnoreThenName\":9,\"named\":\"n\",\"stj\":8,\"StjIgnore\":8,\"StjWhenNull\":\"s\",\"StjNever\":8,\"StjWhenDefault\":8,\"NonSerializedField\":8,\"Kept\":8}";
            var result = JsonSerializer.Deserialize<JsonAttributeCombinationsModel>(json)!;
            Assert.Equal(0, result.NameThenIgnore);
            Assert.Equal(0, result.IgnoreThenName);
            Assert.Equal("n", result.NameThenWhenNull);
            Assert.Equal(8, result.StjName);
            Assert.Equal(0, result.StjIgnore);
            Assert.Equal("s", result.StjWhenNull);
            Assert.Equal(8, result.StjNever);
            Assert.Equal(8, result.StjWhenDefault);
            Assert.Equal(0, result.NonSerializedField);
            Assert.Equal(8, result.Kept);
        }

        [Fact]
        public async Task ObjectValues()
        {
            var token = TestContext.Current.CancellationToken;
            foreach (var (json, check) in new (string, Action<object?>)[]
            {
                ("\"text\"", x => Assert.Equal("text", x)),
                ("5.5", x => Assert.Equal(5.5m, x)),
                ("true", x => Assert.Equal(true, x)),
                ("false", x => Assert.Equal(false, x)),
                ("null", Assert.Null),
                ("[1,\"a\",true,[2],null]", x =>
                {
                    var array = Assert.IsType<object[]>(x);
                    Assert.Equal(1m, array[0]);
                    Assert.Equal("a", array[1]);
                    Assert.Equal(true, array[2]);
                    Assert.Equal(2m, Assert.IsType<object[]>(array[3])[0]);
                    Assert.Null(array[4]);
                }),
            })
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                check(JsonSerializer.Deserialize<object>(json));
                check(JsonSerializer.Deserialize<object>(bytes));
                check(JsonSerializer.Deserialize(json, typeof(object)));
                check(JsonSerializer.Deserialize(bytes, typeof(object)));
                check(JsonSerializer.Deserialize<object>(new MemoryStream(bytes)));
                check(await JsonSerializer.DeserializeAsync<object>(new MemoryStream(bytes), cancellationToken: token));
                check(await JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(object), cancellationToken: token));

                var member = $"{{\"Value\":{json},\"After\":7}}";
                var model = JsonSerializer.Deserialize<MismatchModel<object>>(member)!;
                check(model.Value);
                Assert.Equal(7, model.After);
                model = JsonSerializer.Deserialize<MismatchModel<object>>(Encoding.UTF8.GetBytes(member))!;
                check(model.Value);
                Assert.Equal(7, model.After);
            }
        }

        [Fact]
        public void InterfaceValues()
        {
            const string json = "{\"Value1\":42,\"Value2\":\"Hello\"}";
            Assert.NotNull(JsonSerializer.Deserialize<IBasicModel>(json));
            Assert.NotNull(JsonSerializer.Deserialize<IBasicModel>(Encoding.UTF8.GetBytes(json)));
            Assert.NotNull(JsonSerializer.Deserialize(json, typeof(IBasicModel)));
            Assert.NotNull(JsonSerializer.Deserialize<MismatchModel<IBasicModel>>($"{{\"Value\":{json},\"After\":7}}")!.Value);
        }

        [Theory]
        [InlineData("nulx")]
        [InlineData("trux")]
        [InlineData("falsx")]
        [InlineData("[nul]")]
        [InlineData("[tru]")]
        [InlineData("[fals]")]
        [InlineData("[1,]x")]
        [InlineData("{\"a\"}")]
        [InlineData("{\"a\":1,}x")]
        [InlineData("\"abc")]
        [InlineData("\"ab\\")]
        [InlineData("\"ab\\u00")]
        [InlineData("[\"a\\u0")]
        [InlineData("[1")]
        [InlineData("{\"a\":")]
        [InlineData("@")]
        [InlineData("[,1]")]
        [InlineData("[1,,2]")]
        [InlineData("{\"a\":,1}")]
        [InlineData("{\"a\"::1}")]
        [InlineData(",")]
        [InlineData(":")]
        public async Task InvalidJson_Throws(string json)
        {
            //malformed JSON is a FormatException, JSON that ends early is an EndOfStreamException
            var truncated = json is "\"abc" or "\"ab\\" or "\"ab\\u00" or "[\"a\\u0" or "[1" or "{\"a\":";
            void AssertInvalid(Action action) { if (truncated) _ = Assert.Throws<EndOfStreamException>(action); else _ = Assert.Throws<FormatException>(action); }
            async Task AssertInvalidAsync(Func<Task> action) { if (truncated) _ = await Assert.ThrowsAsync<EndOfStreamException>(action); else _ = await Assert.ThrowsAsync<FormatException>(action); }
            var bytes = Encoding.UTF8.GetBytes(json);
            AssertInvalid(() => JsonSerializer.Deserialize<object>(json));
            AssertInvalid(() => JsonSerializer.Deserialize<object>(bytes));
            AssertInvalid(() => JsonSerializer.Deserialize<object>(new MemoryStream(bytes)));
            await AssertInvalidAsync(() => JsonSerializer.DeserializeAsync<object>(new MemoryStream(bytes), cancellationToken: TestContext.Current.CancellationToken));
            AssertInvalid(() => JsonSerializer.DeserializeJsonObject(json));
            AssertInvalid(() => JsonSerializer.DeserializeJsonObject(bytes));
            AssertInvalid(() => JsonSerializer.Deserialize<SimpleModel[]>(json));
            AssertInvalid(() => JsonSerializer.Deserialize<SimpleModel[]>(bytes));
        }

        [Fact]
        public async Task EscapedStringsAcrossStreamBuffer()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < 12_000; i++)
                sb.Append((i % 5) switch { 0 => '"', 1 => '\\', 2 => '\n', 3 => '\u0001', _ => 'é' });
            var value = new[] { sb.ToString(), "plain", sb.ToString(0, 7_001) };

            var json = JsonSerializer.Serialize(value);
            Assert.True(json.Length > 3 * 16 * 1024);
            Assert.Equal(value, JsonSerializer.Deserialize<string[]>(json));
            Assert.Equal(value, JsonSerializer.Deserialize<string[]>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(value, JsonSerializer.Deserialize<string[]>(new MemoryStream(Encoding.UTF8.GetBytes(json))));
            Assert.Equal(value, await JsonSerializer.DeserializeAsync<string[]>(new MemoryStream(Encoding.UTF8.GetBytes(json)), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(new MemoryStream(Encoding.UTF8.GetBytes(json)))!.ToString());
        }

        [Fact]
        public void EscapedMemberNames()
        {
            var model = new JsonEscapedNamesModel() { Escaped = 1, Long = "value", Plain = 3 };

            var json = JsonSerializer.Serialize(model);
            Assert.Equal(json, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(model)));

            using var document = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty(JsonEscapedNamesModel.EscapedName).GetInt32());
            Assert.Equal("value", document.RootElement.GetProperty(JsonEscapedNamesModel.LongName).GetString());

            foreach (var result in new[] { JsonSerializer.Deserialize<JsonEscapedNamesModel>(json)!, JsonSerializer.Deserialize<JsonEscapedNamesModel>(Encoding.UTF8.GetBytes(json))! })
            {
                Assert.Equal(1, result.Escaped);
                Assert.Equal("value", result.Long);
                Assert.Equal(3, result.Plain);
            }
        }

        [Fact]
        public void DictionaryForms()
        {
            AssertDictionaryForms<Dictionary<string, int>>();
            AssertDictionaryForms<SortedDictionary<string, int>>();
            AssertDictionaryForms<IDictionary<string, int>>();
            AssertDictionaryForms<IReadOnlyDictionary<string, int>>();
        }

        private static void AssertDictionaryForms<T>() where T : IEnumerable<KeyValuePair<string, int>>
        {
            foreach (var json in new[] { "{}", "[]" })
            {
                Assert.Empty(JsonSerializer.Deserialize<T>(json)!);
                Assert.Empty(JsonSerializer.Deserialize<T>(Encoding.UTF8.GetBytes(json))!);
            }
            foreach (var json in new[] { "{\"a\":1,\"b\":2}", "[{\"Key\":\"a\",\"Value\":1},{\"Key\":\"b\",\"Value\":2}]" })
            {
                Assert.Equal([new("a", 1), new("b", 2)], JsonSerializer.Deserialize<T>(json)!.OrderBy(x => x.Key));
                Assert.Equal([new("a", 1), new("b", 2)], JsonSerializer.Deserialize<T>(Encoding.UTF8.GetBytes(json))!.OrderBy(x => x.Key));
                Assert.Equal([new("a", 1), new("b", 2)], JsonSerializer.Deserialize<T>(new MemoryStream(Encoding.UTF8.GetBytes(json)))!.OrderBy(x => x.Key));
            }
            foreach (var json in new[] { "{\"a\" 1}", "{\"a\":1 \"b\":2}", "{1}", "[{\"Key\":\"a\",\"Value\":1} 2]" })
            {
                _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<T>(json));
                _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<T>(Encoding.UTF8.GetBytes(json)));
            }
        }

        [Fact]
        public void InvalidValueVersusMismatch()
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };

            Assert.Contains("Invalid value for Int32", Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<int>("1.5")).Message);
            Assert.Contains("Invalid value for Byte", Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<byte>(Encoding.UTF8.GetBytes("300"))).Message);
            Assert.Contains("Invalid value for Guid", Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<Guid>("\"xyz\"")).Message);

            Assert.Equal(0, JsonSerializer.Deserialize<int>("true"));
            Assert.Contains("Cannot convert to Int32", Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<int>("true", strict)).Message);
            Assert.Equal(0, JsonSerializer.Deserialize<int>("\"xyz\""));
            Assert.Contains("Cannot convert to Int32", Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<int>("\"xyz\"", strict)).Message);
        }

        [Fact]
        public void GraphRoot()
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            var graph = new Graph<SimpleModel>(x => x.Value1);

            var json = JsonSerializer.Serialize(graph);
            Assert.Equal($"\"{graph.Signature}\"", json);
            foreach (var result in new[] { JsonSerializer.Deserialize<Graph<SimpleModel>>(json)!, JsonSerializer.Deserialize<Graph<SimpleModel>>(Encoding.UTF8.GetBytes(json))! })
            {
                Assert.True(result.HasMember(nameof(SimpleModel.Value1)));
                Assert.False(result.HasMember(nameof(SimpleModel.Value2)));
            }
            Assert.True(JsonSerializer.Deserialize<Graph>("\"A:\"")!.IncludeAllMembers);
            Assert.Equal("null", JsonSerializer.Serialize<Graph?>(null));
            Assert.Null(JsonSerializer.Deserialize<Graph>("null"));

            _ = Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<Graph>("\"Q:bad\""));
            foreach (var token in new[] { "5", "true", "false", "{\"a\":1}", "[1]" })
            {
                Assert.Null(JsonSerializer.Deserialize<Graph>(token));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<Graph>(token, strict));
            }
        }

#pragma warning disable xUnit1051 // the token is the value being serialized, not a cancellation signal
        [Fact]
        public void CancellationTokenRoot()
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            Assert.Equal("{}", JsonSerializer.Serialize(CancellationToken.None));
            Assert.Equal("{}", JsonSerializer.Serialize<CancellationToken?>(CancellationToken.None));
            Assert.Equal("null", JsonSerializer.Serialize<CancellationToken?>(null));

            Assert.Equal(default, JsonSerializer.Deserialize<CancellationToken>("{}"));
            Assert.Equal(default, JsonSerializer.Deserialize<CancellationToken>("{}", strict));
            Assert.Equal(default(CancellationToken), JsonSerializer.Deserialize<CancellationToken?>("{\"a\":1}", strict));
            Assert.Null(JsonSerializer.Deserialize<CancellationToken?>("null", strict));

            foreach (var token in new[] { "5", "true", "false", "\"x\"", "[1]" })
            {
                Assert.Equal(default, JsonSerializer.Deserialize<CancellationToken>(token));
                Assert.Null(JsonSerializer.Deserialize<CancellationToken?>(token));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<CancellationToken>(token, strict));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<CancellationToken?>(token, strict));
            }
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<CancellationToken>("null", strict));
        }
#pragma warning restore xUnit1051

        [Fact]
        public async Task NamelessRecords()
        {
            var nameless = new JsonSerializerOptions() { Nameless = true };
            var strictNameless = new JsonSerializerOptions() { Nameless = true, ErrorOnTypeMismatch = true };
            var model = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            var json = JsonSerializer.Serialize(model, nameless);
            Assert.Equal("[true,42,\"moo\"]", json);
            foreach (var result in new[]
            {
                JsonSerializer.Deserialize<RecordModel>(json, nameless)!,
                JsonSerializer.Deserialize<RecordModel>(Encoding.UTF8.GetBytes(json), nameless)!,
                (await JsonSerializer.DeserializeAsync<RecordModel>(new MemoryStream(Encoding.UTF8.GetBytes(json)), nameless, cancellationToken: TestContext.Current.CancellationToken))!,
            })
            {
                Assert.True(result.Property1);
                Assert.Equal(42, result.Property2);
                Assert.Equal("moo", result.Property3);
            }

            var empty = JsonSerializer.Deserialize<RecordModel>("[]", nameless)!;
            Assert.False(empty.Property1);
            Assert.Equal(0, empty.Property2);

            var partial = JsonSerializer.Deserialize<RecordModel>("[true]", nameless)!;
            Assert.True(partial.Property1);

            Assert.Equal([model, model], JsonSerializer.Deserialize<RecordModel[]>(JsonSerializer.Serialize(new[] { model, model }, nameless), nameless));

            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<RecordModel>("[true,42,\"moo\",1]", nameless));
            Assert.Null(JsonSerializer.Deserialize<RecordModel>("{\"Property1\":true}", nameless));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<RecordModel>("{\"Property1\":true}", strictNameless));
            Assert.Null(JsonSerializer.Deserialize<RecordModel>("5", nameless));
        }

        public sealed class UnmatchedConstructorModel
        {
            public int Value { get; }
            public UnmatchedConstructorModel(int other) { Value = other; }
            public UnmatchedConstructorModel(UnmatchedConstructorModel copy) { Value = copy.Value; }
        }

        [Fact]
        public void ConstructorWithoutMatchingMembers()
        {
            Assert.Null(JsonSerializer.Deserialize<UnmatchedConstructorModel>("{\"Value\":5}"));
        }

        [Fact]
        public void ConstructorArgumentMissing()
        {
            var result = JsonSerializer.Deserialize<RecordModel>("{\"Property2\":5}")!;
            Assert.False(result.Property1);
            Assert.Equal(5, result.Property2);
            Assert.False(JsonSerializer.Deserialize<RecordModel>(Encoding.UTF8.GetBytes("{}"))!.Property1);
        }

        public class DirectionalIgnoreModel
        {
            public int First { get; set; }
            [JsonIgnore(JsonIgnoreCondition.WhenReading)]
            public int WriteOnly { get; set; }
            [JsonIgnore(JsonIgnoreCondition.WhenWriting)]
            public int ReadOnly { get; set; }
            public int Last { get; set; }
        }

        public class NoMembersModel { }

        public readonly struct PointStruct
        {
            public int X { get; }
            public int Y { get; }
            public PointStruct(int x, int y) { X = x; Y = y; }
        }

        [Fact]
        public void Object_PropertiesInAnyOrder()
        {
            //the members are found searching back from where the last one was
            const string reversed = "{\"Last\":4,\"ReadOnly\":3,\"WriteOnly\":2,\"First\":1}";
            foreach (var result in new[] { JsonSerializer.Deserialize<DirectionalIgnoreModel>(reversed)!, JsonSerializer.Deserialize<DirectionalIgnoreModel>(Encoding.UTF8.GetBytes(reversed))! })
            {
                Assert.Equal(1, result.First);
                Assert.Equal(0, result.WriteOnly);
                Assert.Equal(3, result.ReadOnly);
                Assert.Equal(4, result.Last);
            }

            //a graph leaves out a member both ways
            var graph = new Graph<DirectionalIgnoreModel>(x => x.First, x => x.ReadOnly);
            foreach (var result in new[] { JsonSerializer.Deserialize<DirectionalIgnoreModel>(reversed, null, graph)!, JsonSerializer.Deserialize<DirectionalIgnoreModel>(Encoding.UTF8.GetBytes(reversed), null, graph)! })
            {
                Assert.Equal(1, result.First);
                Assert.Equal(3, result.ReadOnly);
                Assert.Equal(0, result.Last);
            }

            var model = new DirectionalIgnoreModel() { First = 1, WriteOnly = 2, ReadOnly = 3, Last = 4 };
            Assert.Equal("{\"First\":1,\"WriteOnly\":2,\"Last\":4}", JsonSerializer.Serialize(model));
            Assert.Equal("[1,2,4]", JsonSerializer.Serialize(model, new JsonSerializerOptions() { Nameless = true }));
            Assert.Equal("{\"First\":1}", JsonSerializer.Serialize(model, null, new Graph<DirectionalIgnoreModel>(x => x.First)));
            Assert.Equal("[1]", JsonSerializer.Serialize(model, new JsonSerializerOptions() { Nameless = true }, new Graph<DirectionalIgnoreModel>(x => x.First)));
        }

        [Fact]
        public void Object_InvalidPropertyNames()
        {
            var ignoreCase = new JsonSerializerOptions() { IgnoreCase = true };
            foreach (var options in new[] { null, ignoreCase })
            {
                //an empty name is valid JSON and matches no member
                Assert.Equal(2, JsonSerializer.Deserialize<DirectionalIgnoreModel>("{\"\":1,\"Last\":2}", options)!.Last);
                Assert.Equal(2, JsonSerializer.Deserialize<DirectionalIgnoreModel>(Encoding.UTF8.GetBytes("{\"\":1,\"Last\":2}"), options)!.Last);
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DirectionalIgnoreModel>("{\"First\":1 \"Last\":2}", options));
            }

            //an unknown name is skipped, with or without case
            Assert.Equal(2, JsonSerializer.Deserialize<DirectionalIgnoreModel>("{\"Unknown\":1,\"last\":2}", ignoreCase)!.Last);
            Assert.Equal(2, JsonSerializer.Deserialize<DirectionalIgnoreModel>(Encoding.UTF8.GetBytes("{\"Unknown\":1,\"last\":2}"), ignoreCase)!.Last);
        }

        [Fact]
        public void Object_Nameless_EdgeCases()
        {
            var nameless = new JsonSerializerOptions() { Nameless = true };
            var strictNameless = new JsonSerializerOptions() { Nameless = true, ErrorOnTypeMismatch = true };

            Assert.Equal("[]", JsonSerializer.Serialize(new NoMembersModel(), nameless));
            Assert.Equal("{}", JsonSerializer.Serialize(new NoMembersModel()));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DirectionalIgnoreModel>("[1 2]", nameless));
            Assert.Null(JsonSerializer.Deserialize<DirectionalIgnoreModel>("\"text\"", nameless));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DirectionalIgnoreModel>("\"text\"", strictNameless));
        }

        [Fact]
        public void Struct_ConstructorWithEveryArgumentMissing()
        {
            Assert.Equal(new PointStruct(0, 0), JsonSerializer.Deserialize<PointStruct>("{}"));
            Assert.Equal(new PointStruct(0, 0), JsonSerializer.Deserialize<PointStruct>("[]", new JsonSerializerOptions() { Nameless = true }));
            Assert.Equal(new PointStruct(3, 4), JsonSerializer.Deserialize<PointStruct>("{\"X\":3,\"Y\":4}"));
        }

        [Fact]
        public void DictionaryInterfaces_EveryKeyKind()
        {
            AssertDictionaryKinds(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), new DateTime(2026, 1, 2, 3, 4, 5, 500, DateTimeKind.Utc));
            AssertDictionaryKinds(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
            AssertDictionaryKinds(new DateOnly(2026, 1, 2), new DateOnly(1, 1, 1));
            AssertDictionaryKinds(new TimeOnly(3, 4, 5), new TimeOnly(23, 59));
            AssertDictionaryKinds(true, false);
            AssertDictionaryKinds(Guid.Parse("11111111-2222-3333-4444-555555555555"), Guid.Empty);
            AssertDictionaryKinds(DayOfWeek.Monday, DayOfWeek.Friday);
            AssertDictionaryKinds("a", "b");
            //an object key can't be a property name, it's written as key-value pairs
            AssertDictionaryKinds(new SimpleModel() { Value1 = 1, Value2 = "a" }, new SimpleModel() { Value1 = 2, Value2 = "b" });
        }

        private static void AssertDictionaryKinds<TKey>(TKey key1, TKey key2) where TKey : notnull
        {
            var strict = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            var source = new Dictionary<TKey, int>() { { key1, 1 }, { key2, 2 } };

            AssertSame(JsonSerializer.Deserialize<IDictionary<TKey, int>>(JsonSerializer.Serialize<IDictionary<TKey, int>>(source)));
            AssertSame(JsonSerializer.Deserialize<IReadOnlyDictionary<TKey, int>>(JsonSerializer.Serialize<IReadOnlyDictionary<TKey, int>>(source)));
            AssertSame(JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<TKey, int>>(JsonSerializer.Serialize(new System.Collections.Concurrent.ConcurrentDictionary<TKey, int>(source))));

            Assert.Empty(JsonSerializer.Deserialize<IDictionary<TKey, int>>(JsonSerializer.Serialize<IDictionary<TKey, int>>(new Dictionary<TKey, int>()))!);
            Assert.Empty(JsonSerializer.Deserialize<IReadOnlyDictionary<TKey, int>>(JsonSerializer.Serialize<IReadOnlyDictionary<TKey, int>>(new Dictionary<TKey, int>()))!);
            Assert.Empty(JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<TKey, int>>(JsonSerializer.Serialize(new System.Collections.Concurrent.ConcurrentDictionary<TKey, int>()))!);

            foreach (var token in new[] { "5", "\"x\"", "true" })
            {
                Assert.Null(JsonSerializer.Deserialize<IDictionary<TKey, int>>(token));
                Assert.Null(JsonSerializer.Deserialize<IReadOnlyDictionary<TKey, int>>(token));
                Assert.Null(JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<TKey, int>>(token));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<IDictionary<TKey, int>>(token, strict));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<IReadOnlyDictionary<TKey, int>>(token, strict));
                _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<System.Collections.Concurrent.ConcurrentDictionary<TKey, int>>(token, strict));
            }

            void AssertSame(IEnumerable<KeyValuePair<TKey, int>>? result)
            {
                Assert.NotNull(result);
                var values = result.ToDictionary(x => JsonSerializer.Serialize(x.Key), x => x.Value);
                Assert.Equal(2, values.Count);
                Assert.Equal(1, values[JsonSerializer.Serialize(key1)]);
                Assert.Equal(2, values[JsonSerializer.Serialize(key2)]);
            }
        }
    }
}
