// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETSTANDARD2_0

namespace Zerra.Reflection
{
    internal static class NetStandardTypeExtensions
    {
        extension(Type type)
        {
            //Type.IsByRefLike is not in netstandard2.0, ref structs are marked by the compiler with IsByRefLikeAttribute
            public bool IsByRefLike => type.IsValueType && type.CustomAttributes.Any(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
        }
    }
}

#endif
