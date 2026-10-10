// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

namespace Zerra.Encryption
{
    /// <summary>
    /// Provides symmetric encryption and decryption the way Zerra 5 did, such as for a repository encryption provider reading data Zerra 5 stored.
    /// These modes don't detect changed data; new data uses <see cref="ZerraEncryptor"/>.
    /// </summary>
    [Obsolete]
    public sealed class ZerraEncryptorOld : IEncryptor
    {
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private const int defaultDeriveBytesIterations = 1000;

        private readonly SymmetricAlgorithmTypeOld algorithm;
        private readonly byte[] key;
        private readonly byte[] iv;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptorOld"/> class with a key derived from a password, as Zerra 5's <c>SymmetricEncryptor.GetKey</c> derived it.
        /// </summary>
        /// <param name="key">The password the key and IV are derived from.</param>
        /// <param name="algorithm">The Zerra 5 algorithm, <see cref="SymmetricAlgorithmTypeOld.AESwithShift"/> unless the data was written with another.</param>
        /// <param name="salt">The salt passed to Zerra 5's <c>GetKey</c>, if any.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="hashAlgorithm">The hash algorithm for the derivation, default is SHA1 as in Zerra 5.</param>
        /// <param name="deriveKeyIterations">The number of iterations in the derivation, default is 1000.</param>
        public ZerraEncryptorOld(string key, SymmetricAlgorithmTypeOld algorithm = SymmetricAlgorithmTypeOld.AESwithShift, string? salt = null, SymmetricKeySize keySize = defaultKeySize, HashAlgorithmName? hashAlgorithm = null, int deriveKeyIterations = defaultDeriveBytesIterations)
        {
            this.algorithm = algorithm;
            (this.key, this.iv) = SymmetricEncryptorOld.DeriveKey(key, salt, keySize, hashAlgorithm, deriveKeyIterations);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptorOld"/> class with the key and IV bytes, such as a Zerra 5 <c>SymmetricKey</c>'s.
        /// </summary>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="algorithm">The Zerra 5 algorithm, <see cref="SymmetricAlgorithmTypeOld.AESwithShift"/> unless the data was written with another.</param>
        public ZerraEncryptorOld(byte[] key, byte[] iv, SymmetricAlgorithmTypeOld algorithm = SymmetricAlgorithmTypeOld.AESwithShift)
        {
            this.algorithm = algorithm;
            this.key = key ?? throw new ArgumentNullException(nameof(key));
            this.iv = iv ?? throw new ArgumentNullException(nameof(iv));
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes)
            => SymmetricEncryptorOld.Encrypt(algorithm, key, iv, bytes);

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes)
            => SymmetricEncryptorOld.Decrypt(algorithm, key, iv, bytes);

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public Span<byte> Encrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptorOld.Encrypt(algorithm, key, iv, bytes);

        /// <inheritdoc/>
        public Span<byte> Decrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptorOld.Decrypt(algorithm, key, iv, bytes);
#endif

        /// <inheritdoc/>
        public CryptoFlushStream Encrypt(Stream stream, bool write)
            => SymmetricEncryptorOld.Encrypt(algorithm, key, iv, stream, write, false);

        /// <inheritdoc/>
        public CryptoFlushStream Decrypt(Stream stream, bool write)
            => SymmetricEncryptorOld.Decrypt(algorithm, key, iv, stream, write, false);
    }
}
