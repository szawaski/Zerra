// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

public static class NumericExtensions
{
    public static byte? NullIfDefault(this byte it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static short? NullIfDefault(this short it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static int? NullIfDefault(this int it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static long? NullIfDefault(this long it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static ushort? NullIfDefault(this ushort it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static uint? NullIfDefault(this uint it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static ulong? NullIfDefault(this ulong it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static float? NullIfDefault(this float it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static double? NullIfDefault(this double it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static decimal? NullIfDefault(this decimal it)
    {
        if (it == default)
            return null;
        return it;
    }

    public static byte? NullIfDefault(this byte? it)
    {
        if (it == default(byte))
            return null;
        return it;
    }
    public static short? NullIfDefault(this short? it)
    {
        if (it == default(short))
            return null;
        return it;
    }
    public static int? NullIfDefault(this int? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static long? NullIfDefault(this long? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static ushort? NullIfDefault(this ushort? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static uint? NullIfDefault(this uint? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static ulong? NullIfDefault(this ulong? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static float? NullIfDefault(this float? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static double? NullIfDefault(this double? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static decimal? NullIfDefault(this decimal? it)
    {
        if (it == default)
            return null;
        return it;
    }

    public static byte ZeroIfNull(this byte? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static short ZeroIfNull(this short? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static int ZeroIfNull(this int? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static long ZeroIfNull(this long? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static ushort ZeroIfNull(this ushort? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static uint ZeroIfNull(this uint? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static ulong ZeroIfNull(this ulong? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static float ZeroIfNull(this float? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static double ZeroIfNull(this double? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static decimal ZeroIfNull(this decimal? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }

    public static string ToString(this byte? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this short? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this int? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this long? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this ushort? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this uint? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this ulong? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this float? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this double? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this decimal? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }

    public static string ToString(this byte? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this short? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this int? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this long? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this ushort? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this uint? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this ulong? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this float? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this double? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this decimal? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }

    public static string ToString(this byte? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this short? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this int? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this long? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this ushort? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this uint? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this ulong? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this float? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this double? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this decimal? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
}