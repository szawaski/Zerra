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

        public static string GetSignatureAlgorithmUrl(SignatureAlgorithm algorithm)
        {
            return algorithm switch
            {
                SignatureAlgorithm.RsaSha1 => RsaSha1Url,
                SignatureAlgorithm.RsaSha224 => RsaSha224Url,
                SignatureAlgorithm.RsaSha256 => RsaSha256Url,
                SignatureAlgorithm.RsaSha384 => RsaSha384Url,
                SignatureAlgorithm.RsaSha512 => RsaSha512Url,
                SignatureAlgorithm.HmacSha1 => HmacSha1Url,
                SignatureAlgorithm.HmacSha224 => HmacSha224Url,
                SignatureAlgorithm.HmacSha256 => HmacSha256Url,
                SignatureAlgorithm.HmacSha384 => HmacSha384Url,
                SignatureAlgorithm.HmacSha512 => HmacSha512Url,
                SignatureAlgorithm.EcdsaSha1 => EcdsaSha1Url,
                SignatureAlgorithm.EcdsaSha224 => EcdsaSha224Url,
                SignatureAlgorithm.EcdsaSha256 => EcdsaSha256Url,
                SignatureAlgorithm.EcdsaSha384 => EcdsaSha384Url,
                SignatureAlgorithm.EcdsaSha512 => EcdsaSha512Url,
                _ => throw new NotImplementedException(),
            };
        }
        public static string GetSignatureAlgorithmJwt(SignatureAlgorithm algorithm)
        {
            return algorithm switch
            {
                SignatureAlgorithm.RsaSha256 => RsaSha256Jwt,
                SignatureAlgorithm.RsaSha384 => RsaSha384Jwt,
                SignatureAlgorithm.RsaSha512 => RsaSha512Jwt,
                SignatureAlgorithm.HmacSha256 => HmacSha256Jwt,
                SignatureAlgorithm.HmacSha384 => HmacSha384Jwt,
                SignatureAlgorithm.HmacSha512 => HmacSha512Jwt,
                SignatureAlgorithm.EcdsaSha256 => EcdsaSha256Jwt,
                SignatureAlgorithm.EcdsaSha384 => EcdsaSha384Jwt,
                SignatureAlgorithm.EcdsaSha512 => EcdsaSha512Jwt,
                _ => throw new NotImplementedException(),
            };
        }
        public static SignatureAlgorithm GetSignatureAlgorithmFromUrl(string url)
        {
            return url switch
            {
                RsaSha1Url => SignatureAlgorithm.RsaSha1,
                RsaSha256Url => SignatureAlgorithm.RsaSha256,
                RsaSha224Url => SignatureAlgorithm.RsaSha224,
                RsaSha384Url => SignatureAlgorithm.RsaSha384,
                RsaSha512Url => SignatureAlgorithm.RsaSha512,
                HmacSha1Url => SignatureAlgorithm.HmacSha1,
                HmacSha224Url => SignatureAlgorithm.HmacSha224,
                HmacSha256Url => SignatureAlgorithm.HmacSha256,
                HmacSha384Url => SignatureAlgorithm.HmacSha384,
                HmacSha512Url => SignatureAlgorithm.HmacSha512,
                EcdsaSha1Url => SignatureAlgorithm.EcdsaSha1,
                EcdsaSha224Url => SignatureAlgorithm.EcdsaSha224,
                EcdsaSha256Url => SignatureAlgorithm.EcdsaSha256,
                EcdsaSha384Url => SignatureAlgorithm.EcdsaSha384,
                EcdsaSha512Url => SignatureAlgorithm.EcdsaSha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }
        public static SignatureAlgorithm GetSignatureAlgorithmFromJwt(string jwtAlg)
        {
            return jwtAlg switch
            {
                RsaSha256Jwt => SignatureAlgorithm.RsaSha256,
                RsaSha384Jwt => SignatureAlgorithm.RsaSha384,
                RsaSha512Jwt => SignatureAlgorithm.RsaSha512,
                HmacSha256Jwt => SignatureAlgorithm.HmacSha256,
                HmacSha384Jwt => SignatureAlgorithm.HmacSha384,
                HmacSha512Jwt => SignatureAlgorithm.HmacSha512,
                EcdsaSha256Jwt => SignatureAlgorithm.EcdsaSha256,
                EcdsaSha384Jwt => SignatureAlgorithm.EcdsaSha384,
                EcdsaSha512Jwt => SignatureAlgorithm.EcdsaSha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", jwtAlg)),
            };
        }

        public static string GetDigestAlgorithmUrl(DigestAlgorithm algorithm)
        {
            return algorithm switch
            {
                DigestAlgorithm.Sha1 => Sha1Url,
                DigestAlgorithm.Sha224 => Sha224Url,
                DigestAlgorithm.Sha256 => Sha256Url,
                DigestAlgorithm.Sha384 => Sha384Url,
                DigestAlgorithm.Sha512 => Sha512Url,
                _ => throw new NotImplementedException(),
            };
        }
        public static DigestAlgorithm GetDigestAlgorithmFromUrl(string url)
        {
            return url switch
            {
                Sha1Url => DigestAlgorithm.Sha1,
                Sha224Url => DigestAlgorithm.Sha224,
                Sha256Url => DigestAlgorithm.Sha256,
                Sha384Url => DigestAlgorithm.Sha384,
                Sha512Url => DigestAlgorithm.Sha512,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }

        public static string GetEncryptionAlgorithmUrl(EncryptionAlgorithm algorithm)
        {
            return algorithm switch
            {
                EncryptionAlgorithm.Aes128Cbc => Aes128CbcUrl,
                EncryptionAlgorithm.Aes192Cbc => Aes192CbcUrl,
                EncryptionAlgorithm.Aes256Cbc => Aes256CbcUrl,
                EncryptionAlgorithm.Aes128Kw => Aes128KwUrl,
                EncryptionAlgorithm.Aes192Kw => Aes192KwUrl,
                EncryptionAlgorithm.Aes256Kw => Aes256KwUrl,
                EncryptionAlgorithm.DesCbc => DesCbcUrl,
                EncryptionAlgorithm.TrippleDesCbc => TrippleDesCbcUrl,
                EncryptionAlgorithm.TrippleDesKw => TrippleDesKwUrl,
                _ => throw new NotImplementedException(),
            };
        }
        public static EncryptionAlgorithm GetEncryptionAlgorithmFromUrl(string url)
        {
            return url switch
            {
                Aes128CbcUrl => EncryptionAlgorithm.Aes128Cbc,
                Aes192CbcUrl => EncryptionAlgorithm.Aes192Cbc,
                Aes256CbcUrl => EncryptionAlgorithm.Aes256Cbc,
                Aes128KwUrl => EncryptionAlgorithm.Aes128Kw,
                Aes192KwUrl => EncryptionAlgorithm.Aes192Kw,
                Aes256KwUrl => EncryptionAlgorithm.Aes256Kw,
                DesCbcUrl => EncryptionAlgorithm.DesCbc,
                TrippleDesCbcUrl => EncryptionAlgorithm.TrippleDesCbc,
                TrippleDesKwUrl => EncryptionAlgorithm.TrippleDesKw,
                _ => throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url)),
            };
        }
        
        public static SignatureDescription Create(SignatureAlgorithm algorithm)
        {
            return algorithm switch
            {
                SignatureAlgorithm.RsaSha1 => new RSAPKCS1SHA1SignatureDescription(),
                SignatureAlgorithm.RsaSha224 => throw new NotImplementedException(),
                SignatureAlgorithm.RsaSha256 => new RSAPKCS1SHA256SignatureDescription(),
                SignatureAlgorithm.RsaSha384 => new RSAPKCS1SHA384SignatureDescription(),
                SignatureAlgorithm.RsaSha512 => new RSAPKCS1SHA512SignatureDescription(),
                SignatureAlgorithm.HmacSha1 => throw new NotImplementedException(),
                SignatureAlgorithm.HmacSha224 => throw new NotImplementedException(),
                SignatureAlgorithm.HmacSha256 => throw new NotImplementedException(),
                SignatureAlgorithm.HmacSha384 => throw new NotImplementedException(),
                SignatureAlgorithm.HmacSha512 => throw new NotImplementedException(),
                SignatureAlgorithm.EcdsaSha1 => throw new NotImplementedException(),
                SignatureAlgorithm.EcdsaSha224 => throw new NotImplementedException(),
                SignatureAlgorithm.EcdsaSha256 => throw new NotImplementedException(),
                SignatureAlgorithm.EcdsaSha384 => throw new NotImplementedException(),
                SignatureAlgorithm.EcdsaSha512 => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
        public static SymmetricAlgorithm Create(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.Aes128Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 128;
                        return aes;
                    }
                case EncryptionAlgorithm.Aes192Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 192;
                        return aes;
                    }
                case EncryptionAlgorithm.Aes256Cbc:
                    {
                        var aes = Aes.Create();
                        aes.BlockSize = 128;
                        aes.KeySize = 256;
                        return aes;
                    }

                case EncryptionAlgorithm.Aes128Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");
                case EncryptionAlgorithm.Aes192Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");
                case EncryptionAlgorithm.Aes256Kw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");

                case EncryptionAlgorithm.DesCbc:
                    {
                        var des = DES.Create();
                        return des;
                    }
                case EncryptionAlgorithm.TrippleDesCbc:
                    {
                        var des = TripleDES.Create();
                        return des;
                    }
                case EncryptionAlgorithm.TrippleDesKw:
                    throw new NotImplementedException("Algorithm not implemented, probably need bouncy castle");

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
