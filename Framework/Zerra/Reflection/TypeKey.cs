// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Reflection
{
    /// <summary>
    /// A class to use a combination of a string, two numbers and/or multiple types as a hash key.
    /// </summary>
    internal sealed class TypeKey
    {
        private readonly string? str;
        private readonly int? number1;
        private readonly int? number2;
        private readonly Type[]? typeArray;

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="number1">A number for the hash.</param>
        /// <param name="number2">A second number for the hash.</param>
        /// <param name="typeArray">An array of types for the hash.</param>
        public TypeKey(string? str, int? number1, int? number2, Type[]? typeArray)
        {
            this.str = str;
            this.number1 = number1;
            this.number2 = number2;
            this.typeArray = typeArray;
        }

        /// <summary>
        /// Determines if the TypeKeys are equal
        /// </summary>
        /// <param name="obj">The other TypeKey.</param>
        /// <returns>True if they are equal; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not TypeKey objCasted)
                return false;
            if (this.number1 != objCasted.number1 || this.number2 != objCasted.number2 || this.typeArray?.Length != objCasted.typeArray?.Length || this.str != objCasted.str)
                return false;

            if (this.typeArray is not null && objCasted.typeArray is not null)
            {
                for (var i = 0; i < this.typeArray.Length; i++)
                {
                    if (this.typeArray[i] != objCasted.typeArray[i])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a hash code for the string, numbers, and types of the TypeKey.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            var hash = new HashCode();
            hash.Add(str);
            hash.Add(number1);
            hash.Add(number2);
            if (typeArray is not null)
            {
                for (var i = 0; i < typeArray.Length; i++)
                    hash.Add(typeArray[i]);
            }
            return hash.ToHashCode();
#else
            unchecked
            {
                var hash = (int)2166136261;
                if (str is not null)
                    hash = (hash * 16777619) ^ str.GetHashCode();
                if (number1 is not null)
                    hash = (hash * 16777619) ^ number1.GetHashCode();
                if (number2 is not null)
                    hash = (hash * 16777619) ^ (number2.GetHashCode() + 1);
                if (typeArray is not null)
                {
                    for (var i = 0; i < typeArray.Length; i++)
                        hash = (hash * 16777619) ^ typeArray[i].GetHashCode();
                }
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
            if (str is not null)
            {
                _ = sb.Append(str);
            }
            if (number1 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(number1);
            }
            if (number2 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(number2);
            }
            if (typeArray is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append('[');
                for (var i = 0; i < typeArray.Length; i++)
                {
                    if (i > 0)
                        _ = sb.Append(", ");
                    _ = sb.Append(typeArray[i].Name);
                }
                _ = sb.Append(']');
            }
            return sb.ToString();
        }
    }
}