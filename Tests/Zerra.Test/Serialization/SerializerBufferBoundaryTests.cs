// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Serialization
{
    public class SerializerBufferBoundaryTests
    {
        public sealed class Padded<T>
        {
            public string? Pad { get; set; }
            public byte[]? Bytes { get; set; }
            public T? Value { get; set; }
            public int After { get; set; }
        }

        private static readonly object[] scalars =
        [
            true, (byte)200, (sbyte)-100, (short)-30000, (ushort)60000, -2000000000, 4000000000u, long.MinValue, ulong.MaxValue,
            -1.234567e-30f, -1.2345678901234567e-300, -79228162514264337593543950335m, 'é', '"', '\n', '\u0001', (sbyte)-128, (short)-32768, int.MinValue, float.MinValue, double.MinValue, decimal.MinValue,
            new DateTime(2024, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), new DateTimeOffset(2024, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(-11)),
            TimeSpan.FromTicks(-12345678912345), new DateOnly(2024, 12, 31), new TimeOnly(23, 59, 59, 999), Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            EnumModel.EnumItem3,
        ];

        public static TheoryData<object> Values()
        {
            var data = new TheoryData<object>();
            foreach (var scalar in scalars)
            {
                data.Add(scalar);

                //arrays have their own writers for each type, with and without nulls
                var type = scalar.GetType();
                var array = Array.CreateInstance(type, 3);
                array.SetValue(scalar, 0);
                array.SetValue(scalar, 1);
                array.SetValue(scalar, 2);
                data.Add(array);
                var nullableArray = Array.CreateInstance(typeof(Nullable<>).MakeGenericType(type), 3);
                nullableArray.SetValue(scalar, 0);
                nullableArray.SetValue(scalar, 2);
                data.Add(nullableArray);
            }
            data.Add("a\"b\\c\né中😀 text");
            data.Add(new string?[] { "a\"b", null, "c" });
            data.Add(new byte[] { 1, 2, 3, 250, 251, 252, 253 });
            data.Add(new List<string?>() { "a", null, "c" });
            data.Add(new Dictionary<string, int>() { ["key"] = 1 });
            data.Add(false);
            data.Add("");
            data.Add("\"\\\n\t\u0001\u001f");
            data.Add(new Dictionary<string, int>());
            data.Add(new Dictionary<string, string>() { ["k\"e\\y\n"] = "v\u0002" });
            data.Add(new SimpleModel() { Value1 = 1, Value2 = "a" });
            data.Add(new SimpleModel());
            return data;
        }

        [Fact]
        public async Task Null_AtEveryPositionOfTheBufferEnd()
        {
            await AssertPadded<string>(null);
            await AssertPadded<int?>(null);
            await AssertPadded<SimpleModel>(null);
            await AssertPadded<int[]>(null);
        }

        //streams and the growing output buffer run out partway through the value at each of these pads,
        //a pad that grows by one character moves every byte of the value across where the buffer ends
        //the byte serializer pads with bytes, a string reserves room for its longest encoding so the buffer would grow before the value
        private const int jsonBuffer = 16 * 1024;
        private const int byteBuffer = 8 * 1024;

        [Theory]
        [MemberData(nameof(Values))]
        public async Task Value_AtEveryPositionOfTheBufferEnd(object value)
        {
            var method = typeof(SerializerBufferBoundaryTests).GetMethod(nameof(AssertPadded), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(value.GetType());
            await (Task)method.Invoke(null, [value])!;

            //the nullable form of a value type too
            if (value.GetType().IsValueType)
            {
                var nullable = typeof(SerializerBufferBoundaryTests).GetMethod(nameof(AssertPadded), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(typeof(Nullable<>).MakeGenericType(value.GetType()));
                await (Task)nullable.Invoke(null, [value])!;
            }
        }

        private static async Task AssertPadded<T>(T value)
        {
            var token = TestContext.Current.CancellationToken;
            var byteOptions = new ByteSerializerOptions();

            for (var pad = jsonBuffer - 100; pad <= jsonBuffer; pad++)
            {
                var model = new Padded<T>() { Pad = new string('x', pad), Value = value, After = 7 };
                var expected = JsonSerializer.Serialize(model);

                Assert.Equal(expected, JsonSerializer.Serialize(JsonSerializer.Deserialize<Padded<T>>(expected)));
                var bytes = JsonSerializer.SerializeBytes(model);
                Assert.Equal(expected, JsonSerializer.Serialize(JsonSerializer.Deserialize<Padded<T>>(bytes)));

                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, model, cancellationToken: token);
                Assert.Equal(bytes, stream.ToArray());
                stream.Position = 0;
                Assert.Equal(expected, JsonSerializer.Serialize(await JsonSerializer.DeserializeAsync<Padded<T>>(stream, cancellationToken: token)));
            }

            for (var pad = byteBuffer - 100; pad <= byteBuffer; pad++)
            {
                var model = new Padded<T>() { Bytes = new byte[pad], Value = value, After = 7 };
                var expected = JsonSerializer.Serialize(model);

                var bytes = ByteSerializer.Serialize(model, byteOptions);
                Assert.Equal(expected, JsonSerializer.Serialize(ByteSerializer.Deserialize<Padded<T>>(bytes, byteOptions)));

                using var stream = new MemoryStream();
                await ByteSerializer.SerializeAsync(stream, model, byteOptions, token);
                Assert.Equal(bytes, stream.ToArray());
                stream.Position = 0;
                Assert.Equal(expected, JsonSerializer.Serialize(await ByteSerializer.DeserializeAsync<Padded<T>>(stream, byteOptions, token)));
            }
        }
    }
}
