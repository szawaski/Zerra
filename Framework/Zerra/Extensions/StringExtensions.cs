﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

public static class StringExtensions
{
    public static string Truncate(this string it, int maxLength)
    {
        if (it == null)
            throw new ArgumentNullException(nameof(it));
        if (it.Length < maxLength)
            return it;
        return it.Substring(0, maxLength);
    }

    public static bool ToBoolean(this string obj, bool defaultValue = default)
    {
        if (obj == null)
            return defaultValue;
        if (obj == String.Empty)
            return defaultValue;

        if (obj == "1")
            return true;
        if (obj == "0")
            return false;
        if (obj == "-1")
            return false;

        if (Boolean.TryParse(obj, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static bool? ToBooleanNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Boolean.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static byte ToByte(this string it, byte defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Byte.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static byte? ToByteNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Byte.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static short ToInt16(this string it, short defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static short? ToInt16Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static ushort ToUInt16(this string it, ushort defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static ushort? ToUInt16Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static int ToInt32(this string it, int defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static int? ToInt32Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static uint ToUInt32(this string it, uint defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static uint? ToUInt32Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static long ToInt64(this string it, long defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static long? ToInt64Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static ulong ToUInt64(this string it, ulong defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static ulong? ToUInt64Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static float ToFloat(this string it, float defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Single.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static float? ToFloatNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Single.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static double ToDouble(this string it, double defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Double.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static double? ToDoubleNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static decimal ToDecimal(this string it, decimal defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Decimal.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static decimal? ToDecimalNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Decimal.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static DateTime ToDateTime(this string it, DateTime defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (DateTime.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }
    public static DateTime? ToDateTimeNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (DateTime.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static DateTimeOffset ToDateTimeOffset(this string it, DateTimeOffset defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (DateTimeOffset.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }
    public static DateTimeOffset? ToDateTimeOffsetNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (DateTimeOffset.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static TimeSpan ToTimeSpan(this string it, TimeSpan defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (TimeSpan.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }
    public static TimeSpan? ToTimeSpanNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (TimeSpan.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static Guid ToGuid(this string it, Guid defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Guid.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static Guid? ToGuidNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Guid.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

#if !NET5_0_OR_GREATER
    public static string[] Split(this string it, string seperator)
    {
        return it.Split(new string[] { seperator }, StringSplitOptions.None);
    }
    public static string[] Split(this string it, string seperator, StringSplitOptions options)
    {
        return it.Split(new string[] { seperator }, options);
    }
#endif

    public static unsafe bool MatchWildcard(this string it, string pattern, char wildcard = '*')
    {
        if (String.IsNullOrEmpty(pattern))
            throw new ArgumentException(nameof(pattern));

        fixed (char* pPattern = pattern)
        {
            if (it == null || it.Length == 0)
            {
                if (pattern.Length == 1 && pPattern[0] == wildcard)
                    return true;
                else
                    return false;
            }

            fixed (char* pText = it)
            {
                var i = 0;
                var iPattern = 0;
                int? iWildcard = null;

                var charPattern = pPattern[iPattern];
                for (; i < it.Length; i++)
                {
                    var charText = it[i];

                    if (charPattern == wildcard)
                    {
                        iWildcard = iPattern;
                        if (iPattern == pattern.Length - 1)
                            return true;
                        iPattern++;
                        charPattern = pPattern[iPattern];
                    }

                    if (iWildcard.HasValue)
                    {
                        if (charPattern != charText)
                        {
                            if (iPattern != iWildcard.Value + 1)
                            {
                                iPattern = iWildcard.Value + 1;
                                charPattern = pPattern[iPattern];
                            }
                        }
                        else
                        {
                            if (iPattern == pattern.Length - 1)
                                break;
                            iPattern++;
                            charPattern = pPattern[iPattern];
                        }
                    }
                    else
                    {
                        if (charPattern != charText)
                            return false;
                        if (iPattern == pattern.Length - 1)
                            break;
                        iPattern++;
                        charPattern = pPattern[iPattern];
                    }
                }

                if (iPattern != pattern.Length - 1)
                    return false;
                if (i != it.Length - 1)
                    return false;
                return true;
            }
        }
    }
}