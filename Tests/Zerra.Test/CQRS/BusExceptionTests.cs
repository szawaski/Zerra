// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Test.CQRS
{
    public class BusExceptionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Local_QueryHandlerThrows_ThrowsCqrsServiceException(bool logged)
        {
            var bus = Bus.New("test-service", null, logged ? new BusTests.TestBusLogger() : null, null);
            bus.AddHandler<IExceptionQueryHandler>(new ExceptionQueryHandler());
            var caller = bus.Call<IExceptionQueryHandler>();

            AssertServiceException(Assert.Throws<CqrsServiceException>(() => caller.Throw()), nameof(InvalidOperationException), "Sync", $"{nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.Throw)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => caller.ThrowTaskAsync()), nameof(InvalidOperationException), "In task", $"{nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.ThrowTaskAsync)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => caller.ThrowAsync()), nameof(InvalidOperationException), "In task", $"{nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.ThrowAsync)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => caller.ThrowTaskBeforeTaskAsync()), nameof(ArgumentException), "Before task", $"{nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.ThrowTaskBeforeTaskAsync)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => caller.ThrowBeforeTaskAsync()), nameof(ArgumentException), "Before task", $"{nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.ThrowBeforeTaskAsync)}");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Local_CommandHandlerThrows_ThrowsCqrsServiceException(bool logged)
        {
            var bus = Bus.New("test-service", null, logged ? new BusTests.TestBusLogger() : null, null);
            bus.AddHandler<IExceptionCommandHandler>(new ExceptionCommandHandler());

            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommand())), nameof(InvalidOperationException), "In task", $"Handle-{nameof(ExceptionCommand)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommand { BeforeTask = true })), nameof(ArgumentException), "Before task", $"Handle-{nameof(ExceptionCommand)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommandWithResult())), nameof(InvalidOperationException), "In task", $"Handle-{nameof(ExceptionCommandWithResult)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommandWithResult { BeforeTask = true })), nameof(ArgumentException), "Before task", $"Handle-{nameof(ExceptionCommandWithResult)}");

            //with a timeout the failure is still the handler's
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommand(), TimeSpan.FromSeconds(10))), nameof(InvalidOperationException), "In task", $"Handle-{nameof(ExceptionCommand)}");
            AssertServiceException(await Assert.ThrowsAsync<CqrsServiceException>(() => bus.DispatchAwaitAsync(new ExceptionCommandWithResult(), TimeSpan.FromSeconds(10))), nameof(InvalidOperationException), "In task", $"Handle-{nameof(ExceptionCommandWithResult)}");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Local_RelayedCqrsServiceException_KeepsErrorType(bool logged)
        {
            var bus = Bus.New("test-service", null, logged ? new BusTests.TestBusLogger() : null, null);
            bus.AddHandler<IExceptionQueryHandler>(new ExceptionQueryHandler());

            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => bus.Call<IExceptionQueryHandler>().RelayAsync());
            AssertServiceException(ex, nameof(KeyNotFoundException), "Relayed", $"IOtherQueryHandler.Get - {nameof(IExceptionQueryHandler)}.{nameof(IExceptionQueryHandler.RelayAsync)}");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Local_CallerCancels_ThrowsOperationCanceledException(bool logged)
        {
            var bus = Bus.New("test-service", null, logged ? new BusTests.TestBusLogger() : null, null);
            bus.AddHandler<IExceptionQueryHandler>(new ExceptionQueryHandler());
            bus.AddHandler<IExceptionCommandHandler>(new ExceptionCommandHandler());

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.Call<IExceptionQueryHandler>().WaitAsync(cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            }
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.DispatchAwaitAsync(new ExceptionCommand { Delay = 10000 }, cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            }
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = bus.DispatchAwaitAsync(new ExceptionCommandWithResult { Delay = 10000 }, cancellationTokenSource.Token);
                cancellationTokenSource.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            }

            //a cancellation the caller didn't ask for is the handler's failure
            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => bus.Call<IExceptionQueryHandler>().CancelItselfAsync());
            Assert.Equal(nameof(OperationCanceledException), ex.ErrorType);
        }

        [Fact]
        public async Task Remote_HandlerThrows_MatchesLocalErrorTypeAndMessage()
        {
            var url = TestNetwork.NewUrl();
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES_GCM);

            var busLocal = Bus.New("test-local", null, null, null);
            busLocal.AddHandler<IExceptionQueryHandler>(new ExceptionQueryHandler());
            busLocal.AddHandler<IExceptionCommandHandler>(new ExceptionCommandHandler());

            var busServer = Bus.New("test-server", null, null, null);
            busServer.AddHandler<IExceptionQueryHandler>(new ExceptionQueryHandler());
            busServer.AddHandler<IExceptionCommandHandler>(new ExceptionCommandHandler());
            var server = new TcpCqrsServer(url, serializer, encryptor, null, null);
            busServer.AddQueryServer<IExceptionQueryHandler>(server);
            busServer.AddCommandConsumer<IExceptionCommandHandler>(server);

            var busClient = Bus.New("test-client", null, null, null);
            var client = new TcpCqrsClient(url, serializer, encryptor, null, null);
            busClient.AddQueryClient<IExceptionQueryHandler>(client);
            busClient.AddCommandProducer<IExceptionCommandHandler>(client);

            try
            {
                var calls = new Func<IBus, Task>[]
                {
                    bus => Task.Run(() => bus.Call<IExceptionQueryHandler>().Throw()),
                    bus => bus.Call<IExceptionQueryHandler>().ThrowTaskAsync(),
                    bus => bus.Call<IExceptionQueryHandler>().ThrowAsync(),
                    bus => bus.Call<IExceptionQueryHandler>().ThrowTaskBeforeTaskAsync(),
                    bus => bus.Call<IExceptionQueryHandler>().ThrowBeforeTaskAsync(),
                    bus => bus.Call<IExceptionQueryHandler>().RelayAsync(),
                    bus => bus.DispatchAwaitAsync(new ExceptionCommand()),
                    bus => bus.DispatchAwaitAsync(new ExceptionCommand { BeforeTask = true }),
                    bus => bus.DispatchAwaitAsync(new ExceptionCommandWithResult()),
                    bus => bus.DispatchAwaitAsync(new ExceptionCommandWithResult { BeforeTask = true }),
                };

                foreach (var call in calls)
                {
                    var local = await Assert.ThrowsAsync<CqrsServiceException>(() => call(busLocal));
                    var remote = await Assert.ThrowsAsync<CqrsServiceException>(() => call(busClient));
                    Assert.Equal(local.ErrorType, remote.ErrorType);
                    Assert.Equal(local.Message, remote.Message);
                }
            }
            finally
            {
                await busClient.StopServicesAsync();
                await busServer.StopServicesAsync();
            }
        }

        private static void AssertServiceException(CqrsServiceException ex, string errorType, string message, string source)
        {
            Assert.Equal(errorType, ex.ErrorType);
            Assert.Equal(message, ex.Message);
            Assert.Equal(source, ex.Source);
            Assert.NotNull(ex.StackTrace);
        }

        public interface IExceptionQueryHandler : IQueryHandler
        {
            public int Throw();
            public Task ThrowTaskAsync();
            public Task<int> ThrowAsync();
            public Task ThrowTaskBeforeTaskAsync();
            public Task<int> ThrowBeforeTaskAsync();
            public Task<int> RelayAsync();
            public Task<int> WaitAsync(CancellationToken cancellationToken);
            public Task<int> CancelItselfAsync();
        }
        public sealed class ExceptionQueryHandler : BaseHandler, IExceptionQueryHandler
        {
            public int Throw() => throw new InvalidOperationException("Sync");

            public async Task ThrowTaskAsync()
            {
                await Task.Yield();
                throw new InvalidOperationException("In task");
            }

            public async Task<int> ThrowAsync()
            {
                await Task.Yield();
                throw new InvalidOperationException("In task");
            }

            //not async, so the exception is thrown before a task is returned
            public Task ThrowTaskBeforeTaskAsync() => throw new ArgumentException("Before task");
            public Task<int> ThrowBeforeTaskAsync() => throw new ArgumentException("Before task");

            public async Task<int> RelayAsync()
            {
                await Task.Yield();
                throw new CqrsServiceException(nameof(KeyNotFoundException), "Relayed", "IOtherQueryHandler.Get", null);
            }

            public async Task<int> WaitAsync(CancellationToken cancellationToken)
            {
                await Task.Delay(10000, cancellationToken);
                return 1;
            }

            public async Task<int> CancelItselfAsync()
            {
                await Task.Yield();
                throw new OperationCanceledException();
            }
        }

        public sealed class ExceptionCommand : ICommand
        {
            public bool BeforeTask { get; set; }
            public int Delay { get; set; }
        }
        public sealed class ExceptionCommandWithResult : ICommand<int>
        {
            public bool BeforeTask { get; set; }
            public int Delay { get; set; }
        }

        public interface IExceptionCommandHandler :
            ICommandHandler<ExceptionCommand>,
            ICommandHandler<ExceptionCommandWithResult, int>
        { }
        public sealed class ExceptionCommandHandler : BaseHandler, IExceptionCommandHandler
        {
            public Task Handle(ExceptionCommand command, CancellationToken cancellationToken)
            {
                if (command.BeforeTask)
                    throw new ArgumentException("Before task");
                return Fail(command.Delay, cancellationToken);
            }

            public Task<int> Handle(ExceptionCommandWithResult command, CancellationToken cancellationToken)
            {
                if (command.BeforeTask)
                    throw new ArgumentException("Before task");
                return FailWithResult(command.Delay, cancellationToken);
            }

            private static async Task Fail(int delay, CancellationToken cancellationToken)
            {
                await Task.Delay(delay, cancellationToken);
                throw new InvalidOperationException("In task");
            }
            private static async Task<int> FailWithResult(int delay, CancellationToken cancellationToken)
            {
                await Task.Delay(delay, cancellationToken);
                throw new InvalidOperationException("In task");
            }
        }
    }
}
