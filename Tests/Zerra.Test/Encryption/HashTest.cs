// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using System.Text;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class HashTest
    {
        [Fact]
        public void Hash()
        {
            foreach (var alg in (HashAlgorithmType[])Enum.GetValues(typeof(HashAlgorithmType)))
            {
                const string str = "test";
                var hashString = Hasher.GenerateHash(alg, str);
                Assert.True(Hasher.VerifyHash(alg, str, hashString));
                Assert.False(Hasher.VerifyHash(alg, "wrong", hashString));
                Assert.NotEqual(hashString, Hasher.GenerateHash(alg, str));

                hashString = Hasher.GenerateHash(alg, str, "salt");
                Assert.Equal(hashString, Hasher.GenerateHash(alg, str, "salt"));
                Assert.True(Hasher.VerifyHash(alg, str, hashString));

                var bytes = GetTestBytes();
                var wrongBytes = GetTestBytes();
                wrongBytes[0]++;
                var hashBytes = Hasher.GenerateHash(alg, bytes);
                Assert.True(Hasher.VerifyHash(alg, bytes, hashBytes));
                Assert.False(Hasher.VerifyHash(alg, wrongBytes, hashBytes));
            }
            _ = Assert.Throws<NotSupportedException>(() => Hasher.GenerateHash((HashAlgorithmType)99, "test"));
        }

        public static TheoryData<string> PBKDF2Algorithms => new() { nameof(HashAlgorithmName.SHA1), nameof(HashAlgorithmName.SHA256), nameof(HashAlgorithmName.SHA384), nameof(HashAlgorithmName.SHA512) };

        [Theory]
        [MemberData(nameof(PBKDF2Algorithms))]
        public void PBKDF2Hash(string algorithmName)
        {
            var algorithm = new HashAlgorithmName(algorithmName);
            var hash = Hasher.PBKDF2GenerateHash("test é", algorithm, 1000);
            Assert.StartsWith($"$pbkdf2-{algorithmName.ToLowerInvariant()}$i=1000$", hash);
            Assert.True(Hasher.PBKDF2VerifyHash("test é", hash));
            Assert.False(Hasher.PBKDF2VerifyHash("wrong", hash));
            Assert.NotEqual(hash, Hasher.PBKDF2GenerateHash("test é", algorithm, 1000));
        }

        [Fact]
        public void PBKDF2Hash_Defaults()
        {
            var hash = Hasher.PBKDF2GenerateHash("test");
            Assert.StartsWith("$pbkdf2-sha256$i=600000$", hash);
            //the hash is as long as the hash algorithm's output, the salt 16 bytes
            var parts = hash.Split('$');
            Assert.Equal(22, parts[3].Length);
            Assert.Equal(43, parts[4].Length);
            Assert.True(Hasher.PBKDF2VerifyHash("test", hash));
            Assert.Equal(1_300_000, Hasher.GetDefaultPBKDF2Iterations(HashAlgorithmName.SHA1));
            Assert.Equal(210_000, Hasher.GetDefaultPBKDF2Iterations(HashAlgorithmName.SHA512));
            _ = Assert.Throws<NotSupportedException>(() => Hasher.PBKDF2GenerateHash("test", HashAlgorithmName.MD5));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => Hasher.PBKDF2GenerateHash("test", iterations: 0));
            _ = Assert.Throws<ArgumentNullException>(() => Hasher.PBKDF2GenerateHash(null!));
        }

        [Fact]
        public void PBKDF2NeedsRehash()
        {
            var weak = Hasher.PBKDF2GenerateHash("test", HashAlgorithmName.SHA256, 1000);
            Assert.True(Hasher.PBKDF2NeedsRehash(weak));
            Assert.False(Hasher.PBKDF2NeedsRehash(weak, HashAlgorithmName.SHA256, 1000));
            Assert.True(Hasher.PBKDF2NeedsRehash(weak, HashAlgorithmName.SHA512, 1000));
            Assert.False(Hasher.PBKDF2NeedsRehash(Hasher.PBKDF2GenerateHash("test")));
            Assert.True(Hasher.PBKDF2NeedsRehash("not a hash"));
            //an old format hash
            Assert.True(Hasher.PBKDF2NeedsRehash(oldPbkdf2));
        }

        [Fact]
        public void Verify_InvalidHashes_AreFalse()
        {
            foreach (var hash in new[] { "", " ", "not base64!", "AAAA" })
                Assert.False(Hasher.VerifyHash(HashAlgorithmType.SHA256, "plain", hash));
            Assert.False(Hasher.VerifyHash(HashAlgorithmType.SHA256, [1], [1, 2]));

            var valid = Hasher.PBKDF2GenerateHash("plain", HashAlgorithmName.SHA256, 1000);
            var parts = valid.Split('$');
            foreach (var hash in new[] { "", "AAAA", oldPbkdf2, "$pbkdf2-md5$i=1000$" + parts[3] + "$" + parts[4], "$pbkdf2-sha256$i=0$" + parts[3] + "$" + parts[4],
                "$pbkdf2-sha256$i=-1$" + parts[3] + "$" + parts[4], "$pbkdf2-sha256$1000$" + parts[3] + "$" + parts[4], "$pbkdf2-sha256$i=1000$!!$" + parts[4],
                "$pbkdf2-sha256$i=1000$" + parts[3] + "$", "$pbkdf2-sha256$i=1000$" + parts[3] + "$A", "x" + valid, valid + "$x" })
            {
                Assert.False(Hasher.PBKDF2VerifyHash("plain", hash));
            }
            Assert.False(Hasher.PBKDF2VerifyHash("plain", null!));
        }

        //produced by the old Hasher with the salt "salt"
        private const string oldPbkdf2 = "boi+i61+rp2eEKoGEiQDT+1I0D/LrZaLVgBnhFOdUhTOlw2RLsIEmwQjHUfC64hQaUWyayMl5q3+66CIlf+Vh3NhbHQ=";
