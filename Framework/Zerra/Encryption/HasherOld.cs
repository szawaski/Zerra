// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Hashes the way Zerra 5's <c>Hasher</c> did, to check hashes it stored. New hashes use <see cref="Hasher"/>:
    /// these PBKDF2 hashes use only 1,000 iterations, and the plain hashes allow MD5 and SHA-1.
    /// </summary>
    [Obsolete]
    public static class HasherOld
    {
        private const int pbkdf2HashByteSize = 64;
        private const int pbkdf2Iterations = 1000;
        private static readonly HashAlgorithmName defaultPBKDF2HashAlgorithm = HashAlgorithmName.SHA1;

        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmTypeOld hashAlgorithmType)
        {
            return hashAlgorithmType switch
            {
                HashAlgorithmTypeOld.SHA1 => SHA1.Create(),
                HashAlgorithmTypeOld.SHA256 => SHA256.Create(),
                HashAlgorithmTypeOld.SHA512 => SHA512.Create(),
                HashAlgorithmTypeOld.SHA384 => SHA384.Create(),
                HashAlgorithmTypeOld.MD5 => MD5.Create(),
                _ => throw new NotSupportedException($"{nameof(HashAlgorithmTypeOld)} {hashAlgorithmType} is not supported"),
            };
        }

        /// <summary>
        /// Hashes text with a salt as Zerra 5 did, as Base64 of the hash followed by the salt.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plain">The text to hash.</param>
        /// <param name="salt">The salt, a random one if not given.</param>
        /// <returns>The hash and salt as Base64.</returns>
        public static string GenerateHash(HashAlgorithmTypeOld hashAlgorithmType, string plain, string? salt = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var saltBytes = salt is not null ? Encoding.UTF8.GetBytes(salt) : null;
            return Convert.ToBase64String(GenerateHash(hashAlgorithmType, plainBytes, saltBytes));
        }
        /// <summary>
        /// Hashes data with a salt as Zerra 5 did, as the hash followed by the salt.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plainBytes">The data to hash.</param>
        /// <param name="saltBytes">The salt, a random one if not given.</param>
        /// <returns>The hash and salt.</returns>
        public static byte[] GenerateHash(HashAlgorithmTypeOld hashAlgorithmType, byte[] plainBytes, byte[]? saltBytes = null)
        {
            using (var hashAlgorithm = GetHashAlgorithm(hashAlgorithmType))
            {
                saltBytes ??= Hasher.GenerateSaltBytes();

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
        /// <summary>
        /// Checks text against a hash Zerra 5's <c>Hasher.GenerateHash</c> made.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plain">The text to check.</param>
        /// <param name="hash">The hash and salt as Base64.</param>
        /// <returns>True if the text matches.</returns>
        public static bool VerifyHash(HashAlgorithmTypeOld hashAlgorithmType, string plain, string hash)
        {
            if (String.IsNullOrWhiteSpace(hash))
                return false;
            byte[] hashBytes;
            try
            {
                hashBytes = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }
            return VerifyHash(hashAlgorithmType, Encoding.UTF8.GetBytes(plain), hashBytes);
        }
        /// <summary>
        /// Checks data against a hash Zerra 5's <c>Hasher.GenerateHash</c> made.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plainBytes">The data to check.</param>
        /// <param name="hashWithSaltBytes">The hash and salt.</param>
        /// <returns>True if the data matches.</returns>
        public static bool VerifyHash(HashAlgorithmTypeOld hashAlgorithmType, byte[] plainBytes, byte[] hashWithSaltBytes)
        {
            using (var hashAlgorithm = GetHashAlgorithm(hashAlgorithmType))
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

                return Hasher.FixedTimeEquals(hashAlgorithm.ComputeHash(textBytesSalted), hashBytes);
            }
        }

        /// <summary>
        /// Hashes a password with PBKDF2 as Zerra 5 did, Base64 of 64 hash bytes followed by the salt.
        /// </summary>
        /// <param name="plain">The password to hash.</param>
        /// <param name="salt">The salt, a random one if not given.</param>
        /// <param name="hashAlgorithm">The hash algorithm, SHA-1 as in Zerra 5 if not given.</param>
        /// <returns>The hash and salt as Base64.</returns>
        public static string PBKDF2GenerateHash(string plain, string? salt = null, HashAlgorithmName? hashAlgorithm = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var saltBytes = salt is not null ? Encoding.UTF8.GetBytes(salt) : null;
            return Convert.ToBase64String(PBKDF2GenerateHash(plainBytes, saltBytes, hashAlgorithm));
        }
        /// <summary>
        /// Hashes a password with PBKDF2 as Zerra 5 did, 64 hash bytes followed by the salt.
        /// </summary>
        /// <param name="plainBytes">The password to hash.</param>
        /// <param name="saltBytes">The salt, a random one if not given.</param>
        /// <param name="hashAlgorithm">The hash algorithm, SHA-1 as in Zerra 5 if not given.</param>
        /// <returns>The hash and salt.</returns>
        public static byte[] PBKDF2GenerateHash(byte[] plainBytes, byte[]? saltBytes = null, HashAlgorithmName? hashAlgorithm = null)
        {
            saltBytes ??= Hasher.GenerateSaltBytes();
#if NETSTANDARD2_0
            var hashBytes = Pbkdf2.Derive(plainBytes, saltBytes, pbkdf2Iterations, hashAlgorithm ?? defaultPBKDF2HashAlgorithm, pbkdf2HashByteSize);
#else
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(plainBytes, saltBytes, pbkdf2Iterations, hashAlgorithm ?? defaultPBKDF2HashAlgorithm, pbkdf2HashByteSize);
#endif

            //hash+salt
            var hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];
            Array.Copy(hashBytes, 0, hashWithSaltBytes, 0, hashBytes.Length);
            Array.Copy(saltBytes, 0, hashWithSaltBytes, hashBytes.Length, saltBytes.Length);
            return hashWithSaltBytes;
        }
        /// <summary>
        /// Checks a password against a hash Zerra 5's <c>Hasher.PBKDF2GenerateHash</c> made.
        /// </summary>
        /// <param name="plain">The password to check.</param>
        /// <param name="hash">The hash and salt as Base64.</param>
        /// <param name="hashAlgorithm">The hash algorithm, SHA-1 as in Zerra 5 if not given.</param>
        /// <returns>True if the password matches.</returns>
        public static bool PBKDF2VerifyHash(string plain, string hash, HashAlgorithmName? hashAlgorithm = null)
        {
            if (String.IsNullOrWhiteSpace(hash))
                return false;
            byte[] hashBytes;
            try
            {
                hashBytes = Convert.FromBase64String(hash);
            }
            catch (FormatException)
            {
                return false;
            }
            return PBKDF2VerifyHash(Encoding.UTF8.GetBytes(plain), hashBytes, hashAlgorithm);
        }
        /// <summary>
        /// Checks a password against a hash Zerra 5's <c>Hasher.PBKDF2GenerateHash</c> made.
        /// </summary>
        /// <param name="plainBytes">The password to check.</param>
        /// <param name="hashWithSaltBytes">The hash and salt.</param>
        /// <param name="hashAlgorithm">The hash algorithm, SHA-1 as in Zerra 5 if not given.</param>
        /// <returns>True if the password matches.</returns>
        public static bool PBKDF2VerifyHash(byte[] plainBytes, byte[] hashWithSaltBytes, HashAlgorithmName? hashAlgorithm = null)
        {
            if (hashWithSaltBytes.Length < pbkdf2HashByteSize)
                return false;

            //hash+salt
            var hashBytes = new byte[pbkdf2HashByteSize];
            var saltBytes = new byte[hashWithSaltBytes.Length - pbkdf2HashByteSize];
            Array.Copy(hashWithSaltBytes, 0, hashBytes, 0, hashBytes.Length);
            Array.Copy(hashWithSaltBytes, pbkdf2HashByteSize, saltBytes, 0, saltBytes.Length);

#if NETSTANDARD2_0
            var expectedHashBytes = Pbkdf2.Derive(plainBytes, saltBytes, pbkdf2Iterations, hashAlgorithm ?? defaultPBKDF2HashAlgorithm, pbkdf2HashByteSize);
#else
            var expectedHashBytes = Rfc2898DeriveBytes.Pbkdf2(plainBytes, saltBytes, pbkdf2Iterations, hashAlgorithm ?? defaultPBKDF2HashAlgorithm, pbkdf2HashByteSize);
#endif
            return Hasher.FixedTimeEquals(expectedHashBytes, hashBytes);
        }
    }
}
