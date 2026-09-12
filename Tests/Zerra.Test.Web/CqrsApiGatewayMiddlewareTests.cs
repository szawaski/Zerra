// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class CqrsApiGatewayMiddlewareTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsModel()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21));

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(typeof(ITestQueryHandler), bus.QueryInterfaceType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), bus.QueryMethodName);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsStream_DisposesStream()
        {
            //a stream from a remote service holds a pooled connection that is only released on dispose
            var resultStream = new DisposeSignalStream([1, 2, 3]);
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(resultStream) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetStream)));

            await middleware.Invoke(context);

            Assert.Equal([1, 2, 3], ReadResponse(context));
            Assert.True(resultStream.Disposed);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_HandlerThrows_RespondsWithError()
        {
            var bus = new MockBus { QueryException = new InvalidOperationException("query failed") };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21));

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context));
            Assert.Equal("query failed", exception.Message);
        }

        //UseMiddleware picks a constructor by argument type and a null route matches none, a null route falls back to the constructor default and serves every path
        [Fact(Timeout = timeout)]
        public async Task UseCqrsApiGateway_NullRoute_ServesAnyPath()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var builder = new ApplicationBuilder(new ServiceCollection().AddSingleton<IBus>(bus).AddSingleton(serializer).BuildServiceProvider());
            _ = builder.UseCqrsApiGateway(null);
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21));
            context.Request.Path = "/any/path";

            await app(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task UseCqrsApiGateway_DefaultRoute_OtherPathCallsNext()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var builder = new ApplicationBuilder(new ServiceCollection().AddSingleton<IBus>(bus).AddSingleton(serializer).BuildServiceProvider());
            _ = builder.UseCqrsApiGateway();
            var nextInvoked = false;
            _ = builder.Use(next => context => { nextInvoked = true; return Task.CompletedTask; });
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21));
            context.Request.Path = "/other";

            await app(context);

            Assert.True(nextInvoked);
            Assert.Null(bus.QueryInterfaceType);
        }

        private static ApiRequestData QueryRequest(string methodName, params object[] arguments) => new()
        {
            ProviderType = typeof(ITestQueryHandler).AssemblyQualifiedName,
            ProviderMethod = methodName,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = source
        };

        //what ApiClient sends
        private static DefaultHttpContext CreateContext(ApiRequestData data)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.ContentType = HttpCommon.ContentTypeBytes;
            context.Request.Body = new MemoryStream(serializer.SerializeBytes(data));
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static byte[] ReadResponse(HttpContext context) => ((MemoryStream)context.Response.Body).ToArray();

        private sealed class DisposeSignalStream(byte[] data) : MemoryStream(data)
        {
            public bool Disposed { get; private set; }
            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        //Stands in for the bus the gateway dispatches to, only queries are used
        private sealed class MockBus : IBus
        {
            public RemoteQueryCallResponse? QueryResponse { get; set; }
            public Exception? QueryException { get; set; }
            public Type? QueryInterfaceType { get; private set; }
            public string? QueryMethodName { get; private set; }

            public ILogger? Log => null;
            public string ServiceName => "Mock";

            public Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, byte[]?[] arguments, string source, ISerializer serializer, CancellationToken cancellationToken)
            {
                QueryInterfaceType = interfaceType;
                QueryMethodName = methodName;
                if (QueryException is not null)
                    return Task.FromException<RemoteQueryCallResponse>(QueryException);
                return Task.FromResult(QueryResponse ?? new RemoteQueryCallResponse(null));
            }

            public Task RemoteHandleCommandDispatchAsync(ICommand command, string source, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task RemoteHandleEventDispatchAsync(IEvent @event, string source) => throw new NotImplementedException();

            public void AddHandler<TInterface>(TInterface handler) where TInterface : notnull => throw new NotImplementedException();
            public void AddCommandProducer<TInterface>(ICommandProducer commandProducer) => throw new NotImplementedException();
            public void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer) => throw new NotImplementedException();
            public void AddEventProducer<TInterface>(IEventProducer eventProducer) => throw new NotImplementedException();
            public void AddEventConsumer<TInterface>(IEventConsumer eventConsumer) => throw new NotImplementedException();
            public void AddQueryClient<TInterface>(IQueryClient queryClient) => throw new NotImplementedException();
            public void AddQueryServer<TInterface>(IQueryServer queryServer) => throw new NotImplementedException();
            public TInterface Call<TInterface>() where TInterface : notnull => throw new NotImplementedException();
            public Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null) => throw new NotImplementedException();
            public Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null) where TResult : notnull => throw new NotImplementedException();
            public Task DispatchAsync(ICommand command, TimeSpan timeout) => throw new NotImplementedException();
            public Task DispatchAwaitAsync(ICommand command, TimeSpan timeout) => throw new NotImplementedException();
            public Task DispatchAsync(IEvent @event, TimeSpan timeout) => throw new NotImplementedException();
            public Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, TimeSpan timeout) where TResult : notnull => throw new NotImplementedException();
            public void StopServices() => throw new NotImplementedException();
            public Task StopServicesAsync() => throw new NotImplementedException();
            public void WaitForExit(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task WaitForExitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public TInterface GetService<TInterface>() where TInterface : notnull => throw new NotImplementedException();
            public bool TryGetService<TInterface>([NotNullWhen(true)] out TInterface? instance) where TInterface : notnull => throw new NotImplementedException();
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
        }
    }
}
