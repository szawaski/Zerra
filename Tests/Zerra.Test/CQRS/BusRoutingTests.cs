// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Test.Helpers;

namespace Zerra.Test.CQRS
{
    public class BusRoutingTests
    {
        private static readonly ZerraByteSerializer serializer = new();

        [Fact]
        public async Task Failures_AreLogged()
        {
            var url = TestNetwork.NewUrl();
            var clientLog = new RecordingBusLogger();
            var serverLog = new RecordingBusLogger();
            var handler = new RoutingHandler();

            var server = Bus.New("server", null, serverLog, null);
            server.AddHandler<IRoutingCommandHandler>(handler);
            server.AddHandler<IRoutingEventHandler>(handler);
            server.AddHandler<IRoutingQueryHandler>(handler);
            var tcpServer = new TcpCqrsServer(url, serializer, null, null, null);
            server.AddCommandConsumer<IRoutingCommandHandler>(tcpServer);
            server.AddEventConsumer<IRoutingEventHandler>(tcpServer, EventConsumerMode.PerService);
            server.AddQueryServer<IRoutingQueryHandler>(tcpServer);

            var client = Bus.New("client", null, clientLog, null);
            var tcpClient = new TcpCqrsClient(url, serializer, null, null, null);
            client.AddCommandProducer<IRoutingCommandHandler>(tcpClient);
            client.AddEventProducer<IRoutingEventHandler>(tcpClient);
            client.AddQueryClient<IRoutingQueryHandler>(tcpClient);

            try
            {
                var caller = client.Call<IRoutingQueryHandler>();
                Assert.Equal(2, caller.Double(1));
                Assert.Equal(4, await caller.DoubleAsync(2));
                await caller.RunAsync(3);
                await caller.RunWithCancellationAsync(4, TestContext.Current.CancellationToken);
                Assert.Equal([3, 4], handler.Received);

                _ = Assert.ThrowsAny<Exception>(() => caller.Double(-1));
                _ = await Assert.ThrowsAnyAsync<Exception>(() => caller.DoubleAsync(-1));
                _ = await Assert.ThrowsAnyAsync<Exception>(() => caller.RunAsync(-1));
                _ = await Assert.ThrowsAnyAsync<Exception>(() => client.DispatchAwaitAsync(new RoutingCommand { Value = -1 }));
                _ = await Assert.ThrowsAnyAsync<Exception>(() => client.DispatchAwaitAsync(new RoutingCommandWithResult { Value = -1 }));

                Assert.Equal(3, clientLog.Entries.Count(x => x.Kind == "Call" && !x.Handled && x.Exception is not null));
                Assert.Equal(3, serverLog.Entries.Count(x => x.Kind == "Call" && x.Handled && x.Exception is InvalidOperationException));
                Assert.Equal(2, clientLog.Entries.Count(x => x.Kind == "Command" && !x.Handled && x.Exception is not null));
                Assert.Equal(2, serverLog.Entries.Count(x => x.Kind == "Command" && x.Handled && x.Exception is InvalidOperationException));
            }
            finally
            {
                await server.StopServicesAsync();
            }

            //the server is gone, the event can't be sent
            _ = await Assert.ThrowsAnyAsync<Exception>(() => client.DispatchAsync(new RoutingEvent { Value = 5 }, TimeSpan.FromSeconds(1)));
            Assert.Single(clientLog.Entries, x => x.Kind == "Event" && !x.Handled && x.Exception is not null);

            await client.StopServicesAsync();
        }

