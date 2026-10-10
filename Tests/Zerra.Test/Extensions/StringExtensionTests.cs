// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using Xunit;

namespace Zerra.Test.Extensions
{
    public class StringExtensionTests
    {
        private static CultureInfo CreateCulture()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            culture.NumberFormat.NegativeSign = "−";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            culture.DateTimeFormat.DateSeparator = ".";
            return culture;
        }

        private static void WithCurrentCulture(CultureInfo culture, Action action)
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = culture;
            try
            {
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Parse_Integers_InvariantByDefaultOrProvider()
        {
            var culture = CreateCulture();
            WithCurrentCulture(culture, () =>
            {
                Assert.Equal((byte)200, "200".ToByte());
                Assert.Equal((byte)200, "200".ToByteNullable());
                Assert.Equal((short)-42, "-42".ToInt16());
                Assert.Equal((short)-42, "-42".ToInt16Nullable());
                Assert.Equal((ushort)42, "42".ToUInt16());
                Assert.Equal((ushort)42, "42".ToUInt16Nullable());
                Assert.Equal(-42, "-42".ToInt32());
                Assert.Equal(-42, "-42".ToInt32Nullable());
                Assert.Equal(42u, "42".ToUInt32());
                Assert.Equal(42u, "42".ToUInt32Nullable());
                Assert.Equal(-42L, "-42".ToInt64());
                Assert.Equal(-42L, "-42".ToInt64Nullable());
                Assert.Equal(42ul, "42".ToUInt64());
                Assert.Equal(42ul, "42".ToUInt64Nullable());

                Assert.Equal((byte)200, "200".ToByte(provider: culture));
                Assert.Equal((byte)200, "200".ToByteNullable(culture));
                Assert.Equal((short)-42, "−42".ToInt16(provider: culture));
                Assert.Equal((short)-42, "−42".ToInt16Nullable(culture));
                Assert.Equal((ushort)42, "42".ToUInt16(provider: culture));
                Assert.Equal((ushort)42, "42".ToUInt16Nullable(culture));
                Assert.Equal(-42, "−42".ToInt32(provider: culture));
                Assert.Equal(-42, "−42".ToInt32Nullable(culture));
                Assert.Equal(42u, "42".ToUInt32(provider: culture));
                Assert.Equal(42u, "42".ToUInt32Nullable(culture));
                Assert.Equal(-42L, "−42".ToInt64(provider: culture));
                Assert.Equal(-42L, "−42".ToInt64Nullable(culture));
                Assert.Equal(42ul, "42".ToUInt64(provider: culture));
                Assert.Equal(42ul, "42".ToUInt64Nullable(culture));

                Assert.Equal(0, "−42".ToInt32());
                Assert.Null("−42".ToInt32Nullable());
            });
        }

        [Fact]
        public void Parse_FloatingPoint_InvariantByDefaultOrProvider()
        {
            var culture = CreateCulture();
            WithCurrentCulture(culture, () =>
            {
                Assert.Equal(-1.5f, "-1.5".ToFloat());
                Assert.Equal(-1.5f, "-1.5".ToFloatNullable());
                Assert.Equal(-1.5, "-1.5".ToDouble());
                Assert.Equal(-1.5, "-1.5".ToDoubleNullable());
                Assert.Equal(-1.5m, "-1.5".ToDecimal());
                Assert.Equal(-1.5m, "-1.5".ToDecimalNullable());

                Assert.Equal(-1.5f, "−1,5".ToFloat(provider: culture));
                Assert.Equal(-1.5f, "−1,5".ToFloatNullable(culture));
                Assert.Equal(-1.5, "−1,5".ToDouble(provider: culture));
                Assert.Equal(-1.5, "−1,5".ToDoubleNullable(culture));
                Assert.Equal(-1.5m, "−1,5".ToDecimal(provider: culture));
                Assert.Equal(-1.5m, "−1,5".ToDecimalNullable(culture));
            });
        }

        [Fact]
        public void Parse_DatesAndTimes_InvariantByDefaultOrProvider()
        {
            var culture = CreateCulture();
            var date = new DateTime(2026, 10, 6);
            var offset = new DateTimeOffset(2026, 10, 6, 13, 45, 30, TimeSpan.FromHours(-5));
            WithCurrentCulture(culture, () =>
            {
                Assert.Equal(date, "10/06/2026".ToDateTime());
                Assert.Equal(date, "10/06/2026".ToDateTimeNullable());
                Assert.Equal(offset, "10/06/2026 13:45:30 -05:00".ToDateTimeOffset());
                Assert.Equal(offset, "10/06/2026 13:45:30 -05:00".ToDateTimeOffsetNullable());
                Assert.Equal(new TimeSpan(0, 1, 2, 3, 500), "01:02:03.5".ToTimeSpan());
                Assert.Equal(new TimeSpan(0, 1, 2, 3, 500), "01:02:03.5".ToTimeSpanNullable());
#if !NETSTANDARD2_0
                Assert.Equal(new DateOnly(2026, 10, 6), "10/06/2026".ToDateOnly());
                Assert.Equal(new DateOnly(2026, 10, 6), "10/06/2026".ToDateOnlyNullable());
                Assert.Equal(new TimeOnly(13, 45, 30), "13:45:30".ToTimeOnly());
                Assert.Equal(new TimeOnly(13, 45, 30), "13:45:30".ToTimeOnlyNullable());
#endif

                Assert.Equal(date, "06.10.2026".ToDateTime(provider: culture));
                Assert.Equal(date, "06.10.2026".ToDateTimeNullable(culture));
                Assert.Equal(offset, "06.10.2026 13:45:30 -05:00".ToDateTimeOffset(provider: culture));
                Assert.Equal(offset, "06.10.2026 13:45:30 -05:00".ToDateTimeOffsetNullable(culture));
                Assert.Equal(new TimeSpan(0, 1, 2, 3, 500), "01:02:03.5".ToTimeSpan(provider: culture));
                Assert.Equal(new TimeSpan(0, 1, 2, 3, 500), "01:02:03.5".ToTimeSpanNullable(culture));
#if !NETSTANDARD2_0
                Assert.Equal(new DateOnly(2026, 10, 6), "06.10.2026".ToDateOnly(provider: culture));
                Assert.Equal(new DateOnly(2026, 10, 6), "06.10.2026".ToDateOnlyNullable(culture));
                Assert.Equal(new TimeOnly(13, 45, 30), "13:45:30".ToTimeOnly(provider: culture));
                Assert.Equal(new TimeOnly(13, 45, 30), "13:45:30".ToTimeOnlyNullable(culture));
#endif
            });
        }
        [Fact]
        public void Truncate_ReportsTruncation()
        {
            Assert.Equal("hello", "hello".Truncate(10, out var truncated));
            Assert.False(truncated);

            //exactly at the limit is not a truncation
            Assert.Equal("hello", "hello".Truncate(5, out truncated));
            Assert.False(truncated);

            Assert.Equal("hello", "hello world".Truncate(5, out truncated));
            Assert.True(truncated);

            Assert.Equal("", "test".Truncate(0, out truncated));
            Assert.True(truncated);
        }

        [Fact]
        public void Join_ReportsTruncation()
        {
            _ = StringExtensions.Join(11, "-", "hello", "world", out var truncated);
            Assert.False(truncated);
            _ = StringExtensions.Join(8, "-", "hello", "world", out truncated);
            Assert.True(truncated);

            _ = StringExtensions.Join(16, "-", "hello", "world", "test", out truncated);
            Assert.False(truncated);
            _ = StringExtensions.Join(10, "-", "hello", "world", "test", out truncated);
            Assert.True(truncated);

            _ = StringExtensions.Join(21, "-", "hello", "world", "test", "four", out truncated);
            Assert.False(truncated);
            _ = StringExtensions.Join(12, "-", "hello", "world", "test", "four", out truncated);
            Assert.True(truncated);
        }

        [Fact]
        public void Join_TwoStrings()
        {
            _ = Assert.Throws<ArgumentException>(() => StringExtensions.Join(0, "-", "a", "b"));

            var result = StringExtensions.Join(11, "-", "hello", "world");
            Assert.Equal("hello-world", result);

            result = StringExtensions.Join(8, "-", "hello", "world");
            Assert.Equal(8, result.Length);
            Assert.Contains("-", result);

            result = StringExtensions.Join(8, "[]", "hello", "world");
            Assert.Equal(8, result.Length);
            Assert.Contains("[]", result);

            result = StringExtensions.Join(1, "-", "", "");
            Assert.Equal("-", result);
        }

        [Fact]
        public void Join_ThreeStrings()
        {
            _ = Assert.Throws<ArgumentException>(() => StringExtensions.Join(1, "-", "a", "b", "c"));

            var result = StringExtensions.Join(16, "-", "hello", "world", "test");
            Assert.Equal("hello-world-test", result);

            result = StringExtensions.Join(10, "-", "hello", "world", "test");
            Assert.Equal(10, result.Length);
            Assert.Contains("-", result);

            result = StringExtensions.Join(10, "[]", "hello", "world", "test");
            Assert.Equal(10, result.Length);
            Assert.Contains("[]", result);

            result = StringExtensions.Join(2, "-", "", "", "");
            Assert.Equal("--", result);
        }

        [Fact]
        public void Join_FourStrings()
        {
            _ = Assert.Throws<ArgumentException>(() => StringExtensions.Join(2, "-", "a", "b", "c", "d"));

            var result = StringExtensions.Join(21, "-", "hello", "world", "test", "four");
            Assert.Equal("hello-world-test-four", result);

            result = StringExtensions.Join(12, "-", "hello", "world", "test", "four");
            Assert.Equal(12, result.Length);
            Assert.Contains("-", result);

            result = StringExtensions.Join(12, "[]", "hello", "world", "test", "four");
            Assert.Equal(12, result.Length);
            Assert.Contains("[]", result);

            result = StringExtensions.Join(3, "-", "", "", "", "");
            Assert.Equal("---", result);
        }

        [Fact]
        public void ToBoolean()
        {
            string? nullString = null;
            Assert.False(nullString.ToBoolean());
            Assert.True(nullString.ToBoolean(true));

            Assert.False("".ToBoolean());
            Assert.True("".ToBoolean(true));

            Assert.True("1".ToBoolean());
            Assert.False("0".ToBoolean());
            Assert.False("-1".ToBoolean());

            Assert.True("true".ToBoolean());
            Assert.True("True".ToBoolean());
            Assert.True("TRUE".ToBoolean());

            Assert.False("false".ToBoolean());
            Assert.False("False".ToBoolean());
            Assert.False("FALSE".ToBoolean());

            Assert.False("invalid".ToBoolean());
            Assert.True("invalid".ToBoolean(true));
        }

        [Fact]
        public void ToBooleanNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToBooleanNullable());
            Assert.Null("".ToBooleanNullable());

