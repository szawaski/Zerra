// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Reflection
{
    internal sealed class StringOrdinalIgnoreCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            return String.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

#if NETSTANDARD2_0
        public int GetHashCode(string obj)
        {
            return obj.ToLowerInvariant().GetHashCode();
        }
#else
        public int GetHashCode([DisallowNull] string obj)
        {
            return obj.ToLowerInvariant().GetHashCode();
        }
#endif
    }
}