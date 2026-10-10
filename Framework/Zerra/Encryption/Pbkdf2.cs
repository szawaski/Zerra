// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETSTANDARD2_0
using System.Security.Cryptography;

namespace Zerra.Encryption
{
    internal static class Pbkdf2
    {
        public static byte[] Derive(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int length)
        {
            if (iterations < 1)
                throw new ArgumentOutOfRangeException(nameof(iterations));
            //RFC 8018 5.2, .NET Standard's Rfc2898DeriveBytes only has SHA1 and .NET Framework's rejects salts under 8 bytes
            using var hmac = hashAlgorithm.Name switch
            {
                "SHA1" => (HMAC)new HMACSHA1(password),
                "SHA256" => new HMACSHA256(password),
                "SHA384" => new HMACSHA384(password),
                "SHA512" => new HMACSHA512(password),
                _ => throw new NotSupportedException($"PBKDF2 doesn't support {hashAlgorithm.Name}"),
            };
            var hashLength = hmac.HashSize / 8;
            var result = new byte[length];
            var block = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, block, 0, salt.Length);
            for (int blockIndex = 1, offset = 0; offset < length; blockIndex++, offset += hashLength)
            {
                block[salt.Length] = (byte)(blockIndex >> 24);
                block[salt.Length + 1] = (byte)(blockIndex >> 16);
                block[salt.Length + 2] = (byte)(blockIndex >> 8);
                block[salt.Length + 3] = (byte)blockIndex;

                var u = hmac.ComputeHash(block);
                var t = (byte[])u.Clone();
                for (var i = 1; i < iterations; i++)
                {
                    u = hmac.ComputeHash(u);
                    for (var j = 0; j < t.Length; j++)
                        t[j] ^= u[j];
                }
                Buffer.BlockCopy(t, 0, result, offset, Math.Min(hashLength, length - offset));
            }
            return result;
        }
    }
}
#endif
