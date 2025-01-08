// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Key size options for encrypting with a symmetric key
    /// </summary>
    public enum SymmetricKeySize : short
    {
        /// <summary>
        /// Symmetric Key of 128 Bits
        /// </summary>
        Bits_128 = 128,
        /// <summary>
        /// Symmetric Key of 192 Bits
        /// </summary>
        Bits_192 = 192,
        /// <summary>
        /// Symmetric Key of 256 Bits
        /// </summary>
        Bits_256 = 256
    }
}