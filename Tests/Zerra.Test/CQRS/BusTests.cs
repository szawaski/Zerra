using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.IO;
using Zerra.Serialization;

namespace Zerra.Test.CQRS
{
    public class BusTests
    {
        [Fact]
        public async Task Bus_Call()
        {
            var bus = Bus.New("test-service", null, null, null);
            bus.AddHandler<ITestQueryHandler>(new TestQueryHandler());

            await BusCalls(bus, "test-service");
        }

        [Fact]
        public async Task Bus_Dispatch()
        {
            using var waiter = new SemaphoreSlim(0, 1);
            var results = new List<int>();

            var bus = Bus.New("test-service", null, null, null);
            bus.AddHandler<ITestCommandHandler>(new TestCommandHandler(results, waiter));
            bus.AddHandler<ITestEventHandler>(new TestEventHandler(results, waiter));

            await BusDispatches(bus, waiter, results);
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
            await BusDispatches(bus, waiter, results);

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
            busServer.AddQueryServer<ITestQueryHandler>(new TcpCqrsServer(url, serializer, encryptor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new TcpCqrsClient(url, serializer, encryptor, null));

            await BusCalls(busClient, "test-server");

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
            busServer.AddQueryServer<ITestQueryHandler>(new HttpCqrsServer(url, serializer, encryptor, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new HttpCqrsClient(url, serializer, encryptor, null, null));

            await BusCalls(busClient, "test-server");

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
            busServer.AddQueryServer<ITestQueryHandler>(new TcpCqrsServer(url, serializer, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new TcpCqrsClient(url, serializer, null, null));

            await BusCalls(busClient, "test-server");

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
            busServer.AddQueryServer<ITestQueryHandler>(new HttpCqrsServer(url, serializer, null, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<ITestQueryHandler>(new HttpCqrsClient(url, serializer, null, null, null));

            await BusCalls(busClient, "test-server");

            await busClient.StopServicesAsync();
            await busServer.StopServicesAsync();
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
            var server = new TcpCqrsServer(url, serializer, encryptor, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("test-client", null, null, null);
            var client = new TcpCqrsClient(url, serializer, encryptor, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results);
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
            var server = new TcpCqrsServer(url, serializer, encryptor, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            //the sender logs, and an event with one producer goes straight to it
            var busLogger = new TestBusLogger();
            var busClient = Bus.New("test-client", null, busLogger, null);
            var client = new TcpCqrsClient(url, serializer, encryptor, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results);

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
            var server = new HttpCqrsServer(url, serializer, encryptor, null, null);
            busServer.AddCommandConsumer<ITestCommandHandler>(server);
            busServer.AddEventConsumer<ITestEventHandler>(server, EventConsumerMode.PerReplica);

            var busClient = Bus.New("test-client", null, null, null);
            var client = new HttpCqrsClient(url, serializer, encryptor, null, null);
            busClient.AddCommandProducer<ITestCommandHandler>(client);
            busClient.AddEventProducer<ITestEventHandler>(client);

            await BusDispatches(busClient, waiter, results);
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
            busServer1.AddEventConsumer<ITestEventHandler>(new TcpCqrsServer(url1, serializer, encryptor, null), EventConsumerMode.PerReplica);

            using var waiter2 = new SemaphoreSlim(0, 1);
            var results2 = new List<int>();
            var busServer2 = Bus.New("test-server2", null, null, null);
            busServer2.AddHandler<ITestEventHandler>(new TestEventHandler(results2, waiter2));
            busServer2.AddEventConsumer<ITestEventHandler>(new TcpCqrsServer(url2, serializer, encryptor, null), EventConsumerMode.PerReplica);

            //each producer is sent the event, adding the same producer again is ignored so it isn't sent twice
            var busClient = Bus.New("test-client", null, null, null);
            var client1 = new TcpCqrsClient(url1, serializer, encryptor, null);
            busClient.AddEventProducer<ITestEventHandler>(client1);
            busClient.AddEventProducer<ITestEventHandler>(client1);
            busClient.AddEventProducer<ITestEventHandler>(new TcpCqrsClient(url2, serializer, encryptor, null));

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

        private static async Task BusCalls(IBus bus, string serviceName)
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

            var thingsWithCancellation = bus.Call<ITestQueryHandler>().GetThingsWithCancellation(21, default);
            Assert.Equal(42, thingsWithCancellation);

            var thingsWithCancellationAsync = await bus.Call<ITestQueryHandler>().GetThingsWithCancellationAsync(21, default);
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

            var uploadSum = await bus.Call<ITestQueryHandler>().UploadAsync(new MemoryStream(uploadBytes), 3, default);
            Assert.Equal(uploadBytes.Sum(x => (long)x) + 3, uploadSum);

            //the request data spans several segments before the stream
            var largeArgument = new string('x', 40_000);
            var uploadLarge = await bus.Call<ITestQueryHandler>().UploadWithArgumentAsync(largeArgument, new MemoryStream(uploadBytes));
            Assert.Equal(largeArgument.Length + uploadBytes.Length, uploadLarge);

            var uploadNull = await bus.Call<ITestQueryHandler>().UploadAsync(null, 3, default);
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

        private async Task BusDispatches(IBus bus, SemaphoreSlim waiter, List<int> results)
        {
            await bus.DispatchAsync(new TestCommand { Thing = 21, Delay = 50 });
            Assert.DoesNotContain(21, results);
            await waiter.WaitAsync();
            Assert.Contains(21, results);

            await bus.DispatchAwaitAsync(new TestCommand { Thing = 22, Delay = 50 });
            Assert.Contains(22, results);
            await waiter.WaitAsync();

            var result = await bus.DispatchAwaitAsync(new TestCommandWithResult { Thing = 23, Delay = 50 });
            Assert.Equal(46, result);
            Assert.Contains(46, results);

            await bus.DispatchAsync(new TestEvent { Thing = 31 });
            Assert.DoesNotContain(31, results);
            await waiter.WaitAsync();
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
