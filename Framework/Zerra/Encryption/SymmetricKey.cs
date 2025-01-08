// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Encryption
{
    /// <summary>
    /// Standard symmetric key for encryption with key and initialization vector.
    /// </summary>
    public sealed class SymmetricKey
    {
        /// <summary>
        /// The key.
        /// </summary>
        public byte[] Key { get; }
        /// <summary>
        /// The initialization vector.
        /// </summary>
        public byte[] IV { get; }
        /// <summary>
        /// The size of the key in bytes.
        /// </summary>
        public int KeySize { get; }
        /// <summary>
        /// The size of each encrypted block in bytes.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// Constructs a new symmetric key.
        /// </summary>
        /// <param name="key">The bytes of the key.</param>
        /// <param name="iv">The bytes of the initialization vector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SymmetricKey(byte[] key, byte[] iv)
        {
            this.Key = key ?? throw new ArgumentNullException(nameof(key));
            this.IV = iv ?? throw new ArgumentNullException(nameof(iv));
            this.KeySize = key.Length * 8;
            this.BlockSize = iv.Length * 8;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not SymmetricKey casted)
                return false;
            if (this.KeySize != casted.KeySize || this.BlockSize != casted.BlockSize)
                return false;
            if (!this.Key.AsSpan().SequenceEqual(casted.Key))
                return false;
            if (!this.IV.AsSpan().SequenceEqual(casted.IV))
                return false;
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            return HashCode.Combine(Key, IV, KeySize, BlockSize);
#else
            unchecked
            {
                var hash = (int)2166136261;
                hash = (hash * 16777619) ^ Key.GetHashCode();
                hash = (hash * 16777619) ^ IV.GetHashCode();
                return hash;
            }
#endif
        }
    }
}