// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Incidicates a symmetric encryption algorithm.
    /// </summary>
    public enum SymmetricAlgorithmType : byte
    {
        /// <summary>
        /// Advanced Encryption Standard (AES) algorithm.
        /// </summary>
        AES,
        /// <summary>
        /// Data Encryption Standard (DES) algorithm
        /// </summary>
        DES,
        /// <summary>
        /// Data Encryption Standard (DES) algorithm applied three times to each block
        /// </summary>
        TripleDES,
        /// <summary>
        /// Rivest Cipher 2 (RC2) algorithm.
        /// </summary>
        RC2,
        /// <summary>
        /// Advanced Encryption Standard (AES) algorithm.
        /// The shift inserts a random block used to shift all other blocks so encrypting the same data will look unique.
        /// </summary>
        AESwithShift,
        /// <summary>
        /// Data Encryption Standard (DES) algorithm.
        /// The shift inserts a random block used to shift all other blocks so encrypting the same data will look unique.
        /// </summary>
        DESwithShift,
        /// <summary>
        /// Data Encryption Standard (DES) algorithm applied three times to each block
        /// The shift inserts a random block used to shift all other blocks so encrypting the same data will look unique.
        /// </summary>
        TripleDESwithShift,
        /// <summary>
        /// Rivest Cipher 2 (RC2) algorithm.
        /// The shift inserts a random block used to shift all other blocks so encrypting the same data will look unique.
        /// </summary>
        RC2withShift
    }
}