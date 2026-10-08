using System.Collections.Concurrent;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.IO;
using Zerra.Serialization;
using Zerra.Test.Compression;

namespace Zerra.Test.CQRS
{
    public class BusTests
    {
        [Fact]
        public async Task Bus_Call()
        {
            var bus = Bus.New("test-service", null, null, null);
            bus.AddHandler<ITestQueryHandler>(new TestQueryHandler());

            await BusCalls(bus, "test-service", TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Bus_Dispatch()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var bus = Bus.New("test-service", null, null, null);
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            bus.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));

            await BusDispatches(bus, waiter, results, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Bus_Dispatch_WithTimeout_ThrowsHandlerException()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var bus = Bus.New("test-service", null, null, null);
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));

            //a failure that isn't the timeout is thrown as it is, not swallowed or wrapped
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.DispatchAwaitAsync(new TestCommand { Thing = 30, Fail = true }, TimeSpan.FromSeconds(10)));
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 31, Fail = true }, TimeSpan.FromSeconds(10)));

            //and a result comes back through the timeout
            Assert.Equal(64, await bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 32 }, TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task Bus_Dispatch_WithDefaultTimeout_ThrowsHandlerException()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var bus = Bus.New("test-service", null, null, null, defaultDispatchAwaitTimeout: TimeSpan.FromSeconds(10));
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.DispatchAwaitAsync(new TestCommand { Thing = 33, Fail = true }));
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 34, Fail = true }));

            var timedOut = bus.DispatchAwaitAsync(new TestCommand { Thing = 35, Delay = 200 }, TimeSpan.FromMilliseconds(50));
            _ = await Assert.ThrowsAsync<TimeoutException>(() => timedOut);
        }

        [Fact]
        public async Task Bus_Dispatch_WithBusLogger()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();
            var busLogger = new TestBusLogger();

            var bus = Bus.New("test-service", null, busLogger, null);
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            bus.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));

            //bus logging must not change whether the caller waits for a local handler
            await BusDispatches(bus, waiter, results, TestContext.Current.CancellationToken);

            Assert.True(busLogger.CommandsEnded > 0);
            Assert.True(busLogger.EventsEnded > 0);
        }

        [Fact]
        public async Task BusQueryClientServerTcp()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new TcpCqrsServer(url, serializer, encryptor, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new TcpCqrsClient(url, serializer, encryptor, null, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Fact]
        public async Task BusQueryClientServerHttp()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new HttpCqrsServer(url, serializer, encryptor, null, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new HttpCqrsClient(url, serializer, encryptor, null, null, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Fact]
        public async Task BusQueryClientServerTcpUnencrypted()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new TcpCqrsServer(url, serializer, null, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new TcpCqrsClient(url, serializer, null, null, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Fact]
        public async Task BusQueryClientServerHttpUnencrypted()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new HttpCqrsServer(url, serializer, null, null, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new HttpCqrsClient(url, serializer, null, null, null, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task BusQueryClientServerTcpCompressed(bool encrypt, bool exactEnd)
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = encrypt ? new ZerraEncryptor("test", SymmetricAlgorithmType.AES) : null;
            ICompressor compressor = exactEnd ? new ExactEndCompressor() : new ZerraCompressor(CompressionAlgorithmType.Brotli);

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new TcpCqrsServer(url, serializer, encryptor, compressor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new TcpCqrsClient(url, serializer, encryptor, compressor, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task BusQueryClientServerHttpCompressed(bool encrypt, bool exactEnd)
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = encrypt ? new ZerraEncryptor("test", SymmetricAlgorithmType.AES) : null;
            ICompressor compressor = exactEnd ? new ExactEndCompressor() : new ZerraCompressor(CompressionAlgorithmType.GZip);

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestQueryHandler>(new TestQueryHandler());
            busServer.AddQueryServer<ITestQueryHandler>(new HttpCqrsServer(url, serializer, encryptor, compressor, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new HttpCqrsClient(url, serializer, encryptor, compressor, null, null));

            await BusCalls(busClient, "test-server", TestContext.Current.CancellationToken);

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task BusProducerConsumerCompressed(bool encrypt, bool http, bool exactEnd)
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = encrypt ? new ZerraEncryptor("test", SymmetricAlgorithmType.AES) : null;
            ICompressor compressor = exactEnd ? new ExactEndCompressor() : new ZerraCompressor(CompressionAlgorithmType.Deflate);

            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            busServer.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));
            var server = http ? (CqrsServerBase)new HttpCqrsServer(url, serializer, encryptor, compressor, null, null) : new TcpCqrsServer(url, serializer, encryptor, compressor, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("test-client", null, null, null);
            var client = http ? (CqrsClientBase)new HttpCqrsClient(url, serializer, encryptor, compressor, null, null) : new TcpCqrsClient(url, serializer, encryptor, compressor, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task BusProducerConsumerTcp()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            busServer.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));
            var server = new TcpCqrsServer(url, serializer, encryptor, null, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("test-client", null, null, null);
            var client = new TcpCqrsClient(url, serializer, encryptor, null, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task BusProducerConsumerTcp_WithBusLogger()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            busServer.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));
            var server = new TcpCqrsServer(url, serializer, encryptor, null, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            //the sender logs, and an event with one producer goes straight to it
            var busLogger = new TestBusLogger();
            var busClient = Bus.New("test-client", null, busLogger, null);
            var client = new TcpCqrsClient(url, serializer, encryptor, null, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results, TestContext.Current.CancellationToken);

            Assert.True(busLogger.CommandsEnded > 0);
            Assert.True(busLogger.EventsEnded > 0);
        }

        [Fact]
        public async Task Bus_Dispatch_WithBusLogger_TimesTheHandler()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var busLogger = new TestBusLogger();

            var bus = Bus.New("test-service", null, busLogger, null);
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(new List<int>(), waiter));

            await bus.DispatchAwaitAsync(new TestCommand { Thing = 40, Delay = 100 });

            Assert.InRange(busLogger.LastCommandMilliseconds, 90, 10000);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Bus_OneConsumer_SeveralInterfaces(bool eventFirst)
        {
            var consumer = new SetupOnceConsumer();
            var bus = Bus.New("test-service", null, null, null);
            if (eventFirst)
                bus.AddEventConsumer<ITestEventHandler>(consumer, EventConsumerMode.PerReplica);
            bus.AddCommandConsumer<ITestCommandHandler>(consumer);
            bus.AddCommandConsumer<ISecondTestCommandHandler>(consumer);
            if (!eventFirst)
                bus.AddEventConsumer<ITestEventHandler>(consumer, EventConsumerMode.PerReplica);

            Assert.Equal(1, consumer.CommandSetups);
            Assert.Equal(1, consumer.EventSetups);
            Assert.Equal(new[] { typeof(TestCommand), typeof(TestCommandWithResult), typeof(SecondTestCommand) }.OrderBy(x => x.Name), consumer.CommandTypes.OrderBy(x => x.Name));
            Assert.Equal(2, consumer.CommandTopics.Distinct().Count());
            Assert.Equal(new[] { typeof(TestEvent) }, consumer.EventTypes);

            await bus.StopServicesAsync();
        }

        //sustained traffic between two buses: every message is handled once, each awaited command gets its own result,
        //and the server never runs more handlers at once than its limit, which its commands and events share
        [Theory(Timeout = 120000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BusProducerConsumer_SustainedLoad(bool http)
        {
            const int maxConcurrent = 10;
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();

            var handler = new LoadHandler();
            var busServer = Bus.New("load-server", null, null, null, maxConcurrentCommandsPerTopic: maxConcurrent, maxConcurrentEventsPerTopic: maxConcurrent);
            busServer.AddHandler<ILoadCommandHandler>(handler);
            busServer.AddHandler<ILoadEventHandler>(handler);
            var server = http ? (CqrsServerBase)new HttpCqrsServer(url, serializer, null, null, null, null) : new TcpCqrsServer(url, serializer, null, null, null);
            busServer.AddCommandConsumer<ILoadCommandHandler>(server);
            busServer.AddEventConsumer<ILoadEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("load-client", null, null, null);
            var client = http ? (CqrsClientBase)new HttpCqrsClient(url, serializer, null, null, null, null) : new TcpCqrsClient(url, serializer, null, null, null);
            busClient.AddCommandProducer<ILoadCommandHandler>(client);
            busClient.AddEventProducer<ILoadEventHandler>(client);

            try
            {
                //the server's one throttle is shared by its commands and events
                await SustainedLoad(busClient, handler, maxConcurrent, TestContext.Current.CancellationToken);
            }
            finally
            {
                await busServer.StopServicesAsync();
                await busClient.StopServicesAsync();
            }
        }

        //sustained queries between two buses: each call gets its own result, streamed response or error back, and the server never runs more queries at once than its limit
        [Theory(Timeout = 120000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BusQueries_SustainedLoad(bool http)
        {
            const int maxConcurrent = 10;
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();

            var handler = new LoadQueryHandler();
            var busServer = Bus.New("query-load-server", null, null, null, maxConcurrentQueries: maxConcurrent);
            busServer.AddHandler<ILoadQueryHandler>(handler);
            busServer.AddQueryServer<ILoadQueryHandler>(http ? new HttpCqrsServer(url, serializer, null, null, null, null) : new TcpCqrsServer(url, serializer, null, null, null));

            var busClient = Bus.New("query-load-client", null, null, null);
            busClient.AddQueryClient<ILoadQueryHandler>(http ? new HttpCqrsClient(url, serializer, null, null, null, null) : new TcpCqrsClient(url, serializer, null, null, null));

            try
            {
                var doubling = Enumerable.Range(0, 1000).Select(x => busClient.Call<ILoadQueryHandler>().DoubleAsync(x, TestContext.Current.CancellationToken)).ToArray();
                var streaming = Enumerable.Range(0, 200).Select(async x =>
                {
                    await using var stream = await busClient.Call<ILoadQueryHandler>().BytesAsync(x);
                    return await stream.ToArrayAsync();
                }).ToArray();
                var throwing = Enumerable.Range(0, 100).Select(async x =>
                {
                    try
                    {
                        _ = await busClient.Call<ILoadQueryHandler>().ThrowAsync(x);
                        return null;
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }).ToArray();

                var doubled = await Task.WhenAll(doubling);
                var streamed = await Task.WhenAll(streaming);
                var thrown = await Task.WhenAll(throwing);

                for (var i = 0; i < doubled.Length; i++)
                    Assert.Equal(i * 2, doubled[i]);
                for (var i = 0; i < streamed.Length; i++)
                    Assert.Equal(Enumerable.Repeat((byte)i, 1000 + i), streamed[i]);
                for (var i = 0; i < thrown.Length; i++)
                    Assert.Equal($"Failed {i}", thrown[i]);

                Assert.Equal(1300, handler.Calls);
                Assert.InRange(handler.MaxHandling, 2, maxConcurrent);
            }
            finally
            {
                await busServer.StopServicesAsync();
                await busClient.StopServicesAsync();
            }
        }

        public static async Task SustainedLoad(IBus busClient, LoadHandler handler, int maxHandling, CancellationToken cancellationToken)
        {
            var commands = Enumerable.Range(0, 1000).Select(x => new LoadCommand() { ID = Guid.NewGuid() }).ToArray();
            var commandsWithResult = Enumerable.Range(0, 200).Select(x => new LoadCommandWithResult() { ID = Guid.NewGuid(), Value = x }).ToArray();
            var events = Enumerable.Range(0, 500).Select(x => new LoadEvent() { ID = Guid.NewGuid() }).ToArray();

            var sendingCommands = Task.WhenAll(commands.Select(x => busClient.DispatchAsync(x)));
            var sendingEvents = Task.WhenAll(events.Select(x => busClient.DispatchAsync(x)));
            var results = await Task.WhenAll(commandsWithResult.Select(x => busClient.DispatchAwaitAsync(x)));
            await Task.WhenAll(sendingCommands, sendingEvents);

            for (var i = 0; i < commandsWithResult.Length; i++)
                Assert.Equal(commandsWithResult[i].Value * 2, results[i]);

            var ids = commands.Select(x => x.ID).Concat(commandsWithResult.Select(x => x.ID)).Concat(events.Select(x => x.ID)).ToArray();
            while (ids.Count(handler.Received.ContainsKey) < ids.Length)
                await Task.Delay(10, cancellationToken);
            await Task.Delay(500, cancellationToken);

            Assert.All(ids, id => Assert.Equal(1, handler.Received[id]));
            Assert.Equal(ids.Length, handler.Received.Count);
            Assert.InRange(handler.MaxHandling, 2, maxHandling);
        }

        //the exit is the process's and happens once, after it every WaitForExit returns at once, so no other test may reach a bus consumer's count
        [Fact(Timeout = 60000)]
        public async Task WaitForExit_ReturnsOnceLimitedConsumerHandledItsCount()
        {
            const int count = 3;
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();

            var handler = new LoadHandler();
            var busServer = Bus.New("exit-server", null, null, null);
            busServer.AddHandler<ILoadCommandHandler>(handler);
            busServer.AddCommandConsumer<ILoadCommandHandler>(new TcpCqrsServer(url, serializer, null, null, null), count);

            var busClient = Bus.New("exit-client", null, null, null);
            busClient.AddCommandProducer<ILoadCommandHandler>(new TcpCqrsClient(url, serializer, null, null, null));

            try
            {
                var exiting = busServer.WaitForExitAsync(TestContext.Current.CancellationToken);
                for (var i = 0; i < count; i++)
                {
                    Assert.False(exiting.IsCompleted);
                    await busClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() });
                }

                await exiting.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
                Assert.Equal(count, handler.Received.Count);

                //the server was stopped
                _ = await Assert.ThrowsAnyAsync<Exception>(() => busClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() }, TimeSpan.FromSeconds(5)));
                Assert.Equal(count, handler.Received.Count);

                //waiting after the exit was signalled, as when a consumer reaches its count before the service gets to wait, still stops the services
                var lateUrl = TestNetwork.NewUrl();
                var lateHandler = new LoadHandler();
                var lateBusServer = Bus.New("late-exit-server", null, null, null);
                lateBusServer.AddHandler<ILoadCommandHandler>(lateHandler);
                lateBusServer.AddCommandConsumer<ILoadCommandHandler>(new TcpCqrsServer(lateUrl, serializer, null, null, null));
                var lateBusClient = Bus.New("late-exit-client", null, null, null);
                lateBusClient.AddCommandProducer<ILoadCommandHandler>(new TcpCqrsClient(lateUrl, serializer, null, null, null));
                try
                {
                    await lateBusClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() });
                    await lateBusServer.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
                    _ = await Assert.ThrowsAnyAsync<Exception>(() => lateBusClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() }, TimeSpan.FromSeconds(5)));
                    _ = Assert.Single(lateHandler.Received);
                }
                finally
                {
                    await lateBusClient.StopServicesAsync();
                }

                var syncUrl = TestNetwork.NewUrl();
                var syncBusServer = Bus.New("sync-exit-server", null, null, null);
                syncBusServer.AddHandler<ILoadCommandHandler>(new LoadHandler());
                syncBusServer.AddCommandConsumer<ILoadCommandHandler>(new TcpCqrsServer(syncUrl, serializer, null, null, null));
                var syncBusClient = Bus.New("sync-exit-client", null, null, null);
                syncBusClient.AddCommandProducer<ILoadCommandHandler>(new TcpCqrsClient(syncUrl, serializer, null, null, null));
                try
                {
                    await syncBusClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() });
                    syncBusServer.WaitForExit(TestContext.Current.CancellationToken);
                    _ = await Assert.ThrowsAnyAsync<Exception>(() => syncBusClient.DispatchAwaitAsync(new LoadCommand() { ID = Guid.NewGuid() }, TimeSpan.FromSeconds(1)));
                }
                finally
                {
                    await syncBusClient.StopServicesAsync();
                }
            }
            finally
            {
                await busClient.StopServicesAsync();
            }
        }

        [Fact]
        public async Task Bus_CommandConsumerWithLimit_GetsItsOwnCounter()
        {
            var limited = new SetupOnceConsumer();
            var unlimited = new SetupOnceConsumer();
            var bus = Bus.New("test-service", null, null, null);
            bus.AddCommandConsumer<ITestCommandHandler>(limited, 2);
            bus.AddCommandConsumer<ISecondTestCommandHandler>(unlimited);

            Assert.NotNull(limited.CommandCounter);
            Assert.Equal(2, limited.CommandCounter.ReceiveCountBeforeExit);
            Assert.Null(unlimited.CommandCounter);

            await bus.StopServicesAsync();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task Bus_CommandConsumerWithLimit_OneInterfaceOnly(bool firstLimited, bool secondLimited)
        {
            var consumer = new SetupOnceConsumer();
            var bus = Bus.New("test-service", null, null, null);
            bus.AddCommandConsumer<ITestCommandHandler>(consumer, firstLimited ? 1 : null);

            _ = Assert.Throws<InvalidOperationException>(() => bus.AddCommandConsumer<ISecondTestCommandHandler>(consumer, secondLimited ? 1 : null));
            Assert.Equal(1, consumer.CommandSetups);

            await bus.StopServicesAsync();
        }

        [Fact]
        public void Bus_CommandConsumerWithLimit_LessThanOneThrows()
        {
            var bus = Bus.New("test-service", null, null, null);
            _ = Assert.Throws<ArgumentException>(() => bus.AddCommandConsumer<ITestCommandHandler>(new SetupOnceConsumer(), 0));
        }

        [Fact]
        public async Task Bus_OneQueryServer_SeveralInterfaces()
        {
            var server = new SetupCountingQueryServer();
            var bus = Bus.New("test-service", null, null, null);
            bus.AddQueryServer<ITestQueryHandler>(server);
            bus.AddQueryServer<ISecondTestQueryHandler>(server);

            Assert.Equal(1, server.Setups);
            Assert.Equal(new[] { typeof(ITestQueryHandler), typeof(ISecondTestQueryHandler) }, server.InterfaceTypes);

            await bus.StopServicesAsync();
        }

        [Fact]
        public async Task BusProducerConsumerHttp()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            busServer.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));
            var server = new HttpCqrsServer(url, serializer, encryptor, null, null, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("test-client", null, null, null);
            var client = new HttpCqrsClient(url, serializer, encryptor, null, null, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task BusMultipleEventProducersTcp()
        {
            var url1 = TestNetwork.NewUrl();
            var url2 = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            using var waiter1 = new SemaphoreSlim(0, 1);
            var results1 = new List<int>();
            var busServer1 = Bus.New("test-server1", null, null, null);
            busServer1.AddHandler<ITestEventHandler>(new TestEventHandler(results1, waiter1));
            busServer1.AddEventConsumer<ITestEventHandler>(new TcpCqrsServer(url1, serializer, encryptor, null, null), EventConsumerMode.PerReplica);

            using var waiter2 = new SemaphoreSlim(0, 1);
            var results2 = new List<int>();
            var busServer2 = Bus.New("test-server2", null, null, null);
            busServer2.AddHandler<ITestEventHandler>(new TestEventHandler(results2, waiter2));
            busServer2.AddEventConsumer<ITestEventHandler>(new TcpCqrsServer(url2, serializer, encryptor, null, null), EventConsumerMode.PerReplica);

            //each producer is sent the event, adding the same producer again is ignored so it isn't sent twice
            var busClient = Bus.New("test-client", null, null, null);
            var client1 = new TcpCqrsClient(url1, serializer, encryptor, null, null);
            busClient.AddEventProducer<ITestEventHandler>(client1);
            busClient.AddEventProducer<ITestEventHandler>(client1);
            busClient.AddEventProducer<ITestEventHandler>(new TcpCqrsClient(url2, serializer, encryptor, null, null));

            await busClient.DispatchAsync(new TestEvent { Thing = 41 });
            Assert.True(await waiter1.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(await waiter2.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal([41], results1);
            Assert.Equal([41], results2);

            await Task.Delay(200, TestContext.Current.CancellationToken);
            Assert.Single(results1);

            await busClient.StopServicesAsync();
            await busServer1.StopServicesAsync();
            await busServer2.StopServicesAsync();
        }

        private static async Task BusCalls(IBus bus, string serviceName, CancellationToken cancellationToken)
        {
            var service = bus.Call<ITestQueryHandler>().GetServiceName();
            Assert.Equal(serviceName, service);

            var things = bus.Call<ITestQueryHandler>().GetThings();
            Assert.Equal(42, things);

            var thingsAsync = await bus.Call<ITestQueryHandler>().GetThingsAsync();
            Assert.Equal(42, thingsAsync);

            var thingsWithParam = bus.Call<ITestQueryHandler>().GetThingsWithParam(21);
            Assert.Equal(42, thingsWithParam);

            var thingsWithParamAsync = await bus.Call<ITestQueryHandler>().GetThingsWithParamAsync(21);
            Assert.Equal(42, thingsWithParamAsync);

            var thingsWithCancellation = bus.Call<ITestQueryHandler>().GetThingsWithCancellation(21, cancellationToken);
            Assert.Equal(42, thingsWithCancellation);

            var thingsWithCancellationAsync = await bus.Call<ITestQueryHandler>().GetThingsWithCancellationAsync(21, cancellationToken);
            Assert.Equal(42, thingsWithCancellationAsync);

            var stream = bus.Call<ITestQueryHandler>().GetStream();
            var streamBytes = stream.ToArray();
            stream.Dispose();
            Assert.True(streamBytes.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }));

            var streamAsync = await bus.Call<ITestQueryHandler>().GetStreamAsync();
            var streamAsyncBytes = await streamAsync.ToArrayAsync();
            await streamAsync.DisposeAsync();
            Assert.True(streamAsyncBytes.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }));

            var uploadBytes = new byte[1024 * 1024 + 7];
            new Random(5).NextBytes(uploadBytes);

            var uploadCount = bus.Call<ITestQueryHandler>().Upload(3, new MemoryStream(uploadBytes));
            Assert.Equal(uploadBytes.Length + 3, uploadCount);

            var uploadSum = await bus.Call<ITestQueryHandler>().UploadAsync(new MemoryStream(uploadBytes), 3, cancellationToken);
            Assert.Equal(uploadBytes.Sum(x => (long)x) + 3, uploadSum);

            //the request data spans several segments before the stream
            var largeArgument = new string('x', 40_000);
            var uploadLarge = await bus.Call<ITestQueryHandler>().UploadWithArgumentAsync(largeArgument, new MemoryStream(uploadBytes));
            Assert.Equal(largeArgument.Length + uploadBytes.Length, uploadLarge);

            var uploadNull = await bus.Call<ITestQueryHandler>().UploadAsync(null, 3, cancellationToken);
            Assert.Equal(-1, uploadNull);

            //the handler reads part of the stream, the next call on the connection still works
            for (var i = 0; i < 3; i++)
            {
                var firstByte = await bus.Call<ITestQueryHandler>().UploadReadFirstAsync(new MemoryStream(uploadBytes));
                Assert.Equal(uploadBytes[0], firstByte);
            }

            using (var uploadEcho = await bus.Call<ITestQueryHandler>().UploadEchoAsync(new MemoryStream(uploadBytes)))
            {
                var uploadEchoBytes = await uploadEcho.ToArrayAsync();
                Assert.True(uploadEchoBytes.SequenceEqual(uploadBytes));
            }

            _ = await Assert.ThrowsAnyAsync<Exception>(async () => await bus.Call<ITestQueryHandler>().UploadThrowsAsync(new MemoryStream(uploadBytes)));
            Assert.Equal(42, await bus.Call<ITestQueryHandler>().GetThingsAsync());

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.Call<ITestQueryHandler>().GetThingsWithCancellationAsync(21, cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
            }
        }

        private async Task BusDispatches(IBus bus, SemaphoreSlim waiter, List<int> results, CancellationToken cancellationToken)
        {
            await bus.DispatchAsync(new TestCommand { Thing = 21, Delay = 50 });
            Assert.DoesNotContain(21, results);
            await waiter.WaitAsync(cancellationToken);
            Assert.Contains(21, results);

            await bus.DispatchAwaitAsync(new TestCommand { Thing = 22, Delay = 50 });
            Assert.Contains(22, results);
            await waiter.WaitAsync(cancellationToken);

            var result = await bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 23, Delay = 50 });
            Assert.Equal(46, result);
            Assert.Contains(46, results);

            await bus.DispatchAsync(new TestEvent { Thing = 31 });
            Assert.DoesNotContain(31, results);
            await waiter.WaitAsync(cancellationToken);
            Assert.Contains(31, results);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.DispatchAwaitAsync(new TestCommand { Thing = 24, Delay = 50 }, cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        throw ex.GetBaseException();
                    }
                });
            }

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 25, Delay = 50 }, cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        throw ex.GetBaseException();
                    }
                });
            }

            {
                var task = bus.DispatchAwaitAsync(new TestCommand { Thing = 26, Delay = 200 }, TimeSpan.FromMilliseconds(100));
                _ = await Assert.ThrowsAnyAsync<TimeoutException>(async () =>
                {
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        throw ex.GetBaseException();
                    }
                });
            }

            {
                var task = bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 27, Delay = 200 }, TimeSpan.FromMilliseconds(100));
                _ = await Assert.ThrowsAnyAsync<TimeoutException>(async () =>
                {
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        throw ex.GetBaseException();
                    }
                });
            }
        }


        public interface ITestQueryHandler : IQueryHandler
        {
            public string GetServiceName();
            public int GetThings();
            public Task<int> GetThingsAsync();
            public int GetThingsWithParam(int param);
            public Task<int> GetThingsWithParamAsync(int param);
            public int GetThingsWithCancellation(int param, CancellationToken cancellationToken);
            public Task<int> GetThingsWithCancellationAsync(int param, CancellationToken cancellationToken);
            public Stream GetStream();
            public Task<Stream> GetStreamAsync();
            public int Upload(int param, Stream stream);
            public Task<long> UploadAsync(Stream? stream, int param, CancellationToken cancellationToken);
            public Task<int> UploadWithArgumentAsync(string argument, Stream stream);
            public Task<int> UploadReadFirstAsync(Stream stream);
            public Task<Stream> UploadEchoAsync(Stream stream);
            public Task<int> UploadThrowsAsync(Stream stream);
        }
        public sealed class TestQueryHandler : BaseHandler, ITestQueryHandler
        {
            public string GetServiceName()
            {
                return Context.ServiceName;
            }

            public int GetThings()
            {
                return 42;
            }

            public Task<int> GetThingsAsync()
            {
                return Task.FromResult(42);
            }

            public int GetThingsWithParam(int param)
            {
                return param * 2;
            }

            public Task<int> GetThingsWithParamAsync(int param)
            {
                return Task.FromResult(param * 2);
            }

            public int GetThingsWithCancellation(int param, CancellationToken cancellationToken)
            {
                Task.Delay(50, cancellationToken).Wait(cancellationToken);
                return param * 2;
            }

            public async Task<int> GetThingsWithCancellationAsync(int param, CancellationToken cancellationToken)
            {
                await Task.Delay(50, cancellationToken);
                return param * 2;
            }

            public Stream GetStream()
            {
                var ms = new MemoryStream([1, 2, 3, 4, 5]);
                return ms;
            }

            public Task<Stream> GetStreamAsync()
            {
                var ms = new MemoryStream([1, 2, 3, 4, 5]);
                return Task.FromResult<Stream>(ms);
            }

            public int Upload(int param, Stream stream)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return (int)ms.Length + param;
            }

            public async Task<long> UploadAsync(Stream? stream, int param, CancellationToken cancellationToken)
            {
                if (stream is null)
                    return -1;
                var buffer = new byte[4096];
                long sum = param;
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    for (var i = 0; i < read; i++)
                        sum += buffer[i];
                }
                return sum;
            }

            public async Task<int> UploadWithArgumentAsync(string argument, Stream stream)
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                return argument.Length + (int)ms.Length;
            }

            public Task<int> UploadReadFirstAsync(Stream stream)
            {
                //disposing it like a StreamReader would leaves the rest for the server to read
                using (stream)
                    return Task.FromResult(stream.ReadByte());
            }

            public async Task<Stream> UploadEchoAsync(Stream stream)
            {
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }

            public Task<int> UploadThrowsAsync(Stream stream)
            {
                _ = stream.ReadByte();
                throw new InvalidOperationException("Upload failed");
            }
        }

        public sealed class TestCommand : ICommand
        {
            public int Thing { get; set; }
            public int Delay { get; set; }
            public bool Fail { get; set; }
        }
        public sealed class TestCommandWithResult : ICommand<int>
        {
            public int Thing { get; set; }
            public int Delay { get; set; }
            public bool Fail { get; set; }
        }

        public interface ITestCommandHandler :
            ICommandHandler<TestCommand>,
            ICommandHandler<TestCommandWithResult, int>
        { }

        public sealed class TestCommandHandler : BaseHandler, ITestCommandHandler
        {
            private readonly List<int> results;
            private readonly SemaphoreSlim waiter;
            public TestCommandHandler(List<int> results, SemaphoreSlim waiter)
            {
                this.results = results;
                this.waiter = waiter;
            }

            public async Task Handle(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Delay(command.Delay, cancellationToken);
                if (command.Fail)
                    throw new InvalidOperationException("Test failure");
                results.Add(command.Thing);
                _ = waiter.Release();
            }

            public async Task<int> Handle(TestCommandWithResult command, CancellationToken cancellationToken)
            {
                await Task.Delay(command.Delay, cancellationToken);
                if (command.Fail)
                    throw new InvalidOperationException("Test failure");
                results.Add(command.Thing * 2);
                return command.Thing * 2;
            }
        }

        public interface ISecondTestQueryHandler : IQueryHandler
        {
            public int GetOtherThings();
        }

        public sealed class SetupCountingQueryServer : IQueryServer
        {
            public int Setups;
            public readonly List<Type> InterfaceTypes = new();

            string IQueryServer.ServiceUrl => "test";

            void IQueryServer.Setup(QueryHandlerDelegate providerHandlerAsync) => Setups++;
            void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type) => InterfaceTypes.Add(type);
            void IQueryServer.Open() { }
            void IQueryServer.Close() { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;
        }

        public interface ILoadQueryHandler : IQueryHandler
        {
            public Task<int> DoubleAsync(int value, CancellationToken cancellationToken);
            public Task<Stream> BytesAsync(int value);
            public Task<int> ThrowAsync(int value);
        }

        //counts the calls and the most handlers running at once, each taking a moment so they overlap
        public sealed class LoadQueryHandler : BaseHandler, ILoadQueryHandler
        {
            private int calls;
            private int handling;
            private int maxHandling;
            public int Calls => Volatile.Read(ref calls);
            public int MaxHandling => Volatile.Read(ref maxHandling);

            private async Task Handle()
            {
                _ = Interlocked.Increment(ref calls);
                var now = Interlocked.Increment(ref handling);
                for (var current = Volatile.Read(ref maxHandling); now > current; current = Volatile.Read(ref maxHandling))
                {
                    if (Interlocked.CompareExchange(ref maxHandling, now, current) == current)
                        break;
                }
                try
                {
                    await Task.Delay(1);
                }
                finally
                {
                    _ = Interlocked.Decrement(ref handling);
                }
            }

            public async Task<int> DoubleAsync(int value, CancellationToken cancellationToken)
            {
                await Handle();
                return value * 2;
            }
            public async Task<Stream> BytesAsync(int value)
            {
                await Handle();
                return new MemoryStream(Enumerable.Repeat((byte)value, 1000 + value).ToArray());
            }
            public async Task<int> ThrowAsync(int value)
            {
                await Handle();
                throw new InvalidOperationException($"Failed {value}");
            }
        }

        public sealed class LoadCommand : ICommand
        {
            public Guid ID { get; set; }
        }
        public sealed class LoadCommandWithResult : ICommand<int>
        {
            public Guid ID { get; set; }
            public int Value { get; set; }
        }
        public sealed class LoadEvent : IEvent
        {
            public Guid ID { get; set; }
        }

        public interface ILoadCommandHandler :
            ICommandHandler<LoadCommand>,
            ICommandHandler<LoadCommandWithResult, int>
        { }
        public interface ILoadEventHandler :
            IEventHandler<LoadEvent>
        { }

        //counts how many times each message is handled and the most handlers running at once, each taking a moment so they overlap
        public sealed class LoadHandler : BaseHandler, ILoadCommandHandler, ILoadEventHandler
        {
            public readonly ConcurrentDictionary<Guid, int> Received = new();
            private int handling;
            private int maxHandling;
            public int MaxHandling => Volatile.Read(ref maxHandling);

            private async Task Handle(Guid id)
            {
                _ = Received.AddOrUpdate(id, 1, static (_, times) => times + 1);
                var now = Interlocked.Increment(ref handling);
                for (var current = Volatile.Read(ref maxHandling); now > current; current = Volatile.Read(ref maxHandling))
                {
                    if (Interlocked.CompareExchange(ref maxHandling, now, current) == current)
                        break;
                }
                try
                {
                    await Task.Delay(1);
                }
                finally
                {
                    _ = Interlocked.Decrement(ref handling);
                }
            }

            public Task Handle(LoadCommand command, CancellationToken cancellationToken) => Handle(command.ID);
            public async Task<int> Handle(LoadCommandWithResult command, CancellationToken cancellationToken)
            {
                await Handle(command.ID);
                return command.Value * 2;
            }
            public Task Handle(LoadEvent @event) => Handle(@event.ID);
        }

        public sealed class SecondTestCommand : ICommand
        {
            public int Thing { get; set; }
        }

        public interface ISecondTestCommandHandler :
            ICommandHandler<SecondTestCommand>
        { }

        public sealed class SetupOnceConsumer : ICommandConsumer, IEventConsumer
        {
            public int CommandSetups;
            public int EventSetups;
            public CommandCounter? CommandCounter;
            public readonly List<Type> CommandTypes = new();
            public readonly List<string> CommandTopics = new();
            public readonly List<Type> EventTypes = new();

            string ICommandConsumer.MessageHost => "test";
            string IEventConsumer.MessageHost => "test";

            void ICommandConsumer.Setup(CommandCounter? commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (++CommandSetups > 1)
                    throw new InvalidOperationException("Command consumer already setup");
                CommandCounter = commandCounter;
            }
            void IEventConsumer.Setup(string serviceName, HandleRemoteEventDispatch handlerAsync)
            {
                if (++EventSetups > 1)
                    throw new InvalidOperationException("Event consumer already setup");
            }

            void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
            {
                CommandTypes.Add(type);
                CommandTopics.Add(topic);
            }
            void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type, EventConsumerMode eventConsumerMode) => EventTypes.Add(type);

            void ICommandConsumer.Open() { }
            void IEventConsumer.Open() { }
            void ICommandConsumer.Close() { }
            void IEventConsumer.Close() { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;
        }

        public sealed class TestEvent : IEvent
        {
            public int Thing { get; set; }
        }

        public interface ITestEventHandler :
            IEventHandler<TestEvent>
        { }

        public sealed class TestEventHandler : BaseHandler, ITestEventHandler
        {
            private readonly List<int> results;
            private readonly SemaphoreSlim waiter;
            public TestEventHandler(List<int> results, SemaphoreSlim waiter)
            {
                this.results = results;
                this.waiter = waiter;
            }

            public async Task Handle(TestEvent @event)
            {
                await Task.Delay(50);
                results.Add(@event.Thing);
                _ = waiter.Release();
            }
        }

        public sealed class TestBusLogger : IBusLogger
        {
            private int commandsEnded;
            private int eventsEnded;
            public int CommandsEnded => commandsEnded;
            public int EventsEnded => eventsEnded;
            public long LastCommandMilliseconds { get; private set; }

            public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled) { }
            public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled) { }
            public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled) { }
            public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
            {
                LastCommandMilliseconds = milliseconds;
                _ = Interlocked.Increment(ref commandsEnded);
            }
            public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex) => Interlocked.Increment(ref eventsEnded);
            public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex) { }
        }
    }
}
