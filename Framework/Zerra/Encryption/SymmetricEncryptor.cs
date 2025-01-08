// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Performs symmetric encryption and decryption.
    /// </summary>
    public static class SymmetricEncryptor
    {
        private static readonly byte[] defaultSalt = Encoding.UTF8.GetBytes("ενγρυπτιον"); //20 bytes
        private const int defaultDeriveBytesIterations = 1000;
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private const SymmetricBlockSize defaultBlockSize = SymmetricBlockSize.Bits_128;

        private static byte[] SaltFromPassword(string password, string? salt = null)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var saltBytes = String.IsNullOrWhiteSpace(salt) ? defaultSalt : Encoding.UTF8.GetBytes(salt);
            var hashBytes = Hasher.GenerateHash(HashAlgoritmType.SHA256, passwordBytes, saltBytes);
            return hashBytes;
        }

        /// <summary>
        /// Gets a symmetric key from a string password using <see cref="Rfc2898DeriveBytes"/>.
        /// </summary>
        /// <param name="password">The string to produce a symmetric key.</param>
        /// <param name="salt">An optional salt for the key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="blockSize">The size of each encrypted block.</param>
        /// <param name="iterations">The number of itterations</param>
        /// <returns>The new symmetric key</returns>
        public static SymmetricKey GetKey(string password, string? salt = null, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize, int iterations = defaultDeriveBytesIterations)
        {
            var saltBytes = SaltFromPassword(password, salt);

#if NETSTANDARD2_0
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes))
#else
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA1))
#endif
            {
                var keySizeValue = (int)keySize;
                var blockSizeValue = (int)blockSize;
                var keyBytes = deriveBytes.GetBytes(keySizeValue / 8);
                var ivBytes = deriveBytes.GetBytes(blockSizeValue / 8);

                var symmetricKey = new SymmetricKey(keyBytes, ivBytes);
                return symmetricKey;
            }
        }

        /// <summary>
        /// Generates a new random symmetric key.
        /// </summary>
        /// <param name="symmetricAlgorithmType">The algoritm for the symmetric key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="blockSize">The size of each encrypted block.</param>
        /// <returns>The new symmetric key</returns>
        public static SymmetricKey GenerateKey(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize)
        {
            var (symmetricAlgorithm, _) = GetAlgorithm(symmetricAlgorithmType);
            try
            {
                symmetricAlgorithm.KeySize = (int)keySize;
                symmetricAlgorithm.BlockSize = (int)blockSize;
                symmetricAlgorithm.GenerateKey();
                symmetricAlgorithm.GenerateIV();
                var symmetricKey = new SymmetricKey(symmetricAlgorithm.Key, symmetricAlgorithm.IV);
                return symmetricKey;
            }
            finally
            {
                symmetricAlgorithm.Dispose();
            }
        }

        private static (SymmetricAlgorithm, bool) GetAlgorithm(SymmetricAlgorithmType symmetricAlgorithmType)
        {
            return symmetricAlgorithmType switch
            {
                SymmetricAlgorithmType.AES => (Aes.Create(), false),
                SymmetricAlgorithmType.DES => (DES.Create(), false),
                SymmetricAlgorithmType.TripleDES => (TripleDES.Create(), false),
                SymmetricAlgorithmType.RC2 => (RC2.Create(), false),

                SymmetricAlgorithmType.AESwithShift => (Aes.Create(), true),
                SymmetricAlgorithmType.DESwithShift => (DES.Create(), true),
                SymmetricAlgorithmType.TripleDESwithShift => (TripleDES.Create(), true),
                SymmetricAlgorithmType.RC2withShift => (RC2.Create(), true),

                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="plainData">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static string? Encrypt(SymmetricConfig symmetricConfig, string? plainData) => Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainData);
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(SymmetricConfig symmetricConfig, byte[] plainBytes) => Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainBytes);
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static Span<byte> Encrypt(SymmetricConfig symmetricConfig, ReadOnlySpan<byte> plainBytes) => Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainBytes);
#endif
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="stream">The stream to encrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will encrypt the data.</returns>
        public static CryptoFlushStream Encrypt(SymmetricConfig symmetricConfig, Stream stream, bool write, bool leaveOpen = false) => Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, stream, write, leaveOpen);

        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric algorith type.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="plainData">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static string? Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, string? plainData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (plainData is null)
                return null;
            var plainBytes = Encoding.UTF8.GetBytes(plainData);
            var encryptedBytes = Encrypt(symmetricAlgorithmType, key, plainBytes);
            var encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric algorith type.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, byte[] plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (plainBytes is null)
                throw new ArgumentNullException(nameof(plainBytes));

            if (plainBytes.Length == 0)
                return plainBytes;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Encrypt(symmetricAlgorithmType, key, memoryStream, true, false))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();

                var encryptedBytes = memoryStream.ToArray();
                return encryptedBytes;
            }
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric algorith type.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static Span<byte> Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, ReadOnlySpan<byte> plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (plainBytes.Length == 0)
                return Span<byte>.Empty;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Encrypt(symmetricAlgorithmType, key, memoryStream, true, false))
            {
                cryptoStream.Write(plainBytes);
                cryptoStream.FlushFinalBlock();

                var encryptedBytes = memoryStream.ToArray();
                return encryptedBytes;
            }
        }