        [Fact]
        public async Task Events_ToSeveralProducers_Logged()
        {
            var url1 = TestNetwork.NewUrl();
            var url2 = TestNetwork.NewUrl();
            var clientLog = new RecordingBusLogger();
            var handler1 = new RoutingHandler();
            var handler2 = new RoutingHandler();

            var server1 = Bus.New("server1", null, null, null);
            server1.AddHandler<IRoutingEventHandler>(handler1);
            server1.AddEventConsumer<IRoutingEventHandler>(new TcpCqrsServer(url1, serializer, null, null, null), EventConsumerMode.PerService);
            var server2 = Bus.New("server2", null, null, null);
            server2.AddHandler<IRoutingEventHandler>(handler2);
            server2.AddEventConsumer<IRoutingEventHandler>(new TcpCqrsServer(url2, serializer, null, null, null), EventConsumerMode.PerService);

            var client = Bus.New("client", null, clientLog, null);
            client.AddEventProducer<IRoutingEventHandler>(new TcpCqrsClient(url1, serializer, null, null, null));
            client.AddEventProducer<IRoutingEventHandler>(new TcpCqrsClient(url2, serializer, null, null, null));

            try
            {
                await client.DispatchAsync(new RoutingEvent { Value = 7 });
                await handler1.Wait(1);
                await handler2.Wait(1);
                Assert.Equal([7], handler1.Received);
                Assert.Equal([7], handler2.Received);
                Assert.Equal(2, clientLog.Entries.Count(x => x.Kind == "Event" && !x.Handled && x.Exception is null));
            }
            finally
            {
                await client.StopServicesAsync();
                await server1.StopServicesAsync();
                await server2.StopServicesAsync();
            }
        }

        [Fact]
        public async Task Events_RelayedWithoutHandler()
        {
            //a service receiving an event it doesn't handle sends it on through its own producer
            var relayUrl = TestNetwork.NewUrl();
            var destinationUrl = TestNetwork.NewUrl();
            var relayLog = new RecordingBusLogger();
            var handler = new RoutingHandler();

            var destination = Bus.New("destination", null, null, null);
            destination.AddHandler<IRoutingEventHandler>(handler);
            destination.AddEventConsumer<IRoutingEventHandler>(new TcpCqrsServer(destinationUrl, serializer, null, null, null), EventConsumerMode.PerService);

            var relay = Bus.New("relay", null, relayLog, null);
            relay.AddEventConsumer<IRoutingEventHandler>(new TcpCqrsServer(relayUrl, serializer, null, null, null), EventConsumerMode.PerService);
            relay.AddEventProducer<IRoutingEventHandler>(new TcpCqrsClient(destinationUrl, serializer, null, null, null));

            var source = Bus.New("source", null, null, null);
            source.AddEventProducer<IRoutingEventHandler>(new TcpCqrsClient(relayUrl, serializer, null, null, null));

            try
            {
                await source.DispatchAsync(new RoutingEvent { Value = 9 });
                await handler.Wait(1);
                Assert.Equal([9], handler.Received);
            }
            finally
            {
                await source.StopServicesAsync();
                await relay.StopServicesAsync();
                await destination.StopServicesAsync();
            }
        }

