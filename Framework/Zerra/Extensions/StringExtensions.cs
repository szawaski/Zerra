// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security;

public static class StringExtensions
{
    public static string Truncate(this string it, int maxLength)
    {
        if (it == null || it.Length < maxLength)
            return it;
        return it.Substring(0, maxLength);
    }

    public static bool ToBoolean(this string obj)
    {
        if (obj == null)
            return false;
        if (obj == String.Empty)
            return false;
        if (obj == "1")
            return true;
        if (obj == "0")
            return false;
        if (obj == "-1")
            return false;
        if (Boolean.TryParse(obj, out var tryvalue))
            return tryvalue;
        else
            return false;
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

    public static byte ToByte(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (byte)tryvalue;
        else
            return default;
    }
    public static byte? ToByteNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (byte)tryvalue;
        else
            return null;
    }

    public static short ToInt16(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (short)tryvalue;
        else
            return default;
    }
    public static short? ToInt16Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (short)tryvalue;
        else
            return null;
    }

    public static ushort ToUInt16(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (ushort)tryvalue;
        else
            return default;
    }
    public static ushort? ToUInt16Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (ushort)tryvalue;
        else
            return null;
    }

    public static int ToInt32(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (int)tryvalue;
        else
            return default;
    }
    public static int? ToInt32Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (int)tryvalue;
        else
            return null;
    }

    public static uint ToUInt32(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (uint)tryvalue;
        else
            return default;
    }
    public static uint? ToUInt32Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (uint)tryvalue;
        else
            return null;
    }

    public static long ToInt64(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (long)tryvalue;
        else
            return default;
    }
    public static long? ToInt64Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (long)tryvalue;
        else
            return null;
    }

    public static ulong ToUInt64(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (ulong)tryvalue;
        else
            return default;
    }
    public static ulong? ToUInt64Nullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (ulong)tryvalue;
        else
            return null;
    }

    public static decimal ToDecimal(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Decimal.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return default;
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

    public static float ToFloat(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return (float)tryvalue;
        else
            return default;
    }
    public static float? ToFloatNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return (float)tryvalue;
        else
            return null;
    }

    public static double ToDouble(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (Double.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return default;
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

    public static string ToStringNullable(this string it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;
        return it;
    }

    //private static readonly DateTimeOffset minSqlDateTimeOffset = new DateTimeOffset(1975, 1, 1, 0, 0, 0, TimeSpan.Zero);
    //private static readonly DateTime minSqlDateTime = new DateTime(1975, 1, 1);

    public static DateTime ToDateTime(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (DateTime.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return default;
        //return minSqlDateTime;
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

    public static DateTimeOffset ToDateTimeOffset(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (DateTimeOffset.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return default;
        //return minSqlDateTimeOffset;
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

    public static TimeSpan ToTimeSpan(this string it, string formatExpression) { return ToTimeSpan(it, formatExpression, false); }
    public static TimeSpan ToTimeSpan(this string it, string formatExpression, bool rightBased)
    {
        var splits = new char[] { ':', '.' };
        var formats = formatExpression.ToLower().Split(splits);
        var times = it.Split(splits);
        if (formats.Length < times.Length)
            throw new ArgumentException("Incompatible format expression for input");

        var days = 0;
        var hours = 0;
        var minutes = 0;
        var seconds = 0;
        var milliseconds = 0;

        for (var i = 0; i < formats.Length && i < times.Length; i++)
        {
            var format = rightBased ? formats[formats.Length - 1 - i] : formats[i];
            var value = times[i].ToInt32();

            if (format.Contains("d"))
                days = value;
            else if (format.Contains("h"))
                hours = value;
            else if (format.Contains("m"))
                minutes = value;
            else if (format.Contains("s"))
                seconds = value;
            else if (format.Contains("t"))
                milliseconds = value;
        }

        return new TimeSpan(days, hours, minutes, seconds, milliseconds);
    }

    public static TimeSpan ToTimeSpan(this string it)
    {
        if (it == null)
            return default;
        if (it == String.Empty)
            return default;
        if (TimeSpan.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return default;
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

    public static Guid ToGuid(this string it)
    {
        if (it == null)
            return Guid.Empty;
        if (it == String.Empty)
            return Guid.Empty;
        if (Guid.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return default;
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

    public static SecureString ToSecure(this string it)
    {
        var secure = new SecureString();
        foreach (var c in it)
            secure.AppendChar(c);
        return secure;
    }
    public static string ToUnsecure(this SecureString it)
    {
        if (it == null)
            return null;

        var valuePtr = IntPtr.Zero;
        try
        {
            valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(it);
            return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }

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