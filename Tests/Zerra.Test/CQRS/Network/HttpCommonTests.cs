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
            var position = 0;
            var result = HttpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.True(result);
            Assert.Equal(buffer.Length, position);
        }

        [Fact]
        public void ReadToHeaderEnd_NoHeaderEnd()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49 }; // HTTP/1.1
            var position = 0;
            var result = HttpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.False(result);
        }

        [Fact]
        public void ReadToBreak_FindsLineBreak()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49, 13, 10, 13, 10 }; // HTTP/1.1\r\n\r\n
            var position = 0;
            var result = HttpCommon.ReadToBreak(buffer, ref position);

            Assert.True(result);
        }

        [Fact]
        public void ReadToBreak_NoLineBreak()
        {
            var buffer = new byte[] { 72, 84, 84, 80, 47, 49, 46, 49 }; // HTTP/1.1
            var position = 0;
            var result = HttpCommon.ReadToBreak(buffer, ref position);

            Assert.False(result);
        }

        [Fact]
        public void ReadToHeaderEnd_ShortRead_PositionNotNegative()
        {
            var buffer = new byte[] { 72 }; // H
            var position = 0;

            var result = HttpCommon.TryReadToHeaderEnd(buffer, ref position);

            Assert.False(result);
            Assert.Equal(0, position);
        }

        [Fact]
        public void ReadHeader_LowercaseHeaderNamesAndMediaTypeWithoutCharset()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("POST / HTTP/1.1\r\ncontent-type: application/json\r\nprovider-type: Provider\r\ntransfer-encoding: Chunked\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.Equal(ContentType.Json, header.ContentType);
            Assert.Equal("Provider", header.ProviderType);
            Assert.True(header.Chuncked);
        }

        [Theory]
        [InlineData("chunked")]
        [InlineData("CHUNKED ")]
        [InlineData("chunked,")]
        [InlineData(", chunked")]
        public void ReadHeader_TransferEncodingChunked(string transferEncoding)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"POST / HTTP/1.1\r\nTransfer-Encoding: {transferEncoding}\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.True(header.Chuncked);
        }

        [Theory]
        [InlineData("Transfer-Encoding: gzip, chunked")] //gzip isn't decoded
        [InlineData("Transfer-Encoding: chunked, gzip")] //the body doesn't end with the chunked framing
        [InlineData("Transfer-Encoding: gzip")]
        [InlineData("Transfer-Encoding: ")]
        [InlineData("Transfer-Encoding: chunked, chunked")]
        [InlineData("Transfer-Encoding: chunked\r\nTransfer-Encoding: chunked")] //the lines add up to one list
        [InlineData("Transfer-Encoding: gzip\r\nTransfer-Encoding: chunked")]
        public void ReadHeader_UnsupportedTransferEncoding_Throws(string transferEncodingLines)
        {
            //a body that can't be read would leave the connection out of step with the next request
            var bytes = System.Text.Encoding.UTF8.GetBytes($"POST / HTTP/1.1\r\n{transferEncodingLines}\r\n\r\n");

            _ = Assert.Throws<CqrsNetworkException>(() => HttpCommon.ReadHeader(bytes, bytes.Length));
        }

        [Fact]
        public void ReadHeader_KnownHeadersWithoutBuildingAllHeaders()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("HTTP/1.1 500 Server Error\r\nContent-Type: application/json\r\nContent-Length: 12\r\nProvider-Type: Provider\r\nOrigin: example.com\r\nX-Other: value\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.True(header.IsError);
            Assert.Equal(ContentType.Json, header.ContentType);
            Assert.Equal(12, header.ContentLength);
            Assert.Equal("Provider", header.ProviderType);
            Assert.Equal("example.com", header.Origin);
            Assert.Null(header.Headers);
            Assert.Null(header.Declarations);
        }

        [Fact]
        public void ReadHeader_ParseAllHeaders_BuildsHeadersAndDeclarations()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("OPTIONS / HTTP/1.1\r\nOrigin: example.com\r\nX-Other: one\r\nx-other: two\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length, true);

            Assert.True(header.Preflight);
            Assert.Equal("OPTIONS / HTTP/1.1", header.Declarations);
            Assert.NotNull(header.Headers);
            Assert.Equal(["example.com"], header.Headers["Origin"]);
            Assert.Equal(["one", "two"], header.Headers["X-Other"]);
        }

        [Fact]
        public void ReadHeader_NoHeaders_Throws()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");

            _ = Assert.ThrowsAny<Exception>(() => HttpCommon.ReadHeader(bytes, bytes.Length));
        }

        [Theory]
        [InlineData("application/octet-stream", ContentType.Bytes)]
        [InlineData("application/json; charset=utf-8", ContentType.Json)]
        [InlineData("Application/JSON;charset=UTF-8", ContentType.Json)]
        [InlineData("application/jsonnameless; charset=utf-8", ContentType.JsonNameless)]
        public void ReadHeader_ContentTypeMatchesMediaType(string contentType, ContentType expected)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"POST / HTTP/1.1\r\nContent-Type: {contentType}\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.Equal(expected, header.ContentType);
        }

        [Fact]
        public void BufferOkResponseHeader_WithoutBody_HasZeroContentLength()
        {
            var buffer = new byte[HttpCommon.BufferLength];

            var length = HttpCommon.BufferOkResponseHeader(buffer, null, null, ContentType.Bytes, null, false);

            var response = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
            Assert.Contains("Content-Length: 0\r\n", response);
            Assert.DoesNotContain("Transfer-Encoding", response);
            Assert.EndsWith("\r\n\r\n", response);
        }

        [Fact]
        public void BufferPreflightResponse_WithOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var origin = "https://example.com";

            var length = HttpCommon.BufferPreflightResponse(bufferMemory, origin);

            var expected = "HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: https://example.com\r\nVary: Origin\r\nAccess-Control-Allow-Methods: *\r\nAccess-Control-Allow-Headers: *\r\nContent-Length: 0\r\n\r\n";
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(buffer, 0, length));
        }

        [Fact]
        public void BufferPreflightResponse_OriginNotAllowed_HasNoAllowOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferPreflightResponse(bufferMemory, "https://evil.example.com", false);

            var expected = "HTTP/1.1 200 OK\r\nVary: Origin\r\nAccess-Control-Allow-Methods: *\r\nAccess-Control-Allow-Headers: *\r\nContent-Length: 0\r\n\r\n";
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(buffer, 0, length));
        }

        [Fact]
        public void BufferUnauthorizedResponseHeader_IsEmpty401()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferUnauthorizedResponseHeader(bufferMemory);

            var expected = "HTTP/1.1 401 Unauthorized\r\nVary: Origin\r\nContent-Length: 0\r\n\r\n";
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(buffer, 0, length));

            var header = HttpCommon.ReadHeader(bufferMemory[..length], length);
            Assert.True(header.IsError);
            Assert.Equal("401 Unauthorized", header.ErrorStatus);
        }

        [Fact]
        public void BufferPreflightResponse_WithoutOrigin()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();

            var length = HttpCommon.BufferPreflightResponse(bufferMemory, null);

            var expected = "HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: *\r\nAccess-Control-Allow-Headers: *\r\nContent-Length: 0\r\n\r\n";
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(buffer, 0, length));
        }

        [Fact]
        public void BufferPostRequestHeader_WithAllParameters()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var providerType = "ITestQueryHandler";
            var contentType = ContentType.Bytes;
            var authHeaders = new Dictionary<string, List<string?>>
            {
                { "Authorization", new List<string?> { "Bearer token123" } }
            };

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, providerType, contentType, authHeaders);

            Assert.True(length > 0);
            Assert.True(length < HttpCommon.BufferLength);
        }

        [Fact]
        public void BufferPostRequestHeader_WritesRequestHeadersOnly()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var url = new Uri("http://localhost:9001/api");
            var authHeaders = new Dictionary<string, List<string?>> { { "Authorization", new List<string?> { "Bearer token123" } } };

            var length = HttpCommon.BufferPostRequestHeader(buffer, url, "TestProvider", ContentType.Bytes, authHeaders);

            //response-only Access-Control-Allow headers are not sent
            var expected = "POST /api HTTP/1.1\r\nProvider-Type: TestProvider\r\nContent-Type: application/octet-stream\r\nAuthorization: Bearer token123\r\nTransfer-Encoding: chunked\r\nHost: localhost:9001\r\nOrigin: localhost\r\n\r\n";
            Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(buffer, 0, length));
        }

        [Fact]
        public void BufferPostRequestHeader_WithoutAuthHeaders()
        {
            var buffer = new byte[HttpCommon.BufferLength];
            var bufferMemory = buffer.AsMemory();
            var url = new Uri("http://localhost:9001/api");
            var providerType = "ITestQueryHandler";
            var contentType = ContentType.Json;

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, providerType, contentType, null);

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

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, null, contentType, null);

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

        //the reason phrase differs by server, Kestrel and proxies send their own, any 4xx or 5xx status is an error
        [Theory]
        [InlineData("HTTP/1.1 500 Server Error", "500 Server Error")]
        [InlineData("HTTP/1.1 500 Internal Server Error", "500 Internal Server Error")]
        [InlineData("HTTP/1.1 502 Bad Gateway", "502 Bad Gateway")]
        [InlineData("HTTP/1.1 400 Bad Request", "400 Bad Request")]
        [InlineData("HTTP/1.1 401 Unauthorized", "401 Unauthorized")]
        [InlineData("HTTP/1.1 404 Not Found", "404 Not Found")]
        public void ReadHeader_ErrorStatus_IsError(string statusLine, string status)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{statusLine}\r\nContent-Length: 0\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.True(header.IsError);
            Assert.Equal(status, header.ErrorStatus);
        }

        [Theory]
        [InlineData("HTTP/1.1 200 OK")]
        [InlineData("HTTP/1.1 204 No Content")]
        [InlineData("HTTP/1.1 304 Not Modified")]
        [InlineData("POST /500 HTTP/1.1")] //a request line is never an error, even with a path that looks like a status
        public void ReadHeader_NonErrorStatus_IsNotError(string firstLine)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{firstLine}\r\nContent-Length: 0\r\n\r\n");

            var header = HttpCommon.ReadHeader(bytes, bytes.Length);

            Assert.False(header.IsError);
            Assert.Null(header.ErrorStatus);
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

            var length = HttpCommon.BufferPostRequestHeader(bufferMemory, url, "TestProvider", ContentType.Json, authHeaders);

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
