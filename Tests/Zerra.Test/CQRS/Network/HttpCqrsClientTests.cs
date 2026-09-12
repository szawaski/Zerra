// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using HttpRequestHeader = Zerra.CQRS.Network.HttpRequestHeader;

namespace Zerra.Test.CQRS.Network
{
    public class HttpCqrsClientTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();
        private static readonly IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Call_SendsQueryAndReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, enc);

            var result = await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            Assert.Equal(42, result);
            var request = Assert.Single(server.Requests);
            AssertQueryRequest(request, nameof(ITestQueryHandler.GetThings), 21);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_SendsQueryAndReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, enc);

            var result = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            Assert.Equal(42, result);
            var request = Assert.Single(server.Requests);
            AssertQueryRequest(request, nameof(ITestQueryHandler.GetThings), 21);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Call_StreamResult_ReturnsStream(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteStreamAsync([1, 2, 3, 4, 5]));
            using var client = CreateClient(server, enc);

            var bytes = await Task.Run(() =>
            {
                using var stream = ((IQueryClient)client).Call<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            });

            Assert.Equal([1, 2, 3, 4, 5], bytes);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_StreamResult_ReturnsStream(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteStreamAsync([1, 2, 3, 4, 5]));
            using var client = CreateClient(server, enc);

            await using var stream = await ((IQueryClient)client).CallTaskGeneric<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source, default);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            Assert.Equal([1, 2, 3, 4, 5], ms.ToArray());
        }

        [Fact(Timeout = timeout)]
        public async Task Call_ErrorResponse_ThrowsRemoteServiceException()
        {
            using var server = new FakeServer(null, request => request.WriteErrorAsync("query failed"));
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() => Task.Run(() =>
                ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source)));

            Assert.Equal("query failed", exception.Message);
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_ErrorResponse_ThrowsRemoteServiceException(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteErrorAsync("query failed"));
            using var client = CreateClient(server, enc);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Equal("query failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_WithAuthorizer_SendsSyncAuthorizationHeaders()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, null, new TestAuthorizer());

            _ = await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            var request = Assert.Single(server.Requests);
            Assert.Equal(["Bearer sync"], request.Header.Headers!["Authorization"]);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_WithAuthorizer_SendsAsyncAuthorizationHeaders()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, null, new TestAuthorizer());

            _ = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            var request = Assert.Single(server.Requests);
            Assert.Equal(["Bearer async"], request.Header.Headers!["Authorization"]);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAsync_WithAuthorizer_SendsAsyncAuthorizationHeaders()
        {
            using var server = new FakeServer(null, request => request.WriteEmptyAsync());
            using var client = CreateClient(server, null, new TestAuthorizer());

            await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, default);

            var request = Assert.Single(server.Requests);
            Assert.Equal(["Bearer async"], request.Header.Headers!["Authorization"]);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_SendsThreadPrincipalClaims()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, null);

            var originalPrincipal = Thread.CurrentPrincipal;
            Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "tester")], "test"));
            try
            {
                _ = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);
            }
            finally
            {
                Thread.CurrentPrincipal = originalPrincipal;
            }

            var request = Assert.Single(server.Requests);
            var claim = Assert.Single(request.Data.Claims!);
            Assert.Equal(["name", "tester"], claim);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_ServerClosesConnection_ThrowsCallFailed()
        {
            using var server = new FakeServer(null, request => request.CloseAsync());
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.StartsWith($"Call failed for {nameof(ITestQueryHandler)}.{nameof(ITestQueryHandler.GetThings)}", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_Canceled_ThrowsOperationCanceled()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, null);

            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, new CancellationToken(true)));
            Assert.Empty(server.Requests);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_ReusesConnection()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, null);

            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Equal(3, server.Requests.Count);
            Assert.Equal(1, server.ConnectionCount);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_ResponseHeaderSplitAcrossReads_ReturnsModel()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42, splitHeader: true));
            using var client = CreateClient(server, null);

            var result = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            Assert.Equal(42, result);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_ResponseHeaderSplitAcrossReads_ReturnsModel()
        {
            using var server = new FakeServer(null, request => request.WriteModelAsync(42, splitHeader: true));
            using var client = CreateClient(server, null);

            var result = await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            Assert.Equal(42, result);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAsync_Command_SendsMessageWithoutAwait()
        {
            using var server = new FakeServer(null, request => request.WriteEmptyAsync());
            using var client = CreateClient(server, null);

            await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, default);

            var request = Assert.Single(server.Requests);
            AssertMessageRequest(request, typeof(TestCommand), false, false);
            Assert.Equal(5, serializer.Deserialize<TestCommand>(request.Data.MessageData)!.Value);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAwaitAsync_Command_SendsMessageWithAwait()
        {
            using var server = new FakeServer(null, request => request.WriteEmptyAsync());
            using var client = CreateClient(server, null);

            await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, default);

            var request = Assert.Single(server.Requests);
            AssertMessageRequest(request, typeof(TestCommand), true, false);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DispatchAwaitAsync_CommandWithResult_ReturnsResult(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteModelAsync(42));
            using var client = CreateClient(server, enc);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, default);

            Assert.Equal(42, result);
            var request = Assert.Single(server.Requests);
            AssertMessageRequest(request, typeof(TestCommandWithResult), true, true);
            Assert.Equal(21, serializer.Deserialize<TestCommandWithResult>(request.Data.MessageData)!.Value);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DispatchAwaitAsync_ErrorResponse_ThrowsRemoteServiceException(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteErrorAsync("command failed"));
            using var client = CreateClient(server, enc);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, default));

            Assert.Equal("command failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAsync_Event_SendsMessage()
        {
            using var server = new FakeServer(null, request => request.WriteEmptyAsync());
            using var client = CreateClient(server, null);

            await ((IEventProducer)client).DispatchAsync(new TestEvent { Value = 7 }, source, default);

            var request = Assert.Single(server.Requests);
            AssertMessageRequest(request, typeof(TestEvent), false, false);
            Assert.Equal(7, serializer.Deserialize<TestEvent>(request.Data.MessageData)!.Value);
        }

        //Kestrel and proxies frame a body by its length where HttpCqrsServer chunks it
        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_LengthFramedResponse_ReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, request => request.WriteLengthFramedAsync("HTTP/1.1 200 OK", body => serializer.SerializeAsync(body, 42, default)));
            using var client = CreateClient(server, enc);

            var result = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            Assert.Equal(42, result);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_LengthFramedResponse_ReturnsModel()
        {
            using var server = new FakeServer(null, request => request.WriteLengthFramedAsync("HTTP/1.1 200 OK", body => serializer.SerializeAsync(body, 42, default)));
            using var client = CreateClient(server, null);

            var result = await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            Assert.Equal(42, result);
        }

        //the reason phrase differs by server, the status code is what marks an error
        [Theory(Timeout = timeout)]
        [InlineData("HTTP/1.1 500 Internal Server Error")]
        [InlineData("HTTP/1.1 503 Service Unavailable")]
        [InlineData("HTTP/1.1 400 Bad Request")]
        public async Task CallTaskGeneric_ErrorWithOtherStatusLine_ThrowsRemoteServiceException(string statusLine)
        {
            using var server = new FakeServer(null, request => request.WriteLengthFramedAsync(statusLine, body => ExceptionSerializer.SerializeAsync(serializer, body, new InvalidOperationException("query failed"), default)));
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Equal("query failed", exception.Message);
        }

        //an error without a body, such as Kestrel's 401, can only report its status
        [Theory(Timeout = timeout)]
        [InlineData("HTTP/1.1 401 Unauthorized", "401 Unauthorized")]
        [InlineData("HTTP/1.1 400 Bad Request", "400 Bad Request")]
        [InlineData("HTTP/1.1 404 Not Found", "404 Not Found")]
        public async Task CallTaskGeneric_EmptyErrorResponse_ThrowsWithStatus(string statusLine, string status)
        {
            using var server = new FakeServer(null, request => request.WriteLengthFramedAsync(statusLine));
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Contains(status, exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_EmptyErrorResponse_ThrowsWithStatus()
        {
            using var server = new FakeServer(null, request => request.WriteLengthFramedAsync("HTTP/1.1 401 Unauthorized"));
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() => Task.Run(() =>
                ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source)));

            Assert.Contains("401 Unauthorized", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAwaitAsync_EmptyErrorResponse_ThrowsWithStatus()
        {
            using var server = new FakeServer(null, request => request.WriteLengthFramedAsync("HTTP/1.1 401 Unauthorized"));
            using var client = CreateClient(server, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, default));

            Assert.Contains("401 Unauthorized", exception.Message);
        }

        //an empty error leaves nothing unread, so the connection goes back to the pool and the next call uses it
        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_AfterEmptyErrorResponse_ReusesConnection()
        {
            var responses = 0;
            using var server = new FakeServer(null, request => Interlocked.Increment(ref responses) == 1 ? request.WriteLengthFramedAsync("HTTP/1.1 401 Unauthorized") : request.WriteModelAsync(42));
            using var client = CreateClient(server, null);

            _ = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));
            var result = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            Assert.Equal(42, result);
            Assert.Equal(1, server.ConnectionCount);
        }

        [Fact]
        public void Call_UnregisteredInterface_Throws()
        {
            using var client = new HttpCqrsClient("http://127.0.0.1:9999", serializer, null, null, null);

            var exception = Assert.Throws<Exception>(() =>
                ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            Assert.Contains("is not registered", exception.Message);
        }

        [Fact]
        public void DispatchAsync_UnregisteredCommand_Throws()
        {
            using var client = new HttpCqrsClient("http://127.0.0.1:9999", serializer, null, null, null);

            var exception = Assert.Throws<Exception>(() =>
            {
                _ = ((ICommandProducer)client).DispatchAsync(new TestCommand(), source, default);
            });

            Assert.Contains("is not registered", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_MissingUrl_Throws(string? url)
        {
            _ = Assert.Throws<ArgumentNullException>(() => new HttpCqrsClient(url!, serializer, null, null, null));
        }

        [Fact]
        public void ServiceUrl_ReturnsConstructorUrl()
        {
            using var client = new HttpCqrsClient("http://127.0.0.1:9999", serializer, null, null, null);

            Assert.Equal("http://127.0.0.1:9999", ((IQueryClient)client).ServiceUrl);
            Assert.Equal("http://127.0.0.1:9999", ((ICommandProducer)client).MessageHost);
            Assert.Equal("http://127.0.0.1:9999", ((IEventProducer)client).MessageHost);
        }

        private static HttpCqrsClient CreateClient(FakeServer server, IEncryptor? encryptor, ICqrsAuthorizer? authorizer = null)
        {
            var client = new HttpCqrsClient(server.Url, serializer, encryptor, authorizer, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            ((IEventProducer)client).RegisterEventType(10, "test", typeof(TestEvent));
            return client;
        }

        private static void AssertQueryRequest(FakeRequest request, string methodName, int argument)
        {
            var providerType = typeof(ITestQueryHandler).AssemblyQualifiedName;
            Assert.StartsWith("POST ", request.Header.Declarations);
            Assert.Equal(providerType, request.Header.ProviderType);
            Assert.Equal(serializer.ContentType, request.Header.ContentType);
            Assert.Equal(providerType, request.Data.ProviderType);
            Assert.Equal(methodName, request.Data.ProviderMethod);
            var argumentBytes = Assert.Single(request.Data.ProviderArguments!);
            Assert.Equal(argument, serializer.Deserialize<int>(argumentBytes));
            Assert.Equal(source, request.Data.Source);
        }

        private static void AssertMessageRequest(FakeRequest request, Type messageType, bool messageAwait, bool messageResult)
        {
            Assert.StartsWith("POST ", request.Header.Declarations);
            Assert.Equal(messageType.AssemblyQualifiedName, request.Header.ProviderType);
            Assert.Equal(serializer.ContentType, request.Header.ContentType);
            Assert.Equal(messageType.AssemblyQualifiedName, request.Data.MessageType);
            Assert.Equal(messageAwait, request.Data.MessageAwait);
            Assert.Equal(messageResult, request.Data.MessageResult);
            Assert.Equal(source, request.Data.Source);
        }

        private sealed class TestAuthorizer : ICqrsAuthorizer
        {
            public void Authorize(Dictionary<string, List<string?>> headers) => throw new NotSupportedException();

            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new Dictionary<string, List<string?>>() { ["Authorization"] = ["Bearer async"] });

            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default)
                => new() { ["Authorization"] = ["Bearer sync"] };
        }

        //Speaks the server side of the HTTP protocol so the client can be tested without HttpCqrsServer
        private sealed class FakeServer : IDisposable
        {
            private readonly Socket listener;
            private readonly CancellationTokenSource canceller;
            private readonly IEncryptor? encryptor;
            private readonly Func<FakeRequest, Task> respond;
            private int connectionCount;

            public ConcurrentQueue<FakeRequest> Requests { get; }
            public int ConnectionCount => connectionCount;
            public string Url { get; }

            public FakeServer(IEncryptor? encryptor, Func<FakeRequest, Task> respond)
            {
                this.encryptor = encryptor;
                this.respond = respond;
                this.canceller = new();
                this.Requests = new();

                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen();
                Url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndPoint!).Port}";

                _ = AcceptConnections();
            }

            private async Task AcceptConnections()
            {
                try
                {
                    for (; ; )
                    {
                        var socket = await listener.AcceptAsync(canceller.Token);
                        _ = Interlocked.Increment(ref connectionCount);
                        _ = Task.Run(() => HandleConnection(socket));
                    }
                }
                catch { } //disposed
            }

            private async Task HandleConnection(Socket socket)
            {
                using var stream = new NetworkStream(socket, true);
                try
                {
                    for (; ; )
                    {
                        var header = await ReadHeaderAsync(stream);
                        if (header is null)
                            return;

                        Stream body = new HttpProtocolBodyStream(header.ContentLength, stream, header.BodyStartBuffer, false, true);
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body, false);
                        var data = await serializer.DeserializeAsync<CqrsRequestData>(body, canceller.Token);
                        await body.DisposeAsync();

                        var request = new FakeRequest(stream, encryptor, header, data!);
                        Requests.Enqueue(request);
                        await respond(request);
                    }
                }
                catch { } //connection closed
            }

            private async Task<HttpRequestHeader?> ReadHeaderAsync(Stream stream)
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var position = 0;
                var length = 0;
                do
                {
                    if (position < 0)
                        position = 0;
                    var read = await stream.ReadAsync(buffer.AsMemory(length), canceller.Token);
                    if (read == 0)
                        return null;
                    length += read;
                }
                while (!HttpCommon.TryReadToHeaderEnd(buffer.AsSpan(0, length), ref position));
                return HttpCommon.ReadHeader(buffer.AsMemory(0, length), position, true);
            }

            public void Dispose()
            {
                canceller.Cancel();
                listener.Dispose();
                canceller.Dispose();
            }
        }

        private sealed class FakeRequest
        {
            private readonly Stream stream;
            private readonly IEncryptor? encryptor;

            public HttpRequestHeader Header { get; }
            public CqrsRequestData Data { get; }

            public FakeRequest(Stream stream, IEncryptor? encryptor, HttpRequestHeader header, CqrsRequestData data)
            {
                this.stream = stream;
                this.encryptor = encryptor;
                this.Header = header;
                this.Data = data;
            }

            //a response the way Kestrel or a proxy frames it, its own status line and the body sent by length instead of chunked
            public async Task WriteLengthFramedAsync(string statusLine, Func<Stream, Task>? write = null)
            {
                var body = Array.Empty<byte>();
                if (write is not null)
                {
                    var bodyStream = new MemoryStream();
                    if (encryptor is not null)
                    {
                        var cryptoStream = encryptor.Encrypt(bodyStream, true);
                        await write(cryptoStream);
                        await cryptoStream.FlushFinalBlockAsync();
                        await cryptoStream.DisposeAsync();
                    }
                    else
                    {
                        await write(bodyStream);
                    }
                    body = bodyStream.ToArray();
                }

                var contentTypeLine = body.Length > 0 ? $"{HttpCommon.ContentTypeHeader}: {HttpCommon.ContentTypeBytes}\r\n" : null;
                var header = System.Text.Encoding.UTF8.GetBytes($"{statusLine}\r\n{contentTypeLine}{HttpCommon.ContentLengthHeader}: {body.Length}\r\n\r\n");
                await stream.WriteAsync(header);
                await stream.WriteAsync(body);
            }

            public async Task WriteModelAsync(object? model, bool splitHeader = false)
            {
                await WriteHeaderAsync(false, splitHeader);
                await WriteBodyAsync(body => serializer.SerializeAsync(body, model, default));
            }

            public async Task WriteStreamAsync(byte[] bytes)
            {
                await WriteHeaderAsync(false);
                await WriteBodyAsync(body => body.WriteAsync(bytes).AsTask());
            }

            public async Task WriteErrorAsync(string message)
            {
                await WriteHeaderAsync(true);
                await WriteBodyAsync(body => ExceptionSerializer.SerializeAsync(serializer, body, new InvalidOperationException(message), default));
            }

            public Task WriteEmptyAsync() => WriteHeaderAsync(false);

            public Task CloseAsync()
            {
                stream.Dispose();
                return Task.CompletedTask;
            }

            private async Task WriteHeaderAsync(bool isError, bool splitHeader = false)
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var headerLength = isError
                    ? HttpCommon.BufferErrorResponseHeader(buffer, Header.Origin)
                    : HttpCommon.BufferOkResponseHeader(buffer, Header.Origin, Header.ProviderType, serializer.ContentType, null);
                if (splitHeader)
                {
                    //the client reads the first part before the rest arrives
                    await stream.WriteAsync(buffer.AsMemory(0, 20));
                    await Task.Delay(100);
                    await stream.WriteAsync(buffer.AsMemory(20, headerLength - 20));
                }
                else
                {
                    await stream.WriteAsync(buffer.AsMemory(0, headerLength));
                }
            }

            private async Task WriteBodyAsync(Func<Stream, Task> write)
            {
                var body = new HttpProtocolBodyStream(null, stream, null, true, true);
                if (encryptor is not null)
                {
                    var cryptoStream = encryptor.Encrypt(body, true);
                    await write(cryptoStream);
                    await cryptoStream.FlushFinalBlockAsync();
                    await cryptoStream.DisposeAsync();
                }
                else
                {
                    await write(body);
                    await body.FlushAsync();
                    await body.DisposeAsync();
                }
            }
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
        }

        public sealed class TestCommand : ICommand
        {
            public int Value { get; set; }
        }

        public sealed class TestCommandWithResult : ICommand<int>
        {
            public int Value { get; set; }
        }

        public sealed class TestEvent : IEvent
        {
            public int Value { get; set; }
        }
    }
}
