// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Zerra.Serialization.Json;
using Zerra.Serialization.Bytes;

namespace Zerra.Test
{
    [TestClass]
    public class ByteSerializerTest
    {
        public ByteSerializerTest()
        {
#if DEBUG
            JsonSerializer.Testing = false;
#endif
        }

        [TestMethod]
        public void AllTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = AllTypesModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            //Assert.AreEqual(8383, bytes.Length);
            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void CoreTypes()
        {
            var model1 = CoreTypesModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            //Assert.AreEqual(270, bytes.Length);
            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void BoxedCollections()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UseTypes = true
            };

            var model1 = BoxedCollectionsModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<BoxedCollectionsModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void UseTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UseTypes = true
            };

            var model1 = AllTypesModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void UsePropertyNames()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UsePropertyNames = true
            };

            var model1 = AllTypesModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void EmptyObjectsAndNulls()
        {
            var model1 = new NoPropertiesModel();
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<NoPropertiesModel>(bytes1);
            Assert.IsNotNull(model2);

            var model3 = new NoPropertiesModel[] {
                new(),
                new()
            };
            var bytes3 = ByteSerializer.Serialize(model3);
            var model4 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes3);
            Assert.AreEqual(2, model4.Length);
            Assert.IsNotNull(model4[0]);
            Assert.IsNotNull(model4[1]);

            var model5 = new NoPropertiesModel[] {
               null,
               null
            };
            var bytes5 = ByteSerializer.Serialize(model5);
            var model6 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes5);
            Assert.AreEqual(2, model6.Length);
            Assert.IsNull(model6[0]);
            Assert.IsNull(model6[1]);

            var bytes7 = ByteSerializer.Serialize(null);
            var model7 = ByteSerializer.Deserialize<NoPropertiesModel>(bytes7);
            Assert.IsNull(model7);

            var bytes8 = ByteSerializer.Serialize(null);
            var model8 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes8);
            Assert.IsNull(model8);

            var model9 = Array.Empty<NoPropertiesModel>();
            var bytes9 = ByteSerializer.Serialize(model9);
            var model10 = ByteSerializer.Deserialize<IEnumerable<NoPropertiesModel>>(bytes9);
            Assert.AreEqual(model9.GetType(), model10?.GetType());

            var model11 = new ArrayChainModel[]{
                new()
                {
                    ID = Guid.NewGuid(),
                    Children = null
                }
            };
            var bytes11 = ByteSerializer.Serialize(model11);
            var model12 = ByteSerializer.Deserialize<ArrayChainModel[]>(bytes11);
            Assert.AreEqual(model11[0].Children, model12[0].Children);
        }

        [TestMethod]
        public void Arrays()
        {
            var model1 = SimpleModel.CreateArray();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<SimpleModel[]>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void Boxing()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TestBoxingModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TestBoxingModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void IndexAttribute()
        {
            var model1 = TestSerializerIndexModel1.Create();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<TestSerializerIndexModel2>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void IgnoreIndexAttribute()
        {
            var options = new ByteSerializerOptions()
            {
                IgnoreIndexAttribute = true
            };

            var model1 = TestSerializerIndexModel1.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TestSerializerIndexModel2>(bytes, options);
            AssertHelper.AreNotEqual(model1, model2);
        }

        [TestMethod]
        public void LargeIndexAttribute()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = TestSerializerLongIndexModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TestSerializerLongIndexModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void CookieObject()
        {
            var model1 = new Cookie("tester", "stuff", null, null);
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<Cookie>(bytes);
            Assert.AreEqual(model1.Name, model2.Name);
            Assert.AreEqual(model1.Value, model2.Value);
            Assert.AreEqual(model1.Path, model2.Path);
            Assert.AreEqual(model1.Domain, model2.Domain);
        }

        [TestMethod]
        public void ExceptionObject()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = new Exception("bad things happened");
            model1.Data.Add("stuff", "things");
            
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<Exception>(bytes, options);
            Assert.AreEqual(model1.Message, model2.Message);
            Assert.AreEqual(1, model2.Data.Count);
            Assert.AreEqual(model1.Data["stuff"], model2.Data["stuff"]);
        }

        [TestMethod]
        public void Interface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<ITestInterface>(bytes);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public void StringArrayOfArrayThing()
        {
            var model1 = new string[][] { new string[] { "a", "b", "c" }, new string[] { "d", "e", "f" } };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<string[][]>(bytes);
        }

        [TestMethod]
        public void HashSet()
        {
            var model1 = HashSetModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<HashSetModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void Record()
        {
            var model1 = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<RecordModel>(bytes);
            Assert.IsNotNull(model2);
            Assert.AreEqual(model1.Property1, model2.Property1);
            Assert.AreEqual(model1.Property2, model2.Property2);
            Assert.AreEqual(model1.Property3, model2.Property3);
        }

        [TestMethod]
        public async Task Stream()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = AllTypesModel.Create();
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                ms.Position = 0;
                var model2 = await ByteSerializer.DeserializeAsync<AllTypesModel>(ms, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }
    }
}