        [Fact]
        public async Task Events_ReceivedAreLogged()
        {
            var url = TestNetwork.NewUrl();
            var serverLog = new RecordingBusLogger();
            var handler = new RoutingHandler();

            var server = Bus.New("server", null, serverLog, null);
            server.AddHandler<IRoutingEventHandler>(handler);
            server.AddEventConsumer<IRoutingEventHandler>(new TcpCqrsServer(url, serializer, null, null, null), EventConsumerMode.PerService);
            var client = Bus.New("client", null, null, null);
            client.AddEventProducer<IRoutingEventHandler>(new TcpCqrsClient(url, serializer, null, null, null));

            try
            {
                await client.DispatchAsync(new RoutingEvent { Value = 1 });
                await handler.Wait(1);
                for (var i = 0; i < 100 && serverLog.Entries.IsEmpty; i++)
                    await Task.Delay(10, TestContext.Current.CancellationToken);
                Assert.Single(serverLog.Entries, x => x.Kind == "Event" && x.Handled && x.Exception is null);
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact]
        public async Task Timeouts()
        {
            //the server takes the connection but never answers
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();
            var url = $"127.0.0.1:{((IPEndPoint)listener.LocalEndPoint!).Port}";

            var client = Bus.New("client", null, null, null, defaultCallTimeout: TimeSpan.FromMilliseconds(100), defaultDispatchTimeout: TimeSpan.FromMilliseconds(100));
            var tcpClient = new TcpCqrsClient(url, serializer, null, null, null);
            client.AddCommandProducer<IRoutingCommandHandler>(tcpClient);
            client.AddEventProducer<IRoutingEventHandler>(tcpClient);
            client.AddQueryClient<IRoutingQueryHandler>(tcpClient);

            try
            {
                _ = await Assert.ThrowsAsync<TimeoutException>(() => client.DispatchAsync(new RoutingCommand { Value = 1 }));
                _ = await Assert.ThrowsAsync<TimeoutException>(() => client.DispatchAsync(new RoutingEvent { Value = 1 }));
                _ = await Assert.ThrowsAsync<TimeoutException>(() => client.DispatchAsync(new RoutingCommand { Value = 1 }, TimeSpan.FromMilliseconds(100)));
                _ = await Assert.ThrowsAsync<TimeoutException>(() => client.DispatchAsync(new RoutingEvent { Value = 1 }, TimeSpan.FromMilliseconds(100)));

                var caller = client.Call<IRoutingQueryHandler>();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caller.RunAsync(1));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caller.RunWithCancellationAsync(1, CancellationToken.None));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caller.DoubleAsync(1));
            }
            finally
            {
                await client.StopServicesAsync();
            }
        }

        [Fact]
        public async Task NoTimeout()
        {
            var url = TestNetwork.NewUrl();
            var handler = new RoutingHandler();
            var server = Bus.New("server", null, null, null);
            server.AddHandler<IRoutingCommandHandler>(handler);
            server.AddHandler<IRoutingEventHandler>(handler);
            server.AddHandler<IRoutingQueryHandler>(handler);
            var tcpServer = new TcpCqrsServer(url, serializer, null, null, null);
            server.AddCommandConsumer<IRoutingCommandHandler>(tcpServer);
            server.AddEventConsumer<IRoutingEventHandler>(tcpServer, EventConsumerMode.PerService);
            server.AddQueryServer<IRoutingQueryHandler>(tcpServer);

            var client = Bus.New("client", null, null, null, defaultCallTimeout: TimeSpan.FromSeconds(10), defaultDispatchTimeout: TimeSpan.FromSeconds(10));
            var tcpClient = new TcpCqrsClient(url, serializer, null, null, null);
            client.AddCommandProducer<IRoutingCommandHandler>(tcpClient);
            client.AddEventProducer<IRoutingEventHandler>(tcpClient);
            client.AddQueryClient<IRoutingQueryHandler>(tcpClient);

            try
            {
                await client.DispatchAsync(new RoutingCommand { Value = 1 });
                await client.DispatchAsync(new RoutingEvent { Value = 2 });
                await client.DispatchAsync(new RoutingCommand { Value = 3 }, Timeout.InfiniteTimeSpan);
                await client.DispatchAsync(new RoutingEvent { Value = 4 }, Timeout.InfiniteTimeSpan);
                await client.DispatchAwaitAsync(new RoutingCommand { Value = 5 }, Timeout.InfiniteTimeSpan);
                Assert.Equal(12, await client.DispatchAwaitAsync(new RoutingCommandWithResult { Value = 6 }, Timeout.InfiniteTimeSpan));
                await client.Call<IRoutingQueryHandler>().RunAsync(7);
                await handler.Wait(7);
                Assert.Equal([1, 2, 3, 4, 5, 6, 7], handler.Received.OrderBy(x => x));
            }
            finally
            {
                await client.StopServicesAsync();
                await server.StopServicesAsync();
            }
        }

        [Fact]
        public async Task NoHandlerOrProducer_Throws()
        {
            var bus = Bus.New("client", null, null, null);
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAsync(new RoutingCommand()); });
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAwaitAsync(new RoutingCommandWithResult()); });
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAsync(new RoutingEvent()); });
            _ = Assert.Throws<InvalidOperationException>(() => bus.Call<IRoutingQueryHandler>().Double(1));
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.Call<IRoutingQueryHandler>().RunAsync(1); });
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.Call<IRoutingQueryHandler>().DoubleAsync(1); });

            //producers for other messages only
            var tcpClient = new TcpCqrsClient(TestNetwork.NewUrl(), serializer, null, null, null);
            bus.AddCommandProducer<IOtherCommandHandler>(tcpClient);
            bus.AddEventProducer<IOtherEventHandler>(tcpClient);
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAsync(new RoutingCommand()); });
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAwaitAsync(new RoutingCommandWithResult()); });
            _ = Assert.Throws<InvalidOperationException>(() => { _ = bus.DispatchAsync(new RoutingEvent()); });

            await bus.StopServicesAsync();
        }

        [Fact]
        public async Task StopServices_Sync()
        {
            var url = TestNetwork.NewUrl();
            var handler = new RoutingHandler();
            var server = Bus.New("server", null, null, null);
            server.AddHandler<IRoutingCommandHandler>(handler);
            server.AddHandler<IRoutingEventHandler>(handler);
            server.AddHandler<IRoutingQueryHandler>(handler);
            var tcpServer = new TcpCqrsServer(url, serializer, null, null, null);
            server.AddCommandConsumer<IRoutingCommandHandler>(tcpServer);
            server.AddEventConsumer<IRoutingEventHandler>(tcpServer, EventConsumerMode.PerService);
            server.AddQueryServer<IRoutingQueryHandler>(tcpServer);

            var client = Bus.New("client", null, null, null);
            var tcpClient = new TcpCqrsClient(url, serializer, null, null, null);
            client.AddCommandProducer<IRoutingCommandHandler>(tcpClient);
            client.AddEventProducer<IRoutingEventHandler>(tcpClient);
            client.AddQueryClient<IRoutingQueryHandler>(tcpClient);

            await client.DispatchAwaitAsync(new RoutingCommand { Value = 1 });
            Assert.Equal(2, client.Call<IRoutingQueryHandler>().Double(1));

            server.StopServices();
            _ = await Assert.ThrowsAnyAsync<Exception>(() => client.DispatchAwaitAsync(new RoutingCommand { Value = 2 }, TimeSpan.FromSeconds(1)));
            client.StopServices();
        }

        [Fact]
        public async Task StopServices_Sync_StopsWaitingAfterTheTimeout()
        {
            var url = TestNetwork.NewUrl();
            var log = new RecordingLogger();
            var handler = new BusShutdownTests.ShutdownCommandHandler();
            var server = Bus.New("server", log, null, null, shutdownTimeout: TimeSpan.FromMilliseconds(300));
            server.AddHandler<BusShutdownTests.IShutdownCommandHandler>(handler);
            server.AddCommandConsumer<BusShutdownTests.IShutdownCommandHandler>(new TcpCqrsServer(url, serializer, null, null, null));

            var client = Bus.New("client", null, null, null);
            client.AddCommandProducer<BusShutdownTests.IShutdownCommandHandler>(new TcpCqrsClient(url, serializer, null, null, null));

            await client.DispatchAsync(new BusShutdownTests.ShutdownCommand() { Delay = 3000 });
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            server.StopServices();
            Assert.True(stopwatch.ElapsedMilliseconds < 2500, $"stopping gave up waiting after the timeout, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Contains(log.Entries, x => x.Level == "Warn");

            await client.StopServicesAsync();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StopServices_DisposeFailureIsLogged(bool async)
        {
            //a TCP server's dispose doesn't throw, so this one fails on purpose
            var log = new RecordingLogger();
            var bus = Bus.New("server", log, null, null);
            bus.AddCommandConsumer<IRoutingCommandHandler>(new FailingDisposeConsumer());
            if (async)
                await bus.StopServicesAsync();
            else
                bus.StopServices();
            Assert.Contains(log.Entries, x => x.Level == "Error" && x.Exception is InvalidOperationException);
        }

        [Fact]
        public void Add_Validates()
        {
            var tcpClient = new TcpCqrsClient(TestNetwork.NewUrl(), serializer, null, null, null);
            var tcpServer = new TcpCqrsServer(TestNetwork.NewUrl(), serializer, null, null, null);
            var bus = Bus.New("test", null, null, null);

            _ = Assert.ThrowsAny<Exception>(() => bus.AddHandler<object>(new RoutingHandler()));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddHandler<IRoutingCommandHandler>(null!));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddHandler<IDisposable>(new MemoryStream()));

            _ = Assert.ThrowsAny<Exception>(() => bus.AddCommandProducer<object>(tcpClient));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddCommandProducer<IRoutingCommandHandler>(null!));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddCommandConsumer<object>(tcpServer));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddCommandConsumer<IRoutingCommandHandler>(null!));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddEventProducer<object>(tcpClient));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddEventProducer<IRoutingEventHandler>(null!));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddEventConsumer<object>(tcpServer, EventConsumerMode.PerService));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddEventConsumer<IRoutingEventHandler>(null!, EventConsumerMode.PerService));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddQueryClient<object>(tcpClient));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddQueryClient<IRoutingQueryHandler>(null!));
            _ = Assert.ThrowsAny<Exception>(() => bus.AddQueryServer<object>(tcpServer));
            _ = Assert.Throws<ArgumentNullException>(() => bus.AddQueryServer<IRoutingQueryHandler>(null!));

            tcpServer.Dispose();
        }

        [Fact]
        public async Task Add_Conflicts_AreLoggedAndSkipped()
        {
            var log = new RecordingLogger();
            var tcpClient = new TcpCqrsClient(TestNetwork.NewUrl(), serializer, null, null, null);
            var tcpServer = new TcpCqrsServer(TestNetwork.NewUrl(), serializer, null, null, null);
            var bus = Bus.New("test", log, null, null);

            //interfaces without the messages
            bus.AddCommandProducer<IRoutingQueryHandler>(tcpClient);
            bus.AddCommandConsumer<IRoutingQueryHandler>(tcpServer);
            bus.AddEventProducer<IRoutingQueryHandler>(tcpClient);
            bus.AddEventConsumer<IRoutingQueryHandler>(tcpServer, EventConsumerMode.PerService);
            Assert.Equal(4, log.Entries.Count(x => x.Level == "Error"));

            //a command both produced and consumed, or produced twice
            bus.AddCommandConsumer<IRoutingCommandHandler>(tcpServer);
            bus.AddCommandProducer<IRoutingCommandHandler>(tcpClient);
            bus.AddCommandProducer<IOtherCommandHandler>(tcpClient);
            bus.AddCommandProducer<IOtherCommandHandler>(tcpClient);
            bus.AddCommandConsumer<IOtherCommandHandler>(tcpServer);
            Assert.Equal(4 + 2 + 1 + 1, log.Entries.Count(x => x.Level == "Error"));

            //a query both handled and called, or called twice, or served and called
            bus.AddHandler<IRoutingQueryHandler>(new RoutingHandler());
            bus.AddQueryClient<IRoutingQueryHandler>(tcpClient);
            bus.AddQueryClient<IOtherQueryHandler>(tcpClient);
            bus.AddQueryClient<IOtherQueryHandler>(tcpClient);
            bus.AddQueryServer<IOtherQueryHandler>(tcpServer);
            Assert.Equal(8 + 3, log.Entries.Count(x => x.Level == "Error"));

            Assert.Equal(typeof(RoutingCommand), bus.GetTypeByName(nameof(RoutingCommand)));
            Assert.Equal(typeof(IOtherQueryHandler), bus.GetTypeByName(nameof(IOtherQueryHandler)));
            Assert.Null(bus.GetTypeByName("Missing"));

            await bus.StopServicesAsync();
        }

        [Fact]
        public async Task Add_SameNames_Throw()
        {
            //the name is what's sent between services, so two types with the same name can't be told apart
            var tcpClient = new TcpCqrsClient(TestNetwork.NewUrl(), serializer, null, null, null);
            var tcpServer = new TcpCqrsServer(TestNetwork.NewUrl(), serializer, null, null, null);

            var handlers = Bus.New("test", null, null, null);
            handlers.AddHandler<SameNameA.ISameQueryHandler>(new SameNameA.SameHandler());
            _ = Assert.Throws<InvalidOperationException>(() => handlers.AddHandler<SameNameB.ISameQueryHandler>(new SameNameB.SameHandler()));
            handlers.AddHandler<SameNameA.ISameCommandHandler>(new SameNameA.SameHandler());
            _ = Assert.Throws<InvalidOperationException>(() => handlers.AddHandler<SameNameB.ISameCommandHandlerB>(new SameNameB.SameHandler()));
            handlers.AddHandler<SameNameA.ISameEventHandler>(new SameNameA.SameHandler());
            _ = Assert.Throws<InvalidOperationException>(() => handlers.AddHandler<SameNameB.ISameEventHandlerB>(new SameNameB.SameHandler()));

            var clients = Bus.New("test", null, null, null);
            clients.AddQueryClient<SameNameA.ISameQueryHandler>(tcpClient);
            _ = Assert.Throws<InvalidOperationException>(() => clients.AddQueryClient<SameNameB.ISameQueryHandler>(tcpClient));
            clients.AddCommandProducer<SameNameA.ISameCommandHandler>(tcpClient);
            _ = Assert.Throws<InvalidOperationException>(() => clients.AddCommandProducer<SameNameB.ISameCommandHandlerB>(tcpClient));
            clients.AddEventProducer<SameNameA.ISameEventHandler>(tcpClient);
            _ = Assert.Throws<InvalidOperationException>(() => clients.AddEventProducer<SameNameB.ISameEventHandlerB>(tcpClient));

            var servers = Bus.New("test", null, null, null);
            servers.AddQueryServer<SameNameA.ISameQueryHandler>(tcpServer);
            _ = Assert.Throws<InvalidOperationException>(() => servers.AddQueryServer<SameNameB.ISameQueryHandler>(tcpServer));
            servers.AddCommandConsumer<SameNameA.ISameCommandHandler>(tcpServer);
            _ = Assert.Throws<InvalidOperationException>(() => servers.AddCommandConsumer<SameNameB.ISameCommandHandlerB>(tcpServer));
            servers.AddEventConsumer<SameNameA.ISameEventHandler>(tcpServer, EventConsumerMode.PerService);
            _ = Assert.Throws<InvalidOperationException>(() => servers.AddEventConsumer<SameNameB.ISameEventHandlerB>(tcpServer, EventConsumerMode.PerService));

            await clients.StopServicesAsync();
            await servers.StopServicesAsync();
        }

        [Fact]
        public void Services()
        {
            var services = new BusServices();
            var stream = new MemoryStream();
            services.AddService<IDisposable>(stream);
            var bus = Bus.New("test", null, null, services);

            Assert.Same(stream, bus.GetService<IDisposable>());
            Assert.True(bus.TryGetService<IDisposable>(out var found));
            Assert.Same(stream, found);
            Assert.False(bus.TryGetService<IAsyncDisposable>(out _));
        }

        public sealed class RoutingCommand : ICommand
        {
            public int Value { get; set; }
        }
        public sealed class RoutingCommandWithResult : ICommand<int>
        {
            public int Value { get; set; }
        }
        public sealed class RoutingEvent : IEvent
        {
            public int Value { get; set; }
        }
        public sealed class OtherCommand : ICommand
        {
            public int Value { get; set; }
        }
        public sealed class OtherEvent : IEvent
        {
            public int Value { get; set; }
        }

        public interface IRoutingCommandHandler : ICommandHandler<RoutingCommand>, ICommandHandler<RoutingCommandWithResult, int> { }
        public interface IRoutingEventHandler : IEventHandler<RoutingEvent> { }
        public interface IOtherCommandHandler : ICommandHandler<OtherCommand> { }
        public interface IOtherEventHandler : IEventHandler<OtherEvent> { }
        public interface IRoutingQueryHandler : IQueryHandler
        {
            int Double(int value);
            Task<int> DoubleAsync(int value);
            Task RunAsync(int value);
            Task RunWithCancellationAsync(int value, CancellationToken cancellationToken);
        }
        public interface IOtherQueryHandler : IQueryHandler
        {
            int Other();
        }

        public sealed class RoutingHandler : BaseHandler, IRoutingCommandHandler, IRoutingEventHandler, IRoutingQueryHandler
        {
            public readonly ConcurrentQueue<int> Received = new();

            private Task Handle(int value)
            {
                if (value < 0)
                    throw new InvalidOperationException("Failed");
                Received.Enqueue(value);
                return Task.CompletedTask;
            }

            public async Task Wait(int count)
            {
                for (var i = 0; i < 500 && Received.Count < count; i++)
                    await Task.Delay(10);
            }

            public Task Handle(RoutingCommand command, CancellationToken cancellationToken) => Handle(command.Value);
            public async Task<int> Handle(RoutingCommandWithResult command, CancellationToken cancellationToken)
            {
                await Handle(command.Value);
                return command.Value * 2;
            }
            public Task Handle(RoutingEvent @event) => Handle(@event.Value);

            public int Double(int value) => value < 0 ? throw new InvalidOperationException("Failed") : value * 2;
            public Task<int> DoubleAsync(int value) => Task.FromResult(Double(value));
            public Task RunAsync(int value) => Handle(value);
            public Task RunWithCancellationAsync(int value, CancellationToken cancellationToken) => Handle(value);
        }

        public static class SameNameA
        {
            public sealed class SameCommand : ICommand { }
            public sealed class SameEvent : IEvent { }
            public interface ISameQueryHandler : IQueryHandler { int Get(); }
            public interface ISameCommandHandler : ICommandHandler<SameCommand> { }
            public interface ISameEventHandler : IEventHandler<SameEvent> { }
            public sealed class SameHandler : BaseHandler, ISameQueryHandler, ISameCommandHandler, ISameEventHandler
            {
                public int Get() => 1;
                public Task Handle(SameCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
                public Task Handle(SameEvent @event) => Task.CompletedTask;
            }
        }
        public static class SameNameB
        {
            public sealed class SameCommand : ICommand { }
            public sealed class SameEvent : IEvent { }
            public interface ISameQueryHandler : IQueryHandler { int Get(); }
            public interface ISameCommandHandlerB : ICommandHandler<SameCommand> { }
            public interface ISameEventHandlerB : IEventHandler<SameEvent> { }
            public sealed class SameHandler : BaseHandler, ISameQueryHandler, ISameCommandHandlerB, ISameEventHandlerB
            {
                public int Get() => 2;
                public Task Handle(SameCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
                public Task Handle(SameEvent @event) => Task.CompletedTask;
            }
        }

        public sealed class FailingDisposeConsumer : ICommandConsumer
        {
            public string MessageHost => "failing";
            public void RegisterCommandType(int maxConcurrent, string topic, Type type) { }
            public void Setup(CommandCounter? commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync) { }
            public void Open() { }
            public void Close() { }
            public void Dispose() => throw new InvalidOperationException("Dispose failed");
            public ValueTask DisposeAsync() => throw new InvalidOperationException("Dispose failed");
        }

        public sealed class RecordingBusLogger : IBusLogger
        {
            public readonly ConcurrentQueue<(string Kind, bool Handled, Exception? Exception)> Entries = new();

            public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled) { }
            public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled) { }
            public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled) { }
            public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex) => Entries.Enqueue(("Command", handled, ex));
            public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex) => Entries.Enqueue(("Event", handled, ex));
            public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex) => Entries.Enqueue(("Call", handled, ex));
        }
    }
}
