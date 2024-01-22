// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

public static class StringExtensions
{
    public static string Truncate(this string? it, int maxLength)
    {
		if (it == null) throw new ArgumentNullException(nameof(it));
        if (maxLength < 0) throw new ArgumentException("Cannot be less than zero", nameof(maxLength));
		
        if (it.Length < maxLength)
            return it;
        return it.Substring(0, maxLength);
    }

    public unsafe static string Join(int maxLength, string seperator, string str1, string str2)
    {
        if (maxLength - seperator.Length < 0) throw new ArgumentException("Cannot be shorter than the seperator length", nameof(maxLength));

        var over = str1.Length + str2.Length + seperator.Length - maxLength;
        if (over <= 0)
            return String.Join(seperator, str1, str2);

        var seperatorSpan = seperator.AsSpan();
        var span1 = str1.AsSpan();
        var span2 = str2.AsSpan();

        if (span1.Length > span2.Length)
        {
            var diff = span1.Length - span2.Length;
            var subtract = over > diff ? diff : over;
            over -= subtract;
            span1 = span1.Slice(0, span1.Length - subtract);
        }
        else if (span2.Length > span1.Length)
        {
            var diff = span2.Length - span1.Length;
            var subtract = over > diff ? diff : over;
            over -= subtract;
            span2 = span2.Slice(0, span2.Length - subtract);
        }

        if (over > 0)
        {
            var split = over / 2;
            span1 = span1.Slice(0, span1.Length - split);
            span2 = span2.Slice(0, span2.Length - split - (over % 2 > 0 ? 1 : 0));
        }

        var result = new string('\0', checked(span1.Length + span2.Length + seperatorSpan.Length));
        fixed (char* resultPtr = result)
        {
            var resultSpan = new Span<char>(resultPtr, result.Length);

            span1.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span1.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span2.CopyTo(resultSpan);
        }
        return result;
    }
    public unsafe static string Join(int maxLength, string seperator, string str1, string str2, string str3)
    {
        if (maxLength - seperator.Length < 0) throw new ArgumentException("Cannot be shorter than the seperator length", nameof(maxLength));

        var over = str1.Length + str2.Length + str3.Length + seperator.Length * 2 - maxLength;
        if (over <= 0)
            return String.Join(seperator, str1, str2, str3);

        var seperatorSpan = seperator.AsSpan();
        var span1 = str1.AsSpan();
        var span2 = str2.AsSpan();
        var span3 = str3.AsSpan();

        while (over > 0)
        {
            var index = 1;
            var length = span1.Length;
            if (span2.Length >= length)
            {
                index = 2;
                length = span2.Length;
            }
            if (span3.Length >= length)
            {
                index = 3;
                length = span3.Length;
            }
            switch (index)
            {
                case 1: span1 = span1.Slice(0, span1.Length - 1); break;
                case 2: span2 = span2.Slice(0, span2.Length - 1); break;
                case 3: span3 = span3.Slice(0, span3.Length - 1); break;
            }
            over--;
        }

        var result = new string('\0', checked(span1.Length + span2.Length + span3.Length + seperatorSpan.Length * 2));
        fixed (char* resultPtr = result)
        {
            var resultSpan = new Span<char>(resultPtr, result.Length);

            span1.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span1.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span2.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span2.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span3.CopyTo(resultSpan);
        }
        return result;
    }
    public unsafe static string Join(int maxLength, string seperator, string str1, string str2, string str3, string str4)
    {
        if (maxLength - seperator.Length < 0) throw new ArgumentException("Cannot be shorter than the seperator length", nameof(maxLength));

        var over = str1.Length + str2.Length + str3.Length + str4.Length + seperator.Length * 3 - maxLength;
        if (over <= 0)
            return String.Join(seperator, str1, str2, str3, str4);

        var seperatorSpan = seperator.AsSpan();
        var span1 = str1.AsSpan();
        var span2 = str2.AsSpan();
        var span3 = str3.AsSpan();
        var span4 = str4.AsSpan();

        while (over > 0)
        {
            var index = 1;
            var length = span1.Length;
            if (span2.Length >= length)
            {
                index = 2;
                length = span2.Length;
            }
            if (span3.Length >= length)
            {
                index = 3;
                length = span3.Length;
            }
            if (span4.Length >= length)
            {
                index = 4;
                length = span4.Length;
            }
            switch (index)
            {
                case 1: span1 = span1.Slice(0, span1.Length - 1); break;
                case 2: span2 = span2.Slice(0, span2.Length - 1); break;
                case 3: span3 = span3.Slice(0, span3.Length - 1); break;
                case 4: span4 = span4.Slice(0, span4.Length - 1); break;
            }
            over--;
        }

        var result = new string('\0', checked(span1.Length + span2.Length + span3.Length + span4.Length + seperatorSpan.Length * 3));
        fixed (char* resultPtr = result)
        {
            var resultSpan = new Span<char>(resultPtr, result.Length);

            span1.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span1.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span2.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span2.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span3.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(span3.Length);

            seperatorSpan.CopyTo(resultSpan);
            resultSpan = resultSpan.Slice(seperatorSpan.Length);

            span4.CopyTo(resultSpan);
        }
        return result;
    }

