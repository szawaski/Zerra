using System.Security.Cryptography;

namespace Zerra.Encryption
{
    /// <summary>
    /// Provides symmetric encryption and decryption with a <see cref="SymmetricAlgorithmType"/> and a shared key.
    /// </summary>
    public sealed class ZerraEncryptor : IEncryptor
    {
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private static readonly HashAlgorithmName defaultHashAlgorithm = HashAlgorithmName.SHA256;
        private const int defaultDeriveBytesIterations = 1000;

        private readonly SymmetricAlgorithmType algorithm;
        private readonly byte[] key;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptor"/> class with a key derived from a password.
        /// </summary>
        /// <param name="key">The password the key is derived from, a long random one.</param>
        /// <param name="algorithm">The symmetric algorithm to use for encryption and decryption.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for the key derivation, default is SHA256.</param>
        /// <param name="deriveKeyIterations">The number of iterations to perform in the key derivation, default is 1000.</param>
        public ZerraEncryptor(string key, SymmetricAlgorithmType algorithm, SymmetricKeySize keySize = defaultKeySize, HashAlgorithmName? hashAlgorithm = null, int deriveKeyIterations = defaultDeriveBytesIterations)
        {
            this.algorithm = algorithm;
            this.key = SymmetricEncryptor.DeriveKey(key, null, keySize, hashAlgorithm ?? defaultHashAlgorithm, deriveKeyIterations);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptor"/> class with the key bytes.
        /// </summary>
        /// <param name="key">The key bytes, 16, 24, or 32 of them.</param>
        /// <param name="algorithm">The symmetric algorithm to use for encryption and decryption.</param>
        public ZerraEncryptor(byte[] key, SymmetricAlgorithmType algorithm)
        {
            this.algorithm = algorithm;
            this.key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes)
            => SymmetricEncryptor.Encrypt(algorithm, key, bytes);

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes)
            => SymmetricEncryptor.Decrypt(algorithm, key, bytes);

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public Span<byte> Encrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Encrypt(algorithm, key, bytes);

        /// <inheritdoc/>
        public Span<byte> Decrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Decrypt(algorithm, key, bytes);
#endif

        /// <inheritdoc/>
        public CryptoFlushStream Encrypt(Stream stream, bool write)
            => SymmetricEncryptor.Encrypt(algorithm, key, stream, write, false);

        /// <inheritdoc/>
        public CryptoFlushStream Decrypt(Stream stream, bool write)
            => SymmetricEncryptor.Decrypt(algorithm, key, stream, write, false);
    }
}
