// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Json
{
    internal static partial class Utf8Helper
    {
        private const byte dashByte = (byte)'-';
        private const byte tUpperByte = (byte)'T';
        private const byte colonByte = (byte)':';
        private const byte dotByte = (byte)'.';
        private const byte plusByte = (byte)'+';
        private const byte minusByte = (byte)'-';
        private const byte zUpperByte = (byte)'Z';

        private static int[] daysToMonth365 = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];
        private static int[] daysToMonth366 = [0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366];

        public unsafe static bool TryParse(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            var length = source.Length;

            if (length > 42 || length < 10)
            {
                value = default;
                return false;
            }

            fixed (byte* pSource = source)
            {
                uint d1 = pSource[0] - 48u;
                uint d2 = pSource[1] - 48u;
                uint d3 = pSource[2] - 48u;
                uint d4 = pSource[3] - 48u;
                if (d1 > 9 || d2 > 9 || d3 > 9 || d4 > 9)
                {
                    value = default;
                    return false;
                }
                var year = (int)(d1 * 1000 + d2 * 100 + d3 * 10 + d4);

                if (pSource[4] != dashByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[5] - 48u;
                d2 = pSource[6] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var month = (int)(d1 * 10 + d2);

                if (pSource[7] != dashByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[8] - 48u;
                d2 = pSource[9] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var day = (int)(d1 * 10 + d2);

                if (length == 10)
                {
                    return TryCreateDateTimeOffset(year, month, day, 0, 0, 0, 0, 0, 0, out value);
                }
                if (length < 19)
                {
                    value = default;
                    return false;
                }

                if (pSource[10] != tUpperByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[11] - 48u;
                d2 = pSource[12] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var hours = (int)(d1 * 10 + d2);

                if (pSource[13] != colonByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[14] - 48u;
                d2 = pSource[15] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var minutes = (int)(d1 * 10 + d2);

                if (pSource[16] != colonByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[17] - 48u;
                d2 = pSource[18] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var seconds = (int)(d1 * 10 + d2);

                if (length == 19)
                {
                    return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, 0, 0, 0, out value);
                }
                if (length < 20)
                {
                    value = default;
                    return false;
                }

                var afterTime = pSource[19];
                var fractionTicks = 0;
                var i = 20;
                var offsetNegative = false;
                if (afterTime == dotByte)
                {
                    if (length == 20 || (uint)(source[20] - '0') > 9)
                    {
                        value = default;
                        return false;
                    }
                    for (; i < 27 && i < length; i++)
                    {
                        d1 = source[i];
                        if (d1 == zUpperByte)
                        {
                            if (length != i + 1)
                            {
                                value = default;
                                return false;
                            }
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                        }
                        if (d1 == plusByte)
                        {
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            i++;
                            goto timezone;
                        }
                        if (d1 == minusByte)
                        {
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            i++;
                            offsetNegative = true;
                            goto timezone;
                        }

                        d1 -= 48u;
                        if (d1 > 9)
                        {
                            value = default;
                            return false;
                        }
                        fractionTicks = fractionTicks * 10 + (int)d1;
                    }
                    while (i < length && (uint)(source[i] - '0') <= 9)
                        i++;
                    if (length == i)
                    {
                        for (; i < 27; i++)
                            fractionTicks *= 10;
                        return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                    }

                    afterTime = pSource[i++];
                }

                if (afterTime == zUpperByte)
                {
                    if (length != i)
                    {
                        value = default;
                        return false;
                    }
                    return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                }
                else if (afterTime == plusByte)
                {
                    goto timezone;
                }
                else if (afterTime == minusByte)
                {
                    offsetNegative = true;
                    goto timezone;
                }
                else
                {
                    value = default;
                    return false;
                }

            timezone:

                if (length != i + 5)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[i] - 48u;
                d2 = pSource[i + 1] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                int offsetHours;
                if (offsetNegative)
                    offsetHours = (int)(-d1 * 10 - d2);
                else
                    offsetHours = (int)(d1 * 10 + d2);

                if (pSource[i + 2] != colonByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[i + 3] - 48u;
                d2 = pSource[i + 4] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                int offsetMinutes;
                if (offsetNegative)
                    offsetMinutes = (int)(-d1 * 10 - d2);
                else
                    offsetMinutes = (int)(d1 * 10 + d2);

                return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, offsetHours, offsetMinutes, out value);
            }
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out DateTime value)
        {
            if (!TryParse(source, out DateTimeOffset date))
            {
                value = default;
                return false;
            }
            value = date.UtcDateTime;
            return true;
        }

#if !NETSTANDARD2_0
        public static bool TryParse(ReadOnlySpan<byte> source, out DateOnly value)
        {
            if (!TryParse(source, out DateTime date))
            {
                value = default;
                return false;
            }
            value = DateOnly.FromDateTime(date);
            return true;
        }
#endif

        public unsafe static bool TryParse(ReadOnlySpan<char> source, out DateTimeOffset value)
        {
            var length = source.Length;

            if (length > 42 || length < 10)
            {
                value = default;
                return false;
            }

            fixed (char* pSource = source)
            {
                uint d1 = pSource[0] - 48u;
                uint d2 = pSource[1] - 48u;
                uint d3 = pSource[2] - 48u;
                uint d4 = pSource[3] - 48u;
                if (d1 > 9 || d2 > 9 || d3 > 9 || d4 > 9)
                {
                    value = default;
                    return false;
                }
                var year = (int)(d1 * 1000 + d2 * 100 + d3 * 10 + d4);

                if (pSource[4] != dashByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[5] - 48u;
                d2 = pSource[6] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var month = (int)(d1 * 10 + d2);

                if (pSource[7] != dashByte)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[8] - 48u;
                d2 = pSource[9] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var day = (int)(d1 * 10 + d2);

                if (length == 10)
                {
                    return TryCreateDateTimeOffset(year, month, day, 0, 0, 0, 0, 0, 0, out value);
                }
                if (length < 19)
                {
                    value = default;
                    return false;
                }

                if (pSource[10] != 'T')
                {
                    value = default;
                    return false;
                }

                d1 = pSource[11] - 48u;
                d2 = pSource[12] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var hours = (int)(d1 * 10 + d2);

                if (pSource[13] != ':')
                {
                    value = default;
                    return false;
                }

                d1 = pSource[14] - 48u;
                d2 = pSource[15] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var minutes = (int)(d1 * 10 + d2);

                if (pSource[16] != ':')
                {
                    value = default;
                    return false;
                }

                d1 = pSource[17] - 48u;
                d2 = pSource[18] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                var seconds = (int)(d1 * 10 + d2);

                if (length == 19)
                {
                    return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, 0, 0, 0, out value);
                }
                if (length < 20)
                {
                    value = default;
                    return false;
                }

                var afterTime = pSource[19];
                var fractionTicks = 0;
                var i = 20;
                var offsetNegative = false;
                if (afterTime == dotByte)
                {
                    if (length == 20 || (uint)(source[20] - '0') > 9)
                    {
                        value = default;
                        return false;
                    }
                    for (; i < 27 && i < length; i++)
                    {
                        d1 = source[i];
                        if (d1 == 'Z')
                        {
                            if (length != i + 1)
                            {
                                value = default;
                                return false;
                            }
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                        }
                        if (d1 == '+')
                        {
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            i++;
                            goto timezone;
                        }
                        if (d1 == '-')
                        {
                            fractionTicks *= (int)Math.Pow(10, 27 - i);
                            i++;
                            offsetNegative = true;
                            goto timezone;
                        }

                        d1 -= 48u;
                        if (d1 > 9)
                        {
                            value = default;
                            return false;
                        }
                        fractionTicks = fractionTicks * 10 + (int)d1;
                    }
                    while (i < length && (uint)(source[i] - '0') <= 9)
                        i++;
                    if (length == i)
                    {
                        for (; i < 27; i++)
                            fractionTicks *= 10;
                        return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                    }

                    afterTime = pSource[i++];
                }

                if (afterTime == 'Z')
                {
                    if (length != i)
                    {
                        value = default;
                        return false;
                    }
                    return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, 0, 0, out value);
                }
                else if (afterTime == '+')
                {
                    goto timezone;
                }
                else if (afterTime == '-')
                {
                    offsetNegative = true;
                    goto timezone;
                }
                else
                {
                    value = default;
                    return false;
                }

            timezone:

                if (length != i + 5)
                {
                    value = default;
                    return false;
                }

                d1 = pSource[i] - 48u;
                d2 = pSource[i + 1] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                int offsetHours;
                if (offsetNegative)
                    offsetHours = (int)(-d1 * 10 - d2);
                else
                    offsetHours = (int)(d1 * 10 + d2);

                if (pSource[i + 2] != ':')
                {
                    value = default;
                    return false;
                }

                d1 = pSource[i + 3] - 48u;
                d2 = pSource[i + 4] - 48u;
                if (d1 > 9 || d2 > 9)
                {
                    value = default;
                    return false;
                }
                int offsetMinutes;
                if (offsetNegative)
                    offsetMinutes = (int)(-d1 * 10 - d2);
                else
                    offsetMinutes = (int)(d1 * 10 + d2);

                return TryCreateDateTimeOffset(year, month, day, hours, minutes, seconds, fractionTicks, offsetHours, offsetMinutes, out value);
            }
        }

        public static bool TryParse(ReadOnlySpan<char> source, out DateTime value)
        {
            if (!TryParse(source, out DateTimeOffset date))
            {
                value = default;
                return false;
            }
            value = date.UtcDateTime;
            return true;
        }

#if !NETSTANDARD2_0
        public static bool TryParse(ReadOnlySpan<char> source, out DateOnly value)
        {
            if (!TryParse(source, out DateTime date))
            {
                value = default;
                return false;
            }
            value = DateOnly.FromDateTime(date);
            return true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryCreateDateTimeOffset(int year, int month, int day, int hours, int minutes, int seconds, int fractionTicks, int offsetHours, int offsetMinutes, out DateTimeOffset value)
        {
            if (year < 1 || month < 1 || month > 12 || hours > 23 || minutes > 59 || seconds > 59 || offsetMinutes > 59 || offsetMinutes < -59)
            {
                value = default;
                return false;
            }

            var days = DateTime.IsLeapYear(year) ? daysToMonth366 : daysToMonth365;
            if (day < 1 || day > days[month] - days[month - 1])
            {
                value = default;
                return false;
            }

            var offsetTicks = (offsetHours * 60L + offsetMinutes) * TimeSpan.TicksPerMinute;
            if (offsetTicks > 14 * TimeSpan.TicksPerHour || offsetTicks < -14 * TimeSpan.TicksPerHour)
            {
                value = default;
                return false;
            }

            var yearMinusOne = year - 1;
            long totalDays = (yearMinusOne * 365) + (yearMinusOne / 4) - (yearMinusOne / 100) + (yearMinusOne / 400) + days[month - 1] + day - 1;
            var ticks = (totalDays * TimeSpan.TicksPerDay) + (((hours * 3600L) + (minutes * 60L) + seconds) * TimeSpan.TicksPerSecond) + fractionTicks;

            var utcTicks = ticks - offsetTicks;
            if (utcTicks < 0 || utcTicks > DateTime.MaxValue.Ticks)
            {
                value = default;
                return false;
            }

            value = new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
            return true;
        }
    }
}