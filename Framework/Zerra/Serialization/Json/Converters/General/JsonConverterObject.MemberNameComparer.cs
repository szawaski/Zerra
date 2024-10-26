// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private sealed class MemberNameComparer : IEqualityComparer<string>
        {
            private const int firstUpperCase = 65;
            private const int lastUpperCase = 90;

            public static readonly MemberNameComparer Instance = new();
            private MemberNameComparer() { }

            public unsafe bool Equals(string? x, string? y)
            {
                if (x is null)
                {
                    if (y is null)
                        return true;
                    else
                        return false;
                }
                if (y is null)
                    return false;

                var xLength = x.Length;
                var yLength = y.Length;

                fixed (char* pX = x, pY = y)
                {
                    var i = 0;
                    var j = 0;
                    while (i < xLength)
                    {
                        if (pX[i] != '_')
                            break;
                        i++;
                    }
                    while (j < xLength)
                    {
                        if (pY[i] != '_')
                            break;
                        j++;
                    }
                    if (xLength - i != yLength - j)
                        return false;
                    while (i < xLength)
                    {
                        var c1 = pX[i++];
                        var c2 = pY[j++];
                        if (c1 != c2)
                        {
                            var i1 = (int)c1;
                            var i2 = (int)c2;
                            if (i1 >= firstUpperCase && i1 <= lastUpperCase)
                                i1 += 32;
                            if (i2 >= firstUpperCase && i2 <= lastUpperCase)
                                i2 += 32;
                            if (i1 != c2)
                                return false;
                        }
                    }
                }

                return true;
            }

            public int GetHashCode(
#if !NETSTANDARD2_0
                [DisallowNull]
#endif
        string obj) => obj.GetHashCode();
        }
    }
}