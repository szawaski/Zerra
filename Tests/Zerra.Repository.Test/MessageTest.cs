// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Reflection;

namespace Zerra.Repository.Test
{
    /// <summary>
    /// The shared sequence every messaging transport runs, only through the producer and consumer interfaces.
    /// </summary>
    public static class MessageTest
    {
        private const string source = "Zerra.Repository.Test";
        private const int maxConcurrent = 10;
        private const string claimType = "ZerraMessageTest";
        private const string serviceName = "ZerraTestService";
        /// <summary>
        /// The service names <see cref="TestEventConsumerModePerService"/> registers under, which a transport's cleanup may need to name its subscriptions.
        /// </summary>
        public const string ServiceAName = "ZerraTestServiceA";
        /// <inheritdoc cref="ServiceAName" />
        public const string ServiceBName = "ZerraTestServiceB";

        //a consumer subscribes in the background after Open, and messages sent before it's listening can be missed, so readiness is retried
        private static readonly TimeSpan readyTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan readyAttemptTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan messageTimeout = TimeSpan.FromSeconds(30);
        //long enough for a duplicate copy of an event to show up after the first one did
        private static readonly TimeSpan settleDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        /// A topic name unique to this run so leftovers from other runs are never received, short enough for every transport's name limit.
        /// </summary>
        public static string NewTopic(string name) => $"ZerraTest-{name}-{Guid.NewGuid().ToString("N")[..12]}";

        public static async Task TestSequence(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, CancellationToken cancellationToken)
        {
            //messages carry their type by name, an application's source generation registers its message types, this project doesn't use it
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));
            TypeFinder.Register(typeof(TestEvent));

            var receiver = new Receiver();

