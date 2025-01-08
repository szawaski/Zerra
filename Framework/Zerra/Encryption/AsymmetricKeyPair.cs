// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// Standard asymmetic key for encryption with public key and private key.
    /// </summary>
    public sealed class AsymmetricKeyPair
    {
        /// <summary>
        /// The public key shared with both endpoints of the asymmetric encryption.
        /// </summary>
        public string PublicKey { get; }
        /// <summary>
        /// The secret private key held only by the decrypting side of the asymmetric encryption.
        /// </summary>
        public string? PrivateKey { get; }
        /// <summary>
        /// Creates a new asymmetric key.
        /// </summary>
        /// <param name="publicKey">The public key shared with both endpoints of the asymmetric encryption.</param>
        /// <param name="privateKey">The private key hold only by the decrypting side of the asymmetric encryption.</param>
        public AsymmetricKeyPair(string publicKey, string? privateKey)
        {
            this.PublicKey = publicKey;
            this.PrivateKey = privateKey;
        }
    }
}