#endif
        /// <summary>
        /// Performs a symmetric encryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="stream">The stream to encrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will encrypt the data.</returns>
        public static CryptoFlushStream Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var (symmetricAlgorithm, shiftAlgorithm) = GetAlgorithm(symmetricAlgorithmType);

            ICryptoTransform transform;
            try
            {
                symmetricAlgorithm.KeySize = key.KeySize;
                symmetricAlgorithm.BlockSize = key.BlockSize;
                symmetricAlgorithm.Key = key.Key;
                symmetricAlgorithm.IV = key.IV;

                transform = symmetricAlgorithm.CreateEncryptor();
            }
            finally
            {
                symmetricAlgorithm.Dispose();
            }

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Write, false, leaveOpen);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Read, false, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Read);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
                return new CryptoFlushStream(cryptoStream, transform, leaveOpen);
            }
#else
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Write, false, false);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Read, false, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Read, false);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen);
                return new CryptoFlushStream(cryptoStream, transform, false);
            }
#endif
        }

        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static string? Decrypt(SymmetricConfig symmetricConfig, string? encryptedData) => Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedData);
        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(SymmetricConfig symmetricConfig, byte[] encryptedBytes) => Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedBytes);
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static Span<byte> Decrypt(SymmetricConfig symmetricConfig, ReadOnlySpan<byte> encryptedBytes) => Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedBytes);
#endif
        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricConfig">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will decrypt the data.</returns>
        public static CryptoFlushStream Decrypt(SymmetricConfig symmetricConfig, Stream stream, bool write, bool leaveOpen = false) => Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, stream, write, leaveOpen);

        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static string? Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, string? encryptedData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (encryptedData is null)
                return null;
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            var plainBytes = Decrypt(symmetricAlgorithmType, key, encryptedBytes);

            var plainData = Encoding.UTF8.GetString(plainBytes);
            return plainData;
        }
        public static byte[] Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, byte[] encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (encryptedBytes is null)
                throw new ArgumentNullException(nameof(encryptedBytes));

            if (encryptedBytes.Length == 0)
                return encryptedBytes;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Decrypt(symmetricAlgorithmType, key, memoryStream, true, false))
            {
                cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                cryptoStream.FlushFinalBlock();

                var plainBytes = memoryStream.ToArray();
                return plainBytes;
            }
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static Span<byte> Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, ReadOnlySpan<byte> encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (encryptedBytes.Length == 0)
                return Span<byte>.Empty;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Decrypt(symmetricAlgorithmType, key, memoryStream, true, false))
            {
                cryptoStream.Write(encryptedBytes);
                cryptoStream.FlushFinalBlock();

                var plainBytes = memoryStream.ToArray();
                return plainBytes;
            }
        }
#endif
        /// <summary>
        /// Performs a symmetric decryption
        /// </summary>
        /// <param name="symmetricAlgorithmType">The symmetric encryption information which contains the algorithm and key.</param>
        /// <param name="key">The key for encryption.</param>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will decrypt the data.</returns>
        public static CryptoFlushStream Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var (symmetricAlgorithm, shiftAlgorithm) = GetAlgorithm(symmetricAlgorithmType);

            ICryptoTransform transform;
            try
            {
                symmetricAlgorithm.KeySize = key.KeySize;
                symmetricAlgorithm.BlockSize = key.BlockSize;
                symmetricAlgorithm.Key = key.Key;
                symmetricAlgorithm.IV = key.IV;

                transform = symmetricAlgorithm.CreateDecryptor();
            }
            finally
            {
                symmetricAlgorithm.Dispose();
            }

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Read, true, leaveOpen);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
                return new CryptoFlushStream(cryptoStream, transform, leaveOpen);
            }
#else
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write, false);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Read, true, false);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen);
                return new CryptoFlushStream(cryptoStream, transform, false);
            }
#endif
        }
    }
}