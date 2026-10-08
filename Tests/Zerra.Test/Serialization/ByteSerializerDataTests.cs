// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Xunit;
using System.Net;
using Zerra.Serialization.Bytes;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
    public class ByteSerializerDataTests
    {
        public ByteSerializerDataTests()
        {
#if DEBUG
            Zerra.Serialization.Bytes.IO.ByteReader.Testing = true;
            Zerra.Serialization.Bytes.IO.ByteWriter.Testing = true;
#endif
        }

        [Fact]
        public void Enum_AllUnderlyingTypes_RoundTrip()
        {
            AssertEnumRoundTrips(SByteEnum.Min, SByteEnum.Max);
            AssertEnumRoundTrips(ByteEnum.Min, ByteEnum.Max);
            AssertEnumRoundTrips(Int16Enum.Min, Int16Enum.Max);
            AssertEnumRoundTrips(UInt16Enum.Min, UInt16Enum.Max);
            AssertEnumRoundTrips(Int32Enum.Min, Int32Enum.Max);
            AssertEnumRoundTrips(UInt32Enum.Min, UInt32Enum.Max);
            AssertEnumRoundTrips(Int64Enum.Min, Int64Enum.Max);
            AssertEnumRoundTrips(UInt64Enum.Min, UInt64Enum.Max);
        }

        private static void AssertEnumRoundTrips<T>(params T[] values) where T : struct, Enum
        {
            foreach (var value in values)
            {
                Assert.Equal(value, ByteSerializer.Deserialize<T>(ByteSerializer.Serialize(value)));
                Assert.Equal(value, ByteSerializer.Deserialize<T?>(ByteSerializer.Serialize<T?>(value)));
                Assert.Equal([value, value], ByteSerializer.Deserialize<T[]>(ByteSerializer.Serialize(new[] { value, value })));

                var dictionary = new Dictionary<T, int>() { [value] = 1 };
                Assert.Equal(dictionary, ByteSerializer.Deserialize<Dictionary<T, int>>(ByteSerializer.Serialize(dictionary)));
            }
        }
        [Fact]
        public void DictionaryComplexKeys_RoundTrip()
        {
            var value = new Dictionary<SimpleModel, int?>() { [new() { Value1 = 1, Value2 = "A" }] = 1, [new() { Value1 = 2, Value2 = "B" }] = null };
            var concurrent = new ConcurrentDictionary<SimpleModel, int?>(value);
            IReadOnlyDictionary<SimpleModel, int?> readOnly = value;

            static string[] Flatten(IEnumerable<KeyValuePair<SimpleModel, int?>> dictionary)
                => dictionary.Select(x => $"{x.Key.Value1}|{x.Key.Value2}|{x.Value}").OrderBy(x => x).ToArray();

            Assert.Equal(Flatten(value), Flatten(ByteSerializer.Deserialize<Dictionary<SimpleModel, int?>>(ByteSerializer.Serialize(value))!));
            Assert.Equal(Flatten(value), Flatten(ByteSerializer.Deserialize<ConcurrentDictionary<SimpleModel, int?>>(ByteSerializer.Serialize(concurrent))!));
            Assert.Equal(Flatten(value), Flatten(ByteSerializer.Deserialize<IReadOnlyDictionary<SimpleModel, int?>>(ByteSerializer.Serialize(readOnly))!));
        }
        [Theory]
        [InlineData("")]
        [InlineData("plain")]
        [InlineData("ünïcödé café 日本語")]
        [InlineData("😀 a\"b\\c")]
        public void Strings_RoundTrip(string value)
        {
            Assert.Equal(value, ByteSerializer.Deserialize<string>(ByteSerializer.Serialize(value)));

            var list = new List<string?>() { value, null, value + "x" };
            Assert.Equal(list, ByteSerializer.Deserialize<List<string?>>(ByteSerializer.Serialize(list)));

            var array = new string?[] { value, null };
            Assert.Equal(array, ByteSerializer.Deserialize<string?[]>(ByteSerializer.Serialize(array)));
        }
        [Fact]
        public void TypesBasic()
        {
            var model1 = TypesBasicModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(343, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesBasicModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesArray()
        {
            var model1 = TypesArrayModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesArrayModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesListT()
        {
            var model1 = TypesListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIListT()
        {
            var model1 = TypesIListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIListTOfT()
        {
            var model1 = TypesIListTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIListTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIReadOnlyTList()
        {
            var model1 = TypesIReadOnlyListTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyListTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TypesHashSetT()
        {
            var model1 = TypesHashSetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesHashSetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesISetT()
        {
            var model1 = TypesISetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesISetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesISetTOfT()
        {
            var model1 = TypesISetTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesISetTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIReadOnlySetT()
        {
            var model1 = TypesIReadOnlySetTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1128, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlySetTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
        public void TypesICollectionT()
        {
            var model1 = TypesICollectionTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesICollectionTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesICollectionTOfT()
        {
            var model1 = TypesICollectionTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesICollectionTOfTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIReadOnlyCollectionT()
        {
            var model1 = TypesIReadOnlyCollectionTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyCollectionTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIEnumerableT()
        {
            var model1 = TypesIEnumerableTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIEnumerableTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIEnumerableTOfT()
        {
            var model1 = TypesIEnumerableTOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(1131, bytes.Length);
        }

        [Fact]
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

        [Fact]
        public void TypesIEnumerableOfT()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesIEnumerableOfTModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
        }

        [Fact]
        public void TypesDictionaryT()
        {
            var model1 = TypesDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(698, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesIDictionaryT()
        {
            var model1 = TypesIDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(249, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
        public void TypesIReadOnlyDictionaryT()
        {
            var model1 = TypesIReadOnlyDictionaryTModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(193, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesIReadOnlyDictionaryTModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void TypesCustomCollections()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            var model1 = TypesCustomCollectionsModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            //Assert.Equal(254, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesCustomCollectionsModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesOther()
        {
            var model1 = TypesOtherModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(70, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesOtherModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesType()
        {
            var model1 = TypeModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(186, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypeModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesCore()
        {
            var model1 = TypesCoreModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            Assert.Equal(304, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesCoreModel>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void TypesAll()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
            };

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            Assert.Equal(9031, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesAllModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void EmptyObjectsAndNulls()
        {
            var model1 = new NoPropertiesModel();
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<NoPropertiesModel>(bytes1);
            Assert.NotNull(model2);

            var model3 = new NoPropertiesModel[] {
                new(),
                new()
            };
            var bytes3 = ByteSerializer.Serialize(model3);
            var model4 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes3);
            Assert.Equal(2, model4.Length);
            Assert.NotNull(model4[0]);
            Assert.NotNull(model4[1]);

            var model5 = new NoPropertiesModel[] {
               null,
               null
            };
            var bytes5 = ByteSerializer.Serialize(model5);
            var model6 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes5);
            Assert.Equal(2, model6.Length);
            Assert.Null(model6[0]);
            Assert.Null(model6[1]);

            var bytes7 = ByteSerializer.Serialize(null);
            var model7 = ByteSerializer.Deserialize<NoPropertiesModel>(bytes7);
            Assert.Null(model7);

            var bytes8 = ByteSerializer.Serialize(null);
            var model8 = ByteSerializer.Deserialize<NoPropertiesModel[]>(bytes8);
            Assert.Null(model8);

            var model9 = Array.Empty<NoPropertiesModel>();
            var bytes9 = ByteSerializer.Serialize(model9);
            var model10 = ByteSerializer.Deserialize<IEnumerable<NoPropertiesModel>>(bytes9);
            Assert.Equal(model9.GetType(), model10?.GetType());

            var model11 = new ArrayChainModel[]{
                new()
                {
                    ID = Guid.NewGuid(),
                    Children = null
                }
            };
            var bytes11 = ByteSerializer.Serialize(model11);
            var model12 = ByteSerializer.Deserialize<ArrayChainModel[]>(bytes11);
            Assert.Equal(model11[0].Children, model12[0].Children);
        }

        [Fact]
        public void Arrays()
        {
            var model1 = SimpleModel.CreateArray();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<SimpleModel[]>(bytes);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
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

        [Fact]
        public void IndexAttribute()
        {
            var model1 = TestSerializerIndexModel1.Create();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<TestSerializerIndexModel2>(bytes);
            AssertHelper.AreEqual(model1.Value1, model2.Value1);
            AssertHelper.AreEqual(model1.Value2, model2.Value2);
            AssertHelper.AreEqual(model1.Value3, model2.Value3);
            AssertHelper.AreNotEqual(model1.Value4, model2.Value4);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void CookieObject()
        {
            var model1 = new Cookie("tester", "stuff", null, null);
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<Cookie>(bytes);
            Assert.Equal(model1.Name, model2.Name);
            Assert.Equal(model1.Value, model2.Value);
            Assert.Equal(model1.Path, model2.Path);
            Assert.Equal(model1.Domain, model2.Domain);
        }

        [Fact]
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

            Assert.Equal(5, model2.Property1);
            Assert.Equal(6, model2.Property2);
        }

        [Fact]
        public void StringArrayOfArrayThing()
        {
            var model1 = new string[][] { ["a", "b", "c"], ["d", "e", "f"] };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<string[][]>(bytes);
        }

        [Fact]
        public void Record()
        {
            var model1 = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<RecordModel>(bytes);
            Assert.NotNull(model2);
            Assert.Equal(model1.Property1, model2.Property1);
            Assert.Equal(model1.Property2, model2.Property2);
            Assert.Equal(model1.Property3, model2.Property3);
        }

        [Fact]
        public void DateTimeTypes()
        {
            var dateUtc = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Utc);
            var bytes = ByteSerializer.Serialize(dateUtc);
            var dateUtc2 = ByteSerializer.Deserialize<DateTime>(bytes);
            Assert.Equal(dateUtc, dateUtc2);
            Assert.Equal(DateTimeKind.Utc, dateUtc2.Kind);

            var dateLocal = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Local);
            bytes = ByteSerializer.Serialize(dateLocal);
            var dateLocal2 = ByteSerializer.Deserialize<DateTime>(bytes);
            var dateLocalUtc = dateLocal.ToUniversalTime();
            Assert.Equal(dateLocalUtc, dateLocal2);
            Assert.Equal(DateTimeKind.Utc, dateLocal2.Kind);

            var dateUnspecified = new DateTime(2024, 12, 5, 18, 10, 5, 123, 456, DateTimeKind.Unspecified);
            bytes = ByteSerializer.Serialize(dateUnspecified);
            var dateUnspecified2 = ByteSerializer.Deserialize<DateTime>(bytes);
            Assert.Equal(dateUnspecified, dateUnspecified2);
            Assert.Equal(DateTimeKind.Utc, dateUnspecified2.Kind);
        }

        [Fact]
        public void CustomType()
        {
            ByteSerializer.AddConverter(typeof(CustomType), () => new CustomTypeByteConverter());

            var model1 = CustomTypeModel.Create();
            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<CustomTypeModel>(bytes);
            Assert.NotNull(model2);
            Assert.NotNull(model2.Value);
            Assert.Equal(model1.Value.Things1, model2.Value.Things1);
            Assert.Equal(model1.Value.Things2, model2.Value.Things2);
        }

        [Fact]
        public async Task Stream()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
            };

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(ms, model1, options, TestContext.Current.CancellationToken);
                Assert.Equal(9031, ms.Length);
                ms.Position = 0;
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(ms, options, TestContext.Current.CancellationToken);
                AssertHelper.AreEqual(model1, model2);
            }
        }

        [Fact]
        public void CancellationTokens()
        {
            var model1 = CancellationToken.None;
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<CancellationToken>(bytes1);
            Assert.Equal(model1, model2);

            CancellationToken? model3 = CancellationToken.None;
            var bytes2 = ByteSerializer.Serialize(model3);
            var model4 = ByteSerializer.Deserialize<CancellationToken?>(bytes2);
            Assert.Equal(model3, model4);

            CancellationToken? model5 = CancellationToken.None;
            var bytes3 = ByteSerializer.Serialize(model5);
            var model6 = ByteSerializer.Deserialize<CancellationToken?>(bytes3);
            Assert.Equal(model5, model6);
        }

        [Fact]
        public void Constructors()
        {
            var model1 = new TestSerializerConstructor("Five", 5);
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<TestSerializerConstructor>(bytes1);
            Assert.NotNull(model2);
            Assert.Equal(model1._Value1, model2._Value1);
            Assert.Equal(model1.value2, model2.value2);
        }

        [Fact]
        public void Required()
        {
            var model1 = new TestSerializerRequired { Value1 = 1, Value2 = 2, Value3 = 3 };
            var bytes1 = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<TestSerializerRequired>(bytes1);
            Assert.NotNull(model2);
            Assert.Equal(model1.Value1, model2.Value1);
            Assert.Equal(model1.Value2, model2.Value2);
            Assert.Equal(model1.Value3, model2.Value3);
        }

        [Fact]
        public void DrainBytes()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.MemberNames,
                UseTypes = true,
            };

            var model1 = TypesAllModel.Create();
            var bytes = ByteSerializer.Serialize(model1, options);
            Assert.Equal(223478, bytes.Length);
            var model2 = ByteSerializer.Deserialize<TypesBasicModel>(bytes, options);
            AssertHelper.AreEqual(model1, model2);
        }

        [Fact]
        public void DrainBytes_EveryCollectionType()
        {
            AssertDrains(TypesAllModel.Create());
            AssertDrains(TypesArrayModel.Create());
            AssertDrains(TypesListTModel.Create());
            AssertDrains(TypesIListTModel.Create());
            AssertDrains(TypesIListTOfTModel.Create());
            AssertDrains(TypesIReadOnlyListTModel.Create());
            AssertDrains(TypesIListModel.Create());
            AssertDrains(TypesIListOfTModel.Create());
            AssertDrains(TypesHashSetTModel.Create());
            AssertDrains(TypesISetTModel.Create());
            AssertDrains(TypesISetTOfTModel.Create());
            AssertDrains(TypesIReadOnlySetTModel.Create());
            AssertDrains(TypesICollectionModel.Create());
            AssertDrains(TypesICollectionTModel.Create());
            AssertDrains(TypesICollectionTOfTModel.Create());
            AssertDrains(TypesIReadOnlyCollectionTModel.Create());
            AssertDrains(TypesIEnumerableModel.Create());
            AssertDrains(TypesIEnumerableOfTModel.Create());
            AssertDrains(TypesIEnumerableTModel.Create());
            AssertDrains(TypesIEnumerableTOfTModel.Create());
            AssertDrains(TypesDictionaryTModel.Create());
            AssertDrains(TypesIDictionaryTModel.Create());
            AssertDrains(TypesIDictionaryTOfTModel.Create());
            AssertDrains(TypesIReadOnlyDictionaryTModel.Create());
            AssertDrains(TypesIDictionaryModel.Create());
            AssertDrains(TypesIDictionaryOfTModel.Create());
            AssertDrains(TypesCustomCollectionsModel.Create());
            AssertDrains(new RecordModel(true) { Property2 = 42, Property3 = "moo" });
            AssertDrains(new[] { new RecordModel(true) { Property2 = 1 }, new RecordModel(false) { Property3 = "x" } });
        }

        private static void AssertDrains<T>(T value)
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.MemberNames,
                UseTypes = true,
            };

            var bytes = ByteSerializer.Serialize(new DrainSourceModel<T>() { Before = 1, Value = value, After = "end" }, options);
            var result = ByteSerializer.Deserialize<DrainTargetModel>(bytes, options)!;
            Assert.Equal(1, result.Before);
            Assert.Equal("end", result.After);
        }

        [Fact]
        public void DrainBytes_WithoutTypes_Throws()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.MemberNames
            };

            var bytes = ByteSerializer.Serialize(new DrainSourceModel<int>() { Before = 1, Value = 2, After = "end" }, options);
            var ex = Assert.Throws<NotSupportedException>(() => ByteSerializer.Deserialize<DrainTargetModel>(bytes, options));
            Assert.Contains(nameof(ByteSerializerOptions.UseTypes), ex.Message);
        }

        [Fact]
        public void LargeModel()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
            };

            var models = new List<TypesAllModel>();
            for (var i = 0; i < 1000; i++)
                models.Add(TypesAllModel.Create());

            var bytes = ByteSerializer.Serialize(models, options);
            var result = ByteSerializer.Deserialize<TypesAllModel[]>(bytes, options);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [Fact]
        public async Task LargeModelStream()
        {
            var options = new ByteSerializerOptions()
            {
                IndexType = ByteSerializerIndexType.UInt16
            };

            var models = new List<TypesAllModel>();
            for (var i = 0; i < 10000; i++)
                models.Add(TypesAllModel.Create());

            using var stream = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream, models, options, TestContext.Current.CancellationToken);
            stream.Position = 0;
            var result = await ByteSerializer.DeserializeAsync<TypesAllModel[]>(stream, options, TestContext.Current.CancellationToken);

            for (var i = 0; i < models.Count; i++)
                AssertHelper.AreEqual(models[i], result[i]);
        }

        [Fact]
        public async Task LargeValueToExpandBuffer()
        {
            var model = new string('x', 10000);

            using var stream1 = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream1, model, null, TestContext.Current.CancellationToken);
            stream1.Position = 0;
            var result1 = await ByteSerializer.DeserializeAsync<string>(stream1, null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result1);

            using var stream2 = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream2, model, null, TestContext.Current.CancellationToken);
            stream2.Position = 0;
            var result2 = await ByteSerializer.DeserializeAsync(stream2, typeof(string), null, TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result2);

            using var stream3 = new MemoryStream();
            ByteSerializer.Serialize(stream3, model);
            stream3.Position = 0;
            var result3 = ByteSerializer.Deserialize<string>(stream3);
            AssertHelper.AreEqual(model, result3);

            using var stream4 = new MemoryStream();
            ByteSerializer.Serialize(stream4, model);
            stream4.Position = 0;
            var result4 = ByteSerializer.Deserialize(stream4, typeof(string));
            AssertHelper.AreEqual(model, result4);
        }

        [Fact]
        public void GraphMembers()
        {
            var model1 = new Graph<GraphModel>(x => x.Prop1, x => x.Class.Value1);
            model1.RemoveMember(nameof(GraphModel.Prop2));

            var bytes = ByteSerializer.Serialize(model1);
            var model2 = ByteSerializer.Deserialize<Graph<GraphModel>>(bytes);

            Assert.NotNull(model2);
            Assert.Equal(model1, model2);
            Assert.True(model2.HasMember(nameof(GraphModel.Prop1)));
            Assert.False(model2.HasMember(nameof(GraphModel.Prop2)));
            var childGraph = model2.GetChildGraph(nameof(GraphModel.Class));
            Assert.NotNull(childGraph);
            Assert.True(childGraph.HasMember(nameof(SimpleModel.Value1)));
        }

        [Fact]
        public void GraphAsBaseType()
        {
            var options = new ByteSerializerOptions()
            {
                UseTypes = true
            };

            Graph model1 = new Graph<GraphModel>(true, x => x.Prop1);

            var bytes = ByteSerializer.Serialize(model1, options);
            var model2 = ByteSerializer.Deserialize<Graph>(bytes, options);

            _ = Assert.IsType<Graph<GraphModel>>(model2);
            Assert.Equal(model1, model2);
            Assert.True(model2.IncludeAllMembers);
        }

        [Fact]
        public async Task CoreTypeCollections_LargerThanStreamBuffer()
        {
            await AssertLargeCollections(i => i % 2 == 0);
            await AssertLargeCollections(i => (byte)i);
            await AssertLargeCollections(i => (sbyte)i);
            await AssertLargeCollections(i => (short)i);
            await AssertLargeCollections(i => (ushort)i);
            await AssertLargeCollections(i => i);
            await AssertLargeCollections(i => (uint)i);
            await AssertLargeCollections(i => (long)i << 20);
            await AssertLargeCollections(i => (ulong)i << 20);
            await AssertLargeCollections(i => i * 1.5f);
            await AssertLargeCollections(i => i * 1.5d);
            await AssertLargeCollections(i => i * 1.5m);
            await AssertLargeCollections(i => (char)(i + 32));
            await AssertLargeCollections(i => new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
            await AssertLargeCollections(i => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)).AddMinutes(i));
            await AssertLargeCollections(i => TimeSpan.FromMinutes(i));
            await AssertLargeCollections(i => new DateOnly(2000, 1, 1).AddDays(i));
            await AssertLargeCollections(i => new TimeOnly(0, 0).Add(TimeSpan.FromSeconds(i)));
            await AssertLargeCollections(i => new Guid(i, 0, 0, new byte[8]));
            await AssertLargeCollections(i => $"value {i}");

            await AssertLargeCollections<bool?>(i => i % 3 == 0 ? null : i % 2 == 0);
            await AssertLargeCollections<byte?>(i => i % 3 == 0 ? null : (byte)i);
            await AssertLargeCollections<sbyte?>(i => i % 3 == 0 ? null : (sbyte)i);
            await AssertLargeCollections<short?>(i => i % 3 == 0 ? null : (short)i);
            await AssertLargeCollections<ushort?>(i => i % 3 == 0 ? null : (ushort)i);
            await AssertLargeCollections<int?>(i => i % 3 == 0 ? null : i);
            await AssertLargeCollections<uint?>(i => i % 3 == 0 ? null : (uint)i);
            await AssertLargeCollections<long?>(i => i % 3 == 0 ? null : (long)i << 20);
            await AssertLargeCollections<ulong?>(i => i % 3 == 0 ? null : (ulong)i << 20);
            await AssertLargeCollections<float?>(i => i % 3 == 0 ? null : i * 1.5f);
            await AssertLargeCollections<double?>(i => i % 3 == 0 ? null : i * 1.5d);
            await AssertLargeCollections<decimal?>(i => i % 3 == 0 ? null : i * 1.5m);
            await AssertLargeCollections<char?>(i => i % 3 == 0 ? null : (char)(i + 32));
            await AssertLargeCollections<DateTime?>(i => i % 3 == 0 ? null : new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
            await AssertLargeCollections<DateTimeOffset?>(i => i % 3 == 0 ? null : new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)).AddMinutes(i));
            await AssertLargeCollections<TimeSpan?>(i => i % 3 == 0 ? null : TimeSpan.FromMinutes(i));
            await AssertLargeCollections<DateOnly?>(i => i % 3 == 0 ? null : new DateOnly(2000, 1, 1).AddDays(i));
            await AssertLargeCollections<TimeOnly?>(i => i % 3 == 0 ? null : new TimeOnly(0, 0).Add(TimeSpan.FromSeconds(i)));
            await AssertLargeCollections<Guid?>(i => i % 3 == 0 ? null : new Guid(i, 0, 0, new byte[8]));
        }

        private static async Task AssertLargeCollections<T>(Func<int, T> create)
        {
            const int count = 20_000;
            var items = Enumerable.Range(0, count).Select(create).ToArray();
            var distinct = items.Distinct().ToArray();
            var token = TestContext.Current.CancellationToken;

            var array = ByteSerializer.Serialize(items);
            Assert.True(array.Length > 16 * 1024, typeof(T).Name);
            Assert.Equal(items, ByteSerializer.Deserialize<T[]>(new MemoryStream(array)));
            Assert.Equal(items, await ByteSerializer.DeserializeAsync<T[]>(new MemoryStream(array), cancellationToken: token));

            var list = ByteSerializer.Serialize(items.ToList());
            Assert.Equal(items, ByteSerializer.Deserialize<List<T>>(new MemoryStream(list)));
            Assert.Equal(items, await ByteSerializer.DeserializeAsync<List<T>>(new MemoryStream(list), cancellationToken: token));

            var set = ByteSerializer.Serialize(distinct.ToHashSet());
            Assert.Equal(distinct.ToHashSet(), ByteSerializer.Deserialize<HashSet<T>>(new MemoryStream(set)));
            Assert.Equal(distinct.ToHashSet(), await ByteSerializer.DeserializeAsync<HashSet<T>>(new MemoryStream(set), cancellationToken: token));
        }

        [Fact]
        public async Task StreamLargerThanBuffer_AllTypes()
        {
            await AssertStreamRoundTrip(Enumerable.Range(0, 8).Select(_ => TypesAllModel.Create()).ToArray(), new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.UInt16 });
            await AssertStreamRoundTrip(Enumerable.Range(0, 8).Select(_ => TypesAllModel.Create()).ToArray(), new ByteSerializerOptions() { UseTypes = true, IndexType = ByteSerializerIndexType.UInt16 });
            await AssertStreamRoundTrip(Enumerable.Range(0, 8).Select(_ => TypesAllModel.Create()).ToArray(), new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames });
            await AssertStreamRoundTrip(SpecialTypesModel.CreateArray(2000), null);
        }

        private static async Task AssertStreamRoundTrip<T>(T value, ByteSerializerOptions? options)
        {
            var token = TestContext.Current.CancellationToken;
            var expected = ByteSerializer.Serialize(value, options);
            Assert.True(expected.Length > 3 * 16 * 1024, typeof(T).Name);

            using (var stream = new MemoryStream())
            {
                ByteSerializer.Serialize(stream, value, options);
                Assert.Equal(expected, stream.ToArray());
            }
            using (var stream = new MemoryStream())
            {
                ByteSerializer.Serialize(stream, (object?)value, typeof(T), options);
                Assert.Equal(expected, stream.ToArray());
            }
            using (var stream = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(stream, value, options, token);
                Assert.Equal(expected, stream.ToArray());
            }
            using (var stream = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(stream, (object?)value, typeof(T), options, token);
                Assert.Equal(expected, stream.ToArray());
            }
            if (value!.GetType() == typeof(T))
            {
                //by the runtime type, the same when it's the declared type
                using (var stream = new MemoryStream())
                {
                    ByteSerializer.Serialize(stream, (object)value, options);
                    Assert.Equal(expected, stream.ToArray());
                }
                using (var stream = new MemoryStream())
                {
                    await ByteSerializer.SerializeAsync(stream, (object)value, options, token);
                    Assert.Equal(expected, stream.ToArray());
                }
            }

            Assert.Equal(expected, ByteSerializer.Serialize(ByteSerializer.Deserialize<T>(new MemoryStream(expected), options), options));
            Assert.Equal(expected, ByteSerializer.Serialize((T?)ByteSerializer.Deserialize(new MemoryStream(expected), typeof(T), options), options));
            Assert.Equal(expected, ByteSerializer.Serialize(await ByteSerializer.DeserializeAsync<T>(new MemoryStream(expected), options, token), options));
            Assert.Equal(expected, ByteSerializer.Serialize((T?)await ByteSerializer.DeserializeAsync(new MemoryStream(expected), typeof(T), options, token), options));
        }

        public class PartialRecordSource
        {
            public int Property2 { get; set; }
            public string? Property3 { get; set; }
        }

        [Fact]
        public void ConstructorArgumentMissing()
        {
            var options = new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames };
            var bytes = ByteSerializer.Serialize(new PartialRecordSource() { Property2 = 5, Property3 = "a" }, options);
            var result = ByteSerializer.Deserialize<RecordModel>(bytes, options)!;
            Assert.False(result.Property1);
            Assert.Equal(5, result.Property2);
            Assert.Equal("a", result.Property3);
        }

        [Fact]
        public void Enum_ReflectionOnlyTypes_RoundTrip()
        {
            AssertEnumRoundTrips(DayOfWeek.Sunday, DayOfWeek.Saturday);
            AssertEnumRoundTrips(System.Text.Json.JsonTokenType.None, System.Text.Json.JsonTokenType.Null);
            Assert.Null(ByteSerializer.Deserialize<DayOfWeek?>(ByteSerializer.Serialize<DayOfWeek?>(null)));
            Assert.Equal([DayOfWeek.Monday, null], ByteSerializer.Deserialize<DayOfWeek?[]>(ByteSerializer.Serialize(new DayOfWeek?[] { DayOfWeek.Monday, null })));
            Assert.Equal([System.Text.Json.JsonTokenType.String, null], ByteSerializer.Deserialize<System.Text.Json.JsonTokenType?[]>(ByteSerializer.Serialize(new System.Text.Json.JsonTokenType?[] { System.Text.Json.JsonTokenType.String, null })));
        }

        [Fact]
        public void TypeValues_WithUseTypes_RoundTrip()
        {
            //a Type's runtime type is RuntimeType, it's still written as a Type
            var options = new ByteSerializerOptions() { UseTypes = true };

            var model = ByteSerializer.Deserialize<SpecialTypesModel>(ByteSerializer.Serialize(new SpecialTypesModel() { TypeValue = typeof(string), After = 5 }, options), options)!;
            Assert.Equal(typeof(string), model.TypeValue);
            Assert.Equal(5, model.After);

            Assert.Equal(typeof(int), ByteSerializer.Deserialize<Type>(ByteSerializer.Serialize<Type>(typeof(int), options), options));
            Assert.Equal(typeof(int), ByteSerializer.Deserialize<object>(ByteSerializer.Serialize<object>(typeof(int), options), options));
            Assert.Equal([typeof(int), typeof(string)], ByteSerializer.Deserialize<Type[]>(ByteSerializer.Serialize(new[] { typeof(int), typeof(string) }, options), options));
        }

        public class MixedList : System.Collections.ArrayList { }
        public class MixedDictionary : System.Collections.Hashtable { }
        public sealed class MixedEnumerable : System.Collections.IEnumerable
        {
            private readonly object[] items;
            public MixedEnumerable(params object[] items) => this.items = items;
            public System.Collections.IEnumerator GetEnumerator() => items.GetEnumerator();
        }

        public class MixedCollectionsModel
        {
            public MixedList? List { get; set; }
            public MixedDictionary? Dictionary { get; set; }
            public System.Collections.IList? DeclaredList { get; set; }
            public System.Collections.IDictionary? DeclaredDictionary { get; set; }
            public System.Collections.IEnumerable? DeclaredEnumerable { get; set; }
            public System.Collections.ICollection? DeclaredCollection { get; set; }
            public int After { get; set; }
        }

        [Fact]
        public void NonGenericCollections_MixedItemsWithUseTypes()
        {
            var types = new ByteSerializerOptions() { UseTypes = true };
            var simple = new SimpleModel() { Value1 = 3, Value2 = "c" };

            var list = new MixedList { 1, "two", simple, null };
            var dictionary = new MixedDictionary { { "a", 1 }, { 2, "b" }, { 3L, simple } };
            var model = new MixedCollectionsModel()
            {
                List = list,
                Dictionary = dictionary,
                DeclaredList = list,
                DeclaredDictionary = dictionary,
                DeclaredEnumerable = new List<object?> { 4, "five" },
                DeclaredCollection = new object?[] { 6, "seven" },
            };

            var result = ByteSerializer.Deserialize<MixedCollectionsModel>(ByteSerializer.Serialize(model, types), types)!;
            foreach (var resultList in new[] { result.List!, (System.Collections.IList)result.DeclaredList! })
            {
                Assert.IsType<MixedList>(resultList);
                Assert.Equal(1, resultList[0]);
                Assert.Equal("two", resultList[1]);
                Assert.Equal(3, Assert.IsType<SimpleModel>(resultList[2]).Value1);
                Assert.Null(resultList[3]);
            }
            foreach (var resultDictionary in new[] { result.Dictionary!, (System.Collections.IDictionary)result.DeclaredDictionary! })
            {
                Assert.IsType<MixedDictionary>(resultDictionary);
                Assert.Equal(1, resultDictionary["a"]);
                Assert.Equal("b", resultDictionary[2]);
                Assert.Equal("c", Assert.IsType<SimpleModel>(resultDictionary[3L]).Value2);
            }
            Assert.Equal(new object?[] { 4, "five" }, result.DeclaredEnumerable!.Cast<object?>());
            Assert.Equal(new object?[] { 6, "seven" }, result.DeclaredCollection!.Cast<object?>());

            //a type that's only IEnumerable can be written but not read, there's nothing to add the items to
            var enumerableBytes = ByteSerializer.Serialize(new MixedEnumerable(1, "two", simple), types);
            Assert.NotEmpty(enumerableBytes);
            _ = Assert.ThrowsAny<Exception>(() => ByteSerializer.Deserialize<MixedEnumerable>(enumerableBytes, types));

        }

        [Fact]
        public void NonGenericCollections_WithoutUseTypes_Throw()
        {
            //without the types the bytes can't say what an item declared as object is
            foreach (var model in new[]
            {
                new MixedCollectionsModel() { List = new MixedList { 1 } },
                new MixedCollectionsModel() { Dictionary = new MixedDictionary { { "a", 1 } } },
                new MixedCollectionsModel() { DeclaredList = new MixedList { 1 } },
                new MixedCollectionsModel() { DeclaredDictionary = new MixedDictionary { { "a", 1 } } },
                new MixedCollectionsModel() { DeclaredEnumerable = new object?[] { 1 } },
                new MixedCollectionsModel() { DeclaredCollection = new List<object?> { 1 } },
            })
            {
                _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(model));
            }
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(new MixedEnumerable(1, "two")));

            //nulls and empty collections have nothing to lose
            var result = ByteSerializer.Deserialize<MixedCollectionsModel>(ByteSerializer.Serialize(new MixedCollectionsModel()
            {
                List = new MixedList { null, null },
                DeclaredList = new MixedList(),
                DeclaredDictionary = new MixedDictionary(),
                DeclaredEnumerable = new object?[] { null },
                DeclaredCollection = new List<object?>(),
                After = 8,
            }))!;
            Assert.Equal(new object?[] { null, null }, result.List!.Cast<object?>());
            Assert.Empty(result.DeclaredList!);
            Assert.Empty(result.DeclaredDictionary!);
            Assert.Equal(new object?[] { null }, result.DeclaredEnumerable!.Cast<object?>());
            Assert.Empty(result.DeclaredCollection!);
            Assert.Equal(8, result.After);
        }

        [Fact]
        public async Task ObjectValues_WithoutUseTypes_Throw()
        {
            var types = new ByteSerializerOptions() { UseTypes = true };

            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize<object>(5));
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize<object>("text"));
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(new object()));
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(new List<object?>() { 1 }));
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(new object?[] { "two" }));
            _ = Assert.Throws<NotSupportedException>(() => ByteSerializer.Serialize(new Dictionary<string, object?>() { { "a", 1 } }));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => ByteSerializer.SerializeAsync<object>(new MemoryStream(), 5, cancellationToken: TestContext.Current.CancellationToken));

            //nulls have nothing to lose
            Assert.Null(ByteSerializer.Deserialize<object>(ByteSerializer.Serialize<object?>(null)));
            Assert.Equal(new object?[] { null, null }, ByteSerializer.Deserialize<List<object?>>(ByteSerializer.Serialize(new List<object?>() { null, null })));
            var nullValues = ByteSerializer.Deserialize<Dictionary<string, object?>>(ByteSerializer.Serialize(new Dictionary<string, object?>() { { "a", null } }))!;
            Assert.Null(Assert.Single(nullValues).Value);

            //with the types every value comes back
            Assert.Equal(5, ByteSerializer.Deserialize<object>(ByteSerializer.Serialize<object>(5, types), types));
            Assert.Equal("text", ByteSerializer.Deserialize<object>(ByteSerializer.Serialize<object>("text", types), types));
            object?[] items = [1, "two", null, 5.5m];
            Assert.Equal(items, ByteSerializer.Deserialize<List<object?>>(ByteSerializer.Serialize(items.ToList(), types), types));
            Assert.NotNull(ByteSerializer.Deserialize<object>(ByteSerializer.Serialize(new object(), types), types));
        }
    }
}
