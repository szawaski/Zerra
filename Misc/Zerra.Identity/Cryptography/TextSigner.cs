// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;
using System.Text;
using Zerra.Encryption;

namespace Zerra.Identity.Cryptography
{
    public static class TextSigner
    {
        static TextSigner()
        {
            ConfigSignatureDescriptions.Add();
        }

        public static string GenerateSignatureString(string text, AsymmetricAlgorithm asymmetricAlgorithm, XmlSignatureAlgorithmType signatureAlgorithm, bool base64UrlEncoding)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return GenerateSignatureString(bytes, asymmetricAlgorithm, signatureAlgorithm, base64UrlEncoding);
        }
        public static string GenerateSignatureString(byte[] bytes, AsymmetricAlgorithm asymmetricAlgorithm, XmlSignatureAlgorithmType signatureAlgorithm, bool base64UrlEncoding)
        {
            var signedBytes = GenerateSignatureBytes(bytes, asymmetricAlgorithm, signatureAlgorithm);
            var signedText = base64UrlEncoding ? Base64UrlEncoder.ToBase64String(signedBytes) : Convert.ToBase64String(signedBytes);
            return signedText;
        }
        public static byte[] GenerateSignatureBytes(byte[] bytes, AsymmetricAlgorithm asymmetricAlgorithm, XmlSignatureAlgorithmType signatureAlgorithm)
        {
            var signatureDescription = Algorithms.Create(signatureAlgorithm);
            var hashAlgorithm = signatureDescription.CreateDigest();
            var formatter = signatureDescription.CreateFormatter(asymmetricAlgorithm);

            var hash = hashAlgorithm.ComputeHash(bytes);
            var signatureBytes = formatter.CreateSignature(hash);
            return signatureBytes;
        }

        public static bool Validate(string text, string signature, AsymmetricAlgorithm asymmetricAlgorithm, XmlSignatureAlgorithmType signatureAlgorithm, bool base64UrlEncoding)
        {
            if (signature == null)
                return false;

            var textBytes = Encoding.UTF8.GetBytes(text);
            var signatureBytes = base64UrlEncoding ? Base64UrlEncoder.FromBase64String(signature) : Convert.FromBase64String(signature);
            var valid = Validate(textBytes, signatureBytes, asymmetricAlgorithm, signatureAlgorithm);
            return valid;
        }
        public static bool Validate(byte[] textBytes, byte[] signatureBytes, AsymmetricAlgorithm asymmetricAlgorithm, XmlSignatureAlgorithmType signatureAlgorithm)
        {
            var signatureDescription = Algorithms.Create(signatureAlgorithm);
            var hashAlgorithm = signatureDescription.CreateDigest();
            var deformatter = signatureDescription.CreateDeformatter(asymmetricAlgorithm);

            var hash = hashAlgorithm.ComputeHash(textBytes);
            var valid = deformatter.VerifySignature(hash, signatureBytes);
            return valid;
        }
    }
}
