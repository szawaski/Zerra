// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;
using System.Text;

namespace Zerra.Encryption
{
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
