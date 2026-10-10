// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;
using Xunit;
using Zerra.Encryption;
using Zerra.Test.NetStandard.Helpers;

namespace Zerra.Test.NetStandard.Encryption
{
    public class SymmetricEncryptionTest
    {
        private static readonly byte[] fixedKey = Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();
        private static readonly SymmetricAlgorithmType[] algorithms = [SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC];

        private static byte[] GetData(int length) => Enumerable.Range(0, length).Select(x => (byte)(x * 31)).ToArray();

        [Fact]
        public void SymmetricEncryptor_ReadsNet10()
        {
            Assert.Equal("encrypted on .NET 10", SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_CBC_HMAC, fixedKey, Net10Fixtures.CbcHmacFromNet10));
            Assert.Equal("encrypted on .NET 10", SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_CBC, fixedKey, Net10Fixtures.CbcFromNet10));

            //keys derived from a password, with SHA-1 and with the default SHA-256, match .NET 10's
            var sha1 = new ZerraEncryptor("password", SymmetricAlgorithmType.AES_CBC_HMAC, hashAlgorithm: HashAlgorithmName.SHA1);
            Assert.Equal("derived on .NET 10", Encoding.UTF8.GetString(sha1.Decrypt(Convert.FromBase64String(Net10Fixtures.DerivedFromNet10))));
            var sha256 = new ZerraEncryptor("password", SymmetricAlgorithmType.AES_CBC_HMAC);
            Assert.Equal("derived on .NET 10", Encoding.UTF8.GetString(sha256.Decrypt(Convert.FromBase64String(Net10Fixtures.DerivedSha256FromNet10))));
        }

        [Fact]
        public void SymmetricEncryptor_RoundTrip()
        {
            foreach (var algorithm in algorithms)
            {
                var key = SymmetricEncryptor.GenerateKey();
                //more than two 64 KB chunks
                var data = GetData(150_000);
                Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, data)));
                Assert.Equal("text", SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, "text")));
            }
        }

        [Fact]
        public void Gcm_NotSupported()
        {
            _ = Assert.Throws<PlatformNotSupportedException>(() => SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, fixedKey, new byte[1]));
        }

        //produced by the old SymmetricEncryptor.GetKey("old-password") and Encrypt
#pragma warning disable CS0612 //tests the obsolete Old APIs
        [Fact]
        public void Old_ReadsStoredData()
        {
            var (key, iv) = SymmetricEncryptorOld.DeriveKey("old-password");
            Assert.Equal(Convert.FromBase64String("GhsW5BJipJYLqu+SDgO6Xsqi5tvZ/AWD+FPho2Qs3mU="), key);
            Assert.Equal(Convert.FromBase64String("TCM9StoVkWIqyRtQDKyOFg=="), iv);
            Assert.Equal("stored data", SymmetricEncryptorOld.Decrypt(SymmetricAlgorithmTypeOld.AES, key, iv, "wO8SkoHL22UcE5YSeopEgQ=="));
            Assert.Equal("stored data", SymmetricEncryptorOld.Decrypt(SymmetricAlgorithmTypeOld.AESwithShift, key, iv, "Ui9+CuG84CiE6Uq5EaOJLbOaMBr/2pEqPCi37rRNBEA="));
            var data = GetData(100_000);
            Assert.Equal(data, new ZerraEncryptorOld("password").Decrypt(new ZerraEncryptorOld("password").Encrypt(data)));
        }
#pragma warning restore CS0612
    }
}
