namespace Zerra.Encryption
{
    /// <summary>
    /// Provides symmetric encryption and decryption functionality using a configurable algorithm and key.
    /// </summary>
    public sealed class ZerraEncryptor : IEncryptor
    {
        private readonly SymmetricConfig config;
        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptor"/> class.
        /// </summary>
        /// <param name="key">The encryption key as a string, which will be converted to a symmetric key.</param>
        /// <param name="algorithm">The symmetric algorithm to use for encryption and decryption.</param>
        public ZerraEncryptor(string key, SymmetricAlgorithmType algorithm)
        {
            var symmetricKey = SymmetricEncryptor.GetKey(key);
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
