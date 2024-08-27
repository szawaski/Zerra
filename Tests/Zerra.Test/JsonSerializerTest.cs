// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
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
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
            Zerra.Serialization.Json.IO.JsonWriter.Testing = true;
#endif
        }

        [TestMethod]
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

        [TestMethod]
        public void StringMatchesSystemTextJson()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var baseModel = TypesAllModel.Create();
            var json1 = JsonSerializer.Serialize(baseModel);
            var json2 = System.Text.Json.JsonSerializer.Serialize(baseModel, options);

            Assert.IsTrue(json1 == json2);

            //swap serializers
            var model1 = JsonSerializer.Deserialize<TypesAllModel>(json2);
            var model2 = System.Text.Json.JsonSerializer.Deserialize<TypesAllModel>(json1, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesBasicModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesArray()
        {
            var model1 = TypesArrayModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesArrayModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesListT()
        {
            var model1 = TypesListTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesListTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            var bytes = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyListTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIList()
        {
            var model1 = TypesIListModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIListOfT()
        {
            var model1 = TypesIListOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIListOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesHashSetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesISetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesISetTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlySetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesICollectionTOfTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyCollectionTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIEnumerableTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
        }

        [TestMethod]
        public void StringTypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIEnumerableModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIEnumerableOfT()
        {
            var model1 = TypesIEnumerableOfTModel.Create();
            var json = JsonSerializer.Serialize(model1);
        }

        [TestMethod]
        public void StringTypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesDictionaryTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            var bytes = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIDictionaryTOfT()
        {
            var model1 = TypesIDictionaryTOfTModel.Create();
            var bytes = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIDictionaryTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            var bytes = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesIReadOnlyDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        //[TestMethod]
        //public void StringTypesIDictionary()
        //{
        //    var model1 = TypesIDictionaryModel.Create();
        //    var json = JsonSerializer.Serialize(model1);
        //    var model2 = JsonSerializer.Deserialize<TypesIDictionaryModel>(json);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        //[TestMethod]
        //public void StringTypesIDictionaryOfT()
        //{
        //    var model1 = TypesIDictionaryOfTModel.Create();
        //    var bytes = JsonSerializer.Serialize(model1);
        //    var model2 = JsonSerializer.Deserialize<TypesIDictionaryOfTModel>(bytes);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        [TestMethod]
        public void StringTypesOther()
        {
            var model1 = TypesOtherModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesOtherModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesCore()
        {
            var model1 = TypesCoreModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesCoreModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringTypesAll()
        {
            var model1 = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesAllModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringEnumAsNumbers()
        {
            var options = new JsonSerializerOptions()
            {
                EnumAsNumber = true
            };

            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel, options);
            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));
            var model = JsonSerializer.Deserialize<TypesAllModel>(json, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
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

        [TestMethod]
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
        private static void StringTestNumber<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<T>(json);
            Assert.AreEqual(value, result);
        }
        private static void StringTestNumberAsString<T>(T value)
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
            var baseModel = TypesAllModel.Create();
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
            var model = JsonSerializer.Deserialize<TypesAllModel>(jsonPretty);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
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

        [TestMethod]
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
            Assert.AreEqual(String.Empty, model2);

            var model3 = JsonSerializer.Deserialize<string>("\"\"");
            Assert.AreEqual(String.Empty, model3);

            var model4 = JsonSerializer.Deserialize<string>("{}");
            Assert.AreEqual(String.Empty, model4);

            var model5 = JsonSerializer.Deserialize<object>("null");
            Assert.IsNull(model5);

            var model6 = JsonSerializer.Deserialize<object>("");
            Assert.IsNull(model6);

            var model7 = JsonSerializer.Deserialize<object>("\"\"");
            Assert.AreEqual(String.Empty, model7);

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
            var baseModel = TypesAllModel.Create();
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

        [TestMethod]
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

        [TestMethod]
        public void StringBoxing()
        {
            var baseModel = TestBoxingModel.Create();
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<TestBoxingModel>(json);
        }

        [TestMethod]
        public void StringHashSet()
        {
            var model1 = TypesHashSetTModel.Create();
            var json = JsonSerializer.Serialize(model1);
            var model2 = JsonSerializer.Deserialize<TypesHashSetTModel>(json);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void StringRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var json = JsonSerializer.Serialize(baseModel);
            var model = JsonSerializer.Deserialize<RecordModel>(json);
            Assert.IsNotNull(model);
            Assert.AreEqual(baseModel.Property1, model.Property1);
            Assert.AreEqual(baseModel.Property2, model.Property2);
            Assert.AreEqual(baseModel.Property3, model.Property3);
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
            Assert.IsNotNull(model2.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);

            var json2 = JsonSerializer.Serialize(model1);
            var model3 = JsonSerializer.Deserialize<TypesAllModel>(json2, null, graph);
            AssertHelper.AreEqual(model1.Int32Thing, model3.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model3.Int64Thing);
            Assert.IsNotNull(model3.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model3.ClassThing.Value2);
        }

        [TestMethod]
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
            Assert.IsNotNull(model2a.ClassThing);
            AssertHelper.AreEqual(model1a.ClassThing.Value2, model2a.ClassThing.Value2);

            var jsonb = JsonSerializer.Serialize(model1b, null, graph);
            var model2b = JsonSerializer.Deserialize<TypesAllModel>(jsonb);
            AssertHelper.AreNotEqual(model1b.Int32Thing, model2b.Int32Thing);
            AssertHelper.AreEqual(model1b.Int64Thing, model2b.Int64Thing);
            Assert.IsNull(model2b.ClassThing);

            var json2 = JsonSerializer.Serialize(model1a);
            var model3 = JsonSerializer.Deserialize<TypesAllModel>(json2, null, graph);
            AssertHelper.AreEqual(model1a, model3);
        }

        [TestMethod]
        public void StringJsonObject()
        {
            var baseModel = TypesAllModel.Create();
            var json = JsonSerializer.Serialize(baseModel);
            var jsonObject = JsonSerializer.DeserializeJsonObject(json);

            var json2 = jsonObject.ToString();

            Assert.AreEqual(json, json2);

            var model1 = jsonObject.Bind<TypesAllModel>();

            AssertHelper.AreEqual(baseModel, model1);
        }

        [TestMethod]
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

        [TestMethod]
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
            stream1.Position = 0;
            using var sr2 = new StreamReader(stream1, Encoding.UTF8);
            var json2 = await sr2.ReadToEndAsync();

            Assert.IsTrue(json1 == json2);

            //swap serializers
            using var stream3 = new MemoryStream(Encoding.UTF8.GetBytes(json2));
            var model1 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream3);

            using var stream4 = new MemoryStream(Encoding.UTF8.GetBytes(json1));
            var model2 = await System.Text.Json.JsonSerializer.DeserializeAsync<TypesAllModel>(stream4, options);

            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesBasicModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesArray()
        {
            var model1 = TypesArrayModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesArrayModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesListT()
        {
            var model1 = TypesListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyListTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIList()
        {
            var model1 = TypesIListModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIListOfT()
        {
            var model1 = TypesIListOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIListOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesISetTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlySetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesICollectionTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyCollectionTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
        }

        [TestMethod]
        public async Task StreamTypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIEnumerableModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIEnumerableOfT()
        {
            var model1 = TypesIEnumerableOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
        }

        [TestMethod]
        public async Task StreamTypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesDictionaryTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIDictionaryTOfT()
        {
            var model1 = TypesIDictionaryTOfTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryTOfTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesIReadOnlyDictionaryTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        //[TestMethod]
        //public async Task StreamTypesIDictionary()
        //{
        //    var model1 = TypesIDictionaryModel.Create();
        //    using var stream = new MemoryStream();
        //    await JsonSerializer.SerializeAsync(stream, model1);
        //    stream.Position = 0;
        //    var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryModel>(stream);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        //[TestMethod]
        //public async Task StreamTypesIDictionaryOfT()
        //{
        //    var model1 = TypesIDictionaryOfTModel.Create();
        //    using var stream = new MemoryStream();
        //    await JsonSerializer.SerializeAsync(stream, model1);
        //    stream.Position = 0;
        //    var model2 = await JsonSerializer.DeserializeAsync<TypesIDictionaryOfTModel>(stream);
        //    AssertHelper.AreEqual(model1, model2);
        //}

        [TestMethod]
        public async Task StreamTypesOther()
        {
            var model1 = TypesOtherModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesOtherModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesCore()
        {
            var model1 = TypesCoreModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesCoreModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypesAll()
        {
            var model1 = TypesAllModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
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

            Assert.IsFalse(json.Contains(EnumModel.EnumItem0.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem1.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem2.EnumName()));
            Assert.IsFalse(json.Contains(EnumModel.EnumItem3.EnumName()));

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream, options);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
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

        [TestMethod]
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
        private static async Task StreamTestNumber<T>(T value)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value);
            stream.Position = 0;
            var result = await JsonSerializer.DeserializeAsync<T>(stream);
            Assert.AreEqual(value, result);
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
            var baseModel = TypesAllModel.Create();
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
            var model = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2);
            AssertHelper.AreEqual(baseModel, model);
        }

        [TestMethod]
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

        [TestMethod]
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
            Assert.AreEqual(String.Empty, model2);

            var model3 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.AreEqual(String.Empty, model3);

            var model4 = await JsonSerializer.DeserializeAsync<string>(new MemoryStream(Encoding.UTF8.GetBytes("{}")));
            Assert.AreEqual(String.Empty, model4);

            var model5 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("null")));
            Assert.IsNull(model5);

            var model6 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("")));
            Assert.IsNull(model6);

            var model7 = await JsonSerializer.DeserializeAsync<object>(new MemoryStream(Encoding.UTF8.GetBytes("\"\"")));
            Assert.AreEqual(String.Empty, model7);

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
            var baseModel = TypesAllModel.Create();

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

        [TestMethod]
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

        [TestMethod]
        public async Task StreamRecord()
        {
            var baseModel = new RecordModel(true) { Property2 = 42, Property3 = "moo" };

            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, baseModel);

            stream.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<RecordModel>(stream);
            Assert.IsNotNull(model);
            Assert.AreEqual(baseModel.Property1, model.Property1);
            Assert.AreEqual(baseModel.Property2, model.Property2);
            Assert.AreEqual(baseModel.Property3, model.Property3);
        }

        [TestMethod]
        public async Task StreamHashSet()
        {
            var model1 = TypesHashSetTModel.Create();
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await JsonSerializer.DeserializeAsync<TypesHashSetTModel>(stream);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
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

        [TestMethod]
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
            Assert.IsNotNull(model2.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model2.ClassThing.Value2);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph);
            AssertHelper.AreEqual(model1.Int32Thing, model3.Int32Thing);
            AssertHelper.AreNotEqual(model1.Int64Thing, model3.Int64Thing);
            Assert.IsNotNull(model3.ClassThing);
            AssertHelper.AreEqual(model1.ClassThing.Value2, model3.ClassThing.Value2);
        }

        [TestMethod]
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
            Assert.IsNotNull(model2a.ClassThing);
            AssertHelper.AreEqual(model1a.ClassThing.Value2, model2a.ClassThing.Value2);

            using var stream1b = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1b, model1b, null, graph);
            stream1b.Position = 0;
            var model2b = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream1b);
            AssertHelper.AreNotEqual(model1b.Int32Thing, model2b.Int32Thing);
            AssertHelper.AreNotEqual(model1b.Int64Thing, model2b.Int64Thing);
            Assert.IsNull(model2b.ClassThing);

            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, model1a);
            stream2.Position = 0;
            var model3 = await JsonSerializer.DeserializeAsync<TypesAllModel>(stream2, null, graph);
            AssertHelper.AreEqual(model1a, model3);
        }

        [TestMethod]
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

            Assert.AreEqual(json, json2);

            var model1 = jsonObject.Bind<TypesAllModel>();

            AssertHelper.AreEqual(baseModel, model1);
        }
    }
}
