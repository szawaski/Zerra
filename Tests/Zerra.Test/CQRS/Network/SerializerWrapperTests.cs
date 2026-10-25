// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.CQRS.Network
{
    public class SerializerWrapperTests
    {
        public static TheoryData<string> Serializers => new() { "ZerraJson", "ZerraByte", "SystemTextJson" };

        private static INetworkSerializer Create(string name) => name switch
        {
            "ZerraJson" => new NetworkJsonSerializer(new JsonSerializerOptions()),
            "ZerraByte" => new NetworkByteSerializer(new ByteSerializerOptions()),
            _ => new NetworkSystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions()),
        };

        private static void AssertModel(object? value)
        {
            var result = Assert.IsType<SimpleModel>(value);
            Assert.Equal(42, result.Value1);
            Assert.Equal("Hello", result.Value2);
        }

        [Theory]
        [MemberData(nameof(Serializers))]
        public async Task EveryMethod_RoundTrips(string name)
        {
            var serializer = Create(name);
            var model = new SimpleModel() { Value1 = 42, Value2 = "Hello" };
            var cancellationToken = TestContext.Current.CancellationToken;

            Assert.Equal(name == "ZerraByte" ? ContentType.Bytes : ContentType.Json, serializer.ContentType);

            var bytes = serializer.SerializeBytes<SimpleModel>(model);
            AssertModel(serializer.Deserialize<SimpleModel>(bytes));
            AssertModel(serializer.Deserialize(bytes, typeof(SimpleModel)));
            Assert.Equal(bytes, serializer.SerializeBytes(model, typeof(SimpleModel)));
            if (name != "ZerraByte")
                Assert.Equal(bytes, serializer.SerializeBytes((object)model));

            using (var stream = new MemoryStream())
            {
                serializer.Serialize<SimpleModel>(stream, model);
                stream.Position = 0;
                AssertModel(serializer.Deserialize<SimpleModel>(stream));
            }
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, model, typeof(SimpleModel));
                stream.Position = 0;
                AssertModel(serializer.Deserialize(stream, typeof(SimpleModel)));
            }
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, (object)model);
                Assert.True(stream.Length > 0);
            }

            using (var stream = new MemoryStream())
            {
                await serializer.SerializeAsync<SimpleModel>(stream, model, cancellationToken);
                stream.Position = 0;
                AssertModel(await serializer.DeserializeAsync<SimpleModel>(stream, cancellationToken));
            }
            using (var stream = new MemoryStream())
            {
                await serializer.SerializeAsync(stream, model, typeof(SimpleModel), cancellationToken);
                stream.Position = 0;
                AssertModel(await serializer.DeserializeAsync(stream, typeof(SimpleModel), cancellationToken));
            }
            using (var stream = new MemoryStream())
            {
                await serializer.SerializeAsync(stream, (object)model, cancellationToken);
                Assert.True(stream.Length > 0);
            }
        }

        [Fact]
        public void ZerraByteSerializer_ExposesOptions()
        {
            var options = new ByteSerializerOptions();
            Assert.Same(options, new NetworkByteSerializer(options).Options);
            Assert.Null(new NetworkByteSerializer().Options);

            var jsonOptions = new JsonSerializerOptions();
            Assert.Same(jsonOptions, new NetworkJsonSerializer(jsonOptions).Options);
            Assert.Null(new NetworkJsonSerializer().Options);
        }
    }
}
