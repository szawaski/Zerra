// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
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
        private void SymmetricEncryptorBytes(byte[] key, SymmetricAlgorithmType algorithm)
        {
            var test = GetTestBytes();

            var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, test);
            var result = SymmetricEncryptor.Decrypt(algorithm, key, encrypted);

            Assert.True(test.SequenceEqual(result));
        }

        private void SymmetricEncryptorString(byte[] key, SymmetricAlgorithmType algorithm)
        {
            var test = Convert.ToBase64String(GetTestBytes());

            var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, test);
            var result = SymmetricEncryptor.Decrypt(algorithm, key, encrypted);

            Assert.Equal(test, result);
        }

        private void SymmetricEncryptorStream(byte[] key, SymmetricAlgorithmType algorithm)
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

        private async Task SymmetricEncryptorSerializer(byte[] key, SymmetricAlgorithmType algorithm)
        {
            var options = new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.UInt16 };

            var model1 = TypesAllModel.Create();
            using (var ms = new MemoryStream())
            using (var cryptoStreamWriter = SymmetricEncryptor.Encrypt(algorithm, key, ms, true))
            using (var cryptoStreamReader = SymmetricEncryptor.Decrypt(algorithm, key, ms, false))
            {
                await ByteSerializer.SerializeAsync(cryptoStreamWriter, model1, options);
                cryptoStreamWriter.FlushFinalBlock();
                ms.Position = 0;
                var model2 = await ByteSerializer.DeserializeAsync<TypesAllModel>(cryptoStreamReader, options);
                AssertHelper.AreEqual(model1, model2);
            }
        }

        [Fact]
        public async Task SymmetricEncryptorTests()
        {
            foreach (var algorithm in Enum.GetValues<SymmetricAlgorithmType>())
            {
                foreach (var keySize in new[] { SymmetricKeySize.Bits_128, SymmetricKeySize.Bits_192, SymmetricKeySize.Bits_256 })
                {
                    var key = SymmetricEncryptor.GenerateKey(keySize);
                    Assert.Equal((int)keySize / 8, key.Length);
                    SymmetricEncryptorString(key, algorithm);
                    SymmetricEncryptorBytes(key, algorithm);
                    SymmetricEncryptorStream(key, algorithm);
                    await SymmetricEncryptorSerializer(key, algorithm);
                }
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
                var key = SymmetricEncryptor.GenerateKey();

                Assert.Equal(plain, SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, plain.AsSpan())).ToArray());
                Assert.Equal(plain, SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, plain)));
                Assert.Equal("text", SymmetricEncryptor.Decrypt(algorithm, key, SymmetricEncryptor.Encrypt(algorithm, key, "text")));

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
                using (var reader = SymmetricEncryptor.Encrypt(algorithm, key, source, false))
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
                    Assert.Equal(plain, output.ToArray());
                }
            }
        }

        [Fact]
        public void SymmetricEncryptorDeriveKey()
        {
            var key1 = SymmetricEncryptor.DeriveKey("password", "salt");
            var key2 = SymmetricEncryptor.DeriveKey("password", "salt");
            Assert.Equal(key1, key2);
            Assert.Equal(32, key1.Length);
            Assert.Equal(16, SymmetricEncryptor.DeriveKey("password", keySize: SymmetricKeySize.Bits_128).Length);
            Assert.NotEqual(key1, SymmetricEncryptor.DeriveKey("other", "salt"));
            Assert.NotEqual(key1, SymmetricEncryptor.DeriveKey("password", "pepper"));
            Assert.NotEqual(key1, SymmetricEncryptor.DeriveKey("password"));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.DeriveKey(null!));

            var encrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, key1, "text");
            Assert.Equal("text", SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_GCM, key2, encrypted));
        }

        [Fact]
        public void ZerraEncryptor_KeyBytes()
        {
            var key = SymmetricEncryptor.GenerateKey();
            var fromBytes = new ZerraEncryptor(key, SymmetricAlgorithmType.AES_GCM);
            var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
            Assert.Equal(data, fromBytes.Decrypt(SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, key, data)));

            //the password constructor derives with SHA256, the same as DeriveKey given SHA256
            var fromPassword = new ZerraEncryptor("password", SymmetricAlgorithmType.AES_GCM);
            var derived = SymmetricEncryptor.DeriveKey("password", hashAlgorithm: System.Security.Cryptography.HashAlgorithmName.SHA256);
            Assert.Equal(data, fromPassword.Decrypt(SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, derived, data)));

            _ = Assert.Throws<ArgumentNullException>(() => new ZerraEncryptor((byte[])null!, SymmetricAlgorithmType.AES_GCM));
        }

        //produced by the old SymmetricEncryptor.GetKey("old-password") and Encrypt
        private const string oldPassword = "old-password";
        private const string oldKey = "GhsW5BJipJYLqu+SDgO6Xsqi5tvZ/AWD+FPho2Qs3mU=";
        private const string oldIV = "TCM9StoVkWIqyRtQDKyOFg==";
        private const string oldAes = "wO8SkoHL22UcE5YSeopEgQ==";
        private const string oldShift = "Ui9+CuG84CiE6Uq5EaOJLbOaMBr/2pEqPCi37rRNBEA=";
        private const string oldSaltedShift = "gCzZ7wifdNcgc1Zp7RntaVaJ14ux3MKhSDyR7mcVCPQ=";

