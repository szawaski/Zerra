// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Zerra.Serialization;

namespace Zerra.Test
{
    public class Thing1
    {
        [SerializerIndex(12)]
        public Thing2[] Thing2s { get; set; }
    }
    public class Thing2
    {
        [SerializerIndex(0)]
        public Guid? ID { get; set; }
    }

    [TestClass]
    public class ByteSerializerTest
    {
        [TestMethod]
        public void ByteArrayTypes()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetAllTypesModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<AllTypesModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayEmptyObjectsAndNulls()
        {
            var serializer = new ByteSerializer();

            var model1 = new NoPropertiesModel();
            var bytes1 = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<NoPropertiesModel>(bytes1);
            Assert.IsNotNull(model2);

            var model3 = new NoPropertiesModel[] {
                new NoPropertiesModel(),
                new NoPropertiesModel()
            };
            var bytes3 = serializer.Serialize(model3);
            var model4 = serializer.Deserialize<NoPropertiesModel[]>(bytes3);
            Assert.AreEqual(2, model4.Length);
            Assert.IsNotNull(model4[0]);
            Assert.IsNotNull(model4[1]);

            var model5 = new NoPropertiesModel[] {
               null,
               null
            };
            var bytes5 = serializer.Serialize(model5);
            var model6 = serializer.Deserialize<NoPropertiesModel[]>(bytes5);
            Assert.AreEqual(2, model6.Length);
            Assert.IsNull(model6[0]);
            Assert.IsNull(model6[1]);

            var bytes7 = serializer.Serialize(null);
            var model7 = serializer.Deserialize<NoPropertiesModel>(bytes7);
            Assert.IsNull(model7);

            var bytes8 = serializer.Serialize(null);
            var model8 = serializer.Deserialize<NoPropertiesModel[]>(bytes8);
            Assert.IsNull(model8);

            var model9 = new NoPropertiesModel[0];
            var bytes9 = serializer.Serialize(model9);
            var model10 = serializer.Deserialize<IEnumerable<NoPropertiesModel>>(bytes9);
            Assert.AreEqual(model9.GetType(), model10?.GetType());

            var model11 = new ArrayChainModel[]{
                new ArrayChainModel()
                {
                    ID = Guid.NewGuid(),
                    Children = null
                }
            };
            var bytes11 = serializer.Serialize(model11);
            var model12 = serializer.Deserialize<ArrayChainModel[]>(bytes11);
            Assert.AreEqual(model11[0].Children, model12[0].Children);
        }

        [TestMethod]
        public void ByteArrayArrays()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetArrayModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<BasicModel[]>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayBoxing()
        {
            var serializer = new ByteSerializer(false, true);
            var model1 = Factory.GetBoxingModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<TestBoxingModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByPropertyName()
        {
            var serializer = new ByteSerializer(true);
            var model1 = Factory.GetAllTypesModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<AllTypesReversedModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayBySerializerIndex()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetSerializerIndexModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<TestSerializerIndexModel2>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByIgnoreSerailizerIndex()
        {
            var serializer = new ByteSerializer(false, false, true);
            var model1 = Factory.GetSerializerIndexModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<TestSerializerIndexModel2>(bytes);
            Factory.AssertAreNotEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByLargeSerializerIndex()
        {
            var serializer = new ByteSerializer(false, false, false, ByteSerializerIndexSize.UInt16);
            var model1 = Factory.GetSerializerLongIndexModel();
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<TestSerializerLongIndexModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayCookieObject()
        {
            var serializer = new ByteSerializer();
            var model1 = new Cookie("tester", "stuff", null, null);
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<Cookie>(bytes);
            Assert.AreEqual(model1.Name, model2.Name);
            Assert.AreEqual(model1.Value, model2.Value);
            Assert.AreEqual(model1.Path, model2.Path);
            Assert.AreEqual(model1.Domain, model2.Domain);
        }

        [TestMethod]
        public void ByteArrayExceptionObject()
        {
            var serializer = new ByteSerializer();
            var model1 = new Exception("bad things happened");
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<Exception>(bytes);
            Assert.AreEqual(model1.Message, model2.Message);
        }

        [TestMethod]
        public void ByteArrayInterface()
        {
            var serializer = new ByteSerializer();
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<ITestInterface>(bytes);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public void ByteArrayStringArrayOfArrayThing()
        {
            var serializer = new ByteSerializer();
            var model1 = new string[][] { new string[] { "a", "b", "c" }, new string[] { "d", "e", "f" } };
            var bytes = serializer.Serialize(model1);
            var model2 = serializer.Deserialize<string[][]>(bytes);
        }

        [TestMethod]
        public void StreamTypes()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetAllTypesModel();
            using (var ms = new MemoryStream())
            {
                serializer.SerializeAsync(ms, model1).GetAwaiter().GetResult();
                ms.Position = 0;
                var model2 = serializer.DeserializeAsync<AllTypesModel>(ms).GetAwaiter().GetResult();
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public void StreamDeserializeTypes()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetAllTypesModel();
            var bytes = serializer.Serialize(model1);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = serializer.DeserializeAsync<AllTypesModel>(ms).GetAwaiter().GetResult();
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public void StreamSerializeTypes()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetAllTypesModel();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                serializer.SerializeAsync(ms, model1).GetAwaiter().GetResult();
                bytes = ms.ToArray();
            }

            var model2 = serializer.Deserialize<AllTypesModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void StreamDeserializeByPropertyName()
        {
            var serializer = new ByteSerializer(true);
            var model1 = Factory.GetAllTypesModel();
            var bytes = serializer.Serialize(model1);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = serializer.DeserializeAsync<AllTypesReversedModel>(ms).GetAwaiter().GetResult();
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public void StreamSerializeArrayByPropertyName()
        {
            var serializer = new ByteSerializer(true);
            var model1 = Factory.GetAllTypesModel();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                serializer.SerializeAsync(ms, model1).GetAwaiter().GetResult();
                bytes = ms.ToArray();
            }

            var model2 = serializer.Deserialize<AllTypesReversedModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void StreamDeserializeArray()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetArrayModel();
            var bytes = serializer.Serialize(model1);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = serializer.DeserializeAsync<BasicModel[]>(ms).GetAwaiter().GetResult();
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public void StreamSerializeArray()
        {
            var serializer = new ByteSerializer();
            var model1 = Factory.GetArrayModel();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                serializer.SerializeAsync(ms, model1).GetAwaiter().GetResult();
                bytes = ms.ToArray();
            }

            var model2 = serializer.Deserialize<BasicModel[]>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }
    }
}
