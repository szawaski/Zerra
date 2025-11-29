// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Encryption;
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
    }
}
