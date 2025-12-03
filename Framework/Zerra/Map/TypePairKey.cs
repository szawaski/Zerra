// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Map
{
    /// <summary>
    /// A class to use two types as a hash key.
    /// </summary>
    public class TypePairKey
    {
        private readonly Type? type1;
        private readonly Type? type2;

        public Type? Type1 => type1;
        public Type? Type2 => type2;

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        public TypePairKey(Type? type1, Type? type2)
        {
            this.type1 = type1;
            this.type2 = type2;
        }

        /// <summary>
        /// Determines if the TypeKeys are equal
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if they are equal; otherwise, False.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not TypePairKey objCasted)
                return false;
            if (this.type1 != objCasted.type1 || this.type2 != objCasted.type2)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a hash code unquie for the string and types of the TypeKey
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0

            return HashCode.Combine(type1, type2);
#else
            unchecked
            {
                var hash = (int)2166136261;
                if (type1 is not null)
                    hash = (hash * 16777619) ^ type1.GetHashCode();
                if (type2 is not null)
                    hash = (hash * 16777619) ^ type2.GetHashCode();
                return hash;
            }
#endif
        }

        /// <summary>
        /// Generates a string represenation of the TypeKey
        /// </summary>
        /// <returns>The string representation of the TypeKey.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (type1 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(type1.Name);
            }
            if (type2 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(type2.Name);
            }
            return sb.ToString();
        }
    }
}