    public static bool ToBoolean(this string? it, bool defaultValue = default)
    {
        if (it == null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;

        if (it == "1")
            return true;
        if (it == "0")
            return false;
        if (it == "-1")
            return false;

        if (Boolean.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }
    public static bool? ToBooleanNullable(this string? it)
    {
        if (it == null)
            return null;
        if (it == String.Empty)
            return null;

        if (it == "1")
            return true;
        if (it == "0")
            return false;
        if (it == "-1")
            return false;

        if (Boolean.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    public static byte ToByte(this string? it, byte defaultValue = default)
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
    public static byte? ToByteNullable(this string? it)
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

    public static short ToInt16(this string? it, short defaultValue = default)
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
    public static short? ToInt16Nullable(this string? it)
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

    public static ushort ToUInt16(this string? it, ushort defaultValue = default)
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
    public static ushort? ToUInt16Nullable(this string? it)
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

    public static int ToInt32(this string? it, int defaultValue = default)
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
    public static int? ToInt32Nullable(this string? it)
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

    public static uint ToUInt32(this string? it, uint defaultValue = default)
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
    public static uint? ToUInt32Nullable(this string? it)
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

    public static long ToInt64(this string? it, long defaultValue = default)
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
    public static long? ToInt64Nullable(this string? it)
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

    public static ulong ToUInt64(this string? it, ulong defaultValue = default)
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
    public static ulong? ToUInt64Nullable(this string? it)
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

    public static float ToFloat(this string? it, float defaultValue = default)
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
    public static float? ToFloatNullable(this string? it)
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

    public static double ToDouble(this string? it, double defaultValue = default)
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
    public static double? ToDoubleNullable(this string? it)
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

    public static decimal ToDecimal(this string? it, decimal defaultValue = default)
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
    public static decimal? ToDecimalNullable(this string? it)
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

    public static DateTime ToDateTime(this string? it, DateTime defaultValue = default)
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
    public static DateTime? ToDateTimeNullable(this string? it)
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

    public static DateTimeOffset ToDateTimeOffset(this string? it, DateTimeOffset defaultValue = default)
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
    public static DateTimeOffset? ToDateTimeOffsetNullable(this string? it)
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

    public static TimeSpan ToTimeSpan(this string? it, TimeSpan defaultValue = default)
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
    public static TimeSpan? ToTimeSpanNullable(this string? it)
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

    public static Guid ToGuid(this string? it, Guid defaultValue = default)
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
    public static Guid? ToGuidNullable(this string? it)
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

    public static unsafe bool MatchWildcard(this string? it, string pattern, char wildcard = '*')
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