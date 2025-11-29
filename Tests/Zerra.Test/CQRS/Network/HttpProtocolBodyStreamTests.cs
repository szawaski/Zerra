// Copyright © KaKush LLC
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

            // Should write: hex length + \r\n + data + \r\n
            Assert.True(baseStream.Length > 5);
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

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.WriteAsync(buffer, 0, buffer.Length));
        }

        [Fact]
        public async Task ReadAsync_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.ReadAsync(buffer, 0, buffer.Length));
        }

        [Fact]
        public async Task WriteAsync_WithContentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            await stream.WriteAsync(data, 0, data.Length);

            Assert.Equal(5, baseStream.Length);
        }

        [Fact]
        public async Task WriteAsync_WithoutContentLength_UsesChunkedEncoding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            await stream.WriteAsync(data, 0, data.Length);

            Assert.True(baseStream.Length > 5);
        }

        [Fact]
        public async Task FlushAsync_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3);

            var lengthBeforeFlush = baseStream.Length;
            await stream.FlushAsync();

            Assert.True(baseStream.Length > lengthBeforeFlush);
        }

        [Fact]
        public async Task FlushAsync_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: null, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3);
            await stream.FlushAsync();

            _ = await Assert.ThrowsAsync<CqrsNetworkException>(async () => await stream.FlushAsync());
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
            // Without ContentLength, uses chunked encoding: hex length + \r\n + data + \r\n
            // "3\r\n" (3 bytes) + [1,2,3] (3 bytes) + "\r\n" (2 bytes) = 8 bytes
            Assert.Equal(8, stream.Position);
        }

        [Fact]
        public void WriteWithContentLength_ThenFlush()
        {
            var baseStream = new MemoryStream();
            var stream = new HttpProtocolBodyStream(contentLength: 100, baseStream, new byte[] { }, writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3, 4, 5], 0, 5);

            var lengthBeforeFlush = baseStream.Length;
            stream.Flush();

            // Flush writes ending bytes (0\r\n\r\n) - 5 bytes
            Assert.Equal(lengthBeforeFlush + 5, baseStream.Length);
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
    }
}
