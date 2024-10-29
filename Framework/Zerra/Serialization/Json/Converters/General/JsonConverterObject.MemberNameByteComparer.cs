// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private sealed class MemberNameByteComparer
        {
            private const int firstUpperCaseByte = 65;
            private const int lastUpperCaseByte = 90;
            private const int underscoreByte = (byte)'_';

            public static unsafe bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
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

                fixed (byte* pX = x, pY = y)
                {
                    var i = 0;
                    var j = 0;
                    if (pX[i] == underscoreByte)
                        i++;
                    if (pY[j] == underscoreByte)
                        j++;
                    if (xLength - i != yLength - j)
                        return false;
                    while (i < xLength)
                    {
                        var c1 = pX[i++];
                        var c2 = pY[j++];
                        if (c1 != c2)
                        {
                            if (c1 >= firstUpperCaseByte && c1 <= lastUpperCaseByte)
                                c1 += 32;
                            if (c2 >= firstUpperCaseByte && c2 <= lastUpperCaseByte)
                                c2 += 32;
                            if (c1 != c2)
                                return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}