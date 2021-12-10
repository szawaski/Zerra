// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;
using System.Text;
using Zerra.Encryption;

namespace Zerra.Identity.Cryptography
{
    public static class RSATextSigner
    {
        static RSATextSigner()
        {
            ConfigSignatureDescriptions.Add();
        }

        public static string GenerateSignatureString(string text, RSA rsa, SignatureAlgorithm signatureAlgorithm, bool base64UrlEncoding)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return GenerateSignatureString(bytes, rsa, signatureAlgorithm, base64UrlEncoding);
        }
        public static string GenerateSignatureString(byte[] bytes, RSA rsa, SignatureAlgorithm signatureAlgorithm, bool base64UrlEncoding)
        {
            var signedBytes = GenerateSignatureBytes(bytes, rsa, signatureAlgorithm);
            var signedText = base64UrlEncoding ? Base64UrlEncoder.ToBase64String(signedBytes) : Convert.ToBase64String(signedBytes);
            return signedText;
        }
        public static byte[] GenerateSignatureBytes(byte[] bytes, RSA rsa, SignatureAlgorithm signatureAlgorithm)
        {
            var signatureDescription = Algorithms.Create(signatureAlgorithm);
            var hashAlgorithm = signatureDescription.CreateDigest();
            var formatter = signatureDescription.CreateFormatter(rsa);

            var hash = hashAlgorithm.ComputeHash(bytes);
            var signatureBytes = formatter.CreateSignature(hash);
            return signatureBytes;
        }

        public static bool Validate(string text, string signature, RSA rsa, SignatureAlgorithm signatureAlgorithm, bool base64UrlEncoding)
        {
            if (signature == null)
                return false;

            var textBytes = Encoding.UTF8.GetBytes(text);
            var signatureBytes = base64UrlEncoding ? Base64UrlEncoder.FromBase64String(signature) : Convert.FromBase64String(signature);
            var valid = Validate(textBytes, signatureBytes, rsa, signatureAlgorithm);
            return valid;
        }
        public static bool Validate(byte[] textBytes, byte[] signatureBytes, RSA rsa, SignatureAlgorithm signatureAlgorithm)
        {
            var signatureDescription = Algorithms.Create(signatureAlgorithm);
            var hashAlgorithm = signatureDescription.CreateDigest();
            var deformatter = signatureDescription.CreateDeformatter(rsa);

            var hash = hashAlgorithm.ComputeHash(textBytes);
            var valid = deformatter.VerifySignature(hash, signatureBytes);
            return valid;
        }
    }
}
