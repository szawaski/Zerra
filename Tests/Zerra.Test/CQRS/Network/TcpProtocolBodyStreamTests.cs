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
            
            // Should have written: 4 bytes for length + 5 bytes for data = 9 bytes
            Assert.Equal(9, baseStream.Length);
        }

        [Fact]
        public void Write_MultipleTimes()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            
            stream.Write([1, 2, 3], 0, 3);
            stream.Write([4, 5], 0, 2);
            
            Assert.True(baseStream.Length > 0);
        }

        [Fact]
        public void Flush_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            stream.Write([1, 2, 3], 0, 3);
            
            var lengthBeforeFlush = baseStream.Length;
            stream.Flush();
            
            // Should have written 4 more bytes for ending marker
            Assert.Equal(lengthBeforeFlush + 4, baseStream.Length);
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

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.WriteAsync(buffer, 0, buffer.Length));
        }

        [Fact]
        public async Task ReadAsync_InWriteMode_Throws()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var buffer = new byte[10];

            _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.ReadAsync(buffer, 0, buffer.Length));
        }

        [Fact]
        public async Task WriteAsync_AddsSegmentLength()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            var data = new byte[] { 1, 2, 3, 4, 5 };
            
            await stream.WriteAsync(data, 0, data.Length);
            
            // Should have written: 4 bytes for length + 5 bytes for data = 9 bytes
            Assert.Equal(9, baseStream.Length);
        }

        [Fact]
        public async Task FlushAsync_InWriteMode_WritesEnding()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3);
            
            var lengthBeforeFlush = baseStream.Length;
            await stream.FlushAsync();
            
            // Should have written 4 more bytes for ending marker
            Assert.Equal(lengthBeforeFlush + 4, baseStream.Length);
        }

        [Fact]
        public async Task FlushAsync_TwiceThrows()
        {
            var baseStream = new MemoryStream();
            var stream = new TcpProtocolBodyStream(baseStream, Array.Empty<byte>(), writeMode: true, leaveOpen: false);
            await stream.WriteAsync([1, 2, 3], 0, 3);
            await stream.FlushAsync();

            _ = await Assert.ThrowsAsync<CqrsNetworkException>(async () => await stream.FlushAsync());
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
            // Position includes both segment length (4 bytes) + data (3 bytes) = 7 bytes
            Assert.Equal(7, stream.Position);
            stream.Write([4, 5], 0, 2);
            // Position is cumulative: (4 + 3) + (4 + 2) = 13 bytes
            Assert.Equal(13, stream.Position);
        }
    }
}
