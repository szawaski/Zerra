// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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
                _ = Hasher.VerifyHash(alg, str, hashString);

                hashString = Hasher.GenerateHash(alg, str, strSalt);
                _ = Hasher.VerifyHash(alg, str, hashString);

                var bytes = GetTestBytes();
                var bytesSalt = new byte[] { 1, 2, 3, 4 };
                var hashBytes = Hasher.GenerateHash(alg, bytes);
                _ = Hasher.VerifyHash(alg, bytes, hashBytes);

                hashBytes = Hasher.GenerateHash(alg, bytes, bytesSalt);
                _ = Hasher.VerifyHash(alg, bytes, hashBytes);
            }
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
