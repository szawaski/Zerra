// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Xunit;
using Zerra.Serialization.Json;

namespace Zerra.Test.Serialization
{
    public class JsonWriterFormatTests
    {
        public JsonWriterFormatTests()
        {
#if DEBUG
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
            Zerra.Serialization.Json.IO.JsonWriter.Testing = true;
#endif
        }

        public static TheoryData<DateTime> DateTimes => new()
        {
            new DateTime(2024, 3, 9, 4, 5, 6, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999),
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1),
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1000),
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234560),
            new DateTime(5, 6, 7, 8, 9, 10, DateTimeKind.Utc),
            new DateTime(987, 1, 2, 3, 4, 5, DateTimeKind.Unspecified),
            new DateTime(2024, 7, 4, 12, 30, 0, DateTimeKind.Unspecified).AddTicks(5),
            new DateTime(2024, 7, 4, 12, 30, 0, DateTimeKind.Local).AddTicks(1230000),
            new DateTime(2024, 1, 15, 1, 2, 3, DateTimeKind.Local),
            DateTime.MinValue,
            DateTime.MaxValue,
        };

        public static TheoryData<DateTimeOffset> DateTimeOffsets => new()
        {
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, TimeSpan.FromHours(-11)),
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, TimeSpan.FromHours(14)),
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, new TimeSpan(-3, -30, 0)),
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, new TimeSpan(5, 45, 0)),
            new DateTimeOffset(2024, 3, 9, 4, 5, 6, TimeSpan.FromHours(-1)).AddTicks(1),
            new DateTimeOffset(5, 6, 7, 8, 9, 10, TimeSpan.FromHours(2)).AddTicks(1234567),
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
        };

        [Theory]
        [MemberData(nameof(DateTimes))]
        public void DateTime_MatchesIso8601(DateTime value)
        {
            var expected = System.Text.Json.JsonSerializer.Serialize(value);

            Assert.Equal(expected, JsonSerializer.Serialize(value));
            Assert.Equal(expected, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)));

            var result = JsonSerializer.Deserialize<DateTime>(JsonSerializer.SerializeBytes(value));
            if (value.Kind == DateTimeKind.Local)
                Assert.Equal(value, result.ToLocalTime());
            else
                Assert.Equal(value, result);
        }

        [Theory]
        [MemberData(nameof(DateTimeOffsets))]
        public void DateTimeOffset_MatchesIso8601(DateTimeOffset value)
        {
            var expected = System.Text.Json.JsonSerializer.Serialize(value);

            Assert.Equal(expected, JsonSerializer.Serialize(value));
            Assert.Equal(expected, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)));

            var result = JsonSerializer.Deserialize<DateTimeOffset>(JsonSerializer.SerializeBytes(value));
            Assert.Equal(value, result);
            Assert.Equal(value.Offset, result.Offset);
        }

        [Theory]
        [InlineData("", "\"\"")]
        [InlineData("plain", "\"plain\"")]
        [InlineData("a\"b", "\"a\\\"b\"")]
        [InlineData("a\\b", "\"a\\\\b\"")]
        [InlineData("line\nbreak\ttab\rreturn", "\"line\\nbreak\\ttab\\rreturn\"")]
        [InlineData("\b\f", "\"\\b\\f\"")]
        [InlineData("\u0001\u001F", "\"\\u0001\\u001F\"")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"", "\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\\\"\"")]
        [InlineData("\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "\"\\\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"")]
        [InlineData("ünïcödé ✓", "\"ünïcödé ✓\"")]
        public void String_Escapes(string value, string expected)
        {
            Assert.Equal(expected, JsonSerializer.Serialize(value));
            Assert.Equal(expected, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)));
        }

        [Theory]
        [InlineData("😀")]
        [InlineData("a😀\"b")]
        [InlineData("a\"b😀")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa😀aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\\")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\\aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa😀")]
        [InlineData("\u0000\u001F\"\\\b\f\n\r\t plain ünïcödé")]
        public void String_RoundTrips(string value)
        {
            Assert.Equal(value, JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<string>(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<string>(JsonSerializer.SerializeBytes(value)));
        }

        [Fact]
        public void DictionaryKeys_Escaped()
        {
            var value = new Dictionary<string, int>() { ["a\"b"] = 1, ["plain"] = 2, ["😀\\"] = 3 };

            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.SerializeBytes(value)));
        }

        public sealed class Collections
        {
            public int[] Array { get; set; }
            public List<string> List { get; set; }
            public IReadOnlyList<int[]> Nested { get; set; }
            public HashSet<int> Set { get; set; }
            public ISet<string> ISet { get; set; }
            public IEnumerable<int> Enumerable { get; set; }
            public ICollection<int> Collection { get; set; }
            public IReadOnlyCollection<int> ReadOnlyCollection { get; set; }
            public IList<int> IList { get; set; }
            public IReadOnlySet<int> ReadOnlySet { get; set; }
            public int[] Empty { get; set; }
            public List<Collections> Children { get; set; }
        }

        [Fact]
        public void Collections_RoundTrip()
        {
            var value = new Collections()
            {
                Array = Enumerable.Range(0, 100).ToArray(),
                List = ["a", "b,c", "[d]", "{e}", "\"f\""],
                Nested = [[1, 2], [], [3]],
                Set = [1, 2, 3],
                ISet = new HashSet<string>() { "x", "y" },
                Enumerable = [4, 5],
                Collection = [6],
                ReadOnlyCollection = [7, 8, 9],
                IList = [10, 11],
                ReadOnlySet = new HashSet<int>() { 12 },
                Empty = [],
                Children = [new Collections() { Array = [1] }, new Collections() { List = ["z"] }],
            };

            foreach (var json in new[] { JsonSerializer.Serialize(value), Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)) })
            {
                var result = JsonSerializer.Deserialize<Collections>(json)!;
                Assert.Equal(value.Array, result.Array);
                Assert.Equal(value.List, result.List);
                Assert.Equal(value.Nested.Count, result.Nested.Count);
                for (var i = 0; i < value.Nested.Count; i++)
                    Assert.Equal(value.Nested[i], result.Nested[i]);
                Assert.Equal(value.Set, result.Set);
                Assert.Equal(value.ISet, result.ISet);
                Assert.Equal(value.Enumerable, result.Enumerable);
                Assert.Equal(value.Collection, result.Collection);
                Assert.Equal(value.ReadOnlyCollection, result.ReadOnlyCollection);
                Assert.Equal(value.IList, result.IList);
                Assert.Equal(value.ReadOnlySet, result.ReadOnlySet);
                Assert.Empty(result.Empty);
                Assert.Equal(2, result.Children.Count);
                Assert.Equal([1], result.Children[0].Array);
                Assert.Equal(["z"], result.Children[1].List);
            }
        }

        [Fact]
        public void Collections_RoundTripFromStream()
        {
            var value = new Collections() { Array = Enumerable.Range(0, 5000).ToArray(), List = Enumerable.Range(0, 2000).Select(x => $"item\"{x}").ToList() };

            using var stream = new MemoryStream(JsonSerializer.SerializeBytes(value));
            var result = JsonSerializer.Deserialize<Collections>(stream)!;
            Assert.Equal(value.Array, result.Array);
            Assert.Equal(value.List, result.List);
        }

        [Fact]
        public void JsonObject_Array()
        {
            var json = "[1,[2,3],{\"a\":[4]},\"x\",[]]";
            var result = JsonSerializer.DeserializeJsonObject(json)!;
            Assert.Equal(json, result.ToString());
        }
    }
}
