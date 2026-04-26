// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra
{
    /// <summary>
    /// A key that combines a string and/or a type for use as a dictionary or hash key.
    /// </summary>
    public class TypeKey
    {
        private readonly string? str;
        private readonly Type? type;

        /// <summary>Gets the string component of this key.</summary>
        public string? Str => str;
        /// <summary>Gets the type component of this key.</summary>
        public Type? Type => type;

        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="type">A type for the hash.</param>
        public TypeKey(Type? type)
        {
            this.str = null;
            this.type = type;
        }
        /// <summary>
        /// Creates a new TypeKey.
        /// </summary>
        /// <param name="str">A string for the hash.</param>
        /// <param name="type">A type for the hash.</param>
        public TypeKey(string? str, Type? type)
        {
            this.str = str;
            this.type = type;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="TypeKey"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not TypeKey objCasted)
                return false;
            return this.str == objCasted.str && this.type == objCasted.type;
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="TypeKey"/>.
        /// </summary>
        /// <returns>A hash code derived from the string and type of this instance.</returns>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            return HashCode.Combine(str, type);
#else
            unchecked
            {
                var hash = (int)2166136261;
                if (str is not null)
                    hash = (hash * 16777619) ^ str.GetHashCode();
                if (type is not null)
                    hash = (hash * 16777619) ^ type.GetHashCode();
                return hash;
            }
#endif
        }

        /// <summary>
        /// Returns a string representation of the current <see cref="TypeKey"/>.
        /// </summary>
        /// <returns>A comma-separated string of the non-null components of this instance.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (str is not null)
            {
                _ = sb.Append(str);
            }
            if (type is not null)
            {
                if (sb.Length > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(type.FullName);
            }
            return sb.ToString();
        }
    }
}