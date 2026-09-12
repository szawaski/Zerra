// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.Reflection;
using Zerra.Encryption;
using Zerra.Serialization;
using HttpRequestHeader = Zerra.CQRS.Network.HttpRequestHeader;

namespace Zerra.Test.CQRS.Network
{
    public class HttpCqrsServerTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();
        private static readonly IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

        public HttpCqrsServerTests()
        {
            //the server looks up command/event info that the bus normally generates when handlers are registered
            _ = BusCommandOrEventInfo.GetByType(typeof(ITestMessageHandler), null);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_ReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            Type? receivedType = null;
            string? receivedMethod = null;
            int? receivedArgument = null;
            string? receivedSource = null;
            using var server = StartQueryServer(out var port, enc, (interfaceType, methodName, arguments, requestSource, _, _) =>
            {
                receivedType = interfaceType;
                receivedMethod = methodName;
                receivedArgument = serializer.Deserialize<int>(arguments[0]);
                receivedSource = requestSource;
                return Task.FromResult(new RemoteQueryCallResponse(receivedArgument * 2));
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), enc);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(serializer.ContentType, header.ContentType);
            Assert.Equal(42, await connection.ReadBodyAsync<int>(header, enc));

            Assert.Equal(typeof(ITestQueryHandler), receivedType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), receivedMethod);
            Assert.Equal(21, receivedArgument);
            Assert.Equal(source, receivedSource);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_ReturnsStream(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartQueryServer(out var port, enc, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(new MemoryStream([1, 2, 3, 4, 5]))));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream)), enc);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal([1, 2, 3, 4, 5], await connection.ReadBodyBytesAsync(header, enc));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsStream_DisposesStream()
        {
            var resultStream = new DisposeSignalStream([1, 2, 3]);
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(resultStream)));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream)), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.Equal([1, 2, 3], await connection.ReadBodyBytesAsync(header, null));
            await resultStream.Disposed.Task; //the handler's stream is released once it's sent, such as a file handle
        }

        [Fact(Timeout = timeout)]
        public async Task Request_WithoutBodyLength_IsNotLeftWaiting()
        {
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)));

            //without Content-Length or chunked the body is empty, reading it as chunked would wait for data that never comes
            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendRawAsync($"POST / HTTP/1.1\r\nContent-Type: {HttpCommon.ContentTypeBytes}\r\nHost: 127.0.0.1\r\n\r\n");

            Assert.Equal("", await connection.ReadRawAsync()); //the empty request is rejected
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_HandlerThrows_RespondsWithError(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartQueryServer(out var port, enc, (_, _, _, _, _, _) =>
                Task.FromException<RemoteQueryCallResponse>(new InvalidOperationException("query failed")));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), enc);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, enc);
            Assert.Equal("query failed", exception.Message);
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_WithClaims_SetsThreadPrincipalForHandler()
        {
            IPrincipal? principal = null;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
            {
                principal = Thread.CurrentPrincipal;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            var request = QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21);
            request.Claims = [["name", "tester"]];

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(request, null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            var claimsPrincipal = Assert.IsType<ClaimsPrincipal>(principal);
            Assert.Equal("tester", claimsPrincipal.FindFirst("name")?.Value);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_UnregisteredInterface_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(IOtherQueryHandler), nameof(IOtherQueryHandler.GetOther)), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, null);
            Assert.StartsWith("Unhandled Provider Type", exception.Message);
            Assert.False(handlerInvoked);

            //the request was fully read so the connection is still usable
            handlerInvoked = false;
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null);
            header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(1, await connection.ReadBodyAsync<int>(header, null));
            Assert.True(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ContentTypeMismatch_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            try
            {
                await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null, ContentType.Json);
            }
            catch (IOException) { } //server may reset the connection before the body is sent

            var header = await connection.ReadHeaderAsync();
            Assert.True(header is null || header.IsError);
            Assert.False(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_WithAuthorizer_PassesRequestHeadersToAuthorizer()
        {
            var authorizer = new TestAuthorizer();
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)), authorizer);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null,
                authHeaders: new() { ["Authorization"] = ["Bearer token"] });

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.NotNull(authorizer.ReceivedHeaders);
            Assert.Equal(["Bearer token"], authorizer.ReceivedHeaders["Authorization"]);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_AuthorizerRejects_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            var authorizer = new TestAuthorizer { Reject = true };
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            }, authorizer);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, null);
            Assert.Equal(nameof(UnauthorizedAccessException), exception.ErrorType);
            Assert.False(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_OriginAllowed_InvokesHandler()
        {
            //the request Origin header is the host of the service url
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)), allowOrigins: ["127.0.0.1"]);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
        }

        [Theory(Timeout = timeout)]
        [InlineData("app.example.com", "https://App.Example.com")] //an allowed host matches a browser's full origin
        [InlineData("HTTPS://APP.example.com", "https://app.example.com")] //case doesn't matter
        [InlineData("127.0.0.1", null)] //the service host that HttpCqrsClient sends
        public async Task Query_OriginMatchesAllowedOriginOrHost(string allowOrigin, string? origin)
        {
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)), allowOrigins: [allowOrigin]);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null, origin: origin);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_OriginNotAllowed_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            }, allowOrigins: ["allowed.example.com"]);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null);

            //an empty 401 like Kestrel so the client reports the status
            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.True(header.IsError);
            Assert.Equal("401 Unauthorized", header.ErrorStatus);
            Assert.Equal(0, header.ContentLength);
            Assert.False(handlerInvoked);

            //the body was read so the connection is still usable
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null, origin: "https://allowed.example.com");
            header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(1, await connection.ReadBodyAsync<int>(header, null));
            Assert.True(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Preflight_WithAllowOrigins_EchoesAllowedOriginOnly()
        {
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)), allowOrigins: ["allowed.example.com"]);

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendRawAsync("OPTIONS / HTTP/1.1\r\nHost: 127.0.0.1\r\nOrigin: https://allowed.example.com\r\n\r\n");
            var allowed = await connection.ReadRawAsync();
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", allowed);
            Assert.Contains("Access-Control-Allow-Origin: https://allowed.example.com\r\n", allowed);
            Assert.Contains("Vary: Origin\r\n", allowed);

            await connection.SendRawAsync("OPTIONS / HTTP/1.1\r\nHost: 127.0.0.1\r\nOrigin: https://evil.example.com\r\n\r\n");
            var disallowed = await connection.ReadRawAsync();
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", disallowed);
            Assert.DoesNotContain("Access-Control-Allow-Origin", disallowed);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_MultipleRequestsOnSameConnection()
        {
            using var server = StartQueryServer(out var port, null, (_, _, arguments, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(serializer.Deserialize<int>(arguments[0]) * 2)));

            await using var connection = await TestConnection.ConnectAsync(port);
            for (var i = 1; i <= 3; i++)
            {
                await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), i), null);
                var header = await connection.ReadHeaderAsync();
                Assert.NotNull(header);
                Assert.Equal(i * 2, await connection.ReadBodyAsync<int>(header, null));
            }
        }

        [Fact(Timeout = timeout)]
        public async Task Preflight_RespondsWithCorsHeaders()
        {
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(1)));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendRawAsync("OPTIONS / HTTP/1.1\r\nHost: 127.0.0.1\r\nOrigin: http://example.com\r\n\r\n");

            var response = await connection.ReadRawAsync();
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", response);
            Assert.Contains("Access-Control-Allow-Origin: http://example.com\r\n", response);
            Assert.EndsWith("\r\n\r\n", response);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_InvokesHandlerAndResponds()
        {
            var received = new TaskCompletionSource<(int Value, string Source)>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, command: (command, commandSource, cancellationToken) =>
            {
                _ = received.TrySetResult((((TestCommand)command).Value, commandSource));
                return Task.CompletedTask;
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, false, false), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(0, header.ContentLength); //no body so it must not claim a chunked one
            Assert.False(header.Chuncked);
            Assert.Equal((5, source), await received.Task);
        }

        [Fact(Timeout = timeout)]
        public async Task CommandAwait_RespondsAfterHandlerCompletes()
        {
            var handled = false;
            using var server = StartMessageServer(out var port, null, commandAwait: async (_, _, cancellationToken) =>
            {
                await Task.Delay(50, cancellationToken);
                handled = true;
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, true, false), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.True(handled);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CommandWithResult_ReturnsResult(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartMessageServer(out var port, enc, commandWithResult: (command, _, _) =>
                Task.FromResult<object?>(((TestCommandWithResult)command).Value * 2));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(MessageRequest(new TestCommandWithResult { Value = 21 }, true, true), enc);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(42, await connection.ReadBodyAsync<int>(header, enc));
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CommandAwait_HandlerThrows_RespondsWithError(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartMessageServer(out var port, enc, commandAwait: (_, _, _) =>
                Task.FromException(new InvalidOperationException("command failed")));

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, true, false), enc);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, enc);
            Assert.Equal("command failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Event_InvokesHandlerAndResponds()
        {
            var received = new TaskCompletionSource<(int Value, string Source)>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, @event: (@event, eventSource) =>
            {
                _ = received.TrySetResult((((TestEvent)@event).Value, eventSource));
                return Task.CompletedTask;
            });

            await using var connection = await TestConnection.ConnectAsync(port);
            await connection.SendAsync(MessageRequest(new TestEvent { Value = 7 }, false, false), null);

            var header = await connection.ReadHeaderAsync();
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(0, header.ContentLength);
            Assert.Equal((7, source), await received.Task);
        }

        [Fact]
        public void ServiceUrl_ReturnsConstructorUrl()
        {
            using var server = new HttpCqrsServer("127.0.0.1:9999", serializer, null, null, null);

            Assert.Equal("127.0.0.1:9999", ((IQueryServer)server).ServiceUrl);
            Assert.Equal("127.0.0.1:9999", ((ICommandConsumer)server).MessageHost);
            Assert.Equal("127.0.0.1:9999", ((IEventConsumer)server).MessageHost);
        }

        [Fact]
        public void Open_AfterDispose_Throws()
        {
            var server = new HttpCqrsServer($"127.0.0.1:{GetFreePort()}", serializer, null, null, null);
            server.Dispose();

            _ = Assert.Throws<ObjectDisposedException>(() => ((IQueryServer)server).Open());
        }

        [Fact(Timeout = timeout)]
        public async Task NotSetup_ClosesConnection()
        {
            //opened without registering anything so a connection can't be handled, it must be closed instead of left open
            var port = GetFreePort();
            using var server = new HttpCqrsServer($"127.0.0.1:{port}", serializer, null, null, null);
            ((IQueryServer)server).Open();

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), TestContext.Current.CancellationToken);
            Assert.Equal(0, await client.ReceiveAsync(new byte[1], SocketFlags.None, TestContext.Current.CancellationToken));
        }

        private static HttpCqrsServer StartQueryServer(out int port, IEncryptor? encryptor, QueryHandlerDelegate handler, ICqrsAuthorizer? authorizer = null, string[]? allowOrigins = null)
        {
            port = GetFreePort();
            var server = new HttpCqrsServer($"127.0.0.1:{port}", serializer, encryptor, authorizer, allowOrigins);
            IQueryServer queryServer = server;
            queryServer.Setup(new CommandCounter(), handler);
            queryServer.RegisterInterfaceType(10, typeof(ITestQueryHandler));
            queryServer.Open();
            return server;
        }

        private static HttpCqrsServer StartMessageServer(out int port, IEncryptor? encryptor,
            HandleRemoteCommandDispatch? command = null,
            HandleRemoteCommandDispatch? commandAwait = null,
            HandleRemoteCommandWithResultDispatch? commandWithResult = null,
            HandleRemoteEventDispatch? @event = null)
        {
            port = GetFreePort();
            var server = new HttpCqrsServer($"127.0.0.1:{port}", serializer, encryptor, null, null);
            ICommandConsumer commandConsumer = server;
            commandConsumer.Setup(new CommandCounter(),
                command ?? ((_, _, _) => Task.CompletedTask),
                commandAwait ?? ((_, _, _) => Task.CompletedTask),
                commandWithResult ?? ((_, _, _) => Task.FromResult<object?>(null)));
            commandConsumer.RegisterCommandType(10, "test", typeof(TestCommand));
            commandConsumer.RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            IEventConsumer eventConsumer = server;
            eventConsumer.Setup(@event ?? ((_, _) => Task.CompletedTask));
            eventConsumer.RegisterEventType(10, "test", typeof(TestEvent));
            commandConsumer.Open();
            return server;
        }

        private static CqrsRequestData QueryRequest(Type interfaceType, string methodName, params object[] arguments) => new()
        {
            ProviderType = interfaceType.AssemblyQualifiedName,
            ProviderMethod = methodName,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = source
        };

        private static CqrsRequestData MessageRequest(object message, bool messageAwait, bool messageResult) => new()
        {
            MessageType = message.GetType().AssemblyQualifiedName,
            MessageData = serializer.SerializeBytes(message, message.GetType()),
            MessageAwait = messageAwait,
            MessageResult = messageResult,
            Source = source
        };

        private static int GetFreePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        private sealed class TestAuthorizer : ICqrsAuthorizer
        {
            public bool Reject { get; set; }
            public Dictionary<string, List<string?>>? ReceivedHeaders { get; private set; }

            public void Authorize(Dictionary<string, List<string?>> headers)
            {
                ReceivedHeaders = headers;
                if (Reject)
                    throw new UnauthorizedAccessException();
            }

            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        //Speaks the client side of the HTTP protocol so the server can be tested without HttpCqrsClient
        private sealed class DisposeSignalStream(byte[] data) : MemoryStream(data)
        {
            public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected override void Dispose(bool disposing)
            {
                _ = Disposed.TrySetResult();
                base.Dispose(disposing);
            }
        }

        private sealed class TestConnection : IAsyncDisposable
        {
            private readonly NetworkStream stream;
            private readonly Uri serviceUri;

            private TestConnection(Socket socket, int port)
            {
                this.stream = new NetworkStream(socket, true);
                this.serviceUri = new Uri($"http://127.0.0.1:{port}");
            }

            public static async Task<TestConnection> ConnectAsync(int port)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(IPAddress.Loopback, port);
                return new TestConnection(socket, port);
            }

            public async Task SendAsync(CqrsRequestData data, IEncryptor? encryptor, ContentType? contentType = null, Dictionary<string, List<string?>>? authHeaders = null, string? origin = null)
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var headerLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.ProviderType ?? data.MessageType, contentType ?? serializer.ContentType, authHeaders);
                if (origin is not null)
                {
                    //a browser sends its own origin instead of the service host
                    var header = Encoding.UTF8.GetString(buffer, 0, headerLength).Replace($"Origin: {serviceUri.Host}\r\n", $"Origin: {origin}\r\n");
                    buffer = Encoding.UTF8.GetBytes(header);
                    headerLength = buffer.Length;
                }
                await stream.WriteAsync(buffer.AsMemory(0, headerLength));

                var body = new HttpProtocolBodyStream(null, stream, null, true, true);
                if (encryptor is not null)
                {
                    var cryptoStream = encryptor.Encrypt(body, true);
                    await serializer.SerializeAsync(cryptoStream, data, default);
                    await cryptoStream.FlushFinalBlockAsync();
                    await cryptoStream.DisposeAsync();
                }
                else
                {
                    await serializer.SerializeAsync(body, data, default);
                    await body.FlushAsync();
                    await body.DisposeAsync();
                }
            }

            public async Task SendRawAsync(string request)
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(request));
            }

            public async Task<string> ReadRawAsync()
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var read = await stream.ReadAsync(buffer);
                return Encoding.UTF8.GetString(buffer, 0, read);
            }

            //returns null when the server closes the connection without responding
            public async Task<HttpRequestHeader?> ReadHeaderAsync()
            {
                var buffer = new byte[HttpCommon.BufferLength];
                var position = 0;
                var length = 0;
                try
                {
                    do
                    {
                        if (position < 0)
                            position = 0;
                        var read = await stream.ReadAsync(buffer.AsMemory(length));
                        if (read == 0)
                            return null;
                        length += read;
                    }
                    while (!HttpCommon.TryReadToHeaderEnd(buffer.AsSpan(0, length), ref position));
                }
                catch (IOException)
                {
                    return null;
                }
                return HttpCommon.ReadHeader(buffer.AsMemory(0, length), position);
            }

            public async Task<T?> ReadBodyAsync<T>(HttpRequestHeader header, IEncryptor? encryptor)
            {
                await using var body = OpenBody(header, encryptor);
                return await serializer.DeserializeAsync<T>(body, default);
            }

            public async Task<byte[]> ReadBodyBytesAsync(HttpRequestHeader header, IEncryptor? encryptor)
            {
                await using var body = OpenBody(header, encryptor);
                using var ms = new MemoryStream();
                await body.CopyToAsync(ms);
                return ms.ToArray();
            }

            public async Task<RemoteServiceException> ReadErrorAsync(HttpRequestHeader header, IEncryptor? encryptor)
            {
                await using var body = OpenBody(header, encryptor);
                return await ExceptionSerializer.DeserializeAsync("test", serializer, body, default);
            }

            private Stream OpenBody(HttpRequestHeader header, IEncryptor? encryptor)
            {
                Stream body = new HttpProtocolBodyStream(header.ContentLength, stream, header.BodyStartBuffer, false, true);
                if (encryptor is not null)
                    body = encryptor.Decrypt(body, false);
                return body;
            }

            public ValueTask DisposeAsync() => stream.DisposeAsync();
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
        }

        public interface IOtherQueryHandler : IQueryHandler
        {
            int GetOther();
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

        public interface ITestMessageHandler :
            ICommandHandler<TestCommand>,
            ICommandHandler<TestCommandWithResult, int>,
            IEventHandler<TestEvent>
        { }
    }
}