            Assert.True("1".ToBooleanNullable());
            Assert.False("0".ToBooleanNullable());
            Assert.False("-1".ToBooleanNullable());

            Assert.True("true".ToBooleanNullable());
            Assert.False("false".ToBooleanNullable());

            Assert.Null("invalid".ToBooleanNullable());
        }

        [Fact]
        public void ToByte()
        {
            string? nullString = null;
            Assert.Equal(0, nullString.ToByte());
            Assert.Equal(42, nullString.ToByte(42));

            Assert.Equal(0, "".ToByte());
            Assert.Equal(42, "".ToByte(42));

            Assert.Equal(123, "123".ToByte());
            Assert.Equal(0, "0".ToByte());
            Assert.Equal(255, "255".ToByte());

            Assert.Equal(0, "256".ToByte());
            Assert.Equal(42, "256".ToByte(42));

            Assert.Equal(0, "invalid".ToByte());
            Assert.Equal(42, "invalid".ToByte(42));
        }

        [Fact]
        public void ToByteNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToByteNullable());
            Assert.Null("".ToByteNullable());

            Assert.Equal((byte)123, "123".ToByteNullable());
            Assert.Equal((byte)0, "0".ToByteNullable());
            Assert.Equal((byte)255, "255".ToByteNullable());

            Assert.Null("invalid".ToByteNullable());
            Assert.Null("256".ToByteNullable());
        }

        [Fact]
        public void ToInt16()
        {
            string? nullString = null;
            Assert.Equal(0, nullString.ToInt16());
            Assert.Equal(42, nullString.ToInt16(42));

            Assert.Equal(0, "".ToInt16());
            Assert.Equal(42, "".ToInt16(42));

            Assert.Equal(12345, "12345".ToInt16());
            Assert.Equal(-12345, "-12345".ToInt16());
            Assert.Equal(0, "0".ToInt16());

            Assert.Equal(0, "40000".ToInt16());
            Assert.Equal(42, "40000".ToInt16(42));

            Assert.Equal(0, "invalid".ToInt16());
            Assert.Equal(42, "invalid".ToInt16(42));
        }

        [Fact]
        public void ToInt16Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToInt16Nullable());
            Assert.Null("".ToInt16Nullable());

            Assert.Equal((short)12345, "12345".ToInt16Nullable());
            Assert.Equal((short)-12345, "-12345".ToInt16Nullable());

            Assert.Null("invalid".ToInt16Nullable());
            Assert.Null("40000".ToInt16Nullable());
        }

        [Fact]
        public void ToUInt16()
        {
            string? nullString = null;
            Assert.Equal(0u, nullString.ToUInt16());
            Assert.Equal(42u, nullString.ToUInt16(42));

            Assert.Equal(0u, "".ToUInt16());
            Assert.Equal(42u, "".ToUInt16(42));

            Assert.Equal(12345u, "12345".ToUInt16());
            Assert.Equal(0u, "0".ToUInt16());
            Assert.Equal(65535u, "65535".ToUInt16());

            Assert.Equal(0u, "65536".ToUInt16());
            Assert.Equal(0u, "-1".ToUInt16());

            Assert.Equal(0u, "invalid".ToUInt16());
            Assert.Equal(42u, "invalid".ToUInt16(42));
        }

        [Fact]
        public void ToUInt16Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToUInt16Nullable());
            Assert.Null("".ToUInt16Nullable());

            Assert.Equal((ushort)12345, "12345".ToUInt16Nullable());
            Assert.Equal((ushort)65535, "65535".ToUInt16Nullable());

            Assert.Null("invalid".ToUInt16Nullable());
            Assert.Null("65536".ToUInt16Nullable());
        }

        [Fact]
        public void ToInt32()
        {
            string? nullString = null;
            Assert.Equal(0, nullString.ToInt32());
            Assert.Equal(42, nullString.ToInt32(42));

            Assert.Equal(0, "".ToInt32());
            Assert.Equal(42, "".ToInt32(42));

            Assert.Equal(123456, "123456".ToInt32());
            Assert.Equal(-123456, "-123456".ToInt32());
            Assert.Equal(0, "0".ToInt32());

            Assert.Equal(0, "2147483648".ToInt32());
            Assert.Equal(42, "2147483648".ToInt32(42));

            Assert.Equal(0, "invalid".ToInt32());
            Assert.Equal(42, "invalid".ToInt32(42));
        }

        [Fact]
        public void ToInt32Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToInt32Nullable());
            Assert.Null("".ToInt32Nullable());

            Assert.Equal(123456, "123456".ToInt32Nullable());
            Assert.Equal(-123456, "-123456".ToInt32Nullable());

            Assert.Null("invalid".ToInt32Nullable());
            Assert.Null("2147483648".ToInt32Nullable());
        }

        [Fact]
        public void ToUInt32()
        {
            string? nullString = null;
            Assert.Equal(0u, nullString.ToUInt32());
            Assert.Equal(42u, nullString.ToUInt32(42));

            Assert.Equal(0u, "".ToUInt32());
            Assert.Equal(42u, "".ToUInt32(42));

            Assert.Equal(123456u, "123456".ToUInt32());
            Assert.Equal(0u, "0".ToUInt32());
            Assert.Equal(4294967295u, "4294967295".ToUInt32());

            Assert.Equal(0u, "4294967296".ToUInt32());
            Assert.Equal(0u, "-1".ToUInt32());

            Assert.Equal(0u, "invalid".ToUInt32());
            Assert.Equal(42u, "invalid".ToUInt32(42));
        }

        [Fact]
        public void ToUInt32Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToUInt32Nullable());
            Assert.Null("".ToUInt32Nullable());

            Assert.Equal(123456u, "123456".ToUInt32Nullable());
            Assert.Equal(4294967295u, "4294967295".ToUInt32Nullable());

            Assert.Null("invalid".ToUInt32Nullable());
            Assert.Null("4294967296".ToUInt32Nullable());
        }

        [Fact]
        public void ToInt64()
        {
            string? nullString = null;
            Assert.Equal(0L, nullString.ToInt64());
            Assert.Equal(42L, nullString.ToInt64(42));

            Assert.Equal(0L, "".ToInt64());
            Assert.Equal(42L, "".ToInt64(42));

            Assert.Equal(1234567890L, "1234567890".ToInt64());
            Assert.Equal(-1234567890L, "-1234567890".ToInt64());
            Assert.Equal(0L, "0".ToInt64());

            Assert.Equal(0L, "9223372036854775808".ToInt64());
            Assert.Equal(42L, "9223372036854775808".ToInt64(42));

            Assert.Equal(0L, "invalid".ToInt64());
            Assert.Equal(42L, "invalid".ToInt64(42));
        }

        [Fact]
        public void ToInt64Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToInt64Nullable());
            Assert.Null("".ToInt64Nullable());

            Assert.Equal(1234567890L, "1234567890".ToInt64Nullable());
            Assert.Equal(-1234567890L, "-1234567890".ToInt64Nullable());

            Assert.Null("invalid".ToInt64Nullable());
            Assert.Null("9223372036854775808".ToInt64Nullable());
        }

        [Fact]
        public void ToUInt64()
        {
            string? nullString = null;
            Assert.Equal(0UL, nullString.ToUInt64());
            Assert.Equal(42UL, nullString.ToUInt64(42));

            Assert.Equal(0UL, "".ToUInt64());
            Assert.Equal(42UL, "".ToUInt64(42));

            Assert.Equal(1234567890UL, "1234567890".ToUInt64());
            Assert.Equal(0UL, "0".ToUInt64());
            Assert.Equal(18446744073709551615UL, "18446744073709551615".ToUInt64());

            Assert.Equal(0UL, "18446744073709551616".ToUInt64());
            Assert.Equal(0UL, "-1".ToUInt64());

            Assert.Equal(0UL, "invalid".ToUInt64());
            Assert.Equal(42UL, "invalid".ToUInt64(42));
        }

        [Fact]
        public void ToUInt64Nullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToUInt64Nullable());
            Assert.Null("".ToUInt64Nullable());

            Assert.Equal(1234567890UL, "1234567890".ToUInt64Nullable());
            Assert.Equal(18446744073709551615UL, "18446744073709551615".ToUInt64Nullable());

            Assert.Null("invalid".ToUInt64Nullable());
            Assert.Null("18446744073709551616".ToUInt64Nullable());
        }

        [Fact]
        public void ToFloat()
        {
            string? nullString = null;
            Assert.Equal(0f, nullString.ToFloat());
            Assert.Equal(42f, nullString.ToFloat(42));

            Assert.Equal(0f, "".ToFloat());
            Assert.Equal(42f, "".ToFloat(42));

            Assert.Equal(123.45f, "123.45".ToFloat());
            Assert.Equal(-123.45f, "-123.45".ToFloat());
            Assert.Equal(0f, "0".ToFloat());

            Assert.Equal(1.23f, "1.23e0".ToFloat(), 2);

            Assert.Equal(0f, "invalid".ToFloat());
            Assert.Equal(42f, "invalid".ToFloat(42));
        }

        [Fact]
        public void ToFloatNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToFloatNullable());
            Assert.Null("".ToFloatNullable());

            Assert.Equal(123.45f, "123.45".ToFloatNullable());
            Assert.Equal(-123.45f, "-123.45".ToFloatNullable());

            Assert.Null("invalid".ToFloatNullable());
        }

        [Fact]
        public void ToDouble()
        {
            string? nullString = null;
            Assert.Equal(0d, nullString.ToDouble());
            Assert.Equal(42d, nullString.ToDouble(42));

            Assert.Equal(0d, "".ToDouble());
            Assert.Equal(42d, "".ToDouble(42));

            Assert.Equal(123.456789, "123.456789".ToDouble());
            Assert.Equal(-123.456789, "-123.456789".ToDouble());
            Assert.Equal(0d, "0".ToDouble());

            Assert.Equal(1.23e10, "1.23e10".ToDouble(), 5);

            Assert.Equal(0d, "invalid".ToDouble());
            Assert.Equal(42d, "invalid".ToDouble(42));
        }

        [Fact]
        public void ToDoubleNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToDoubleNullable());
            Assert.Null("".ToDoubleNullable());

            Assert.Equal(123.456789, "123.456789".ToDoubleNullable());
            Assert.Equal(-123.456789, "-123.456789".ToDoubleNullable());

            Assert.Null("invalid".ToDoubleNullable());
        }

        [Fact]
        public void ToDecimal()
        {
            string? nullString = null;
            Assert.Equal(0m, nullString.ToDecimal());
            Assert.Equal(42m, nullString.ToDecimal(42));

            Assert.Equal(0m, "".ToDecimal());
            Assert.Equal(42m, "".ToDecimal(42));

            Assert.Equal(123.456789m, "123.456789".ToDecimal());
            Assert.Equal(-123.456789m, "-123.456789".ToDecimal());
            Assert.Equal(0m, "0".ToDecimal());

            Assert.Equal(0m, "invalid".ToDecimal());
            Assert.Equal(42m, "invalid".ToDecimal(42));
        }

        [Fact]
        public void ToDecimalNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToDecimalNullable());
            Assert.Null("".ToDecimalNullable());

            Assert.Equal(123.456789m, "123.456789".ToDecimalNullable());
            Assert.Equal(-123.456789m, "-123.456789".ToDecimalNullable());

            Assert.Null("invalid".ToDecimalNullable());
        }

        [Fact]
        public void ToDateTime()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToDateTime());
            Assert.Equal(default, "".ToDateTime());

            var result = "2023-01-15".ToDateTime();
            Assert.Equal(2023, result.Year);
            Assert.Equal(1, result.Month);
            Assert.Equal(15, result.Day);

            result = "2023-01-15T10:30:45".ToDateTime();
            Assert.Equal(10, result.Hour);
            Assert.Equal(30, result.Minute);

            var customDefault = new DateTime(2000, 1, 1);
            var customResult = "invalid".ToDateTime(customDefault);
            Assert.Equal(customDefault, customResult);

            Assert.Equal(default, "invalid".ToDateTime());
        }

        [Fact]
        public void ToDateTimeNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToDateTimeNullable());
            Assert.Null("".ToDateTimeNullable());

            var result = "2023-01-15".ToDateTimeNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(2023, result.Value.Year);

            Assert.Null("invalid".ToDateTimeNullable());
        }

        [Fact]
        public void ToDateTimeOffset()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToDateTimeOffset());
            Assert.Equal(default, "".ToDateTimeOffset());

            var result = "2023-01-15T10:30:45+00:00".ToDateTimeOffset();
            Assert.Equal(2023, result.Year);

            Assert.Equal(default, "invalid".ToDateTimeOffset());
        }

        [Fact]
        public void ToDateTimeOffsetNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToDateTimeOffsetNullable());
            Assert.Null("".ToDateTimeOffsetNullable());

            var result = "2023-01-15T10:30:45+00:00".ToDateTimeOffsetNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(2023, result.Value.Year);

            Assert.Null("invalid".ToDateTimeOffsetNullable());
        }

        [Fact]
        public void ToTimeSpan()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToTimeSpan());
            Assert.Equal(default, "".ToTimeSpan());

            var result = "10:30:45".ToTimeSpan();
            Assert.Equal(10, result.Hours);
            Assert.Equal(30, result.Minutes);
            Assert.Equal(45, result.Seconds);

            result = "1.10:30:45".ToTimeSpan();
            Assert.Equal(1, result.Days);

            Assert.Equal(default, "invalid".ToTimeSpan());
        }

        [Fact]
        public void ToTimeSpanNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToTimeSpanNullable());
            Assert.Null("".ToTimeSpanNullable());

            var result = "10:30:45".ToTimeSpanNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(10, result.Value.Hours);

            Assert.Null("invalid".ToTimeSpanNullable());
        }

