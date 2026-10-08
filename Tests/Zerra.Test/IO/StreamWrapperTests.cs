// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.IO;

namespace Zerra.Test.IO
{
    public class StreamWrapperTests
    {
        private sealed class PassThroughWrapper(Stream stream, bool leaveOpen) : StreamWrapper(stream, leaveOpen) { }

        private sealed class XorTransform(Stream stream, bool leaveOpen) : StreamTransform(stream, leaveOpen)
        {
            public override long Length => stream.Length;
            public override long Position { get => stream.Position; set => stream.Position = value; }
            public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
            public override void SetLength(long value) => stream.SetLength(value);

            protected override int InternalRead(Span<byte> buffer)
            {
                var read = stream.Read(buffer);
                for (var i = 0; i < read; i++)
                    buffer[i] ^= 0x5A;
                return read;
            }
            protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);
                for (var i = 0; i < read; i++)
                    buffer.Span[i] ^= 0x5A;
                return read;
            }
            protected override void InternalWrite(ReadOnlySpan<byte> buffer)
            {
                var copy = buffer.ToArray();
                for (var i = 0; i < copy.Length; i++)
                    copy[i] ^= 0x5A;
                stream.Write(copy);
            }
            protected override ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var copy = buffer.ToArray();
                for (var i = 0; i < copy.Length; i++)
                    copy[i] ^= 0x5A;
                return stream.WriteAsync(copy, cancellationToken);
            }
        }

        [Fact]
        public async Task StreamWrapper_Forwards()
        {
            var token = TestContext.Current.CancellationToken;
            var inner = new MemoryStream();
            using (var wrapper = new PassThroughWrapper(inner, true))
            {
                Assert.True(wrapper.CanRead);
                Assert.True(wrapper.CanWrite);
                Assert.True(wrapper.CanSeek);
                Assert.False(wrapper.CanTimeout);
                _ = Assert.Throws<InvalidOperationException>(() => wrapper.ReadTimeout);
                _ = Assert.Throws<InvalidOperationException>(() => wrapper.ReadTimeout = 1);
                _ = Assert.Throws<InvalidOperationException>(() => wrapper.WriteTimeout);
                _ = Assert.Throws<InvalidOperationException>(() => wrapper.WriteTimeout = 1);

                wrapper.Write([1, 2], 0, 2);
                wrapper.Write([3]);
                await wrapper.WriteAsync(new byte[] { 4 }, 0, 1, token);
                await wrapper.WriteAsync(new byte[] { 5 }, token);
                wrapper.WriteByte(6);
                wrapper.EndWrite(wrapper.BeginWrite([7], 0, 1, null, null));
                wrapper.Flush();
                await wrapper.FlushAsync(token);
                Assert.Equal(7, wrapper.Length);
                Assert.Equal(7, wrapper.Position);

                wrapper.SetLength(8);
                Assert.Equal(0, wrapper.Seek(0, SeekOrigin.Begin));
                wrapper.Position = 0;
                var buffer = new byte[2];
                Assert.Equal(2, wrapper.Read(buffer, 0, 2));
                Assert.Equal(1, wrapper.Read(buffer.AsSpan(0, 1)));
                Assert.Equal(1, await wrapper.ReadAsync(buffer, 0, 1, token));
                Assert.Equal(1, await wrapper.ReadAsync(buffer.AsMemory(0, 1), token));
                Assert.Equal(6, wrapper.ReadByte());
                Assert.Equal(1, wrapper.EndRead(wrapper.BeginRead(buffer, 0, 1, null, null)));

                wrapper.Position = 0;
                using var copy1 = new MemoryStream();
                wrapper.CopyTo(copy1, 4);
                Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0 }, copy1.ToArray());
                wrapper.Position = 0;
                using var copy2 = new MemoryStream();
                await wrapper.CopyToAsync(copy2, 4, token);
                Assert.Equal(copy1.ToArray(), copy2.ToArray());
            }
            Assert.True(inner.CanRead);

            using (new PassThroughWrapper(inner, false)) { }
            Assert.False(inner.CanRead);

            var inner2 = new MemoryStream();
            await new PassThroughWrapper(inner2, false).DisposeAsync();
            Assert.False(inner2.CanRead);
            var inner3 = new MemoryStream();
            new PassThroughWrapper(inner3, false).Close();
            Assert.False(inner3.CanRead);
            _ = Assert.Throws<ArgumentNullException>(() => new PassThroughWrapper(null!, false));
        }

        [Fact]
        public async Task StreamTransform_RoutesThroughTransform()
        {
            var token = TestContext.Current.CancellationToken;
            var inner = new MemoryStream();
            using (var transform = new XorTransform(inner, true))
            {
                Assert.False(transform.CanTimeout);
                _ = Assert.Throws<InvalidOperationException>(() => transform.ReadTimeout);
                _ = Assert.Throws<InvalidOperationException>(() => transform.WriteTimeout = 1);
                _ = Assert.Throws<NotSupportedException>(() => transform.BeginRead(new byte[1], 0, 1, null, null));
                _ = Assert.Throws<NotSupportedException>(() => transform.EndRead(null!));
                _ = Assert.Throws<NotSupportedException>(() => transform.BeginWrite(new byte[1], 0, 1, null, null));
                _ = Assert.Throws<NotSupportedException>(() => transform.EndWrite(null!));

                transform.Write([1, 2], 0, 2);
                transform.Write([3]);
                await transform.WriteAsync(new byte[] { 4 }, 0, 1, token);
                await transform.WriteAsync(new byte[] { 5 }, token);
                transform.WriteByte(6);
                transform.Flush();
                await transform.FlushAsync(token);
                Assert.Equal(new byte[] { 1 ^ 0x5A, 2 ^ 0x5A, 3 ^ 0x5A, 4 ^ 0x5A, 5 ^ 0x5A, 6 ^ 0x5A }, inner.ToArray());

                transform.Position = 0;
                var buffer = new byte[2];
                Assert.Equal(2, transform.Read(buffer, 0, 2));
                Assert.Equal(new byte[] { 1, 2 }, buffer);
                Assert.Equal(1, transform.Read(buffer.AsSpan(0, 1)));
                Assert.Equal(1, await transform.ReadAsync(buffer, 0, 1, token));
                Assert.Equal(1, await transform.ReadAsync(buffer.AsMemory(0, 1), token));
                Assert.Equal(6, transform.ReadByte());
                Assert.Equal(-1, transform.ReadByte());

                transform.Position = 0;
                using var copy1 = new MemoryStream();
                transform.CopyTo(copy1, 4);
                Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, copy1.ToArray());
                transform.Position = 0;
                using var copy2 = new MemoryStream();
                await transform.CopyToAsync(copy2, 4, token);
                Assert.Equal(copy1.ToArray(), copy2.ToArray());
            }
            Assert.True(inner.CanRead);

            var inner2 = new MemoryStream();
            await new XorTransform(inner2, false).DisposeAsync();
            Assert.False(inner2.CanRead);
            var inner3 = new MemoryStream();
            new XorTransform(inner3, false).Close();
            Assert.False(inner3.CanRead);
            _ = Assert.Throws<ArgumentNullException>(() => new XorTransform(null!, false));
        }
    }
}