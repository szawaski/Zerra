// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
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
        [InlineData("ünïcödé café 日本語")]
        public void String_RoundTrips(string value)
        {
            Assert.Equal(value, JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<string>(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<string>(JsonSerializer.SerializeBytes(value)));
        }

        [Fact]
        public void DictionaryKeys_Escaped()
        {
            var value = new Dictionary<string, int>() { ["a\"b"] = 1, ["plain"] = 2, ["😀\\"] = 3, ["café"] = 4 };

            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(JsonSerializer.SerializeBytes(value)));
        }

        [Fact]
        public void DictionaryKeys_Dates_RoundTrip()
        {
            AssertKeyRoundTrips(new DateTime(2026, 10, 6, 13, 45, 30, DateTimeKind.Utc).AddTicks(1234567));
            AssertKeyRoundTrips(new DateTime(2026, 10, 6, 13, 45, 30, DateTimeKind.Unspecified));
            AssertKeyRoundTrips(new DateTimeOffset(2026, 10, 6, 13, 45, 30, TimeSpan.FromHours(-5)));
            AssertKeyRoundTrips(new DateOnly(2026, 10, 6));
            AssertKeyRoundTrips(new TimeOnly(13, 45, 30, 500));
            AssertKeyRoundTrips(new TimeOnly(13, 45, 30));
            AssertKeyRoundTrips(new TimeSpan(-1, 2, 3, 4, 500));
        }

        private static void AssertKeyRoundTrips<TKey>(TKey key) where TKey : notnull
        {
            var value = new Dictionary<TKey, int>() { [key] = 1 };

            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<TKey, int>>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<TKey, int>>(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<Dictionary<TKey, int>>(JsonSerializer.Serialize(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<TKey, int>>(System.Text.Json.JsonSerializer.Serialize(value)));
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

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(-7)]
        [InlineData(123456789)]
        [InlineData(-123456789)]
        [InlineData(1000000000)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void Int32_Parses(int value)
        {
            var json = value.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(value, JsonSerializer.Deserialize<int>(json));
            Assert.Equal(value, JsonSerializer.Deserialize<int>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(value, JsonSerializer.Deserialize<int?>(json));
            Assert.Equal([value, value], JsonSerializer.Deserialize<int[]>($"[{json},{json}]"));
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-7L)]
        [InlineData(999999999999999999L)]
        [InlineData(-999999999999999999L)]
        [InlineData(1000000000000000000L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void Int64_Parses(long value)
        {
            var json = value.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(value, JsonSerializer.Deserialize<long>(json));
            Assert.Equal(value, JsonSerializer.Deserialize<long>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(value, JsonSerializer.Deserialize<long?>(json));
        }

        [Fact]
        public void Integers_ParseAtLimits()
        {
            AssertIntegerParses<sbyte>(0, -7, 99, -99, sbyte.MaxValue, sbyte.MinValue);
            AssertIntegerParses<byte>(0, 7, 99, byte.MaxValue);
            AssertIntegerParses<short>(0, -7, 9999, -9999, short.MaxValue, short.MinValue);
            AssertIntegerParses<ushort>(0, 7, 9999, ushort.MaxValue);
            AssertIntegerParses<uint>(0, 7, 999999999, uint.MaxValue);
            AssertIntegerParses<ulong>(0, 7, 9999999999999999999, ulong.MaxValue);

            var options = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<sbyte>("128", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<byte>("256", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<byte>("-1", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<short>("32768", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ushort>("65536", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<uint>("4294967296", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ulong>("18446744073709551616", options));
        }

        private static void AssertIntegerParses<T>(params T[] values) where T : struct, IFormattable
        {
            foreach (var value in values)
            {
                var json = value.ToString(null, CultureInfo.InvariantCulture);
                Assert.Equal(value, JsonSerializer.Deserialize<T>(json));
                Assert.Equal(value, JsonSerializer.Deserialize<T>(Encoding.UTF8.GetBytes(json)));
                Assert.Equal(value, JsonSerializer.Deserialize<T?>(json));
                Assert.Equal(value, JsonSerializer.Deserialize<T?>(Encoding.UTF8.GetBytes(json)));
            }
        }

        [Fact]
        public void Int32_InvalidNumber_Throws()
        {
            var options = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<int>("12.5", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<int>("12345678901", options));
        }

        [Theory]
        [InlineData(SignedEnum.Negative)]
        [InlineData(SignedEnum.Zero)]
        [InlineData(SignedEnum.Positive)]
        public void Enum_ParsesNumber(SignedEnum value)
        {
            var json = ((int)value).ToString(CultureInfo.InvariantCulture);
            Assert.Equal(value, JsonSerializer.Deserialize<SignedEnum>(json));
            Assert.Equal(value, JsonSerializer.Deserialize<SignedEnum>(Encoding.UTF8.GetBytes(json)));
        }

        public enum SignedEnum { Negative = -1, Zero = 0, Positive = 5 }

        public enum UnsignedLongEnum : ulong { Zero = 0, Max = ulong.MaxValue }

        [Fact]
        public void Enum_ParsesName()
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(SignedEnum.Negative, JsonSerializer.Deserialize<SignedEnum>("\"Negative\""));
                Assert.Equal(SignedEnum.Negative, JsonSerializer.Deserialize<SignedEnum>(Encoding.UTF8.GetBytes("\"Negative\"")));
                Assert.Equal(SignedEnum.Positive, JsonSerializer.Deserialize<SignedEnum?>("\"Positive\""));
                Assert.Equal(SignedEnum.Positive, JsonSerializer.Deserialize<SignedEnum>("\"Posit\\u0069ve\""));
                Assert.Equal(SignedEnum.Positive, JsonSerializer.Deserialize<SignedEnum>(Encoding.UTF8.GetBytes("\"Posit\\u0069ve\"")));
            }

            var options = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<SignedEnum>("\"Missing\"", options));
            Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<SignedEnum>(Encoding.UTF8.GetBytes("\"Missing\""), options));
        }

        [Fact]
        public void Enum_ParsesNumberAboveInt64()
        {
            var json = ulong.MaxValue.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(UnsignedLongEnum.Max, JsonSerializer.Deserialize<UnsignedLongEnum>(json));
            Assert.Equal(UnsignedLongEnum.Max, JsonSerializer.Deserialize<UnsignedLongEnum>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(UnsignedLongEnum.Max, JsonSerializer.Deserialize<UnsignedLongEnum?>(json));
            Assert.Equal(UnsignedLongEnum.Max, JsonSerializer.Deserialize<UnsignedLongEnum>(JsonSerializer.Serialize(UnsignedLongEnum.Max, new JsonSerializerOptions() { EnumAsNumber = true })));
        }

        [Theory]
        [InlineData("\"00:00:00\"")]
        [InlineData("\"01:02:03.4560000\"")]
        [InlineData("\"-2.15:54:46.8420010\"")]
        [InlineData("\"10675199.02:48:05.4775807\"")]
        [InlineData("\"1:2:3\"")]
        public void TimeSpan_Parses(string json)
        {
            var expected = TimeSpan.Parse(json.Trim('"'), CultureInfo.InvariantCulture);
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeSpan>(json));
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeSpan>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeSpan?>(json));
        }

        [Theory]
        [InlineData("\"00:00:00\"")]
        [InlineData("\"13:45:30.1234567\"")]
        [InlineData("\"23:59:59.9999999\"")]
        public void TimeOnly_Parses(string json)
        {
            var expected = TimeOnly.Parse(json.Trim('"'), CultureInfo.InvariantCulture);
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeOnly>(json));
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeOnly>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeOnly?>(json));
        }

        [Fact]
        public void Numbers_IgnoreCurrentCulture()
        {
            var value = new CultureModel()
            {
                Double = -1234.5,
                Single = 1.25f,
                Decimal = -9876.54m,
                Int = -42,
                Long = -1234567890123,
                TimeSpan = new TimeSpan(1, 2, 3, 4, 500),
                Keys = new() { { 1.5, 1 }, { -2.25, 2 } },
            };

            var original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            culture.NumberFormat.NegativeSign = "−";
            CultureInfo.CurrentCulture = culture;
            try
            {
                var expected = System.Text.Json.JsonSerializer.Serialize(value);

                var json = JsonSerializer.Serialize(value);
                Assert.Equal(expected, json);
                Assert.Equal(expected, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)));

                var result = JsonSerializer.Deserialize<CultureModel>(json)!;
                var resultBytes = JsonSerializer.Deserialize<CultureModel>(Encoding.UTF8.GetBytes(json))!;
                foreach (var item in new[] { result, resultBytes })
                {
                    Assert.Equal(value.Double, item.Double);
                    Assert.Equal(value.Single, item.Single);
                    Assert.Equal(value.Decimal, item.Decimal);
                    Assert.Equal(value.Int, item.Int);
                    Assert.Equal(value.Long, item.Long);
                    Assert.Equal(value.TimeSpan, item.TimeSpan);
                    Assert.Equal(value.Keys, item.Keys);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        public class CultureModel
        {
            public double Double { get; set; }
            public float Single { get; set; }
            public decimal Decimal { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public Dictionary<double, int> Keys { get; set; } = null!;
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