#if !NETSTANDARD2_0
        [Fact]
        public void ToDateOnly()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToDateOnly());
            Assert.Equal(default, "".ToDateOnly());

            var result = "2023-01-15".ToDateOnly();
            Assert.Equal(2023, result.Year);
            Assert.Equal(1, result.Month);
            Assert.Equal(15, result.Day);

            Assert.Equal(default, "invalid".ToDateOnly());
        }

        [Fact]
        public void ToDateOnlyNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToDateOnlyNullable());
            Assert.Null("".ToDateOnlyNullable());

            var result = "2023-01-15".ToDateOnlyNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(2023, result.Value.Year);

            Assert.Null("invalid".ToDateOnlyNullable());
        }

        [Fact]
        public void ToTimeOnly()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToTimeOnly());
            Assert.Equal(default, "".ToTimeOnly());

            var result = "10:30:45".ToTimeOnly();
            Assert.Equal(10, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);

            Assert.Equal(default, "invalid".ToTimeOnly());
        }

        [Fact]
        public void ToTimeOnlyNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToTimeOnlyNullable());
            Assert.Null("".ToTimeOnlyNullable());

            var result = "10:30:45".ToTimeOnlyNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(10, result.Value.Hour);

            Assert.Null("invalid".ToTimeOnlyNullable());
        }
