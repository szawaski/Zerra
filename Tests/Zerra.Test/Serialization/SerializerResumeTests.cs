// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Json;
using Zerra.Serialization.Json.IO;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
#if DEBUG
    //the readers and writers in testing mode stop every other read and write as if the buffer ran out, so every converter resumes where it left off
    //the mode is static, so these run alone, and other tests leave it on, so it's turned off for the expected values
    [Collection(nameof(SerializerTestingMode))]
    public class SerializerResumeTests
    {
        public static TheoryData<object> Models() => new()
        {
            TypesAllModel.Create(),
            TypesArrayModel.Create(),
            TypesListTModel.Create(),
            TypesIListTModel.Create(),
            TypesIListTOfTModel.Create(),
            TypesIReadOnlyListTModel.Create(),
            TypesHashSetTModel.Create(),
            TypesISetTModel.Create(),
            TypesISetTOfTModel.Create(),
#if !NETSTANDARD2_0
            TypesIReadOnlySetTModel.Create(),
#endif
            TypesICollectionTModel.Create(),
            TypesICollectionTOfTModel.Create(),
            TypesIReadOnlyCollectionTModel.Create(),
            TypesIEnumerableTModel.Create(),
            TypesDictionaryTModel.Create(),
            TypesIDictionaryTModel.Create(),
            TypesIDictionaryTOfTModel.Create(),
            TypesIReadOnlyDictionaryTModel.Create(),
            TypesCustomCollectionsModel.Create(),
            TypesOtherModel.Create(),
            SpecialTypesModel.CreateArray(20),
            new RecordModel(true) { Property2 = 42, Property3 = "moo" },
        };

        [Theory]
        [MemberData(nameof(Models))]
        public async Task Json_ResumesEveryConverter(object model)
        {
            var type = model.GetType();
            var token = TestContext.Current.CancellationToken;
            foreach (var options in new[] { new JsonSerializerOptions(), new JsonSerializerOptions() { Nameless = true }, new JsonSerializerOptions() { EnumAsNumber = true, DoNotWriteNullProperties = true } })
            {
                JsonWriter.Testing = false;
                JsonReader.Testing = false;
                var expected = JsonSerializer.Serialize(model, type, options);
                var expectedBytes = JsonSerializer.SerializeBytes(model, type, options);
                var expectedModel = JsonSerializer.Deserialize(expected, type, options)!;

                JsonWriter.Testing = true;
                JsonReader.Testing = true;
                try
                {
                    Assert.Equal(expected, JsonSerializer.Serialize(model, type, options));
                    Assert.Equal(expectedBytes, JsonSerializer.SerializeBytes(model, type, options));
                    using (var stream = new MemoryStream())
                    {
                        await JsonSerializer.SerializeAsync(stream, model, type, options, cancellationToken: token);
                        Assert.Equal(expectedBytes, stream.ToArray());
                    }

                    AssertHelper.AreEqual(expectedModel, JsonSerializer.Deserialize(expected, type, options)!);
                    AssertHelper.AreEqual(expectedModel, JsonSerializer.Deserialize(expectedBytes, type, options)!);
                    AssertHelper.AreEqual(expectedModel, (await JsonSerializer.DeserializeAsync(new MemoryStream(expectedBytes), type, options, cancellationToken: token))!);
                    if (!options.Nameless)
                        Assert.Equal(JsonSerializer.DeserializeJsonObject(expected)!.ToString(), JsonSerializer.DeserializeJsonObject(expectedBytes)!.ToString());
                }
                finally
                {
                    JsonWriter.Testing = false;
                    JsonReader.Testing = false;
                }
            }
        }

        [Fact]
        public async Task Json_ResumesObjectsAndNonGenericDictionaries()
        {
            var token = TestContext.Current.CancellationToken;
            var custom = new TypesIDictionaryOfTModel.CustomIDictionary();
            custom[1] = "one";
            var model = new JsonSerializerDataTests.ObjectsModel()
            {
                Value = JsonSerializer.Deserialize<object>("{\"a\":[1,{\"b\":\"c\"},null,true],\"d\":{\"e\":-1.5e3},\"\":\"empty\"}"),
                Dictionary = new System.Collections.Hashtable() { ["a"] = 1, ["b"] = new SimpleModel() { Value1 = 2, Value2 = "two" }, ["c"] = new List<object?>() { 1, null } },
                Custom = custom,
                Json = JsonSerializer.DeserializeJsonObject("{\"x\":[1,2,{\"y\":\"z\"}]}"),
                After = 7,
            };
            JsonWriter.Testing = false;
            JsonReader.Testing = false;
            var expected = JsonSerializer.Serialize(model);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);

            JsonWriter.Testing = true;
            JsonReader.Testing = true;
            try
            {
                Assert.Equal(expected, JsonSerializer.Serialize(model));
                Assert.Equal(expectedBytes, JsonSerializer.SerializeBytes(model));
                using (var stream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(stream, model, cancellationToken: token);
                    Assert.Equal(expectedBytes, stream.ToArray());
                }

                var results = new[]
                {
                    JsonSerializer.Deserialize<JsonSerializerDataTests.ObjectsModel>(expected)!,
                    JsonSerializer.Deserialize<JsonSerializerDataTests.ObjectsModel>(expectedBytes)!,
                    (await JsonSerializer.DeserializeAsync<JsonSerializerDataTests.ObjectsModel>(new MemoryStream(expectedBytes), cancellationToken: token))!,
                };
                JsonWriter.Testing = false;
                foreach (var result in results)
                    Assert.Equal(expected, JsonSerializer.Serialize(result));
            }
            finally
            {
                JsonWriter.Testing = false;
                JsonReader.Testing = false;
            }
        }

        [Theory]
        [MemberData(nameof(Models))]
        public async Task Bytes_ResumesEveryConverter(object model)
        {
            var type = model.GetType();
            var token = TestContext.Current.CancellationToken;
            foreach (var options in new[] { new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.UInt16 }, new ByteSerializerOptions() { UseTypes = true, IndexType = ByteSerializerIndexType.UInt16 }, new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames } })
            {
                ByteWriter.Testing = false;
                ByteReader.Testing = false;
                var expected = ByteSerializer.Serialize(model, type, options);
                var expectedModel = ByteSerializer.Deserialize(expected, type, options)!;

                ByteWriter.Testing = true;
                ByteReader.Testing = true;
                try
                {
                    Assert.Equal(expected, ByteSerializer.Serialize(model, type, options));
                    using (var stream = new MemoryStream())
                    {
                        await ByteSerializer.SerializeAsync(stream, model, type, options, token);
                        Assert.Equal(expected, stream.ToArray());
                    }

                    AssertHelper.AreEqual(expectedModel, ByteSerializer.Deserialize(expected, type, options)!);
                    AssertHelper.AreEqual(expectedModel, (await ByteSerializer.DeserializeAsync(new MemoryStream(expected), type, options, token))!);
                }
                finally
                {
                    ByteWriter.Testing = false;
                    ByteReader.Testing = false;
                }
            }
        }

        public static TheoryData<object> DrainModels() => new()
        {
            TypesAllModel.Create(),
            TypesArrayModel.Create(),
            TypesListTModel.Create(),
            TypesIListTModel.Create(),
            TypesIListTOfTModel.Create(),
            TypesIReadOnlyListTModel.Create(),
            TypesIListModel.Create(),
            TypesIListOfTModel.Create(),
            TypesHashSetTModel.Create(),
            TypesISetTModel.Create(),
            TypesISetTOfTModel.Create(),
#if !NETSTANDARD2_0
            TypesIReadOnlySetTModel.Create(),
#endif
            TypesICollectionModel.Create(),
            TypesICollectionTModel.Create(),
            TypesICollectionTOfTModel.Create(),
            TypesIReadOnlyCollectionTModel.Create(),
            TypesIEnumerableModel.Create(),
            TypesIEnumerableOfTModel.Create(),
            TypesIEnumerableTModel.Create(),
            TypesIEnumerableTOfTModel.Create(),
            TypesDictionaryTModel.Create(),
            TypesIDictionaryTModel.Create(),
            TypesIDictionaryTOfTModel.Create(),
            TypesIReadOnlyDictionaryTModel.Create(),
            TypesIDictionaryModel.Create(),
            TypesIDictionaryOfTModel.Create(),
            TypesCustomCollectionsModel.Create(),
        };

        //a member the target doesn't have is read past, resuming like any other read
        [Theory]
        [MemberData(nameof(DrainModels))]
        public async Task Bytes_ResumesDrain(object model)
        {
            var token = TestContext.Current.CancellationToken;
            var options = new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames, UseTypes = true };
            var sourceType = typeof(DrainSourceModel<>).MakeGenericType(model.GetType());
            var source = Activator.CreateInstance(sourceType)!;
            sourceType.GetProperty(nameof(DrainSourceModel<int>.Before))!.SetValue(source, 1);
            sourceType.GetProperty(nameof(DrainSourceModel<int>.Value))!.SetValue(source, model);
            sourceType.GetProperty(nameof(DrainSourceModel<int>.After))!.SetValue(source, "end");

            ByteWriter.Testing = false;
            ByteReader.Testing = false;
            var bytes = ByteSerializer.Serialize(source, sourceType, options);

            ByteReader.Testing = true;
            try
            {
                var result = ByteSerializer.Deserialize<DrainTargetModel>(bytes, options)!;
                Assert.Equal(1, result.Before);
                Assert.Equal("end", result.After);
                var resultAsync = (await ByteSerializer.DeserializeAsync<DrainTargetModel>(new MemoryStream(bytes), options, token))!;
                Assert.Equal(1, resultAsync.Before);
                Assert.Equal("end", resultAsync.After);
            }
            finally
            {
                ByteReader.Testing = false;
            }
        }

    }
#endif

    [CollectionDefinition(nameof(SerializerTestingMode), DisableParallelization = true)]
    public sealed class SerializerTestingMode { }
}
