// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;
using Zerra.Compression;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class KestrelCqrsServerMiddlewareRequestTests
    {
        private const int timeout = 10000;

        public interface IRequestQueryHandler : IQueryHandler
        {
            int Double(int value);
            Stream GetStream();
        }

        private sealed class RecordingAuthorizer : ICqrsAuthorizer
        {
            public Dictionary<string, List<string?>>? Headers { get; private set; }
            public void Authorize(Dictionary<string, List<string?>> headers) => Headers = headers;
            public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(new Dictionary<string, List<string?>>());
            public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default) => new();
        }

        private static KestrelCqrsServerMiddleware CreateMiddleware(ISerializer serializer, QueryHandlerDelegate query, ICqrsAuthorizer? authorizer = null, ICompressor? compressor = null)
        {
            var settings = new KestrelCqrsServerLinkedSettings(null, authorizer, serializer.ContentType);
            IQueryServer queryServer = new KestrelCqrsServerQueryServer(settings);
            queryServer.Setup(query);
            queryServer.RegisterInterfaceType(10, typeof(IRequestQueryHandler));
            return new KestrelCqrsServerMiddleware(_ => Task.CompletedTask, serializer, null, compressor, null, settings);
        }

        private static DefaultHttpContext CreateContext(ISerializer serializer, CqrsRequestData data, string? contentType, CancellationToken cancellationToken, bool providerHeader = true, ICompressor? compressor = null)
        {
            var context = new DefaultHttpContext();
            context.RequestAborted = cancellationToken;
            context.Request.Method = "POST";
            context.Request.ContentType = contentType;
            if (providerHeader)
                context.Request.Headers[HttpCommon.ProviderTypeHeader] = data.ProviderType;
            context.Request.Headers["Authorization"] = "Bearer token";
            var body = serializer.SerializeBytes(data);
            context.Request.Body = new MemoryStream(compressor is null ? body : compressor.Compress(body));
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static CqrsRequestData Request(ISerializer serializer, string method, params object[] arguments) => new()
        {
            ProviderType = nameof(IRequestQueryHandler),
            ProviderMethod = method,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = "test",
        };

        private static Task<RemoteQueryCallResponse> Double(ISerializer serializer, byte[]?[] arguments)
            => Task.FromResult(new RemoteQueryCallResponse(serializer.Deserialize<int>(arguments[0]!) * 2));

        [Theory(Timeout = timeout)]
        [InlineData(null, true)]
        [InlineData("text/plain", true)]
        [InlineData(HttpCommon.ContentTypeJson, true)]
        [InlineData(HttpCommon.ContentTypeBytes, false)]
        public async Task BadRequest_Returns400(string? contentType, bool providerHeader)
        {
            //no content type, an unknown one, one the server doesn't use, or no provider header
            var serializer = new ZerraByteSerializer();
            var called = false;
            using var middleware = CreateMiddleware(serializer, (_, _, _, _, _, _, _) => { called = true; return Task.FromResult(new RemoteQueryCallResponse(1)); });
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.Double), 2), contentType, TestContext.Current.CancellationToken, providerHeader);

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.False(called);
        }

        public static TheoryData<bool> Nameless => new() { false, true };

        [Theory(Timeout = timeout)]
        [MemberData(nameof(Nameless))]
        public async Task JsonServer_RespondsWithJsonContentType(bool nameless)
        {
            var serializer = new ZerraJsonSerializer(new Zerra.Serialization.Json.JsonSerializerOptions() { Nameless = nameless });
            using var middleware = CreateMiddleware(serializer, (_, _, arguments, _, _, _, _) => Double(serializer, arguments));
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.Double), 21), nameless ? HttpCommon.ContentTypeJsonNameless : HttpCommon.ContentTypeJson, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(nameless ? HttpCommon.ContentTypeJsonNameless : HttpCommon.ContentTypeJson, context.Response.Headers[HttpCommon.ContentTypeHeader]);
            Assert.Equal(42, serializer.Deserialize<int>(((MemoryStream)context.Response.Body).ToArray()));
        }

        [Fact(Timeout = timeout)]
        public async Task Authorizer_ReceivesTheRequestHeaders()
        {
            var serializer = new ZerraByteSerializer();
            var authorizer = new RecordingAuthorizer();
            using var middleware = CreateMiddleware(serializer, (_, _, arguments, _, _, _, _) => Double(serializer, arguments), authorizer);
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.Double), 1), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken);

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(["Bearer token"], authorizer.Headers!["Authorization"]);
        }

        [Fact(Timeout = timeout)]
        public async Task WithoutAuthorizer_ClaimsBecomeThePrincipal()
        {
            //the client sends its principal's claims and the handler sees them
            var serializer = new ZerraByteSerializer();
            string? name = null;
            using var middleware = CreateMiddleware(serializer, (_, _, arguments, _, _, _, _) =>
            {
                name = ((ClaimsPrincipal?)Thread.CurrentPrincipal)?.FindFirst("name")?.Value;
                return Double(serializer, arguments);
            });
            var request = Request(serializer, nameof(IRequestQueryHandler.Double), 1);
            request.Claims = [["name", "tester"]];

            await middleware.Invoke(CreateContext(serializer, request, HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken));

            Assert.Equal("tester", name);
        }

        public sealed class RequestCommand : ICommand
        {
            public int Value { get; set; }
        }
        public sealed class RequestResultCommand : ICommand<int>
        {
            public int Value { get; set; }
        }
        public sealed class RequestEvent : IEvent
        {
            public int Value { get; set; }
        }

        private sealed class Handled
        {
            public object? Message;
            public string? How;
        }

        private static KestrelCqrsServerMiddleware CreateMessageMiddleware(ISerializer serializer, Handled handled, QueryHandlerDelegate? query = null)
        {
            var settings = new KestrelCqrsServerLinkedSettings(null, null, serializer.ContentType);
            IQueryServer queryServer = new KestrelCqrsServerQueryServer(settings);
            queryServer.Setup(query ?? ((_, _, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(1))));
            queryServer.RegisterInterfaceType(10, typeof(IRequestQueryHandler));
            ICommandConsumer commandConsumer = new KestrelCqrsServerCommandConsumer(settings);
            commandConsumer.RegisterCommandType(10, "topic", typeof(RequestCommand));
            commandConsumer.RegisterCommandType(10, "topic", typeof(RequestResultCommand));
            commandConsumer.Setup(null,
                (command, _, _) => { handled.Message = command; handled.How = "dispatch"; return Task.CompletedTask; },
                (command, _, _) => { handled.Message = command; handled.How = "await"; return Task.CompletedTask; },
                (command, _, _) => { handled.Message = command; handled.How = "result"; return Task.FromResult<object?>(((RequestResultCommand)command).Value * 2); });
            IEventConsumer eventConsumer = new KestrelCqrsServerEventConsumer(settings);
            eventConsumer.RegisterEventType(10, "topic", typeof(RequestEvent), EventConsumerMode.PerService);
            eventConsumer.Setup("service", (@event, _) => { handled.Message = @event; handled.How = "event"; return Task.CompletedTask; });
            return new KestrelCqrsServerMiddleware(_ => Task.CompletedTask, serializer, null, null, null, settings);
        }

        private static CqrsRequestData Message(ISerializer serializer, object message, bool messageAwait = false, bool messageResult = false) => new()
        {
            MessageType = message.GetType().Name,
            MessageData = serializer.SerializeBytes(message, message.GetType()),
            MessageAwait = messageAwait,
            MessageResult = messageResult,
            Source = "test",
        };

        public static TheoryData<int, string> Messages => new()
        {
            { 0, "dispatch" },
            { 1, "await" },
            { 2, "result" },
            { 3, "event" },
        };

        [Theory(Timeout = timeout)]
        [MemberData(nameof(Messages))]
        public async Task JsonServer_HandlesMessages(int kind, string how)
        {
            var serializer = new ZerraJsonSerializer();
            var handled = new Handled();
            using var middleware = CreateMessageMiddleware(serializer, handled);
            var request = kind switch
            {
                0 => Message(serializer, new RequestCommand { Value = 5 }),
                1 => Message(serializer, new RequestCommand { Value = 5 }, messageAwait: true),
                2 => Message(serializer, new RequestResultCommand { Value = 5 }, messageResult: true),
                _ => Message(serializer, new RequestEvent { Value = 5 }),
            };
            var context = CreateContext(serializer, request, HttpCommon.ContentTypeJson, TestContext.Current.CancellationToken, providerHeader: false);
            context.Request.Headers[HttpCommon.ProviderTypeHeader] = request.MessageType;

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(how, handled.How);
            Assert.Equal(HttpCommon.ContentTypeJson, context.Response.Headers[HttpCommon.ContentTypeHeader]);
            var body = ((MemoryStream)context.Response.Body).ToArray();
            if (kind == 2)
                Assert.Equal(10, serializer.Deserialize<int>(body));
            else
                Assert.Empty(body);
        }

        public static TheoryData<string> InvalidRequests => new()
        {
            "upload command",
            "empty body",
            "unregistered provider",
            "unregistered message",
            "null command",
            "null event",
        };

        [Theory(Timeout = timeout)]
        [MemberData(nameof(InvalidRequests))]
        public async Task InvalidRequest_RespondsWithError(string invalid)
        {
            var serializer = new ZerraJsonSerializer();
            var handled = new Handled();
            using var middleware = CreateMessageMiddleware(serializer, handled);
            var request = invalid switch
            {
                "unregistered provider" => new CqrsRequestData() { ProviderType = "Missing", ProviderMethod = "Method", ProviderArguments = [], Source = "test" },
                "unregistered message" => new CqrsRequestData() { MessageType = "Missing", MessageData = "{}"u8.ToArray(), Source = "test" },
                "null command" => new CqrsRequestData() { MessageType = nameof(RequestCommand), MessageData = "null"u8.ToArray(), Source = "test" },
                "null event" => new CqrsRequestData() { MessageType = nameof(RequestEvent), MessageData = "null"u8.ToArray(), Source = "test" },
                _ => Message(serializer, new RequestCommand { Value = 5 }),
            };
            var context = CreateContext(serializer, request, HttpCommon.ContentTypeJson, TestContext.Current.CancellationToken, providerHeader: false);
            context.Request.Headers[HttpCommon.ProviderTypeHeader] = request.ProviderType ?? request.MessageType;
            if (invalid == "empty body")
            {
                context.Request.Body = new MemoryStream();
            }
            else if (invalid == "upload command")
            {
                //only queries take a stream
                var dataBytes = ((MemoryStream)context.Request.Body).ToArray();
                var body = new MemoryStream();
                body.Write(BitConverter.GetBytes(dataBytes.Length));
                body.Write(dataBytes);
                body.Write(BitConverter.GetBytes(0));
                body.Write([1, 2]);
                body.Position = 0;
                context.Request.Body = body;
                context.Request.Headers[HttpCommon.UploadStreamHeader] = HttpCommon.UploadStreamValue;
            }

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal(HttpCommon.ContentTypeJson, context.Response.Headers[HttpCommon.ContentTypeHeader]);
            Assert.Null(handled.How);
            var error = ExceptionSerializer.Deserialize("test", serializer, ((MemoryStream)context.Response.Body).ToArray());
            if (invalid.StartsWith("unregistered"))
                Assert.EndsWith($"is not registered with {nameof(KestrelCqrsServerMiddleware)}", error.Message);
            else
                Assert.Equal("Invalid Request", error.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task NoProviderOrMessage_RespondsBadRequest()
        {
            var serializer = new ZerraJsonSerializer();
            var handled = new Handled();
            using var middleware = CreateMessageMiddleware(serializer, handled);
            var context = CreateContext(serializer, new CqrsRequestData() { Source = "test" }, HttpCommon.ContentTypeJson, TestContext.Current.CancellationToken, providerHeader: false);
            context.Request.Headers[HttpCommon.ProviderTypeHeader] = "Anything";

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Null(handled.How);
        }

        [Fact(Timeout = timeout)]
        public async Task EncryptedStreamResponse_Decrypts()
        {
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test-key", SymmetricAlgorithmType.AESwithPrefix);
            var streamData = Enumerable.Range(0, 5000).Select(x => (byte)x).ToArray();
            var settings = new KestrelCqrsServerLinkedSettings(null, null, serializer.ContentType);
            IQueryServer queryServer = new KestrelCqrsServerQueryServer(settings);
            queryServer.Setup((_, _, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(new MemoryStream(streamData))));
            queryServer.RegisterInterfaceType(10, typeof(IRequestQueryHandler));
            using var middleware = new KestrelCqrsServerMiddleware(_ => Task.CompletedTask, serializer, encryptor, null, null, settings);
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.GetStream)), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken);
            context.Request.Body = new MemoryStream(encryptor.Encrypt(((MemoryStream)context.Request.Body).ToArray()));

            await middleware.Invoke(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(streamData, encryptor.Decrypt(((MemoryStream)context.Response.Body).ToArray()));
        }

        private sealed class FailingStream : Stream
        {
            private int reads;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (reads++ > 0)
                    throw new IOException("source failed");
                buffer.AsSpan(offset, count).Fill(7);
                return count;
            }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        [Fact(Timeout = timeout)]
        public async Task CompressedStreamResponse_SourceFails_RespondsWithError()
        {
            //the compress stream is still disposed when the source fails partway
            var serializer = new ZerraByteSerializer();
            var compressor = new ZerraCompressor(CompressionAlgorithmType.GZip);
            using var middleware = CreateMiddleware(serializer, (_, _, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(new FailingStream())), compressor: compressor);
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.GetStream)), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken, compressor: compressor);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
        }

        public sealed class ThrowingModel
        {
            public int Value { get => throw new InvalidOperationException("model failed"); set { } }
        }

        [Fact(Timeout = timeout)]
        public async Task CompressedModelResponse_SerializeFails_RespondsWithError()
        {
            var serializer = new ZerraByteSerializer();
            var compressor = new ZerraCompressor(CompressionAlgorithmType.GZip);
            using var middleware = CreateMiddleware(serializer, (_, _, _, _, _, _, _) => Task.FromResult(new RemoteQueryCallResponse(new ThrowingModel())), compressor: compressor);
            var context = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.Double), 1), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken, compressor: compressor);

            await middleware.Invoke(context);

            Assert.Equal(500, context.Response.StatusCode);
        }

        private static byte[] Decompress(ICompressor compressor, HttpContext context)
        {
            using var decompress = compressor.Decompress(new MemoryStream(((MemoryStream)context.Response.Body).ToArray()), false);
            using var output = new MemoryStream();
            decompress.CopyTo(output);
            return output.ToArray();
        }

        [Fact(Timeout = timeout)]
        public async Task Compressor_CompressesModelAndStreamResponses()
        {
            var serializer = new ZerraByteSerializer();
            var compressor = new ZerraCompressor(CompressionAlgorithmType.GZip);
            var streamData = Enumerable.Range(0, 5000).Select(x => (byte)x).ToArray();
            using var middleware = CreateMiddleware(serializer, (_, method, arguments, _, _, _, _) => method == nameof(IRequestQueryHandler.GetStream)
                ? Task.FromResult(new RemoteQueryCallResponse(new MemoryStream(streamData)))
                : Double(serializer, arguments), compressor: compressor);

            var model = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.Double), 21), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken, compressor: compressor);
            await middleware.Invoke(model);
            Assert.Equal(42, serializer.Deserialize<int>(Decompress(compressor, model)));

            var stream = CreateContext(serializer, Request(serializer, nameof(IRequestQueryHandler.GetStream)), HttpCommon.ContentTypeBytes, TestContext.Current.CancellationToken, compressor: compressor);
            await middleware.Invoke(stream);
            Assert.Equal(streamData, Decompress(compressor, stream));
        }
    }
}
