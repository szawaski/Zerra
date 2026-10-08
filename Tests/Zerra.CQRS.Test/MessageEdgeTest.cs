// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Security.Claims;
using Xunit;
using Zerra.Reflection;

namespace Zerra.CQRS.Test
{
    /// <summary>
    /// What every transport does the same at its edges: registration rules, unregistered messages, environments, and claims on every kind of message.
    /// </summary>
    public static class MessageEdgeTest
    {
        private const int maxConcurrent = 4;
        private const string source = "EdgeSource";
        private const string claimType = "edge-claim";
        private static readonly TimeSpan messageTimeout = TimeSpan.FromSeconds(60);

        private static void RegisterTypes()
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));
            TypeFinder.Register(typeof(TestEvent));
        }

        /// <summary>
        /// Consumers must be set up before registering and refuse a second type with the same name, producers refuse messages they don't have registered.
        /// </summary>
        public static async Task TestRegistrationRules(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, CancellationToken cancellationToken)
        {
            RegisterTypes();
            Assert.False(String.IsNullOrWhiteSpace(commandProducer.MessageHost));
            Assert.False(String.IsNullOrWhiteSpace(eventProducer.MessageHost));
            Assert.False(String.IsNullOrWhiteSpace(commandConsumer.MessageHost));
            Assert.False(String.IsNullOrWhiteSpace(eventConsumer.MessageHost));

            _ = Assert.ThrowsAny<Exception>(() => commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand)));
            _ = Assert.ThrowsAny<Exception>(() => eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica));

            commandConsumer.Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
            eventConsumer.Setup("EdgeService", (_, _) => Task.CompletedTask);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
            _ = Assert.Throws<InvalidOperationException>(() => commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(Other.TestCommand)));
            _ = Assert.Throws<InvalidOperationException>(() => eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(Other.TestEvent), EventConsumerMode.PerReplica));

            _ = await Assert.ThrowsAnyAsync<Exception>(() => commandProducer.DispatchAsync(new TestCommand(), source, cancellationToken));
            _ = await Assert.ThrowsAnyAsync<Exception>(() => commandProducer.DispatchAwaitAsync(new TestCommand(), source, cancellationToken));
            _ = await Assert.ThrowsAnyAsync<Exception>(() => commandProducer.DispatchAwaitAsync(new TestCommandWithResult(), source, cancellationToken));
            _ = await Assert.ThrowsAnyAsync<Exception>(() => eventProducer.DispatchAsync(new TestEvent(), source, cancellationToken));
        }

        /// <summary>
        /// Messages sent to the topics from outside Zerra that can't be read are logged and skipped, the messages after them still arrive.
        /// </summary>
        /// <param name="buildBody">The transport's message with a type, data, and source, serialized.</param>
        /// <param name="sendRawCommand">Sends a body as it is to the command topic.</param>
        /// <param name="sendRawEvent">Sends a body as it is to the event topic.</param>
        /// <param name="extraErrors">Sends anything else the transport rejects, returning how many errors it causes.</param>
        public static async Task TestMalformedMessages(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, Zerra.Serialization.ISerializer serializer, TestLogger log,
            Func<string?, byte[]?, string?, byte[]> buildBody, Func<byte[], Task> sendRawCommand, Func<byte[], Task> sendRawEvent, Func<Task<int>>? extraErrors, CancellationToken cancellationToken)
        {
            RegisterTypes();
            var received = new ConcurrentDictionary<Guid, TaskCompletionSource>();
            TaskCompletionSource Expect(Guid id) => received.GetOrAdd(id, static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

            commandConsumer.Setup(null,
                (command, _, _) => { Expect(((TestCommand)command).ID).TrySetResult(); return Task.CompletedTask; },
                (command, _, _) => { Expect(((TestCommand)command).ID).TrySetResult(); return Task.CompletedTask; },
                (command, _, _) => Task.FromResult<object?>(null));
            eventConsumer.Setup("EdgeService", (@event, eventSource) => { Expect(((TestEvent)@event).ID).TrySetResult(); return Task.CompletedTask; });
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            async Task SendUntilReceived(bool isEvent)
            {
                for (var i = 0; i < 60; i++)
                {
                    var id = Guid.NewGuid();
                    if (isEvent)
                        await eventProducer.DispatchAsync(new TestEvent { ID = id }, source, cancellationToken);
                    else
                        await commandProducer.DispatchAsync(new TestCommand { ID = id }, source, cancellationToken);
                    try
                    {
                        await Expect(id).Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
                        return;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                Assert.Fail($"{(isEvent ? "event" : "command")} not received");
            }

            commandConsumer.Open();
            eventConsumer.Open();
            try
            {
                await SendUntilReceived(false);
                await SendUntilReceived(true);
                var errorsBefore = log.Errors;

                var commandData = serializer.SerializeBytes(new TestCommand { ID = Guid.NewGuid() });
                var eventData = serializer.SerializeBytes(new TestEvent { ID = Guid.NewGuid() });
                //a type it doesn't know, no source, and data that isn't a message
                await sendRawCommand(buildBody("UnknownCommand", commandData, source));
                await sendRawCommand(buildBody(nameof(TestCommand), commandData, null));
                await sendRawCommand(buildBody(nameof(TestCommand), serializer.SerializeBytes<TestCommand?>(null), source));
                await sendRawEvent(buildBody("UnknownEvent", eventData, source));
                await sendRawEvent(buildBody(nameof(TestEvent), eventData, null));
                await sendRawEvent(buildBody(nameof(TestEvent), serializer.SerializeBytes<TestEvent?>(null), source));
                var expectedErrors = 6;
                if (extraErrors is not null)
                    expectedErrors += await extraErrors();

                await SendUntilReceived(false);
                await SendUntilReceived(true);

                for (var i = 0; i < 100 && log.Errors - errorsBefore < expectedErrors; i++)
                    await Task.Delay(100, cancellationToken);
                Assert.True(log.Errors - errorsBefore >= expectedErrors, $"{log.Errors - errorsBefore} errors, expected {expectedErrors}");
            }
            finally
            {
                commandConsumer.Close();
                eventConsumer.Close();
            }
        }

        /// <summary>
        /// A consumer limited to two commands answers awaited ones before it reports the limit is reached.
        /// </summary>
        public static async Task TestReceiveLimitAwaited(ICommandProducer commandProducer, ICommandConsumer commandConsumer, string commandTopic, CancellationToken cancellationToken)
        {
            RegisterTypes();
            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handled = 0;
            commandConsumer.Setup(new CommandCounter(2, () => reached.TrySetResult()),
                (_, _, _) => { _ = Interlocked.Increment(ref handled); return Task.CompletedTask; },
                (_, _, _) => { _ = Interlocked.Increment(ref handled); return Task.CompletedTask; },
                (command, _, _) => Task.FromResult<object?>(((TestCommandWithResult)command).Value));
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));

            commandConsumer.Open();
            try
            {
                await commandProducer.DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);
                Assert.Equal(5, await commandProducer.DispatchAwaitAsync(new TestCommandWithResult { ID = Guid.NewGuid(), Value = 5 }, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken));
                await reached.Task.WaitAsync(messageTimeout, cancellationToken);
                Assert.Equal(1, Volatile.Read(ref handled));
            }
            finally
            {
                commandConsumer.Close();
            }
        }

        /// <summary>
        /// A command with a result as the first thing a producer sends, then a command and an event, all carrying the sender's claims.
        /// </summary>
        public static async Task TestRoundTrip(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, CancellationToken cancellationToken)
        {
            RegisterTypes();
            var received = new ConcurrentDictionary<Guid, TaskCompletionSource<string?>>();
            TaskCompletionSource<string?> Expect(Guid id) => received.GetOrAdd(id, static _ => new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously));
            static string? Claim() => (Thread.CurrentPrincipal as ClaimsPrincipal)?.FindFirst(claimType)?.Value;

            commandConsumer.Setup(null,
                (command, _, _) => { _ = Expect(((TestCommand)command).ID).TrySetResult(Claim()); return Task.CompletedTask; },
                (command, _, _) => { _ = Expect(((TestCommand)command).ID).TrySetResult(Claim()); return Task.CompletedTask; },
                (command, _, _) => { var c = (TestCommandWithResult)command; _ = Expect(c.ID).TrySetResult(Claim()); return Task.FromResult<object?>(c.Value + 1); });
            eventConsumer.Setup("EdgeService", (@event, eventSource) => { _ = Expect(((TestEvent)@event).ID).TrySetResult(Claim()); return Task.CompletedTask; });
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            commandConsumer.Open();
            eventConsumer.Open();
            var previousPrincipal = Thread.CurrentPrincipal;
            try
            {
                var claimValue = Guid.NewGuid().ToString("N");
                Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, claimValue)], "Test"));

                var withResult = new TestCommandWithResult { ID = Guid.NewGuid(), Value = 41 };
                Assert.Equal(42, await commandProducer.DispatchAwaitAsync(withResult, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken));
                Assert.Equal(claimValue, await Expect(withResult.ID).Task.WaitAsync(messageTimeout, cancellationToken));

                var command = new TestCommand { ID = Guid.NewGuid() };
                await commandProducer.DispatchAsync(command, source, cancellationToken);
                Assert.Equal(claimValue, await Expect(command.ID).Task.WaitAsync(messageTimeout, cancellationToken));

                //an event subscription can start after the event is sent, it's sent until one arrives
                var arrived = false;
                for (var i = 0; i < 30 && !arrived; i++)
                {
                    var @event = new TestEvent { ID = Guid.NewGuid() };
                    await eventProducer.DispatchAsync(@event, source, cancellationToken);
                    try
                    {
                        Assert.Equal(claimValue, await Expect(@event.ID).Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
                        arrived = true;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                Assert.True(arrived);
            }
            finally
            {
                Thread.CurrentPrincipal = previousPrincipal;
                commandConsumer.Close();
                eventConsumer.Close();
            }
        }
    }
}

namespace Zerra.CQRS.Test.Other
{
    //the same names as the test messages, a transport sends a message's name so it can't tell them apart
    public sealed class TestCommand : ICommand { }
    public sealed class TestEvent : IEvent { }
}
