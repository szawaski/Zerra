// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

public static class TypeExtensions
{
    private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new();
    public static string GetNiceName(this Type it)
    {
        if (it == null)
            return "null";
        var niceName = niceNames.GetOrAdd(it, (it) =>
        {
            return GenerateNiceName(it, false);
        });
        return niceName;
    }

    private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();
    public static string GetNiceFullName(this Type it)
    {
        if (it == null)
            return "null";
        var niceFullName = niceFullNames.GetOrAdd(it, (it) =>
        {
            return GenerateNiceName(it, true);
        });
        return niceFullName;
    }

    //framework dependent if property exists
    private static Func<object, object?>? typeGetIsZSArrayGetter;
    private static bool loadedTypeGetIsZSArrayGetter = false;
    private static Func<object, object?>? GetTypeGetIsZSArrayGetter()
    {
        if (!loadedTypeGetIsZSArrayGetter)
        {
            lock (niceFullNames)
            {
                if (!loadedTypeGetIsZSArrayGetter)
                {
                    if (TypeAnalyzer.GetTypeDetail(typeof(Type)).TryGetMember("IsSZArray", out var member))
                        typeGetIsZSArrayGetter = member.Getter;
                    loadedTypeGetIsZSArrayGetter = true;
                }
            }
        }
        return typeGetIsZSArrayGetter;
    }

    private static string GenerateNiceName(Type type, bool ns)
    {
        var typeDetails = TypeAnalyzer.GetTypeDetail(type);

        if (type.IsArray)
        {
            var sb = new StringBuilder();
            if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
                _ = sb.Append(type.Namespace).Append('.');

            var rank = type.GetArrayRank();
            var elementType = typeDetails.InnerTypes[0];
            var elementTypeName = elementType.GetNiceName();
            _ = sb.Append(elementTypeName);
            _ = sb.Append('[');
            var getter = GetTypeGetIsZSArrayGetter();

            var szArray = getter != null && (bool)getter(type)!;
            if (!szArray)
            {
                if (rank == 1)
                {
                    _ = sb.Append('*');
                }
                else if (rank > 1)
                {
                    for (var i = 0; i < rank - 1; i++)
                        _ = sb.Append(',');
                }
            }
            _ = sb.Append(']');

            return sb.ToString();
        }

        if (type.IsGenericType)
        {
            var sb = new StringBuilder();
            if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
                _ = sb.Append(type.Namespace).Append('.');

            _ = sb.Append(type.Name.Split('`')[0]);

            var genericTypes = typeDetails.InnerTypes;
            _ = sb.Append('<');
            var first = true;
            foreach (var genericType in genericTypes)
            {
                if (!first)
                    _ = sb.Append(',');
                first = false;
                if (ns)
                    _ = sb.Append(genericType.GetNiceFullName());
                else
                    _ = sb.Append(genericType.GetNiceName());
            }
            _ = sb.Append('>');

            return sb.ToString();
        }

        if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
        {
            return $"{type.Namespace}.{type.Name}";
        }

        return type.Name;
    }
}
