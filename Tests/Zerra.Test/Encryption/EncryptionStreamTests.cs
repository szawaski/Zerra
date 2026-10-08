// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Security.Cryptography;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class EncryptionStreamTests
    {
        [Fact]
        public void CryptoPrefixStreamRead()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoPrefixStream(shiftmsin, blockSize, CryptoStreamMode.Read, false, false))
            {
                shiftstream.CopyTo(shiftmsout);
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsin, blockSize, CryptoStreamMode.Read, true, false))
            {
                unshift.CopyTo(unshiftmsout);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public void CryptoPrefixStreamWrite()
        {
            const int blockSize = 256;
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoPrefixStream(shiftmsout, blockSize, CryptoStreamMode.Write, false, false))
            {
                shift.Write(test, 0, test.Length);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length - blockSize / 8 == test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsout, blockSize, CryptoStreamMode.Write, true, false))
            {
                unshift.Write(result, 0, result.Length);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public async Task CryptoPrefixStreamReadAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsin = new MemoryStream(test))
            using (var shiftmsout = new MemoryStream())
            using (var shiftstream = new CryptoPrefixStream(shiftmsin, 256, CryptoStreamMode.Read, false, false))
            {
                await shiftstream.CopyToAsync(shiftmsout, TestContext.Current.CancellationToken);
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsin = new MemoryStream(result))
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsin, 256, CryptoStreamMode.Read, true, false))
            {
                await unshift.CopyToAsync(unshiftmsout, TestContext.Current.CancellationToken);
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public async Task CryptoPrefixStreamWriteAsync()
        {
            var test = GetTestBytes();

            byte[] result;
            using (var shiftmsout = new MemoryStream())
            using (var shift = new CryptoPrefixStream(shiftmsout, 256, CryptoStreamMode.Write, false, false))
            {
                await shift.WriteAsync(test, 0, test.Length, TestContext.Current.CancellationToken);
                shiftmsout.Position = 0;
                result = shiftmsout.ToArray();
            }

            Assert.True(result.Length > test.Length);

            byte[] unshiftresult;
            using (var unshiftmsout = new MemoryStream())
            using (var unshift = new CryptoPrefixStream(unshiftmsout, 256, CryptoStreamMode.Write, true, false))
            {
                await unshift.WriteAsync(result, 0, result.Length, TestContext.Current.CancellationToken);
                unshiftmsout.Position = 0;
                unshiftresult = unshiftmsout.ToArray();
            }

            Assert.Equal(test.Length, unshiftresult.Length);
            for (var i = 0; i < test.Length; i++)
                Assert.Equal(test[i], unshiftresult[i]);
        }

        [Fact]
        public void CryptoPrefixUnique()
        {
            var test = Convert.ToBase64String(GetTestBytes());
            var key = SymmetricEncryptor.GetKey("test", null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);

            var values = new List<string>();
            for (var i = 0; i < 100; i++)
                values.Add(SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithPrefix, key, test));

            Assert.True(values.Distinct().Count() == values.Count);
            //the whole last block, the last few base64 characters are mostly padding and collide by chance
            var endings = values.Select(x => Convert.ToBase64String(Convert.FromBase64String(x)[^16..]));
            Assert.True(endings.Distinct().Count() == values.Count);

            for (var i = 0; i < values.Count; i++)
            {
                var decrypted = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithPrefix, key, values[i]);
                Assert.Equal(test, decrypted);
            }
        }

        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            return bytes;
        }

#pragma warning disable CS0612 // Type or member is obsolete
        private static Stream CreateTransform(bool shift, Stream stream, CryptoStreamMode mode, bool decode)
            => shift
                ? new CryptoShiftStream(stream, 128, mode, decode, true)
                : new CryptoPrefixStream(stream, 128, mode, decode, true);
