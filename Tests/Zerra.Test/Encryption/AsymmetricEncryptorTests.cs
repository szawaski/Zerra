// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class AsymmetricEncryptorTests
    {
        [Fact]
        public void RoundTrip()
        {
            var keys = AsymmetricEncryptor.GenerateKey();
            Assert.NotNull(keys.PrivateKey);
            Assert.NotEqual(keys.PublicKey, keys.PrivateKey);

            const string plain = "secret é 😀";
            var encrypted = AsymmetricEncryptor.RSAEncrypt(keys.PublicKey, plain);
            Assert.NotEqual(plain, encrypted);
            Assert.NotEqual(encrypted, AsymmetricEncryptor.RSAEncrypt(keys.PublicKey, plain));
            Assert.Equal(plain, AsymmetricEncryptor.RSADecrypt(keys.PrivateKey!, encrypted));
        }

        [Fact]
        public void WrongKey_Throws()
        {
            var keys = AsymmetricEncryptor.GenerateKey();
            var other = AsymmetricEncryptor.GenerateKey();
            var encrypted = AsymmetricEncryptor.RSAEncrypt(keys.PublicKey, "secret");
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.RSADecrypt(other.PrivateKey!, encrypted));
            _ = Assert.ThrowsAny<CryptographicException>(() => AsymmetricEncryptor.RSADecrypt(keys.PublicKey, encrypted));
        }
    }
}