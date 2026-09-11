// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    public class ApiClientTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_CanBeCalledRepeatedly()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Equal(3, server.Requests.Count);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_CanBeCalledRepeatedly()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source)));
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_StreamResult_IsReadable()
        {
            using var server = new FakeGateway(_ => [1, 2, 3, 4, 5]);
            using var client = CreateClient(server);

            await using var stream = await ((IQueryClient)client).CallTaskGeneric<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source, default);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal([1, 2, 3, 4, 5], ms.ToArray());
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAwaitAsync_CommandWithResult_RequestsResult()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, default);

            Assert.Equal(42, result);
            var request = Assert.Single(server.Requests);
            Assert.True(request.MessageAwait);
            Assert.True(request.MessageResult);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispatch_Command_SendsMessageAwait(bool messageAwait)
        {
            using var server = new FakeGateway(_ => []);
            using var client = CreateClient(server);

            if (messageAwait)
                await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, default);
            else
                await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, default);

            var request = Assert.Single(server.Requests);
            Assert.Equal(messageAwait, request.MessageAwait);
            Assert.False(request.MessageResult);
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_UrlWithoutScheme_UsesHttp()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = new ApiClient(server.Url["http://".Length..], serializer, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));

            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_Fails_LogsError()
        {
            int port;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            }
            var log = new ErrorLog();
            using var client = new ApiClient($"http://127.0.0.1:{port}/", serializer, log, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));

            //the failure happens in the returned task, nothing is listening
            _ = await Assert.ThrowsAnyAsync<Exception>(() => ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, default));

            Assert.Equal("Call Failed", Assert.Single(log.Errors));
        }

        private sealed class ErrorLog : ILogger
        {
            public ConcurrentQueue<string?> Errors { get; } = new();
            public void Trace(string message) { }
            public void Debug(string message) { }
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string? message = null, Exception? ex = null) => Errors.Enqueue(message);
            public void Error(Exception? ex = null) => Errors.Enqueue(null);
            public void Critical(string? message = null, Exception? ex = null) { }
            public void Critical(Exception? ex = null) { }
        }

        private static ApiClient CreateClient(FakeGateway server)
        {
            var client = new ApiClient(server.Url, serializer, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            return client;
        }

        //Stands in for the API gateway, answering every request with the bytes from the responder
        private sealed class FakeGateway : IDisposable
        {
            private readonly HttpListener listener;
            private readonly Func<ApiRequestData, byte[]> respond;

            public ConcurrentQueue<ApiRequestData> Requests { get; } = new();
            public string Url { get; }

            public FakeGateway(Func<ApiRequestData, byte[]> respond)
            {
                this.respond = respond;

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
                        var data = await serializer.DeserializeAsync<ApiRequestData>(context.Request.InputStream, default);
                        Requests.Enqueue(data!);

                        var bytes = respond(data!);
                        context.Response.StatusCode = 200;
                        context.Response.ContentLength64 = bytes.Length;
                        await context.Response.OutputStream.WriteAsync(bytes);
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
    }
}
