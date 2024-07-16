// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Zerra.Serialization.Bytes;

namespace Zerra.Test
{
    [TestClass]
    public class ByteSerializerTest
    {
        public ByteSerializerTest()
        {
#if DEBUG
            Zerra.Serialization.Bytes.IO.ByteReader.Testing = true;
            Zerra.Serialization.Bytes.IO.ByteWriter.Testing = true;
#endif
        }

        [TestMethod]
        public void TypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(315, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesBasicModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesArray()
        {
            var model1 = TypesArrayModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesArrayModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesList()
        {
            var model1 = TypesListModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesListModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIList()
        {
            var model1 = TypesIListModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIListModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlyList()
        {
            var model1 = TypesIReadOnlyListModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyListModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesHashSet()
        {
            var model1 = TypesHashSetModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesHashSetModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesISet()
        {
            var model1 = TypesISetModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesISetModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlySet()
        {
            var model1 = TypesIReadOnlySetModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlySetModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesICollection()
        {
            var model1 = TypesICollectionModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesICollectionModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlyCollection()
        {
            var model1 = TypesIReadOnlyCollectionModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyCollectionModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIEnumerable()
        {
            var model1 = TypesIEnumerableModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIEnumerableModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesDictionary()
        {
            var model1 = TypesDictionaryModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(462, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesDictionaryModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesOther()
        {
            var model1 = TypesOtherModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(70, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesOtherModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesCore()
        {
            var model1 = TypesCoreModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(270, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesCoreModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesBoxedCollections()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UseTypes = true
            };

            var model1 = TypesBoxedCollectionsModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesBoxedCollectionsModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesAll()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            Assert.AreEqual(8393, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesAllModel>(bytes, options);
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

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesAllModel>(bytes, options);
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

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesAllModel>(bytes, options);
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

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                Assert.AreEqual(8393, ms.Length);
                ms.Position = 0;
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(ms, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }
    }
}
