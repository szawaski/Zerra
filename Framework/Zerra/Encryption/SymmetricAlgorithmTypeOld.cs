// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// The symmetric algorithms Zerra 5 used, for data it encrypted. The numbers are Zerra 5's, so stored values keep their meaning.
    /// New data uses <see cref="SymmetricAlgorithmType"/>.
    /// </summary>
    public enum SymmetricAlgorithmTypeOld : byte
    {
        /// <summary>
        /// AES in CBC mode with the IV derived from the key, so the same data always encrypts to the same bytes.
        /// </summary>
        AES = 0,

        /// <summary>
        /// AES in CBC mode with a random block that shifts the others, Zerra 5's default.
        /// </summary>
        AESwithShift = 4,
    }
}
