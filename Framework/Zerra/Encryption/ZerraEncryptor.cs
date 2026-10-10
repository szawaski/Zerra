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
        private readonly byte[]? key;
        private readonly SymmetricKeyRing? ring;

        //stored data needs a fixed key, the repository rejects one that rotates
        internal bool HasKeySource => ring is not null;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraEncryptor"/> class with keys that rotate.
        /// Data is encrypted with the current key, and data from the current or previous key decrypts.
        /// </summary>
        /// <param name="keySource">Supplies the current key and when it expires. It's first asked when the encryptor is first used.</param>
        /// <param name="algorithm">The symmetric algorithm to use for encryption and decryption.</param>
        public ZerraEncryptor(ISymmetricKeySource keySource, SymmetricAlgorithmType algorithm)
        {
            this.algorithm = algorithm;
            this.ring = new SymmetricKeyRing(keySource ?? throw new ArgumentNullException(nameof(keySource)));
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] bytes)
        {
            if (ring is null)
                return SymmetricEncryptor.Encrypt(algorithm, key!, bytes);
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                return bytes;
            return CryptoChunkStream.Encrypt(algorithm, ring.GetState().Current, bytes);
        }

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] bytes)
        {
            if (ring is null)
                return SymmetricEncryptor.Decrypt(algorithm, key!, bytes);
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                return bytes;
            return DecryptWithRing(ring, bytes);
        }

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public Span<byte> Encrypt(ReadOnlySpan<byte> bytes)
        {
            if (ring is null)
                return SymmetricEncryptor.Encrypt(algorithm, key!, bytes);
            if (bytes.Length == 0)
                return Span<byte>.Empty;
            return CryptoChunkStream.Encrypt(algorithm, ring.GetState().Current, bytes);
        }

        /// <inheritdoc/>
        public Span<byte> Decrypt(ReadOnlySpan<byte> bytes)
        {
            if (ring is null)
                return SymmetricEncryptor.Decrypt(algorithm, key!, bytes);
            if (bytes.Length == 0)
                return Span<byte>.Empty;
            return DecryptWithRing(ring, bytes);
        }
#endif

        //the key check that starts the data picks the key
        private byte[] DecryptWithRing(SymmetricKeyRing ring, ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < CryptoChunkStream.KeyCheckSize)
                throw new CryptographicException("Encrypted data ended before its final chunk");
            return CryptoChunkStream.Decrypt(algorithm, ring.SelectKey(bytes.Slice(0, CryptoChunkStream.KeyCheckSize)), bytes);
        }

        /// <inheritdoc/>
        public CryptoFlushStream Encrypt(Stream stream, bool write)
        {
            if (ring is null)
                return SymmetricEncryptor.Encrypt(algorithm, key!, stream, write, false);
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            return new CryptoFlushStream(new CryptoChunkStream(stream, algorithm, ring, true, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, false));
        }

        /// <inheritdoc/>
        public CryptoFlushStream Decrypt(Stream stream, bool write)
        {
            if (ring is null)
                return SymmetricEncryptor.Decrypt(algorithm, key!, stream, write, false);
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            return new CryptoFlushStream(new CryptoChunkStream(stream, algorithm, ring, false, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, false));
        }
    }
}
