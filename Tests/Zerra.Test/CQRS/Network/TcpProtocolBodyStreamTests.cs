// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class TcpProtocolBodyStreamTests
    {
        [Fact]
        public void Constructor_WriteMode()
        {
            var baseStream = new MemoryStream();
            var readBuffer = Array.Empty<byte>();

            var stream = new TcpProtocolBodyStream(baseStream, readBuffer, writeMode: true, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void Constructor_ReadMode()
        {
            var baseStream = new MemoryStream();
            var readBuffer = Array.Empty<byte>();

            var stream = new TcpProtocolBodyStream(baseStream, readBuffer, writeMode: false, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanRead_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanWrite_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);

            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void CanSeek_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void Length_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void Position_Get()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public void Position_Set_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.Position = 100);
        }

        [Fact]
        public void Seek_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLength_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.SetLength(100));
        }

        [Fact]
        public void Write_InReadMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);
            var buffer = new byte[] { 1, 2, 3 };

            _ = Assert.Throws<InvalidOperationException>(() => stream.Write(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Read_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = Assert.Throws<InvalidOperationException>(() => stream.Read(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Write_AddsSegmentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            stream.Write(data, 0, data.Length);
            stream.Flush();

            // 4 bytes for length + 5 bytes for data + 4 bytes for ending
            Assert.Equal([5, 0, 0, 0, 1, 2, 3, 4, 5, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public void Write_MultipleTimes()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);

            stream.Write([1, 2, 3], 0, 3);
            stream.Write([4, 5], 0, 2);
            stream.Flush();

            // Buffered writes combine into one segment
            Assert.Equal([5, 0, 0, 0, 1, 2, 3, 4, 5, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public void Flush_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3], 0, 3);

            // Writes are buffered until flush
            Assert.Equal(0, baseStream.Length);
            stream.Flush();

            Assert.Equal([3, 0, 0, 0, 1, 2, 3, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public void Flush_WithoutData_WritesOnlyEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);

            stream.Flush();

            Assert.Equal([0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public async Task FlushAsync_WithPrefix_SendsPrefixDataAndEndingInOneWrite()
        {
            var baseStream = new WriteCountingStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false, writePrefix: new byte[] { 9, 9 });

            await stream.WriteAsync(new byte[] { 1, 2 }, TestContext.Current.CancellationToken);
            await stream.WriteAsync(new byte[] { 3 }, TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, baseStream.WriteCount);
            Assert.Equal([9, 9, 3, 0, 0, 0, 1, 2, 3, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public void Flush_WithPrefixWithoutData_SendsPrefixAndEndingInOneWrite()
        {
            var baseStream = new WriteCountingStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false, writePrefix: new byte[] { 9, 9 });

            stream.Flush();

            Assert.Equal(1, baseStream.WriteCount);
            Assert.Equal([9, 9, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Write_LargerThanBuffer_RoundTrips(bool async)
        {
            var data = Enumerable.Range(0, 100_000).Select(x => (byte)x).ToArray();
            var baseStream = new MemoryStream();
            var writer = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: true, writePrefix: new byte[] { 9 });
            if (async)
            {
                await writer.WriteAsync(data.AsMemory(0, 30_000), TestContext.Current.CancellationToken);
                await writer.WriteAsync(data.AsMemory(30_000), TestContext.Current.CancellationToken);
                await writer.FlushAsync(TestContext.Current.CancellationToken);
            }
            else
            {
                writer.Write(data, 0, 30_000);
                writer.Write(data, 30_000, data.Length - 30_000);
                writer.Flush();
            }

            var written = baseStream.ToArray();
            Assert.Equal(9, written[0]);
            var reader = new TcpProtocolBodyStream(new MemoryStream(written, 1, written.Length - 1), Array.Empty<byte>(), writeMode: false, leaveOpen: false);
            using var ms = new MemoryStream();
            await reader.CopyToAsync(ms, TestContext.Current.CancellationToken);
            Assert.Equal(data, ms.ToArray());
        }

        [Fact]
        public void Flush_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3], 0, 3);
            stream.Flush();

            _ = Assert.Throws<CqrsNetworkException>(() => stream.Flush());
        }

        [Fact]
        public async Task WriteAsync_InReadMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);
            var buffer = new byte[] { 1, 2, 3 };

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.WriteAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReadAsync_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.ReadExactlyAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task WriteAsync_AddsSegmentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            await stream.WriteAsync(data, 0, data.Length, TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            // 4 bytes for length + 5 bytes for data + 4 bytes for ending
            Assert.Equal([5, 0, 0, 0, 1, 2, 3, 4, 5, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public async Task FlushAsync_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3, TestContext.Current.CancellationToken);

            // Writes are buffered until flush
            Assert.Equal(0, baseStream.Length);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            Assert.Equal([3, 0, 0, 0, 1, 2, 3, 0, 0, 0, 0], baseStream.ToArray());
        }

        [Fact]
        public async Task FlushAsync_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3, TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            _ = await Assert.ThrowsAsync<CqrsNetworkException>(async () => await stream.FlushAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: true);

            stream.Dispose();

            // Stream should be disposed but leaveOpen=true means base stream should still be open
            Assert.True(baseStream.CanRead);
        }

        [Fact]
        public void Dispose_ClosesBaseStreamWhenNotLeaveOpen()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: false);

            stream.Dispose();

            // Stream should be disposed and base stream should be closed
            _ = Assert.Throws<ObjectDisposedException>(() => baseStream.ReadByte());
        }

        [Fact]
        public async Task DisposeAsync_ReleasesResources()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: false, leaveOpen: true);

            await stream.DisposeAsync();

            // Stream should be disposed but leaveOpen=true means base stream should still be open
            Assert.True(baseStream.CanRead);
        }

        [Fact]
        public void Position_IncrementsOnWrite()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);

            Assert.Equal(0, stream.Position);
            stream.Write([1, 2, 3], 0, 3);
            // Position counts the data written, framing is added when segments are sent
            Assert.Equal(3, stream.Position);
            stream.Write([4, 5], 0, 2);
            Assert.Equal(5, stream.Position);
        }

        //counts writes to show buffered data goes out together
        private sealed class WriteCountingStream : Stream
        {
            private readonly MemoryStream inner = new();

            public int WriteCount { get; private set; }

            public byte[] ToArray() => inner.ToArray();

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => throw new NotSupportedException(); }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteCount++;
                inner.Write(buffer, offset, count);
            }
            public override void Write(ReadOnlySpan<byte> buffer)
            {
                WriteCount++;
                inner.Write(buffer);
            }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                WriteCount++;
                inner.Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                WriteCount++;
                inner.Write(buffer.Span);
                return ValueTask.CompletedTask;
            }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }
    }
}
