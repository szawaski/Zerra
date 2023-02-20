﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    public static class SymmetricEncryptor
    {
        private static readonly byte[] defaultSalt = Encoding.UTF8.GetBytes("ενγρυπτιον"); //20 bytes
        private const SymmetricKeySize defaultKeySize = SymmetricKeySize.Bits_256;
        private const SymmetricBlockSize defaultBlockSize = SymmetricBlockSize.Bits_128;
        private static byte[] SaltFromPassword(string password, string salt = null)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var saltBytes = String.IsNullOrWhiteSpace(salt) ? defaultSalt : Encoding.UTF8.GetBytes(salt);
            var hashBytes = Hasher.GenerateHash(HashAlgoritmType.SHA256, passwordBytes, saltBytes);
            return hashBytes;
        }

        public static SymmetricKey GetKey(string password, string salt = null, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize)
        {
            var saltBytes = SaltFromPassword(password, salt);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes))
            {
                var keySizeValue = (int)keySize;
                var blockSizeValue = (int)blockSize;
                var keyBytes = deriveBytes.GetBytes(keySizeValue / 8);
                var ivBytes = deriveBytes.GetBytes(blockSizeValue / 8);

                var symmetricKey = new SymmetricKey(keyBytes, ivBytes);
                return symmetricKey;
            }
        }

        public static SymmetricKey GenerateKey(SymmetricConfig symmetricConfig, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize) { return GenerateKey(symmetricConfig.Algorithm, keySize, blockSize); }
        public static SymmetricKey GenerateKey(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKeySize keySize = defaultKeySize, SymmetricBlockSize blockSize = defaultBlockSize)
        {
            var (symmetricAlgorithm, _) = GetAlgorithm(symmetricAlgorithmType);
            symmetricAlgorithm.KeySize = (int)keySize;
            symmetricAlgorithm.BlockSize = (int)blockSize;
            symmetricAlgorithm.GenerateKey();
            symmetricAlgorithm.GenerateIV();
            var symmetricKey = new SymmetricKey(symmetricAlgorithm.Key, symmetricAlgorithm.IV);
            return symmetricKey;
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
            ;
        }

        public static string Encrypt(SymmetricConfig symmetricConfig, string plainData) { return Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainData); }
        public static byte[] Encrypt(SymmetricConfig symmetricConfig, byte[] plainBytes) { return Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainBytes); }
#if !NETSTANDARD2_0
        public static Span<byte> Encrypt(SymmetricConfig symmetricConfig, Span<byte> plainBytes) { return Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, plainBytes); }
#endif
        public static FinalBlockStream Encrypt(SymmetricConfig symmetricConfig, Stream stream, bool write, bool leaveOpen = false) { return Encrypt(symmetricConfig.Algorithm, symmetricConfig.Key, stream, write, leaveOpen); }

        public static string Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, string plainData)
        {
            if (plainData == null)
                return null;
            var plainBytes = Encoding.UTF8.GetBytes(plainData);
            var encryptedBytes = Encrypt(symmetricAlgorithmType, key, plainBytes);
            var encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }
        public static byte[] Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, byte[] plainBytes)
        {
            if (plainBytes == null)
                return null;
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
        public static Span<byte> Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Span<byte> plainBytes)
        {
            if (plainBytes == null)
                return null;
            if (plainBytes.Length == 0)
                return plainBytes;
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
        public static FinalBlockStream Encrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Stream stream, bool write, bool leaveOpen = false)
        {
            var (symmetricAlgorithm, shiftAlgorithm) = GetAlgorithm(symmetricAlgorithmType);

            symmetricAlgorithm.KeySize = key.KeySize;
            symmetricAlgorithm.BlockSize = key.BlockSize;
            symmetricAlgorithm.Key = key.Key;
            symmetricAlgorithm.IV = key.IV;

            var transform = symmetricAlgorithm.CreateEncryptor();

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Write, false, leaveOpen);
                    return new FinalBlockStream(shiftStream, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Read, false, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Read);
                    return new FinalBlockStream(cryptoStream, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
                return new FinalBlockStream(cryptoStream, leaveOpen);
            }
#else
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Write, false, false);
                    return new FinalBlockStream(shiftStream, false);
                }
                else
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Read, false, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Read, false);
                    return new FinalBlockStream(cryptoStream, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen);
                return new FinalBlockStream(cryptoStream, false);
            }
#endif
        }

        public static string Decrypt(SymmetricConfig symmetricConfig, string encryptedData) { return Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedData); }
        public static byte[] Decrypt(SymmetricConfig symmetricConfig, byte[] encryptedBytes) { return Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedBytes); }
#if !NETSTANDARD2_0
        public static Span<byte> Decrypt(SymmetricConfig symmetricConfig, Span<byte> encryptedBytes) { return Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, encryptedBytes); }
#endif
        public static FinalBlockStream Decrypt(SymmetricConfig symmetricConfig, Stream stream, bool write, bool leaveOpen = false) { return Decrypt(symmetricConfig.Algorithm, symmetricConfig.Key, stream, write, leaveOpen); }


        public static string Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, string encryptedData)
        {
            if (encryptedData == null)
                return null;
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            var plainBytes = Decrypt(symmetricAlgorithmType, key, encryptedBytes);

            var plainData = Encoding.UTF8.GetString(plainBytes);
            return plainData;
        }
        public static byte[] Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, byte[] encryptedBytes)
        {
            if (encryptedBytes == null)
                return null;
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
        public static Span<byte> Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Span<byte> encryptedBytes)
        {
            if (encryptedBytes == null)
                return null;
            if (encryptedBytes.Length == 0)
                return encryptedBytes;
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
        public static FinalBlockStream Decrypt(SymmetricAlgorithmType symmetricAlgorithmType, SymmetricKey key, Stream stream, bool write, bool leaveOpen = false)
        {
            var (symmetricAlgorithm, shiftAlgorithm) = GetAlgorithm(symmetricAlgorithmType);

            symmetricAlgorithm.KeySize = key.KeySize;
            symmetricAlgorithm.BlockSize = key.BlockSize;
            symmetricAlgorithm.Key = key.Key;
            symmetricAlgorithm.IV = key.IV;

            var transform = symmetricAlgorithm.CreateDecryptor();

            //NetStandard2.0 CryptoStream does not option leaveOpen but has no critial memory releases in dispose
#if NETSTANDARD2_0
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write);
                    return new FinalBlockStream(cryptoStream, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Read, true, leaveOpen);
                    return new FinalBlockStream(shiftStream, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
                return new FinalBlockStream(cryptoStream, leaveOpen);
            }
#else
            if (shiftAlgorithm)
            {
                if (write)
                {
                    var shiftStream = new CryptoShiftStream(stream, key.BlockSize, CryptoStreamMode.Write, true, leaveOpen);
                    var cryptoStream = new CryptoStream(shiftStream, transform, CryptoStreamMode.Write, false);
                    return new FinalBlockStream(cryptoStream, false);
                }
                else
                {
                    var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read, leaveOpen);
                    var shiftStream = new CryptoShiftStream(cryptoStream, key.BlockSize, CryptoStreamMode.Read, true, false);
                    return new FinalBlockStream(shiftStream, false);
                }
            }
            else
            {
                var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read, leaveOpen);
                return new FinalBlockStream(cryptoStream, false);
            }
#endif
        }
    }
}