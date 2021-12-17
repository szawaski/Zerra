// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Security.Cryptography;

namespace Zerra.Identity.Cryptography
{
    public static class Algorithms
    {
        public const string Sha1Url = "http://www.w3.org/2000/09/xmldsig#sha1";
        public const string Sha224Url = "http://www.w3.org/2001/04/xmlenc#sha224";
        public const string Sha256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
        public const string Sha384Url = "http://www.w3.org/2001/04/xmldsig-more#sha384";
        public const string Sha512Url = "http://www.w3.org/2001/04/xmlenc#sha512";

        public const string RsaSha1Url = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        public const string RsaSha224Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha224";
        public const string RsaSha256Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public const string RsaSha384Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
        public const string RsaSha512Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";

        public const string HmacSha1Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha1";
        public const string HmacSha224Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha224";
        public const string HmacSha256Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";
        public const string HmacSha384Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";
        public const string HmacSha512Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";

        public const string EcdsaSha1Url = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha1";
        public const string EcdsaSha224Url = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha224";
        public const string EcdsaSha256Url = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
        public const string EcdsaSha384Url = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha384";
        public const string EcdsaSha512Url = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512";

        public const string Aes128CbcUrl = "http://www.w3.org/2001/04/xmlenc#aes128-cbc";
        public const string Aes192CbcUrl = "http://www.w3.org/2001/04/xmlenc#aes192-cbc";
        public const string Aes256CbcUrl = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";

        public const string Aes128KwUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes128";
        public const string Aes192KwUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes192";
        public const string Aes256KwUrl = "http://www.w3.org/2001/04/xmlenc#kw-aes256";

        public const string DesCbcUrl = "http://www.w3.org/2001/04/xmlenc#des-cbc";
        public const string TrippleDesCbcUrl = "http://www.w3.org/2001/04/xmlenc#tripledes-cbc";
        public const string TrippleDesKwUrl = "http://www.w3.org/2001/04/xmlenc#kw-tripledes";

        public const string Rsa = "http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        public const string RsaOaep = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";

        public const string HmacSha256Jwt = "HS256";
        public const string HmacSha384Jwt = "HS384";
        public const string HmacSha512Jwt = "HS512";

        public const string RsaSha256Jwt = "RS256";
        public const string RsaSha384Jwt = "RS384";
        public const string RsaSha512Jwt = "RS512";

        public const string EcdsaSha256Jwt = "ES256";
        public const string EcdsaSha384Jwt = "ES384";
        public const string EcdsaSha512Jwt = "ES512";

