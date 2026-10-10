// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Indicates a hash algorithm for <see cref="Hasher"/>.
    /// </summary>
    public enum HashAlgorithmType : byte
    {
        /// <summary>
        /// Secure Hash Algorithm 2 with 256 bits (SHA-256)
        /// </summary>
        SHA256,
        /// <summary>
        /// Secure Hash Algorithm 2 with 512 bits (SHA-512)
        /// </summary>
        SHA512,
        /// <summary>
        /// Secure Hash Algorithm 2 with 384 bits (SHA-384)
        /// </summary>
        SHA384
    }
}
