// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
    /// <summary>
    /// Performs asymmetric encryption and decryption.
    /// </summary>
    public static class AsynmmetricEncryptor
    {
        public static AsymmetricKeyPair GenerateKey()
        {
            using (var RSA = new RSACryptoServiceProvider())
            {
                var keys = new AsymmetricKeyPair(RSA.ToXmlString(false), RSA.ToXmlString(true));
                return keys;
            }
        }
        /// <summary>
        /// Performs an asymmetric encryption using the Rivest, Shamir, and Adleman (RSA) algorithm.
        /// </summary>
        /// <param name="publicKey">The public key shared by the encrytor and decryptor.</param>
        /// <param name="plainData">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static string RSAEncrypt(string publicKey, string plainData)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                var plainBytes = Encoding.UTF8.GetBytes(plainData);
                var encryptedBytes = rsa.Encrypt(plainBytes, true);
                var encryptedData = Convert.ToBase64String(encryptedBytes);
                return encryptedData;
            }
        }
        /// <summary>
        /// Performs an asymmetric decryption using the Rivest, Shamir, and Adleman (RSA) algorithm.
        /// </summary>
        /// <param name="privateKey">The secret private key shared by the encrytor and decryptor.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static string RSADecrypt(string privateKey, string encryptedData)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                var encryptedBytes = Convert.FromBase64String(encryptedData);
                var plainBytes = rsa.Decrypt(encryptedBytes, true);
                var plainData = Encoding.UTF8.GetString(plainBytes);
                return plainData;
            }
        }
    }
}
