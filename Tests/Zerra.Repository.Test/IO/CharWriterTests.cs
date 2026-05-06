// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Globalization;
using Xunit;
using Zerra.Repository.IO;

namespace Zerra.Repository.Test.IO
{
    public sealed class CharWriterTests
    {
        // ── Numerics ──────────────────────────────────────────────────────────

        [Fact]
        public void Write_Byte_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write((byte)255);
                Assert.Equal("255", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_SByte_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write((sbyte)-128);
                Assert.Equal("-128", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Short_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write((short)-32768);
                Assert.Equal("-32768", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_UShort_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write((ushort)65535);
                Assert.Equal("65535", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Int_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(int.MinValue);
                Assert.Equal(int.MinValue.ToString(), writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_UInt_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(uint.MaxValue);
                Assert.Equal(uint.MaxValue.ToString(), writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Long_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(long.MinValue);
                Assert.Equal(long.MinValue.ToString(), writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_ULong_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(ulong.MaxValue);
                Assert.Equal(ulong.MaxValue.ToString(), writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Float_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(3.14f);
                var result = writer.ToString();
                Assert.Equal(3.14f, float.Parse(result, CultureInfo.InvariantCulture));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Double_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(3.141592653589793);
                var result = writer.ToString();
                Assert.Equal(3.141592653589793, double.Parse(result, CultureInfo.InvariantCulture));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_Decimal_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(123456789.987654321m);
                var result = writer.ToString();
                Assert.Equal(123456789.987654321m, decimal.Parse(result, CultureInfo.InvariantCulture));
            }
            finally { writer.Dispose(); }
        }

        // ── Char / String / Span ──────────────────────────────────────────────

        [Fact]
        public void Write_Char_AppendsCharacter()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write('A');
                writer.Write('!');
                Assert.Equal("A!", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_String_AppendsString()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write("hello");
                writer.Write(" world");
                Assert.Equal("hello world", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_NullString_IsNoOp()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write((string?)null);
                Assert.Equal(0, writer.Length);
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_EmptyString_IsNoOp()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(string.Empty);
                Assert.Equal(0, writer.Length);
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_CharArray_AppendsRange()
        {
            var chars = new char[] { 'f', 'o', 'o', 'b', 'a', 'r' };
            var writer = new CharWriter();
            try
            {
                writer.Write(chars, 0, chars.Length);
                Assert.Equal("foobar", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_ReadOnlySpan_AppendsChars()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write("abc".AsSpan());
                writer.Write("xyz".AsSpan());
                Assert.Equal("abcxyz", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_EmptySpan_IsNoOp()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(ReadOnlySpan<char>.Empty);
                Assert.Equal(0, writer.Length);
            }
            finally { writer.Dispose(); }
        }

        // ── Guid ──────────────────────────────────────────────────────────────

        [Fact]
        public void Write_Guid_RoundTrips()
        {
            var value = Guid.NewGuid();
            var writer = new CharWriter();
            try
            {
                writer.Write(value);
                var result = writer.ToString();
                Assert.Equal(value, Guid.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_GuidEmpty_RoundTrips()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(Guid.Empty);
                var result = writer.ToString();
                Assert.Equal(Guid.Empty, Guid.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        // ── Byte[] Hex ────────────────────────────────────────────────────────

        [Fact]
        public void Write_ByteArray_Hex_AllZeros()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(new byte[] { 0x00, 0x00 }, CharWriter.ByteFormat.Hex);
                Assert.Equal("0000", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_ByteArray_Hex_AllOnes()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(new byte[] { 0xFF, 0xFF }, CharWriter.ByteFormat.Hex);
                Assert.Equal("ffff", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_ByteArray_Hex_KnownValues()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(new byte[] { 0x0A, 0x1B, 0xCD, 0xEF }, CharWriter.ByteFormat.Hex);
                Assert.Equal("0a1bcdef", writer.ToString());
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void Write_ByteArray_Hex_RoundTripsViaConvert()
        {
            var original = new byte[] { 1, 2, 3, 255, 128, 64 };
            var writer = new CharWriter();
            try
            {
                writer.Write(original, CharWriter.ByteFormat.Hex);
                var hex = writer.ToString();
                var parsed = Convert.FromHexString(hex);
                Assert.Equal(original, parsed);
            }
            finally { writer.Dispose(); }
        }

        // ── Write(in CharWriter) ──────────────────────────────────────────────

        [Fact]
        public void Write_InCharWriter_AppendsCopiedContent()
        {
            var inner = new CharWriter();
            var outer = new CharWriter();
            try
            {
                inner.Write("inner content");
                outer.Write("prefix-");
                outer.Write(in inner);
                Assert.Equal("prefix-inner content", outer.ToString());
            }
            finally
            {
                inner.Dispose();
                outer.Dispose();
            }
        }

        [Fact]
        public void Write_InCharWriter_EmptySource_IsNoOp()
        {
            var inner = new CharWriter();
            var outer = new CharWriter();
            try
            {
                outer.Write("data");
                outer.Write(in inner);
                Assert.Equal("data", outer.ToString());
            }
            finally
            {
                inner.Dispose();
                outer.Dispose();
            }
        }

        // ── Buffer growth ─────────────────────────────────────────────────────

        [Fact]
        public void Write_BeyondInitialBuffer_GrowsCorrectly()
        {
            var writer = new CharWriter(4);
            try
            {
                var longString = new string('x', 2000);
                writer.Write(longString);
                Assert.Equal(2000, writer.Length);
                Assert.Equal(longString, writer.ToString());
            }
            finally { writer.Dispose(); }
        }

#if NET6_0_OR_GREATER
        // ── DateOnly ──────────────────────────────────────────────────────────

        [Fact]
        public void DateOnly_ISO8601_RoundTrips()
        {
            var value = new DateOnly(2024, 7, 4);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                Assert.Equal(value, DateOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void DateOnly_MsSql_RoundTrips()
        {
            var value = new DateOnly(2000, 1, 1);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MsSql);
                var result = writer.ToString();
                Assert.Equal(value, DateOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void DateOnly_MySql_RoundTrips()
        {
            var value = new DateOnly(1999, 12, 31);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MySql);
                // MySql DateOnly appends a trailing space — trim before parsing
                var result = writer.ToString().TrimEnd();
                Assert.Equal(value, DateOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void DateOnly_PostgreSql_RoundTrips()
        {
            var value = new DateOnly(2024, 2, 29);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.PostgreSql);
                var result = writer.ToString();
                Assert.Equal(value, DateOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        // ── TimeOnly ──────────────────────────────────────────────────────────

        [Fact]
        public void TimeOnly_ISO8601_RoundTrips()
        {
            // ISO8601 keeps all 7 fractional digits (no trailing-zero trim)
            var value = new TimeOnly(10, 30, 45, 123, 456);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void TimeOnly_ISO8601_NoFraction_RoundTrips()
        {
            var value = new TimeOnly(8, 0, 0);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void TimeOnly_MsSql_RoundTrips()
        {
            var value = new TimeOnly(14, 55, 33).Add(TimeSpan.FromTicks(1234567));
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MsSql);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void TimeOnly_MySql_RoundTrips()
        {
            // MySql truncates to microseconds; use exact microsecond tick value
            var value = new TimeOnly(9, 20, 10).Add(TimeSpan.FromTicks(1234560));
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MySql);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void TimeOnly_PostgreSql_RoundTrips()
        {
            var value = new TimeOnly(23, 59, 59).Add(TimeSpan.FromTicks(9876540));
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.PostgreSql);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }

        [Fact]
        public void TimeOnly_MySql_NoFraction_RoundTrips()
        {
            var value = new TimeOnly(0, 0, 0);
            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MySql);
                var result = writer.ToString();
                Assert.Equal(value, TimeOnly.Parse(result));
            }
            finally { writer.Dispose(); }
        }
#endif

        // ── DateTime ISO8601 ──────────────────────────────────────────────────

        [Fact]
        public void DateTime_ISO8601_Utc_RoundTrips()
        {
            var value = new DateTime(2024, 3, 15, 10, 30, 45, 123, DateTimeKind.Utc).AddTicks(4567);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
                Assert.Equal(DateTimeKind.Utc, parsed.Kind);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_ISO8601_Local_RoundTrips()
        {
            var value = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Local);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
                Assert.Equal(DateTimeKind.Local, parsed.Kind);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_ISO8601_Unspecified_RoundTrips()
        {
            var value = new DateTime(2024, 11, 20, 23, 59, 59, DateTimeKind.Unspecified);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
                Assert.Equal(DateTimeKind.Unspecified, parsed.Kind);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_ISO8601_NoFractionalSeconds_RoundTrips()
        {
            var value = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTime MsSql ────────────────────────────────────────────────────

        [Fact]
        public void DateTime_MsSql_RoundTrips()
        {
            // MsSql truncates to milliseconds and converts to UTC
            var value = new DateTime(2024, 3, 15, 10, 30, 45, 123, DateTimeKind.Utc);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MsSql);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_MsSql_NoFraction_RoundTrips()
        {
            var value = new DateTime(2024, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MsSql);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTime MySql ────────────────────────────────────────────────────

        [Fact]
        public void DateTime_MySql_RoundTrips()
        {
            // MySql truncates to microseconds (6 fractional digits) and converts to UTC
            var value = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(1234560); // exact microseconds

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MySql);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_MySql_NoFraction_RoundTrips()
        {
            var value = new DateTime(2024, 7, 4, 12, 0, 0, 0, DateTimeKind.Utc);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MySql);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTime PostgreSql ───────────────────────────────────────────────

        [Fact]
        public void DateTime_PostgreSql_RoundTrips()
        {
            // PostgreSql truncates to microseconds and converts to UTC
            var value = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(9876540); // exact microseconds

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.PostgreSql);
                var result = writer.ToString();
                var parsed = DateTime.Parse(result, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTimeOffset ISO8601 ────────────────────────────────────────────

        [Fact]
        public void DateTimeOffset_ISO8601_Utc_RoundTrips()
        {
            var value = new DateTimeOffset(2024, 3, 15, 10, 30, 45, 123, TimeSpan.Zero).AddTicks(4567);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTimeOffset_ISO8601_WithOffset_RoundTrips()
        {
            var value = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(5));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTimeOffset_ISO8601_NegativeOffset_RoundTrips()
        {
            var value = new DateTimeOffset(2024, 1, 10, 5, 45, 30, TimeSpan.FromHours(-7));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTimeOffset_ISO8601_NoFractionalSeconds_RoundTrips()
        {
            var value = new DateTimeOffset(2024, 8, 20, 0, 0, 0, TimeSpan.Zero);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTimeOffset MsSql ──────────────────────────────────────────────

        [Fact]
        public void DateTimeOffset_MsSql_RoundTrips()
        {
            // MsSql DateTimeOffset keeps offset, truncates to milliseconds
            var value = new DateTimeOffset(2024, 3, 15, 10, 30, 45, 123, TimeSpan.FromHours(2));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.MsSql);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── DateTimeOffset PostgreSql ─────────────────────────────────────────

        [Fact]
        public void DateTimeOffset_PostgreSql_RoundTrips()
        {
            // PostgreSql DateTimeOffset keeps offset, truncates to microseconds
            var value = new DateTimeOffset(2024, 5, 5, 9, 15, 30, TimeSpan.FromHours(3)).AddTicks(1234560);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.DateTimeFormat.PostgreSql);
                var result = writer.ToString();
                var parsed = DateTimeOffset.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── TimeSpan ISO8601 / MsSql ──────────────────────────────────────────

        [Fact]
        public void TimeSpan_ISO8601_Positive_RoundTrips()
        {
            var value = new TimeSpan(3, 14, 25, 36, 0).Add(TimeSpan.FromTicks(1234567));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_ISO8601_Negative_RoundTrips()
        {
            var value = new TimeSpan(-2, -5, -30, -10, 0).Add(TimeSpan.FromTicks(-9876543));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_ISO8601_NoDays_RoundTrips()
        {
            var value = new TimeSpan(0, 10, 20, 30, 0).Add(TimeSpan.FromTicks(1000000));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_ISO8601_NoFraction_RoundTrips()
        {
            var value = new TimeSpan(1, 2, 3, 4, 0);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.ISO8601);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_MsSql_RoundTrips()
        {
            var value = new TimeSpan(1, 8, 30, 45, 0).Add(TimeSpan.FromTicks(1234567));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MsSql);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── TimeSpan MySql / PostgreSql ───────────────────────────────────────

        [Fact]
        public void TimeSpan_MySql_RoundTrips()
        {
            // MySql truncates to microseconds (6 digits), so use exact microsecond value
            var value = new TimeSpan(0, 5, 45, 10, 0).Add(TimeSpan.FromTicks(1234560));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MySql);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_PostgreSql_RoundTrips()
        {
            var value = new TimeSpan(2, 11, 22, 33, 0).Add(TimeSpan.FromTicks(9876540));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.PostgreSql);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_MySql_Negative_RoundTrips()
        {
            var value = new TimeSpan(-1, -3, -15, -0, 0).Add(TimeSpan.FromTicks(-1234560));

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.MySql);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void TimeSpan_PostgreSql_NoFraction_RoundTrips()
        {
            var value = new TimeSpan(0, 1, 2, 3, 0);

            var writer = new CharWriter();
            try
            {
                writer.Write(value, CharWriter.TimeFormat.PostgreSql);
                var result = writer.ToString();
                var parsed = TimeSpan.Parse(result);
                Assert.Equal(value, parsed);
            }
            finally
            {
                writer.Dispose();
            }
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var writer = new CharWriter();
            writer.Write(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), CharWriter.DateTimeFormat.ISO8601);
            writer.Dispose();
            writer.Dispose(); // must not throw
        }

        [Fact]
        public void Clear_ResetsPositionAndAllowsReuse()
        {
            var writer = new CharWriter();
            try
            {
                writer.Write(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), CharWriter.DateTimeFormat.ISO8601);
                Assert.True(writer.Length > 0);
                writer.Clear();
                Assert.Equal(0, writer.Length);
                Assert.Equal(string.Empty, writer.ToString());
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
