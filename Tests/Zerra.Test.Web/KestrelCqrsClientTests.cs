// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class KestrelCqrsClientTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();
        private static readonly IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_CanBeCalledRepeatedly(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, _ => FakeResponse.Model(42));
            using var client = CreateClient(server, enc);

            //the client is shared by every request so one call must not leave it unusable
            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));

            Assert.Equal(3, server.Requests.Count);
            var request = server.Requests.First();
            Assert.Equal(typeof(ITestQueryHandler).AssemblyQualifiedName, request.Data.ProviderType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), request.Data.ProviderMethod);
            Assert.Equal(21, serializer.Deserialize<int>(request.Data.ProviderArguments![0]));
            Assert.Equal(source, request.Data.Source);
            Assert.Equal("localhost", request.OriginHeader); //the service host like HttpCqrsClient, so a server with allowed origins accepts it
        }

        [Fact(Timeout = timeout)]
        public async Task Call_CanBeCalledRepeatedly()
        {
            using var server = new FakeServer(null, _ => FakeResponse.Model(42));
            using var client = CreateClient(server, null);

            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source)));
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_StreamResult_IsReadable(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, _ => new FakeResponse(200, [1, 2, 3, 4, 5]));
            using var client = CreateClient(server, enc);

            //the response stays open for the stream after the call returns
            await using var stream = await ((IQueryClient)client).CallTaskGeneric<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source, TestContext.Current.CancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal([1, 2, 3, 4, 5], ms.ToArray());
        }

        [Fact(Timeout = timeout)]
        public async Task Call_StreamResult_IsReadable()
        {
            using var server = new FakeServer(null, _ => new FakeResponse(200, [1, 2, 3, 4, 5]));
            using var client = CreateClient(server, null);

            using var stream = await Task.Run(() => ((IQueryClient)client).Call<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source));
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            Assert.Equal([1, 2, 3, 4, 5], ms.ToArray());
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CallTaskGeneric_ErrorResponse_ThrowsRemoteServiceException(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, _ => FakeResponse.Error("query failed"));
            using var client = CreateClient(server, enc);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));

            Assert.Equal("query failed", exception.Message);
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);

            //a failed call doesn't leave the client unusable either
            server.Respond = _ => FakeResponse.Model(42);
            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DispatchAwaitAsync_CommandWithResult_RequestsResult(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = new FakeServer(enc, _ => FakeResponse.Model(42));
            using var client = CreateClient(server, enc);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(42, result);
            var request = Assert.Single(server.Requests);
            Assert.Equal(typeof(TestCommandWithResult).AssemblyQualifiedName, request.Data.MessageType);
            Assert.True(request.Data.MessageAwait);
            Assert.True(request.Data.MessageResult); //the server only responds with the result when asked for it
            Assert.Equal(21, ((TestCommandWithResult)serializer.Deserialize(request.Data.MessageData!, typeof(TestCommandWithResult))!).Value);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispatch_Command_SendsMessageAwait(bool messageAwait)
        {
            using var server = new FakeServer(null, _ => new FakeResponse(200, []));
            using var client = CreateClient(server, null);

            if (messageAwait)
                await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);
            else
                await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);

            var request = Assert.Single(server.Requests);
            Assert.Equal(typeof(TestCommand).AssemblyQualifiedName, request.Data.MessageType);
            Assert.Equal(typeof(TestCommand).AssemblyQualifiedName, request.ProviderTypeHeader);
            Assert.Equal(messageAwait, request.Data.MessageAwait);
            Assert.False(request.Data.MessageResult);
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAsync_Event_SendsMessage()
        {
            using var server = new FakeServer(null, _ => new FakeResponse(200, []));
            using var client = CreateClient(server, null);

            await ((IEventProducer)client).DispatchAsync(new TestEvent { Value = 7 }, source, TestContext.Current.CancellationToken);

            var request = Assert.Single(server.Requests);
            Assert.Equal(typeof(TestEvent).AssemblyQualifiedName, request.Data.MessageType);
            Assert.Equal(7, ((TestEvent)serializer.Deserialize(request.Data.MessageData!, typeof(TestEvent))!).Value);
        }

        private static KestrelCqrsClient CreateClient(FakeServer server, IEncryptor? encryptor)
        {
            var client = new KestrelCqrsClient(server.Url, serializer, encryptor, null, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            ((IEventProducer)client).RegisterEventType(10, "test", typeof(TestEvent));
            return client;
        }

        private sealed record FakeRequest(string? ProviderTypeHeader, string? OriginHeader, CqrsRequestData Data);

        private sealed record FakeResponse(int StatusCode, byte[] Body)
        {
            public static FakeResponse Model(object model) => new(200, serializer.SerializeBytes(model));
            public static FakeResponse Error(string message) => new(500, ExceptionSerializer.Serialize(serializer, new InvalidOperationException(message)));
        }

        //Stands in for the Kestrel server, answering every request with the response from the responder
        private sealed class FakeServer : IDisposable
        {
            private readonly HttpListener listener;
            private readonly IEncryptor? encryptor;

            public Func<FakeRequest, FakeResponse> Respond { get; set; }
            public ConcurrentQueue<FakeRequest> Requests { get; } = new();
            public string Url { get; }

            public FakeServer(IEncryptor? encryptor, Func<FakeRequest, FakeResponse> respond)
            {
                this.encryptor = encryptor;
                this.Respond = respond;

                int port;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    port = ((IPEndPoint)socket.LocalEndPoint!).Port;
                }
                Url = $"http://localhost:{port}/";

                listener = new HttpListener();
                listener.Prefixes.Add(Url);
                listener.Start();
                _ = HandleRequests();
            }

            private async Task HandleRequests()
            {
                try
                {
                    for (; ; )
                    {
                        var context = await listener.GetContextAsync();

                        Stream body = context.Request.InputStream;
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body, false);
                        var data = await serializer.DeserializeAsync<CqrsRequestData>(body, default);
                        var request = new FakeRequest(context.Request.Headers[HttpCommon.ProviderTypeHeader], context.Request.Headers[HttpCommon.OriginHeader], data!);
                        Requests.Enqueue(request);

                        var response = Respond(request);
                        context.Response.StatusCode = response.StatusCode;
                        if (encryptor is not null)
                        {
                            var cryptoStream = encryptor.Encrypt(context.Response.OutputStream, true);
                            await cryptoStream.WriteAsync(response.Body);
                            await cryptoStream.FlushFinalBlockAsync();
                            await cryptoStream.DisposeAsync();
                        }
                        else
                        {
                            context.Response.ContentLength64 = response.Body.Length;
                            await context.Response.OutputStream.WriteAsync(response.Body);
                        }
                        context.Response.Close();
                    }
                }
                catch { } //stopped
            }

            public void Dispose() => listener.Close();
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
