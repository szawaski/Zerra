// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using Zerra.Reflection;

public static class TypeAnalyzerExtensions
{
    public static TypeDetail GetTypeDetail(this Type it) => TypeAnalyzer.GetTypeDetail(it);
    public static MethodDetail GetMethodDetail(this Type it, string name, Type[]? parameterTypes = null) => TypeAnalyzer.GetMethodDetail(it, name, parameterTypes);
    public static ConstructorDetail GetConstructorDetail(this Type it, Type[]? parameterTypes = null) => TypeAnalyzer.GetConstructorDetail(it, parameterTypes);
    public static MethodDetail GetGenericMethodDetail(this MethodInfo it, params Type[] types) => TypeAnalyzer.GetGenericMethodDetail(it, types);
    public static MethodDetail GetGenericMethodDetail(this MethodDetail it, params Type[] types) => TypeAnalyzer.GetGenericMethodDetail(it, types);
    public static TypeDetail GetGenericTypeDetail(this TypeDetail it, params Type[] types) => TypeAnalyzer.GetGenericTypeDetail(it, types);
    public static Type GetGenericType(this Type it, params Type[] types) => TypeAnalyzer.GetGenericType(it, types);
    public static TypeDetail GetGenericTypeDetail(this Type it, params Type[] types) => TypeAnalyzer.GetGenericTypeDetail(it, types);
}
