﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    public static class Hasher
    {
        private const int saltByteLength = 16;
        public static byte[] GenerateSaltBytes(int saltLength = saltByteLength)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var saltBytes = new byte[saltLength];
                rng.GetNonZeroBytes(saltBytes);
                return saltBytes;
            }
        }

        private static HashAlgorithm GetHashAlgorithm(HashAlgoritmType hashAlgoritmType)
        {
            return hashAlgoritmType switch
            {
                HashAlgoritmType.SHA1 => SHA1.Create(),
                HashAlgoritmType.SHA256 => SHA256.Create(),
                HashAlgoritmType.SHA512 => SHA512.Create(),
                HashAlgoritmType.SHA384 => SHA384.Create(),
                HashAlgoritmType.MD5 => MD5.Create(),
                _ => throw new NotImplementedException(),
            };
        }

        public static string GenerateHash(HashAlgoritmType hashAlgoritmType, string plain, string salt = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var saltBytes = salt != null ? Encoding.UTF8.GetBytes(salt) : null;
            var hashedBytes = GenerateHash(hashAlgoritmType, plainBytes, saltBytes);
            var hash = Convert.ToBase64String(hashedBytes);
            return hash;
        }
        public static byte[] GenerateHash(HashAlgoritmType hashAlgoritmType, byte[] plainBytes, byte[] saltBytes = null)
        {
            using (var hashAlgorithm = GetHashAlgorithm(hashAlgoritmType))
            {
                saltBytes ??= GenerateSaltBytes();

                //plain+salt
                var textBytesSalted = new byte[plainBytes.Length + saltBytes.Length];
                Array.Copy(plainBytes, 0, textBytesSalted, 0, plainBytes.Length);
                Array.Copy(saltBytes, 0, textBytesSalted, plainBytes.Length, saltBytes.Length);

                var hashBytes = hashAlgorithm.ComputeHash(textBytesSalted);

                //hash+salt
                var hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];
                Array.Copy(hashBytes, 0, hashWithSaltBytes, 0, hashBytes.Length);
                Array.Copy(saltBytes, 0, hashWithSaltBytes, hashBytes.Length, saltBytes.Length);

                return hashWithSaltBytes;
            }
        }
        public static bool VerifyHash(HashAlgoritmType hashAlgoritmType, string plain, string hash)
        {
            if (String.IsNullOrWhiteSpace(hash))
                return false;
            try
            {
                var hashBytes = Convert.FromBase64String(hash);
                var plainBytes = Encoding.UTF8.GetBytes(plain);
                return VerifyHash(hashAlgoritmType, plainBytes, hashBytes);
            }
            catch
            {
                return false;
            }
        }
        public static bool VerifyHash(HashAlgoritmType hashAlgoritmType, byte[] plainBytes, byte[] hashWithSaltBytes)
        {
            using (var hashAlgorithm = GetHashAlgorithm(hashAlgoritmType))
            {
                var hashSizeBytes = hashAlgorithm.HashSize / 8;
                if (hashWithSaltBytes.Length < hashSizeBytes)
                    return false;

                //hash+salt
                var hashBytes = new byte[hashSizeBytes];
                var saltBytes = new byte[hashWithSaltBytes.Length - hashSizeBytes];
                Array.Copy(hashWithSaltBytes, 0, hashBytes, 0, hashBytes.Length);
                Array.Copy(hashWithSaltBytes, hashSizeBytes, saltBytes, 0, saltBytes.Length);

                //plain+salt
                var textBytesSalted = new byte[plainBytes.Length + saltBytes.Length];
                Array.Copy(plainBytes, 0, textBytesSalted, 0, plainBytes.Length);
                Array.Copy(saltBytes, 0, textBytesSalted, plainBytes.Length, saltBytes.Length);

                var expectedHashBytes = hashAlgorithm.ComputeHash(textBytesSalted);

                return expectedHashBytes.SequenceEqual(hashBytes);
            }
        }

        private const int pbkdf2HashByteSize = 64;
        private const int rfc2898HashItterations = 1000; //default per source code
        public static string PBKDF2GenerateHash(string plain, string salt = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var saltBytes = salt != null ? Encoding.UTF8.GetBytes(salt) : null;
            var hashedBytes = PBKDF2GenerateHash(plainBytes, saltBytes);
            var hash = Convert.ToBase64String(hashedBytes);
            return hash;
        }
        public static byte[] PBKDF2GenerateHash(byte[] plainBytes, byte[] saltBytes = null)
        {
            saltBytes ??= GenerateSaltBytes();

            using (var deriveBytes = new Rfc2898DeriveBytes(plainBytes, saltBytes, rfc2898HashItterations))
            {
                var hashBytes = deriveBytes.GetBytes(pbkdf2HashByteSize);

                //hash+salt
                var hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];
                Array.Copy(hashBytes, 0, hashWithSaltBytes, 0, hashBytes.Length);
                Array.Copy(saltBytes, 0, hashWithSaltBytes, hashBytes.Length, saltBytes.Length);

                return hashWithSaltBytes;
            }
        }
        public static bool PBKDF2VerifyHash(string plain, string hash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(hash);
                var plainBytes = Encoding.UTF8.GetBytes(plain);
                return PBKDF2VerifyHash(plainBytes, hashBytes);
            }
            catch
            {
                return false;
            }
        }
        public static bool PBKDF2VerifyHash(byte[] plainBytes, byte[] hashWithSaltBytes)
        {
            if (hashWithSaltBytes.Length < pbkdf2HashByteSize)
                return false;

            //hash+salt
            var hashBytes = new byte[pbkdf2HashByteSize];
            var saltBytes = new byte[hashWithSaltBytes.Length - pbkdf2HashByteSize];
            Array.Copy(hashWithSaltBytes, 0, hashBytes, 0, hashBytes.Length);
            Array.Copy(hashWithSaltBytes, pbkdf2HashByteSize, saltBytes, 0, saltBytes.Length);

            using (var deriveBytes = new Rfc2898DeriveBytes(plainBytes, saltBytes, rfc2898HashItterations))
            {
                var expectedHashBytes = deriveBytes.GetBytes(pbkdf2HashByteSize);

                return expectedHashBytes.SequenceEqual(hashBytes);
            }
        }
    }
}
