// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.Serialization;

namespace Zerra.Test.CQRS
{
    public class BusContextTests
    {
        private interface ITestDependency { }
        private class TestDependency : ITestDependency { }

        private interface IFoo { }
        private interface IBar { }
        private class Foo : IFoo { }
        private class Bar : IBar { }

        private class MockBus : IBus
        {
            public void AddHandler<TInterface>(TInterface handler) where TInterface : notnull => throw new NotImplementedException();
            public void AddCommandProducer<TInterface>(ICommandProducer commandProducer) => throw new NotImplementedException();
            public void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer) => throw new NotImplementedException();
            public void AddEventProducer<TInterface>(IEventProducer eventProducer) => throw new NotImplementedException();
            public void AddEventConsumer<TInterface>(IEventConsumer eventConsumer) => throw new NotImplementedException();
            public void AddQueryClient<TInterface>(IQueryClient queryClient) => throw new NotImplementedException();
            public void AddQueryServer<TInterface>(IQueryServer queryServer) => throw new NotImplementedException();
            public Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, string?[] arguments, string source, bool isApi, ISerializer serializer, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task RemoteHandleEventDispatchAsync(IEvent @event, string source, bool isApi) => throw new NotImplementedException();
            public TInterface Call<TInterface>() where TInterface : notnull => throw new NotImplementedException();
            public Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null) where TResult : notnull => throw new NotImplementedException();
            public void StopServices() => throw new NotImplementedException();
            public Task StopServicesAsync() => throw new NotImplementedException();
            public void WaitForExit(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task WaitForExitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        [Fact]
        public void BusContext_Properties_AreSet()
        {
            var busScopes = new BusScopes();
            var mockBus = new MockBus();
            var serviceName = "TestService";

            var busContext = new BusContext(mockBus, serviceName, null, busScopes);

            Assert.Same(mockBus, busContext.Bus);
            Assert.Equal(serviceName, busContext.Service);
            Assert.Null(busContext.Log);
        }

        [Fact]
        public void BusContext_WithNullLogger_LogIsNull()
        {
            var busScopes = new BusScopes();
            var mockBus = new MockBus();

            var busContext = new BusContext(mockBus, "Service", null, busScopes);

            Assert.Null(busContext.Log);
        }

        [Fact]
        public void BusContext_Get_WithRegisteredDependency()
        {
            var busScopes = new BusScopes();
            var dependency = new TestDependency();
            busScopes.AddScope<ITestDependency>(dependency);
            var mockBus = new MockBus();

            var busContext = new BusContext(mockBus, "Service", null, busScopes);
            var retrieved = busContext.Get<ITestDependency>();

            Assert.Same(dependency, retrieved);
        }

        [Fact]
        public void BusContext_Get_WithUnregisteredDependency_ThrowsArgumentException()
        {
            var busScopes = new BusScopes();
            var mockBus = new MockBus();

            var busContext = new BusContext(mockBus, "Service", null, busScopes);

            var ex = Assert.Throws<ArgumentException>(() => busContext.Get<ITestDependency>());
            Assert.Contains(typeof(ITestDependency).FullName, ex.Message);
        }

        [Fact]
        public void BusContext_Get_WithNullBusScopes()
        {
            var mockBus = new MockBus();

            var busContext = new BusContext(mockBus, "Service", null, null!);

            var ex = Assert.Throws<ArgumentException>(() => busContext.Get<ITestDependency>());
            Assert.Contains(typeof(ITestDependency).FullName, ex.Message);
        }

        [Fact]
        public void BusContext_WithMultipleDependencies_CanRetrieveEach()
        {
            var busScopes = new BusScopes();
            var foo = new Foo();
            var bar = new Bar();
            busScopes.AddScope<IFoo>(foo);
            busScopes.AddScope<IBar>(bar);

            var mockBus = new MockBus();
            var busContext = new BusContext(mockBus, "Service", null, busScopes);

            var retrievedFoo = busContext.Get<IFoo>();
            var retrievedBar = busContext.Get<IBar>();

            Assert.Same(foo, retrievedFoo);
            Assert.Same(bar, retrievedBar);
        }
    }
}
