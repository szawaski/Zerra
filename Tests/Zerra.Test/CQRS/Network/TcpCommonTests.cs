// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class TcpCommonTests
    {
        [Fact]
        public void BufferLength_Constant()
        {
            Assert.Equal(1024 * 8, TcpCommon.BufferLength);
        }

        [Fact]
        public void ReadToHeaderEnd_FindsHeaderEnder()
        {
            var buffer = new byte[] { 1, 2, 3, 126, 5, 6 }; // 126 is '~'
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.True(result);
            Assert.Equal(4, position); // Position after the header ender
        }

        [Fact]
        public void ReadToHeaderEnd_NoHeaderEnder()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6 };
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.False(result);
            Assert.Equal(6, position); // Position at end of buffer
        }

        [Fact]
        public void ReadToHeaderEnd_HeaderEnderAtStart()
        {
            var buffer = new byte[] { 126, 2, 3, 4, 5, 6 }; // 126 is '~'
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.True(result);
            Assert.Equal(1, position);
        }

        [Fact]
        public void BufferHeader_EndsWithHeaderEnder()
        {
            var buffer = new byte[TcpCommon.BufferLength];

            var length = TcpCommon.BufferHeader(buffer.AsMemory(), ContentType.Bytes);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void BufferHeader_WritesExactHeader()
        {
            var buffer = new byte[TcpCommon.BufferLength];

            var length = TcpCommon.BufferHeader(buffer, ContentType.Json);
            var uploadLength = TcpCommon.BufferHeader(buffer.AsMemory(length), ContentType.Json, true);
            var errorLength = TcpCommon.BufferErrorHeader(buffer.AsMemory(length + uploadLength), ContentType.Json);

            Assert.Equal($"RAW|{(int)ContentType.Json}~", System.Text.Encoding.UTF8.GetString(buffer, 0, length));
            Assert.Equal($"UPL|{(int)ContentType.Json}~", System.Text.Encoding.UTF8.GetString(buffer, length, uploadLength));
            Assert.Equal($"ERR|{(int)ContentType.Json}~", System.Text.Encoding.UTF8.GetString(buffer, length + uploadLength, errorLength));
        }

        [Fact]
        public void BufferErrorHeader_EndsWithHeaderEnder()
        {
            var buffer = new byte[TcpCommon.BufferLength];

            var length = TcpCommon.BufferErrorHeader(buffer.AsMemory(), ContentType.Bytes);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void ReadHeader_Method()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            // First buffer a header
            var headerLength = TcpCommon.BufferHeader(bufferMemory, ContentType.Bytes);

            // Then read it back
            var header = TcpCommon.ReadHeader(bufferMemory, headerLength); // -1 to exclude the enderr

            Assert.NotNull(header);
            Assert.Equal(ContentType.Bytes, header.ContentType);
            Assert.False(header.IsError);
        }

        [Fact]
        public void ReadHeader_WithErrorPrefix()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            // Buffer an error header
            var headerLength = TcpCommon.BufferErrorHeader(bufferMemory, ContentType.Bytes);

            // Then read it back
            var header = TcpCommon.ReadHeader(bufferMemory, headerLength);

            Assert.NotNull(header);
            Assert.True(header.IsError);
        }

        [Fact]
        public void BufferHeader_RoundTrip()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var originalContentType = ContentType.Json;

            // Buffer a header
            var headerLength = TcpCommon.BufferHeader(bufferMemory, originalContentType);

            // Read it back
            var header = TcpCommon.ReadHeader(bufferMemory[..headerLength], headerLength);

            Assert.Equal(originalContentType, header.ContentType);
            Assert.False(header.IsError);
        }

        [Fact]
        public void BufferErrorHeader_RoundTrip()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var originalContentType = ContentType.Bytes;

            // Buffer an error header
            var headerLength = TcpCommon.BufferErrorHeader(bufferMemory, originalContentType);

            // Read it back
            var header = TcpCommon.ReadHeader(bufferMemory[..headerLength], headerLength);

            Assert.Equal(originalContentType, header.ContentType);
            Assert.True(header.IsError);
        }

        [Theory]
        [InlineData("RAW|x~")]
        [InlineData("RAW~")]
        [InlineData("RAW|1|extra~")]
        [InlineData("RAW|Provider|1~")]
        [InlineData("BAD|1~")]
        public void ReadHeader_Invalid_Throws(string header)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(header);
            _ = Assert.Throws<CqrsNetworkException>(() => TcpCommon.ReadHeader(bytes, bytes.Length));
        }

        [Fact]
        public void ReadHeader_Upload()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("UPL|1~");
            var header = TcpCommon.ReadHeader(bytes, bytes.Length);
            Assert.True(header.IsUpload);
        }
    }
}