#pragma warning disable CS0612 //tests the obsolete Old APIs
        public static TheoryData<HashAlgorithmTypeOld, string> OldHashes => new()
        {
            { HashAlgorithmTypeOld.SHA1, "vfHa86ZWwfLcSU0XOxsWZs9TivRzYWx0" },
            { HashAlgorithmTypeOld.SHA256, "M+1j2taPghkyGCSqs2DRhTmy3YVBRi9adIpMkAXg829zYWx0" },
            { HashAlgorithmTypeOld.SHA512, "5hrKOCEhNiHK9Hwt4qeBaCInz7eNxWco8XX7//422kAYY59o6pXslcLyjzUGIlYIp2yHMNfSLmKY+Xgnw1txVHNhbHQ=" },
            { HashAlgorithmTypeOld.SHA384, "N5mID3TSfYRvsl2DPgVCD94QDywm40zqdTAYRDyWL6bXC3nVDQBEZiztZ2n6w/LDc2FsdA==" },
            { HashAlgorithmTypeOld.MD5, "m9q8BBgd7LO0tGrBmIZBnnNhbHQ=" },
        };

        [Theory]
        [MemberData(nameof(OldHashes))]
        public void Old_ReadsStoredHashes(HashAlgorithmTypeOld algorithm, string hash)
        {
            Assert.True(HasherOld.VerifyHash(algorithm, "plain", hash));
            Assert.False(HasherOld.VerifyHash(algorithm, "wrong", hash));
            Assert.Equal(hash, HasherOld.GenerateHash(algorithm, "plain", "salt"));
        }

        [Fact]
        public void Old_ReadsStoredPBKDF2()
        {
            Assert.True(HasherOld.PBKDF2VerifyHash("password", oldPbkdf2));
            Assert.False(HasherOld.PBKDF2VerifyHash("wrong", oldPbkdf2));
            Assert.Equal(oldPbkdf2, HasherOld.PBKDF2GenerateHash("password", "salt"));

            foreach (var algorithm in new[] { HashAlgorithmName.SHA1, HashAlgorithmName.SHA256, HashAlgorithmName.SHA512 })
            {
                var bytes = GetTestBytes();
                var hash = HasherOld.PBKDF2GenerateHash(bytes, null, algorithm);
                Assert.True(HasherOld.PBKDF2VerifyHash(bytes, hash, algorithm));
                Assert.False(HasherOld.PBKDF2VerifyHash(bytes, hash, algorithm == HashAlgorithmName.SHA1 ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA1));
            }
            foreach (var hash in new[] { "", " ", "not base64!", "AAAA" })
            {
                Assert.False(HasherOld.VerifyHash(HashAlgorithmTypeOld.SHA256, "plain", hash));
                Assert.False(HasherOld.PBKDF2VerifyHash("plain", hash));
            }
        }
#pragma warning restore CS0612

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }
    }
}
