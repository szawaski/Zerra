// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;

namespace Zerra.Test.Serialization
{
    //a struct read member by member must keep every member, including when the read pauses for more data partway through it
    public class StructModelTests
    {
        public StructModelTests()
        {
#if DEBUG
            Zerra.Serialization.Bytes.IO.ByteReader.Testing = true;
            Zerra.Serialization.Bytes.IO.ByteWriter.Testing = true;
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
            Zerra.Serialization.Json.IO.JsonWriter.Testing = true;
#endif
        }

        public struct StructModel
        {
            public int A { get; set; }
            public string? Text { get; set; }
            public int B { get; set; }
            public int Init { get; init; }
            public int Field;
        }

        //read through its constructor, the members it doesn't take are set after
        public struct StructConstructorModel
        {
            public StructConstructorModel(int a)
            {
                A = a;
            }
            public int A { get; }
            public int B { get; set; }
            public string? Text { get; set; }
        }

        //a struct typed member is not serialized, structs are read as the value itself or as collection items
        public class StructHolder
        {
            public List<StructModel>? List { get; set; }
            public StructModel[]? Array { get; set; }
            public int After { get; set; }
        }

        //longer than either serializer's stream buffer, so a member is split across reads in release builds too
        private static readonly string longText = new('x', 40 * 1024);

        private static StructModel CreateModel(int seed, string text) => new()
        {
            A = seed + 1,
            Text = text,
            B = seed + 2,
            Init = seed + 3,
            Field = seed + 4
        };

        private static StructHolder CreateHolder(string text) => new()
        {
            List = [CreateModel(10, text), CreateModel(20, text)],
            Array = [CreateModel(30, text), CreateModel(40, text)],
            After = 50
        };

        private static void AssertModel(int seed, string text, StructModel model)
        {
            Assert.Equal(seed + 1, model.A);
            Assert.Equal(text, model.Text);
            Assert.Equal(seed + 2, model.B);
            Assert.Equal(seed + 3, model.Init);
            Assert.Equal(seed + 4, model.Field);
        }

        private static void AssertHolder(string text, StructHolder? holder)
        {
            Assert.NotNull(holder);
            Assert.NotNull(holder.List);
            Assert.Equal(2, holder.List.Count);
            AssertModel(10, text, holder.List[0]);
            AssertModel(20, text, holder.List[1]);
            Assert.NotNull(holder.Array);
            Assert.Equal(2, holder.Array.Length);
            AssertModel(30, text, holder.Array[0]);
            AssertModel(40, text, holder.Array[1]);
            Assert.Equal(50, holder.After);
        }

        [Fact]
        public void JsonString()
        {
            AssertModel(0, "moo", JsonSerializer.Deserialize<StructModel>(JsonSerializer.Serialize(CreateModel(0, "moo"))));
            AssertHolder("moo", JsonSerializer.Deserialize<StructHolder>(JsonSerializer.Serialize(CreateHolder("moo"))));
        }

        [Fact]
        public void JsonNameless()
        {
            var options = new JsonSerializerOptions() { Nameless = true };
            AssertModel(0, "moo", JsonSerializer.Deserialize<StructModel>(JsonSerializer.Serialize(CreateModel(0, "moo"), options), options));
            AssertHolder("moo", JsonSerializer.Deserialize<StructHolder>(JsonSerializer.Serialize(CreateHolder("moo"), options), options));
        }

        [Fact]
        public async Task JsonStream()
        {
            foreach (var options in new[] { null, new JsonSerializerOptions() { Nameless = true } })
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, CreateHolder(longText), options, null, TestContext.Current.CancellationToken);
                stream.Position = 0;
                AssertHolder(longText, await JsonSerializer.DeserializeAsync<StructHolder>(stream, options, null, TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public void Bytes()
        {
            foreach (var options in new[] { null, new ByteSerializerOptions() { IgnoreIndexAttribute = true } })
            {
                AssertModel(0, "moo", ByteSerializer.Deserialize<StructModel>(ByteSerializer.Serialize(CreateModel(0, "moo"), options), options));
                AssertHolder("moo", ByteSerializer.Deserialize<StructHolder>(ByteSerializer.Serialize(CreateHolder("moo"), options), options));
            }
        }

        [Fact]
        public void Constructor()
        {
            var model = new StructConstructorModel(1) { B = 2, Text = "moo" };

            var json = JsonSerializer.Deserialize<StructConstructorModel>(JsonSerializer.Serialize(model));
            Assert.Equal(1, json.A);
            Assert.Equal(2, json.B);
            Assert.Equal("moo", json.Text);

            var bytes = ByteSerializer.Deserialize<StructConstructorModel>(ByteSerializer.Serialize(model));
            Assert.Equal(1, bytes.A);
            Assert.Equal(2, bytes.B);
            Assert.Equal("moo", bytes.Text);
        }

        [Fact]
        public async Task BytesStream()
        {
            using var stream = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream, CreateHolder(longText), null, TestContext.Current.CancellationToken);
            stream.Position = 0;
            AssertHolder(longText, await ByteSerializer.DeserializeAsync<StructHolder>(stream, null, TestContext.Current.CancellationToken));
        }
    }
}
