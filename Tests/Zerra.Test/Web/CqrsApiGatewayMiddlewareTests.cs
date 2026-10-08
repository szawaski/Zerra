// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(typeof(ITestQueryHandler), bus.QueryInterfaceType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), bus.QueryMethodName);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
            //serialized straight to the response, so the length isn't known up front
            Assert.Equal("application/octet-stream", context.Response.ContentType);
            Assert.Null(context.Response.ContentLength);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_Upload_PassesStream()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            //{int32 little endian length}{data} segments, {int32 0}, then the stream bytes
            var dataBytes = ((MemoryStream)context.Request.Body).ToArray();
            var body = new MemoryStream();
            body.Write(BitConverter.GetBytes(dataBytes.Length));
            body.Write(dataBytes);
            body.Write(BitConverter.GetBytes(0));
            body.Write([5, 6, 7, 8]);
            body.Position = 0;
            context.Request.Body = body;
            context.Request.Headers["Upload-Stream"] = "true";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), bus.QueryMethodName);
            Assert.Equal([5, 6, 7, 8], bus.QueryArgumentStreamBytes);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsStream_DisposesStream()
        {
            //a stream from a remote service holds a pooled connection that is only released on dispose
            var resultStream = new DisposeSignalStream([1, 2, 3]);
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(resultStream) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetStream)), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal([1, 2, 3], ReadResponse(context));
            Assert.True(resultStream.Disposed);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_HandlerThrows_RespondsWithError()
        {
            var bus = new MockBus { QueryException = new InvalidOperationException("query failed") };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context));
            Assert.Equal("query failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_RemoteServiceThrows_RespondsWithOriginalType()
        {
            //the error came from a handler in another service, the browser sees the type it was thrown as
            var bus = new MockBus { QueryException = new RemoteServiceException(nameof(InvalidOperationException), "remote failed", "remote-service", null) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context));
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);
            Assert.Equal("remote failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_RemoteServiceThrowsSecurityException_RespondsUnauthorized()
        {
            var bus = new MockBus { QueryException = new RemoteServiceException(nameof(System.Security.SecurityException), "not yours", "remote-service", null) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(401, context.Response.StatusCode);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context));
            Assert.Equal(nameof(System.Security.SecurityException), exception.ErrorType);
        }

        [Fact(Timeout = timeout)]
        public async Task Authorizer_ThrowsSecurityException_RespondsUnauthorized()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, authorizer: new RejectingAuthorizer());
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
            var exception = ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context));
            Assert.Equal("not allowed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_AcceptJsonNameless_RespondsNameless()
        {
            var jsonSerializer = new ZerraJsonSerializer();
            var namelessSerializer = new ZerraJsonSerializer(new Zerra.Serialization.Json.JsonSerializerOptions() { Nameless = true });
            var model = new TestModel { Id = 7, Name = "Seven" };
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(model) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, jsonSerializer);

            var context = new DefaultHttpContext();
            context.RequestAborted = TestContext.Current.CancellationToken;
            context.Request.Method = "POST";
            context.Request.ContentType = "application/json; charset=utf-8";
            context.Request.Headers.Accept = "application/jsonnameless; charset=utf-8";
            context.Request.Body = new MemoryStream(jsonSerializer.SerializeBytes(new ApiRequestData()
            {
                ProviderType = typeof(ITestQueryHandler).Name,
                ProviderMethod = nameof(ITestQueryHandler.GetModel),
                ProviderArguments = [],
                Source = source
            }));
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.StartsWith("application/jsonnameless", context.Response.ContentType);
            var result = namelessSerializer.Deserialize<TestModel>(ReadResponse(context));
            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
            Assert.Equal("Seven", result.Name);
        }

        [Fact(Timeout = timeout)]
        public async Task ContentTypeMissing_RespondsBadRequest()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.ContentType = null;

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task ContentTypeNotMatchingSerializer_RespondsBadRequest()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.ContentType = "application/json";

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task AcceptNotSupported_RespondsBadRequest()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Accept = "application/json";

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task NoAllowOrigins_AllowsAnyOrigin()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "https://anywhere.example";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("*", context.Response.Headers.AccessControlAllowOrigin.ToString());
        }

        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_AllowedOrigin_EchoesOrigin()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["https://app.example"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "https://app.example";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("https://app.example", context.Response.Headers.AccessControlAllowOrigin.ToString());
            Assert.Equal("Origin", context.Response.Headers.Vary.ToString());
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_AllowedHost_EchoesOrigin()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["app.example"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "https://APP.example:8443";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("https://APP.example:8443", context.Response.Headers.AccessControlAllowOrigin.ToString());
        }

        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_DisallowedOrigin_RespondsUnauthorized()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["https://app.example"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "https://evil.example";

            await middleware.Invoke(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
            Assert.Null(bus.QueryInterfaceType);
        }

        //like the other CQRS servers a request must have an origin when origins are restricted
        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_NoOrigin_RespondsUnauthorized()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["https://app.example"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
            Assert.Null(bus.QueryInterfaceType);
        }

        //ApiClient sends the gateway host as the origin
        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_ApiClientHostOrigin_Allowed()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["https://app.example", "gateway.example"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "gateway.example";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_Preflight_EchoesAllowedOriginOnly()
        {
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["https://app.example"]);

            var allowed = new DefaultHttpContext();
            allowed.RequestAborted = TestContext.Current.CancellationToken;
            allowed.Request.Method = "OPTIONS";
            allowed.Request.Headers.Origin = "https://app.example";
            await middleware.Invoke(allowed);
            Assert.Equal("https://app.example", allowed.Response.Headers.AccessControlAllowOrigin.ToString());

            var disallowed = new DefaultHttpContext();
            disallowed.RequestAborted = TestContext.Current.CancellationToken;
            disallowed.Request.Method = "OPTIONS";
            disallowed.Request.Headers.Origin = "https://evil.example";
            await middleware.Invoke(disallowed);
            Assert.False(disallowed.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        }

        [Fact(Timeout = timeout)]
        public async Task AllowOrigins_Wildcard_AllowsAnyOrigin()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, allowOrigins: ["*"]);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Origin = "https://anywhere.example";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("*", context.Response.Headers.AccessControlAllowOrigin.ToString());
        }

        [Fact(Timeout = timeout)]
        public async Task UseCqrsApiGateway_WithAllowOrigins_AppliesOrigins()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var builder = new ApplicationBuilder(new ServiceCollection().AddSingleton<IBus>(bus).AddSingleton(serializer).BuildServiceProvider());
            _ = builder.UseCqrsApiGateway("/api", ["https://app.example"]);
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Path = "/api";
            context.Request.Headers.Origin = "https://evil.example";

            await app(context);

            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact(Timeout = timeout)]
        public async Task UseCqrsApiGateway_NullRouteWithAllowOrigins_ServesAnyPath()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var builder = new ApplicationBuilder(new ServiceCollection().AddSingleton<IBus>(bus).AddSingleton(serializer).BuildServiceProvider());
            _ = builder.UseCqrsApiGateway(null, ["https://app.example"]);
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Path = "/any/path";
            context.Request.Headers.Origin = "https://app.example";

            await app(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("https://app.example", context.Response.Headers.AccessControlAllowOrigin.ToString());
        }

        //UseMiddleware picks a constructor by argument type and a null route matches none, a null route falls back to the constructor default and serves every path
        [Fact(Timeout = timeout)]
        public async Task UseCqrsApiGateway_NullRoute_ServesAnyPath()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var builder = new ApplicationBuilder(new ServiceCollection().AddSingleton<IBus>(bus).AddSingleton(serializer).BuildServiceProvider());
            _ = builder.UseCqrsApiGateway(null);
            var app = builder.Build();
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
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
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Path = "/other";

            await app(context);

            Assert.True(nextInvoked);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task NamelessServer_JsonNamelessRequest_RespondsNameless()
        {
            var namelessSerializer = new ZerraJsonSerializer(new Zerra.Serialization.Json.JsonSerializerOptions() { Nameless = true });
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(new TestModel { Id = 3, Name = "Three" }) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, namelessSerializer);
            var context = CreateContext(namelessSerializer, new ApiRequestData() { ProviderType = nameof(ITestQueryHandler), ProviderMethod = nameof(ITestQueryHandler.GetModel), ProviderArguments = [], Source = source }, HttpCommon.ContentTypeJsonNameless, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("application/jsonnameless; charset=utf-8", context.Response.ContentType);
            var result = namelessSerializer.Deserialize<TestModel>(ReadResponse(context));
            Assert.Equal(3, result!.Id);
            Assert.Equal("Three", result.Name);
        }

        [Fact(Timeout = timeout)]
        public async Task JsonServer_RespondsJson()
        {
            var jsonSerializer = new ZerraJsonSerializer();
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(new TestModel { Id = 4 }) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, jsonSerializer);
            var context = CreateContext(jsonSerializer, new ApiRequestData() { ProviderType = nameof(ITestQueryHandler), ProviderMethod = nameof(ITestQueryHandler.GetModel), ProviderArguments = [], Source = source }, HttpCommon.ContentTypeJson, TestContext.Current.CancellationToken);
            context.Request.Headers.Accept = "application/json";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
            Assert.Equal(4, jsonSerializer.Deserialize<TestModel>(ReadResponse(context))!.Id);
        }

        [Theory(Timeout = timeout)]
        [InlineData("application/octet-stream")]
        [InlineData("text/html")]
        [InlineData("*/*")]
        public async Task Accept_MatchingOrUnknown_UsesTheSerializer(string accept)
        {
            //an accept the gateway doesn't know is ignored, like a browser's */*
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Accept = accept;

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(42, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Fact(Timeout = timeout)]
        public async Task AcceptJsonNameless_ByteServer_RespondsBadRequest()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Accept = "application/jsonnameless";

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task Authorizer_ReceivesTheRequestHeaders()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var authorizer = new RecordingAuthorizer();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer, authorizer: authorizer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Headers.Authorization = "Bearer token";

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(["Bearer token"], authorizer.Headers!["Authorization"]);
        }

        private static MemoryStream UploadBody(byte[] dataBytes, byte[] streamBytes)
        {
            var body = new MemoryStream();
            body.Write(BitConverter.GetBytes(dataBytes.Length));
            body.Write(dataBytes);
            body.Write(BitConverter.GetBytes(0));
            body.Write(streamBytes);
            body.Position = 0;
            return body;
        }

        [Fact(Timeout = timeout)]
        public async Task Upload_LiftsTheBodySizeLimit()
        {
            //the stream's length is up to the uploader
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(42) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            var feature = new MaxRequestBodySizeFeature();
            context.Features.Set<IHttpMaxRequestBodySizeFeature>(feature);
            context.Request.Body = UploadBody(((MemoryStream)context.Request.Body).ToArray(), [1, 2]);
            context.Request.Headers[HttpCommon.UploadStreamHeader] = HttpCommon.UploadStreamValue;

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Null(feature.MaxRequestBodySize);
            Assert.Equal([1, 2], bus.QueryArgumentStreamBytes);
        }

        [Fact(Timeout = timeout)]
        public async Task Upload_WithoutProviderType_RespondsWithError()
        {
            //only queries take a stream
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(new ApiRequestData() { MessageType = nameof(TestCommand), MessageData = "{}", Source = source }, TestContext.Current.CancellationToken);
            context.Request.Body = UploadBody(((MemoryStream)context.Request.Body).ToArray(), [1, 2]);
            context.Request.Headers[HttpCommon.UploadStreamHeader] = HttpCommon.UploadStreamValue;

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("Invalid Request", ExceptionSerializer.Deserialize(source, serializer, ReadResponse(context)).Message);
            Assert.Null(bus.Command);
        }

        [Fact(Timeout = timeout)]
        public async Task EmptyBody_RespondsWithError()
        {
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Request.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Null(bus.QueryInterfaceType);
        }

        [Fact(Timeout = timeout)]
        public async Task NoProviderOrMessage_RespondsBadRequest()
        {
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(new ApiRequestData() { Source = source }, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_NullResult_RespondsEmpty()
        {
            var bus = new MockBus { QueryResponse = new RemoteQueryCallResponse(null) };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetModel)), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Empty(ReadResponse(context));
        }

        [Theory(Timeout = timeout)]
        [InlineData(false, "dispatch")]
        [InlineData(true, "await")]
        public async Task Command_Dispatches(bool messageAwait, string dispatch)
        {
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(new ApiRequestData() { MessageType = nameof(TestCommand), MessageData = "{\"Value\":5}", MessageAwait = messageAwait, Source = source }, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(dispatch, bus.CommandDispatch);
            Assert.Equal(5, Assert.IsType<TestCommand>(bus.Command).Value);
            Assert.Empty(ReadResponse(context));
        }

        [Fact(Timeout = timeout)]
        public async Task Command_WithResult_RespondsWithTheResult()
        {
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(new ApiRequestData() { MessageType = nameof(TestResultCommand), MessageData = "{\"Value\":5}", MessageResult = true, Source = source }, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("result", bus.CommandDispatch);
            Assert.Equal(10, serializer.Deserialize<int>(ReadResponse(context)));
        }

        [Theory(Timeout = timeout)]
        [InlineData("Missing", "{}", false)]
        [InlineData(nameof(TestModel), "{}", false)]
        [InlineData(nameof(TestCommand), "null", false)]
        [InlineData("Missing", "{}", true)]
        [InlineData(nameof(TestModel), "{}", true)]
        [InlineData(nameof(TestResultCommand), "null", true)]
        public async Task Command_Invalid_RespondsWithError(string messageType, string messageData, bool messageResult)
        {
            //not registered, not a command, or no command in the data
            var bus = new MockBus();
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(new ApiRequestData() { MessageType = messageType, MessageData = messageData, MessageResult = messageResult, Source = source }, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Null(bus.Command);
        }

        [Fact(Timeout = timeout)]
        public async Task Canceled_RespondsWithoutAnError()
        {
            var bus = new MockBus { QueryException = new OperationCanceledException() };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Empty(ReadResponse(context));
        }

        [Fact(Timeout = timeout)]
        public async Task ErrorAfterResponseStarted_AbortsTheConnection()
        {
            //the status can't change once the response started, the client sees a failed request instead of a cut off body
            var bus = new MockBus { QueryException = new InvalidOperationException("query failed") };
            var middleware = new CqrsApiGatewayMiddleware(_ => Task.CompletedTask, bus, serializer);
            var context = CreateContext(QueryRequest(nameof(ITestQueryHandler.GetThings), 21), TestContext.Current.CancellationToken);
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
            var lifetime = new AbortRecordingLifetimeFeature();
            context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);

            await middleware.Invoke(context);

            Assert.True(lifetime.Aborted);
            Assert.Empty(ReadResponse(context));
        }

        private static DefaultHttpContext CreateContext(ISerializer requestSerializer, ApiRequestData data, string contentType, CancellationToken cancellationToken)
        {
            var context = new DefaultHttpContext();
            context.RequestAborted = cancellationToken;
            context.Request.Method = "POST";
            context.Request.ContentType = contentType;
            context.Request.Body = new MemoryStream(requestSerializer.SerializeBytes(data));
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static ApiRequestData QueryRequest(string methodName, params object[] arguments) => new()
        {
            ProviderType = typeof(ITestQueryHandler).Name,
            ProviderMethod = methodName,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = source
        };

        //what ApiClient sends
        private static DefaultHttpContext CreateContext(ApiRequestData data, CancellationToken cancellationToken)
        {
            var context = new DefaultHttpContext();
            context.RequestAborted = cancellationToken;
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
            public byte[]? QueryArgumentStreamBytes { get; private set; }

            public ILogger? Log => null;
            public string ServiceName => "Mock";

            public Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, byte[]?[] arguments, Stream? argumentStream, string source, ISerializer serializer, CancellationToken cancellationToken)
            {
                QueryInterfaceType = interfaceType;
                QueryMethodName = methodName;
                if (argumentStream is not null)
                {
                    using var ms = new MemoryStream();
                    argumentStream.CopyTo(ms);
                    QueryArgumentStreamBytes = ms.ToArray();
                }
                if (QueryException is not null)
                    return Task.FromException<RemoteQueryCallResponse>(QueryException);
                return Task.FromResult(QueryResponse ?? new RemoteQueryCallResponse(null));
            }

            public ICommand? Command { get; private set; }
            public string? CommandDispatch { get; private set; }

            public Task RemoteHandleCommandDispatchAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                Command = command;
                CommandDispatch = "dispatch";
                return Task.CompletedTask;
            }
            public Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                Command = command;
                CommandDispatch = "await";
                return Task.CompletedTask;
            }
            public Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                Command = command;
                CommandDispatch = "result";
                return Task.FromResult<object?>(((TestResultCommand)command).Value * 2);
            }
            public Task RemoteHandleEventDispatchAsync(IEvent @event, string source) => throw new NotImplementedException();
            public Type? GetTypeByName(string name) => name switch
            {
                nameof(ITestQueryHandler) => typeof(ITestQueryHandler),
                nameof(TestCommand) => typeof(TestCommand),
                nameof(TestResultCommand) => typeof(TestResultCommand),
                nameof(TestModel) => typeof(TestModel),
                _ => null
            };

            public void AddHandler<TInterface>(TInterface handler) where TInterface : notnull => throw new NotImplementedException();
            public void AddCommandProducer<TInterface>(ICommandProducer commandProducer) => throw new NotImplementedException();
            public void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer) => throw new NotImplementedException();
            public void AddEventProducer<TInterface>(IEventProducer eventProducer) => throw new NotImplementedException();
            public void AddEventConsumer<TInterface>(IEventConsumer eventConsumer, EventConsumerMode eventConsumerMode) => throw new NotImplementedException();
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

        private sealed class RejectingAuthorizer : ICqrsAuthorizer
        {
            public void Authorize(Dictionary<string, List<string?>> headers) => throw new System.Security.SecurityException("not allowed");
            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default) => new();
            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => new(new Dictionary<string, List<string?>>());
        }

        public sealed class TestCommand : ICommand
        {
            public int Value { get; set; }
        }

        public sealed class TestResultCommand : ICommand<int>
        {
            public int Value { get; set; }
        }

        private sealed class RecordingAuthorizer : ICqrsAuthorizer
        {
            public Dictionary<string, List<string?>>? Headers { get; private set; }
            public void Authorize(Dictionary<string, List<string?>> headers) => Headers = headers;
            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default) => new();
            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => new(new Dictionary<string, List<string?>>());
        }

        private sealed class MaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
        {
            public bool IsReadOnly => false;
            public long? MaxRequestBodySize { get; set; } = 100;
        }

        private sealed class StartedResponseFeature : HttpResponseFeature
        {
            public override bool HasStarted => true;
        }

        private sealed class AbortRecordingLifetimeFeature : IHttpRequestLifetimeFeature
        {
            public bool Aborted { get; private set; }
            public CancellationToken RequestAborted { get; set; }
            public void Abort() => Aborted = true;
        }

        public sealed class TestModel
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
            TestModel GetModel();
        }
    }
}