#pragma warning restore CS0612 // Type or member is obsolete

        private static byte[] Transform(bool shift, byte[] data, CryptoStreamMode mode, bool decode, int chunk)
        {
            using var source = new MemoryStream(data);
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                using var transform = CreateTransform(shift, source, mode, decode);
                var buffer = new byte[chunk];
                int read;
                while ((read = transform.Read(buffer, 0, buffer.Length)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                using (var transform = CreateTransform(shift, output, mode, decode))
                {
                    for (var i = 0; i < data.Length; i += chunk)
                        transform.Write(data, i, Math.Min(chunk, data.Length - i));
                }
            }
            return output.ToArray();
        }

        private static async Task<byte[]> TransformAsync(bool shift, byte[] data, CryptoStreamMode mode, bool decode, int chunk)
        {
            using var source = new MemoryStream(data);
            using var output = new MemoryStream();
            if (mode == CryptoStreamMode.Read)
            {
                await using var transform = CreateTransform(shift, source, mode, decode);
                var buffer = new byte[chunk];
                int read;
                while ((read = await transform.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
                    output.Write(buffer, 0, read);
            }
            else
            {
                await using (var transform = CreateTransform(shift, output, mode, decode))
                {
                    for (var i = 0; i < data.Length; i += chunk)
                        await transform.WriteAsync(data.AsMemory(i, Math.Min(chunk, data.Length - i)), TestContext.Current.CancellationToken);
                }
            }
            return output.ToArray();
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(false, 7)]
        [InlineData(false, 100)]
        [InlineData(false, 9_000)]
        [InlineData(true, 1)]
        [InlineData(true, 7)]
        [InlineData(true, 100)]
        [InlineData(true, 9_000)]
        public async Task TransformStreams_RoundTrip(bool shift, int chunk)
        {
            var data = Enumerable.Range(0, 20_000).Select(x => (byte)(x * 31)).ToArray();

            foreach (var encodeMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
            {
                foreach (var decodeMode in new[] { CryptoStreamMode.Read, CryptoStreamMode.Write })
                {
                    var encoded = Transform(shift, data, encodeMode, false, chunk);
                    Assert.Equal(data.Length + 16, encoded.Length);
                    if (shift)
                        Assert.NotEqual(data, encoded.Skip(16).ToArray());
                    Assert.Equal(data, Transform(shift, encoded, decodeMode, true, chunk));

                    var encodedAsync = await TransformAsync(shift, data, encodeMode, false, chunk);
                    Assert.Equal(data, await TransformAsync(shift, encodedAsync, decodeMode, true, chunk));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TransformStreams_Properties(bool shift)
        {
            using var reader = CreateTransform(shift, new MemoryStream(), CryptoStreamMode.Read, false);
            Assert.True(reader.CanRead);
            Assert.False(reader.CanWrite);
            Assert.False(reader.CanSeek);
            Assert.Equal(0, reader.Position);
            _ = Assert.Throws<NotSupportedException>(() => reader.Length);
            _ = Assert.Throws<NotSupportedException>(() => reader.Position = 1);
            _ = Assert.Throws<NotSupportedException>(() => reader.Seek(0, SeekOrigin.Begin));
            _ = Assert.Throws<NotSupportedException>(() => reader.SetLength(1));
            _ = Assert.ThrowsAny<Exception>(() => reader.Write([1], 0, 1));

            using var writer = CreateTransform(shift, new MemoryStream(), CryptoStreamMode.Write, false);
            Assert.False(writer.CanRead);
            Assert.True(writer.CanWrite);
            _ = Assert.ThrowsAny<Exception>(() => writer.Read(new byte[1], 0, 1));

#pragma warning disable CS0612 // Type or member is obsolete
            _ = Assert.Throws<ArgumentException>(() => new CryptoShiftStream(new MemoryStream(), 12, CryptoStreamMode.Read, false, false));
#pragma warning restore CS0612 // Type or member is obsolete
            _ = Assert.Throws<ArgumentException>(() => new CryptoPrefixStream(new MemoryStream(), 100, CryptoStreamMode.Read, false, false));
        }

        //returns one byte per read, so the prefix arrives in pieces
        private sealed class OneByteStream(byte[] data) : MemoryStream(data)
        {
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
            public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(buffer.Length, 1)));
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, 1)), cancellationToken);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TransformStreams_SourceInPieces(bool shift)
        {
            var data = Enumerable.Range(0, 1000).Select(x => (byte)(x * 31)).ToArray();
            var encoded = Transform(shift, data, CryptoStreamMode.Write, false, 100);

            using (var transform = CreateTransform(shift, new OneByteStream(encoded), CryptoStreamMode.Read, true))
            using (var output = new MemoryStream())
            {
                transform.CopyTo(output);
                Assert.Equal(data, output.ToArray());
            }
            await using (var transform = CreateTransform(shift, new OneByteStream(encoded), CryptoStreamMode.Read, true))
            using (var output = new MemoryStream())
            {
                await transform.CopyToAsync(output, TestContext.Current.CancellationToken);
                Assert.Equal(data, output.ToArray());
            }
        }

        [Fact]
        public async Task FlushFinalBlockAsync_WritesTheEnd()
        {
            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithPrefix);
            var data = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();
            foreach (var algorithm in new[] { SymmetricAlgorithmType.AES, SymmetricAlgorithmType.AESwithPrefix })
            {
                using var output = new MemoryStream();
                var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true);
                await encrypt.WriteAsync(data, TestContext.Current.CancellationToken);
                await encrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                await encrypt.DisposeAsync();
                Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, output.ToArray()));
            }
