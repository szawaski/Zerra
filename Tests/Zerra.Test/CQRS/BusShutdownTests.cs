using System.Diagnostics;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Test.CQRS
{
    public class BusShutdownTests
    {
        [Fact]
        public async Task StopServicesAsync_FinishesQueryInProgress_Tcp()
        {
            var url = "http://localhost:9011";
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var handler = new ShutdownQueryHandler();
            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<IShutdownQueryHandler>(handler);
            busServer.AddQueryServer<IShutdownQueryHandler>(new TcpCqrsServer(url, serializer, encryptor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<IShutdownQueryHandler>(new TcpCqrsClient(url, serializer, encryptor, null));

            var call = busClient.Call<IShutdownQueryHandler>().Slow(500, TestContext.Current.CancellationToken);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            var stopwatch = Stopwatch.StartNew();
            await busServer.StopServicesAsync();

            Assert.True(handler.Completed, "the query finished before the server stopped");
            Assert.True(stopwatch.ElapsedMilliseconds >= 300, $"stopping waited for the query, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.Equal(500, await call);

            await busClient.StopServicesAsync();
        }

        [Fact]
        public async Task StopServicesAsync_FinishesQueryInProgress_Http()
        {
            var url = "http://localhost:9012";
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var handler = new ShutdownQueryHandler();
            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<IShutdownQueryHandler>(handler);
            busServer.AddQueryServer<IShutdownQueryHandler>(new HttpCqrsServer(url, serializer, encryptor, null, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<IShutdownQueryHandler>(new HttpCqrsClient(url, serializer, encryptor, null, null));

            var call = busClient.Call<IShutdownQueryHandler>().Slow(500, TestContext.Current.CancellationToken);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            await busServer.StopServicesAsync();

            Assert.True(handler.Completed, "the query finished before the server stopped");
            Assert.Equal(500, await call);

            await busClient.StopServicesAsync();
        }

        [Fact]
        public async Task StopServices_FinishesQueryInProgress_Tcp()
        {
            var url = "http://localhost:9013";
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var handler = new ShutdownQueryHandler();
            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<IShutdownQueryHandler>(handler);
            busServer.AddQueryServer<IShutdownQueryHandler>(new TcpCqrsServer(url, serializer, encryptor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddQueryClient<IShutdownQueryHandler>(new TcpCqrsClient(url, serializer, encryptor, null));

            var call = busClient.Call<IShutdownQueryHandler>().Slow(500, TestContext.Current.CancellationToken);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            busServer.StopServices();

            Assert.True(handler.Completed, "the query finished before the server stopped");
            Assert.Equal(500, await call);

            busClient.StopServices();
        }

        [Fact]
        public async Task StopServicesAsync_FinishesFireAndForgetCommand_Tcp()
        {
            var url = "http://localhost:9014";
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var handler = new ShutdownCommandHandler();
            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<IShutdownCommandHandler>(handler);
            busServer.AddCommandConsumer<IShutdownCommandHandler>(new TcpCqrsServer(url, serializer, encryptor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddCommandProducer<IShutdownCommandHandler>(new TcpCqrsClient(url, serializer, encryptor, null));

            //the server acknowledges before the handler runs, so the sender has already moved on
            await busClient.DispatchAsync(new ShutdownCommand() { Delay = 500 });
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            await busServer.StopServicesAsync();

            Assert.True(handler.Completed, "the command finished before the server stopped");
            Assert.False(handler.Cancelled);

            await busClient.StopServicesAsync();
        }

        [Fact]
        public async Task StopServicesAsync_StopsWaitingAfterTheTimeoutWithoutCancelling_Tcp()
        {
            var url = "http://localhost:9015";
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

            var handler = new ShutdownCommandHandler();
            var busServer = Bus.New("test-server", null, null, null, shutdownTimeout: TimeSpan.FromMilliseconds(300));
            busServer.AddHandler<IShutdownCommandHandler>(handler);
            busServer.AddCommandConsumer<IShutdownCommandHandler>(new TcpCqrsServer(url, serializer, encryptor, null));

            var busClient = Bus.New("test-client", null, null, null);
            busClient.AddCommandProducer<IShutdownCommandHandler>(new TcpCqrsClient(url, serializer, encryptor, null));

            await busClient.DispatchAsync(new ShutdownCommand() { Delay = 3000 });
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            var stopwatch = Stopwatch.StartNew();
            await busServer.StopServicesAsync();
            Assert.True(stopwatch.ElapsedMilliseconds < 2500, $"stopping gave up waiting after the timeout, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.False(handler.Finished.Task.IsCompleted, "the handler was still running when stopping gave up waiting");

            //giving up waiting doesn't cancel the handler, it keeps going and finishes
            await handler.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(handler.Cancelled);
            Assert.True(handler.Completed);

            await busClient.StopServicesAsync();
        }

        public interface IShutdownQueryHandler : IQueryHandler
        {
            Task<int> Slow(int delay, CancellationToken cancellationToken);
        }
        public sealed class ShutdownQueryHandler : BaseHandler, IShutdownQueryHandler
        {
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public volatile bool Completed;

            public async Task<int> Slow(int delay, CancellationToken cancellationToken)
            {
                _ = Started.TrySetResult(true);
                await Task.Delay(delay, cancellationToken);
                Completed = true;
                return delay;
            }
        }

        public sealed class ShutdownCommand : ICommand
        {
            public int Delay { get; set; }
        }
        public interface IShutdownCommandHandler : ICommandHandler<ShutdownCommand> { }
        public sealed class ShutdownCommandHandler : BaseHandler, IShutdownCommandHandler
        {
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public volatile bool Completed;
            public volatile bool Cancelled;

            public async Task Handle(ShutdownCommand command, CancellationToken cancellationToken)
            {
                _ = Started.TrySetResult(true);
                try
                {
                    await Task.Delay(command.Delay, cancellationToken);
                    Completed = true;
                }
                catch (OperationCanceledException)
                {
                    Cancelled = true;
                }
                finally
                {
                    _ = Finished.TrySetResult(true);
                }
            }
        }
    }
}
