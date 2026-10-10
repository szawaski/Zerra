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
    public class HashTest
    {
        //RFC 6070 for SHA-1, and the widely published vectors for SHA-256 and SHA-512, all of "password" with "salt"
        //only SHA-1 is built into .NET Standard, the others are Zerra's own PBKDF2
        [Theory]
        [InlineData("SHA1", 1, 20, "0c60c80f961f0e71f3a9b524af6012062fe037a6")]
        [InlineData("SHA1", 4096, 20, "4b007901b765489abead49d926f721d065a429c1")]
        [InlineData("SHA256", 1, 32, "120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b")]
        [InlineData("SHA256", 4096, 32, "c5e478d59288c841aa530db6845c4c8d962893a001ce4e11a4963873aa98134a")]
        [InlineData("SHA256", 2, 40, null)]
        [InlineData("SHA512", 1, 64, "867f70cf1ade02cff3752599a3a53dc4af34c7a669815ae5d513554e1c8cf252c02d470a285a0501bad999bfe943c08f050235d7d68b1da55e63f73b60a57fce")]
        public void Pbkdf2_KnownVectors(string algorithm, int iterations, int length, string? expected)
        {
            var result = Pbkdf2.Derive(Encoding.ASCII.GetBytes("password"), Encoding.ASCII.GetBytes("salt"), iterations, new HashAlgorithmName(algorithm), length);
            if (expected is not null)
                Assert.Equal(expected, BitConverter.ToString(result).Replace("-", "").ToLowerInvariant());
            //more than one block, the second block's start matches the first's of a longer request
            Assert.Equal(length, result.Length);
        }

        [Fact]
        public void PBKDF2_ReadsNet10()
        {
            foreach (var hash in new[] { Net10Fixtures.Pbkdf2SHA1FromNet10, Net10Fixtures.Pbkdf2SHA256FromNet10, Net10Fixtures.Pbkdf2SHA384FromNet10, Net10Fixtures.Pbkdf2SHA512FromNet10 })
            {
                Assert.True(Hasher.PBKDF2VerifyHash("password é", hash));
                Assert.False(Hasher.PBKDF2VerifyHash("wrong", hash));
            }
#pragma warning disable CS0612 //tests the obsolete Old APIs
            Assert.True(HasherOld.PBKDF2VerifyHash("password", Net10Fixtures.OldPbkdf2Sha256FromNet10, HashAlgorithmName.SHA256));
#pragma warning restore CS0612
        }

        [Fact]
        public void PBKDF2Hash()
        {
            foreach (var algorithm in new[] { HashAlgorithmName.SHA1, HashAlgorithmName.SHA256, HashAlgorithmName.SHA384, HashAlgorithmName.SHA512 })
            {
                var hash = Hasher.PBKDF2GenerateHash("test", algorithm, 1000);
                Assert.True(Hasher.PBKDF2VerifyHash("test", hash));
                Assert.False(Hasher.PBKDF2VerifyHash("wrong", hash));
                Assert.False(Hasher.PBKDF2NeedsRehash(hash, algorithm, 1000));
            }
        }

        [Fact]
        public void Hash()
        {
            foreach (var alg in (HashAlgorithmType[])Enum.GetValues(typeof(HashAlgorithmType)))
            {
                var hash = Hasher.GenerateHash(alg, "test");
                Assert.True(Hasher.VerifyHash(alg, "test", hash));
                Assert.False(Hasher.VerifyHash(alg, "wrong", hash));
            }
        }

        //produced by the old Hasher with the salt "salt"
#pragma warning disable CS0612 //tests the obsolete Old APIs
        [Fact]
        public void Old_ReadsStoredHashes()
        {
            Assert.True(HasherOld.PBKDF2VerifyHash("password", "boi+i61+rp2eEKoGEiQDT+1I0D/LrZaLVgBnhFOdUhTOlw2RLsIEmwQjHUfC64hQaUWyayMl5q3+66CIlf+Vh3NhbHQ="));
            Assert.True(HasherOld.VerifyHash(HashAlgorithmTypeOld.SHA256, "plain", "M+1j2taPghkyGCSqs2DRhTmy3YVBRi9adIpMkAXg829zYWx0"));
            Assert.True(HasherOld.VerifyHash(HashAlgorithmTypeOld.MD5, "plain", "m9q8BBgd7LO0tGrBmIZBnnNhbHQ="));
        }
#pragma warning restore CS0612
    }
}