#pragma warning disable CS0612 //tests the obsolete Old APIs
        [Fact]
        public void Old_ReadsStoredData()
        {
            var (key, iv) = SymmetricEncryptorOld.DeriveKey(oldPassword);
            Assert.Equal(Convert.FromBase64String(oldKey), key);
            Assert.Equal(Convert.FromBase64String(oldIV), iv);
            //the same password gives the same key bytes in both
            Assert.Equal(key, SymmetricEncryptor.DeriveKey(oldPassword));

            Assert.Equal("stored data", SymmetricEncryptorOld.Decrypt(SymmetricAlgorithmTypeOld.AES, key, iv, oldAes));
            Assert.Equal("stored data", SymmetricEncryptorOld.Decrypt(SymmetricAlgorithmTypeOld.AESwithShift, key, iv, oldShift));
            //plain AES is deterministic, so it writes exactly what was stored
            Assert.Equal(oldAes, SymmetricEncryptorOld.Encrypt(SymmetricAlgorithmTypeOld.AES, key, iv, "stored data"));

            Assert.Equal("stored data", Encoding.UTF8.GetString(new ZerraEncryptorOld(oldPassword).Decrypt(Convert.FromBase64String(oldShift))));
            Assert.Equal("stored data", Encoding.UTF8.GetString(new ZerraEncryptorOld(oldPassword, SymmetricAlgorithmTypeOld.AES).Decrypt(Convert.FromBase64String(oldAes))));
            Assert.Equal("salted data", Encoding.UTF8.GetString(new ZerraEncryptorOld(oldPassword, salt: "pepper").Decrypt(Convert.FromBase64String(oldSaltedShift))));
            Assert.Equal("stored data", Encoding.UTF8.GetString(new ZerraEncryptorOld(key, iv).Decrypt(Convert.FromBase64String(oldShift))));
        }

        [Fact]
        public async Task Old_RoundTrips()
        {
            var (key, iv) = SymmetricEncryptorOld.DeriveKey("password");
            var data = GetTestBytes();
            foreach (var algorithm in new[] { SymmetricAlgorithmTypeOld.AES, SymmetricAlgorithmTypeOld.AESwithShift })
            {
                Assert.Equal(data, SymmetricEncryptorOld.Decrypt(algorithm, key, iv, SymmetricEncryptorOld.Encrypt(algorithm, key, iv, data)));
                Assert.Equal(data, SymmetricEncryptorOld.Decrypt(algorithm, key, iv, SymmetricEncryptorOld.Encrypt(algorithm, key, iv, data.AsSpan())).ToArray());
                Assert.Equal("text", SymmetricEncryptorOld.Decrypt(algorithm, key, iv, SymmetricEncryptorOld.Encrypt(algorithm, key, iv, "text")));
                Assert.Null(SymmetricEncryptorOld.Encrypt(algorithm, key, iv, (string?)null));
                Assert.Empty(SymmetricEncryptorOld.Encrypt(algorithm, key, iv, Array.Empty<byte>()));
                Assert.Empty(SymmetricEncryptorOld.Decrypt(algorithm, key, iv, Array.Empty<byte>()));

                var encryptor = new ZerraEncryptorOld("password", algorithm);
                using (var ms = new MemoryStream())
                {
                    await using (var writer = encryptor.Encrypt(ms, true))
                    {
                        await writer.WriteAsync(data, TestContext.Current.CancellationToken);
                        await writer.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                    }
                    using var reader = encryptor.Decrypt(new MemoryStream(ms.ToArray()), false);
                    Assert.Equal(data, reader.ToArray());
                }
            }

            //an empty stream reads as empty, as before
            using (var reader = SymmetricEncryptorOld.Decrypt(SymmetricAlgorithmTypeOld.AES, key, iv, new MemoryStream(), false))
            using (var sr = new StreamReader(reader))
                Assert.Equal("", sr.ReadToEnd());

            _ = Assert.Throws<NotSupportedException>(() => SymmetricEncryptorOld.Encrypt((SymmetricAlgorithmTypeOld)99, key, iv, data));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptorOld.Encrypt(SymmetricAlgorithmTypeOld.AES, key, null!, data));
            _ = Assert.Throws<ArgumentNullException>(() => new ZerraEncryptorOld(key, null!));
        }
#pragma warning restore CS0612
    }
}