        public static string GetSignatureAlgorithmUrl(XmlSignatureAlgorithmType algorithm)
        {
            return algorithm switch
            {
                XmlSignatureAlgorithmType.RsaSha1 => RsaSha1Url,
                XmlSignatureAlgorithmType.RsaSha224 => RsaSha224Url,
                XmlSignatureAlgorithmType.RsaSha256 => RsaSha256Url,
                XmlSignatureAlgorithmType.RsaSha384 => RsaSha384Url,
                XmlSignatureAlgorithmType.RsaSha512 => RsaSha512Url,
                XmlSignatureAlgorithmType.HmacSha1 => HmacSha1Url,
                XmlSignatureAlgorithmType.HmacSha224 => HmacSha224Url,
                XmlSignatureAlgorithmType.HmacSha256 => HmacSha256Url,
                XmlSignatureAlgorithmType.HmacSha384 => HmacSha384Url,
                XmlSignatureAlgorithmType.HmacSha512 => HmacSha512Url,
                XmlSignatureAlgorithmType.EcdsaSha1 => EcdsaSha1Url,
                XmlSignatureAlgorithmType.EcdsaSha224 => EcdsaSha224Url,
                XmlSignatureAlgorithmType.EcdsaSha256 => EcdsaSha256Url,
                XmlSignatureAlgorithmType.EcdsaSha384 => EcdsaSha384Url,
                XmlSignatureAlgorithmType.EcdsaSha512 => EcdsaSha512Url,
                _ => throw new NotImplementedException(),
            };
        }
        public static string GetSignatureAlgorithmJwt(XmlSignatureAlgorithmType algorithm)
        {
            return algorithm switch
            {
                XmlSignatureAlgorithmType.RsaSha256 => RsaSha256Jwt,
                XmlSignatureAlgorithmType.RsaSha384 => RsaSha384Jwt,
                XmlSignatureAlgorithmType.RsaSha512 => RsaSha512Jwt,
                XmlSignatureAlgorithmType.HmacSha256 => HmacSha256Jwt,
                XmlSignatureAlgorithmType.HmacSha384 => HmacSha384Jwt,
                XmlSignatureAlgorithmType.HmacSha512 => HmacSha512Jwt,
                XmlSignatureAlgorithmType.EcdsaSha256 => EcdsaSha256Jwt,
                XmlSignatureAlgorithmType.EcdsaSha384 => EcdsaSha384Jwt,
                XmlSignatureAlgorithmType.EcdsaSha512 => EcdsaSha512Jwt,
                _ => throw new NotImplementedException(),
            };
        }
        public static XmlSignatureAlgorithmType GetSignatureAlgorithmFromUrl(string url)
        {
            return url switch
            {
                RsaSha1Url => XmlSignatureAlgorithmType.RsaSha1,
                RsaSha256Url => XmlSignatureAlgorithmType.RsaSha256,
                RsaSha224Url => XmlSignatureAlgorithmType.RsaSha224,
                RsaSha384Url => XmlSignatureAlgorithmType.RsaSha384,
                RsaSha512Url => XmlSignatureAlgorithmType.RsaSha512,
                HmacSha1Url => XmlSignatureAlgorithmType.HmacSha1,
                HmacSha224Url => XmlSignatureAlgorithmType.HmacSha224,
                HmacSha256Url => XmlSignatureAlgorithmType.HmacSha256,
                HmacSha384Url => XmlSignatureAlgorithmType.HmacSha384,
                HmacSha512Url => XmlSignatureAlgorithmType.HmacSha512,
                EcdsaSha1Url => XmlSignatureAlgorithmType.EcdsaSha1,
                EcdsaSha224Url => XmlSignatureAlgorithmType.EcdsaSha224,
                EcdsaSha256Url => XmlSignatureAlgorithmType.EcdsaSha256,
                EcdsaSha384Url => XmlSignatureAlgorithmType.EcdsaSha384,
                EcdsaSha512Url => XmlSignatureAlgorithmType.EcdsaSha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }
        public static XmlSignatureAlgorithmType GetSignatureAlgorithmFromJwt(string jwtAlg)
        {
            return jwtAlg switch
            {
                RsaSha256Jwt => XmlSignatureAlgorithmType.RsaSha256,
                RsaSha384Jwt => XmlSignatureAlgorithmType.RsaSha384,
                RsaSha512Jwt => XmlSignatureAlgorithmType.RsaSha512,
                HmacSha256Jwt => XmlSignatureAlgorithmType.HmacSha256,
                HmacSha384Jwt => XmlSignatureAlgorithmType.HmacSha384,
                HmacSha512Jwt => XmlSignatureAlgorithmType.HmacSha512,
                EcdsaSha256Jwt => XmlSignatureAlgorithmType.EcdsaSha256,
                EcdsaSha384Jwt => XmlSignatureAlgorithmType.EcdsaSha384,
                EcdsaSha512Jwt => XmlSignatureAlgorithmType.EcdsaSha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", jwtAlg)),
            };
        }

        public static string GetDigestAlgorithmUrl(XmlDigestAlgorithmType algorithm)
        {
            return algorithm switch
            {
                XmlDigestAlgorithmType.Sha1 => Sha1Url,
                XmlDigestAlgorithmType.Sha224 => Sha224Url,
                XmlDigestAlgorithmType.Sha256 => Sha256Url,
                XmlDigestAlgorithmType.Sha384 => Sha384Url,
                XmlDigestAlgorithmType.Sha512 => Sha512Url,
                _ => throw new NotImplementedException(),
            };
        }
        public static XmlDigestAlgorithmType GetDigestAlgorithmFromUrl(string url)
        {
            return url switch
            {
                Sha1Url => XmlDigestAlgorithmType.Sha1,
                Sha224Url => XmlDigestAlgorithmType.Sha224,
                Sha256Url => XmlDigestAlgorithmType.Sha256,
                Sha384Url => XmlDigestAlgorithmType.Sha384,
                Sha512Url => XmlDigestAlgorithmType.Sha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }

        public static string GetEncryptionAlgorithmUrl(XmlEncryptionAlgorithmType algorithm)
        {
            return algorithm switch
            {
                XmlEncryptionAlgorithmType.Aes128Cbc => Aes128CbcUrl,
                XmlEncryptionAlgorithmType.Aes192Cbc => Aes192CbcUrl,
                XmlEncryptionAlgorithmType.Aes256Cbc => Aes256CbcUrl,
                XmlEncryptionAlgorithmType.Aes128Kw => Aes128KwUrl,
                XmlEncryptionAlgorithmType.Aes192Kw => Aes192KwUrl,
                XmlEncryptionAlgorithmType.Aes256Kw => Aes256KwUrl,
                XmlEncryptionAlgorithmType.DesCbc => DesCbcUrl,
                XmlEncryptionAlgorithmType.TrippleDesCbc => TrippleDesCbcUrl,
                XmlEncryptionAlgorithmType.TrippleDesKw => TrippleDesKwUrl,
                _ => throw new NotImplementedException(),
            };
        }
        public static XmlEncryptionAlgorithmType GetEncryptionAlgorithmFromUrl(string url)
        {
            return url switch
            {
                Aes128CbcUrl => XmlEncryptionAlgorithmType.Aes128Cbc,
                Aes192CbcUrl => XmlEncryptionAlgorithmType.Aes192Cbc,
                Aes256CbcUrl => XmlEncryptionAlgorithmType.Aes256Cbc,
                Aes128KwUrl => XmlEncryptionAlgorithmType.Aes128Kw,
                Aes192KwUrl => XmlEncryptionAlgorithmType.Aes192Kw,
                Aes256KwUrl => XmlEncryptionAlgorithmType.Aes256Kw,
                DesCbcUrl => XmlEncryptionAlgorithmType.DesCbc,
                TrippleDesCbcUrl => XmlEncryptionAlgorithmType.TrippleDesCbc,
                TrippleDesKwUrl => XmlEncryptionAlgorithmType.TrippleDesKw,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }
        
        public static SignatureDescription Create(XmlSignatureAlgorithmType algorithm)
        {
            return algorithm switch
            {
                XmlSignatureAlgorithmType.RsaSha1 => new RSAPKCS1SHA1SignatureDescription(),
                XmlSignatureAlgorithmType.RsaSha224 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.RsaSha256 => new RSAPKCS1SHA256SignatureDescription(),
                XmlSignatureAlgorithmType.RsaSha384 => new RSAPKCS1SHA384SignatureDescription(),
                XmlSignatureAlgorithmType.RsaSha512 => new RSAPKCS1SHA512SignatureDescription(),
                XmlSignatureAlgorithmType.HmacSha1 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.HmacSha224 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.HmacSha256 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.HmacSha384 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.HmacSha512 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.EcdsaSha1 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.EcdsaSha224 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.EcdsaSha256 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.EcdsaSha384 => throw new NotImplementedException(),
                XmlSignatureAlgorithmType.EcdsaSha512 => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
        public static SymmetricAlgorithm Create(XmlEncryptionAlgorithmType algorithm)
        {
            switch (algorithm)
            {
                case XmlEncryptionAlgorithmType.Aes128Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 128;
                        return aes;
                    }
                case XmlEncryptionAlgorithmType.Aes192Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 192;
                        return aes;
                    }
                case XmlEncryptionAlgorithmType.Aes256Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 256;
                        return aes;
                    }

                case XmlEncryptionAlgorithmType.Aes128Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");
                case XmlEncryptionAlgorithmType.Aes192Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");
                case XmlEncryptionAlgorithmType.Aes256Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");

                case XmlEncryptionAlgorithmType.DesCbc:
                    {
                        var des = DES.Create();
                        return des;
                    }
                case XmlEncryptionAlgorithmType.TrippleDesCbc:
                    {
                        var des = TripleDES.Create();
                        return des;
                    }
                case XmlEncryptionAlgorithmType.TrippleDesKw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
