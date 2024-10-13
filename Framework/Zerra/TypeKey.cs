// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;

namespace Zerra
{
    //This appeard to slow things down as a struct

    /// <summary>
    /// A class to use a combination of a string and/or multiple types as a hash key.
    /// </summary>
    public class TypeKey
    {
        private readonly string? str;
        private readonly int? number;
        private readonly Type? type1;
        private readonly Type? type2;
        private readonly Type[]? typeArray;

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        public TypeKey(Type? type1, Type? type2)
        {
            this.str = null;
            this.number = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = null;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(Type[]? typeArray)
        {
            this.str = null;
            this.number = null;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(Type? type1, Type[]? typeArray)
        {
            this.str = null;
            this.number = null;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(Type? type1, Type? type2, Type[]? typeArray)
        {
            this.str = null;
            this.number = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = typeArray;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="type1">A type for the hash.</param>
        public TypeKey(string? str, Type? type1)
        {
            this.str = str;
            this.number = null;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = null;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        public TypeKey(string? str, Type? type1, Type? type2)
        {
            this.str = str;
            this.number = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = null;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(string? str, Type[]? typeArray)
        {
            this.str = str;
            this.number = null;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(string? str, Type? type1, Type[]? typeArray)
        {
            this.str = str;
            this.number = null;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="type1">A type for the hash.</param>
        /// <param name="type2">A type for the hash.</param>
        /// <param name="typeArray">Types for the hash.</param>
        public TypeKey(string str, Type? type1, Type? type2, Type[]? typeArray)
        {
            this.str = str;
            this.number = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = typeArray;
        }

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="number">A number for the hash.</param>
        public TypeKey(string str, int? number)
        {
            this.str = str;
            this.number = number;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = null;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="number">A number for the hash.</param>
        ///  <param name="typeArray">Types for the hash.</param>
        public TypeKey(string str, int? number, Type[]? typeArray)
        {
            this.str = str;
            this.number = number;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = typeArray;
        }

        /// <summary>
        /// Determines if the TypeKeys are equal
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if they are equal; otherwise, False.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not TypeKey objCasted)
                return false;
            if (this.number != objCasted.number || this.type1 != objCasted.type1 || this.type2 != objCasted.type2 || this.typeArray?.Length != objCasted.typeArray?.Length || this.str != objCasted.str)
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
        /// Gets a hash code unquie for the string and types of the TypeKey
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0

            if (typeArray is null)
                return HashCode.Combine(str, number, type1, type2);

            switch (typeArray.Length)
            {
                case 0: return HashCode.Combine(str, number, type1, type2);
                case 1: return HashCode.Combine(str, number, type1, type2, typeArray[0]);
                case 2: return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1]);
                case 3: return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2]);
                case 4: return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3]);
                case 5:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2], 
                        HashCode.Combine(typeArray[3], typeArray[4]));
                case 6:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5]));
                case 7:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6]));
                case 8:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6], typeArray[7]));
                case 9:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8]));
                case 10:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9]));
                case 11:
                    return HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                        HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9], typeArray[10]));
            }

            var code = HashCode.Combine(str, number, type1, type2, typeArray[0], typeArray[1], typeArray[2],
                            HashCode.Combine(typeArray[3], typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9], typeArray[10]));
            for (var i = 11; i < typeArray.Length; i++)
                code = HashCode.Combine(code, typeArray[i]);

            return code;
#else
            unchecked
            {
                var hash = (int)2166136261;
                if (str is not null)
                    hash = (hash * 16777619) ^ str.GetHashCode();
                if (number is not null)
                    hash = (hash * 16777619) ^ number.GetHashCode();
                if (type1 is not null)
                    hash = (hash * 16777619) ^ type1.GetHashCode();
                if (type2 is not null)
                    hash = (hash * 16777619) ^ type2.GetHashCode();
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
            if (number is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(number);
            }
            if (type1 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(type1.GetNiceName());
            }
            if (type2 is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(type2.GetNiceName());
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
                    _ = sb.Append(typeArray[i].GetNiceName());
                }
                _ = sb.Append(']');
            }
            return sb.ToString();
        }
    }
}