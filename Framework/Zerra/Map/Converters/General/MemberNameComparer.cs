// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Buffers;

namespace Zerra.Map.Converters.General
{
    internal sealed class MemberNameComparer : IEqualityComparer<string>
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
                        var v1 = (int)c1;
                        var v2 = (int)c2;
                        if (v1 >= firstUpperCase && v1 <= lastUpperCase)
                            v1 += 32;
                        if (v2 >= firstUpperCase && v2 <= lastUpperCase)
                            v2 += 32;
                        if (v1 != v2)
                            return false;
                    }
                }
            }

            return true;
        }

        public unsafe int GetHashCode(
#if !NETSTANDARD2_0
            [DisallowNull]
#endif
        string obj)
        {
            char[]? rented = null;
            scoped Span<char> chars;
            if (obj.Length < 128)
            {
                chars = stackalloc char[obj.Length];
            }
            else
            {
                rented = ArrayPoolHelper<char>.Rent(obj.Length);
                chars = rented;
            }

            bool altered = false;
            var length = obj.Length;
            var charsIndex = 0;
            fixed (char* p = obj)
            {
                var i = 0;
                if (p[i] == '_')
                {
                    i++;
                    altered = true;
                }
                while (i < length)
                {
                    var c = p[i++];
                    var v = (int)c;
                    if (v >= firstUpperCase && v <= lastUpperCase)
                    {
                        v += 32;
                        if (!altered)
                            altered = true;
                    }
                    chars[charsIndex++] = (char)v;
                }
            }

            if (rented is not null)
                ArrayPoolHelper<char>.Return(rented, charsIndex);

            if (!altered)
                return obj.GetHashCode();

#if NET7_0_OR_GREATER
            return String.GetHashCode(chars.Slice(0, charsIndex));
#else
            return chars.Slice(0, charsIndex).ToString().GetHashCode();
#endif
        }
    }
}