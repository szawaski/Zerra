// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    //KestrelCqrsClient against the real KestrelCqrsServerMiddleware hosted in a real Kestrel server (KestrelCqrsClientTests
    //covers the client against a hand-rolled fake server, HttpCqrsClientKestrelTests covers a different client against this
    //same real middleware), the pairing Store.Shipping.Service in Demo/Store actually uses for command and event dispatch.
    public class KestrelCqrsClientIntegrationTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();

        [Fact(Timeout = timeout)]
        public async Task Command_Await_RunsHandler()
        {
            await using var server = await TestServer.StartAsync();
            using var client = CreateClient(server.Url);

            await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommand { Value = 5 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(1, server.CommandCount);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_WithResult_ReturnsResult()
        {
            await using var server = await TestServer.StartAsync();
            using var client = CreateClient(server.Url);

            var result = await ((ICommandProducer)client).DispatchAwaitAsync(new TestCommandWithResult { Value = 21 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(42, result);
        }

        [Fact(Timeout = timeout)]
        public async Task Event_RunsHandler()
        {
            await using var server = await TestServer.StartAsync();
            using var client = CreateClient(server.Url);

            await ((IEventProducer)client).DispatchAsync(new TestEvent { Value = 7 }, source, TestContext.Current.CancellationToken);

            Assert.Equal(7, await server.EventReceived.WaitAsync(TestContext.Current.CancellationToken));
        }

        private static KestrelCqrsClient CreateClient(string url)
        {
            var client = new KestrelCqrsClient(url, serializer, null, null, null, null);
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

            public static async Task<TestServer> StartAsync()
            {
                var settings = new KestrelCqrsServerLinkedSettings(null, null, serializer.ContentType);

                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Logging.ClearProviders();
                var app = builder.Build();
                _ = app.UseKestrelCqrsServer(serializer, null, null, settings);
                await app.StartAsync();

                var server = new TestServer(app, settings, app.Urls.First());

                ICommandConsumer commandConsumer = new KestrelCqrsServerCommandConsumer(settings);
                commandConsumer.Setup(new CommandCounter(),
                    (_, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.CompletedTask; },
                    (_, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.CompletedTask; },
                    (command, _, _) => { _ = Interlocked.Increment(ref server.commandCount); return Task.FromResult<object?>(((TestCommandWithResult)command).Value * 2); });
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommand));
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommandWithResult));

                IEventConsumer eventConsumer = new KestrelCqrsServerEventConsumer(settings);
                eventConsumer.Setup((@event, eventSource) => { _ = server.eventReceived.TrySetResult(((TestEvent)@event).Value); return Task.CompletedTask; });
                eventConsumer.RegisterEventType(10, "test", typeof(TestEvent));

                return server;
            }

            public async ValueTask DisposeAsync()
            {
                await app.StopAsync();
                await app.DisposeAsync();
                settings.Dispose();
            }
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
