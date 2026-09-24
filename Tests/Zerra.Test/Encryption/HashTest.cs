// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class HashTest
    {
        [Fact]
        public void Hash()
        {
            foreach (var alg in (HashAlgoritmType[])System.Enum.GetValues(typeof(HashAlgoritmType)))
            {
                var str = "test";
                var strSalt = "salt";
                var hashString = Hasher.GenerateHash(alg, str);
                Assert.True(Hasher.VerifyHash(alg, str, hashString));
                Assert.False(Hasher.VerifyHash(alg, "wrong", hashString));

                hashString = Hasher.GenerateHash(alg, str, strSalt);
                Assert.True(Hasher.VerifyHash(alg, str, hashString));
                Assert.False(Hasher.VerifyHash(alg, "wrong", hashString));

                var bytes = GetTestBytes();
                var wrongBytes = GetTestBytes();
                wrongBytes[0]++;
                var bytesSalt = new byte[] { 1, 2, 3, 4 };
                var hashBytes = Hasher.GenerateHash(alg, bytes);
                Assert.True(Hasher.VerifyHash(alg, bytes, hashBytes));
                Assert.False(Hasher.VerifyHash(alg, wrongBytes, hashBytes));

                hashBytes = Hasher.GenerateHash(alg, bytes, bytesSalt);
                Assert.True(Hasher.VerifyHash(alg, bytes, hashBytes));
                Assert.False(Hasher.VerifyHash(alg, wrongBytes, hashBytes));
            }
        }

        public static TheoryData<string?> PBKDF2Algorithms => new() { null, nameof(HashAlgorithmName.SHA1), nameof(HashAlgorithmName.SHA256), nameof(HashAlgorithmName.SHA384), nameof(HashAlgorithmName.SHA512) };

        [Theory]
        [MemberData(nameof(PBKDF2Algorithms))]
        public void PBKDF2HashString(string? algorithmName)
        {
            HashAlgorithmName? alg = algorithmName is null ? null : new HashAlgorithmName(algorithmName);

            var hash = Hasher.PBKDF2GenerateHash("test", null, alg);
            Assert.True(Hasher.PBKDF2VerifyHash("test", hash, alg));
            Assert.False(Hasher.PBKDF2VerifyHash("wrong", hash, alg));

            hash = Hasher.PBKDF2GenerateHash("test", "salt", alg);
            Assert.True(Hasher.PBKDF2VerifyHash("test", hash, alg));
            Assert.False(Hasher.PBKDF2VerifyHash("wrong", hash, alg));
        }

        [Theory]
        [MemberData(nameof(PBKDF2Algorithms))]
        public void PBKDF2HashBytes(string? algorithmName)
        {
            HashAlgorithmName? alg = algorithmName is null ? null : new HashAlgorithmName(algorithmName);

            var bytes = GetTestBytes();
            var wrongBytes = GetTestBytes();
            wrongBytes[0]++;

            var hash = Hasher.PBKDF2GenerateHash(bytes, null, alg);
            Assert.True(Hasher.PBKDF2VerifyHash(bytes, hash, alg));
            Assert.False(Hasher.PBKDF2VerifyHash(wrongBytes, hash, alg));

            hash = Hasher.PBKDF2GenerateHash(bytes, new byte[] { 1, 2, 3, 4 }, alg);
            Assert.True(Hasher.PBKDF2VerifyHash(bytes, hash, alg));
            Assert.False(Hasher.PBKDF2VerifyHash(wrongBytes, hash, alg));
        }

        [Fact]
        public void PBKDF2HashAlgorithmMismatch()
        {
            //the algorithm is not stored in the hash, verifying with a different one must not match
            var hash = Hasher.PBKDF2GenerateHash("test", null, HashAlgorithmName.SHA256);
            Assert.False(Hasher.PBKDF2VerifyHash("test", hash));
            Assert.False(Hasher.PBKDF2VerifyHash("test", hash, HashAlgorithmName.SHA512));

            var bytes = GetTestBytes();
            var hashBytes = Hasher.PBKDF2GenerateHash(bytes, null, HashAlgorithmName.SHA256);
            Assert.False(Hasher.PBKDF2VerifyHash(bytes, hashBytes));
            Assert.False(Hasher.PBKDF2VerifyHash(bytes, hashBytes, HashAlgorithmName.SHA512));
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }
    }
}
