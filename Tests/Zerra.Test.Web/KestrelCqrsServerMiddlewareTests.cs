// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class KestrelCqrsServerMiddlewareTests
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
            Type? receivedType = null;
            string? receivedMethod = null;
            int? receivedArgument = null;
            using var middleware = CreateMiddleware(enc, query: (interfaceType, methodName, arguments, _, _, _) =>
            {
                receivedType = interfaceType;
                receivedMethod = methodName;
                receivedArgument = serializer.Deserialize<int>(arguments[0]);
                return Task.FromResult(new RemoteQueryCallResponse(42));
            });
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), enc);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(typeof(ITestQueryHandler), receivedType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), receivedMethod);
            Assert.Equal(21, receivedArgument);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context, enc)));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsStream_DisposesStream()
        {
            var resultStream = new DisposeSignalStream([1, 2, 3]);
            using var middleware = CreateMiddleware(null, query: (_, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(resultStream)));
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetStream)), null);

            await middleware.Invoke(context);

            Assert.Equal([1, 2, 3], ReadResponse(context, null));
            Assert.True(resultStream.Disposed); //the handler's stream is released once it's sent, such as a file handle
        }

        [Fact(Timeout = timeout)]
        public async Task Query_HandlerThrows_RespondsWithError()
        {
            using var middleware = CreateMiddleware(null, query: (_, _, _, _, _, _) =>
                Task.FromException<RemoteQueryCallResponse>(new InvalidOperationException("query failed")));
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context, null));
            Assert.Equal("query failed", exception.Message);
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_OriginAllowed_InvokesHandler()
        {
            var handlerInvoked = false;
            using var middleware = CreateMiddleware(null, allowOrigins: ["allowed.example.com"], query: (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(42));
            });
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null, origin: "allowed.example.com");

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.True(handlerInvoked);
            Assert.Equal("allowed.example.com", context.Response.Headers.AccessControlAllowOrigin);
        }

        [Theory(Timeout = timeout)]
        [InlineData("other.example.com")]
        [InlineData(null)]
        public async Task Query_OriginNotAllowed_Returns401(string? origin)
        {
            var handlerInvoked = false;
            using var middleware = CreateMiddleware(null, allowOrigins: ["allowed.example.com"], query: (_, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(42));
            });
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null, origin: origin);

            await middleware.Invoke(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.False(handlerInvoked);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Command_InvokesHandlerWithConfiguredSerializer(bool messageAwait)
        {
            ICommand? received = null;
            using var middleware = CreateMiddleware(null,
                command: (command, _, _) => { received = command; return Task.CompletedTask; },
                commandAwait: (command, _, _) => { received = command; return Task.CompletedTask; });
            //the command bytes come from the configured serializer, not JSON
            var context = CreateContext(MessageRequest(new TestCommand { Value = 5 }, messageAwait, false), null);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(5, Assert.IsType<TestCommand>(received).Value);
            Assert.Empty(ReadResponse(context, null));
            Assert.Equal(typeof(TestCommand).AssemblyQualifiedName, context.Response.Headers[HttpCommon.ProviderTypeHeader]);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CommandWithResult_ReturnsResultWithSingleHeaders(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var middleware = CreateMiddleware(enc, commandWithResult: (command, _, _) =>
                Task.FromResult<object?>(((TestCommandWithResult)command).Value * 2));
            var context = CreateContext(MessageRequest(new TestCommandWithResult { Value = 21 }, true, true), enc);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context, enc)));
            //browsers reject a response with more than one allowed origin
            Assert.Single(context.Response.Headers.AccessControlAllowOrigin);
            Assert.Single(context.Response.Headers.ContentType);
        }

        [Fact(Timeout = timeout)]
        public async Task Event_InvokesEventHandler()
        {
            IEvent? received = null;
            using var middleware = CreateMiddleware(null, @event: (@event, _) => { received = @event; return Task.CompletedTask; });
            var context = CreateContext(MessageRequest(new TestEvent { Value = 7 }, false, false), null);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(7, Assert.IsType<TestEvent>(received).Value);
        }

        [Theory(Timeout = timeout)]
        [InlineData("app.example.com", "https://App.Example.com")] //an allowed host matches the browser's full origin
        [InlineData("https://app.example.com", "https://APP.example.com")]
        [InlineData("https://app.example.com:8443", "https://app.example.com:8443")]
        [InlineData("LocalHost", "localhost")] //what KestrelCqrsClient sends
        public async Task Query_OriginMatchesAllowedOriginOrHost(string allowOrigin, string origin)
        {
            using var middleware = CreateMiddleware(null, allowOrigins: [allowOrigin], query: (_, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(42)));
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null, origin: origin);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Theory(Timeout = timeout)]
        [InlineData("https://a.example.com")] //allowed by host
        [InlineData("https://b.example.com")] //allowed by full origin
        public async Task Preflight_AllowedOrigin_EchoesOnlyThatOrigin(string origin)
        {
            using var middleware = CreateMiddleware(null, allowOrigins: ["a.example.com", "https://b.example.com"]);
            var context = CreatePreflightContext(origin);

            await middleware.Invoke(context);

            //browsers reject a list of origins so only the request's origin comes back
            Assert.Equal(origin, Assert.Single(context.Response.Headers.AccessControlAllowOrigin));
        }

        [Fact(Timeout = timeout)]
        public async Task Preflight_OriginNotAllowed_HasNoAllowOrigin()
        {
            using var middleware = CreateMiddleware(null, allowOrigins: ["a.example.com", "b.example.com"]);
            var context = CreatePreflightContext("https://other.example.com");

            await middleware.Invoke(context);

            Assert.Equal(0, context.Response.Headers.AccessControlAllowOrigin.Count);
        }

        [Fact(Timeout = timeout)]
        public async Task Preflight_WithoutAllowOrigins_AllowsAll()
        {
            using var middleware = CreateMiddleware(null);
            var context = CreatePreflightContext("https://other.example.com");

            await middleware.Invoke(context);

            Assert.Equal("*", Assert.Single(context.Response.Headers.AccessControlAllowOrigin));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_CanceledWhileWaitingForThrottle_DoesNotReleaseIt()
        {
            var firstStarted = new TaskCompletionSource();
            var releaseFirst = new TaskCompletionSource();
            var settings = CreateSettings(null, null, async (_, _, _, _, _, _) =>
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
                return new RemoteQueryCallResponse(42);
            }, null, null, null, null);
            using var throttle = new SemaphoreSlim(1, 1);
            settings.Types[typeof(ITestQueryHandler)].Dispose();
            settings.Types[typeof(ITestQueryHandler)] = throttle;
            var middleware = new KestrelCqrsServerMiddleware(_ => Task.CompletedTask, serializer, null, null, settings);

            //the first request holds the only slot
            var first = middleware.Invoke(CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null));
            await firstStarted.Task;

            //the second gives up while waiting for it
            using var cts = new CancellationTokenSource();
            var secondContext = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null);
            secondContext.RequestAborted = cts.Token;
            var second = middleware.Invoke(secondContext);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            cts.Cancel();
            await second;

            //releasing a slot it never took would let another request past the limit
            Assert.Equal(0, throttle.CurrentCount);

            releaseFirst.SetResult();
            await first;
            Assert.Equal(1, throttle.CurrentCount);
        }

        [Fact(Timeout = timeout)]
        public async Task OtherRoute_CallsNext()
        {
            var nextInvoked = false;
            var settings = CreateSettings("/cqrs", null, null, null, null, null, null);
            using var middleware = new KestrelCqrsServerMiddleware(_ => { nextInvoked = true; return Task.CompletedTask; }, serializer, null, null, settings);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null);
            context.Request.Path = "/other";

            await middleware.Invoke(context);

            Assert.True(nextInvoked);
        }

        //UseMiddleware picks a constructor by argument type and a null encryptor or log matches none, the extension builds the middleware itself
        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseKestrelCqrsServer_NullLog_ServesQuery(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var settings = CreateSettings(null, null, (_, _, arguments, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(serializer.Deserialize<int>(arguments[0]) * 2)), null, null, null, null);
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            _ = builder.UseKestrelCqrsServer(serializer, enc, null, settings);
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), enc);

            await app(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context, enc)));
        }

        [Fact(Timeout = timeout)]
        public async Task UseKestrelCqrsServer_OtherRoute_CallsNext()
        {
            using var settings = CreateSettings("/cqrs", null, null, null, null, null, null);
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
            _ = builder.UseKestrelCqrsServer(serializer, null, null, settings);
            var nextInvoked = false;
            _ = builder.Use(next => context => { nextInvoked = true; return Task.CompletedTask; });
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), null);
            context.Request.Path = "/other";

            await app(context);

            Assert.True(nextInvoked);
        }

        private static KestrelCqrsServerMiddleware CreateMiddleware(IEncryptor? encryptor, string[]? allowOrigins = null, QueryHandlerDelegate? query = null,
            HandleRemoteCommandDispatch? command = null, HandleRemoteCommandDispatch? commandAwait = null, HandleRemoteCommandWithResultDispatch? commandWithResult = null, HandleRemoteEventDispatch? @event = null)
        {
            var settings = CreateSettings(null, allowOrigins, query, command, commandAwait, commandWithResult, @event);
            return new KestrelCqrsServerMiddleware(_ => Task.CompletedTask, serializer, encryptor, null, settings);
        }

        private static KestrelCqrsServerLinkedSettings CreateSettings(string? route, string[]? allowOrigins, QueryHandlerDelegate? query,
            HandleRemoteCommandDispatch? command, HandleRemoteCommandDispatch? commandAwait, HandleRemoteCommandWithResultDispatch? commandWithResult, HandleRemoteEventDispatch? @event)
        {
            var settings = new KestrelCqrsServerLinkedSettings(route, null, serializer.ContentType) { AllowOrigins = allowOrigins };

            IQueryServer queryServer = new KestrelCqrsServerQueryServer(settings);
            queryServer.Setup(new CommandCounter(), query ?? ((_, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(null))));
            queryServer.RegisterInterfaceType(10, typeof(ITestQueryHandler));

            ICommandConsumer commandConsumer = new KestrelCqrsServerCommandConsumer(settings);
            commandConsumer.Setup(new CommandCounter(),
                command ?? ((_, _, _) => Task.CompletedTask),
                commandAwait ?? ((_, _, _) => Task.CompletedTask),
                commandWithResult ?? ((_, _, _) => Task.FromResult<object?>(null)));
            commandConsumer.RegisterCommandType(10, "test", typeof(TestCommand));
            commandConsumer.RegisterCommandType(10, "test", typeof(TestCommandWithResult));

            IEventConsumer eventConsumer = new KestrelCqrsServerEventConsumer(settings);
            eventConsumer.Setup(@event ?? ((_, _) => Task.CompletedTask));
            eventConsumer.RegisterEventType(10, "test", typeof(TestEvent));

            return settings;
        }

        private static CqrsRequestData QueryRequest(string methodName, params object[] arguments) => new()
        {
            ProviderType = typeof(ITestQueryHandler).AssemblyQualifiedName,
            ProviderMethod = methodName,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = source
        };

        private static CqrsRequestData MessageRequest(object message, bool messageAwait, bool messageResult) => new()
        {
            MessageType = message.GetType().AssemblyQualifiedName,
            MessageData = serializer.SerializeBytes(message, message.GetType()),
            MessageAwait = messageAwait,
            MessageResult = messageResult,
            Source = source
        };

        //what KestrelCqrsClient sends
        private static DefaultHttpContext CreateContext(CqrsRequestData data, IEncryptor? encryptor, string? origin = null)
        {
            var body = new MemoryStream();
            if (encryptor is not null)
            {
                var cryptoStream = encryptor.Encrypt(body, true);
                serializer.Serialize(cryptoStream, data);
                cryptoStream.FlushFinalBlock();
                cryptoStream.Dispose();
            }
            else
            {
                serializer.Serialize(body, data);
            }

            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.ContentType = HttpCommon.ContentTypeBytes;
            context.Request.Headers[HttpCommon.ProviderTypeHeader] = data.ProviderType ?? data.MessageType;
            if (origin is not null)
                context.Request.Headers.Origin = origin;
            context.Request.Body = new MemoryStream(body.ToArray());
            context.Response.Body = new MemoryStream();
            return context;
        }

        //what a browser sends before a cross origin request
        private static DefaultHttpContext CreatePreflightContext(string origin)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "OPTIONS";
            context.Request.Headers.Origin = origin;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static byte[] ReadResponse(HttpContext context, IEncryptor? encryptor)
        {
            var body = new MemoryStream(((MemoryStream)context.Response.Body).ToArray());
            Stream stream = encryptor is not null ? encryptor.Decrypt(body, false) : body;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private sealed class DisposeSignalStream(byte[] data) : MemoryStream(data)
        {
            public bool Disposed { get; private set; }
            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
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
