// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Xunit;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;

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

        [Fact]
        public void DictionaryKeys_Enums()
        {
            var value = new Dictionary<SignedEnum, int>() { [SignedEnum.Negative] = 1, [SignedEnum.Positive] = 2 };
            var expected = System.Text.Json.JsonSerializer.Serialize(value);

            Assert.Equal(expected, JsonSerializer.Serialize(value));
            Assert.Equal(expected, Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(value)));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<SignedEnum, int>>(expected));
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<SignedEnum, int>>(Encoding.UTF8.GetBytes(expected)));
            Assert.Equal(value, JsonSerializer.Deserialize<IDictionary<SignedEnum, int>>(expected));
            Assert.Equal(value, JsonSerializer.Deserialize<IReadOnlyDictionary<SignedEnum, int>>(expected));

            var pairs = "[{\"Key\":\"Negative\",\"Value\":1},{\"Key\":\"Positive\",\"Value\":2}]";
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<SignedEnum, int>>(pairs));
            Assert.Equal(value, JsonSerializer.Deserialize<IDictionary<SignedEnum, int>>(pairs));

            (var patched, var graph) = JsonSerializer.DeserializePatch<Dictionary<SignedEnum, int>>(expected);
            Assert.Equal(value, patched);
            Assert.True(graph.HasMember("Negative"));
            Assert.True(graph.HasMember("Positive"));

            var options = new JsonSerializerOptions() { EnumAsNumber = true };
            var numbers = JsonSerializer.Serialize(value, options);
            Assert.Equal("{\"-1\":1,\"5\":2}", numbers);
            Assert.Equal(value, JsonSerializer.Deserialize<Dictionary<SignedEnum, int>>(numbers, options));
            (var patchedNumbers, var numbersGraph) = JsonSerializer.DeserializePatch<Dictionary<SignedEnum, int>>(numbers, options);
            Assert.Equal(value, patchedNumbers);
            Assert.True(numbersGraph.HasMember("-1"));
            Assert.True(numbersGraph.HasMember("5"));
        }

        [Fact]
        public void DictionaryComplexKeys_RoundTrip()
        {
            var value = new ComplexKeyDictionaries()
            {
                Dictionary = new Dictionary<SimpleModel, int?>() { [new() { Value1 = 1, Value2 = "A" }] = 1, [new() { Value1 = 2, Value2 = "B" }] = null },
                IDictionary = new Dictionary<SimpleModel, int?>() { [new() { Value1 = 3, Value2 = "C" }] = 3 },
                IReadOnlyDictionary = new Dictionary<SimpleModel, int?>() { [new() { Value1 = 4, Value2 = "D" }] = 4 },
                ConcurrentDictionary = new ConcurrentDictionary<SimpleModel, int?>(new Dictionary<SimpleModel, int?>() { [new() { Value1 = 5, Value2 = "E" }] = 5, [new() { Value1 = 6, Value2 = "F" }] = 6 }),
            };

            foreach (var result in new[] { JsonSerializer.Deserialize<ComplexKeyDictionaries>(JsonSerializer.Serialize(value))!, JsonSerializer.Deserialize<ComplexKeyDictionaries>(JsonSerializer.SerializeBytes(value))! })
            {
                Assert.Equal(Flatten(value.Dictionary), Flatten(result.Dictionary));
                Assert.Equal(Flatten(value.IDictionary), Flatten(result.IDictionary));
                Assert.Equal(Flatten(value.IReadOnlyDictionary), Flatten(result.IReadOnlyDictionary));
                Assert.Equal(Flatten(value.ConcurrentDictionary), Flatten(result.ConcurrentDictionary));
            }
        }

        private static string[] Flatten(IEnumerable<KeyValuePair<SimpleModel, int?>> dictionary)
            => dictionary.Select(x => $"{x.Key.Value1}|{x.Key.Value2}|{x.Value}").OrderBy(x => x).ToArray();

        public class ComplexKeyDictionaries
        {
            public Dictionary<SimpleModel, int?> Dictionary { get; set; } = null!;
            public IDictionary<SimpleModel, int?> IDictionary { get; set; } = null!;
            public IReadOnlyDictionary<SimpleModel, int?> IReadOnlyDictionary { get; set; } = null!;
            public ConcurrentDictionary<SimpleModel, int?> ConcurrentDictionary { get; set; } = null!;
        }

        [Fact]
        public void DictionaryKeys_PatchGraphNamesMatchWrittenNames()
        {
            var dates = new Dictionary<DateTime, int>() { [new DateTime(2026, 10, 6, 13, 45, 30, DateTimeKind.Utc)] = 1 };
            (var datesResult, var datesGraph) = JsonSerializer.DeserializePatch<Dictionary<DateTime, int>>(JsonSerializer.Serialize(dates));
            Assert.Equal(dates, datesResult);
            Assert.True(datesGraph.HasMember("2026-10-06T13:45:30Z"));

            var original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = culture;
            try
            {
                var numbers = new Dictionary<double, int>() { [1.5] = 1 };
                (var numbersResult, var numbersGraph) = JsonSerializer.DeserializePatch<Dictionary<double, int>>(JsonSerializer.Serialize(numbers));
                Assert.Equal(numbers, numbersResult);
                Assert.True(numbersGraph.HasMember("1.5"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Theory]
        [InlineData("1.5e3", 1500.0)]
        [InlineData("-2E-2", -0.02)]
        [InlineData("1E+2", 100.0)]
        [InlineData("0.5", 0.5)]
        public void Numbers_ParseExponents(string json, double expected)
        {
            Assert.Equal(expected, JsonSerializer.Deserialize<double>(json));
            Assert.Equal(expected, JsonSerializer.Deserialize<double>(Encoding.UTF8.GetBytes(json)));
            Assert.Equal((float)expected, JsonSerializer.Deserialize<float>(json));
            Assert.Equal([expected, 3.0], JsonSerializer.Deserialize<List<double>>($"[{json},3]"));
            Assert.Equal([expected, 3.0], JsonSerializer.Deserialize<List<double>>(Encoding.UTF8.GetBytes($"[{json},3]")));
        }

        [Fact]
        public void DateOnlyTimeOnly_MatchSystemTextJson()
        {
            DateOnly[] dates = [DateOnly.MinValue, new DateOnly(5, 6, 7), new DateOnly(2026, 10, 6), DateOnly.MaxValue];
            TimeOnly[] times = [TimeOnly.MinValue, new TimeOnly(13, 45, 30), new TimeOnly(13, 45, 30, 123), new TimeOnly(1, 2, 3).Add(TimeSpan.FromTicks(1)), TimeOnly.MaxValue];

            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(dates), JsonSerializer.Serialize(dates));
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(dates), Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(dates)));
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(times), JsonSerializer.Serialize(times));
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(times), Encoding.UTF8.GetString(JsonSerializer.SerializeBytes(times)));
            Assert.Equal(dates, JsonSerializer.Deserialize<DateOnly[]>(JsonSerializer.Serialize(dates)));
            Assert.Equal(times, JsonSerializer.Deserialize<TimeOnly[]>(JsonSerializer.SerializeBytes(times)));
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
            var numbers = new JsonSerializerOptions() { EnumAsNumber = true };
            foreach (var value in values)
            {
                Assert.Equal(value, JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value)));
                Assert.Equal(value, JsonSerializer.Deserialize<T>(JsonSerializer.SerializeBytes(value)));
                Assert.Equal(value, JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, numbers), numbers));
                Assert.Equal(value, JsonSerializer.Deserialize<T>(JsonSerializer.SerializeBytes(value, numbers), numbers));
                Assert.Equal(value, JsonSerializer.Deserialize<T?>(JsonSerializer.Serialize<T?>(value, numbers), numbers));
                Assert.Equal(value, JsonSerializer.Deserialize<T?>(JsonSerializer.SerializeBytes<T?>(value)));

                var dictionary = new Dictionary<T, int>() { [value] = 1 };
                Assert.Equal(dictionary, JsonSerializer.Deserialize<Dictionary<T, int>>(JsonSerializer.Serialize(dictionary)));
                Assert.Equal(dictionary, JsonSerializer.Deserialize<Dictionary<T, int>>(JsonSerializer.Serialize(dictionary, numbers), numbers));
            }
        }
        [Fact]
        public void Enum_NumberOutOfUnderlyingRange_Throws()
        {
            var options = new JsonSerializerOptions() { ErrorOnTypeMismatch = true };
            foreach (var json in new[] { "300", "-1", "\"300\"", "\"-1\"" })
            {
                Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<EnumModel>(json, options));
                Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<EnumModel>(Encoding.UTF8.GetBytes(json), options));
            }

            Assert.Equal(EnumModel.EnumItem2, JsonSerializer.Deserialize<EnumModel>("2", options));
            Assert.Equal(EnumModel.EnumItem2, JsonSerializer.Deserialize<EnumModel?>("\"2\"", options));
            Assert.Equal(SignedEnum.Negative, JsonSerializer.Deserialize<SignedEnum?>(Encoding.UTF8.GetBytes("-1"), options));
        }

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
