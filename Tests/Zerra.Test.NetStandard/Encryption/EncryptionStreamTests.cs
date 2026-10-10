// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.NetStandard.Encryption
{
    public class EncryptionStreamTests
    {
        private static readonly SymmetricAlgorithmType[] algorithms = [SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC];

        private static byte[] GetData(int length) => Enumerable.Range(0, length).Select(x => (byte)(x * 31)).ToArray();

        [Fact]
        public async Task ChunkedStreams_RoundTrip()
        {
            foreach (var algorithm in algorithms)
            {
                var key = SymmetricEncryptor.GenerateKey();
                var data = GetData(150_000);

                //written as a stream, read as a stream
                byte[] encrypted;
                using (var output = new MemoryStream())
                {
                    using (var writer = SymmetricEncryptor.Encrypt(algorithm, key, output, true, true))
                    {
                        for (var i = 0; i < data.Length; i += 9_000)
                            await writer.WriteAsync(data, i, Math.Min(9_000, data.Length - i), TestContext.Current.CancellationToken);
                        writer.FlushFinalBlock();
                    }
                    encrypted = output.ToArray();
                }
                using (var reader = SymmetricEncryptor.Decrypt(algorithm, key, new MemoryStream(encrypted), false))
                using (var output = new MemoryStream())
                {
                    await reader.CopyToAsync(output, 7_000, TestContext.Current.CancellationToken);
                    Assert.Equal(data, output.ToArray());
                }

                //encrypted by reading, decrypted by writing
                using (var reader = SymmetricEncryptor.Encrypt(algorithm, key, new MemoryStream(data), false))
                using (var output = new MemoryStream())
                {
                    reader.CopyTo(output);
                    encrypted = output.ToArray();
                }
                using (var output = new MemoryStream())
                {
                    using (var writer = SymmetricEncryptor.Decrypt(algorithm, key, output, true, true))
                    {
                        writer.Write(encrypted, 0, encrypted.Length);
                        writer.FlushFinalBlock();
                    }
                    Assert.Equal(data, output.ToArray());
                }
            }
        }

        [Fact]
        public void AuthenticatedModes_RejectChanges()
        {
            var key = SymmetricEncryptor.GenerateKey();
            var encrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_CBC_HMAC, key, GetData(150_000));
            foreach (var position in new[] { 0, 16, 20, 40, 70_000, encrypted.Length - 1 })
            {
                var changed = (byte[])encrypted.Clone();
                changed[position] ^= 0x01;
                _ = Assert.ThrowsAny<CryptographicException>(() => SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_CBC_HMAC, key, changed));
            }
            _ = Assert.ThrowsAny<CryptographicException>(() => SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_CBC_HMAC, key, encrypted.Take(encrypted.Length - 1).ToArray()));
            _ = Assert.ThrowsAny<CryptographicException>(() => SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricEncryptor.GenerateKey(), encrypted));
        }
    }
}
