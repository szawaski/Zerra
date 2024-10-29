// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private sealed class MemberNameCharComparer
        {
            private const int firstUpperCaseByte = 65;
            private const int lastUpperCaseByte = 90;

            public static unsafe bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
                if (x.Length == 0)
                {
                    if (y.Length == 0)
                        return true;
                    else
                        return false;
                }
                if (y.Length == 0)
                    return false;

                var xLength = x.Length;
                var yLength = y.Length;

                fixed (char* pX = x, pY = y)
                {
                    var i = 0;
                    var j = 0;
                    if (pX[i] == '_')
                        i++;
                    if (pY[j] == '_')
                        j++;
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
                            if (i1 >= firstUpperCaseByte && i1 <= lastUpperCaseByte)
                                i1 += 32;
                            if (i2 >= firstUpperCaseByte && i2 <= lastUpperCaseByte)
                                i2 += 32;
                            if (i1 != c2)
                                return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}