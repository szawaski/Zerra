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
using Zerra.Serialization.Json;

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
                Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));

            Assert.Equal(3, server.Requests.Count);
        }

        //like HttpCqrsClient and KestrelCqrsClient, a gateway with allowed origins requires one
        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_SendsHostAsOrigin()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            _ = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken);

            Assert.True(server.Origins.TryDequeue(out var origin));
            Assert.Equal("localhost", origin);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_CanBeCalledRepeatedly()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            for (var i = 0; i < 3; i++)
                Assert.Equal(42, await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source), TestContext.Current.CancellationToken));
        }

        [Fact(Timeout = timeout)]
        public async Task CallTaskGeneric_StreamResult_IsReadable()
        {
            using var server = new FakeGateway(_ => [1, 2, 3, 4, 5]);
            using var client = CreateClient(server);

            await using var stream = await ((IQueryClient)client).CallTaskGeneric<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source, TestContext.Current.CancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);

            Assert.Equal([1, 2, 3, 4, 5], ms.ToArray());
        }

        [Fact(Timeout = timeout)]
        public async Task DispatchAwaitAsync_CommandWithResult_RequestsResult()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, TestContext.Current.CancellationToken);

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
                await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);
            else
                await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);

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

            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
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
            _ = await Assert.ThrowsAnyAsync<Exception>(() => ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));

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

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DispatchAsync_Event_SendsMessage(bool withLog)
        {
            using var server = new FakeGateway(_ => []);
            using var client = CreateClient(server, log: withLog ? new ErrorLog() : null);

            await ((IEventProducer)client).DispatchAsync(new TestEvent { Value = 5 }, source, TestContext.Current.CancellationToken);

            var request = Assert.Single(server.Requests);
            Assert.Equal(nameof(TestEvent), request.MessageType);
            Assert.Equal(5, JsonSerializer.Deserialize<TestEvent>(request.MessageData!)!.Value);
        }

        [Fact(Timeout = timeout)]
        public async Task Operations_WithAuthorizer_SendHeaders()
        {
            using var server = new FakeGateway(data => data.ProviderMethod is not null || data.MessageResult ? serializer.SerializeBytes(42) : []);
            using var client = CreateClient(server, new TestAuthorizer());

            Assert.Equal(42, await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source), TestContext.Current.CancellationToken));
            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
            await ((ICommandProducer)client).DispatchAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(["Bearer sync", "Bearer async", "Bearer async"], server.Authorizations);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_UploadStream_SendsDataThenStream()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            using var client = CreateClient(server);
            var upload = new byte[100_000];
            new Random(2).NextBytes(upload);

            Assert.Equal(42, await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.Upload), [typeof(int), typeof(Stream)], [3, new MemoryStream(upload)], source), TestContext.Current.CancellationToken));
            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.Upload), [typeof(int), typeof(Stream)], [3, new MemoryStream(upload)], source, TestContext.Current.CancellationToken));

            Assert.Equal(2, server.Uploads.Count);
            Assert.All(server.Uploads, x => Assert.Equal(upload, x));
            Assert.All(server.Requests, x => Assert.Equal(3, serializer.Deserialize<int>(x.ProviderArguments![0])));
        }

        [Fact(Timeout = timeout)]
        public async Task Call_TwoStreamArguments_ThrowsAndLogs()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42));
            var log = new ErrorLog();
            using var client = CreateClient(server, log: log);
            var queryClient = (IQueryClient)client;
            object[] arguments = [new MemoryStream(), new MemoryStream()];

            _ = Assert.Throws<ArgumentException>(() => queryClient.Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStreams), [typeof(Stream), typeof(Stream)], arguments, source));
            //ApiClient builds the request before its first await, so the failure is thrown before a task is returned
            _ = Assert.Throws<ArgumentException>(() => { _ = queryClient.CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStreams), [typeof(Stream), typeof(Stream)], arguments, source, TestContext.Current.CancellationToken); });
            _ = Assert.Throws<ArgumentException>(() => { _ = queryClient.CallTask(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStreams), [typeof(Stream), typeof(Stream)], arguments, source, TestContext.Current.CancellationToken); });
            Assert.Equal(3, log.Errors.Count);
            Assert.Empty(server.Requests);
        }

        [Fact(Timeout = timeout)]
        public async Task Operations_ErrorResponse_ThrowsRemoteServiceException()
        {
            var error = new MemoryStream();
            ExceptionSerializer.Serialize(serializer, error, new InvalidOperationException("gateway failed"));
            using var server = new FakeGateway(_ => error.ToArray(), 500);
            using var client = CreateClient(server);

            Assert.Equal("gateway failed", (await Assert.ThrowsAsync<RemoteServiceException>(() => Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source), TestContext.Current.CancellationToken))).Message);
            Assert.Equal("gateway failed", (await Assert.ThrowsAsync<RemoteServiceException>(() => ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken))).Message);
            Assert.Equal("gateway failed", (await Assert.ThrowsAsync<RemoteServiceException>(() => ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken))).Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_StreamResult_IsReadable()
        {
            using var server = new FakeGateway(_ => [1, 2, 3, 4, 5]);
            using var client = CreateClient(server);

            var bytes = await Task.Run(() =>
            {
                using var stream = ((IQueryClient)client).Call<Stream>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream), [], [], source);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }, TestContext.Current.CancellationToken);

            Assert.Equal([1, 2, 3, 4, 5], bytes);
        }

        [Fact(Timeout = timeout)]
        public async Task Call_VoidResult_ReadsNothing()
        {
            using var server = new FakeGateway(_ => []);
            using var client = CreateClient(server);

            //a Task query has no result to read
            await ((IQueryClient)client).CallTask(typeof(ITestQueryHandler), nameof(ITestQueryHandler.Run), [], [], source, TestContext.Current.CancellationToken);
            _ = Assert.Single(server.Requests);
        }

        [Theory(Timeout = timeout)]
        [InlineData(ContentType.Json)]
        [InlineData(ContentType.JsonNameless)]
        public async Task CallTaskGeneric_JsonSerializer_SendsJsonContentType(ContentType contentType)
        {
            ISerializer jsonSerializer = new ZerraJsonSerializer(new Zerra.Serialization.Json.JsonSerializerOptions() { Nameless = contentType == ContentType.JsonNameless });
            Assert.Equal(contentType, jsonSerializer.ContentType);
            string? received = null;
            using var listener = new HttpListener();
            int port;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                port = ((IPEndPoint)socket.LocalEndPoint!).Port;
            }
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            var serving = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                received = context.Request.ContentType;
                var bytes = jsonSerializer.SerializeBytes(42);
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }, TestContext.Current.CancellationToken);

            using var client = new ApiClient($"http://localhost:{port}/", jsonSerializer, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
            await serving;
            Assert.StartsWith(contentType == ContentType.Json ? HttpCommon.ContentTypeJson : HttpCommon.ContentTypeJsonNameless, received);
        }

        [Fact(Timeout = timeout)]
        public async Task Route_IsAppendedToTheEndpoint()
        {
            using var server = new FakeGateway(_ => serializer.SerializeBytes(42), route: "api");
            using var client = new ApiClient(server.Url, serializer, null, null, "api");
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));

            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
        }

        private sealed class TestAuthorizer : ICqrsAuthorizer
        {
            public void Authorize(Dictionary<string, List<string?>> headers) => throw new NotSupportedException();

            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new Dictionary<string, List<string?>>() { ["Authorization"] = ["Bearer async"] });

            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default)
                => new() { ["Authorization"] = ["Bearer sync"] };
        }

        private static ApiClient CreateClient(FakeGateway server, ICqrsAuthorizer? authorizer = null, ILogger? log = null)
        {
            var client = new ApiClient(server.Url, serializer, log, authorizer);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            ((IEventProducer)client).RegisterEventType(10, "test", typeof(TestEvent));
            return client;
        }

        //Stands in for the API gateway, answering every request with the bytes from the responder
        private sealed class FakeGateway : IDisposable
        {
            private readonly HttpListener listener;
            private readonly Func<ApiRequestData, byte[]> respond;
            private readonly int statusCode;

            public ConcurrentQueue<ApiRequestData> Requests { get; } = new();
            public ConcurrentQueue<string?> Origins { get; } = new();
            public ConcurrentQueue<string?> Authorizations { get; } = new();
            public ConcurrentQueue<byte[]> Uploads { get; } = new();
            public string Url { get; }

            public FakeGateway(Func<ApiRequestData, byte[]> respond, int statusCode = 200, string? route = null)
            {
                this.respond = respond;
                this.statusCode = statusCode;

                int port;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    port = ((IPEndPoint)socket.LocalEndPoint!).Port;
                }
                Url = $"http://localhost:{port}/";

                listener = new HttpListener();
                listener.Prefixes.Add(route is null ? Url : $"{Url}{route}/");
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
                        ApiRequestData? data;
                        if (context.Request.Headers[HttpCommon.UploadStreamHeader] == HttpCommon.UploadStreamValue)
                        {
                            //the request data is framed so the uploaded stream can follow it, the same as the gateway reads it
                            using (var uploadDataStream = new TcpProtocolBodyStream(context.Request.InputStream, null, false, true))
                                data = await serializer.DeserializeAsync<ApiRequestData>(uploadDataStream, default);
                            using var upload = new MemoryStream();
                            await context.Request.InputStream.CopyToAsync(upload);
                            Uploads.Enqueue(upload.ToArray());
                        }
                        else
                        {
                            data = await serializer.DeserializeAsync<ApiRequestData>(context.Request.InputStream, default);
                        }
                        Requests.Enqueue(data!);
                        Origins.Enqueue(context.Request.Headers["Origin"]);
                        Authorizations.Enqueue(context.Request.Headers["Authorization"]);

                        var bytes = respond(data!);
                        context.Response.StatusCode = statusCode;
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
            int Upload(int value, Stream stream);
            int GetStreams(Stream first, Stream second);
            Task Run();
        }

        public sealed class TestEvent : IEvent
        {
            public int Value { get; set; }
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
