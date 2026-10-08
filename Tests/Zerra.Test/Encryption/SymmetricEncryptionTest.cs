// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Encryption;
using Zerra.IO;
using Zerra.Serialization.Bytes;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Encryption
{
    public class SymmetricEncryptionTest
    {
        private void SymmetricEncryptorBytes(SymmetricKey key, SymmetricAlgorithmType algorithm)
        {
            var test = GetTestBytes();

            var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, test);
            var result = SymmetricEncryptor.Decrypt(algorithm, key, encrypted);

            Assert.True(test.SequenceEqual(result));
        }

        private void SymmetricEncryptorString(SymmetricKey key, SymmetricAlgorithmType algorithm)
        {
            var test = Convert.ToBase64String(GetTestBytes());

            var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, test);
            var result = SymmetricEncryptor.Decrypt(algorithm, key, encrypted);

            Assert.Equal(test, result);
        }

        private async Task SymmetricEncryptorStream(SymmetricKey key, SymmetricAlgorithmType algorithm)
        {
            var test = GetTestBytes();

            using (var ms = new MemoryStream())
            using (var cryptoStreamWriter = SymmetricEncryptor.Encrypt(algorithm, key, ms, true))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(algorithm, key, ms, false))
            {
                cryptoStreamWriter.Write(test, 0, test.Length);
                cryptoStreamWriter.FlushFinalBlock();
                ms.Position = 0;
                var result = cryptoStreamReader.ToArray();
                Assert.True(test.SequenceEqual(result));
            }
        }

        private async Task SymmetricEncryptorSerializer(SymmetricKey key, SymmetricAlgorithmType algorithm)
        {
            var options = new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.UInt16 };

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            using (var cryptoStreamWriter = SymmetricEncryptor.Encrypt(algorithm, key, ms, true))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(algorithm, key, ms, false))
            {
                var expected = ByteSerializer.Serialize(model1, options);
                await ByteSerializer.SerializeAsync(cryptoStreamWriter, model1, options);
                cryptoStreamWriter.FlushFinalBlock();
                ms.Position = 0;
                var bytes = ms.ToArray();
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(cryptoStreamReader, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }

        [Fact]
        public async Task SymmetricEncryptorTests()
        {
            foreach (var algorithm in Enum.GetValues<SymmetricAlgorithmType>())
            {
                var key = SymmetricEncryptor.GenerateKey(algorithm);
                SymmetricEncryptorString(key, algorithm);
                SymmetricEncryptorBytes(key, algorithm);
                await SymmetricEncryptorStream(key, algorithm);
                await SymmetricEncryptorSerializer(key, algorithm);
            }
        }

        [Fact]
        public void SymmetricEncryptorEmptyStream()
        {
            var result = Array.Empty<byte>();

            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithPrefix);

            using (var ms = new MemoryStream(result))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES, key, ms, false))
            using (var sr = new StreamReader(cryptoStreamReader))
            {
                _ = sr.ReadToEnd();
            }
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }

        [Fact]
        public void SymmetricEncryptorOverloads()
        {
            var plain = Enumerable.Range(0, 5_000).Select(x => (byte)(x * 13)).ToArray();
            foreach (var algorithm in Enum.GetValues<SymmetricAlgorithmType>())
            {
                foreach (var minimum in new[] { false, true })
                {
                    var key = SymmetricEncryptor.GenerateKey(algorithm, minimum, minimum);
                    var config = new SymmetricConfig(algorithm, key);

                    Assert.Equal(plain, SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, plain.AsSpan())).ToArray());
                    Assert.Equal(plain, SymmetricEncryptor.Decrypt(config, SymmetricEncryptor.Encrypt(config, plain)));
                    Assert.Equal(plain, SymmetricEncryptor.Decrypt(config, SymmetricEncryptor.Encrypt(config, plain.AsSpan())).ToArray());
                    Assert.Equal("text", SymmetricEncryptor.Decrypt(config, SymmetricEncryptor.Encrypt(config, "text")));

                    Assert.Null(SymmetricEncryptor.Encrypt(algorithm, key, (string?)null));
                    Assert.Null(SymmetricEncryptor.Decrypt(algorithm, key, (string?)null));
                    Assert.Equal("", SymmetricEncryptor.Encrypt(algorithm, key, ""));
                    Assert.Equal("", SymmetricEncryptor.Decrypt(algorithm, key, ""));
                    Assert.Empty(SymmetricEncryptor.Encrypt(algorithm, key, Array.Empty<byte>()));
                    Assert.Empty(SymmetricEncryptor.Decrypt(algorithm, key, Array.Empty<byte>()));
                    Assert.True(SymmetricEncryptor.Encrypt(algorithm, key, ReadOnlySpan<byte>.Empty).IsEmpty);
                    Assert.True(SymmetricEncryptor.Decrypt(algorithm, key, ReadOnlySpan<byte>.Empty).IsEmpty);

                    byte[] encrypted;
                    using (var source = new MemoryStream(plain))
                    using (var reader = SymmetricEncryptor.Encrypt(config, source, false))
                    using (var output = new MemoryStream())
                    {
                        reader.CopyTo(output);
                        encrypted = output.ToArray();
                    }
                    using (var output = new MemoryStream())
                    {
                        using (var writer = SymmetricEncryptor.Decrypt(config, output, true, true))
                        {
                            writer.Write(encrypted, 0, encrypted.Length);
                            writer.FlushFinalBlock();
                        }
                        Assert.Equal(plain, output.ToArray());
                    }
                }
            }
        }

        [Fact]
        public void SymmetricEncryptorGetKey()
        {
            var key1 = SymmetricEncryptor.GetKey("password", "salt");
            var key2 = SymmetricEncryptor.GetKey("password", "salt");
            Assert.Equal(key1.Key, key2.Key);
            Assert.Equal(key1.IV, key2.IV);
            Assert.NotEqual(key1.Key, SymmetricEncryptor.GetKey("other", "salt").Key);
            Assert.NotEqual(key1.Key, SymmetricEncryptor.GetKey("password", "pepper").Key);
            Assert.NotEqual(key1.Key, SymmetricEncryptor.GetKey("password").Key);

            var encrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithPrefix, key1, "text");
            Assert.Equal("text", SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithPrefix, key2, encrypted));
        }
    }
}
