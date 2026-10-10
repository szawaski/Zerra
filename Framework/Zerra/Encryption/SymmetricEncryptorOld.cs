// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Performs symmetric encryption and decryption the way Zerra 5 did, for data it encrypted.
    /// These modes don't detect changed data; new data uses <see cref="SymmetricEncryptor"/>.
    /// </summary>
    [Obsolete]
    public static class SymmetricEncryptorOld
    {
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private const int defaultDeriveBytesIterations = 1000;
        private const int blockSize = 128;

        /// <summary>
        /// Derives a key and IV from a password the way Zerra 5's <c>SymmetricEncryptor.GetKey</c> did.
        /// </summary>
        /// <param name="password">The password to derive the key from.</param>
        /// <param name="salt">An optional salt for the key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="hashAlgorithm">The hash algorithm for the derivation, default is SHA1 as in Zerra 5.</param>
        /// <param name="deriveKeyIterations">The number of iterations in the derivation, default is 1000.</param>
        /// <returns>The key and IV bytes.</returns>
        public static (byte[] Key, byte[] IV) DeriveKey(string password, string? salt = null, SymmetricKeySize keySize = defaultKeySize, HashAlgorithmName? hashAlgorithm = null, int deriveKeyIterations = defaultDeriveBytesIterations)
        {
            var keyLength = (int)keySize / 8;
            var bytes = SymmetricEncryptor.DeriveBytes(password, salt, keyLength + blockSize / 8, hashAlgorithm, deriveKeyIterations);
            var key = new byte[keyLength];
            var iv = new byte[blockSize / 8];
            Buffer.BlockCopy(bytes, 0, key, 0, key.Length);
            Buffer.BlockCopy(bytes, key.Length, iv, 0, iv.Length);
            return (key, iv);
        }

        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="plainData">The text to encrypt.</param>
        /// <returns>The encrypted data as Base64.</returns>
        public static string? Encrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, string? plainData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            if (plainData is null)
                return null;
            var plainBytes = Encoding.UTF8.GetBytes(plainData);
            var encryptedBytes = Encrypt(algorithm, key, iv, plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }
        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, byte[] plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (plainBytes is null)
                throw new ArgumentNullException(nameof(plainBytes));

            if (plainBytes.Length == 0)
                return plainBytes;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Encrypt(algorithm, key, iv, memoryStream, true, false))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric encryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="plainBytes">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static Span<byte> Encrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, ReadOnlySpan<byte> plainBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            if (plainBytes.Length == 0)
                return Span<byte>.Empty;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Encrypt(algorithm, key, iv, memoryStream, true, false))
            {
                cryptoStream.Write(plainBytes);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
#endif
        /// <summary>
        /// Performs a symmetric encryption on a stream.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="stream">The stream to encrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will encrypt the data.</returns>
        public static CryptoFlushStream Encrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var transform = CreateTransform(algorithm, key, iv, true);

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (algorithm == SymmetricAlgorithmTypeOld.AESwithShift)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write);
                    var shiftStream = new CryptoShiftStream(cryptoStream, blockSize, CryptoStreamMode.Write, false, leaveOpen);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, blockSize, CryptoStreamMode.Read, false, leaveOpen);
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
            if (algorithm == SymmetricAlgorithmTypeOld.AESwithShift)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, blockSize, CryptoStreamMode.Write, false, false);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, blockSize, CryptoStreamMode.Read, false, leaveOpen);
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
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="encryptedData">The Base64 data to decrypt.</param>
        /// <returns>The decrypted text.</returns>
        public static string? Decrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, string? encryptedData)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            if (encryptedData is null)
                return null;
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var plainBytes = Decrypt(algorithm, key, iv, encryptedBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        /// <summary>
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, byte[] encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (encryptedBytes is null)
                throw new ArgumentNullException(nameof(encryptedBytes));

            if (encryptedBytes.Length == 0)
                return encryptedBytes;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Decrypt(algorithm, key, iv, memoryStream, true, false))
            {
                cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
#if !NETSTANDARD2_0
        /// <summary>
        /// Performs a symmetric decryption.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="encryptedBytes">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static Span<byte> Decrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, ReadOnlySpan<byte> encryptedBytes)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            if (encryptedBytes.Length == 0)
                return Span<byte>.Empty;
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = Decrypt(algorithm, key, iv, memoryStream, true, false))
            {
                cryptoStream.Write(encryptedBytes);
                cryptoStream.FlushFinalBlock();
                return memoryStream.ToArray();
            }
        }
#endif
        /// <summary>
        /// Performs a symmetric decryption on a stream.
        /// </summary>
        /// <param name="algorithm">The Zerra 5 algorithm.</param>
        /// <param name="key">The key bytes.</param>
        /// <param name="iv">The IV bytes.</param>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="write">Indicates the stream is for writing.</param>
        /// <param name="leaveOpen">Indicates if the original stream will stay open after the returning stream is closed or disposed.</param>
        /// <returns>The stream that will decrypt the data.</returns>
        public static CryptoFlushStream Decrypt(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, Stream stream, bool write, bool leaveOpen = false)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            var transform = CreateTransform(algorithm, key, iv, false);

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (algorithm == SymmetricAlgorithmTypeOld.AESwithShift)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, blockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                    var shiftStream = new CryptoShiftStream(cryptoStream, blockSize, CryptoStreamMode.Read, true, leaveOpen);
                    return new CryptoFlushStream(shiftStream, transform, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
                return new CryptoFlushStream(cryptoStream, transform, leaveOpen);
            }
#else
            if (algorithm == SymmetricAlgorithmTypeOld.AESwithShift)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, blockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write, false);
                    return new CryptoFlushStream(cryptoStream, transform, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, blockSize, CryptoStreamMode.Read, true, false);
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

        private static ICryptoTransform CreateTransform(SymmetricAlgorithmTypeOld algorithm, byte[] key, byte[] iv, bool encrypt)
        {
            if (algorithm != SymmetricAlgorithmTypeOld.AES && algorithm != SymmetricAlgorithmTypeOld.AESwithShift)
                throw new NotSupportedException($"{nameof(SymmetricAlgorithmTypeOld)} {algorithm} is not supported");

            using (var aes = Aes.Create())
            {
                aes.KeySize = key.Length * 8;
                aes.BlockSize = blockSize;
                aes.Key = key;
                aes.IV = iv;
                return encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
            }
        }
    }
}
