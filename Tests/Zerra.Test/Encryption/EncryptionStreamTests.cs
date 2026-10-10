// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Security.Cryptography;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class EncryptionStreamTests
    {
        private static readonly SymmetricAlgorithmType[] chunkedAlgorithms = [SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricAlgorithmType.AES_GCM];
        private static readonly SymmetricAlgorithmType[] authenticatedAlgorithms = [SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricAlgorithmType.AES_GCM];

        [Fact]
        public void EncryptionIsUnique()
        {
            var test = Convert.ToBase64String(GetTestBytes());
            var key = SymmetricEncryptor.DeriveKey("test");

            foreach (var algorithm in chunkedAlgorithms)
            {
                var values = new List<string>();
                for (var i = 0; i < 100; i++)
                    values.Add(SymmetricEncryptor.Encrypt(algorithm, key, test)!);

                Assert.True(values.Distinct().Count() == values.Count);
                //the whole last block, the last few base64 characters are mostly padding and collide by chance
                var endings = values.Select(x => Convert.ToBase64String(Convert.FromBase64String(x)[^16..]));
                Assert.True(endings.Distinct().Count() == values.Count);

                for (var i = 0; i < values.Count; i++)
                    Assert.Equal(test, SymmetricEncryptor.Decrypt(algorithm, key, values[i]));
            }
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }

        private static byte[] EncryptStream(SymmetricAlgorithmType algorithm, byte[] key, byte[] data, CryptoStreamMode mode, int chunk)
        {
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var source = new MemoryStream(data);
                using var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, source, false);
                var buffer = new byte[chunk];
                int read;
                while ((read = encrypt.Read(buffer, 0, buffer.Length)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                using var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true, true);
                for (var i = 0; i < data.Length; i += chunk)
                    encrypt.Write(data, i, Math.Min(chunk, data.Length - i));
                encrypt.FlushFinalBlock();
            }
            return output.ToArray();
        }

        private static byte[] DecryptStream(SymmetricAlgorithmType algorithm, byte[] key, byte[] data, CryptoStreamMode mode, int chunk)
        {
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var source = new MemoryStream(data);
                using var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, source, false);
                var buffer = new byte[chunk];
                int read;
                while ((read = decrypt.Read(buffer, 0, buffer.Length)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                using var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, output, true, true);
                for (var i = 0; i < data.Length; i += chunk)
                    decrypt.Write(data, i, Math.Min(chunk, data.Length - i));
                decrypt.FlushFinalBlock();
            }
            return output.ToArray();
        }

        private static async Task<byte[]> EncryptStreamAsync(SymmetricAlgorithmType algorithm, byte[] key, byte[] data, CryptoStreamMode mode, int chunk)
        {
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var source = new MemoryStream(data);
                await using var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, source, false);
                var buffer = new byte[chunk];
                int read;
                while ((read = await encrypt.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                await using var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true, true);
                for (var i = 0; i < data.Length; i += chunk)
                    await encrypt.WriteAsync(data.AsMemory(i, Math.Min(chunk, data.Length - i)), TestContext.Current.CancellationToken);
                await encrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
            }
            return output.ToArray();
        }

        private static async Task<byte[]> DecryptStreamAsync(SymmetricAlgorithmType algorithm, byte[] key, byte[] data, CryptoStreamMode mode, int chunk)
        {
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var source = new MemoryStream(data);
                await using var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, source, false);
                var buffer = new byte[chunk];
                int read;
                while ((read = await decrypt.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                await using var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, output, true, true);
                for (var i = 0; i < data.Length; i += chunk)
                    await decrypt.WriteAsync(data.AsMemory(i, Math.Min(chunk, data.Length - i)), TestContext.Current.CancellationToken);
                await decrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
            }
            return output.ToArray();
        }

        [Theory]
        [InlineData(SymmetricAlgorithmType.AES_CBC, 1)]
        [InlineData(SymmetricAlgorithmType.AES_CBC, 9_000)]
        [InlineData(SymmetricAlgorithmType.AES_CBC, 100_000)]
        [InlineData(SymmetricAlgorithmType.AES_CBC_HMAC, 1)]
        [InlineData(SymmetricAlgorithmType.AES_CBC_HMAC, 9_000)]
        [InlineData(SymmetricAlgorithmType.AES_CBC_HMAC, 100_000)]
        [InlineData(SymmetricAlgorithmType.AES_GCM, 1)]
        [InlineData(SymmetricAlgorithmType.AES_GCM, 9_000)]
        [InlineData(SymmetricAlgorithmType.AES_GCM, 100_000)]
        public async Task ChunkedStreams_RoundTrip(SymmetricAlgorithmType algorithm, int chunk)
        {
            var key = SymmetricEncryptor.GenerateKey();
            //more than two chunks, and exactly two
            foreach (var length in new[] { 150_000, 2 * 64 * 1024, 0 })
            {
                var data = Enumerable.Range(0, length).Select(x => (byte)(x * 31)).ToArray();
                foreach (var encryptMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
                {
                    foreach (var decryptMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
                    {
                        var encrypted = EncryptStream(algorithm, key, data, encryptMode, chunk);
                        Assert.Equal(data, DecryptStream(algorithm, key, encrypted, decryptMode, chunk));
                        if (length > 0)
                            Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, encrypted));

                        var encryptedAsync = await EncryptStreamAsync(algorithm, key, data, encryptMode, chunk);
                        Assert.Equal(data, await DecryptStreamAsync(algorithm, key, encryptedAsync, decryptMode, chunk));
                    }
                }

                //the byte array path writes the same format the streams read
                if (length > 0)
                {
                    var encryptedBytes = SymmetricEncryptor.Encrypt(algorithm, key, data);
                    Assert.Equal(data, DecryptStream(algorithm, key, encryptedBytes, CryptoStreamMode.Read, chunk));
                }
            }
        }

        //returns one byte per read, so every part arrives in pieces
        private sealed class OneByteStream(byte[] data) : MemoryStream(data)
        {
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
            public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(buffer.Length, 1)));
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, 1)), cancellationToken);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);
        }

        //a decrypting reader never reads past the final chunk, so a network stream with more after the message isn't consumed or waited on
        [Fact]
        public async Task ChunkedStreams_ReadOnlyTheMessage()
        {
            foreach (var algorithm in chunkedAlgorithms)
            {
                var key = SymmetricEncryptor.GenerateKey();
                var data = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();
                var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, data);
                var followed = encrypted.Concat(new byte[] { 1, 2, 3 }).ToArray();

                using (var source = new OneByteStream(followed))
                {
                    using (var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, source, false, true))
                    using (var output = new MemoryStream())
                    {
                        decrypt.CopyTo(output);
                        Assert.Equal(data, output.ToArray());
                    }
                    Assert.Equal(encrypted.Length, source.Position);
                }
                using (var source = new OneByteStream(followed))
                {
                    await using (var decrypt = SymmetricEncryptor.Decrypt(algorithm, key, source, false, true))
                    using (var output = new MemoryStream())
                    {
                        await decrypt.CopyToAsync(output, TestContext.Current.CancellationToken);
                        Assert.Equal(data, output.ToArray());
                    }
                    Assert.Equal(encrypted.Length, source.Position);
                }
            }
        }

        private static void AssertRejected(SymmetricAlgorithmType algorithm, byte[] key, byte[] encrypted)
        {
            _ = Assert.ThrowsAny<CryptographicException>(() => SymmetricEncryptor.Decrypt(algorithm, key, encrypted));
            _ = Assert.ThrowsAny<CryptographicException>(() => DecryptStream(algorithm, key, encrypted, CryptoStreamMode.Read, 1000));
            _ = Assert.ThrowsAny<CryptographicException>(() => DecryptStream(algorithm, key, encrypted, CryptoStreamMode.Write, 1000));
        }

        //the header, then chunks of {4 byte length}{body}
        private static List<(int Offset, int Length)> GetChunks(SymmetricAlgorithmType algorithm, byte[] encrypted)
        {
            var offset = algorithm switch { SymmetricAlgorithmType.AES_GCM => 16, SymmetricAlgorithmType.AES_CBC_HMAC => 16, _ => 0 };
            var chunks = new List<(int, int)>();
            while (offset < encrypted.Length)
            {
                var length = 4 + (int)(BitConverter.ToUInt32(encrypted, offset) & 0x7FFFFFFF);
                chunks.Add((offset, length));
                offset += length;
            }
            return chunks;
        }

        [Fact]
        public void AuthenticatedModes_RejectChanges()
        {
            var data = Enumerable.Range(0, 150_000).Select(x => (byte)(x * 7)).ToArray();
            foreach (var algorithm in authenticatedAlgorithms)
            {
                var key = SymmetricEncryptor.GenerateKey();
                var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, data);
                var chunks = GetChunks(algorithm, encrypted);
                Assert.Equal(3, chunks.Count);

                //a changed byte anywhere: header, a length, an IV, the ciphertext, a tag
                foreach (var position in new[] { 0, chunks[0].Offset, chunks[0].Offset + 5, chunks[1].Offset + 100, chunks[2].Offset - 1, encrypted.Length - 1 })
                {
                    var changed = (byte[])encrypted.Clone();
                    changed[position] ^= 0x01;
                    AssertRejected(algorithm, key, changed);
                }

                //the final chunk cut off, or the last byte
                AssertRejected(algorithm, key, encrypted[..chunks[2].Offset]);
                AssertRejected(algorithm, key, encrypted[..^1]);

                //the two full chunks swapped
                var swapped = (byte[])encrypted.Clone();
                Array.Copy(encrypted, chunks[0].Offset, swapped, chunks[1].Offset, chunks[0].Length);
                Array.Copy(encrypted, chunks[1].Offset, swapped, chunks[0].Offset, chunks[1].Length);
                AssertRejected(algorithm, key, swapped);

                //a chunk from another message of the same key
                var other = SymmetricEncryptor.Encrypt(algorithm, key, data);
                var mixed = (byte[])encrypted.Clone();
                Array.Copy(other, chunks[1].Offset, mixed, chunks[1].Offset, chunks[1].Length);
                AssertRejected(algorithm, key, mixed);

                //another key
                AssertRejected(algorithm, SymmetricEncryptor.GenerateKey(), encrypted);
            }
        }

        [Fact]
        public void ChunkedModes_RejectBadFraming()
        {
            foreach (var algorithm in chunkedAlgorithms)
            {
                var key = SymmetricEncryptor.GenerateKey();
                var encrypted = SymmetricEncryptor.Encrypt(algorithm, key, new byte[100]);
                var chunks = GetChunks(algorithm, encrypted);

                //data after the final chunk
                _ = Assert.ThrowsAny<CryptographicException>(() => SymmetricEncryptor.Decrypt(algorithm, key, encrypted.Concat(new byte[] { 0 }).ToArray()));
                _ = Assert.ThrowsAny<CryptographicException>(() => DecryptStream(algorithm, key, encrypted.Concat(new byte[] { 0 }).ToArray(), CryptoStreamMode.Write, 1000));

                //a length larger than any chunk, before anything that size is read
                var huge = (byte[])encrypted.Clone();
                BitConverter.GetBytes(0x7FFFFFFFu).CopyTo(huge, chunks[0].Offset);
                AssertRejected(algorithm, key, huge);

                //an empty message has no final chunk
                _ = Assert.ThrowsAny<CryptographicException>(() => DecryptStream(algorithm, key, [], CryptoStreamMode.Read, 1000));
                _ = Assert.ThrowsAny<CryptographicException>(() => DecryptStream(algorithm, key, [], CryptoStreamMode.Write, 1000));
            }
        }

        [Fact]
        public async Task ChunkedStreams_Properties()
        {
            var key = SymmetricEncryptor.GenerateKey();
            using (var reader = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_GCM, key, new MemoryStream(), false))
            {
                Assert.True(reader.CanRead);
                Assert.False(reader.CanWrite);
                Assert.False(reader.CanSeek);
                _ = Assert.ThrowsAny<Exception>(() => reader.Write([1], 0, 1));
                _ = await Assert.ThrowsAnyAsync<Exception>(async () => await reader.WriteAsync(new byte[1], TestContext.Current.CancellationToken));
            }
            using (var writer = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, key, new MemoryStream(), true))
            {
                Assert.False(writer.CanRead);
                Assert.True(writer.CanWrite);
                _ = Assert.ThrowsAny<Exception>(() => writer.Read(new byte[1], 0, 1));
                _ = await Assert.ThrowsAnyAsync<Exception>(async () => await writer.ReadExactlyAsync(new byte[1], TestContext.Current.CancellationToken));
            }

            //an encrypting writer that isn't finished writes its end when disposed
            using var output = new MemoryStream();
            using (var writer = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AES_GCM, key, output, true, true))
                writer.Write([1, 2, 3], 0, 3);
            Assert.Equal(new byte[] { 1, 2, 3 }, SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AES_GCM, key, output.ToArray()));
        }

        private static Stream CreateShift(Stream stream, CryptoStreamMode mode, bool decode)
            => new CryptoShiftStream(stream, 128, mode, decode, true);

        private static byte[] Shift(byte[] data, CryptoStreamMode mode, bool decode, int chunk)
        {
            using var source = new MemoryStream(data);
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var transform = CreateShift(source, mode, decode);
                var buffer = new byte[chunk];
                int read;
                while ((read = transform.Read(buffer, 0, buffer.Length)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                using (var transform = CreateShift(output, mode, decode))
                {
                    for (var i = 0; i < data.Length; i += chunk)
                        transform.Write(data, i, Math.Min(chunk, data.Length - i));
                }
            }
            return output.ToArray();
        }

        private static async Task<byte[]> ShiftAsync(byte[] data, CryptoStreamMode mode, bool decode, int chunk)
        {
            using var source = new MemoryStream(data);
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                await using var transform = CreateShift(source, mode, decode);
                var buffer = new byte[chunk];
                int read;
                while ((read = await transform.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                await using (var transform = CreateShift(output, mode, decode))
                {
                    for (var i = 0; i < data.Length; i += chunk)
                        await transform.WriteAsync(data.AsMemory(i, Math.Min(chunk, data.Length - i)), TestContext.Current.CancellationToken);
                }
            }
            return output.ToArray();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(100)]
        [InlineData(9_000)]
        public async Task ShiftStream_RoundTrip(int chunk)
        {
            var data = Enumerable.Range(0, 20_000).Select(x => (byte)(x * 31)).ToArray();

            foreach (var encodeMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
            {
                foreach (var decodeMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
                {
                    var encoded = Shift(data, encodeMode, false, chunk);
                    Assert.Equal(data.Length + 16, encoded.Length);
                    Assert.NotEqual(data, encoded.Skip(16).ToArray());
                    Assert.Equal(data, Shift(encoded, decodeMode, true, chunk));

                    var encodedAsync = await ShiftAsync(data, encodeMode, false, chunk);
                    Assert.Equal(data, await ShiftAsync(encodedAsync, decodeMode, true, chunk));
                }
            }
        }

        [Fact]
        public void ShiftStream_Properties()
        {
            using var reader = CreateShift(new MemoryStream(), CryptoStreamMode.Read, false);
            Assert.True(reader.CanRead);
            Assert.False(reader.CanWrite);
            Assert.False(reader.CanSeek);
            Assert.Equal(0, reader.Position);
            _ = Assert.Throws<NotSupportedException>(() => reader.Length);
            _ = Assert.Throws<NotSupportedException>(() => reader.Position = 1);
            _ = Assert.Throws<NotSupportedException>(() => reader.Seek(0, SeekOrigin.Begin));
            _ = Assert.Throws<NotSupportedException>(() => reader.SetLength(1));
            _ = Assert.ThrowsAny<Exception>(() => reader.Write([1], 0, 1));

            using var writer = CreateShift(new MemoryStream(), CryptoStreamMode.Write, false);
            Assert.False(writer.CanRead);
            Assert.True(writer.CanWrite);
            _ = Assert.ThrowsAny<Exception>(() => writer.Read(new byte[1], 0, 1));

            _ = Assert.Throws<ArgumentException>(() => new CryptoShiftStream(new MemoryStream(), 12, CryptoStreamMode.Read, false, false));
        }

        [Fact]
        public async Task ShiftStream_SourceInPieces()
        {
            var data = Enumerable.Range(0, 1000).Select(x => (byte)(x * 31)).ToArray();
            var encoded = Shift(data, CryptoStreamMode.Write, false, 100);

            using (var transform = CreateShift(new OneByteStream(encoded), CryptoStreamMode.Read, true))
            using (var output = new MemoryStream())
            {
                transform.CopyTo(output);
                Assert.Equal(data, output.ToArray());
            }
            await using (var transform = CreateShift(new OneByteStream(encoded), CryptoStreamMode.Read, true))
            using (var output = new MemoryStream())
            {
                await transform.CopyToAsync(output, TestContext.Current.CancellationToken);
                Assert.Equal(data, output.ToArray());
            }
        }

        [Fact]
        public async Task FlushFinalBlockAsync_WritesTheEnd()
        {
            var data = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();
            foreach (var algorithm in new[] { SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricAlgorithmType.AES_GCM })
            {
                var key = SymmetricEncryptor.GenerateKey();
                using var output = new MemoryStream();
                var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true);
                await encrypt.WriteAsync(data, TestContext.Current.CancellationToken);
                await encrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                await encrypt.DisposeAsync();
                Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, output.ToArray()));
            }
            using (var output = new MemoryStream())
            {
                var transform = (CryptoShiftStream)CreateShift(output, CryptoStreamMode.Write, false);
                await transform.WriteAsync(data, TestContext.Current.CancellationToken);
                await transform.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                Assert.Equal(data.Length + 16, output.Length);
            }
        }

        //finishes each async write later, so a flush that isn't awaited hasn't written yet
        private sealed class DelayedWriteStream : MemoryStream
        {
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Delay(20, cancellationToken);
                Write(buffer.Span);
            }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        [Fact]
        public async Task FlushFinalBlockAsync_WaitsForTheEnd()
        {
            var data = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();
            foreach (var algorithm in new[] { SymmetricAlgorithmType.AES_CBC, SymmetricAlgorithmType.AES_CBC_HMAC, SymmetricAlgorithmType.AES_GCM })
            {
                var key = SymmetricEncryptor.GenerateKey();
                using var output = new DelayedWriteStream();
                var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true, true);
                await encrypt.WriteAsync(data, TestContext.Current.CancellationToken);
                await encrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);

                //the whole value is written once the flush completes, before the stream is disposed
                Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, output.ToArray()));
                await encrypt.DisposeAsync();
            }
        }

        [Fact]
        public async Task ShiftStream_KeyBlockCutShort_Throws()
        {
            //the stream ends partway through the 16 byte key block
            using (var transform = CreateShift(new MemoryStream(new byte[5]), CryptoStreamMode.Read, true))
                _ = Assert.Throws<InvalidOperationException>(() => transform.Read(new byte[32], 0, 32));
            using (var transform = CreateShift(new MemoryStream(new byte[5]), CryptoStreamMode.Read, true))
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await transform.ReadExactlyAsync(new byte[32], TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ShiftStream_WrongDirection_Throws()
        {
            using (var reader = CreateShift(new MemoryStream(new byte[32]), CryptoStreamMode.Read, true))
            {
                _ = Assert.Throws<InvalidOperationException>(() => reader.Write(new byte[1], 0, 1));
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.WriteAsync(new byte[1], TestContext.Current.CancellationToken));
            }
            using (var writer = CreateShift(new MemoryStream(), CryptoStreamMode.Write, false))
            {
                _ = Assert.Throws<InvalidOperationException>(() => writer.Read(new byte[1], 0, 1));
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await writer.ReadExactlyAsync(new byte[1], TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public void SymmetricEncryptor_NullArguments()
        {
            var key = SymmetricEncryptor.GenerateKey();
            const SymmetricAlgorithmType algorithm = SymmetricAlgorithmType.AES_GCM;
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, null!, "text"));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, null!, new byte[1]));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, key, (byte[])null!));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, null!, new ReadOnlySpan<byte>(new byte[1])));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, null!, new MemoryStream(), true));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Encrypt(algorithm, key, (Stream)null!, true));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, null!, "text"));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, null!, new byte[1]));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, key, (byte[])null!));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, null!, new ReadOnlySpan<byte>(new byte[1])));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, null!, new MemoryStream(), true));
            _ = Assert.Throws<ArgumentNullException>(() => SymmetricEncryptor.Decrypt(algorithm, key, (Stream)null!, true));

            _ = Assert.Throws<NotSupportedException>(() => SymmetricEncryptor.Encrypt((SymmetricAlgorithmType)99, key, new byte[1]));
        }

        [Fact]
        public void ZerraEncryptor_BytesAndSpans()
        {
            foreach (var algorithm in chunkedAlgorithms)
            {
                var encryptor = new ZerraEncryptor("secret", algorithm);
                var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
                Assert.Equal(data, encryptor.Decrypt(encryptor.Encrypt(data)));
                Assert.Equal(data, encryptor.Decrypt(encryptor.Encrypt(data.AsSpan())).ToArray());
                Assert.NotEqual(data, encryptor.Encrypt(data));
            }
        }
    }
}
