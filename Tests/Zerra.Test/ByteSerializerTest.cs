// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
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
        public void TypesListT()
        {
            var model1 = TypesListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIListTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIList()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIListModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIListModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIListOfT()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIListOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIListOfTModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesHashSetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesISetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesISetTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlySetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesICollection()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesICollectionModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesICollectionModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesICollectionTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesICollectionTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyCollectionTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIEnumerableTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(1131, bytes.Length);
        }

        [TestMethod]
        public void TypesIEnumerable()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16,
                UseTypes = true
            };

            var model1 = TypesIEnumerableModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIEnumerableModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIEnumerableOfT()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIEnumerableOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
        }

        [TestMethod]
        public void TypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(249, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(249, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIDictionaryTOfT()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIDictionaryTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIDictionaryTOfTModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(193, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIDictionary()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIDictionaryModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIDictionaryModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesIDictionaryOfT()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIDictionaryOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesIDictionaryOfTModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesCustomCollections()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesCustomCollectionsModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            //Assert.AreEqual(254, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesCustomCollectionsModel>(bytes, options);
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
        public void TypesType()
        {
            var model1 = TypeModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(185, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypeModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesCore()
        {
            var model1 = TypesCoreModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.AreEqual(304, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesCoreModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void TypesAll()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
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
                IndexType = ByteSerializerIndexType.UInt16,
                UseTypes = true
            };

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<TypesAllModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [TestMethod]
        public void IndexTypeMemberNames()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.MemberNames
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
                IndexType = ByteSerializerIndexType.UInt16
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
            var model1 = new string[][] { ["a", "b", "c"], ["d", "e", "f"] };
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
        public void DateTimeTypes()
        {
            var dateUtc = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Utc);
            var bytes = ByteSerializer.Serialize(dateUtc);
            var dateUtc2 = ByteSerializer.Deserialize<DateTime>(bytes);
            Assert.AreEqual(dateUtc, dateUtc2);
            Assert.AreEqual(DateTimeKind.Utc, dateUtc2.Kind);

            var dateLocal = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Local);
            bytes = ByteSerializer.Serialize(dateLocal);
            var dateLocal2 = ByteSerializer.Deserialize<DateTime>(bytes);
            var dateLocalUtc = dateLocal.ToUniversalTime();
            Assert.AreEqual(dateLocalUtc, dateLocal2);
            Assert.AreEqual(DateTimeKind.Utc, dateLocal2.Kind);

            var dateUnspecified = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Unspecified);
            bytes = ByteSerializer.Serialize(dateUnspecified);
            var dateUnspecified2 = ByteSerializer.Deserialize<DateTime>(bytes);
            Assert.AreEqual(dateUnspecified, dateUnspecified2);
            Assert.AreEqual(DateTimeKind.Utc, dateUnspecified2.Kind);
        }

        [TestMethod]
        public void CustomType()
        {
            ByteSerializer.AddConverter(typeof(CustomTypeByteConverter<>), typeof(CustomType));

            var model1 = CustomTypeModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<CustomTypeModel>(bytes);
            Assert.IsNotNull(model2);
            Assert.IsNotNull(model2.Value);
            Assert.AreEqual(model1.Value.Things1, model2.Value.Things1);
            Assert.AreEqual(model1.Value.Things2, model2.Value.Things2);
        }

        [TestMethod]
        public async Task Stream()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
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

        [TestMethod]
        public void CancellationTokens()
        {
            var model1 = CancellationToken.None;
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<CancellationToken>(bytes1);
            Assert.AreEqual(model1, model2);

            CancellationToken? model3 = CancellationToken.None;
            var bytes2 = ByteSerializer.Serialize(model3);
            var model4 = ByteSerializer.Deserialize<CancellationToken?>(bytes2);
            Assert.AreEqual(model3, model4);

            CancellationToken? model5 = CancellationToken.None;
            var bytes3 = ByteSerializer.Serialize(model5);
            var model6 = ByteSerializer.Deserialize<CancellationToken?>(bytes3);
            Assert.AreEqual(model5, model6);
        }
    }
}
