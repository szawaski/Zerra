// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Encryption
{
    public sealed class SymmetricKey
    {
        public byte[] Key { get; private set; }
        public byte[] IV { get; private set; }
        public int KeySize { get; private set; }
        public int BlockSize { get; private set; }
        public SymmetricKey(byte[] key, byte[] iv)
        {
            this.Key = key ?? throw new ArgumentNullException("key");
            this.IV = iv ?? throw new ArgumentNullException("iv");
            this.KeySize = key.Length * 8;
            this.BlockSize = iv.Length * 8;
        }

        public override bool Equals(object obj)
        {
            if (obj is not SymmetricKey casted)
                return false;
            if (this.KeySize != casted.KeySize || this.BlockSize != casted.BlockSize)
                return false;
            if (!this.Key.BytesEquals(casted.Key))
                return false;
            if (!this.IV.BytesEquals(casted.IV))
                return false;
            return true;
        }

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