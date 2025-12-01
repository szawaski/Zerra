// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using System.Text;
using Xunit;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
    public class JsonSerializerTest
    {
        public JsonSerializerTest()
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
            Assert.Equal(String.Empty, model4);

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
            }

            //deserialize will include all unicode escapes, some serialize differently
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;

                var charsLower = $"\"\\u{i:x4}\"";
                var charsUpper = $"\"\\u{i:X4}\"";

                var result = JsonSerializer.Deserialize<char>(charsLower);
                Assert.Equal(c, result);

                result = JsonSerializer.Deserialize<char>(charsUpper);
                Assert.Equal(c, result);
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
        public async Task StreamMatchesNewtonsoft()
        {
            var baseModel = TypesAllModel.Create();

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
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2);
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
            await JsonSerializer.SerializeAsync(stream1, baseModel);
            stream1.Position = 0;
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            var json1 = await sr1.ReadToEndAsync();

            using var stream2 = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream2, baseModel, options);
            stream2.Position = 0;
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync();

            Assert.True(json1 == json2);

            //swap serializers
            using var stream3 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream3);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream4, options);

            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypes()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesBasicModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesArray()
        {
            var model1 = TypesArrayModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesArrayModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesListT()
        {
            var model1 = TypesListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIList()
        {
            var model1 = TypesIListModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIListOfT()
        {
            var model1 = TypesIListOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlySetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyCollectionTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
        }

        [Fact]
        public async Task StreamTypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIEnumerableOfT()
        {
            var model1 = TypesIEnumerableOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
        }

        [Fact]
        public async Task StreamTypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesDictionaryTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIDictionaryTOfT()
        {
            var model1 = TypesIDictionaryTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyDictionaryTModel>(stream);
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
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesCustomCollectionsModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesOther()
        {
            var model1 = TypesOtherModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesOtherModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesCore()
        {
            var model1 = TypesCoreModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);

            var json = Encoding.UTF8.GetString(stream.ToArray());

            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesCoreModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamTypesAll()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
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
            await JsonSerializer.SerializeAsync(stream, baseModel, options);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = sr.ReadToEnd();

            Assert.DoesNotContain(EnumModel.EnumItem0.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem1.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem2.EnumName(), json);
            Assert.DoesNotContain(EnumModel.EnumItem3.EnumName(), json);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamConvertNullables()
        {
            var baseModel = BasicTypesNotNullable.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<BasicTypesNullable>(stream1);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<BasicTypesNotNullable>(stream2);
            BasicTypesNotNullable.AssertAreEqual(baseModel, model2);
        }

        [Fact]
        public async Task StreamConvertTypes()
        {
            var baseModel = TypesAllModel.Create();

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, baseModel);

            stream1.Position = 0;
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllAsStringsModel>(stream1);
            TypesAllAsStringsModel.AreEqual(baseModel, model1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);

            stream2.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2);
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
            await JsonSerializer.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream2);
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
            await JsonSerializer.SerializeAsync(stream3, model3);
            stream3.Position = 0;
            var result3 = await JsonSerializer.DeserializeAsync<EnumConversionModel1>(stream3);
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
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
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
            await JsonSerializer.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options);
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
            await JsonSerializer.SerializeAsync(stream, baseModel, options);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [Fact]
        public async Task StreamEmptys()
        {
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream1, null);
            using var sr1 = new StreamReader(stream1, Encoding.UTF8);
            stream1.Position = 0;
            var json1 = await sr1.ReadToEndAsync();
            Assert.Equal("null", json1);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync<string>(stream2, String.Empty);
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            stream2.Position = 0;
            var json2 = await sr2.ReadToEndAsync();
            Assert.Equal("\"\"", json2);

            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream3, null);
            using var sr3 = new StreamReader(stream3, Encoding.UTF8);
            stream3.Position = 0;
            var json3 = await sr3.ReadToEndAsync();
            Assert.Equal("null", json3);

            using var stream4 = new MemoryStream();
            await JsonSerializer.SerializeAsync<object>(stream4, new object());
            using var sr4 = new StreamReader(stream4, Encoding.UTF8);
            stream4.Position = 0;
            var json4 = await sr4.ReadToEndAsync();
            Assert.Equal("{}", json4);

            var model1 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.Null(model1);

            var model2 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.Equal(String.Empty, model2);

            var model3 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.Equal(String.Empty, model3);

            var model4 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.Equal(String.Empty, model4);

            var model5 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.Null(model5);

            var model6 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.Null(model6);

            var model7 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.Equal(String.Empty, model7);

            var model8 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.NotNull(model8);

            var model9 = await JsonSerializer.DeserializeAsync<int>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.Equal(0, model9);

            var model10 = await JsonSerializer.DeserializeAsync<int?>(new MemoryStream(Encoding.UTF8.GetBytes("")));
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
            await JsonSerializer.SerializeAsync(stream1, dateUtc);
            stream1.Position = 0;
            var dateUtc2 = await JsonSerializer.DeserializeAsync<DateTime>(stream1);
            Assert.Equal(dateUtc, dateUtc2);
            Assert.Equal(DateTimeKind.Utc, dateUtc2.Kind);

            var dateLocal = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Local);
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, dateLocal);
            stream2.Position = 0;
            var dateLocal2 = await JsonSerializer.DeserializeAsync<DateTime>(stream2);
            var dateLocalUtc = dateLocal.ToUniversalTime();
            Assert.Equal(dateLocalUtc, dateLocal2);
            Assert.Equal(DateTimeKind.Utc, dateLocal2.Kind);

            var dateUnspecified = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Unspecified);
            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, dateUnspecified);
            stream3.Position = 0;
            var dateUnspecified2 = await JsonSerializer.DeserializeAsync<DateTime>(stream3);
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
                await JsonSerializer.SerializeAsync(stream, c);
                using var sr = new StreamReader(stream, Encoding.UTF8);
                stream.Position = 0;
                var json = await sr.ReadToEndAsync();
                stream.Position = 0;
                var result = await JsonSerializer.DeserializeAsync<char>(stream);
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
                await JsonSerializer.SerializeAsync(streamStr, str);
                using var srStr = new StreamReader(streamStr, Encoding.UTF8);
                streamStr.Position = 0;
                json = await srStr.ReadToEndAsync();
                streamStr.Position = 0;
                var resultStr = await JsonSerializer.DeserializeAsync<string>(streamStr);
                Assert.Equal(str, resultStr);
            }

            //deserialize will include all unicode escapes, some serialize differently
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;
                using var charsLowerStream = new MemoryStream(Encoding.UTF8.GetBytes($"\"\\u{i:x4}\""));
                using var charsUpperStream = new MemoryStream(Encoding.UTF8.GetBytes($"\"\\u{i:X4}\""));

                var result = await JsonSerializer.DeserializeAsync<char>(charsLowerStream);
                Assert.Equal(c, result);

                result = await JsonSerializer.DeserializeAsync<char>(charsUpperStream);
                Assert.Equal(c, result);
            }
        }

        [Fact]
        public async Task StreamExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<Exception>(stream);
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
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<ITestInterface>(stream);

            Assert.Equal(5, model2.Property1);
            Assert.Equal(6, model2.Property2);
        }

        [Fact]
        public async Task StreamEmptyModel()
        {
            var baseModel = TypesAllModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<EmptyModel>(stream);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task StreamGetsSets()
        {
            var baseModel = new GetsSetsModel(1, 2);
            var baseModelJson = baseModel.ToJsonString();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<GetsSetsModel>(stream);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task StreamDrainModel()
        {
            var model1 = TypesCoreAlternatingModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var result1 = await JsonSerializer.DeserializeAsync<TypesCoreModel>(stream1);
            AssertHelper.AreEqual(model1, result1);

            var model2 = TypesCoreModel.Create();
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model2);
            stream2.Position = 0;
            var result2 = await JsonSerializer.DeserializeAsync<TypesCoreAlternatingModel>(stream2);
            AssertHelper.AreEqual(result2, model2);
        }

        [Fact]
        public async Task StreamLargeModel()
        {
            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, models);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<TypesAllModel[]>(stream);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [Fact]
        public async Task StreamRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<RecordModel>(stream);
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
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public async Task StreamBoxing()
        {
            var baseModel = TestBoxingModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TestBoxingModel>(stream);
        }

        [Fact]
        public async Task StreamPropertyNameAttribute()
        {
            var baseModel = JsonPropertyNameAttributeTestModel.Create();

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            Assert.Contains("\"1property\"", json);
            Assert.Contains("\"property2\"", json);
            Assert.Contains("\"3property\"", json);

            _ = json.Replace("\"property2\"", "\"PROPERTY2\"");

            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var model = await JsonSerializer.DeserializeAsync<JsonPropertyNameAttributeTestModel>(stream2);
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
            await JsonSerializer.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            using var sr = new StreamReader(stream, Encoding.UTF8);
            var json = await sr.ReadToEndAsync();

            Assert.Contains("\"Property1\"", json);
            Assert.DoesNotContain("\"Property2\"", json);
            Assert.Contains("\"Property3\"", json);
            Assert.DoesNotContain("\"Property4\"", json);
            Assert.Contains("\"Property5a\"", json);
            Assert.DoesNotContain("\"Property5b\"", json);
            Assert.Contains("\"Property6a\"", json);
            Assert.DoesNotContain("\"Property6b\"", json);

            using var stream2 = new MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(stream2, baseModel);
            stream2.Position = 0;
            using var sr2 = new StreamReader(stream2, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync();

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
            await JsonSerializer.SerializeAsync(stream1, model1, null, graph);
            stream1.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1);
            AssertHelper.AreEqual(model1.Int32Thing, model2.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model2.Int64Thing);
            Assert.NotNull(model2.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph);
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
            await JsonSerializer.SerializeAsync(stream1a, model1a, null, graph);
            stream1a.Position = 0;
            var model2a = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1a);
            AssertHelper.AreEqual(model1a.Int32Thing, model2a.Int32Thing);
            AssertHelper.AreNotEqual(model1a.Int64Thing, model2a.Int64Thing);
            Assert.NotNull(model2a.ClassThing);
            AssertHelper.AreEqual(model1a.ClassThing.Value2, model2a.ClassThing.Value2);

            using var stream1b = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1b, model1b, null, graph);
            stream1b.Position = 0;
            var model2b = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1b);
            AssertHelper.AreNotEqual(model1b.Int32Thing, model2b.Int32Thing);
            AssertHelper.AreEqual(model1b.Int64Thing, model2b.Int64Thing);
            Assert.Null(model2b.ClassThing);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1a);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph);
            AssertHelper.AreEqual(model1a, model3);
        }

        [Fact]
        public async Task StreamJsonObject()
        {
            var baseModel = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);
            stream.Position = 0;
            var json = Encoding.UTF8.GetString(stream.ToArray());
            stream.Position = 0;
            var jsonObject = await JsonSerializer.DeserializeJsonObjectAsync(stream);

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
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypeModel>(stream);
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
            await JsonSerializer.SerializeAsync(stream, model);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<string[]>(stream);

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
            var result1 = await JsonSerializer.DeserializeAsync<SimpleModel>(streamUpper, options);
            Assert.Equal(model.Value1, result1.Value1);
            Assert.Equal(model.Value2, result1.Value2);

            using var streamLower = new MemoryStream(Encoding.UTF8.GetBytes(json.ToUpper()));
            var result2 = await JsonSerializer.DeserializeAsync<SimpleModel>(streamLower, options);
            Assert.Equal(model.Value1, result2.Value1);
            Assert.Equal(model.Value2, result2.Value2);
        }

        [Fact]
        public async Task StreamCustomType()
        {
            JsonSerializer.AddConverter(typeof(CustomType), () => new CustomTypeJsonConverter());

            var model1 = CustomTypeModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<CustomTypeModel>(stream);
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
            var model2 = await JsonSerializer.DeserializeAsync<CancellationToken>(stream1);
            AssertHelper.AreEqual(model1, model2);

            CancellationToken? model3 = CancellationToken.None;
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model3);
            stream2.Position = 0;
            var model4 = await JsonSerializer.DeserializeAsync<CancellationToken?>(stream2);
            AssertHelper.AreEqual(model3, model4);

            CancellationToken? model5 = null;
            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, model5);
            stream3.Position = 0;
            var model6 = await JsonSerializer.DeserializeAsync<CancellationToken?>(stream3);
            AssertHelper.AreEqual(model5, model6);
        }

        [Fact]
        public async Task StreamConstructorParameters()
        {
            var model1 = new TestSerializerConstructor("Five", 5);
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TestSerializerConstructor>(stream1);
            Assert.NotNull(model2);
            Assert.Equal(model1._Value1, model2._Value1);
            Assert.Equal(model1.value2, model2.value2);
        }

        [Fact]
        public async Task StreamPatch()
        {
            var model1 = TypesBasicModel.Create();
            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            (var model2, var graph) = await JsonSerializer.DeserializePatchAsync<TypesAllModel>(stream1);

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
            await JsonSerializer.SerializeAsync(stream1, model1);
            stream1.Position = 0;
            (var model2, var graph) = await JsonSerializer.DeserializePatchAsync<Dictionary<string, string>>(stream1);
            Assert.True(graph.HasMember("One"));
            Assert.True(graph.HasMember("Two"));
        }
    }
}
