// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    //HttpCqrsClient against the middleware hosted in a real Kestrel server, which frames and words its responses its own way
    public class HttpCqrsClientKestrelTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();
        private static readonly IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_ReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            await using var server = await TestServer.StartAsync(enc);
            using var client = CreateClient(server.Url, enc);

            var asyncResult = await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken);
            var syncResult = await Task.Run(() => ((IQueryClient)client).Call<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [5], source), TestContext.Current.CancellationToken);

            Assert.Equal(42, asyncResult);
            Assert.Equal(10, syncResult);
        }

        //larger than the serializer's 8 KB read buffer, and more than one of Kestrel's chunks
        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_LargeResult_ReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            await using var server = await TestServer.StartAsync(enc);
            using var client = CreateClient(server.Url, enc);

            var result = await ((IQueryClient)client).CallTaskGeneric<string>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetLarge), [typeof(int)], [50_000], source, TestContext.Current.CancellationToken);

            Assert.Equal(new string('x', 50_000), result);
        }

        //Kestrel sends "500 Internal Server Error" where HttpCqrsServer sends "500 Server Error"
        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_HandlerThrows_ThrowsRemoteServiceException(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            await using var server = await TestServer.StartAsync(enc);
            using var client = CreateClient(server.Url, enc);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.Fail), [], [], source, TestContext.Current.CancellationToken));

            Assert.Equal("query failed", exception.Message);

            //the connection is still good after the error
            Assert.Equal(42, await ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));
        }

        //the middleware answers a disallowed origin with an empty 401
        [Fact(Timeout = timeout)]
        public async Task Query_OriginNotAllowed_ThrowsWithStatus()
        {
            await using var server = await TestServer.StartAsync(null, ["example.com"]);
            using var client = CreateClient(server.Url, null);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() =>
                ((IQueryClient)client).CallTaskGeneric<int>(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), [typeof(int)], [21], source, TestContext.Current.CancellationToken));

            Assert.Contains("401 Unauthorized", exception.Message);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Command_WithResult_ReturnsResult(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            await using var server = await TestServer.StartAsync(enc);
            using var client = CreateClient(server.Url, enc);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(42, result);
            Assert.Equal(1, server.CommandCount);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_Await_RunsHandler()
        {
            await using var server = await TestServer.StartAsync(null);
            using var client = CreateClient(server.Url, null);

            await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(1, server.CommandCount);
        }

        [Fact(Timeout = timeout)]
        public async Task Event_RunsHandler()
        {
            await using var server = await TestServer.StartAsync(null);
            using var client = CreateClient(server.Url, null);

            await ((IEventProducer)client).DispatchAsync(new TestEvent { Value = 7 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(7, await server.EventReceived.WaitAsync(TestContext.Current.CancellationToken));
        }

        private static HttpCqrsClient CreateClient(string url, IEncryptor? encryptor)
        {
            var client = new HttpCqrsClient(url, serializer, encryptor, null, null);
            ((IQueryClient)client).RegisterInterfaceType(10, typeof(ITestQueryHandler));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommand));
            ((ICommandProducer)client).RegisterCommandType(10, "test", typeof(TestCommandWithResult));
            ((IEventProducer)client).RegisterEventType(10, "test", typeof(TestEvent));
            return client;
        }

        //the middleware on Kestrel with handlers for the test types
        private sealed class TestServer : IAsyncDisposable
        {
            private readonly WebApplication app;
            private readonly KestrelCqrsServerLinkedSettings settings;
            private readonly TaskCompletionSource<int> eventReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int commandCount;

            public string Url { get; }
            public int CommandCount => Volatile.Read(ref commandCount);
            public Task<int> EventReceived => eventReceived.Task;

            private TestServer(WebApplication app, KestrelCqrsServerLinkedSettings settings, string url)
            {
                this.app = app;
                this.settings = settings;
                this.Url = url;
            }

            public static async Task<TestServer> StartAsync(IEncryptor? encryptor, string[]? allowOrigins = null)
            {
                var settings = new KestrelCqrsServerLinkedSettings(null, null, serializer.ContentType) { AllowOrigins = allowOrigins };

                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Logging.ClearProviders();
                var app = builder.Build();
                _ = app.UseKestrelCqrsServer(serializer, encryptor, null, settings);
                await app.StartAsync();

                var server = new TestServer(app, settings, app.Urls.First());

                IQueryServer queryServer = new KestrelCqrsServerQueryServer(settings);
                queryServer.Setup(new CommandCounter(), (_, methodName, arguments, _, _, _) => methodName switch
                {
                    nameof(ITestQueryHandler.GetThings) => Task.FromResult(new RemoteQueryCallResponse(serializer.Deserialize<int>(arguments[0]) * 2)),
                    nameof(ITestQueryHandler.GetLarge) => Task.FromResult(new RemoteQueryCallResponse(new string('x', serializer.Deserialize<int>(arguments[0])))),
                    nameof(ITestQueryHandler.Fail) => Task.FromException<RemoteQueryCallResponse>(new InvalidOperationException("query failed")),
                    _ => throw new NotSupportedException(methodName)
                });
                queryServer.RegisterInterfaceType(10, typeof(ITestQueryHandler));

                ICommandConsumer commandConsumer = new KestrelCqrsServerCommandConsumer(settings);
                commandConsumer.Setup(new CommandCounter(),
                    (_, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.CompletedTask; },
                    (_, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.CompletedTask; },
                    (command, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.FromResult<object?>(((TestCommandWithResult)command).Value * 2); });
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommand));
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommandWithResult));

                IEventConsumer eventConsumer = new KestrelCqrsServerEventConsumer(settings);
                eventConsumer.Setup("test-service", (@event, eventSource) => { _ = server.eventReceived.TrySetResult(((TestEvent)@event).Value); return Task.CompletedTask; });
                eventConsumer.RegisterEventType(10, "test", typeof(TestEvent), EventConsumerMode.PerReplica);

                return server;
            }

            public async ValueTask DisposeAsync()
            {
                await app.StopAsync();
                await app.DisposeAsync();
                settings.Dispose();
            }
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            string GetLarge(int length);
            int Fail();
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
