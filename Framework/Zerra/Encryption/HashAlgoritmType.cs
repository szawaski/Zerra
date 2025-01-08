// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Incidicates a hash algorithm type.
    /// </summary>
    public enum HashAlgoritmType : byte
    {
        /// <summary>
        /// Secure Hash Algorithm 1 (SHA-1)
        /// </summary>
        SHA1,
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
        SHA384,
        /// <summary>
        /// Message Digest 5 Algorithm (MD5)
        /// </summary>
        MD5
    }
}