#endif

        [Fact]
        public void ToGuid()
        {
            string? nullString = null;
            Assert.Equal(default, nullString.ToGuid());
            Assert.Equal(default, "".ToGuid());

            var guidString = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
            var result = guidString.ToGuid();
            Assert.Equal(new Guid(guidString), result);

            var guidStringWithBraces = "{f47ac10b-58cc-4372-a567-0e02b2c3d479}";
            result = guidStringWithBraces.ToGuid();
            Assert.Equal(new Guid(guidStringWithBraces), result);

            Assert.Equal(default, "invalid".ToGuid());

            var customDefault = Guid.NewGuid();
            result = "invalid".ToGuid(customDefault);
            Assert.Equal(customDefault, result);
        }

        [Fact]
        public void ToGuidNullable()
        {
            string? nullString = null;
            Assert.Null(nullString.ToGuidNullable());
            Assert.Null("".ToGuidNullable());

            var guidString = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
            var result = guidString.ToGuidNullable();
            _ = Assert.NotNull(result);
            Assert.Equal(new Guid(guidString), result.Value);

            Assert.Null("invalid".ToGuidNullable());
        }

        [Fact]
        public void MatchWildcard()
        {
            _ = Assert.Throws<ArgumentException>(() => "test".MatchWildcard(null));
            _ = Assert.Throws<ArgumentException>(() => "test".MatchWildcard(""));

            string? nullString = null;
            Assert.True(nullString.MatchWildcard("*"));
            Assert.False(nullString.MatchWildcard("test"));

            Assert.True("".MatchWildcard("*"));

            Assert.True("test".MatchWildcard("test"));
            Assert.False("test".MatchWildcard("hello"));

            Assert.True("test123".MatchWildcard("test*"));
            Assert.False("hello123".MatchWildcard("test*"));

            Assert.True("mytest".MatchWildcard("*test"));
            Assert.False("mytest".MatchWildcard("*hello"));

            Assert.True("test123end".MatchWildcard("test*end"));
            Assert.True("testend".MatchWildcard("test*end"));
            Assert.True("testingend".MatchWildcard("test*end"));

            Assert.True("test123end456".MatchWildcard("test*end*"));
            Assert.True("abcdefg".MatchWildcard("a*c*g"));

            Assert.True("anything".MatchWildcard("*"));
            Assert.True("".MatchWildcard("*"));

            Assert.True("test123".MatchWildcard("test?", '?'));
            Assert.True("test123".MatchWildcard("test%", '%'));
            Assert.True("test".MatchWildcard("test?", '?'));

            Assert.False("Test".MatchWildcard("test"));
            Assert.False("TEST".MatchWildcard("test*"));

            Assert.True("test-123.456".MatchWildcard("test*"));
            Assert.True("file.txt".MatchWildcard("*.txt"));
        }

        [Fact]
        public void MatchWildcard_MatchesRegex()
        {
            //every short text and pattern against the regex the pattern means, the wildcard matching zero or more characters
            var texts = new List<string>() { "" };
            for (var length = 1; length <= 5; length++)
                texts.AddRange(Combinations("ab", length));
            var patterns = new List<string>();
            for (var length = 1; length <= 4; length++)
                patterns.AddRange(Combinations("ab*", length));

            var failures = new List<string>();
            foreach (var pattern in patterns)
            {
                var regex = new System.Text.RegularExpressions.Regex("^" + string.Concat(pattern.Select(c => c == '*' ? ".*" : System.Text.RegularExpressions.Regex.Escape(c.ToString()))) + "$");
                foreach (var text in texts)
                {
                    if (text.MatchWildcard(pattern) != regex.IsMatch(text))
                        failures.Add($"\"{text}\" \"{pattern}\"");
                }
            }
            Assert.True(failures.Count == 0, $"{failures.Count} wrong: {string.Join(", ", failures.Take(10))}");

            static IEnumerable<string> Combinations(string chars, int length)
                => length == 0 ? [""] : Combinations(chars, length - 1).SelectMany(x => chars.Select(c => x + c));
        }

        [Fact]
        public void Join_TruncatesTheLongerFirst()
        {
            Assert.Equal("abcdef-xy", StringExtensions.Join(9, "-", "abcdefghij", "xy", out var truncated));
            Assert.True(truncated);
            Assert.Equal("xy-abcdef", StringExtensions.Join(9, "-", "xy", "abcdefghij"));
            Assert.Equal("abc-xyz", StringExtensions.Join(7, "-", "abcde", "xyzw"));
            Assert.Equal("abc", "abcdef".Truncate(3));
        }
    }
}
