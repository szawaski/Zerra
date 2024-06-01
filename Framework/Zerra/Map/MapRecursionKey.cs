// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Map
{
    internal class MapRecursionKey
    {
        private readonly object source;
        private readonly Type target;
        public MapRecursionKey(object source, Type target)
        {
            this.source = source;
            this.target = target;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MapRecursionKey objCasted)
                return false;
            return objCasted.source == source && objCasted.target == target;
        }

        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            return HashCode.Combine(source, target);
#else
            unchecked
            {
                var hash = (int)2166136261;
                hash = hash * 16777619 ^ source.GetHashCode();
                hash = hash * 16777619 ^ target.GetHashCode();
                return hash;
            }
#endif
        }
    }
}
