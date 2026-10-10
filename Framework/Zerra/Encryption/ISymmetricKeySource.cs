// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Supplies the current key for a <see cref="ZerraEncryptor"/>, so keys can be rotated without restarting services.
    /// </summary>
    /// <remarks>
    /// The encryptor asks again when the key expires, and when data fails with both the current and previous keys, at most once a minute.
    /// Data encrypted with the previous key still decrypts, so services can move to a new key at different times.
    /// </remarks>
    public interface ISymmetricKeySource
    {
        /// <summary>
        /// Gets the current key.
        /// </summary>
        /// <returns>The key bytes, 16, 24, or 32 of them, and when to ask for the key again, in UTC.</returns>
        (byte[] Key, DateTime Expiration) GetKey();

        /// <summary>
        /// Gets the current key.
        /// </summary>
        /// <returns>The key bytes, 16, 24, or 32 of them, and when to ask for the key again, in UTC.</returns>
        ValueTask<(byte[] Key, DateTime Expiration)> GetKeyAsync();
    }
}
