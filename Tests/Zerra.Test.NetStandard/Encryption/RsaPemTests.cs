// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;
using Zerra.Test.NetStandard.Helpers;

namespace Zerra.Test.NetStandard.Encryption
{
    public class RsaPemTests
    {
        [Fact]
        public void WritesWhatNet10Writes()
        {
            var parameters = RsaPem.Import(Net10Fixtures.Pkcs8PrivateKey);
            Assert.Equal(Net10Fixtures.SpkiPublicKey, RsaPem.ExportPublicKey(parameters));
            Assert.Equal(Net10Fixtures.Pkcs8PrivateKey, RsaPem.ExportPrivateKey(parameters));

            var pkcs1 = RsaPem.Import(Net10Fixtures.Pkcs1PrivateKey);
            Assert.Equal(parameters.Modulus, pkcs1.Modulus);
            Assert.Equal(parameters.Exponent, pkcs1.Exponent);
            Assert.Equal(parameters.D, pkcs1.D);
            Assert.Equal(parameters.P, pkcs1.P);
            Assert.Equal(parameters.Q, pkcs1.Q);
            Assert.Equal(parameters.DP, pkcs1.DP);
            Assert.Equal(parameters.DQ, pkcs1.DQ);
            Assert.Equal(parameters.InverseQ, pkcs1.InverseQ);

            foreach (var pem in new[] { Net10Fixtures.SpkiPublicKey, Net10Fixtures.Pkcs1PublicKey })
            {
                var imported = RsaPem.Import(pem);
                Assert.Equal(parameters.Modulus, imported.Modulus);
                Assert.Equal(parameters.Exponent, imported.Exponent);
                Assert.Null(imported.D);
            }
        }

        //many keys from .NET Framework's RSA, so some have values with leading zeros, and its ImportParameters is strict about their sizes
        [Fact]
        public void RoundTripsFrameworkKeys()
        {
            for (var i = 0; i < 20; i++)
            {
                using var rsa = new RSACryptoServiceProvider(i < 18 ? 2048 : 3072);
                var parameters = rsa.ExportParameters(true);

                var imported = RsaPem.Import(RsaPem.ExportPrivateKey(parameters));
                Assert.Equal(parameters.Modulus, imported.Modulus);
                Assert.Equal(parameters.D, imported.D);
                Assert.Equal(parameters.P, imported.P);
                Assert.Equal(parameters.InverseQ, imported.InverseQ);

                using var other = RSA.Create();
                other.ImportParameters(imported);
                var data = new byte[] { 1, 2, 3 };
                Assert.Equal(data, other.Decrypt(rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1), RSAEncryptionPadding.OaepSHA1));
                Assert.Equal(parameters.Modulus, RsaPem.Import(RsaPem.ExportPublicKey(parameters)).Modulus);
            }
        }

        [Fact]
        public void RejectsOtherKeys()
        {
            _ = Assert.ThrowsAny<CryptographicException>(() => RsaPem.Import(Net10Fixtures.EncryptedPrivateKey));
            _ = Assert.ThrowsAny<CryptographicException>(() => RsaPem.Import(Net10Fixtures.EcPublicKey));
            _ = Assert.ThrowsAny<CryptographicException>(() => RsaPem.Import("not a key"));
            _ = Assert.ThrowsAny<CryptographicException>(() => RsaPem.Import("-----BEGIN PUBLIC KEY-----\nAAAA\n-----END PUBLIC KEY-----"));
            _ = Assert.ThrowsAny<CryptographicException>(() => RsaPem.Import("-----BEGIN PUBLIC KEY-----\n!!!\n-----END PUBLIC KEY-----"));
        }
    }
}
