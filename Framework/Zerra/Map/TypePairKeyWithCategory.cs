// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Map
{
    /// <summary>
    /// A class to use two types as a hash key.
    /// </summary>
    internal class TypePairKeyWithCategory
    {
        private readonly byte category;
        private readonly Type type1;
        private readonly Type type2;

        public Type Type1 => type1;
        public Type Type2 => type2;

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="category">A byte to use as a category for the hash. This can be used to create different hashes for different purposes.</param>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        public TypePairKeyWithCategory(byte category, Type type1, Type type2)
        {
            this.category = category;
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
            if (obj is not TypePairKeyWithCategory objCasted)
                return false;
            if (this.category != objCasted.category || this.type1 != objCasted.type1 || this.type2 != objCasted.type2)
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

            return HashCode.Combine(category, type1, type2);
#else
            unchecked
            {
                var hash = (int)2166136261;
                hash = (hash * 16777619) ^ category.GetHashCode();
                hash = (hash * 16777619) ^ type1.GetHashCode();
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
            _ = sb.Append(category).Append('-');
            _ = sb.Append(type1.FullName).Append(", ");
            _ = sb.Append(type2.FullName);
            return sb.ToString();
        }
    }
}