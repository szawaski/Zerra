// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

public static class TypeHelperExtensions
{
    public static string GetNiceName(this Type it) => TypeHelper.GetNiceName(it);
    public static string GetNiceFullName(this Type it) => TypeHelper.GetNiceFullName(it);
}
