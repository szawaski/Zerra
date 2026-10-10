// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Hashes data and passwords.
    /// </summary>
    public static class Hasher
    {
        private const int saltByteLength = 16;
        private static readonly HashAlgorithmName defaultPasswordHashAlgorithm = HashAlgorithmName.SHA256;
#if NETSTANDARD2_0
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
#endif

        /// <summary>
        /// Generates a random salt.
        /// </summary>
        /// <param name="saltLength">The length of the salt in bytes.</param>
        /// <returns>The salt bytes.</returns>
        public static byte[] GenerateSaltBytes(int saltLength = saltByteLength)
        {
            var saltBytes = new byte[saltLength];
#if NETSTANDARD2_0
            lock (rng)
                rng.GetBytes(saltBytes);
#else
            RandomNumberGenerator.Fill(saltBytes);
#endif
            return saltBytes;
        }

        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmType hashAlgorithmType)
        {
            return hashAlgorithmType switch
            {
                HashAlgorithmType.SHA256 => SHA256.Create(),
                HashAlgorithmType.SHA512 => SHA512.Create(),
                HashAlgorithmType.SHA384 => SHA384.Create(),
                _ => throw new NotSupportedException($"{nameof(HashAlgorithmType)} {hashAlgorithmType} is not supported"),
            };
        }

        /// <summary>
        /// Hashes text with a salt, as Base64 of the hash followed by the salt.
        /// This is a single fast hash for checking data, don't use it for passwords, use <see cref="PBKDF2GenerateHash"/>.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plain">The text to hash.</param>
        /// <param name="salt">The salt, a random one if not given.</param>
        /// <returns>The hash and salt as Base64.</returns>
        public static string GenerateHash(HashAlgorithmType hashAlgorithmType, string plain, string? salt = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var saltBytes = salt is not null ? Encoding.UTF8.GetBytes(salt) : null;
            return Convert.ToBase64String(GenerateHash(hashAlgorithmType, plainBytes, saltBytes));
        }
        /// <summary>
        /// Hashes data with a salt, as the hash followed by the salt.
        /// This is a single fast hash for checking data, don't use it for passwords, use <see cref="PBKDF2GenerateHash"/>.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plainBytes">The data to hash.</param>
        /// <param name="saltBytes">The salt, a random one if not given.</param>
        /// <returns>The hash and salt.</returns>
        public static byte[] GenerateHash(HashAlgorithmType hashAlgorithmType, byte[] plainBytes, byte[]? saltBytes = null)
        {
            using (var hashAlgorithm = GetHashAlgorithm(hashAlgorithmType))
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
        /// <summary>
        /// Checks text against a hash from <see cref="GenerateHash(HashAlgorithmType, string, string?)"/>.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plain">The text to check.</param>
        /// <param name="hash">The hash and salt as Base64.</param>
        /// <returns>True if the text matches.</returns>
        public static bool VerifyHash(HashAlgorithmType hashAlgorithmType, string plain, string hash)
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
        /// Checks data against a hash from <see cref="GenerateHash(HashAlgorithmType, byte[], byte[])"/>.
        /// </summary>
        /// <param name="hashAlgorithmType">The hash algorithm.</param>
        /// <param name="plainBytes">The data to check.</param>
        /// <param name="hashWithSaltBytes">The hash and salt.</param>
        /// <returns>True if the data matches.</returns>
        public static bool VerifyHash(HashAlgorithmType hashAlgorithmType, byte[] plainBytes, byte[] hashWithSaltBytes)
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

                var expectedHashBytes = hashAlgorithm.ComputeHash(textBytesSalted);
                return FixedTimeEquals(expectedHashBytes, hashBytes);
            }
        }

        /// <summary>
        /// The PBKDF2 iterations used when none are given: OWASP's 2023 recommendations of 600,000 for SHA-256, 210,000 for SHA-384 and SHA-512, and 1,300,000 for SHA-1.
        /// </summary>
        /// <param name="hashAlgorithm">The hash algorithm.</param>
        /// <returns>The number of iterations.</returns>
        public static int GetDefaultPBKDF2Iterations(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return 1_300_000;
            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return 600_000;
            if (hashAlgorithm == HashAlgorithmName.SHA384 || hashAlgorithm == HashAlgorithmName.SHA512)
                return 210_000;
            throw new NotSupportedException($"PBKDF2 doesn't support {hashAlgorithm.Name}");
        }

        /// <summary>
        /// Hashes a password with PBKDF2 and a random salt, as a PHC string such as <c>$pbkdf2-sha256$i=600000$salt$hash</c>.
        /// The string holds the algorithm and iterations, so stronger settings can be used later and older hashes still checked.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="hashAlgorithm">The hash algorithm, SHA-256 if not given.</param>
        /// <param name="iterations">The iterations, <see cref="GetDefaultPBKDF2Iterations"/> if not given.</param>
        /// <returns>The PHC string to store.</returns>
        public static string PBKDF2GenerateHash(string password, HashAlgorithmName? hashAlgorithm = null, int? iterations = null)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            var algorithm = hashAlgorithm ?? defaultPasswordHashAlgorithm;
            var id = GetPBKDF2Id(algorithm) ?? throw new NotSupportedException($"PBKDF2 doesn't support {algorithm.Name}");
            var iterationCount = iterations ?? GetDefaultPBKDF2Iterations(algorithm);
            if (iterationCount < 1)
                throw new ArgumentOutOfRangeException(nameof(iterations));

            var salt = GenerateSaltBytes();
