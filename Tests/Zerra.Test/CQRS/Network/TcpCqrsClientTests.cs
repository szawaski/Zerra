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

namespace Zerra.Test.CQRS.Network
{
    public class TcpCqrsClientTests
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
        public async Task CallTaskGeneric_CanceledDuringResponse_DoesNotWaitForAbort()
        {
            using var server = new FakeServer(null, async request =>
            {
                //the header and part of a 100 byte segment, the rest never comes
                var buffer = new byte[TcpCommon.BufferLength];
                var headerLength = TcpCommon.BufferHeader(buffer, request.Header.ProviderType!, serializer.ContentType);
                await request.WriteRawAsync(buffer[..headerLength].Concat(BitConverter.GetBytes(100)).Concat(new byte[10]).ToArray());
            });
            using var client = CreateClient(server, null);
            using var cts = new CancellationTokenSource(300);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, cts.Token));
            stopwatch.Stop();

            //the server already responded so there is no abort to acknowledge, waiting for one would add the abort timeout
            Assert.True(stopwatch.ElapsedMilliseconds < 800, $"cancel took {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_CanceledDuringRequest_DoesNotWaitForAbort()
        {
            //the server accepts but never reads so a large request fills the socket buffers and the write waits
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();
            using var client = new TcpCqrsClient($"127.0.0.1:{((IPEndPoint)listener.LocalEndPoint!).Port}", serializer, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            using var cts = new CancellationTokenSource();

            var call = ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(byte[])], [new byte[64 * 1024 * 1024]], source, cts.Token);
            using var server = await listener.AcceptAsync(TestContext.Current.CancellationToken);
            await Task.Delay(200, TestContext.Current.CancellationToken); //the request fills the buffers and waits

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            cts.Cancel();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
            stopwatch.Stop();

            //the server doesn't have the whole request so there is no abort to acknowledge, waiting for one would add the abort timeout
            Assert.True(stopwatch.ElapsedMilliseconds < 800, $"cancel took {stopwatch.ElapsedMilliseconds}ms");
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
        public async Task CallTaskGeneric_InvalidResponseOnPooledConnection_DoesNotResend()
        {
            var calls = 0;
            using var server = new FakeServer(null, request => Interlocked.Increment(ref calls) == 1
                ? request.WriteModelAsync(42)
                : request.WriteRawAsync(System.Text.Encoding.UTF8.GetBytes("BAD|*|1~")));
            using var client = CreateClient(server, null);
            _ = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            //the server responded so it has the request, sending it again on a new connection could run it twice
            Assert.StartsWith("Call failed", exception.Message);
            Assert.Equal(2, server.Requests.Count);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_StreamResultDisposedEarly_DoesNotReuseConnection()
        {
            using var server = new FakeServer(null, request => request.Data.ProviderMethod == nameof(ITestQueryHandler.GetStream)
                ? request.WriteStreamAsync(new byte[100_000])
                : request.WriteModelAsync(42));
            using var client = CreateClient(server, null);

            await using (var stream = await ((IQueryClient)client).CallTaskGeneric<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source, default))
            {
                _ = await stream.ReadAsync(new byte[1], TestContext.Current.CancellationToken);
                await Task.Delay(50, TestContext.Current.CancellationToken); //let the rest of the body arrive
            }

            var result = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default);

            //the unread body stays with the old connection instead of being read as the next response
            Assert.Equal(42, result);
            Assert.Equal(2, server.Requests.Count);
            Assert.Equal(2, server.ConnectionCount);
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

        [Fact]
        public void Call_UnregisteredInterface_Throws()
        {
            using var client = new TcpCqrsClient("127.0.0.1:9999", serializer, null, null);

            var exception = Assert.Throws<Exception>(() =>
                ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source));

            Assert.Contains("is not registered", exception.Message);
        }

        [Fact]
        public void DispatchAsync_UnregisteredCommand_Throws()
        {
            using var client = new TcpCqrsClient("127.0.0.1:9999", serializer, null, null);

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
            _ = Assert.Throws<ArgumentNullException>(() => new TcpCqrsClient(url!, serializer, null, null));
        }

        [Fact]
        public void ServiceUrl_ReturnsConstructorUrl()
        {
            using var client = new TcpCqrsClient("127.0.0.1:9999", serializer, null, null);

            Assert.Equal("127.0.0.1:9999", ((IQueryClient)client).ServiceUrl);
            Assert.Equal("127.0.0.1:9999", ((ICommandProducer)client).MessageHost);
            Assert.Equal("127.0.0.1:9999", ((IEventProducer)client).MessageHost);
        }

        private static TcpCqrsClient CreateClient(FakeServer server, IEncryptor? encryptor)
        {
            var client = new TcpCqrsClient(server.Url, serializer, encryptor, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            ((IEventProducer)client).RegisterEventType(10, "test", typeof(TestEvent));
            return client;
        }

        private static void AssertQueryRequest(FakeRequest request, string methodName, int argument)
        {
            var providerType = typeof(ITestQueryHandler).AssemblyQualifiedName;
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
            Assert.Equal(messageType.AssemblyQualifiedName, request.Header.ProviderType);
            Assert.Equal(serializer.ContentType, request.Header.ContentType);
            Assert.Equal(messageType.AssemblyQualifiedName, request.Data.MessageType);
            Assert.Equal(messageAwait, request.Data.MessageAwait);
            Assert.Equal(messageResult, request.Data.MessageResult);
            Assert.Equal(source, request.Data.Source);
        }

        //Speaks the server side of the TCP protocol so the client can be tested without TcpCqrsServer
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
                Url = $"127.0.0.1:{((IPEndPoint)listener.LocalEndPoint!).Port}";

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

                        Stream body = new TcpProtocolBodyStream(stream, header.BodyStartBuffer, false, true);
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

            private async Task<TcpRequestHeader?> ReadHeaderAsync(Stream stream)
            {
                var buffer = new byte[TcpCommon.BufferLength];
                var position = 0;
                var length = 0;
                do
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(length), canceller.Token);
                    if (read == 0)
                        return null;
                    length += read;
                }
                while (!TcpCommon.TryReadToHeaderEnd(buffer.AsSpan(0, length), ref position));
                return TcpCommon.ReadHeader(buffer.AsMemory(0, length), position);
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

            public TcpRequestHeader Header { get; }
            public CqrsRequestData Data { get; }

            public FakeRequest(Stream stream, IEncryptor? encryptor, TcpRequestHeader header, CqrsRequestData data)
            {
                this.stream = stream;
                this.encryptor = encryptor;
                this.Header = header;
                this.Data = data;
            }

            public async Task WriteModelAsync(object? model)
            {
                await WriteHeaderAsync(false);
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

            public async Task WriteRawAsync(byte[] bytes) => await stream.WriteAsync(bytes);

            public Task CloseAsync()
            {
                stream.Dispose();
                return Task.CompletedTask;
            }

            private async Task WriteHeaderAsync(bool isError)
            {
                var buffer = new byte[TcpCommon.BufferLength];
                var headerLength = isError
                    ? TcpCommon.BufferErrorHeader(buffer, Header.ProviderType, serializer.ContentType)
                    : TcpCommon.BufferHeader(buffer, Header.ProviderType!, serializer.ContentType);
                await stream.WriteAsync(buffer.AsMemory(0, headerLength));
            }

            private async Task WriteBodyAsync(Func<Stream, Task> write)
            {
                var body = new TcpProtocolBodyStream(stream, null, true, true);
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
