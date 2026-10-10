// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// The symmetric algorithms used before, for data encrypted with them.
    /// New data uses <see cref="SymmetricAlgorithmType"/>.
    /// </summary>
    public enum SymmetricAlgorithmTypeOld : byte
    {
        /// <summary>
        /// AES in CBC mode with the IV derived from the key, so the same data always encrypts to the same bytes.
        /// </summary>
        AES,

        /// <summary>
        /// AES in CBC mode with a random block that shifts the others, the old default.
        /// </summary>
        AESwithShift
    }
}
