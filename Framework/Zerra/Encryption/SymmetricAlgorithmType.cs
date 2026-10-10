// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Indicates a symmetric encryption algorithm and mode.
    /// </summary>
    /// <remarks>
    /// The numbers are fixed so stored values keep their meaning. For data Zerra 5 encrypted, use <see cref="SymmetricAlgorithmTypeOld"/>.
    /// </remarks>
    public enum SymmetricAlgorithmType : byte
    {
        /// <summary>
        /// AES in CBC mode with a random IV for each chunk. Keeps data private but doesn't detect changes to it.
        /// </summary>
        AES_CBC = 10,

        /// <summary>
        /// AES in CBC mode with a random IV, then HMAC-SHA256 over each chunk. Keeps data private and rejects data that was changed, reordered, or cut short.
        /// Works on every platform.
        /// </summary>
        AES_CBC_HMAC = 11,

        /// <summary>
        /// AES in GCM mode. Keeps data private and rejects data that was changed, reordered, or cut short, faster than AES_CBC_HMAC.
        /// Needs .NET Core 3.0 or later, it throws <see cref="PlatformNotSupportedException"/> on .NET Standard.
        /// </summary>
        AES_GCM = 12,
    }
}
