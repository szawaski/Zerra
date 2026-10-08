// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using Xunit;
using Zerra.Repository.IO;

namespace Zerra.Repository.Test.IO
{
    //each format is compared with .NET's formatting over values that reach every padding and offset case
    public sealed class CharWriterDateTests
    {
        private static readonly int[] years = [1, 9, 10, 99, 100, 999, 1000, 2024, 9999];
        private static readonly long[] fractions = [0, 1, 10, 99, 100, 1000, 9999, 10000, 12345, 100000, 1000000, 1230000, 5000000, 9999999];
        private static readonly TimeSpan[] offsets = [TimeSpan.Zero, new(5, 45, 0), new(-5, 0, 0), new(-10, 0, 0), new(14, 0, 0), new(0, -30, 0), new(-9, -30, 0), new(0, 30, 0), new(10, 0, 0)];

        private static IEnumerable<DateTime> Dates()
        {
            foreach (var year in years)
            {
                foreach (var (month, day, hour, minute, second) in new[] { (1, 1, 0, 0, 0), (9, 9, 9, 9, 9), (10, 10, 10, 10, 10), (12, 31, 23, 59, 59) })
                {
                    foreach (var fraction in fractions)
                        yield return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(fraction);
                }
            }
        }

        private delegate void WriteAction(ref CharWriter writer);
        private static string Write(WriteAction write)
        {
            var writer = new CharWriter();
            try
            {
                write(ref writer);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void DateTime_MatchesDotNet()
        {
            foreach (var value in Dates())
            {
                Assert.Equal(value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.ISO8601)));
                Assert.Equal(value.ToString("yyyy-MM-dd HH:mm:ss.FFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.MsSql)));
                Assert.Equal(value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.MySql)));
                Assert.Equal(value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.PostgreSql)));

                var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
                Assert.Equal(value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(unspecified, CharWriter.DateTimeFormat.ISO8601)));
            }

            var local = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);
            Assert.Equal(local.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(local, CharWriter.DateTimeFormat.ISO8601)));
        }

        [Fact]
        public void DateTimeOffset_MatchesDotNet()
        {
            foreach (var date in Dates())
            {
                foreach (var offset in offsets)
                {
                    var unspecified = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                    if (unspecified.Ticks - offset.Ticks < DateTime.MinValue.Ticks || unspecified.Ticks - offset.Ticks > DateTime.MaxValue.Ticks)
                        continue;
                    var value = new DateTimeOffset(unspecified, offset);

                    Assert.Equal(value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.ISO8601)));
                    Assert.Equal(value.ToString("yyyy-MM-dd HH:mm:ss.FFFzzz", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.MsSql)));
                    Assert.Equal(value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.FFFFFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.MySql)));
                    Assert.Equal(value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFzzz", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.DateTimeFormat.PostgreSql)));
                }
            }
        }

        [Fact]
        public void DateOnly_MatchesDotNet()
        {
            foreach (var date in Dates())
            {
                var value = DateOnly.FromDateTime(date);
                var expected = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                foreach (var format in new[] { CharWriter.DateTimeFormat.ISO8601, CharWriter.DateTimeFormat.MsSql, CharWriter.DateTimeFormat.MySql, CharWriter.DateTimeFormat.PostgreSql })
                    Assert.Equal(expected, Write((ref CharWriter x) => x.Write(value, format)));
            }
        }

        private static IEnumerable<TimeSpan> TimeSpans()
        {
            foreach (var days in new[] { 0, 1, 9, 10, 123 })
            {
                foreach (var (hour, minute, second) in new[] { (0, 0, 0), (9, 9, 9), (10, 10, 10), (23, 59, 59) })
                {
                    foreach (var fraction in fractions)
                    {
                        var ticks = new TimeSpan(days, hour, minute, second).Ticks + fraction;
                        yield return new TimeSpan(ticks);
                        yield return new TimeSpan(-ticks);
                    }
                }
            }
        }

        private static string SixDigits(long ticks)
        {
            var fraction = ticks % TimeSpan.TicksPerSecond / 10;
            return fraction > 0 ? "." + fraction.ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0') : "";
        }

        [Fact]
        public void TimeSpan_MatchesDotNet()
        {
            foreach (var value in TimeSpans())
            {
                var expected = value.ToString("c", CultureInfo.InvariantCulture);
                Assert.Equal(expected, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.ISO8601)));
                Assert.Equal(expected, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.MsSql)));

                var duration = value.Duration();
                var expectedSix = (value.Ticks < 0 ? "-" : "") + (duration.Days > 0 ? $"{duration.Days}." : "") + duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) + SixDigits(duration.Ticks);
                Assert.Equal(expectedSix, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.MySql)));
                Assert.Equal(expectedSix, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.PostgreSql)));
            }
        }

        [Fact]
        public void TimeOnly_MatchesDotNet()
        {
            foreach (var span in TimeSpans())
            {
                if (span.Ticks < 0 || span.Days > 0)
                    continue;
                var value = new TimeOnly(span.Ticks);

                var hasFraction = value.Ticks % TimeSpan.TicksPerSecond > 0;
                Assert.Equal(value.ToString(hasFraction ? "HH:mm:ss.fffffff" : "HH:mm:ss", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.ISO8601)));
                Assert.Equal(value.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture), Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.MsSql)));
                var expectedSix = value.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + SixDigits(value.Ticks);
                Assert.Equal(expectedSix, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.MySql)));
                Assert.Equal(expectedSix, Write((ref CharWriter x) => x.Write(value, CharWriter.TimeFormat.PostgreSql)));
            }
        }
    }
}
