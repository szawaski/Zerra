// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Performs symmetric encryption and decryption with a <see cref="SymmetricAlgorithmType"/>.
    /// </summary>
    public static class SymmetricEncryptor
    {
        private static readonly byte[] defaultSalt = Encoding.UTF8.GetBytes("ενγρυπτιον"); //20 bytes
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private static readonly HashAlgorithmName defaultHashAlgorithm = HashAlgorithmName.SHA1;
        private const int defaultDeriveBytesIterations = 1000;

        //PBKDF2 output starts the same however many bytes are asked for, so a key derived here matches the key SymmetricEncryptorOld derives with its IV
        internal static byte[] DeriveBytes(string password, string? salt, int length, HashAlgorithmName? hashAlgorithm, int deriveKeyIterations)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var saltBytes = Hasher.GenerateHash(HashAlgorithmType.SHA256, passwordBytes, String.IsNullOrWhiteSpace(salt) ? defaultSalt : Encoding.UTF8.GetBytes(salt));
#if NETSTANDARD2_0
            return Pbkdf2.Derive(passwordBytes, saltBytes, deriveKeyIterations, hashAlgorithm ?? defaultHashAlgorithm, length);
#else
            return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, deriveKeyIterations, hashAlgorithm ?? defaultHashAlgorithm, length);
#endif
        }

        /// <summary>
        /// Derives a key from a password with PBKDF2. Use a long random password, a guessable one can be found by trying candidates.
        /// </summary>
        /// <param name="password">The password to derive the key from.</param>
        /// <param name="salt">An optional salt for the key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="hashAlgorithm">The hash algorithm for the derivation, default is SHA1.</param>
        /// <param name="deriveKeyIterations">The number of iterations in the derivation, default is 1000.</param>
        /// <returns>The key bytes.</returns>
        public static byte[] DeriveKey(string password, string? salt = null, SymmetricKeySize keySize = defaultKeySize, HashAlgorithmName? hashAlgorithm = null, int deriveKeyIterations = defaultDeriveBytesIterations)
            => DeriveBytes(password, salt, (int)keySize / 8, hashAlgorithm, deriveKeyIterations);

        /// <summary>
        /// Generates a new random key.
        /// </summary>
        /// <param name="keySize">The size of the key.</param>
        /// <returns>The key bytes.</returns>
        public static byte[] GenerateKey(SymmetricKeySize keySize = defaultKeySize)
        {
            var key = new byte[(int)keySize / 8];
#if NETSTANDARD2_0
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(key);
#else
            RandomNumberGenerator.Fill(key);
#endif
            return key;
        }

        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="plainData">The text to encrypt.</param>
        /// <returns>The encrypted data as Base64.</returns>
        public static string? Encrypt(SymmetricAlgorithmType algorithm, byte[] key, string? plainData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (plainData is null)
                return null;
            var plainBytes = Encoding.UTF8.GetBytes(plainData);
            var encryptedBytes = Encrypt(algorithm, key, plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }
        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(SymmetricAlgorithmType algorithm, byte[] key, byte[] plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (plainBytes is null)
                throw new ArgumentNullException(nameof(plainBytes));

            if (plainBytes.Length == 0)
                return plainBytes;
            return CryptoChunkStream.Encrypt(algorithm, key, plainBytes);
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static Span<byte> Encrypt(SymmetricAlgorithmType algorithm, byte[] key, ReadOnlySpan<byte> plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (plainBytes.Length == 0)
                return Span<byte>.Empty;
            return CryptoChunkStream.Encrypt(algorithm, key, plainBytes);
        }
#endif
        /// <summary>
        /// Performs a symmetric encryption on a stream.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="stream">The stream to encrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will encrypt the data.</returns>
        public static CryptoFlushStream Encrypt(SymmetricAlgorithmType algorithm, byte[] key, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return new CryptoFlushStream(new CryptoChunkStream(stream, algorithm, key, true, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen));
        }

        /// <summary>
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="encryptedData">The Base64 data to decrypt.</param>
        /// <returns>The decrypted text.</returns>
        public static string? Decrypt(SymmetricAlgorithmType algorithm, byte[] key, string? encryptedData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (encryptedData is null)
                return null;
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var plainBytes = Decrypt(algorithm, key, encryptedBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        /// <summary>
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(SymmetricAlgorithmType algorithm, byte[] key, byte[] encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (encryptedBytes is null)
                throw new ArgumentNullException(nameof(encryptedBytes));

            if (encryptedBytes.Length == 0)
                return encryptedBytes;
            return CryptoChunkStream.Decrypt(algorithm, key, encryptedBytes);
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static Span<byte> Decrypt(SymmetricAlgorithmType algorithm, byte[] key, ReadOnlySpan<byte> encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (encryptedBytes.Length == 0)
                return Span<byte>.Empty;
            return CryptoChunkStream.Decrypt(algorithm, key, encryptedBytes);
        }
#endif
        /// <summary>
        /// Performs a symmetric decryption on a stream.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will decrypt the data.</returns>
        public static CryptoFlushStream Decrypt(SymmetricAlgorithmType algorithm, byte[] key, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return new CryptoFlushStream(new CryptoChunkStream(stream, algorithm, key, false, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen));
        }
    }
}
