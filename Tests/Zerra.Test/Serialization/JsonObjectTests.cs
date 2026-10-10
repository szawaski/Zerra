// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Text;
using Xunit;
using Zerra.Serialization.Json;

namespace Zerra.Test.Serialization
{
    public class JsonObjectTests
    {
        public JsonObjectTests()
        {
#if DEBUG
            Zerra.Serialization.Json.IO.JsonReader.Testing = true;
#endif
        }

        [Fact]
        public void ToString_RoundTrip()
        {
            const string json = "{\"Null\":null,\"True\":true,\"False\":false,\"Int\":-12,\"Decimal\":1.5,\"String\":\"Hello\",\"Empty\":\"\",\"Object\":{\"Inner\":[1,\"two\",{}]},\"Array\":[],\"Nested\":[[1,2],[3]]}";
            var obj = JsonSerializer.DeserializeJsonObject(json)!;
            Assert.Equal(json, obj.ToString());
        }

        [Theory]
        [InlineData("quote\"slash\\")]
        [InlineData("\b\f\n\r\t")]
        [InlineData("\u0001\u001F")]
        [InlineData("😀")]
        [InlineData("start \n middle \u0002 end")]
        public void ToString_EscapesLikeSerializer(string value)
        {
            var obj = new JsonObject(value);
            Assert.Equal(JsonSerializer.Serialize(value), obj.ToString());
            Assert.Equal(value, (string?)JsonSerializer.DeserializeJsonObject(obj.ToString())!);
        }

