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
    public static Int16? NullIfDefault(this Int16 it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static Int32? NullIfDefault(this Int32 it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static Int64? NullIfDefault(this Int64 it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt16? NullIfDefault(this UInt16 it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt32? NullIfDefault(this UInt32 it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt64? NullIfDefault(this UInt64 it)
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
    public static Int16? NullIfDefault(this Int16? it)
    {
        if (it == default(Int16))
            return null;
        return it;
    }
    public static Int32? NullIfDefault(this Int32? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static Int64? NullIfDefault(this Int64? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt16? NullIfDefault(this UInt16? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt32? NullIfDefault(this UInt32? it)
    {
        if (it == default)
            return null;
        return it;
    }
    public static UInt64? NullIfDefault(this UInt64? it)
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
    public static Int16 ZeroIfNull(this Int16? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static Int32 ZeroIfNull(this Int32? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static Int64 ZeroIfNull(this Int64? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static UInt16 ZeroIfNull(this UInt16? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static UInt32 ZeroIfNull(this UInt32? it)
    {
        if (it.HasValue)
            return it.Value;
        return 0;
    }
    public static UInt64 ZeroIfNull(this UInt64? it)
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
    public static string ToString(this Int16? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this Int32? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this Int64? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this UInt16? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this UInt32? it, string format)
    {
        if (it.HasValue)
            return it.Value.ToString(format);
        return String.Empty;
    }
    public static string ToString(this UInt64? it, string format)
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
    public static string ToString(this Int16? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this Int32? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this Int64? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt16? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt32? it, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt64? it, IFormatProvider formatProvider)
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
    public static string ToString(this Int16? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this Int32? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this Int64? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt16? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt32? it, string format, IFormatProvider formatProvider)
    {
        if (it.HasValue)
            return it.Value.ToString(format, formatProvider);
        return String.Empty;
    }
    public static string ToString(this UInt64? it, string format, IFormatProvider formatProvider)
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