// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class KeyRotationTests
    {
        private sealed class TestKeySource : ISymmetricKeySource
        {
            public byte[] Key = RandomNumberGenerator.GetBytes(32);
            public DateTime Expiration = DateTime.UtcNow.AddHours(1);
            public int Calls;

            public void Rotate() => Key = RandomNumberGenerator.GetBytes(32);

            public (byte[] Key, DateTime Expiration) GetKey()
            {
                Calls++;
                return (Key, Expiration);
            }

            public ValueTask<(byte[] Key, DateTime Expiration)> GetKeyAsync()
            {
                Calls++;
                return new((Key, Expiration));
            }
        }

        public static TheoryData<SymmetricAlgorithmType> Algorithms => new() { SymmetricAlgorithmType.AES_GCM, SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricAlgorithmType.AES_CBC };

        private static readonly byte[] data = RandomNumberGenerator.GetBytes(150_000);

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void FirstUse_FetchesTheKey(SymmetricAlgorithmType algorithm)
        {
            var source = new TestKeySource();
            var encryptor = new ZerraEncryptor(source, algorithm);
            Assert.Equal(0, source.Calls);

            var encrypted = encryptor.Encrypt(data);
            Assert.Equal(1, source.Calls);
            Assert.Equal(data, encryptor.Decrypt(encrypted));
            Assert.Equal(data, new ZerraEncryptor(source.Key, algorithm).Decrypt(encrypted));
            Assert.Equal(1, source.Calls);
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void PreviousKey_StillDecrypts(SymmetricAlgorithmType algorithm)
        {
            //the first key expires, so the receiver moves to the next key on its next use
            var source = new TestKeySource { Expiration = DateTime.UtcNow.AddMilliseconds(-1) };
            var receiver = new ZerraEncryptor(source, algorithm);
            var oldKeySender = new ZerraEncryptor(source.Key, algorithm);
            Assert.Equal(data, receiver.Decrypt(oldKeySender.Encrypt(data)));

            source.Rotate();
            source.Expiration = DateTime.UtcNow.AddHours(1);
            var newKeySender = new ZerraEncryptor(source.Key, algorithm);
            Assert.Equal(data, newKeySender.Decrypt(receiver.Encrypt(data)));

            //the sender hasn't moved yet, its data reads with the previous key without asking the source
            var calls = source.Calls;
            Assert.Equal(data, receiver.Decrypt(oldKeySender.Encrypt(data)));
            Assert.Equal(calls, source.Calls);
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void NewerKey_IsFetchedOnFailure_OnceAMinute(SymmetricAlgorithmType algorithm)
        {
            var source = new TestKeySource();
            var receiver = new ZerraEncryptor(source, algorithm);
            Assert.Equal(data, receiver.Decrypt(receiver.Encrypt(data)));

            //the sender moved to a new key before the receiver's expired
            source.Rotate();
            var newKeySender = new ZerraEncryptor(source.Key, algorithm);
            var calls = source.Calls;
            Assert.Equal(data, receiver.Decrypt(newKeySender.Encrypt(data)));
            Assert.Equal(calls + 1, source.Calls);

            //data no key reads only asks the source once a minute
            var unknown = new ZerraEncryptor(RandomNumberGenerator.GetBytes(32), algorithm).Encrypt(data);
            _ = Assert.ThrowsAny<CryptographicException>(() => receiver.Decrypt(unknown));
            _ = Assert.ThrowsAny<CryptographicException>(() => receiver.Decrypt(unknown));
            Assert.Equal(calls + 1, source.Calls);
        }

        [Fact]
        public void ExpiredKey_IsFetchedBeforeUse_AndTheSameKeyKeepsThePrevious()
        {
            var source = new TestKeySource();
            var encryptor = new ZerraEncryptor(source, SymmetricAlgorithmType.AES_GCM);
            var firstKeySender = new ZerraEncryptor(source.Key, SymmetricAlgorithmType.AES_GCM);
            source.Expiration = DateTime.UtcNow.AddMilliseconds(-1);
            _ = encryptor.Encrypt(data);

            source.Rotate();
            var encrypted = encryptor.Encrypt(data);
            Assert.Equal(2, source.Calls);
            Assert.Equal(data, new ZerraEncryptor(source.Key, SymmetricAlgorithmType.AES_GCM).Decrypt(encrypted));

            //the source gives the same key again, the first key is still the previous one
            _ = encryptor.Encrypt(data);
            Assert.Equal(3, source.Calls);
            source.Expiration = DateTime.UtcNow.AddHours(1);
            Assert.Equal(data, encryptor.Decrypt(firstKeySender.Encrypt(data)));
            Assert.Equal(4, source.Calls);
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public async Task Streams_UsePreviousAndNewerKeys(SymmetricAlgorithmType algorithm)
        {
            var source = new TestKeySource();
            var receiver = new ZerraEncryptor(source, algorithm);
            Assert.Equal(data, receiver.Decrypt(receiver.Encrypt(data)));
            var oldKeySender = new ZerraEncryptor(source.Key, algorithm);
            source.Rotate();
            var newKeySender = new ZerraEncryptor(source.Key, algorithm);

            foreach (var sender in new[] { newKeySender, oldKeySender, newKeySender })
            {
                var encrypted = await EncryptStreamAsync(sender, data);
                Assert.Equal(data, await DecryptReadingAsync(receiver, encrypted));
                Assert.Equal(data, DecryptReading(receiver, encrypted));
                Assert.Equal(data, await DecryptWritingAsync(receiver, encrypted));
                Assert.Equal(data, DecryptWriting(receiver, encrypted));
            }

            //the receiver encrypts streams with its current key, which is now the new one
            Assert.Equal(data, newKeySender.Decrypt(await EncryptStreamAsync(receiver, data)));
            Assert.Equal(data, newKeySender.Decrypt(EncryptReading(receiver, data)));
        }

        [Theory]
        [MemberData(nameof(Algorithms))]
        public async Task ChangedLaterChunk_FailsWithoutAnotherKey(SymmetricAlgorithmType algorithm)
        {
            //the key that read the first chunk is kept, a later chunk failing is the data being changed
            var source = new TestKeySource();
            var receiver = new ZerraEncryptor(source, algorithm);
            var encrypted = receiver.Encrypt(data);
            encrypted[^20] ^= 1;
            var calls = source.Calls;

            if (algorithm == SymmetricAlgorithmType.AES_CBC)
            {
                //nothing in AES_CBC detects a changed byte, only a wrong key, it can still break the padding
                try
                {
                    Assert.NotEqual(data, receiver.Decrypt(encrypted));
                }
                catch (CryptographicException) { }
            }
            else
            {
                _ = Assert.ThrowsAny<CryptographicException>(() => receiver.Decrypt(encrypted));
                _ = await Assert.ThrowsAnyAsync<CryptographicException>(() => DecryptReadingAsync(receiver, encrypted));
            }
            Assert.Equal(calls, source.Calls);
        }

        [Fact]
        public void AES_CBC_WrongKey_Throws()
        {
            var encrypted = new ZerraEncryptor(RandomNumberGenerator.GetBytes(32), SymmetricAlgorithmType.AES_CBC).Encrypt(data);
            _ = Assert.ThrowsAny<CryptographicException>(() => new ZerraEncryptor(RandomNumberGenerator.GetBytes(32), SymmetricAlgorithmType.AES_CBC).Decrypt(encrypted));
        }

        [Fact]
        public void BadKeyFromSource_Throws()
        {
            var source = new TestKeySource { Key = new byte[10] };
            _ = Assert.Throws<CryptographicException>(() => new ZerraEncryptor(source, SymmetricAlgorithmType.AES_GCM).Encrypt(data));
            _ = Assert.Throws<ArgumentNullException>(() => new ZerraEncryptor((ISymmetricKeySource)null!, SymmetricAlgorithmType.AES_GCM));
        }

        private static async Task<byte[]> EncryptStreamAsync(IEncryptor encryptor, byte[] plain)
        {
            using var output = new MemoryStream();
            await using (var stream = encryptor.Encrypt(output, true))
            {
                for (var offset = 0; offset < plain.Length; offset += 8192)
                    await stream.WriteAsync(plain.AsMemory(offset, Math.Min(8192, plain.Length - offset)));
                await stream.FlushFinalBlockAsync();
            }
            return output.ToArray();
        }

        private static byte[] EncryptReading(IEncryptor encryptor, byte[] plain)
        {
            using var stream = encryptor.Encrypt(new MemoryStream(plain), false);
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }

        private static async Task<byte[]> DecryptReadingAsync(IEncryptor encryptor, byte[] encrypted)
        {
            await using var stream = encryptor.Decrypt(new MemoryStream(encrypted), false);
            using var output = new MemoryStream();
            await stream.CopyToAsync(output);
            return output.ToArray();
        }

        private static byte[] DecryptReading(IEncryptor encryptor, byte[] encrypted)
        {
            using var stream = encryptor.Decrypt(new MemoryStream(encrypted), false);
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }

        private static async Task<byte[]> DecryptWritingAsync(IEncryptor encryptor, byte[] encrypted)
        {
            using var output = new MemoryStream();
            await using (var stream = encryptor.Decrypt(output, true))
            {
                for (var offset = 0; offset < encrypted.Length; offset += 5000)
                    await stream.WriteAsync(encrypted.AsMemory(offset, Math.Min(5000, encrypted.Length - offset)));
            }
            return output.ToArray();
        }

        private static byte[] DecryptWriting(IEncryptor encryptor, byte[] encrypted)
        {
            using var output = new MemoryStream();
            using (var stream = encryptor.Decrypt(output, true))
            {
                for (var offset = 0; offset < encrypted.Length; offset += 5000)
                    stream.Write(encrypted, offset, Math.Min(5000, encrypted.Length - offset));
            }
            return output.ToArray();
        }
    }
}
