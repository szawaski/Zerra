// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

/// <summary>
/// Global extensions for enum name resolution and parsing using the <see cref="global::EnumName"/> system.
/// </summary>
public static class EnumNameExtensions
{
    /// <summary>
    /// Gets the string representation of an enum value using EnumName attributes.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value.</param>
    /// <returns>The enum as a string.</returns>
    public static string EnumName<T>(this T value)
        where T : Enum 
    {
        return global::EnumName.GetName<T>(value);
    }

    /// <summary>
    /// Gets the string representation of a nullable enum value using EnumName attributes.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The nullable enum value.</param>
    /// <returns>The enum as a string, or null if the value is null.</returns>
    public static string? EnumName<T>(this T? value)
        where T : struct, Enum
    {
        if (value is null)
            return null;
        return global::EnumName.GetName<T>(value.Value);
    }

    /// <summary>
    /// Parses a string to an enum value using EnumName parsing.
    /// </summary>
    /// <typeparam name="T">The enum type to parse into.</typeparam>
    /// <param name="it">The string representation of the enum value.</param>
    /// <returns>The parsed enum value.</returns>
    public static T ToEnum<T>(this string? it)
    where T : Enum
    {
        return global::EnumName.Parse<T>(it);
    }
    /// <summary>
    /// Attempts to parse a string to a nullable enum value using EnumName parsing.
    /// </summary>
    /// <typeparam name="T">The enum type to parse into.</typeparam>
    /// <param name="it">The string representation of the enum value.</param>
    /// <returns>The parsed enum value, or null if parsing fails or the input is null.</returns>
    public static T? ToEnumNullable<T>(this string? it)
        where T : Enum
    {
        if (global::EnumName.TryParse<T>(it, out var value))
            return value;
        return default;
    }
}