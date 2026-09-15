// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;


namespace Zerra.Test.CQRS.Network
{
    public class HttpProtocolBodyStreamTests
    {
        [Fact]
        public void Constructor_WriteMode_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var readBuffer = new byte[] { };

            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, readBuffer, writeMode: true, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanWrite);
            Assert.False(stream.CanRead);
        }

        [Fact]
        public void Constructor_WriteMode_WithoutContentLength()
        {
            var baseStream = new MemoryStream();
            var readBuffer = new byte[] { };

            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, readBuffer, writeMode: true, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void Constructor_ReadMode_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var readBuffer = new byte[] { };

            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, readBuffer, writeMode: false, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void Constructor_ReadMode_WithoutContentLength()
        {
            var baseStream = new MemoryStream();
            var readBuffer = new byte[] { };

            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, readBuffer, writeMode: false, leaveOpen: false);

            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanRead_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanWrite_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);

            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void CanSeek_Property()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void Length_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            Assert.Equal(100, stream.Length);
        }

        [Fact]
        public void Length_WithoutContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public void Position_Get()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public void Position_Set_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.Position = 100);
        }

        [Fact]
        public void Seek_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLength_ThrowsNotSupported()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            _ = Assert.Throws<NotSupportedException>(() => stream.SetLength(100));
        }

        [Fact]
        public void Write_InReadMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);
            var buffer = new byte[] { 1, 2, 3 };

            _ = Assert.Throws<InvalidOperationException>(() => stream.Write(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Read_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = Assert.Throws<InvalidOperationException>(() => stream.Read(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Write_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            stream.Write(data, 0, data.Length);

            // Should write data as-is (no chunking with content-length)
            Assert.Equal(5, baseStream.Length);
        }

        [Fact]
        public void Write_WithoutContentLength_UsesChunkedEncoding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            stream.Write(data, 0, data.Length);

            // Writes are buffered until flush
            Assert.Equal(0, baseStream.Length);
            stream.Flush();

            // Should write: padded hex length + \r\n + data + \r\n + ending
            byte[] expected = [.. "00000005\r\n"u8, 1, 2, 3, 4, 5, .. "\r\n0\r\n\r\n"u8];
            Assert.Equal(expected, baseStream.ToArray());
        }

        [Fact]
        public void Write_MultipleTimes()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);

            stream.Write([1, 2, 3], 0, 3);
            stream.Write([4, 5], 0, 2);

            Assert.Equal(5, baseStream.Length);
        }

        [Fact]
        public void Flush_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3], 0, 3);

            var lengthBeforeFlush = baseStream.Length;
            stream.Flush();

            // Should have written ending bytes (0\r\n\r\n)
            Assert.True(baseStream.Length > lengthBeforeFlush);
        }

        [Fact]
        public void Flush_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3], 0, 3);
            stream.Flush();

            _ = Assert.Throws<CqrsNetworkException>(() => stream.Flush());
        }

        [Fact]
        public void Flush_InReadMode()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            // Should not throw in read mode
            stream.Flush();
        }

        [Fact]
        public async Task WriteAsync_InReadMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);
            var buffer = new byte[] { 1, 2, 3 };

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.WriteAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReadAsync_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.ReadExactlyAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task WriteAsync_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            await stream.WriteAsync(data, 0, data.Length, TestContext.Current.CancellationToken);

            Assert.Equal(5, baseStream.Length);
        }

        [Fact]
        public async Task WriteAsync_WithoutContentLength_UsesChunkedEncoding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            await stream.WriteAsync(data, 0, data.Length, TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            byte[] expected = [.. "00000005\r\n"u8, 1, 2, 3, 4, 5, .. "\r\n0\r\n\r\n"u8];
            Assert.Equal(expected, baseStream.ToArray());
        }

        [Fact]
        public async Task FlushAsync_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3, TestContext.Current.CancellationToken);

            var lengthBeforeFlush = baseStream.Length;
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            Assert.True(baseStream.Length > lengthBeforeFlush);
        }

        [Fact]
        public async Task FlushAsync_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3, TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            _ = await Assert.ThrowsAsync<CqrsNetworkException>(async () => await stream.FlushAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: true);

            stream.Dispose();

            Assert.True(baseStream.CanRead);
        }

        [Fact]
        public void Dispose_ClosesBaseStreamWhenNotLeaveOpen()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: false);

            stream.Dispose();

            _ = Assert.Throws<ObjectDisposedException>(() => baseStream.ReadByte());
        }

        [Fact]
        public async Task DisposeAsync_ReleasesResources()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: false, leaveOpen: true);

            await stream.DisposeAsync();

            Assert.True(baseStream.CanRead);
        }

        [Fact]
        public void Position_IncrementsOnWrite_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);

            Assert.Equal(0, stream.Position);
            stream.Write([1, 2, 3], 0, 3);
            // With ContentLength, writes data directly with no format overhead
            Assert.Equal(3, stream.Position);
            stream.Write([4, 5], 0, 2);
            Assert.Equal(5, stream.Position);
        }

        [Fact]
        public void Position_IncrementsOnWrite_WithoutContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);

            Assert.Equal(0, stream.Position);
            stream.Write([1, 2, 3], 0, 3);
            // Position counts the data written, chunk framing is added when chunks are sent
            Assert.Equal(3, stream.Position);
        }

        [Fact]
        public async Task FlushAsync_WithPrefix_SendsPrefixChunkAndEndingInOneWrite()
        {
            var baseStream = new WriteCountingStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: true, leaveOpen: false, writePrefix: "HEADER"u8.ToArray());

            await stream.WriteAsync("hel"u8.ToArray(), TestContext.Current.CancellationToken);
            await stream.WriteAsync("lo"u8.ToArray(), TestContext.Current.CancellationToken);
            await stream.FlushAsync(TestContext.Current.CancellationToken);

            Assert.Equal(1, baseStream.WriteCount);
            Assert.Equal("HEADER00000005\r\nhello\r\n0\r\n\r\n", System.Text.Encoding.UTF8.GetString(baseStream.ToArray()));
        }

        [Fact]
        public void Flush_WithPrefixWithoutData_SendsPrefixAndEndingInOneWrite()
        {
            var baseStream = new WriteCountingStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: true, leaveOpen: false, writePrefix: "HEADER"u8.ToArray());

            stream.Flush();

            Assert.Equal(1, baseStream.WriteCount);
            Assert.Equal("HEADER0\r\n\r\n", System.Text.Encoding.UTF8.GetString(baseStream.ToArray()));
        }

        [Fact]
        public void Constructor_WithPrefixAndContentLength_Throws()
        {
            _ = Assert.Throws<ArgumentException>(() => new HttpProtocolBodyStream(10, new MemoryStream(), null, writeMode: true, leaveOpen: false, writePrefix: "HEADER"u8.ToArray()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteChunked_LargerThanBuffer_RoundTrips(bool async)
        {
            var data = Enumerable.Range(0, 100_000).Select(x => (byte)x).ToArray();
            var baseStream = new MemoryStream();
            var writer = new HttpProtocolBodyStream(null, baseStream, null, writeMode: true, leaveOpen: true, writePrefix: "H"u8.ToArray());
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
            Assert.Equal((byte)'H', written[0]);
            var reader = new HttpProtocolBodyStream(null, new NoReadPastEndStream(written[1..]), null, writeMode: false, leaveOpen: false);
            using var ms = new MemoryStream();
            await reader.CopyToAsync(ms, TestContext.Current.CancellationToken);
            Assert.Equal(data, ms.ToArray());
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteWithContentLength_ThenFlush_WritesOnlyBody(bool async)
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 5, baseStream, new byte[] { }, writeMode: true, leaveOpen: true);
            if (async)
            {
                await stream.WriteAsync(new byte[] { 1, 2, 3, 4, 5 }, TestContext.Current.CancellationToken);
                await stream.FlushAsync(TestContext.Current.CancellationToken);
            }
            else
            {
                stream.Write([1, 2, 3, 4, 5], 0, 5);
                stream.Flush();
            }

            //the chunked ending would be read as the start of the next message
            Assert.Equal([1, 2, 3, 4, 5], baseStream.ToArray());
        }

        [Fact]
        public void WriteWithoutContentLength_ThenFlush()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3, 4, 5], 0, 5);

            var lengthBeforeFlush = baseStream.Length;
            stream.Flush();

            // Without content length, flush adds ending bytes
            Assert.True(baseStream.Length > lengthBeforeFlush);
        }

        [Theory]
        [InlineData("5\r\nhello\r\n0\r\n\r\n", "hello")]
        [InlineData("3\r\nhel\r\n2\r\nlo\r\n0\r\n\r\n", "hello")]
        [InlineData("0\r\n\r\n", "")]
        public void ReadChunked_DoesNotReadPastBody(string body, string expected)
        {
            //a network stream blocks when read past the body, this throws instead
            var baseStream = new NoReadPastEndStream(System.Text.Encoding.UTF8.GetBytes(body));
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Theory]
        [InlineData("5\r\nhello\r\n0\r\n\r\n", "hello")]
        [InlineData("3\r\nhel\r\n2\r\nlo\r\n0\r\n\r\n", "hello")]
        [InlineData("0\r\n\r\n", "")]
        public async Task ReadChunkedAsync_DoesNotReadPastBody(string body, string expected)
        {
            //a network stream blocks when read past the body, this throws instead
            var baseStream = new NoReadPastEndStream(System.Text.Encoding.UTF8.GetBytes(body));
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Fact]
        public async Task ReadChunkedAsync_BodyInReadStartBuffer_DoesNotReadStream()
        {
            //the whole body arrived with the header
            var baseStream = new NoReadPastEndStream([]);
            var stream = new HttpProtocolBodyStream(null, baseStream, System.Text.Encoding.UTF8.GetBytes("5\r\nhello\r\n0\r\n\r\n"), writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Fact]
        public async Task ReadChunkedAsync_RoundTripsWrittenBody()
        {
            var data = Enumerable.Range(0, 1000).Select(x => (byte)x).ToArray();
            var baseStream = new MemoryStream();
            var writer = new HttpProtocolBodyStream(null, baseStream, null, writeMode: true, leaveOpen: true);
            await writer.WriteAsync(data.AsMemory(0, 600), TestContext.Current.CancellationToken);
            await writer.WriteAsync(data.AsMemory(600), TestContext.Current.CancellationToken);
            await writer.FlushAsync(TestContext.Current.CancellationToken);

            var reader = new HttpProtocolBodyStream(null, new NoReadPastEndStream(baseStream.ToArray()), null, writeMode: false, leaveOpen: true);
            using var ms = new MemoryStream();
            await reader.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal(data, ms.ToArray());
        }

        [Fact]
        public async Task ReadChunkedAsync_StreamEndsInSegmentLength_Throws()
        {
            var stream = new HttpProtocolBodyStream(null, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("5\r\nhello\r\n")), null, writeMode: false, leaveOpen: true);

            _ = await Assert.ThrowsAsync<ConnectionAbortedException>(() => stream.CopyToAsync(new MemoryStream(), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReadChunkedAsync_ConsumesLineBreakEndingBody()
        {
            //the line break ending the body arrives in a later read, leaving it would corrupt the next response on a reused connection
            var baseStream = new NoReadPastEndStream(System.Text.Encoding.UTF8.GetBytes("5\r\nhello\r\n0\r\n"), System.Text.Encoding.UTF8.GetBytes("\r\n"));
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(ms.ToArray()));
            Assert.True(baseStream.AtEnd);
        }

        [Fact]
        public void ReadChunked_ConsumesLineBreakEndingBody()
        {
            var baseStream = new NoReadPastEndStream(System.Text.Encoding.UTF8.GetBytes("5\r\nhello\r\n0\r\n"), System.Text.Encoding.UTF8.GetBytes("\r\n"));
            var stream = new HttpProtocolBodyStream(null, baseStream, null, writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(ms.ToArray()));
            Assert.True(baseStream.AtEnd);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadContentLength_BodyArrivesInPieces_ReadsWholeBody(bool async)
        {
            //each read asks for more than has arrived so the reads return in pieces
            var data = Enumerable.Range(0, 100).Select(x => (byte)x).ToArray();
            var baseStream = new NoReadPastEndStream(data[..30], data[30..60], data[60..]);
            var stream = new HttpProtocolBodyStream(data.Length, baseStream, null, writeMode: false, leaveOpen: true);

            using var ms = new MemoryStream();
            var buffer = new byte[50];
            int read;
            while ((read = async ? await stream.ReadAsync(buffer, TestContext.Current.CancellationToken) : stream.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            Assert.Equal(data, ms.ToArray());
            Assert.True(baseStream.AtEnd);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadChunked_StreamEndsInChunk_Throws(bool async)
        {
            //the chunk claims 5 bytes but the connection closes after 3
            var stream = new HttpProtocolBodyStream(null, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("5\r\nhel")), null, writeMode: false, leaveOpen: true);
            var buffer = new byte[10];

            if (async)
                //a single inexact read is the point of this test, it should observe the short read and throw
#pragma warning disable CA2022 // Avoid inexact read
                _ = await Assert.ThrowsAsync<ConnectionAbortedException>(async () => await stream.ReadAsync(buffer, TestContext.Current.CancellationToken));
#pragma warning restore CA2022 // Avoid inexact read
            else
                _ = await Assert.ThrowsAsync<ConnectionAbortedException>(() => Task.Run(() => stream.Read(buffer, 0, buffer.Length), TestContext.Current.CancellationToken));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReadContentLength_StreamEndsEarly_Throws(bool async)
        {
            //the body claims 10 bytes but the connection closes after 3
            var stream = new HttpProtocolBodyStream(10, new MemoryStream([1, 2, 3]), null, writeMode: false, leaveOpen: true);
            var buffer = new byte[10];

            if (async)
                //a single inexact read is the point of this test, it should observe the short read and throw
#pragma warning disable CA2022 // Avoid inexact read
                _ = await Assert.ThrowsAsync<ConnectionAbortedException>(async () => await stream.ReadAsync(buffer, TestContext.Current.CancellationToken));
#pragma warning restore CA2022 // Avoid inexact read
            else
                _ = await Assert.ThrowsAsync<ConnectionAbortedException>(() => Task.Run(() => stream.Read(buffer, 0, buffer.Length), TestContext.Current.CancellationToken));
        }

        //returns each byte array from a separate read, and throws if read past the last one
        private sealed class NoReadPastEndStream : Stream
        {
            private readonly byte[][] reads;
            private int readIndex;
            private int position;

            public NoReadPastEndStream(params byte[][] reads)
            {
                this.reads = reads;
            }

            public bool AtEnd => readIndex == reads.Length || (readIndex == reads.Length - 1 && position == reads[readIndex].Length);

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
            public override int Read(Span<byte> buffer)
            {
                while (readIndex < reads.Length && position == reads[readIndex].Length)
                {
                    readIndex++;
                    position = 0;
                }
                if (readIndex == reads.Length)
                    throw new InvalidOperationException("Read past the end of the body");
                var bytes = reads[readIndex];
                var count = Math.Min(buffer.Length, bytes.Length - position);
                bytes.AsSpan(position, count).CopyTo(buffer);
                position += count;
                return count;
            }
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromResult(Read(buffer.Span));
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(Read(buffer.AsSpan(offset, count)));

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