        [Fact]
        public void ToString_NumbersIgnoreCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NegativeSign = "−";
            CultureInfo.CurrentCulture = culture;
            try
            {
                Assert.Equal("-1.5", new JsonObject(-1.5m).ToString());
                Assert.Equal("[-1.5,2.25]", JsonSerializer.DeserializeJsonObject("[-1.5,2.25]")!.ToString());
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Constructors()
        {
            Assert.Equal(JsonObject.JsonObjectType.Null, new JsonObject().JsonType);
            Assert.True(new JsonObject().IsNull);
            Assert.Equal(JsonObject.JsonObjectType.Boolean, new JsonObject(true).JsonType);
            Assert.Equal(JsonObject.JsonObjectType.Number, new JsonObject(1m).JsonType);
            Assert.Equal(JsonObject.JsonObjectType.String, new JsonObject("a").JsonType);
            Assert.Equal(JsonObject.JsonObjectType.Object, new JsonObject(new Dictionary<string, JsonObject>()).JsonType);
            Assert.Equal(JsonObject.JsonObjectType.Array, new JsonObject(new List<JsonObject>()).JsonType);
            Assert.False(new JsonObject(false).IsNull);

            Assert.Equal("null", new JsonObject().ToString());
            Assert.Equal("true", new JsonObject(true).ToString());
            Assert.Equal("false", new JsonObject(false).ToString());
            Assert.Equal("\"\"", new JsonObject("").ToString());
            Assert.Equal("{}", new JsonObject(new Dictionary<string, JsonObject>()).ToString());
            Assert.Equal("[]", new JsonObject(new List<JsonObject>()).ToString());
        }

        [Fact]
        public void ObjectIndexer()
        {
            var obj = JsonSerializer.DeserializeJsonObject("{\"A\":1}")!;
            Assert.Equal(1, (int)obj["A"]);

            obj["A"] = new JsonObject(2m);
            obj["B"] = new JsonObject("b");
            Assert.Equal("{\"A\":2,\"B\":\"b\"}", obj.ToString());

            Assert.Throws<ArgumentException>(() => obj["Missing"]);
            Assert.Throws<InvalidCastException>(() => obj[0]);
            Assert.Throws<InvalidCastException>(() => obj[0] = new JsonObject());
            Assert.Throws<InvalidCastException>(() => obj.Add(new JsonObject()));
            Assert.Throws<InvalidCastException>(() => obj.Remove(new JsonObject()));
            Assert.Throws<InvalidCastException>(() => obj.GetEnumerator());
            Assert.Throws<InvalidCastException>(() => ((System.Collections.IEnumerable)obj).GetEnumerator());
        }

        [Fact]
        public void ArrayIndexer()
        {
            var array = JsonSerializer.DeserializeJsonObject("[1,2]")!;
            Assert.Equal(2, (int)array[1]);

            array[1] = new JsonObject(5m);
            var added = new JsonObject("x");
            array.Add(added);
            Assert.Equal("[1,5,\"x\"]", array.ToString());

            Assert.True(array.Remove(added));
            Assert.False(array.Remove(added));
            Assert.Equal("[1,5]", array.ToString());

            var values = new List<int>();
            foreach (var item in array)
                values.Add((int)item);
            Assert.Equal([1, 5], values);
            var count = 0;
            foreach (var _ in (System.Collections.IEnumerable)array)
                count++;
            Assert.Equal(2, count);

            Assert.Throws<ArgumentOutOfRangeException>(() => array[5]);
            Assert.Throws<InvalidCastException>(() => array["A"]);
            Assert.Throws<InvalidCastException>(() => array["A"] = new JsonObject());
        }

        [Fact]
        public void Cast_Numbers()
        {
            var obj = JsonSerializer.DeserializeJsonObject("{\"Value\":42,\"Null\":null,\"Text\":\"42\"}")!;
            var value = obj["Value"];
            var nul = obj["Null"];
            var text = obj["Text"];

            Assert.Equal((byte)42, (byte)value);
            Assert.Equal((sbyte)42, (sbyte)value);
            Assert.Equal((short)42, (short)value);
            Assert.Equal((ushort)42, (ushort)value);
            Assert.Equal(42, (int)value);
            Assert.Equal(42u, (uint)value);
            Assert.Equal(42L, (long)value);
            Assert.Equal(42UL, (ulong)value);
            Assert.Equal(42f, (float)value);
            Assert.Equal(42d, (double)value);
            Assert.Equal(42m, (decimal)value);

            Assert.Equal((byte)42, (byte?)value);
            Assert.Equal((sbyte)42, (sbyte?)value);
            Assert.Equal((short)42, (short?)value);
            Assert.Equal((ushort)42, (ushort?)value);
            Assert.Equal(42, (int?)value);
            Assert.Equal(42u, (uint?)value);
            Assert.Equal(42L, (long?)value);
            Assert.Equal(42UL, (ulong?)value);
            Assert.Equal(42f, (float?)value);
            Assert.Equal(42d, (double?)value);
            Assert.Equal(42m, (decimal?)value);

            Assert.Null((byte?)nul);
            Assert.Null((sbyte?)nul);
            Assert.Null((short?)nul);
            Assert.Null((ushort?)nul);
            Assert.Null((int?)nul);
            Assert.Null((uint?)nul);
            Assert.Null((long?)nul);
            Assert.Null((ulong?)nul);
            Assert.Null((float?)nul);
            Assert.Null((double?)nul);
            Assert.Null((decimal?)nul);

            Assert.Throws<InvalidCastException>(() => (byte)text);
            Assert.Throws<InvalidCastException>(() => (sbyte)text);
            Assert.Throws<InvalidCastException>(() => (short)text);
            Assert.Throws<InvalidCastException>(() => (ushort)text);
            Assert.Throws<InvalidCastException>(() => (int)text);
            Assert.Throws<InvalidCastException>(() => (uint)text);
            Assert.Throws<InvalidCastException>(() => (long)text);
            Assert.Throws<InvalidCastException>(() => (ulong)text);
            Assert.Throws<InvalidCastException>(() => (float)text);
            Assert.Throws<InvalidCastException>(() => (double)text);
            Assert.Throws<InvalidCastException>(() => (decimal)text);

            Assert.Throws<InvalidCastException>(() => (byte?)text);
            Assert.Throws<InvalidCastException>(() => (sbyte?)text);
            Assert.Throws<InvalidCastException>(() => (short?)text);
            Assert.Throws<InvalidCastException>(() => (ushort?)text);
            Assert.Throws<InvalidCastException>(() => (int?)text);
            Assert.Throws<InvalidCastException>(() => (uint?)text);
            Assert.Throws<InvalidCastException>(() => (long?)text);
            Assert.Throws<InvalidCastException>(() => (ulong?)text);
            Assert.Throws<InvalidCastException>(() => (float?)text);
            Assert.Throws<InvalidCastException>(() => (double?)text);
            Assert.Throws<InvalidCastException>(() => (decimal?)text);
        }

        [Fact]
        public void Cast_Boolean()
        {
            Assert.True((bool)new JsonObject(true));
            Assert.False((bool?)new JsonObject(false));
            Assert.Null((bool?)new JsonObject());
            Assert.Throws<InvalidCastException>(() => (bool)new JsonObject(1m));
            Assert.Throws<InvalidCastException>(() => (bool?)new JsonObject(1m));
        }

        [Fact]
        public void Cast_Strings()
        {
            var dateTime = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
            var dateTimeOffset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(-5));
            var timeSpan = new TimeSpan(1, 2, 3, 4, 500);
#if !NETSTANDARD2_0
            var dateOnly = new DateOnly(2024, 1, 2);
            var timeOnly = new TimeOnly(3, 4, 5, 600);
#endif
            var guid = Guid.NewGuid();

#if !NETSTANDARD2_0
            var json = JsonSerializer.Serialize(new { Char = 'c', DateTime = dateTime, DateTimeOffset = dateTimeOffset, TimeSpan = timeSpan, DateOnly = dateOnly, TimeOnly = timeOnly, Guid = guid, String = "s", Empty = "", Null = (string?)null, Number = 1 });
#else
            var json = JsonSerializer.Serialize(new { Char = 'c', DateTime = dateTime, DateTimeOffset = dateTimeOffset, TimeSpan = timeSpan, Guid = guid, String = "s", Empty = "", Null = (string?)null, Number = 1 });
#endif
            var obj = JsonSerializer.DeserializeJsonObject(json)!;
            var nul = obj["Null"];
            var number = obj["Number"];

            Assert.Equal('c', (char)obj["Char"]);
            Assert.Equal(dateTime, (DateTime)obj["DateTime"]);
            Assert.Equal(DateTimeKind.Utc, ((DateTime)obj["DateTime"]).Kind);
            Assert.Equal(dateTimeOffset, (DateTimeOffset)obj["DateTimeOffset"]);
            Assert.Equal(dateTimeOffset.Offset, ((DateTimeOffset)obj["DateTimeOffset"]).Offset);
            Assert.Equal(timeSpan, (TimeSpan)obj["TimeSpan"]);
#if !NETSTANDARD2_0
            Assert.Equal(dateOnly, (DateOnly)obj["DateOnly"]);
            Assert.Equal(timeOnly, (TimeOnly)obj["TimeOnly"]);
#endif
            Assert.Equal(guid, (Guid)obj["Guid"]);
            Assert.Equal("s", (string?)obj["String"]);

            Assert.Equal('c', (char?)obj["Char"]);
            Assert.Equal(dateTime, (DateTime?)obj["DateTime"]);
            Assert.Equal(dateTimeOffset, (DateTimeOffset?)obj["DateTimeOffset"]);
            Assert.Equal(timeSpan, (TimeSpan?)obj["TimeSpan"]);
#if !NETSTANDARD2_0
            Assert.Equal(dateOnly, (DateOnly?)obj["DateOnly"]);
            Assert.Equal(timeOnly, (TimeOnly?)obj["TimeOnly"]);
#endif
            Assert.Equal(guid, (Guid?)obj["Guid"]);

            Assert.Null((char?)nul);
            Assert.Null((DateTime?)nul);
            Assert.Null((DateTimeOffset?)nul);
            Assert.Null((TimeSpan?)nul);
#if !NETSTANDARD2_0
            Assert.Null((DateOnly?)nul);
            Assert.Null((TimeOnly?)nul);
#endif
            Assert.Null((Guid?)nul);
            Assert.Null((string?)nul);

            Assert.Throws<InvalidCastException>(() => (char)obj["Empty"]);
            Assert.Throws<InvalidCastException>(() => (char?)obj["Empty"]);

            Assert.Throws<InvalidCastException>(() => (char)number);
            Assert.Throws<InvalidCastException>(() => (DateTime)number);
            Assert.Throws<InvalidCastException>(() => (DateTimeOffset)number);
            Assert.Throws<InvalidCastException>(() => (TimeSpan)number);
#if !NETSTANDARD2_0
            Assert.Throws<InvalidCastException>(() => (DateOnly)number);
            Assert.Throws<InvalidCastException>(() => (TimeOnly)number);
#endif
            Assert.Throws<InvalidCastException>(() => (Guid)number);
            Assert.Throws<InvalidCastException>(() => (string?)number);

            Assert.Throws<InvalidCastException>(() => (char?)number);
            Assert.Throws<InvalidCastException>(() => (DateTime?)number);
            Assert.Throws<InvalidCastException>(() => (DateTimeOffset?)number);
            Assert.Throws<InvalidCastException>(() => (TimeSpan?)number);
#if !NETSTANDARD2_0
            Assert.Throws<InvalidCastException>(() => (DateOnly?)number);
            Assert.Throws<InvalidCastException>(() => (TimeOnly?)number);
#endif
            Assert.Throws<InvalidCastException>(() => (Guid?)number);
        }