#pragma warning disable CS0612 // Type or member is obsolete
            foreach (var shift in new[] { false, true })
            {
                using var output = new MemoryStream();
                var transform = CreateTransform(shift, output, CryptoStreamMode.Write, false);
                await transform.WriteAsync(data, TestContext.Current.CancellationToken);
                if (transform is CryptoPrefixStream prefix)
                    await prefix.FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                else
                    await ((CryptoShiftStream)transform).FlushFinalBlockAsync(TestContext.Current.CancellationToken);
                Assert.Equal(data.Length + 16, output.Length);
            }
#pragma warning restore CS0612 // Type or member is obsolete
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
#pragma warning disable CS0612 // Type or member is obsolete
            foreach (var algorithm in new[] { SymmetricAlgorithmType.AES, SymmetricAlgorithmType.AESwithPrefix, SymmetricAlgorithmType.AESwithShift })
#pragma warning restore CS0612 // Type or member is obsolete
            {
                var key = SymmetricEncryptor.GenerateKey(algorithm);
                using var output = new DelayedWriteStream();
                var encrypt = SymmetricEncryptor.Encrypt(algorithm, key, output, true, true);
                await encrypt.WriteAsync(data, TestContext.Current.CancellationToken);
                await encrypt.FlushFinalBlockAsync(TestContext.Current.CancellationToken);

                //the whole value is written once the flush completes, before the stream is disposed
                Assert.Equal(data, SymmetricEncryptor.Decrypt(algorithm, key, output.ToArray()));
                await encrypt.DisposeAsync();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TransformStreams_KeyBlockCutShort_Throws(bool shift)
        {
            //the stream ends partway through the 16 byte key block
            using (var transform = CreateTransform(shift, new MemoryStream(new byte[5]), CryptoStreamMode.Read, true))
                _ = Assert.Throws<InvalidOperationException>(() => transform.Read(new byte[32], 0, 32));
            using (var transform = CreateTransform(shift, new MemoryStream(new byte[5]), CryptoStreamMode.Read, true))
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await transform.ReadExactlyAsync(new byte[32], TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TransformStreams_WrongDirection_Throws(bool shift)
        {
            using (var reader = CreateTransform(shift, new MemoryStream(new byte[32]), CryptoStreamMode.Read, true))
            {
                _ = Assert.Throws<InvalidOperationException>(() => reader.Write(new byte[1], 0, 1));
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.WriteAsync(new byte[1], TestContext.Current.CancellationToken));
            }
            using (var writer = CreateTransform(shift, new MemoryStream(), CryptoStreamMode.Write, false))
            {
                _ = Assert.Throws<InvalidOperationException>(() => writer.Read(new byte[1], 0, 1));
                _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await writer.ReadExactlyAsync(new byte[1], TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public void SymmetricEncryptor_NullArguments()
        {
            var key = SymmetricEncryptor.GenerateKey(SymmetricAlgorithmType.AESwithPrefix);
            const SymmetricAlgorithmType algorithm = SymmetricAlgorithmType.AESwithPrefix;
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
        }

        [Fact]
        public void ZerraEncryptor_BytesAndSpans()
        {
            var encryptor = new ZerraEncryptor("secret", SymmetricAlgorithmType.AESwithPrefix);
            var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
            Assert.Equal(data, encryptor.Decrypt(encryptor.Encrypt(data)));
            Assert.Equal(data, encryptor.Decrypt(encryptor.Encrypt(data.AsSpan())).ToArray());
            Assert.NotEqual(data, encryptor.Encrypt(data));
        }
    }
}
