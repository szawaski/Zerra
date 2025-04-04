// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

public static class TypeExtensions
{
    public static string GetNiceName(this Type it) => Discovery.GetNiceName(it);
    public static string GetNiceFullName(this Type it) => Discovery.GetNiceFullName(it);
}