        [Fact]
        public void Cast_Arrays()
        {
            var array = JsonSerializer.DeserializeJsonObject("[1,2]")!;
            Assert.Equal([1, 2], ((JsonObject[])array!).Select(x => (int)x));
            Assert.Equal([1, 2], ((List<JsonObject>)array!).Select(x => (int)x));
            Assert.Null((JsonObject[]?)new JsonObject());
            Assert.Null((List<JsonObject>?)new JsonObject());
            Assert.Throws<InvalidCastException>(() => (JsonObject[]?)new JsonObject(1m));
            Assert.Throws<InvalidCastException>(() => (List<JsonObject>?)new JsonObject(1m));
        }

        [Fact]
        public async Task RootScalars()
        {
            foreach (var useBytes in new[] { false, true })
            {
                JsonObject Read(string json) => (useBytes ? JsonSerializer.DeserializeJsonObject(Encoding.UTF8.GetBytes(json)) : JsonSerializer.DeserializeJsonObject(json))!;

                Assert.True(Read("null").IsNull);
                Assert.True((bool)Read("true"));
                Assert.False((bool)Read("false"));
                Assert.Equal("a\nb", (string?)Read("\"a\nb\""));
                Assert.Equal("ab", (string?)Read("\"ab\""));
                Assert.Equal(5m, (decimal)Read("5"));
            }

            var streamed = await JsonSerializer.DeserializeJsonObjectAsync(new MemoryStream(Encoding.UTF8.GetBytes("\"a\nb\"")), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("a\nb", (string?)streamed!);
        }

        [Fact]
        public void Numbers_OutsideDecimalRange()
        {
            foreach (var json in new[] { "1e400", "[1e400]", "{\"a\":1e400}" })
            {
                Assert.Equal(json, JsonSerializer.DeserializeJsonObject(json)!.ToString());
                Assert.Equal(json, JsonSerializer.DeserializeJsonObject(Encoding.UTF8.GetBytes(json))!.ToString());
            }

            var big = JsonSerializer.DeserializeJsonObject("1e300")!;
            Assert.Equal(JsonObject.JsonObjectType.Number, big.JsonType);
            Assert.Equal(1e300, (double)big);
            Assert.Throws<OverflowException>(() => (decimal)big);
            Assert.Throws<OverflowException>(() => (int)big);
            Assert.Equal(double.PositiveInfinity, (double)JsonSerializer.DeserializeJsonObject("1e400")!);

            Assert.Equal(1e-30, (double)JsonSerializer.DeserializeJsonObject(Encoding.UTF8.GetBytes("1e-30"))!);
            Assert.Equal(1e-30, (double)JsonSerializer.DeserializeJsonObject("1e-30")!);
            Assert.Equal(0m, (decimal)JsonSerializer.DeserializeJsonObject("0e5")!);
        }

        [Fact]
        public void Numbers_Exponents()
        {
            Assert.Equal(100000m, (decimal)JsonSerializer.DeserializeJsonObject("1e5")!);
            Assert.Equal(100000m, (decimal)JsonSerializer.DeserializeJsonObject(Encoding.UTF8.GetBytes("1e5"))!);
            Assert.Equal(-0.025m, (decimal)JsonSerializer.DeserializeJsonObject("-2.5E-2")!);
            Assert.Equal(100000, (int)JsonSerializer.DeserializeJsonObject("[1e5]")![0]);
        }
    }
}
