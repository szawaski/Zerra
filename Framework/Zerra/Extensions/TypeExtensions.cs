// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

public static class TypeExtensions
{
    private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new ConcurrentFactoryDictionary<Type, string>();
    public static string GetNiceName(this Type it)
    {
        var niceName = niceNames.GetOrAdd(it, (t) =>
        {
            return GenerateNiceName(t, false);
        });
        return niceName;
    }

    private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new ConcurrentFactoryDictionary<Type, string>();
    public static string GetNiceFullName(this Type it)
    {
        var niceFullName = niceFullNames.GetOrAdd(it, (t) =>
        {
            return GenerateNiceName(t, true);
        });
        return niceFullName;
    }

    //framework dependent if property exists
    private static Func<object, object> typeGetIsZSArrayGetter;
    private static bool loadedTypeGetIsZSArrayGetter = false;
    private static Func<object, object> GetTypeGetIsZSArrayGetter()
    {
        if (!loadedTypeGetIsZSArrayGetter)
        {
            lock (niceFullNames)
            {
                if (!loadedTypeGetIsZSArrayGetter)
                {
                    if (TypeAnalyzer.GetTypeDetail(typeof(Type)).TryGetMember("IsSZArray", out MemberDetail member))
                        typeGetIsZSArrayGetter = member.Getter;
                    loadedTypeGetIsZSArrayGetter = true;
                }
            }
        }
        return typeGetIsZSArrayGetter;
    }

    private static string GenerateNiceName(Type type, bool ns)
    {
        var sb = new StringBuilder();

        var typeDetails = TypeAnalyzer.GetTypeDetail(type);

        if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
        {
            sb.Append(type.Namespace).Append('.');
        }

        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var elementType = typeDetails.InnerTypes[0];
            var elementTypeName = elementType.GetNiceName();
            sb.Append(elementTypeName);
            sb.Append('[');
            var getter = GetTypeGetIsZSArrayGetter();
            var szArray = getter != null && (bool)getter(type);
            if (!szArray)
            {
                if (rank == 1)
                {
                    sb.Append('*');
                }
                else if (rank > 1)
                {
                    for (var i = 0; i < rank - 1; i++)
                        sb.Append(',');
                }
            }
            sb.Append(']');
        }
        else
        {
            sb.Append(type.Name.Split('`')[0]);

            if (type.IsGenericType)
            {
                var genericTypes = typeDetails.InnerTypes;
                sb.Append('<');
                bool first = true;
                foreach (var genericType in genericTypes)
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    if (ns)
                        sb.Append(genericType.GetNiceFullName());
                    else
                        sb.Append(genericType.GetNiceName());
                }
                sb.Append('>');
            }
        }

        return sb.ToString();
    }
}
