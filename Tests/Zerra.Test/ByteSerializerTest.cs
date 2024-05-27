// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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
        public ByteSerializerTest()
        {
#if DEBUG
            JsonSerializer.Testing = false;
#endif
        }

        [TestMethod]
        public void ByteArrayTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = Factory.GetAllTypesModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayEmptyObjectsAndNulls()
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
        public void ByteArrayArrays()
        {
            var model1 = Factory.GetArrayModel();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<BasicModel[]>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayBoxing()
        {
            var options = new ByteSerializerOptions()
            {
                IncludePropertyTypes = true
            };

            var model1 = Factory.GetBoxingModel();
            var bytes = ByteSerializerOld.Serialize(model1, options);
            var model2 = ByteSerializerOld.Deserialize<TestBoxingModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByPropertyName()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UsePropertyNames = true
            };

            var model1 = Factory.GetAllTypesModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<AllTypesReversedModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayBySerializerIndex()
        {
            var model1 = Factory.GetSerializerIndexModel();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<TestSerializerIndexModel2>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByIgnoreSerailizerIndex()
        {
            var options = new ByteSerializerOptions()
            {
                IgnoreIndexAttribute = true
            };

            var model1 = Factory.GetSerializerIndexModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TestSerializerIndexModel2>(bytes, options);
            Factory.AssertAreNotEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayByLargeSerializerIndex()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = Factory.GetSerializerLongIndexModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TestSerializerLongIndexModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public void ByteArrayCookieObject()
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
        public void ByteArrayExceptionObject()
        {
            var model1 = new Exception("bad things happened");
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<Exception>(bytes);
            Assert.AreEqual(model1.Message, model2.Message);
        }

        [TestMethod]
        public void ByteArrayInterface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };
            var bytes = ByteSerializerOld.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<ITestInterface>(bytes);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public void ByteArrayStringArrayOfArrayThing()
        {
            var model1 = new string[][] { new string[] { "a", "b", "c" }, new string[] { "d", "e", "f" } };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<string[][]>(bytes);
        }

        [TestMethod]
        public void ByteArrayHashSet()
        {
            var model1 = Factory.GetHashSetModel();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<HashSetModel>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = Factory.GetAllTypesModel();
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                ms.Position = 0;
                var model2 = await ByteSerializer.DeserializeAsync<AllTypesModel>(ms, options);
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task StreamDeserializeTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = Factory.GetAllTypesModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = await ByteSerializer.DeserializeAsync<AllTypesModel>(ms, options);
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task StreamSerializeTypes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16
            };

            var model1 = Factory.GetAllTypesModel();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                bytes = ms.ToArray();
            }

            var model2 = ByteSerializer.Deserialize<AllTypesModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamDeserializeByPropertyName()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UsePropertyNames = true
            };

            var model1 = Factory.GetAllTypesModel();
            var bytes = ByteSerializer.Serialize(model1, options);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = await ByteSerializer.DeserializeAsync<AllTypesReversedModel>(ms, options);
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task StreamSerializeByPropertyName()
        {
            var options = new ByteSerializerOptions()
            {
                IndexSize = ByteSerializerIndexSize.UInt16,
                UsePropertyNames = true
            };

            var model1 = Factory.GetAllTypesModel();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                bytes = ms.ToArray();
            }

            var model2 = ByteSerializer.Deserialize<AllTypesReversedModel>(bytes, options);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamDeserializeArray()
        {
            var model1 = Factory.GetArrayModel();
            var bytes = ByteSerializer.Serialize(model1);
            using (var ms = new MemoryStream(bytes))
            {
                var model2 = await ByteSerializer.DeserializeAsync<BasicModel[]>(ms);
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task StreamSerializeArray()
        {
            var model1 = Factory.GetArrayModel();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1);
                bytes = ms.ToArray();
            }

            var model2 = ByteSerializer.Deserialize<BasicModel[]>(bytes);
            Factory.AssertAreEqual(model1, model2);
        }

        [TestMethod]
        public async Task StreamArrayInterface()
        {
            ITestInterface model1 = new TestInterfaceImplemented()
            {
                Property1 = 5,
                Property2 = 6,
                Property3 = 7
            };

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await ByteSerializerOld.SerializeAsync(ms, model1);
                bytes = ms.ToArray();
            }

            var model2 = ByteSerializerOld.Deserialize<ITestInterface>(bytes);

            Assert.AreEqual(5, model2.Property1);
            Assert.AreEqual(6, model2.Property2);
        }

        [TestMethod]
        public async Task StreamArrayBoxing()
        {
            var options = new ByteSerializerOptions()
            {
                IncludePropertyTypes = true
            };

            var model1 = Factory.GetBoxingModel();
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options);
                ms.Position = 0;
                var model2 = await ByteSerializerOld.DeserializeAsync<TestBoxingModel>(ms, options);
                Factory.AssertAreEqual(model1, model2);
            }
        }

        [TestMethod]
        public async Task StreamHashSet()
        {
            var model1 = Factory.GetHashSetModel();
            using var stream = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream, model1);
            stream.Position = 0;
            var model2 = await ByteSerializer.DeserializeAsync<HashSetModel>(stream);
            Factory.AssertAreEqual(model1, model2);
        }
    }
}
