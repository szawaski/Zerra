// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Serialization.Json;
using Zerra.SourceGeneration;

namespace Zerra.Test.CQRS.Network
{
    public class ApiServerHandlerTest
    {
        public ApiServerHandlerTest()
        {
            RegisterTestTypes();
        }

        private interface ITestProvider
        {
            string GetData();
            int Add(int a, int b);
        }

        private class TestCommand : ICommand
        {
        }

        private class TestCommandWithResult : ICommand<string>
        {
        }

        private class MockBus : IBus
        {
            public RemoteQueryCallResponse? QueryResponse { get; set; }
            public object? CommandResult { get; set; }

            public ILog Log => null;

            public string ServiceName => "Mock";

            public void AddHandler<TInterface>(TInterface handler) where TInterface : notnull
            {
                throw new NotImplementedException();
            }

            public void AddCommandProducer<TInterface>(ICommandProducer commandProducer) => throw new NotImplementedException();
            public void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer) => throw new NotImplementedException();
            public void AddEventProducer<TInterface>(IEventProducer eventProducer) => throw new NotImplementedException();
            public void AddEventConsumer<TInterface>(IEventConsumer eventConsumer) => throw new NotImplementedException();
            public void AddQueryClient<TInterface>(IQueryClient queryClient) => throw new NotImplementedException();
            public void AddQueryServer<TInterface>(IQueryServer queryServer) => throw new NotImplementedException();

            public Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, string?[] arguments, string source, bool isApi, ISerializer serializer, CancellationToken cancellationToken)
            {
                return Task.FromResult(QueryResponse ?? new RemoteQueryCallResponse(null));
            }

            public Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            {
                return Task.FromResult(CommandResult);
            }

            public Task RemoteHandleEventDispatchAsync(IEvent @event, string source, bool isApi)
            {
                return Task.CompletedTask;
            }

            public TInterface Call<TInterface>() where TInterface : notnull => throw new NotImplementedException();
            public Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null) where TResult : notnull => throw new NotImplementedException();
            public void StopServices() => throw new NotImplementedException();
            public Task StopServicesAsync() => throw new NotImplementedException();
            public void WaitForExit(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task WaitForExitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public TInterface GetService<TInterface>() where TInterface : notnull => throw new NotImplementedException();
            public bool TryGetService<TInterface>([MaybeNullWhen(false)] out TInterface? instance) where TInterface : notnull => throw new NotImplementedException();
        }

        private static void RegisterTestTypes()
        {
            // Register types so TypeHelper can resolve them by name
            TypeHelper.Register(typeof(ITestProvider));
            TypeHelper.Register(typeof(TestCommand));
            TypeHelper.Register(typeof(TestCommandWithResult));
        }

        [Fact]
        public async Task HandleRequestAsync_WithNullData_ReturnsNull()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            var data = new ApiRequestData();

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderType_CallsRemoteQueryCall()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            bus.QueryResponse = new RemoteQueryCallResponse("test response");

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderTypeReturningStream_ReturnsStreamResponse()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            var testStream = new MemoryStream(new byte[] { 1, 2, 3 });
            bus.QueryResponse = new RemoteQueryCallResponse(testStream);

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result.Stream);
            testStream.Dispose();
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderTypeReturningModel_ReturnsSerializedResponse()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            bus.QueryResponse = new RemoteQueryCallResponse("test model");

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithMessageType_DispatchesCommand()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var commandJson = JsonSerializer.Serialize(new TestCommand());
            var data = new ApiRequestData
            {
                MessageType = typeof(TestCommand).AssemblyQualifiedName,
                MessageData = commandJson,
                MessageAwait = false,
                MessageResult = false,
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithMessageTypeAndAwait_DispatchesCommandWithAwait()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var commandJson = JsonSerializer.Serialize(new TestCommand());
            var data = new ApiRequestData
            {
                MessageType = typeof(TestCommand).AssemblyQualifiedName,
                MessageData = commandJson,
                MessageAwait = true,
                MessageResult = false,
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithMessageTypeAndResult_DispatchesCommandWithResult()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            bus.CommandResult = "test result";

            var commandJson = JsonSerializer.Serialize(new TestCommandWithResult());
            var data = new ApiRequestData
            {
                MessageType = typeof(TestCommandWithResult).AssemblyQualifiedName,
                MessageData = commandJson,
                MessageAwait = false,
                MessageResult = true,
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderTypeAndNullProviderMethod_ThrowsArgumentNullException()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = null,
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None));
        }

        [Fact]
        public async Task HandleRequestAsync_WithMessageTypeAndNullMessageData_ThrowsArgumentNullException()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                MessageType = typeof(TestCommand).AssemblyQualifiedName,
                MessageData = null,
                MessageAwait = false,
                MessageResult = false,
                Source = "TestSource"
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None));
        }

        [Fact]
        public async Task HandleRequestAsync_WithCancellationToken_PassesTokenToBus()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            var cts = new CancellationTokenSource();

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, cts.Token);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithEmptyProviderArguments_ReturnsResponse()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();
            bus.QueryResponse = new RemoteQueryCallResponse("result");

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderArgumentsNull_ThrowsArgumentNullException()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = null,
                Source = "TestSource"
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None));
        }

        [Fact]
        public async Task HandleRequestAsync_WithSourceNull_ThrowsArgumentNullException()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = null
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None));
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderTypeEmpty_ReturnsNull()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = string.Empty,
                MessageType = null,
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithMessageTypeEmpty_ReturnsNull()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = null,
                MessageType = string.Empty,
                Source = "TestSource"
            };

            var result = await ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_WithProviderTypeAndNullSource_ThrowsArgumentNullException()
        {
            var bus = new MockBus();
            var serializer = new ZerraJsonSerializer();

            var data = new ApiRequestData
            {
                ProviderType = typeof(ITestProvider).AssemblyQualifiedName,
                ProviderMethod = "GetData",
                ProviderArguments = Array.Empty<string>(),
                Source = null
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ApiServerHandler.HandleRequestAsync(bus, serializer, data, CancellationToken.None));
        }
    }
}
