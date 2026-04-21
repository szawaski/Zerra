using System.Security.Cryptography;

namespace Zerra.Encryption
{
    /// <summary>
    /// Provides symmetric encryption and decryption functionality using a configurable algorithm and key.
    /// </summary>
    public sealed class ZerraEncryptor : IEncryptor
    {
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private const SymmetricBlockSize defaultBlockSize = SymmetricBlockSize.Bits_128;
        private static readonly HashAlgorithmName defaultHashAlgorithm = HashAlgorithmName.SHA256;
        private const int defaultDeriveBytesIterations = 1000;

        private readonly SymmetricConfig config;
        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptor"/> class.
        /// </summary>
        /// <param name="key">The encryption key as a string, which will be converted to a symmetric key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="blockSize">The size of each encrypted block.</param>
        /// <param name="algorithm">The symmetric algorithm to use for encryption and decryption.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for the key derivation, default is SHA256.</param>
        /// <param name="deriveKeyIterations">The number of iterations to perform in the key derivation, default is 1000.</param>
        public ZerraEncryptor(string key, SymmetricAlgorithmType algorithm, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize, HashAlgorithmName? hashAlgorithm = null, int deriveKeyIterations = defaultDeriveBytesIterations)
        {
            var symmetricKey = SymmetricEncryptor.GetKey(key, null, keySize, blockSize, hashAlgorithm ?? defaultHashAlgorithm, deriveKeyIterations);
            config = new SymmetricConfig(algorithm, symmetricKey);
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes)
            => SymmetricEncryptor.Encrypt(config, bytes);

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes)
            => SymmetricEncryptor.Decrypt(config, bytes);

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public Span<byte> Encrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Encrypt(config, bytes);

        /// <inheritdoc/>
        public Span<byte> Decrypt(ReadOnlySpan<byte> bytes)
            => SymmetricEncryptor.Decrypt(config, bytes);
#endif

        /// <inheritdoc/>
        public CryptoFlushStream Encrypt(Stream stream, bool write)
            => SymmetricEncryptor.Encrypt(config, stream, write, false);

        /// <inheritdoc/>
        public CryptoFlushStream Decrypt(Stream stream, bool write)
            => SymmetricEncryptor.Decrypt(config, stream, write, false);
    }
}
