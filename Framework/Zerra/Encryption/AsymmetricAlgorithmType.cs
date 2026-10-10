// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// The JSON Web Encryption (RFC 7516, 7518) algorithms <see cref="AsymmetricEncryptor"/> uses: one that encrypts a random key with RSA, and one that encrypts the data with that key.
    /// </summary>
    public enum AsymmetricAlgorithmType : byte
    {
        /// <summary>
        /// RSA-OAEP-256 for the key, A256GCM for the data. Needs .NET Core 3.0 or later, it throws <see cref="PlatformNotSupportedException"/> on .NET Standard.
        /// </summary>
        RSA_OAEP_256_A256GCM = 1,

        /// <summary>
        /// RSA-OAEP for the key, A256CBC-HS512 (AES-256-CBC with HMAC-SHA512) for the data. Works on every platform, including .NET Framework.
        /// </summary>
        RSA_OAEP_A256CBC_HS512 = 2,
    }
}
