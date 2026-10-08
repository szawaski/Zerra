// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Xunit;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Serialization
{
    public class JsonSerializerOverloadTests
    {
        private const string json = "{\"Value1\":42,\"Value2\":\"Hello\"}";
        private static readonly SimpleModel model = new() { Value1 = 42, Value2 = "Hello" };

        public JsonSerializerOverloadTests()
        {
#if DEBUG
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
            Zerra.Serialization.Json.IO.JsonWriter.Testing = true;
#endif
        }

        private static void AssertModel(object? value)
        {
            var result = Assert.IsType<SimpleModel>(value);
            Assert.Equal(42, result.Value1);
            Assert.Equal("Hello", result.Value2);
        }

        private static void AssertGraph(Graph? graph)
        {
            Assert.NotNull(graph);
            Assert.True(graph.HasMember(nameof(SimpleModel.Value1)));
            Assert.True(graph.HasMember(nameof(SimpleModel.Value2)));
        }

        private static MemoryStream ToStream() => new(Encoding.UTF8.GetBytes(json));

        private static string WriteToString(Action<Stream> write)
        {
            using var stream = new MemoryStream();
            write(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        [Fact]
        public async Task Serialize()
        {
            Assert.Equal(json, JsonSerializer.Serialize(model));
            Assert.Equal(json, JsonSerializer.Serialize((object)model));
            Assert.Equal(json, JsonSerializer.Serialize(model, typeof(SimpleModel)));

            Assert.Equal(json, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(model)));
            Assert.Equal(json, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes((object)model)));
            Assert.Equal(json, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(model, typeof(SimpleModel))));

            Assert.Equal(json, WriteToString(x => JsonSerializer.Serialize(x, model)));
            Assert.Equal(json, WriteToString(x => JsonSerializer.Serialize(x, (object)model)));
            Assert.Equal(json, WriteToString(x => JsonSerializer.Serialize(x, model, typeof(SimpleModel))));

            using var stream1 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream1, model, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(json, Encoding.UTF8.GetString(stream1.ToArray()));
            using var stream2 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream2, (object)model, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(json, Encoding.UTF8.GetString(stream2.ToArray()));
            using var stream3 = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream3, model, typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(json, Encoding.UTF8.GetString(stream3.ToArray()));
        }

        [Fact]
        public async Task Deserialize()
        {
            var bytes = Encoding.UTF8.GetBytes(json);

            AssertModel(JsonSerializer.Deserialize<SimpleModel>(json));
            AssertModel(JsonSerializer.Deserialize(json, typeof(SimpleModel)));
            AssertModel(JsonSerializer.Deserialize<SimpleModel>(json.AsSpan()));
            AssertModel(JsonSerializer.Deserialize(json.AsSpan(), typeof(SimpleModel)));
            AssertModel(JsonSerializer.Deserialize<SimpleModel>(bytes));
            AssertModel(JsonSerializer.Deserialize(bytes, typeof(SimpleModel)));
            AssertModel(JsonSerializer.Deserialize<SimpleModel>(ToStream()));
            AssertModel(JsonSerializer.Deserialize(ToStream(), typeof(SimpleModel)));
            AssertModel(await JsonSerializer.DeserializeAsync<SimpleModel>(ToStream(), cancellationToken: TestContext.Current.CancellationToken));
            AssertModel(await JsonSerializer.DeserializeAsync(ToStream(), typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeserializePatch()
        {
            var bytes = Encoding.UTF8.GetBytes(json);

            var (result1, graph1) = JsonSerializer.DeserializePatch<SimpleModel>(json);
            AssertModel(result1);
            AssertGraph(graph1);
            var (result2, graph2) = JsonSerializer.DeserializePatch(json, typeof(SimpleModel));
            AssertModel(result2);
            AssertGraph(graph2);
            var (result3, graph3) = JsonSerializer.DeserializePatch<SimpleModel>(json.AsSpan());
            AssertModel(result3);
            AssertGraph(graph3);
            var (result4, graph4) = JsonSerializer.DeserializePatch(json.AsSpan(), typeof(SimpleModel));
            AssertModel(result4);
            AssertGraph(graph4);
            var (result5, graph5) = JsonSerializer.DeserializePatch<SimpleModel>(bytes);
            AssertModel(result5);
            AssertGraph(graph5);
            var (result6, graph6) = JsonSerializer.DeserializePatch(bytes, typeof(SimpleModel));
            AssertModel(result6);
            AssertGraph(graph6);
            var (result7, graph7) = JsonSerializer.DeserializePatch<SimpleModel>(ToStream());
            AssertModel(result7);
            AssertGraph(graph7);
            var (result8, graph8) = JsonSerializer.DeserializePatch(ToStream(), typeof(SimpleModel));
            AssertModel(result8);
            AssertGraph(graph8);
            var (result9, graph9) = await JsonSerializer.DeserializePatchAsync<SimpleModel>(ToStream(), cancellationToken: TestContext.Current.CancellationToken);
            AssertModel(result9);
            AssertGraph(graph9);
            var (result10, graph10) = await JsonSerializer.DeserializePatchAsync(ToStream(), typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken);
            AssertModel(result10);
            AssertGraph(graph10);
        }

        [Fact]
        public async Task DeserializeJsonObject()
        {
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(json)!.ToString());
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(json.AsSpan())!.ToString());
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(Encoding.UTF8.GetBytes(json))!.ToString());
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(ToStream())!.ToString());
            Assert.Equal(json, (await JsonSerializer.DeserializeJsonObjectAsync(ToStream(), cancellationToken: TestContext.Current.CancellationToken))!.ToString());
        }

        [Fact]
        public async Task DeserializePatch_LargerThanBuffer()
        {
            var model = TypesAllModel.Create();
            var bytes = JsonSerializer.SerializeBytes(model);
            Assert.True(bytes.Length > 16 * 1024);

            var (result1, graph1) = JsonSerializer.DeserializePatch<TypesAllModel>(new MemoryStream(bytes));
            AssertHelper.AreEqual(model, result1);
            Assert.True(graph1!.HasMember(nameof(TypesAllModel.Int32Thing)));

            var (result2, graph2) = JsonSerializer.DeserializePatch(new MemoryStream(bytes), typeof(TypesAllModel));
            AssertHelper.AreEqual(model, result2);
            Assert.True(graph2!.HasMember(nameof(TypesAllModel.Int32Thing)));

            var (result3, graph3) = await JsonSerializer.DeserializePatchAsync<TypesAllModel>(new MemoryStream(bytes), cancellationToken: TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result3);
            Assert.True(graph3!.HasMember(nameof(TypesAllModel.Int32Thing)));

            var (result4, graph4) = await JsonSerializer.DeserializePatchAsync(new MemoryStream(bytes), typeof(TypesAllModel), cancellationToken: TestContext.Current.CancellationToken);
            AssertHelper.AreEqual(model, result4);
            Assert.True(graph4!.HasMember(nameof(TypesAllModel.Int32Thing)));
        }

        [Fact]
        public async Task DeserializeJsonObject_LargerThanBuffer()
        {
            var models = Enumerable.Range(0, 2000).Select(x => new SimpleModel { Value1 = x, Value2 = $"Value {x}" }).ToArray();
            var large = JsonSerializer.Serialize(models);
            Assert.True(large.Length > 16 * 1024);

            Assert.Equal(large, JsonSerializer.DeserializeJsonObject(new MemoryStream(Encoding.UTF8.GetBytes(large)))!.ToString());
            Assert.Equal(large, (await JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(Encoding.UTF8.GetBytes(large)), cancellationToken: TestContext.Current.CancellationToken))!.ToString());
        }

        [Fact]
        public async Task Empty()
        {
            Assert.Null(JsonSerializer.Deserialize<SimpleModel>(""));
            Assert.Equal("", JsonSerializer.Deserialize<string>(""));
            Assert.Null(JsonSerializer.Deserialize("", typeof(SimpleModel)));
            Assert.Equal("", JsonSerializer.Deserialize("", typeof(string)));
            Assert.Null(JsonSerializer.Deserialize<SimpleModel>(Array.Empty<byte>()));
            Assert.Equal("", JsonSerializer.Deserialize<string>(Array.Empty<byte>()));
            Assert.Null(JsonSerializer.Deserialize(Array.Empty<byte>(), typeof(SimpleModel)));
            Assert.Equal("", JsonSerializer.Deserialize(Array.Empty<byte>(), typeof(string)));
            Assert.Null(JsonSerializer.Deserialize<SimpleModel>(new MemoryStream()));
            Assert.Equal("", JsonSerializer.Deserialize<string>(new MemoryStream()));
            Assert.Null(JsonSerializer.Deserialize(new MemoryStream(), typeof(SimpleModel)));
            Assert.Equal("", JsonSerializer.Deserialize(new MemoryStream(), typeof(string)));
            Assert.Null(await JsonSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal("", await JsonSerializer.DeserializeAsync<string>(new MemoryStream(), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await JsonSerializer.DeserializeAsync(new MemoryStream(), typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal("", await JsonSerializer.DeserializeAsync(new MemoryStream(), typeof(string), cancellationToken: TestContext.Current.CancellationToken));

            Assert.Null(JsonSerializer.DeserializePatch<SimpleModel>("").Item1);
            Assert.Null(JsonSerializer.DeserializePatch("", typeof(SimpleModel)).Item1);
            Assert.Null(JsonSerializer.DeserializePatch<SimpleModel>(Array.Empty<byte>()).Item1);
            Assert.Null(JsonSerializer.DeserializePatch(Array.Empty<byte>(), typeof(SimpleModel)).Item1);
            Assert.Null(JsonSerializer.DeserializePatch<SimpleModel>(new MemoryStream()).Item1);
            Assert.Null(JsonSerializer.DeserializePatch(new MemoryStream(), typeof(SimpleModel)).Item1);
            Assert.Null((await JsonSerializer.DeserializePatchAsync<SimpleModel>(new MemoryStream(), cancellationToken: TestContext.Current.CancellationToken)).Item1);
            Assert.Null((await JsonSerializer.DeserializePatchAsync(new MemoryStream(), typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken)).Item1);

            Assert.True(JsonSerializer.DeserializeJsonObject("")!.IsNull);
            Assert.True(JsonSerializer.DeserializeJsonObject(Array.Empty<byte>())!.IsNull);
            Assert.True(JsonSerializer.DeserializeJsonObject(new MemoryStream())!.IsNull);
            Assert.True((await JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(), cancellationToken: TestContext.Current.CancellationToken))!.IsNull);
        }

        [Fact]
        public async Task Truncated_Throws()
        {
            var truncated = json[..10];
            var bytes = Encoding.UTF8.GetBytes(truncated);
            var token = TestContext.Current.CancellationToken;

            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize<SimpleModel>(truncated));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize(truncated, typeof(SimpleModel)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize<SimpleModel>(bytes));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize(bytes, typeof(SimpleModel)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize<SimpleModel>(new MemoryStream(bytes)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.Deserialize(new MemoryStream(bytes), typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => JsonSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));

            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch<SimpleModel>(truncated));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch(truncated, typeof(SimpleModel)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch<SimpleModel>(bytes));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch(bytes, typeof(SimpleModel)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch<SimpleModel>(new MemoryStream(bytes)));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializePatch(new MemoryStream(bytes), typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => JsonSerializer.DeserializePatchAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => JsonSerializer.DeserializePatchAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));

            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializeJsonObject(truncated));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializeJsonObject(bytes));
            _ = Assert.Throws<EndOfStreamException>(() => JsonSerializer.DeserializeJsonObject(new MemoryStream(bytes)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(bytes), cancellationToken: token));
        }

        [Fact]
        public async Task NullArguments_Throw()
        {
            var token = TestContext.Current.CancellationToken;

            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(json, null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(Encoding.UTF8.GetBytes(json), null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize(ToStream(), null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<SimpleModel>((Stream)null!));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.DeserializeAsync(ToStream(), null!, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.DeserializeAsync<SimpleModel>(null!, cancellationToken: token));

            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializePatch(json, null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializePatch(Encoding.UTF8.GetBytes(json), null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializePatch(ToStream(), null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializePatch<SimpleModel>((Stream)null!));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.DeserializePatchAsync(ToStream(), null!, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.DeserializePatchAsync<SimpleModel>(null!, cancellationToken: token));

            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.DeserializeJsonObject((Stream)null!));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.DeserializeJsonObjectAsync(null!, cancellationToken: token));

            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(model, (Type)null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.SerializeBytes(model, (Type)null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new MemoryStream(), model, (Type)null!));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize<SimpleModel>(null!, model));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), model, (Type)null!, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.SerializeAsync<SimpleModel>(null!, model, cancellationToken: token));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null!, (object)model));
            _ = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null!, (object)model, typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.SerializeAsync(null!, (object)model, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => JsonSerializer.SerializeAsync(null!, (object)model, typeof(SimpleModel), cancellationToken: token));
        }

        [Fact]
        public async Task NullObject()
        {
            var token = TestContext.Current.CancellationToken;

            //the text and bytes are JSON null
            Assert.Equal("null", JsonSerializer.Serialize((SimpleModel?)null));
            Assert.Equal("null", JsonSerializer.Serialize((object?)null));
            Assert.Equal("null", JsonSerializer.Serialize((object?)null, typeof(SimpleModel)));
            Assert.Equal("null"u8.ToArray(), JsonSerializer.SerializeBytes((SimpleModel?)null));
            Assert.Equal("null"u8.ToArray(), JsonSerializer.SerializeBytes((object?)null));
            Assert.Equal("null"u8.ToArray(), JsonSerializer.SerializeBytes((object?)null, typeof(SimpleModel)));

            //and so is what a stream gets
            var writes = new Func<Stream, Task>[]
            {
                stream => { JsonSerializer.Serialize<SimpleModel>(stream, null); return Task.CompletedTask; },
                stream => { JsonSerializer.Serialize(stream, (object?)null); return Task.CompletedTask; },
                stream => { JsonSerializer.Serialize(stream, (object?)null, typeof(SimpleModel)); return Task.CompletedTask; },
                stream => JsonSerializer.SerializeAsync<SimpleModel>(stream, null, cancellationToken: token),
                stream => JsonSerializer.SerializeAsync(stream, (object?)null, cancellationToken: token),
                stream => JsonSerializer.SerializeAsync(stream, (object?)null, typeof(SimpleModel), cancellationToken: token),
            };
            foreach (var write in writes)
            {
                using var stream = new MemoryStream();
                await write(stream);
                Assert.Equal("null", Encoding.UTF8.GetString(stream.ToArray()));
                Assert.Null(JsonSerializer.Deserialize<SimpleModel>(new MemoryStream(stream.ToArray())));
            }
        }

        [Fact]
        public async Task SerializeObject_LargerThanBuffer()
        {
            var token = TestContext.Current.CancellationToken;
            var large = Enumerable.Range(0, 3000).Select(x => new SimpleModel() { Value1 = x, Value2 = new string('x', 20) }).ToArray();
            var expected = JsonSerializer.Serialize(large);

            using (var stream = new MemoryStream())
            {
                JsonSerializer.Serialize(stream, (object)large);
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }
            using (var stream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(stream, (object)large, cancellationToken: token);
                Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public async Task StreamTrailingWhitespacePastBuffer()
        {
            var bytes = Encoding.UTF8.GetBytes(json + new string(' ', 20_000));
            var token = TestContext.Current.CancellationToken;

            AssertModel(JsonSerializer.Deserialize<SimpleModel>(new MemoryStream(bytes)));
            AssertModel(JsonSerializer.Deserialize(new MemoryStream(bytes), typeof(SimpleModel)));
            AssertModel(await JsonSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            AssertModel(await JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));
            AssertModel(JsonSerializer.DeserializePatch<SimpleModel>(new MemoryStream(bytes)).Item1);
            AssertModel(JsonSerializer.DeserializePatch(new MemoryStream(bytes), typeof(SimpleModel)).Item1);
            AssertModel((await JsonSerializer.DeserializePatchAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token)).Item1);
            AssertModel((await JsonSerializer.DeserializePatchAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token)).Item1);
            Assert.Equal(json, JsonSerializer.DeserializeJsonObject(new MemoryStream(bytes))!.ToString());
            Assert.Equal(json, (await JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(bytes), cancellationToken: token))!.ToString());
        }

        [Theory]
        [InlineData("x")]
        [InlineData(" {}")]
        [InlineData("{\"Value1\":42,\"Value2\":\"Hello\"}")]
        [InlineData(" \r\n\t ,")]
        [InlineData("LONG_WHITESPACE_THEN_x")]
        public async Task TrailingContent_Throws(string trailing)
        {
            var text = json + (trailing == "LONG_WHITESPACE_THEN_x" ? new string(' ', 20_000) + "x" : trailing);
            var bytes = Encoding.UTF8.GetBytes(text);
            var token = TestContext.Current.CancellationToken;

            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<SimpleModel>(text));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize(text, typeof(SimpleModel)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<SimpleModel>(bytes));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize(bytes, typeof(SimpleModel)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<SimpleModel>(new MemoryStream(bytes)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize(new MemoryStream(bytes), typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            _ = await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));

            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch<SimpleModel>(text));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch(text, typeof(SimpleModel)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch<SimpleModel>(bytes));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch(bytes, typeof(SimpleModel)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch<SimpleModel>(new MemoryStream(bytes)));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializePatch(new MemoryStream(bytes), typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.DeserializePatchAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            _ = await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.DeserializePatchAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));

            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializeJsonObject(text));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializeJsonObject(bytes));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.DeserializeJsonObject(new MemoryStream(bytes)));
            _ = await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(bytes), cancellationToken: token));
        }

        [Theory]
        [InlineData("5", 5)]
        [InlineData(" 5 \r\n", 5)]
        [InlineData("\"5\"\t", 5)]
        public void TrailingWhitespace_RootValues(string text, int expected)
        {
            Assert.Equal(expected, JsonSerializer.Deserialize<int>(text));
            Assert.Equal(expected, JsonSerializer.Deserialize<int>(Encoding.UTF8.GetBytes(text)));
            Assert.Equal(expected, JsonSerializer.Deserialize<int>(new MemoryStream(Encoding.UTF8.GetBytes(text))));
            _ = Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<int>(text + "5x"));
        }

        [Fact]
        public void Depth_PastTheLimit_Throws()
        {
            var shallow = DepthModel.Create(20);
            Assert.Equal(19, JsonSerializer.Deserialize<DepthModel>(JsonSerializer.Serialize(shallow))!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Child!.Level);

            var deep = DepthModel.Create(40);
            _ = Assert.Throws<StackOverflowException>(() => JsonSerializer.Serialize(deep));
            _ = Assert.Throws<StackOverflowException>(() => JsonSerializer.Serialize((object)deep, typeof(DepthModel)));
            var deepJson = string.Concat(Enumerable.Repeat("{\"Child\":", 40)) + "null" + new string('}', 40);
            _ = Assert.Throws<StackOverflowException>(() => JsonSerializer.Deserialize<DepthModel>(deepJson));
            _ = Assert.Throws<StackOverflowException>(() => JsonSerializer.Deserialize(deepJson, typeof(DepthModel)));
        }

        [Fact]
        public void RootDeclaredAsInterface_WritesTheRuntimeType()
        {
            Assert.Equal(JsonSerializer.Serialize(model), JsonSerializer.Serialize((object)model, typeof(IBasicModel)));
            Assert.Equal(JsonSerializer.SerializeBytes(model), JsonSerializer.SerializeBytes((object)model, typeof(IBasicModel)));
            Assert.Equal("null", JsonSerializer.Serialize((object?)null, typeof(IBasicModel)));
        }

        public sealed class DepthModel
        {
            public int Level { get; set; }
            public DepthModel? Child { get; set; }

            public static DepthModel Create(int levels)
            {
                var model = new DepthModel() { Level = 0 };
                var current = model;
                for (var i = 1; i < levels; i++)
                {
                    current.Child = new DepthModel() { Level = i };
                    current = current.Child;
                }
                return model;
            }
        }

        [Zerra.Reflection.GenerateTypeDetail]
        public interface ISimpleValues
        {
            int Value1 { get; set; }
            string? Value2 { get; set; }
        }
    }
}
