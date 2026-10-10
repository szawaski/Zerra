// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.NetStandard.Encryption
{
    public class KeyRotationTests
    {
        private sealed class TestKeySource : ISymmetricKeySource
        {
            public byte[] Key = NewKey();
            public DateTime Expiration = DateTime.UtcNow.AddMilliseconds(-1);
            public int Calls;

            public (byte[] Key, DateTime Expiration) GetKey()
            {
                Calls++;
                return (Key, Expiration);
            }

            public ValueTask<(byte[] Key, DateTime Expiration)> GetKeyAsync()
            {
                Calls++;
                return new ValueTask<(byte[] Key, DateTime Expiration)>((Key, Expiration));
            }
        }

        private static readonly SymmetricAlgorithmType[] algorithms = [SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC];
        private static readonly byte[] data = Enumerable.Range(0, 150_000).Select(x => (byte)(x * 31)).ToArray();

        private static byte[] NewKey()
        {
            var key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(key);
            return key;
        }

        [Fact]
        public async Task PreviousAndNewerKeys_Decrypt()
        {
            foreach (var algorithm in algorithms)
            {
                //the first key expires, the receiver moves to the second on its next use
                var source = new TestKeySource();
                var receiver = new ZerraEncryptor(source, algorithm);
                var firstKeySender = new ZerraEncryptor(source.Key, algorithm);
                Assert.Equal(data, receiver.Decrypt(firstKeySender.Encrypt(data)));

                source.Key = NewKey();
                source.Expiration = DateTime.UtcNow.AddHours(1);
                var secondKeySender = new ZerraEncryptor(source.Key, algorithm);
                Assert.Equal(data, secondKeySender.Decrypt(receiver.Encrypt(data)));

                var calls = source.Calls;
                Assert.Equal(data, receiver.Decrypt(firstKeySender.Encrypt(data)));
                Assert.Equal(calls, source.Calls);

                //a sender already on a third key, the receiver asks the source once
                source.Key = NewKey();
                var thirdKeySender = new ZerraEncryptor(source.Key, algorithm);
                using (var stream = receiver.Decrypt(new MemoryStream(thirdKeySender.Encrypt(data)), false))
                using (var output = new MemoryStream())
                {
                    await stream.CopyToAsync(output);
                    Assert.Equal(data, output.ToArray());
                }
                Assert.Equal(calls + 1, source.Calls);

                //no key reads this, and the source isn't asked again within a minute
                _ = Assert.ThrowsAny<CryptographicException>(() => receiver.Decrypt(new ZerraEncryptor(NewKey(), algorithm).Encrypt(data)));
                Assert.Equal(calls + 1, source.Calls);
            }
        }
    }
}
