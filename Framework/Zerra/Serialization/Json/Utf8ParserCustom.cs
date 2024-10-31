// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Globalization;

namespace Zerra.Serialization.Json
{
    internal static partial class Utf8ParserCustom
    {
        public unsafe static bool TryParse(ReadOnlySpan<byte> source, out DateTime value)
        {
            if (source.Length > 33)
            {
                value = default;
                return false;
            }

            var length = source.Length;
            fixed (byte* pSource = source)
            {
                var pChars = stackalloc char[length];
                for (var i = 0; i < length; i++)
                    pChars[i] = (char)pSource[i];


#if NETSTANDARD2_0
                if (!DateTime.TryParse(new String(pChars, 0, length), null, DateTimeStyles.RoundtripKind, out value))
#else
                var chars = new Span<char>(pChars, length);
                if (!DateTime.TryParse(chars, null, DateTimeStyles.RoundtripKind, out value))
#endif
                    return false;
                return true;
            }
        }

        public unsafe static bool TryParse(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            if (source.Length > 33)
            {
                value = default;
                return false;
            }

            var length = source.Length;
            fixed (byte* pSource = source)
            {
                var pChars = stackalloc char[length];
                for (var i = 0; i < length; i++)
                    pChars[i] = (char)pSource[i];


#if NETSTANDARD2_0
                if (!DateTimeOffset.TryParse(new String(pChars, 0, length), null, DateTimeStyles.RoundtripKind, out value))
#else
                var chars = new Span<char>(pChars, length);
                if (!DateTimeOffset.TryParse(chars, null, DateTimeStyles.RoundtripKind, out value))
#endif
                    return false;
                return true;
            }
        }

#if NET6_0_OR_GREATER
        public unsafe static bool TryParse(ReadOnlySpan<byte> source, out DateOnly value)
        {
            if (source.Length > 33)
            {
                value = default;
                return false;
            }

            var length = source.Length;
            fixed (byte* pSource = source)
            {
                var pChars = stackalloc char[length];
                for (var i = 0; i < length; i++)
                    pChars[i] = (char)pSource[i];


#if NETSTANDARD2_0
                if (!DateOnly.TryParse(new String(pChars, 0, length), out value))
#else
                var chars = new Span<char>(pChars, length);
                if (!DateOnly.TryParse(chars, out value))
#endif
                    return false;
                return true;
            }
        }
#endif
    }
}