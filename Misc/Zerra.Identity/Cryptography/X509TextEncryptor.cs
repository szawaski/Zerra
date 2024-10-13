// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Zerra.Encryption;

namespace Zerra.Identity.Cryptography
{
    public static class X509TextEncryptor
    {
        static X509TextEncryptor()
        {
            ConfigSignatureDescriptions.Add();
        }

        public static string EncryptString(string text, X509Certificate2 cert, bool base64UrlEncoding)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encrypyedBytes = EncryptBytes(bytes, cert);
            return base64UrlEncoding ? Base64UrlEncoder.ToBase64String(encrypyedBytes) : Convert.ToBase64String(encrypyedBytes);
        }
        public static byte[] EncryptBytes(byte[] bytes, X509Certificate2 cert)
        {
            var rsa = cert.GetRSAPublicKey();
            if (rsa is null)
                throw new IdentityProviderException("X509 must be RSA");

            var encryptedBytes = rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return encryptedBytes;
        }

        public static string DecryptString(string text, X509Certificate2 cert, bool base64UrlEncoding)
        {
            var bytes = base64UrlEncoding ? Base64UrlEncoder.FromBase64String(text) : Convert.FromBase64String(text);
            var decryptedBytes = DecryptBytes(bytes, cert);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        public static byte[] DecryptBytes(byte[] bytes, X509Certificate2 cert)
        {
            var rsa = cert.GetRSAPrivateKey();
            if (rsa is null)
                throw new IdentityProviderException("X509 must be RSA");

            var decryptedBytes = rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return decryptedBytes;
        }
    }
}
