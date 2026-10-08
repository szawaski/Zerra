// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.Reflection;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    public class TcpCqrsServerTests
    {
        private const int timeout = 10000;
        private const string source = "test-source";

        private static readonly ISerializer serializer = new ZerraByteSerializer();
        private static readonly IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AES);

        public TcpCqrsServerTests()
        {
            //the server looks up command/event info that the bus normally generates when handlers are registered
            _ = BusCommandOrEventInfo.GetByType(typeof(ITestMessageHandler), null);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_ReturnsModel(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            Type? receivedType = null;
            string? receivedMethod = null;
            int? receivedArgument = null;
            string? receivedSource = null;
            using var server = StartQueryServer(out var port, enc, (interfaceType, methodName, arguments, _, requestSource, _, _) =>
            {
                receivedType = interfaceType;
                receivedMethod = methodName;
                receivedArgument = serializer.Deserialize<int>(arguments[0]);
                receivedSource = requestSource;
                return Task.FromResult(new RemoteQueryCallResponse(receivedArgument * 2));
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), enc, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(serializer.ContentType, header.ContentType);
            Assert.Equal(42, await connection.ReadBodyAsync<int>(header, enc, TestContext.Current.CancellationToken));

            Assert.Equal(typeof(ITestQueryHandler), receivedType);
            Assert.Equal(nameof(ITestQueryHandler.GetThings), receivedMethod);
            Assert.Equal(21, receivedArgument);
            Assert.Equal(source, receivedSource);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_ReturnsStream(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartQueryServer(out var port, enc, (_, _, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(new MemoryStream([1, 2, 3, 4, 5]))));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream)), enc, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal([1, 2, 3, 4, 5], await connection.ReadBodyBytesAsync(header, enc, TestContext.Current.CancellationToken));
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ReturnsStream_DisposesStream()
        {
            var resultStream = new DisposeSignalStream([1, 2, 3]);
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(resultStream)));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetStream)), null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.Equal([1, 2, 3], await connection.ReadBodyBytesAsync(header, null, TestContext.Current.CancellationToken));
            await resultStream.Disposed.Task; //the handler's stream is released once it's sent, such as a file handle
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Query_HandlerThrows_RespondsWithError(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartQueryServer(out var port, enc, (_, _, _, _, _, _, _) =>
                Task.FromException<RemoteQueryCallResponse>(new InvalidOperationException("query failed")));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), enc, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, enc, TestContext.Current.CancellationToken);
            Assert.Equal("query failed", exception.Message);
            Assert.Equal(nameof(InvalidOperationException), exception.ErrorType);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_WithClaims_SetsThreadPrincipalForHandler()
        {
            IPrincipal? principal = null;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _, _) =>
            {
                principal = Thread.CurrentPrincipal;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            var request = QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21);
            request.Claims = [["name", "tester"]];

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(request, null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            var claimsPrincipal = Assert.IsType<ClaimsPrincipal>(principal);
            Assert.Equal("tester", claimsPrincipal.FindFirst("name")?.Value);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_UnregisteredInterface_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(QueryRequest(typeof(IOtherQueryHandler), nameof(IOtherQueryHandler.GetOther)), null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, null, TestContext.Current.CancellationToken);
            Assert.StartsWith("Unhandled Provider Type", exception.Message);
            Assert.False(handlerInvoked);

            //the request was fully read so the connection is still usable
            handlerInvoked = false;
            await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null, cancellationToken: TestContext.Current.CancellationToken);
            header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(1, await connection.ReadBodyAsync<int>(header, null, TestContext.Current.CancellationToken));
            Assert.True(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_ContentTypeMismatch_DoesNotInvokeHandler()
        {
            var handlerInvoked = false;
            using var server = StartQueryServer(out var port, null, (_, _, _, _, _, _, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(new RemoteQueryCallResponse(1));
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            try
            {
                await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 21), null, ContentType.Json, TestContext.Current.CancellationToken);
            }
            catch (IOException) { } //server may reset the connection before the body is sent

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.True(header is null || header.IsError);
            Assert.False(handlerInvoked);
        }

        [Fact(Timeout = timeout)]
        public async Task Query_MultipleRequestsOnSameConnection()
        {
            using var server = StartQueryServer(out var port, null, (_, _, arguments, _, _, _, _) =>
                Task.FromResult(new RemoteQueryCallResponse(serializer.Deserialize<int>(arguments[0]) * 2)));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            for (var i = 1; i <= 3; i++)
            {
                await connection.SendAsync(QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), i), null, cancellationToken: TestContext.Current.CancellationToken);
                var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(header);
                Assert.Equal(i * 2, await connection.ReadBodyAsync<int>(header, null, TestContext.Current.CancellationToken));
            }
        }

        [Fact(Timeout = timeout)]
        public async Task Command_InvokesHandlerAndResponds()
        {
            var received = new TaskCompletionSource<(int Value, string Source)>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, command: (command, commandSource, cancellationToken) =>
            {
                _ = received.TrySetResult((((TestCommand)command).Value, commandSource));
                return Task.CompletedTask;
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, false, false), null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal((5, source), await received.Task);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_WithoutLimit_ReleasesThrottle()
        {
            var handled = 0;
            using var server = StartMessageServer(out var port, null, command: (_, _, _) =>
            {
                _ = Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });

            //more than the server's 10 concurrent, each handled after its response
            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            for (var i = 0; i < 15; i++)
            {
                await connection.SendAsync(MessageRequest(new TestCommand { Value = i }, false, false), null, cancellationToken: TestContext.Current.CancellationToken);
                var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(header);
                Assert.False(header.IsError);
            }
            while (Volatile.Read(ref handled) < 15)
                await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        [Fact(Timeout = timeout)]
        public async Task CommandAwait_RespondsAfterHandlerCompletes()
        {
            var handled = false;
            using var server = StartMessageServer(out var port, null, commandAwait: async (_, _, cancellationToken) =>
            {
                await Task.Delay(50, cancellationToken);
                handled = true;
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, true, false), null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.True(handled);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CommandWithResult_ReturnsResult(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartMessageServer(out var port, enc, commandWithResult: (command, _, _) =>
                Task.FromResult<object?>(((TestCommandWithResult)command).Value * 2));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestCommandWithResult { Value = 21 }, true, true), enc, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(42, await connection.ReadBodyAsync<int>(header, enc, TestContext.Current.CancellationToken));
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CommandAwait_HandlerThrows_RespondsWithError(bool encrypt)
        {
            var enc = encrypt ? encryptor : null;
            using var server = StartMessageServer(out var port, enc, commandAwait: (_, _, _) =>
                Task.FromException(new InvalidOperationException("command failed")));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 5 }, true, false), enc, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.True(header.IsError);
            var exception = await connection.ReadErrorAsync(header, enc, TestContext.Current.CancellationToken);
            Assert.Equal("command failed", exception.Message);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_PastReceiveLimit_RejectedWithoutCountingComplete()
        {
            var exits = 0;
            var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, commandAwait: async (_, _, _) =>
            {
                started.SetResult();
                await release.Task;
            }, commandCounter: new CommandCounter(1, () =>
            {
                _ = Interlocked.Increment(ref exits);
                exited.SetResult();
            }));

            await using var first = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await first.SendAsync(MessageRequest(new TestCommand { Value = 1 }, true, false), null, cancellationToken: TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(TestContext.Current.CancellationToken);

            //the second request on the connection is read after the server has finished with the first
            await using var second = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            for (var i = 0; i < 2; i++)
            {
                await second.SendAsync(MessageRequest(new TestCommand { Value = 2 }, true, false), null, cancellationToken: TestContext.Current.CancellationToken);
                var rejected = await second.ReadHeaderAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(rejected);
                Assert.True(rejected.IsError);
                Assert.Equal("Cannot receive any more commands", (await second.ReadErrorAsync(rejected, null, TestContext.Current.CancellationToken)).Message);
            }
            Assert.Equal(0, Volatile.Read(ref exits));

            release.SetResult();
            var header = await first.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            await exited.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, Volatile.Read(ref exits));
        }

        [Fact(Timeout = timeout)]
        public async Task Event_InvokesHandlerAndResponds()
        {
            var received = new TaskCompletionSource<(int Value, string Source)>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, @event: (@event, eventSource) =>
            {
                _ = received.TrySetResult((((TestEvent)@event).Value, eventSource));
                return Task.CompletedTask;
            });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestEvent { Value = 7 }, false, false), null, cancellationToken: TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal((7, source), await received.Task);
        }

        [Fact]
        public void ServiceUrl_ReturnsConstructorUrl()
        {
            using var server = new TcpCqrsServer("127.0.0.1:9999", serializer, null, null, null);

            Assert.Equal("127.0.0.1:9999", ((IQueryServer)server).ServiceUrl);
            Assert.Equal("127.0.0.1:9999", ((ICommandConsumer)server).MessageHost);
            Assert.Equal("127.0.0.1:9999", ((IEventConsumer)server).MessageHost);
        }

        [Fact]
        public void Open_AfterDispose_Throws()
        {
            var server = new TcpCqrsServer($"127.0.0.1:{GetFreePort()}", serializer, null, null, null);
            server.Dispose();

            _ = Assert.Throws<ObjectDisposedException>(() => ((IQueryServer)server).Open());
        }

        [Fact]
        public void Setup_EachRoleOnce_EvenAfterOpen()
        {
            using var server = OpenOnFreePort(out _, (freePort) => new TcpCqrsServer($"127.0.0.1:{freePort}", serializer, null, null, null), (server) =>
            {
                ((ICommandConsumer)server).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((ICommandConsumer)server).Open();
            });
            ((IEventConsumer)server).Setup("test-service", (_, _) => Task.CompletedTask);

            _ = Assert.Throws<InvalidOperationException>(() => ((ICommandConsumer)server).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null)));
            _ = Assert.Throws<InvalidOperationException>(() => ((IEventConsumer)server).Setup("test-service", (_, _) => Task.CompletedTask));
        }

        [Fact]
        public void Open_PortInUse_ThrowsAndDisposes()
        {
            using var taken = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            taken.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            taken.Listen();
            var server = new TcpCqrsServer($"127.0.0.1:{((IPEndPoint)taken.LocalEndPoint!).Port}", serializer, null, null, null);

            _ = Assert.Throws<SocketException>(() => ((IQueryServer)server).Open());
            server.Dispose();
        }

        [Fact(Timeout = timeout)]
        public async Task NotSetup_ClosesConnection()
        {
            //opened without registering anything so a connection can't be handled, it must be closed instead of left open
            using var server = OpenOnFreePort(out var port, (freePort) => new TcpCqrsServer($"127.0.0.1:{freePort}", serializer, null, null, null), (server) => ((IQueryServer)server).Open());

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), TestContext.Current.CancellationToken);
            Assert.Equal(0, await client.ReceiveAsync(new byte[1], SocketFlags.None, TestContext.Current.CancellationToken));
        }

        [Fact(Timeout = timeout)]
        public async Task RequestHeaderTooLong_ClosesConnection()
        {
            using var server = StartMessageServer(out var port, null);

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendRawAsync(Enumerable.Repeat((byte)'a', TcpCommon.BufferLength).ToArray(), TestContext.Current.CancellationToken);

            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.True(header is null || header.IsError);
        }

        public static TheoryData<string> InvalidRequests => new()
        {
            "EmptyBody",
            "NoProviderOrMessage",
            "UnregisteredMessage",
            "NullCommand",
            "NullEvent",
        };

        [Theory(Timeout = timeout)]
        [MemberData(nameof(InvalidRequests))]
        public async Task InvalidRequest_RespondsWithError(string kind)
        {
            var handled = false;
            using var server = StartMessageServer(out var port, null,
                command: (_, _, _) => { handled = true; return Task.CompletedTask; },
                @event: (_, _) => { handled = true; return Task.CompletedTask; });

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            var token = TestContext.Current.CancellationToken;
            switch (kind)
            {
                case "EmptyBody":
                    await connection.SendBodyAsync<CqrsRequestData>(nameof(TestCommand), null, token);
                    break;
                case "NoProviderOrMessage":
                    await connection.SendBodyAsync(nameof(TestCommand), new CqrsRequestData() { Source = source }, token);
                    break;
                case "UnregisteredMessage":
                    await connection.SendAsync(MessageRequest(new OtherCommand(), false, false), null, cancellationToken: token);
                    break;
                case "NullCommand":
                    await connection.SendBodyAsync(nameof(TestCommand), new CqrsRequestData() { MessageType = nameof(TestCommand), MessageData = serializer.SerializeBytes<TestCommand>(null), Source = source }, token);
                    break;
                case "NullEvent":
                    await connection.SendBodyAsync(nameof(TestEvent), new CqrsRequestData() { MessageType = nameof(TestEvent), MessageData = serializer.SerializeBytes<TestEvent>(null), Source = source }, token);
                    break;
            }

            var header = await connection.ReadHeaderAsync(token);
            if (kind == "EmptyBody")
            {
                //an unreadable request closes the connection
                Assert.Null(header);
            }
            else
            {
                Assert.NotNull(header);
                Assert.True(header.IsError);
                _ = await connection.ReadErrorAsync(header, null, token);
            }
            Assert.False(handled);
        }

        [Fact(Timeout = timeout)]
        public async Task Command_WithLimit_CountsCompleteAfterTheHandler()
        {
            var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = StartMessageServer(out var port, null, command: (_, _, _) => Task.CompletedTask, commandCounter: new CommandCounter(1, exited.SetResult));

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            await connection.SendAsync(MessageRequest(new TestCommand { Value = 1 }, false, false), null, cancellationToken: TestContext.Current.CancellationToken);
            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);

            await exited.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AbortedWhileHandling_SkipsTheResponseAndKeepsTheConnection(bool query)
        {
            //the client gave up, the handler still finishes but its response isn't sent, the connection takes the next request
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            async Task Wait()
            {
                if (Interlocked.Increment(ref calls) > 1)
                    return;
                started.SetResult();
                await release.Task;
            }
            using var server = query
                ? StartQueryServer(out var port, null, async (_, _, _, _, _, _, _) =>
                {
                    await Wait();
                    return new RemoteQueryCallResponse(1);
                })
                : StartMessageServer(out port, null, commandAwait: async (_, _, _) => await Wait());

            await using var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken);
            var request = query ? QueryRequest(typeof(ITestQueryHandler), nameof(ITestQueryHandler.GetThings), 1) : MessageRequest(new TestCommand { Value = 1 }, true, false);
            await connection.SendAsync(request, null, cancellationToken: TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.True(await connection.AbortAsync(TestContext.Current.CancellationToken));
            release.SetResult();

            await connection.SendAsync(request, null, cancellationToken: TestContext.Current.CancellationToken);
            var header = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(header);
            Assert.False(header.IsError);
            Assert.Equal(2, calls);
        }


        [Theory(Timeout = timeout)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_WhileOpen_WaitsForAFailingHandler(bool async)
        {
            //disposing without closing first stops listening, waits for the handler, and isn't thrown its failure
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var server = StartMessageServer(out var port, null, command: async (_, _, _) =>
            {
                started.SetResult();
                await Task.Delay(200);
                throw new InvalidOperationException("handler failed");
            });

            await using (var connection = await TestConnection.ConnectAsync(port, TestContext.Current.CancellationToken))
            {
                await connection.SendAsync(MessageRequest(new TestCommand { Value = 1 }, false, false), null, cancellationToken: TestContext.Current.CancellationToken);
                _ = await connection.ReadHeaderAsync(TestContext.Current.CancellationToken);
            }
            await started.Task.WaitAsync(TestContext.Current.CancellationToken);

            if (async)
            {
                await server.DisposeAsync();
                await server.DisposeAsync();
            }
            else
            {
                server.Dispose();
                server.Dispose();
            }

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _ = await Assert.ThrowsAsync<SocketException>(() => client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port), TestContext.Current.CancellationToken).AsTask());
        }

        private static TcpCqrsServer StartQueryServer(out int port, IEncryptor? encryptor, QueryHandlerDelegate handler)
        {
            return OpenOnFreePort(out port, (freePort) => new TcpCqrsServer($"127.0.0.1:{freePort}", serializer, encryptor, null, null), (server) =>
            {
                IQueryServer queryServer = server;
                queryServer.Setup(handler);
                queryServer.RegisterInterfaceType(10, typeof(ITestQueryHandler));
                queryServer.Open();
            });
        }

        private static TcpCqrsServer StartMessageServer(out int port, IEncryptor? encryptor,
            HandleRemoteCommandDispatch? command = null,
            HandleRemoteCommandDispatch? commandAwait = null,
            HandleRemoteCommandWithResultDispatch? commandWithResult = null,
            HandleRemoteEventDispatch? @event = null,
            CommandCounter? commandCounter = null)
        {
            return OpenOnFreePort(out port, (freePort) => new TcpCqrsServer($"127.0.0.1:{freePort}", serializer, encryptor, null, null), (server) =>
            {
                ICommandConsumer commandConsumer = server;
                commandConsumer.Setup(commandCounter,
                    command ?? ((_, _, _) => Task.CompletedTask),
                    commandAwait ?? ((_, _, _) => Task.CompletedTask),
                    commandWithResult ?? ((_, _, _) => Task.FromResult<object?>(null)));
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommand));
                commandConsumer.RegisterCommandType(10, "test", typeof(TestCommandWithResult));
                IEventConsumer eventConsumer = server;
                eventConsumer.Setup("test-service", @event ?? ((_, _) => Task.CompletedTask));
                eventConsumer.RegisterEventType(10, "test", typeof(TestEvent), EventConsumerMode.PerReplica);
                commandConsumer.Open();
            });
        }

        private static CqrsRequestData QueryRequest(Type interfaceType, string methodName, params object[] arguments) => new()
        {
            ProviderType = interfaceType.Name,
            ProviderMethod = methodName,
            ProviderArguments = arguments.Select(x => serializer.SerializeBytes(x, x.GetType())).ToArray(),
            Source = source
        };

        private static CqrsRequestData MessageRequest(object message, bool messageAwait, bool messageResult) => new()
        {
            MessageType = message.GetType().Name,
            MessageData = serializer.SerializeBytes(message, message.GetType()),
            MessageAwait = messageAwait,
            MessageResult = messageResult,
            Source = source
        };

        //the free port is released before the server binds it, so something else can take it in between
        private static T OpenOnFreePort<T>(out int port, Func<int, T> create, Action<T> open) where T : IDisposable
        {
            for (var attempt = 1; ; attempt++)
            {
                port = GetFreePort();
                var server = create(port);
                try
                {
                    open(server);
                    return server;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse && attempt < 10)
                {
                    server.Dispose();
                }
            }
        }

        private static int GetFreePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        //Speaks the client side of the TCP protocol so the server can be tested without TcpCqrsClient
        private sealed class DisposeSignalStream(byte[] data) : MemoryStream(data)
        {
            public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected override void Dispose(bool disposing)
            {
                _ = Disposed.TrySetResult();
                base.Dispose(disposing);
            }
        }

        private sealed class TestConnection : IAsyncDisposable
        {
            private readonly NetworkStream stream;

            private TestConnection(Socket socket)
            {
                this.stream = new NetworkStream(socket, true);
            }

            public static async Task<TestConnection> ConnectAsync(int port, CancellationToken cancellationToken = default)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
                return new TestConnection(socket);
            }

            public async Task SendAsync(CqrsRequestData data, IEncryptor? encryptor, ContentType? contentType = null, CancellationToken cancellationToken = default)
            {
                var buffer = new byte[TcpCommon.BufferLength];
                var headerLength = TcpCommon.BufferHeader(buffer, data.ProviderType ?? data.MessageType!, contentType ?? serializer.ContentType);
                await stream.WriteAsync(buffer.AsMemory(0, headerLength), cancellationToken);

                var body = new TcpProtocolBodyStream(stream, null, true, true);
                if (encryptor is not null)
                {
                    var cryptoStream = encryptor.Encrypt(body, true);
                    await serializer.SerializeAsync(cryptoStream, data, cancellationToken);
                    await cryptoStream.FlushFinalBlockAsync();
                    await cryptoStream.DisposeAsync();
                }
                else
                {
                    await serializer.SerializeAsync(body, data, cancellationToken);
                    await body.FlushAsync(cancellationToken);
                    await body.DisposeAsync();
                }
            }

            //any body under a header for the provider type, such as null or a request missing its fields
            public async Task SendBodyAsync<T>(string providerType, T? data, CancellationToken cancellationToken = default)
            {
                var buffer = new byte[TcpCommon.BufferLength];
                var headerLength = TcpCommon.BufferHeader(buffer, providerType, serializer.ContentType);
                await stream.WriteAsync(buffer.AsMemory(0, headerLength), cancellationToken);

                var body = new TcpProtocolBodyStream(stream, null, true, true);
                await serializer.SerializeAsync(body, data, cancellationToken);
                await body.FlushAsync(cancellationToken);
                await body.DisposeAsync();
            }

            public async Task SendRawAsync(byte[] bytes, CancellationToken cancellationToken = default) => await stream.WriteAsync(bytes, cancellationToken);

            //the client side of SocketAbortMonitor, true when the server acknowledges
            public async Task<bool> AbortAsync(CancellationToken cancellationToken = default)
            {
                await stream.WriteAsync(new byte[1], cancellationToken);
                var buffer = new byte[2];
                return await stream.ReadAsync(buffer, cancellationToken) == 1 && buffer[0] == 0;
            }

            //returns null when the server closes the connection without responding
            public async Task<TcpRequestHeader?> ReadHeaderAsync(CancellationToken cancellationToken = default)
            {
                var buffer = new byte[TcpCommon.BufferLength];
                var position = 0;
                var length = 0;
                try
                {
                    do
                    {
                        var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
                        if (read == 0)
                            return null;
                        length += read;
                    }
                    while (!TcpCommon.TryReadToHeaderEnd(buffer.AsSpan(0, length), ref position));
                }
                catch (IOException)
                {
                    return null;
                }
                return TcpCommon.ReadHeader(buffer.AsMemory(0, length), position);
            }

            public async Task<T?> ReadBodyAsync<T>(TcpRequestHeader header, IEncryptor? encryptor, CancellationToken cancellationToken = default)
            {
                await using var body = OpenBody(header, encryptor);
                return await serializer.DeserializeAsync<T>(body, cancellationToken);
            }

            public async Task<byte[]> ReadBodyBytesAsync(TcpRequestHeader header, IEncryptor? encryptor, CancellationToken cancellationToken = default)
            {
                await using var body = OpenBody(header, encryptor);
                using var ms = new MemoryStream();
                await body.CopyToAsync(ms, cancellationToken);
                return ms.ToArray();
            }

            public async Task<RemoteServiceException> ReadErrorAsync(TcpRequestHeader header, IEncryptor? encryptor, CancellationToken cancellationToken = default)
            {
                await using var body = OpenBody(header, encryptor);
                return await ExceptionSerializer.DeserializeAsync("test", serializer, body, cancellationToken);
            }

            private Stream OpenBody(TcpRequestHeader header, IEncryptor? encryptor)
            {
                Stream body = new TcpProtocolBodyStream(stream, header.BodyStartBuffer, false, true);
                if (encryptor is not null)
                    body = encryptor.Decrypt(body, false);
                return body;
            }

            public ValueTask DisposeAsync() => stream.DisposeAsync();
        }

        public interface ITestQueryHandler : IQueryHandler
        {
            int GetThings(int value);
            Stream GetStream();
        }

        public interface IOtherQueryHandler : IQueryHandler
        {
            int GetOther();
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

        public sealed class OtherCommand : ICommand
        {
            public int Value { get; set; }
        }

        public interface ITestMessageHandler :
            ICommandHandler<TestCommand>,
            ICommandHandler<TestCommandWithResult, int>,
            IEventHandler<TestEvent>
        { }
    }
}
