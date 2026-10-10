// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;
using Zerra.Test.NetStandard.Helpers;

namespace Zerra.Test.NetStandard.Encryption
{
    public class AsymmetricEncryptorTests
    {
        [Fact]
        public void RoundTrip()
        {
            var keys = AsymmetricEncryptor.GenerateKey();
            Assert.StartsWith("-----BEGIN PUBLIC KEY-----", keys.PublicKey);
            Assert.StartsWith("-----BEGIN PRIVATE KEY-----", keys.PrivateKey);
            Assert.Equal(256, RsaPem.Import(keys.PublicKey).Modulus!.Length);
            Assert.Equal(384, RsaPem.Import(AsymmetricEncryptor.GenerateKey(3072).PublicKey).Modulus!.Length);

            var encrypted = AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512, keys.PublicKey, "secret é");
            Assert.Equal("secret é", AsymmetricEncryptor.Decrypt(keys.PrivateKey!, encrypted));
            var large = Enumerable.Range(0, 1_000_000).Select(x => (byte)x).ToArray();
            Assert.Equal(large, AsymmetricEncryptor.DecryptBytes(keys.PrivateKey!, AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512, keys.PublicKey, large)));
        }

        [Fact]
        public void ReadsNet10()
        {
            Assert.Equal("encrypted on .NET 10", AsymmetricEncryptor.Decrypt(Net10Fixtures.Pkcs8PrivateKey, Net10Fixtures.JweFromNet10));
            Assert.Equal("encrypted on .NET 10", AsymmetricEncryptor.Decrypt(Net10Fixtures.Pkcs1PrivateKey, Net10Fixtures.JweFromNet10));
            //and .NET 10's key encrypts here
            Assert.Equal("back", AsymmetricEncryptor.Decrypt(Net10Fixtures.Pkcs8PrivateKey, AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512, Net10Fixtures.Pkcs1PublicKey, "back")));
        }

        [Fact]
        public void Changed_Throws()
        {
            var parts = Net10Fixtures.JweFromNet10.Split('.');
            for (var i = 1; i < parts.Length; i++)
            {
                var changed = (string[])parts.Clone();
                var bytes = Base64UrlEncoder.FromBase64UrlString(changed[i]);
                bytes[0] ^= 0x01;
                changed[i] = Base64UrlEncoder.ToBase64UrlString(bytes);
                _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Decrypt(Net10Fixtures.Pkcs8PrivateKey, String.Join(".", changed)));
            }
        }

        [Fact]
        public void SmallKeys_Throw()
        {
            using var small = new RSACryptoServiceProvider(1024);
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_A256CBC_HS512, RsaPem.ExportPublicKey(small.ExportParameters(false)), "secret"));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AsymmetricEncryptor.GenerateKey(1024));
        }

        [Fact]
        public void Gcm_NotSupported()
        {
            var keys = AsymmetricEncryptor.GenerateKey();
            _ = Assert.Throws<PlatformNotSupportedException>(() => AsymmetricEncryptor.Encrypt(AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM, keys.PublicKey, "secret"));
            //a header saying RSA-OAEP-256 with A256GCM
            _ = Assert.Throws<PlatformNotSupportedException>(() => AsymmetricEncryptor.Decrypt(keys.PrivateKey!, "eyJhbGciOiJSU0EtT0FFUC0yNTYiLCJlbmMiOiJBMjU2R0NNIn0.AA.AA.AA.AA"));
        }
    }
}
