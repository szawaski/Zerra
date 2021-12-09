// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Identity.Cryptography
{
    public class SymmetricKey
    {
        public byte[] Key { get; private set; }
        public byte[] IV { get; private set; }
        public int KeySize { get; private set; }
        public int BlockSize { get; private set; }
        public SymmetricKey(byte[] key, byte[] iv, int keySize, int blockSize)
        {
            this.Key = key;
            this.IV = iv;
            this.KeySize = keySize;
            this.BlockSize = blockSize;
            Validate();
        }
        public SymmetricKey(byte[] key, byte[] iv, SymmetricKeySize keySize, SymmetricBlockSize blockSize)
        {
            this.Key = key;
            this.IV = iv;
            this.KeySize = (int)keySize;
            this.BlockSize = (int)blockSize;
            Validate();
        }
        private void Validate()
        {
            if (this.Key.Length != this.KeySize / 8)
                throw new ArgumentException("Invalid Key for the KeySize");
            if (this.IV.Length != this.BlockSize / 8)
                throw new ArgumentException("Invalid IV for the BlockSize");
        }
    }

    public class AsymmetricKeyPair
    {
        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }
        public AsymmetricKeyPair(string publicKey, string privateKey)
        {
            this.PublicKey = publicKey;
            this.PrivateKey = privateKey;
        }
    }

    public enum SymmetricBlockSize
    {
        Bits_128_AES = 128,
        Bits_192 = 192, //Not Supported in .NetCore/.NetStandard
        Bits_256 = 256  //Not Supported in .NetCore/.NetStandard
    }

    public enum SymmetricKeySize
    {
        Bits_128_AES = 128,
        Bits_192_AES = 192,
        Bits_256_AES = 256
    }

    public static class Encryption
    {
        private const SymmetricKeySize defaultRijndaelKeySize = SymmetricKeySize.Bits_256_AES;
        private const SymmetricBlockSize defaultRijndaelBlockSize = SymmetricBlockSize.Bits_128_AES;

        private static string SaltFromPassword(string password)
        {
            return "ωερρα" + (Math.Pow(password.Length, .02)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static SymmetricKey GetSymmetricKey(string password, string salt = null, SymmetricKeySize keySize = defaultRijndaelKeySize, SymmetricBlockSize blockSize = defaultRijndaelBlockSize)
        {
            if (String.IsNullOrWhiteSpace(salt))
                salt = SaltFromPassword(password);
            byte[] saltBytes = Encoding.ASCII.GetBytes(salt);

            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, saltBytes);

            int keySizeValue = (int)keySize;
            int blockSizeValue = (int)blockSize;
            byte[] keyBytes = pdb.GetBytes(keySizeValue / 8);
            byte[] ivBytes = pdb.GetBytes(blockSizeValue / 8);

            SymmetricKey symmetricKey = new SymmetricKey(keyBytes, ivBytes, keySizeValue, blockSizeValue);
            return symmetricKey;
        }
        public static AsymmetricKeyPair GetAsymmetricKey()
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            AsymmetricKeyPair keys = new AsymmetricKeyPair(RSA.ToXmlString(false), RSA.ToXmlString(true));
            RSA.Clear();
            return keys;
        }

        public static SymmetricKey RijndaelGenerateKey(SymmetricKeySize keySize = defaultRijndaelKeySize, SymmetricBlockSize blockSize = defaultRijndaelBlockSize)
        {
            RijndaelManaged rijndael = new RijndaelManaged();
            rijndael.KeySize = (int)keySize;
            rijndael.BlockSize = (int)blockSize;
            rijndael.GenerateKey();
            rijndael.GenerateIV();
            SymmetricKey symmetricKey = new SymmetricKey(rijndael.Key, rijndael.IV, rijndael.KeySize, rijndael.BlockSize);
            return symmetricKey;
        }

        public static string RijndaelEncrypt(SymmetricKey key, string plainData)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainData);
            var encryptedBytes = RijndaelEncrypt(key, plainBytes);
            string encryptedData = Convert.ToBase64String(encryptedBytes);
            return encryptedData;
        }
        public static byte[] RijndaelEncrypt(SymmetricKey key, byte[] plainBytes)
        {
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = RijndaelEncrypt(key, memoryStream, true))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.Flush();
                cryptoStream.FlushFinalBlock();

                var encryptedBytes = memoryStream.ToArray();
                return encryptedBytes;
            }
        }
        public static CryptoStream RijndaelEncrypt(SymmetricKey key, Stream stream, bool write)
        {
            var rijndael = new RijndaelManaged
            {
                KeySize = key.KeySize,
                BlockSize = key.BlockSize,
                Key = key.Key,
                IV = key.IV
            };

            var transform = rijndael.CreateEncryptor();
            var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);
            return cryptoStream;
        }
        public static long RijndaelEncryptByteLength(long plainByteLength)
        {
            return ((plainByteLength + 16) / 16) * 16;
        }

        public static string RijndaelDecrypt(SymmetricKey key, string encryptedData)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

            var plainBytes = RijndaelDecrypt(key, encryptedBytes);

            string plainData = Encoding.UTF8.GetString(plainBytes);
            return plainData;
        }
        public static byte[] RijndaelDecrypt(SymmetricKey key, byte[] encryptedBytes)
        {
            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = RijndaelDecrypt(key, memoryStream, true))
            {
                cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                cryptoStream.Flush();
                cryptoStream.FlushFinalBlock();

                var plainBytes = memoryStream.ToArray();
                return plainBytes;
            }
        }
        public static CryptoStream RijndaelDecrypt(SymmetricKey key, Stream stream, bool write)
        {
            var rijndael = new RijndaelManaged
            {
                KeySize = key.KeySize,
                BlockSize = key.BlockSize,
                Key = key.Key,
                IV = key.IV
            };

            var transform = rijndael.CreateDecryptor();
            var cryptoStream = new CryptoStream(stream, transform, write ? CryptoStreamMode.Write : CryptoStreamMode.Read);

            return cryptoStream;
        }
        public static long RijndaelDecryptMaxByteLength(long encryptedByteLength)
        {
            return ((encryptedByteLength / 16) * 16) - 16;
        }

        public static string RSAEncrypt(string publicKey, string plainData)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainData);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes, true);
                string encryptedData = Convert.ToBase64String(encryptedBytes);
                return encryptedData;
            }
        }
        public static string RSADecrypt(string privateKey, string encryptedData)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
                byte[] plainBytes = rsa.Decrypt(encryptedBytes, true);
                string plainData = Encoding.UTF8.GetString(plainBytes);

                rsa.Clear();
                return plainData;
            }
        }

        public static byte[] GenerateSaltBytes(int minSize = 24, int maxSize = 48)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            Random random = GetRandomizer(rng);
            int saltSize = random.Next(minSize, maxSize);
            byte[] saltBytes = new byte[saltSize];
            rng.GetNonZeroBytes(saltBytes);
            return saltBytes;
        }
        public static string GenerateSaltString(int minSize = 24, int maxSize = 48)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            Random random = GetRandomizer(rng);
            int saltSize = random.Next(minSize, maxSize);

            byte[] strBytes = new byte[saltSize];
            rng.GetBytes(strBytes);

            return Convert.ToBase64String(strBytes);
        }

        private static readonly char[] passwordCharactersUpper = "ABCDEFGHIJKLMNOPQRSTUVWXUYZ".ToCharArray();
        private static readonly char[] passwordCharactersLower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] passwordCharactersNumeric = "0123456789".ToCharArray();
        private static readonly char[] passwordCharactersSpecial = "!@#$%^&*?+-|.,:~".ToCharArray();
        public static string GeneratePassword(int length, bool upperCase, bool lowerCase, bool numeric, bool special)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                Random random = GetRandomizer(rng);

                List<char> passwordChars = new List<char>();
                if (upperCase)
                    passwordChars.AddRange(passwordCharactersUpper);
                if (lowerCase)
                    passwordChars.AddRange(passwordCharactersLower);
                if (numeric)
                    passwordChars.AddRange(passwordCharactersNumeric);
                if (special)
                    passwordChars.AddRange(passwordCharactersSpecial);

                char[] chars = new char[length];
                for (int x = 0; x < length; x++)
                {
                    chars[x] = passwordChars[random.Next(passwordChars.Count - 1)];
                }
                return new String(chars);
            }
        }

        private static Random GetRandomizer(RNGCryptoServiceProvider rng)
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int seed = (randomBytes[0] & 0x7f) << 24 | randomBytes[1] << 16 | randomBytes[2] << 8 | randomBytes[3];
            return new Random(seed);
        }

        public static string SHA512GenerateHash(string text) { return SHA512GenerateHash(text, (string)null); }
        public static string SHA512GenerateHash(string text, string salt)
        {
            return SHA512GenerateHash(text, salt != null ? Encoding.UTF8.GetBytes(salt) : null);
        }
        private static string SHA512GenerateHash(string text, byte[] salt)
        {
            if (salt == null)
                salt = GenerateSaltBytes();

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] textBytesSalted = new byte[textBytes.Length + salt.Length];

            for (int i = 0; i < textBytes.Length; i++)
                textBytesSalted[i] = textBytes[i];

            for (int i = 0; i < salt.Length; i++)
                textBytesSalted[textBytes.Length + i] = salt[i];

            using (var hash = new SHA512Managed())
            {
                byte[] hashBytes = hash.ComputeHash(textBytesSalted);

                byte[] hashWithSaltBytes = new byte[hashBytes.Length + salt.Length];

                for (int i = 0; i < hashBytes.Length; i++)
                    hashWithSaltBytes[i] = hashBytes[i];

                for (int i = 0; i < salt.Length; i++)
                    hashWithSaltBytes[hashBytes.Length + i] = salt[i];

                string hashValue = Convert.ToBase64String(hashWithSaltBytes);

                return hashValue;
            }
        }
        private const int sha512HashSize = 64;
        public static bool SHA512VerifyHash(string text, string hashedText)
        {
            byte[] hashBytesSalted = null;
            try
            {
                hashBytesSalted = Convert.FromBase64String(hashedText);
            }
            catch
            {
                return false;
            }

            if (hashBytesSalted.Length < sha512HashSize)
                return false;

            byte[] saltBytes = new byte[hashBytesSalted.Length - sha512HashSize];

            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashBytesSalted[sha512HashSize + i];

            string expectedHashString = SHA512GenerateHash(text, saltBytes);

            return (hashedText == expectedHashString);
        }
    }
}