            commandConsumer.Setup(new CommandCounter(), receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            eventConsumer.Setup(serviceName, receiver.HandleEventAsync);
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);

            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            commandConsumer.Open();
            eventConsumer.Open();
            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);
                await WaitForEventConsumer(eventProducer, receiver, cancellationToken);

                await TestDispatchAsync(commandProducer, receiver, cancellationToken);
                await TestDispatchAwaitAsync(commandProducer, receiver, cancellationToken);
                await TestDispatchAwaitAsyncWithResult(commandProducer, receiver, cancellationToken);
                await TestDispatchAwaitAsyncError(commandProducer, receiver, cancellationToken);
                await TestDispatchAwaitAsyncWithResultError(commandProducer, receiver, cancellationToken);
                await TestDispatchAwaitAsyncConcurrent(commandProducer, cancellationToken);
                await TestClaims(commandProducer, receiver, cancellationToken);
                await TestEvent(eventProducer, receiver, cancellationToken);
                await TestEventsConcurrent(eventProducer, receiver, cancellationToken);
            }
            finally
            {
                commandConsumer.Close();
                eventConsumer.Close();
            }
        }

        private static Task WaitForCommandConsumer(ICommandProducer producer, CancellationToken cancellationToken)
        {
            return RetryUntilReady("Command consumer", cancellationToken, (attemptCancellationToken) =>
                producer.DispatchAwaitAsync(new TestCommand() { ID = Guid.NewGuid() }, source, attemptCancellationToken));
        }

        private static Task WaitForEventConsumer(IEventProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            return RetryUntilReady("Event consumer", cancellationToken, async (attemptCancellationToken) =>
            {
                var @event = new TestEvent() { ID = Guid.NewGuid() };
                var received = receiver.Expect(@event.ID);
                await producer.DispatchAsync(@event, source, attemptCancellationToken);
                _ = await received.WaitAsync(attemptCancellationToken);
            });
        }

        private static async Task RetryUntilReady(string name, CancellationToken cancellationToken, Func<CancellationToken, Task> attempt)
        {
            var timer = Stopwatch.StartNew();
            for (; ; )
            {
                using var canceller = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                canceller.CancelAfter(readyAttemptTimeout);
                try
                {
                    await attempt(canceller.Token);
                    return;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested || timer.Elapsed >= readyTimeout)
                        throw new TimeoutException($"{name} was not ready after {readyTimeout.TotalSeconds} seconds", ex);
                }
                await Task.Delay(500, cancellationToken);
            }
        }

        private static async Task TestDispatchAsync(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommand() { ID = Guid.NewGuid(), Value = 11, Text = "DispatchAsync" };
            var received = receiver.Expect(command.ID);

            await producer.DispatchAsync(command, source, cancellationToken);

            var result = await received.WaitAsync(messageTimeout, cancellationToken);
            Assert.Equal(HandlerType.CommandAsync, result.Handler);
            Assert.Equal(source, result.Source);
            var receivedCommand = Assert.IsType<TestCommand>(result.Message);
            Assert.Equal(command.Value, receivedCommand.Value);
            Assert.Equal(command.Text, receivedCommand.Text);
        }

        private static async Task TestDispatchAwaitAsync(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommand() { ID = Guid.NewGuid(), Value = 22, Text = "DispatchAwaitAsync" };
            var received = receiver.Expect(command.ID);

            await producer.DispatchAwaitAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);

            //awaiting the dispatch means the handler already ran
            Assert.True(received.IsCompleted);
            var result = await received;
            Assert.Equal(HandlerType.CommandAwaitAsync, result.Handler);
            Assert.Equal(source, result.Source);
            var receivedCommand = Assert.IsType<TestCommand>(result.Message);
            Assert.Equal(command.Value, receivedCommand.Value);
            Assert.Equal(command.Text, receivedCommand.Text);
        }

        private static async Task TestDispatchAwaitAsyncWithResult(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommandWithResult() { ID = Guid.NewGuid(), Value = 33 };
            var received = receiver.Expect(command.ID);

            var result = await producer.DispatchAwaitAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);

            Assert.Equal(command.Value * 2, result);
            Assert.True(received.IsCompleted);
            var receivedResult = await received;
            Assert.Equal(HandlerType.CommandWithResult, receivedResult.Handler);
            Assert.Equal(source, receivedResult.Source);
        }

        private static async Task TestDispatchAwaitAsyncError(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommand() { ID = Guid.NewGuid(), Throw = true };
            var received = receiver.Expect(command.ID);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() => producer.DispatchAwaitAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken));

            Assert.Equal(ErrorMessage(command.ID), exception.Message);
            Assert.True(received.IsCompleted);
        }

        private static async Task TestDispatchAwaitAsyncWithResultError(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommandWithResult() { ID = Guid.NewGuid(), Throw = true };
            var received = receiver.Expect(command.ID);

            var exception = await Assert.ThrowsAsync<RemoteServiceException>(() => producer.DispatchAwaitAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken));

            Assert.Equal(ErrorMessage(command.ID), exception.Message);
            Assert.True(received.IsCompleted);
        }

        private static async Task TestDispatchAwaitAsyncConcurrent(ICommandProducer producer, CancellationToken cancellationToken)
        {
            //more than maxConcurrent so the producer and consumer throttles are exercised
            var commands = Enumerable.Range(0, maxConcurrent * 3).Select(x => new TestCommandWithResult() { ID = Guid.NewGuid(), Value = x }).ToArray();

            var results = await Task.WhenAll(commands.Select(x => producer.DispatchAwaitAsync(x, source, cancellationToken))).WaitAsync(messageTimeout, cancellationToken);

            for (var i = 0; i < commands.Length; i++)
                Assert.Equal(commands[i].Value * 2, results[i]);
        }

        private static async Task TestClaims(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var claimValue = Guid.NewGuid().ToString("N");
            var command = new TestCommand() { ID = Guid.NewGuid() };
            var received = receiver.Expect(command.ID);

            var previousPrincipal = Thread.CurrentPrincipal;
            Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, claimValue)], "Test"));
            try
            {
                await producer.DispatchAwaitAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);
            }
            finally
            {
                Thread.CurrentPrincipal = previousPrincipal;
            }

            var result = await received;
            Assert.Equal(claimValue, result.Claim);
        }

        private static async Task TestEvent(IEventProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var @event = new TestEvent() { ID = Guid.NewGuid(), Value = 44, Text = "Event" };
            var received = receiver.Expect(@event.ID);

            await producer.DispatchAsync(@event, source, cancellationToken);

            var result = await received.WaitAsync(messageTimeout, cancellationToken);
            Assert.Equal(HandlerType.Event, result.Handler);
            Assert.Equal(source, result.Source);
            var receivedEvent = Assert.IsType<TestEvent>(result.Message);
            Assert.Equal(@event.Value, receivedEvent.Value);
            Assert.Equal(@event.Text, receivedEvent.Text);
        }

        private static async Task TestEventsConcurrent(IEventProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var events = Enumerable.Range(0, maxConcurrent * 3).Select(x => new TestEvent() { ID = Guid.NewGuid(), Value = x }).ToArray();
            var received = events.Select(x => receiver.Expect(x.ID)).ToArray();

            await Task.WhenAll(events.Select(x => producer.DispatchAsync(x, source, cancellationToken))).WaitAsync(messageTimeout, cancellationToken);

            var results = await Task.WhenAll(received).WaitAsync(messageTimeout, cancellationToken);
            for (var i = 0; i < events.Length; i++)
                Assert.Equal(events[i].Value, Assert.IsType<TestEvent>(results[i].Message).Value);
        }

        /// <summary>
        /// The sequence for <see cref="EventConsumerMode.PerService"/>, only through the producer and consumer interfaces.
        /// The first two consumers stand in for two replicas of one service and share each event between them,
        /// the third is another service subscribed to the same events and gets its own copy of every one.
        /// </summary>
        public static async Task TestEventConsumerModePerService(IEventProducer eventProducer, IEventConsumer serviceAReplica1, IEventConsumer serviceAReplica2, IEventConsumer serviceB, string eventTopic, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestEvent));

            var receivedA1 = new EventReceiver();
            var receivedA2 = new EventReceiver();
            var receivedB = new EventReceiver();

            serviceAReplica1.Setup(ServiceAName, receivedA1.HandleEventAsync);
            serviceAReplica1.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerService);
            serviceAReplica2.Setup(ServiceAName, receivedA2.HandleEventAsync);
            serviceAReplica2.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerService);
            serviceB.Setup(ServiceBName, receivedB.HandleEventAsync);
            serviceB.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerService);

            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            serviceAReplica1.Open();
            serviceAReplica2.Open();
            serviceB.Open();
            try
            {
                //the replicas share a subscription, so one of them receiving the probe means the service is listening
                await RetryUntilReady("Event consumers", cancellationToken, async (attemptCancellationToken) =>
                {
                    await eventProducer.DispatchAsync(new TestEvent() { ID = Guid.NewGuid() }, source, attemptCancellationToken);
                    await WaitUntil(() => receivedA1.Count + receivedA2.Count > 0 && receivedB.Count > 0, readyAttemptTimeout, attemptCancellationToken);
                });

                var events = Enumerable.Range(0, maxConcurrent * 3).Select(x => new TestEvent() { ID = Guid.NewGuid(), Value = x }).ToArray();
                var expected = events.Select(x => x.ID).OrderBy(x => x).ToArray();

                await Task.WhenAll(events.Select(x => eventProducer.DispatchAsync(x, source, cancellationToken))).WaitAsync(messageTimeout, cancellationToken);

                //at least, so a duplicate is caught by the assertion below rather than making the wait time out
                await WaitUntil(() => receivedB.Received(expected).Count >= expected.Length, messageTimeout, cancellationToken);
                await WaitUntil(() => receivedA1.Received(expected).Count + receivedA2.Received(expected).Count >= expected.Length, messageTimeout, cancellationToken);

                //a second copy of an event would arrive after the first, so the counts are only trustworthy once everything has settled
                await Task.Delay(settleDelay, cancellationToken);

                //the other service is subscribed on its own, so it gets every event
                Assert.Equal(expected, receivedB.Received(expected));
                //the replicas compete, so between them they get every event exactly once
                Assert.Equal(expected, receivedA1.Received(expected).Concat(receivedA2.Received(expected)).OrderBy(x => x));
            }
            finally
            {
                serviceAReplica1.Close();
                serviceAReplica2.Close();
                serviceB.Close();
            }
        }

        private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            while (!condition())
            {
                if (timer.Elapsed >= timeout)
                    throw new TimeoutException($"The condition was not met after {timeout.TotalSeconds} seconds");
                await Task.Delay(100, cancellationToken);
            }
        }

        private static string ErrorMessage(Guid id) => $"Test Error {id}";

        private enum HandlerType
        {
            CommandAsync,
            CommandAwaitAsync,
            CommandWithResult,
            Event
        }

        private sealed record Received(object Message, string Source, HandlerType Handler, string? Claim);

        //records every event ID it was given, including duplicates, so a test can count the copies a consumer received
        private sealed class EventReceiver
        {
            private readonly ConcurrentQueue<Guid> ids = new();

            public int Count => ids.Count;

            //the probe events sent while waiting for the consumers to be listening are not part of any expectation
            public List<Guid> Received(IReadOnlyCollection<Guid> expected) => ids.Where(expected.Contains).OrderBy(x => x).ToList();

            public Task HandleEventAsync(IEvent @event, string source)
            {
                ids.Enqueue(Assert.IsType<TestEvent>(@event).ID);
                return Task.CompletedTask;
            }
        }

        //records what each handler received by message ID, so a test can wait for exactly its own message
        private sealed class Receiver
        {
            private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Received>> received = new();

            public Task<Received> Expect(Guid id) => GetCompletion(id).Task;

            private TaskCompletionSource<Received> GetCompletion(Guid id) => received.GetOrAdd(id, static _ => new TaskCompletionSource<Received>(TaskCreationOptions.RunContinuationsAsynchronously));

            private void Complete(Guid id, object message, string source, HandlerType handler)
            {
                var claim = (Thread.CurrentPrincipal as ClaimsPrincipal)?.FindFirst(claimType)?.Value;
                _ = GetCompletion(id).TrySetResult(new Received(message, source, handler, claim));
            }

            public Task HandleCommandAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommand>(command);
                Complete(testCommand.ID, command, source, HandlerType.CommandAsync);
                return Task.CompletedTask;
            }

            public Task HandleCommandAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommand>(command);
                Complete(testCommand.ID, command, source, HandlerType.CommandAwaitAsync);
                if (testCommand.Throw)
                    throw new InvalidOperationException(ErrorMessage(testCommand.ID));
                return Task.CompletedTask;
            }

            public Task<object?> HandleCommandWithResultAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommandWithResult>(command);
                Complete(testCommand.ID, command, source, HandlerType.CommandWithResult);
                if (testCommand.Throw)
                    throw new InvalidOperationException(ErrorMessage(testCommand.ID));
                return Task.FromResult<object?>(testCommand.Value * 2);
            }

            public Task HandleEventAsync(IEvent @event, string source)
            {
                var testEvent = Assert.IsType<TestEvent>(@event);
                Complete(testEvent.ID, @event, source, HandlerType.Event);
                return Task.CompletedTask;
            }
        }
    }
}
