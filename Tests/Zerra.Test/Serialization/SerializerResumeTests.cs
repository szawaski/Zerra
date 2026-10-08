// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Json;
using Zerra.Serialization.Json.IO;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
    //the readers and writers in testing mode stop every other read and write as if the buffer ran out, so every converter resumes where it left off
    //the mode is static, so these run alone
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
            TypesIReadOnlySetTModel.Create(),
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

        [Theory]
        [MemberData(nameof(Models))]
        public async Task Bytes_ResumesEveryConverter(object model)
        {
            var type = model.GetType();
            var token = TestContext.Current.CancellationToken;
            foreach (var options in new[] { new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.UInt16 }, new ByteSerializerOptions() { UseTypes = true, IndexType = ByteSerializerIndexType.UInt16 }, new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames } })
            {
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

    }

    [CollectionDefinition(nameof(SerializerTestingMode), DisableParallelization = true)]
    public sealed class SerializerTestingMode { }
}
