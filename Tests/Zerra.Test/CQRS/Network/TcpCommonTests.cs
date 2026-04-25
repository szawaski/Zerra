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
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(bufferMemory, ref position);

            Assert.True(result);
            Assert.Equal(4, position); // Position after the header ender
        }

        [Fact]
        public void ReadToHeaderEnd_NoHeaderEnder()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6 };
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(bufferMemory, ref position);

            Assert.False(result);
            Assert.Equal(6, position); // Position at end of buffer
        }

        [Fact]
        public void ReadToHeaderEnd_HeaderEnderAtStart()
        {
            var buffer = new byte[] { 126, 2, 3, 4, 5, 6 }; // 126 is '~'
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = TcpCommon.TryReadToHeaderEnd(bufferMemory, ref position);

            Assert.True(result);
            Assert.Equal(1, position);
        }

        [Fact]
        public void BufferHeader_WithProviderType()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.Bytes;

            var length = TcpCommon.BufferHeader(bufferMemory, "TestProvider", contentType);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);

            // Verify the header ends with ~
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void BufferHeader_WithoutProviderType()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.Bytes;

            var length = TcpCommon.BufferHeader(bufferMemory, "", contentType);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void BufferHeader_WithNullProviderType()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.Bytes;

            var length = TcpCommon.BufferHeader(bufferMemory, null, contentType);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void BufferErrorHeader_WithProviderType()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.Bytes;

            var length = TcpCommon.BufferErrorHeader(bufferMemory, "TestProvider", contentType);

            Assert.True(length > 0);
            Assert.True(length < TcpCommon.BufferLength);
            Assert.Equal(126, buffer[length - 1]); // 126 is '~'
        }

        [Fact]
        public void BufferErrorHeader_WithoutProviderType()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.Bytes;

            var length = TcpCommon.BufferErrorHeader(bufferMemory, null, contentType);

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
            var headerLength = TcpCommon.BufferHeader(bufferMemory, "MyProvider", ContentType.Bytes);

            // Then read it back
            var header = TcpCommon.ReadHeader(bufferMemory, headerLength); // -1 to exclude the enderr

            Assert.NotNull(header);
            Assert.Equal("MyProvider", header.ProviderType);
            Assert.Equal(ContentType.Bytes, header.ContentType);
            Assert.False(header.IsError);
        }

        [Fact]
        public void ReadHeader_WithErrorPrefix()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            // Buffer an error header
            var headerLength = TcpCommon.BufferErrorHeader(bufferMemory, "ErrorProvider", ContentType.Bytes);

            // Then read it back
            var header = TcpCommon.ReadHeader(bufferMemory, headerLength);

            Assert.NotNull(header);
            Assert.Equal("ErrorProvider", header.ProviderType);
            Assert.True(header.IsError);
        }

        [Fact]
        public void BufferHeader_RoundTrip()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var originalProvider = "RoundTripProvider";
            var originalContentType = ContentType.Json;

            // Buffer a header
            var headerLength = TcpCommon.BufferHeader(bufferMemory, originalProvider, originalContentType);

            // Read it back
            var header = TcpCommon.ReadHeader(bufferMemory[..headerLength], headerLength);

            Assert.Equal(originalProvider, header.ProviderType);
            Assert.Equal(originalContentType, header.ContentType);
            Assert.False(header.IsError);
        }

        [Fact]
        public void BufferErrorHeader_RoundTrip()
        {
            var buffer = new byte[TcpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var originalProvider = "ErrorProvider";
            var originalContentType = ContentType.Bytes;

            // Buffer an error header
            var headerLength = TcpCommon.BufferErrorHeader(bufferMemory, originalProvider, originalContentType);

            // Read it back
            var header = TcpCommon.ReadHeader(bufferMemory[..headerLength], headerLength);

            Assert.Equal(originalProvider, header.ProviderType);
            Assert.Equal(originalContentType, header.ContentType);
            Assert.True(header.IsError);
        }
    }
}
