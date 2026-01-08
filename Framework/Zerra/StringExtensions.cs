// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

/// <summary>
/// Provides extension methods for string manipulation and conversion operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="it">The string to truncate. Must not be null.</param>
    /// <param name="maxLength">The maximum length of the truncated string. Must be non-negative.</param>
    /// <returns>The original string if it is shorter than or equal to maxLength; otherwise, a substring of the specified length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="it"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxLength"/> is less than zero.</exception>
    public static string Truncate(this string? it, int maxLength)
    {
        if (it is null) throw new ArgumentNullException(nameof(it));
        if (maxLength < 0) throw new ArgumentException("Cannot be less than zero", nameof(maxLength));

        if (it.Length < maxLength)
            return it;
        return it.Substring(0, maxLength);
    }

    /// <summary>
    /// Joins two strings with a separator, truncating to the specified maximum length if necessary.
    /// Truncation is applied proportionally to the longer string first.
    /// </summary>
    /// <param name="maxLength">The maximum length of the result. Must be at least as long as the separator.</param>
    /// <param name="seperator">The string to use as a separator between the two strings.</param>
    /// <param name="str1">The first string to join.</param>
    /// <param name="str2">The second string to join.</param>
    /// <returns>The joined string, truncated if necessary to fit within maxLength.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxLength"/> is shorter than the separator length.</exception>
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

    /// <summary>
    /// Joins three strings with a separator, truncating to the specified maximum length if necessary.
    /// Truncation is applied evenly to the longest strings.
    /// </summary>
    /// <param name="maxLength">The maximum length of the result. Must be at least twice the separator length.</param>
    /// <param name="seperator">The string to use as a separator between the strings.</param>
    /// <param name="str1">The first string to join.</param>
    /// <param name="str2">The second string to join.</param>
    /// <param name="str3">The third string to join.</param>
    /// <returns>The joined string, truncated if necessary to fit within maxLength.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxLength"/> is shorter than twice the separator length.</exception>
    public unsafe static string Join(int maxLength, string seperator, string str1, string str2, string str3)
    {
        if (maxLength - (seperator.Length * 2) < 0) throw new ArgumentException("Cannot be shorter than the seperator length", nameof(maxLength));

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

    /// <summary>
    /// Joins four strings with a separator, truncating to the specified maximum length if necessary.
    /// Truncation is applied evenly to the longest strings.
    /// </summary>
    /// <param name="maxLength">The maximum length of the result. Must be at least three times the separator length.</param>
    /// <param name="seperator">The string to use as a separator between the strings.</param>
    /// <param name="str1">The first string to join.</param>
    /// <param name="str2">The second string to join.</param>
    /// <param name="str3">The third string to join.</param>
    /// <param name="str4">The fourth string to join.</param>
    /// <returns>The joined string, truncated if necessary to fit within maxLength.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxLength"/> is shorter than three times the separator length.</exception>
    public unsafe static string Join(int maxLength, string seperator, string str1, string str2, string str3, string str4)
    {
        if (maxLength - (seperator.Length * 3) < 0) throw new ArgumentException("Cannot be shorter than the seperator length", nameof(maxLength));

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

    /// <summary>
    /// Converts the string to a boolean value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to false.</param>
    /// <returns>The boolean value if successfully parsed; otherwise, the default value.</returns>
    public static bool ToBoolean(this string? it, bool defaultValue = default)
    {
        if (it is null)
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

    /// <summary>
    /// Converts the string to a nullable boolean value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The boolean value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static bool? ToBooleanNullable(this string? it)
    {
        if (it is null)
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

    /// <summary>
    /// Converts the string to a byte value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The byte value if successfully parsed; otherwise, the default value.</returns>
    public static byte ToByte(this string? it, byte defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Byte.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable byte value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The byte value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static byte? ToByteNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Byte.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 16-bit signed integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The Int16 value if successfully parsed; otherwise, the default value.</returns>
    public static short ToInt16(this string? it, short defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 16-bit signed integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The Int16 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static short? ToInt16Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 16-bit unsigned integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The UInt16 value if successfully parsed; otherwise, the default value.</returns>
    public static ushort ToUInt16(this string? it, ushort defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 16-bit unsigned integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The UInt16 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static ushort? ToUInt16Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt16.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 32-bit signed integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The Int32 value if successfully parsed; otherwise, the default value.</returns>
    public static int ToInt32(this string? it, int defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 32-bit signed integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The Int32 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static int? ToInt32Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 32-bit unsigned integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The UInt32 value if successfully parsed; otherwise, the default value.</returns>
    public static uint ToUInt32(this string? it, uint defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 32-bit unsigned integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The UInt32 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static uint? ToUInt32Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt32.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 64-bit signed integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The Int64 value if successfully parsed; otherwise, the default value.</returns>
    public static long ToInt64(this string? it, long defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Int64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 64-bit signed integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The Int64 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static long? ToInt64Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Int64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a 64-bit unsigned integer value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.</param>
    /// <returns>The UInt64 value if successfully parsed; otherwise, the default value.</returns>
    public static ulong ToUInt64(this string? it, ulong defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (UInt64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable 64-bit unsigned integer value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The UInt64 value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static ulong? ToUInt64Nullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (UInt64.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a single-precision floating-point value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.0f.</param>
    /// <returns>The float value if successfully parsed; otherwise, the default value.</returns>
    public static float ToFloat(this string? it, float defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Single.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable single-precision floating-point value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The float value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static float? ToFloatNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Single.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a double-precision floating-point value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0.0.</param>
    /// <returns>The double value if successfully parsed; otherwise, the default value.</returns>
    public static double ToDouble(this string? it, double defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Double.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable double-precision floating-point value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The double value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static double? ToDoubleNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Double.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a decimal value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to 0m.</param>
    /// <returns>The decimal value if successfully parsed; otherwise, the default value.</returns>
    public static decimal ToDecimal(this string? it, decimal defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Decimal.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable decimal value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The decimal value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static decimal? ToDecimalNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Decimal.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a DateTime value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to DateTime.MinValue.</param>
    /// <returns>The DateTime value if successfully parsed; otherwise, the default value.</returns>
    public static DateTime ToDateTime(this string? it, DateTime defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (DateTime.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable DateTime value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The DateTime value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static DateTime? ToDateTimeNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (DateTime.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a DateTimeOffset value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to DateTimeOffset.MinValue.</param>
    /// <returns>The DateTimeOffset value if successfully parsed; otherwise, the default value.</returns>
    public static DateTimeOffset ToDateTimeOffset(this string? it, DateTimeOffset defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (DateTimeOffset.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable DateTimeOffset value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The DateTimeOffset value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static DateTimeOffset? ToDateTimeOffsetNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (DateTimeOffset.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a TimeSpan value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to TimeSpan.Zero.</param>
    /// <returns>The TimeSpan value if successfully parsed; otherwise, the default value.</returns>
    public static TimeSpan ToTimeSpan(this string? it, TimeSpan defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (TimeSpan.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable TimeSpan value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The TimeSpan value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static TimeSpan? ToTimeSpanNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (TimeSpan.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Converts the string to a DateOnly value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to DateOnly.MinValue.</param>
    /// <returns>The DateOnly value if successfully parsed; otherwise, the default value.</returns>
    public static DateOnly ToDateOnly(this string? it, DateOnly defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (DateOnly.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable DateOnly value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The DateOnly value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static DateOnly? ToDateOnlyNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (DateOnly.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

    /// <summary>
    /// Converts the string to a TimeOnly value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to TimeOnly.MinValue.</param>
    /// <returns>The TimeOnly value if successfully parsed; otherwise, the default value.</returns>
    public static TimeOnly ToTimeOnly(this string? it, TimeOnly defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (TimeOnly.TryParse(it, out var tryDateTime))
            return tryDateTime;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable TimeOnly value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The TimeOnly value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static TimeOnly? ToTimeOnlyNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (TimeOnly.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }
#endif

    /// <summary>
    /// Converts the string to a Guid value, returning a default value if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <param name="defaultValue">The value to return if conversion fails or the string is null or empty. Defaults to Guid.Empty.</param>
    /// <returns>The Guid value if successfully parsed; otherwise, the default value.</returns>
    public static Guid ToGuid(this string? it, Guid defaultValue = default)
    {
        if (it is null)
            return defaultValue;
        if (it == String.Empty)
            return defaultValue;
        if (Guid.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return defaultValue;
    }

    /// <summary>
    /// Converts the string to a nullable Guid value, returning null if the string is null or empty.
    /// </summary>
    /// <param name="it">The string to convert. May be null.</param>
    /// <returns>The Guid value if successfully parsed; null if the string is null, empty, or cannot be parsed.</returns>
    public static Guid? ToGuidNullable(this string? it)
    {
        if (it is null)
            return null;
        if (it == String.Empty)
            return null;
        if (Guid.TryParse(it, out var tryvalue))
            return tryvalue;
        else
            return null;
    }

#if !NET5_0_OR_GREATER
    /// <summary>
    /// Splits the string using the specified separator string.
    /// </summary>
    /// <param name="it">The string to split. Must not be null.</param>
    /// <param name="seperator">The separator string.</param>
    /// <returns>An array of strings split by the separator.</returns>
    public static string[] Split(this string it, string seperator)
    {
        return it.Split(new string[] { seperator }, StringSplitOptions.None);
    }

    /// <summary>
    /// Splits the string using the specified separator string and split options.
    /// </summary>
    /// <param name="it">The string to split. Must not be null.</param>
    /// <param name="seperator">The separator string.</param>
    /// <param name="options">Specifies whether to remove empty entries from the result.</param>
    /// <returns>An array of strings split by the separator.</returns>
    public static string[] Split(this string it, string seperator, StringSplitOptions options)
    {
        return it.Split(new string[] { seperator }, options);
    }
#endif

    /// <summary>
    /// Matches the string against a wildcard pattern.
    /// </summary>
    /// <param name="it">The string to match. May be null.</param>
    /// <param name="pattern">The wildcard pattern to match against. Must not be null or empty.</param>
    /// <param name="wildcard">The character used as a wildcard. Defaults to '*'. The wildcard matches zero or more characters.</param>
    /// <returns>True if the string matches the pattern; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern"/> is null or empty.</exception>
    public static unsafe bool MatchWildcard(this string? it, string pattern, char wildcard = '*')
    {
        if (String.IsNullOrEmpty(pattern))
            throw new ArgumentException(nameof(pattern));

        fixed (char* pPattern = pattern)
        {
            if (it is null || it.Length == 0)
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