#if NETSTANDARD2_0
            var hash = Pbkdf2.Derive(Encoding.UTF8.GetBytes(password), salt, iterationCount, algorithm, GetHashLength(algorithm));
#else
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterationCount, algorithm, GetHashLength(algorithm));
#endif
            return $"${id}$i={iterationCount.ToString(CultureInfo.InvariantCulture)}${ToPhcBase64(salt)}${ToPhcBase64(hash)}";
        }

        /// <summary>
        /// Checks a password against a PHC string from <see cref="PBKDF2GenerateHash"/>.
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <param name="hash">The PHC string.</param>
        /// <returns>True if the password matches, false if it doesn't or the string isn't a PBKDF2 PHC string.</returns>
        public static bool PBKDF2VerifyHash(string password, string hash)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));
            if (!TryParsePhc(hash, out var algorithm, out var iterations, out var salt, out var expected))
                return false;

#if NETSTANDARD2_0
            var actual = Pbkdf2.Derive(Encoding.UTF8.GetBytes(password), salt, iterations, algorithm, expected.Length);
#else
            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, algorithm, expected.Length);
#endif
            return FixedTimeEquals(actual, expected);
        }

        /// <summary>
        /// Whether a stored PHC string is weaker than the settings given, so the password can be hashed again the next time it's checked.
        /// </summary>
        /// <param name="hash">The PHC string.</param>
        /// <param name="hashAlgorithm">The hash algorithm wanted, SHA-256 if not given.</param>
        /// <param name="iterations">The iterations wanted, <see cref="GetDefaultPBKDF2Iterations"/> if not given.</param>
        /// <returns>True if it uses another algorithm or fewer iterations, or isn't a PBKDF2 PHC string.</returns>
        public static bool PBKDF2NeedsRehash(string hash, HashAlgorithmName? hashAlgorithm = null, int? iterations = null)
        {
            var algorithm = hashAlgorithm ?? defaultPasswordHashAlgorithm;
            if (!TryParsePhc(hash, out var hashedAlgorithm, out var hashedIterations, out _, out _))
                return true;
            return hashedAlgorithm != algorithm || hashedIterations < (iterations ?? GetDefaultPBKDF2Iterations(algorithm));
        }

        private static string? GetPBKDF2Id(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return "pbkdf2-sha1";
            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return "pbkdf2-sha256";
            if (hashAlgorithm == HashAlgorithmName.SHA384)
                return "pbkdf2-sha384";
            if (hashAlgorithm == HashAlgorithmName.SHA512)
                return "pbkdf2-sha512";
            return null;
        }

        private static int GetHashLength(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return 20;
            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return 32;
            if (hashAlgorithm == HashAlgorithmName.SHA384)
                return 48;
            return 64;
        }

        //$<id>$i=<iterations>$<salt>$<hash>, the PHC string format, Base64 without padding
        private static bool TryParsePhc(string? hash, out HashAlgorithmName algorithm, out int iterations, out byte[] salt, out byte[] expected)
        {
            algorithm = default;
            iterations = 0;
            salt = [];
            expected = [];
            if (hash is null)
                return false;

            var parts = hash.Split('$');
            if (parts.Length != 5 || parts[0].Length != 0 || !parts[2].StartsWith("i=", StringComparison.Ordinal))
                return false;

            switch (parts[1])
            {
                case "pbkdf2-sha1": algorithm = HashAlgorithmName.SHA1; break;
                case "pbkdf2-sha256": algorithm = HashAlgorithmName.SHA256; break;
                case "pbkdf2-sha384": algorithm = HashAlgorithmName.SHA384; break;
                case "pbkdf2-sha512": algorithm = HashAlgorithmName.SHA512; break;
                default: return false;
            }
            if (!Int32.TryParse(parts[2].Substring(2), NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations < 1)
                return false;

            try
            {
                salt = FromPhcBase64(parts[3]);
                expected = FromPhcBase64(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }
            return expected.Length > 0;
        }

        private static string ToPhcBase64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

        private static byte[] FromPhcBase64(string value)
        {
            var padding = (4 - value.Length % 4) % 4;
            if (padding == 3)
                throw new FormatException();
            return Convert.FromBase64String(value + new string('=', padding));
        }

        internal static bool FixedTimeEquals(byte[] left, byte[] right)
        {
#if NETSTANDARD2_0
            if (left.Length != right.Length)
                return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
#else
            return CryptographicOperations.FixedTimeEquals(left, right);
#endif
        }
    }
}
