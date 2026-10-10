// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Encryption
{
    /// <summary>
    /// An RSA key pair for <see cref="AsymmetricEncryptor"/>, as PEM.
    /// </summary>
    public sealed class AsymmetricKeyPair
    {
        /// <summary>
        /// The public key, as SubjectPublicKeyInfo PEM, given to whoever encrypts.
        /// </summary>
        public string PublicKey { get; }
        /// <summary>
        /// The private key, as PKCS#8 PEM, held only by whoever decrypts.
        /// </summary>
        public string? PrivateKey { get; }
        /// <summary>
        /// Creates a new key pair.
        /// </summary>
        /// <param name="publicKey">The public key as PEM.</param>
        /// <param name="privateKey">The private key as PEM, if held.</param>
        public AsymmetricKeyPair(string publicKey, string? privateKey)
        {
            this.PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
            this.PrivateKey = privateKey;
        }
    }
}
