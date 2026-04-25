// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class HttpCommonTests
    {
        [Fact]
        public void BufferLength_Constant()
        {
            Assert.Equal(1024 * 16, HttpCommon.BufferLength);
        }

        [Fact]
        public void ReadToHeaderEnd_FindsHeaderEnd()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49, 32, 50, 48, 48, 32, 79, 75, 13, 10, 13, 10 }; // HTTP/1.1 200 OK\r\n\r\n
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = HttpCommon.TryReadToHeaderEnd(bufferMemory, ref position);

            Assert.True(result);
            Assert.Equal(buffer.Length, position);
        }

        [Fact]
        public void ReadToHeaderEnd_NoHeaderEnd()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49 }; // HTTP/1.1
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = HttpCommon.TryReadToHeaderEnd(bufferMemory, ref position);

            Assert.False(result);
        }

        [Fact]
        public void ReadToBreak_FindsLineBreak()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49, 13, 10, 13, 10 }; // HTTP/1.1\r\n\r\n
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = HttpCommon.ReadToBreak(bufferMemory, ref position);

            Assert.True(result);
        }

        [Fact]
        public void ReadToBreak_NoLineBreak()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49 }; // HTTP/1.1
            var bufferMemory = buffer.AsMemory();
            var position = 0;
            var result = HttpCommon.ReadToBreak(bufferMemory, ref position);

            Assert.False(result);
        }

        [Fact]
        public void BufferPreflightResponse_WithOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var origin = "https://example.com";

            var length = HttpCommon.BufferPreflightResponse(bufferMemory, origin);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferPreflightResponse_WithoutOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferPreflightResponse(bufferMemory, null);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferPostRequestHeader_WithAllParameters()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var origin = "https://example.com";
            var providerType = "ITestQueryHandler";
            var contentType = ContentType.Bytes;
            var authHeaders = new Dictionary<string, List<string?>>
            {
                { "Authorization", new List<string?> { "Bearer token123" } }
            };

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, origin, providerType, contentType, authHeaders);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferPostRequestHeader_WithoutAuthHeaders()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var origin = "https://example.com";
            var providerType = "ITestQueryHandler";
            var contentType = ContentType.Json;

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, origin, providerType, contentType, null);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferPostRequestHeader_WithJsonContentType()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var contentType = ContentType.JsonNameless;

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, null, null, contentType, null);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferErrorResponseHeader_WithOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var origin = "https://example.com";

            var length = HttpCommon.BufferErrorResponseHeader(bufferMemory, origin);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferErrorResponseHeader_WithoutOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferErrorResponseHeader(bufferMemory, null);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferOkResponseHeader_Simple()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferOkResponseHeader(bufferMemory);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferOkResponseHeader_WithParameters()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var origin = "https://example.com";
            var providerType = "ITestQueryHandler";
            var contentType = ContentType.Bytes;
            var authHeaders = new Dictionary<string, List<string?>>
            {
                { "Authorization", new List<string?> { "Bearer token123" } }
            };

            var length = HttpCommon.BufferOkResponseHeader(bufferMemory, origin, providerType, contentType, authHeaders);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferOkResponseHeader_WithJsonContentType()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var contentType = ContentType.JsonNameless;

            var length = HttpCommon.BufferOkResponseHeader(bufferMemory, null, null, contentType, null);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferNotFoundResponseHeader()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferNotFoundResponseHeader(bufferMemory);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void ReadHeader_ValidHttpHeader()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            // Buffer a valid response header
            var headerLength = HttpCommon.BufferOkResponseHeader(bufferMemory, "https://example.com", "TestProvider", ContentType.Bytes, null);

            // Read it back
            var header = HttpCommon.ReadHeader(bufferMemory, headerLength);

            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal("TestProvider", header.ProviderType);
            Assert.Equal(ContentType.Bytes, header.ContentType);
        }

        [Fact]
        public void ReadHeader_ErrorResponseHeader()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            // Buffer an error response header
            var headerLength = HttpCommon.BufferErrorResponseHeader(bufferMemory, "https://example.com");

            // Read it back
            var header = HttpCommon.ReadHeader(bufferMemory, headerLength);

            Assert.NotNull(header);
            Assert.True(header.IsError);
        }

        [Fact]
        public void BufferPostRequestHeader_WithMultipleAuthHeaders()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var authHeaders = new Dictionary<string, List<string?>>
            {
                { "Authorization", new List<string?> { "Bearer token123" } },
                { "X-Custom-Header", new List<string?> { "value1", "value2" } }
            };

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, null, "TestProvider", ContentType.Json, authHeaders);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferOkResponseHeader_AllContentTypes()
        {
            foreach (var contentType in new[] { ContentType.Bytes, ContentType.Json, ContentType.JsonNameless })
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var bufferMemory = buffer.AsMemory();

                var length = HttpCommon.BufferOkResponseHeader(bufferMemory, null, null, contentType, null);

                Assert.True(length > 0);
                Assert.True(length < HttpCommon.BufferLength);
            }
        }
    }
}
