// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

/// <summary>
/// Global Extensions for the <c>EnumName</c> Attribute.
/// </summary>
public static class EnumNameExtensions
{
    /// <summary>
    /// Get the string representation of an Enum using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <param name="value">The Enum value.</param>
    /// <returns>The Enum as a string.</returns>
    public static string EnumName<T>(this T value)
        where T : Enum 
    {
        return global::EnumName.GetName<T>(value);
    }

    /// <summary>
    /// Get the string representation of an Enum using EnumName Attributes.
    /// </summary>
    /// <typeparam name="T">The Enum type.</typeparam>
    /// <param name="value">The Enum value.</param>
    /// <returns>The Enum as a string.</returns>
    public static string? EnumName<T>(this T? value)
        where T : struct, Enum
    {
        if (value is null)
            return null;
        return global::EnumName.GetName<T>(value.Value);
    }
}