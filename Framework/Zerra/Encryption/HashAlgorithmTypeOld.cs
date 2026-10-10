// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// The hash algorithms Zerra 5's <c>HashAlgoritmType</c> had, for <see cref="HasherOld"/> to check hashes made with them. The numbers are Zerra 5's.
    /// New hashes use <see cref="HashAlgorithmType"/>.
    /// </summary>
    [Obsolete]
    public enum HashAlgorithmTypeOld : byte
    {
        /// <summary>
        /// Secure Hash Algorithm 1 (SHA-1)
        /// </summary>
        SHA1 = 0,
        /// <summary>
        /// Secure Hash Algorithm 2 with 256 bits (SHA-256)
        /// </summary>
        SHA256 = 1,
        /// <summary>
        /// Secure Hash Algorithm 2 with 512 bits (SHA-512)
        /// </summary>
        SHA512 = 2,
        /// <summary>
        /// Secure Hash Algorithm 2 with 384 bits (SHA-384)
        /// </summary>
        SHA384 = 3,
        /// <summary>
        /// Message Digest 5 Algorithm (MD5)
        /// </summary>
        MD5 = 4,
    }
}
