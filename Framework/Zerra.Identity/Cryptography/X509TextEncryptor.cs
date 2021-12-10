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
            if (cert.GetRSAPublicKey() is RSA rsa)
            {
                var encryptedBytes = rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
                return encryptedBytes;
            }
            else
            {
                throw new IdentityProviderException(String.Format("Not implemented loading x509 public key type {0}", cert.GetRSAPublicKey()));
            }
        }

        public static string DecryptString(string text, X509Certificate2 cert, bool base64UrlEncoding)
        {
            var bytes = base64UrlEncoding ? Base64UrlEncoder.FromBase64String(text) : Convert.FromBase64String(text);
            var decryptedBytes = DecryptBytes(bytes, cert);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        public static byte[] DecryptBytes(byte[] bytes, X509Certificate2 cert)
        {
            if (cert.PrivateKey == null)
                throw new IdentityProviderException("x509 does not have a private key");

            if (cert.PrivateKey is RSA rsa)
            {
                var decryptedBytes = rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
                return decryptedBytes;
            }
            else
            {
                throw new IdentityProviderException(String.Format("Not implemented loading x509 private key type {0}", cert.GetRSAPublicKey().GetType()));
            }
        }
    }
}
