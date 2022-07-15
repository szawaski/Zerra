// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

public static class EnumNameExtensions
{
    public static string EnumName<T>(this T value)
        where T : Enum 
    {
        return global::EnumName.GetName<T>(value);
    }

    public static string EnumName<T>(this T? value)
    where T : struct, Enum
    {
        if (value == null)
            return null;
        return global::EnumName.GetName<T>(value.Value);
    }
}