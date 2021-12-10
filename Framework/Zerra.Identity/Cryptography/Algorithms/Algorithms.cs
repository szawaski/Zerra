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
            switch (algorithm)
            {
                case SignatureAlgorithm.RsaSha1:
                    return RsaSha1Url;
                case SignatureAlgorithm.RsaSha224:
                    return RsaSha224Url;
                case SignatureAlgorithm.RsaSha256:
                    return RsaSha256Url;
                case SignatureAlgorithm.RsaSha384:
                    return RsaSha384Url;
                case SignatureAlgorithm.RsaSha512:
                    return RsaSha512Url;

                case SignatureAlgorithm.HmacSha1:
                    return HmacSha1Url;
                case SignatureAlgorithm.HmacSha224:
                    return HmacSha224Url;
                case SignatureAlgorithm.HmacSha256:
                    return HmacSha256Url;
                case SignatureAlgorithm.HmacSha384:
                    return HmacSha384Url;
                case SignatureAlgorithm.HmacSha512:
                    return HmacSha512Url;

                case SignatureAlgorithm.EcdsaSha1:
                    return EcdsaSha1Url;
                case SignatureAlgorithm.EcdsaSha224:
                    return EcdsaSha224Url;
                case SignatureAlgorithm.EcdsaSha256:
                    return EcdsaSha256Url;
                case SignatureAlgorithm.EcdsaSha384:
                    return EcdsaSha384Url;
                case SignatureAlgorithm.EcdsaSha512:
                    return EcdsaSha512Url;

                default:
                    throw new NotImplementedException();
            }
        }
        public static string GetSignatureAlgorithmJwt(SignatureAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SignatureAlgorithm.RsaSha256:
                    return RsaSha256Jwt;
                case SignatureAlgorithm.RsaSha384:
                    return RsaSha384Jwt;
                case SignatureAlgorithm.RsaSha512:
                    return RsaSha512Jwt;

                case SignatureAlgorithm.HmacSha256:
                    return HmacSha256Jwt;
                case SignatureAlgorithm.HmacSha384:
                    return HmacSha384Jwt;
                case SignatureAlgorithm.HmacSha512:
                    return HmacSha512Jwt;

                case SignatureAlgorithm.EcdsaSha256:
                    return EcdsaSha256Jwt;
                case SignatureAlgorithm.EcdsaSha384:
                    return EcdsaSha384Jwt;
                case SignatureAlgorithm.EcdsaSha512:
                    return EcdsaSha512Jwt;

                default:
                    throw new NotImplementedException();
            }
        }
        public static SignatureAlgorithm GetSignatureAlgorithmFromUrl(string url)
        {
            switch (url)
            {
                case RsaSha1Url:
                    return SignatureAlgorithm.RsaSha1;
                case RsaSha256Url:
                    return SignatureAlgorithm.RsaSha256;
                case RsaSha224Url:
                    return SignatureAlgorithm.RsaSha224;
                case RsaSha384Url:
                    return SignatureAlgorithm.RsaSha384;
                case RsaSha512Url:
                    return SignatureAlgorithm.RsaSha512;

                case HmacSha1Url:
                    return SignatureAlgorithm.HmacSha1;
                case HmacSha224Url:
                    return SignatureAlgorithm.HmacSha224;
                case HmacSha256Url:
                    return SignatureAlgorithm.HmacSha256;
                case HmacSha384Url:
                    return SignatureAlgorithm.HmacSha384;
                case HmacSha512Url:
                    return SignatureAlgorithm.HmacSha512;

                case EcdsaSha1Url:
                    return SignatureAlgorithm.EcdsaSha1;
                case EcdsaSha224Url:
                    return SignatureAlgorithm.EcdsaSha224;
                case EcdsaSha256Url:
                    return SignatureAlgorithm.EcdsaSha256;
                case EcdsaSha384Url:
                    return SignatureAlgorithm.EcdsaSha384;
                case EcdsaSha512Url:
                    return SignatureAlgorithm.EcdsaSha512;

                default:
                    throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url));
            }
        }
        public static SignatureAlgorithm GetSignatureAlgorithmFromJwt(string jwtAlg)
        {
            switch (jwtAlg)
            {
                case RsaSha256Jwt:
                    return SignatureAlgorithm.RsaSha256;
                case RsaSha384Jwt:
                    return SignatureAlgorithm.RsaSha384;
                case RsaSha512Jwt:
                    return SignatureAlgorithm.RsaSha512;

                case HmacSha256Jwt:
                    return SignatureAlgorithm.HmacSha256;
                case HmacSha384Jwt:
                    return SignatureAlgorithm.HmacSha384;
                case HmacSha512Jwt:
                    return SignatureAlgorithm.HmacSha512;

                case EcdsaSha256Jwt:
                    return SignatureAlgorithm.EcdsaSha256;
                case EcdsaSha384Jwt:
                    return SignatureAlgorithm.EcdsaSha384;
                case EcdsaSha512Jwt:
                    return SignatureAlgorithm.EcdsaSha512;

                default:
                    throw new ArgumentException(String.Format("Algorithm not recoginized {0}", jwtAlg));
            }
        }

        public static string GetDigestAlgorithmUrl(DigestAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case DigestAlgorithm.Sha1:
                    return Sha1Url;
                case DigestAlgorithm.Sha224:
                    return Sha224Url;
                case DigestAlgorithm.Sha256:
                    return Sha256Url;
                case DigestAlgorithm.Sha384:
                    return Sha384Url;
                case DigestAlgorithm.Sha512:
                    return Sha512Url;
                default:
                    throw new NotImplementedException();
            }
        }
        public static DigestAlgorithm GetDigestAlgorithmFromUrl(string url)
        {
            switch (url)
            {
                case Sha1Url:
                    return DigestAlgorithm.Sha1;
                case Sha224Url:
                    return DigestAlgorithm.Sha224;
                case Sha256Url:
                    return DigestAlgorithm.Sha256;
                case Sha384Url:
                    return DigestAlgorithm.Sha384;
                case Sha512Url:
                    return DigestAlgorithm.Sha512;
                default:
                    throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url));
            }
        }

        public static string GetEncryptionAlgorithmUrl(EncryptionAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case EncryptionAlgorithm.Aes128Cbc:
                    return Aes128CbcUrl;
                case EncryptionAlgorithm.Aes192Cbc:
                    return Aes192CbcUrl;
                case EncryptionAlgorithm.Aes256Cbc:
                    return Aes256CbcUrl;

                case EncryptionAlgorithm.Aes128Kw:
                    return Aes128KwUrl;
                case EncryptionAlgorithm.Aes192Kw:
                    return Aes192KwUrl;
                case EncryptionAlgorithm.Aes256Kw:
                    return Aes256KwUrl;

                case EncryptionAlgorithm.DesCbc:
                    return DesCbcUrl;
                case EncryptionAlgorithm.TrippleDesCbc:
                    return TrippleDesCbcUrl;
                case EncryptionAlgorithm.TrippleDesKw:
                    return TrippleDesKwUrl;

                default:
                    throw new NotImplementedException();
            }
        }
        public static EncryptionAlgorithm GetEncryptionAlgorithmFromUrl(string url)
        {
            switch (url)
            {
                case Aes128CbcUrl:
                    return EncryptionAlgorithm.Aes128Cbc;
                case Aes192CbcUrl:
                    return EncryptionAlgorithm.Aes192Cbc;
                case Aes256CbcUrl:
                    return EncryptionAlgorithm.Aes256Cbc;

                case Aes128KwUrl:
                    return EncryptionAlgorithm.Aes128Kw;
                case Aes192KwUrl:
                    return EncryptionAlgorithm.Aes192Kw;
                case Aes256KwUrl:
                    return EncryptionAlgorithm.Aes256Kw;

                case DesCbcUrl:
                    return EncryptionAlgorithm.DesCbc;
                case TrippleDesCbcUrl:
                    return EncryptionAlgorithm.TrippleDesCbc;
                case TrippleDesKwUrl:
                    return EncryptionAlgorithm.TrippleDesKw;

                default:
                    throw new ArgumentException(String.Format("Algorithm not recoginized {0}", url));
            }
        }
        
        public static SignatureDescription Create(SignatureAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SignatureAlgorithm.RsaSha1:
                    return new RSAPKCS1SHA1SignatureDescription();
                case SignatureAlgorithm.RsaSha224:
                    throw new NotImplementedException();
                case SignatureAlgorithm.RsaSha256:
                    return new RSAPKCS1SHA256SignatureDescription();
                case SignatureAlgorithm.RsaSha384:
                    return new RSAPKCS1SHA384SignatureDescription();
                case SignatureAlgorithm.RsaSha512:
                    return new RSAPKCS1SHA512SignatureDescription();

                case SignatureAlgorithm.HmacSha1:
                    throw new NotImplementedException();
                case SignatureAlgorithm.HmacSha224:
                    throw new NotImplementedException();
                case SignatureAlgorithm.HmacSha256:
                    throw new NotImplementedException();
                case SignatureAlgorithm.HmacSha384:
                    throw new NotImplementedException();
                case SignatureAlgorithm.HmacSha512:
                    throw new NotImplementedException();

                case SignatureAlgorithm.EcdsaSha1:
                    throw new NotImplementedException();
                case SignatureAlgorithm.EcdsaSha224:
                    throw new NotImplementedException();
                case SignatureAlgorithm.EcdsaSha256:
                    throw new NotImplementedException();
                case SignatureAlgorithm.EcdsaSha384:
                    throw new NotImplementedException();
                case SignatureAlgorithm.EcdsaSha512:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
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
