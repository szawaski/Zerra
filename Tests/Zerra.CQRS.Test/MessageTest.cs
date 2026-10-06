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

namespace Zerra.CQRS.Test
{
    /// <summary>
    /// The shared sequence every messaging transport runs, only through the producer and consumer interfaces.
    /// </summary>
    public static class MessageTest
    {
        private const string source = "Zerra.CQRS.Test";
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

            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync);
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
                await TestDispatchAwaitAsyncCanceled(commandProducer, receiver, cancellationToken);
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

        /// <summary>
        /// A command sent before any consumer for it has run: the producer creates where it goes, so it waits there until a consumer starts.
        /// </summary>
        public static async Task TestCommandSentBeforeConsumer(ICommandProducer commandProducer, ICommandConsumer commandConsumer, string commandTopic, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));

            var receiver = new Receiver();

            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            var command = new TestCommand() { ID = Guid.NewGuid(), Value = 23, Text = "SentBeforeConsumer" };
            var received = receiver.Expect(command.ID);
            await commandProducer.DispatchAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);

            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandConsumer.Open();
            try
            {
                var result = await received.WaitAsync(readyTimeout, cancellationToken);
                Assert.Equal(HandlerType.CommandAsync, result.Handler);
                Assert.Equal(command.Text, Assert.IsType<TestCommand>(result.Message).Text);
            }
            finally
            {
                commandConsumer.Close();
            }
        }

        /// <summary>
        /// One consumer for two command topics and an event topic, set up and opened in the order the bus does when it's added for each interface.
        /// </summary>
        public static async Task TestSharedConsumer<TConsumer>(ICommandProducer commandProducer, IEventProducer eventProducer, TConsumer consumer, string commandTopicA, string commandTopicB, string eventTopic, bool eventFirst, CancellationToken cancellationToken)
            where TConsumer : ICommandConsumer, IEventConsumer
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));
            TypeFinder.Register(typeof(TestEvent));

            var receiver = new Receiver();
            var commandConsumer = (ICommandConsumer)consumer;
            var eventConsumer = (IEventConsumer)consumer;

            if (eventFirst)
            {
                eventConsumer.Setup(serviceName, receiver.HandleEventAsync);
                eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
                eventConsumer.Open();
            }
            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopicA, typeof(TestCommand));
            commandConsumer.Open();
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopicB, typeof(TestCommandWithResult));
            commandConsumer.Open();
            if (!eventFirst)
            {
                eventConsumer.Setup(serviceName, receiver.HandleEventAsync);
                eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
                eventConsumer.Open();
            }

            _ = Assert.Throws<InvalidOperationException>(() => commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync));
            _ = Assert.Throws<InvalidOperationException>(() => eventConsumer.Setup(serviceName, receiver.HandleEventAsync));

            commandProducer.RegisterCommandType(maxConcurrent, commandTopicA, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopicB, typeof(TestCommandWithResult));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);
                await RetryUntilReady("Second command consumer", cancellationToken, (attemptCancellationToken) =>
                    commandProducer.DispatchAwaitAsync(new TestCommandWithResult() { ID = Guid.NewGuid() }, source, attemptCancellationToken));
                await WaitForEventConsumer(eventProducer, receiver, cancellationToken);
            }
            finally
            {
                commandConsumer.Close();
                eventConsumer.Close();
            }
        }

        /// <summary>
        /// Two replicas that each receive one command before exiting. The one that received a command still handling it doesn't hold up the other command.
        /// </summary>
        public static async Task TestReceiveLimitHandsOff(ICommandProducer commandProducer, ICommandConsumer replica1, ICommandConsumer replica2, string commandTopic, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestCommand));

            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var received1 = new BlockingReceiver(release.Task);
            var received2 = new BlockingReceiver(release.Task);

            replica1.Setup(new CommandCounter(1, () => { }), received1.HandleCommandAsync, received1.HandleCommandAsync, received1.HandleCommandWithResultAwaitAsync);
            replica1.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            replica2.Setup(new CommandCounter(1, () => { }), received2.HandleCommandAsync, received2.HandleCommandAsync, received2.HandleCommandWithResultAwaitAsync);
            replica2.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));

            replica1.Open();
            replica2.Open();
            try
            {
                var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                foreach (var id in ids)
                    await commandProducer.DispatchAsync(new TestCommand() { ID = id }, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);

                await WaitUntil(() => received1.IDs.Count + received2.IDs.Count == 2, readyTimeout, cancellationToken);
                Assert.Equal(ids.OrderBy(x => x), received1.IDs.Concat(received2.IDs).OrderBy(x => x));
                _ = Assert.Single(received1.IDs);
                _ = Assert.Single(received2.IDs);
            }
            finally
            {
                release.SetResult();
                replica1.Close();
                replica2.Close();
            }
        }

        /// <summary>
        /// Sustained traffic from one producer to two replicas. Every command is handled once by one of them, every event once by each,
        /// each awaited command gets its own result back, and neither replica runs more handlers at once than it was registered for.
        /// </summary>
        public static async Task TestSustainedLoad<TConsumer>(ICommandProducer commandProducer, IEventProducer eventProducer, TConsumer replica1, TConsumer replica2, string commandTopic, string eventTopic, CancellationToken cancellationToken)
            where TConsumer : ICommandConsumer, IEventConsumer
        {
            const int commandCount = 1000;
            const int commandWithResultCount = 200;
            const int eventCount = 500;
            var loadTimeout = TimeSpan.FromMinutes(3);

            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));
            TypeFinder.Register(typeof(TestEvent));

            var received1 = new LoadReceiver();
            var received2 = new LoadReceiver();
            foreach (var (replica, received) in new[] { (replica1, received1), (replica2, received2) })
            {
                ((ICommandConsumer)replica).Setup(null, received.HandleCommandAsync, received.HandleCommandAsync, received.HandleCommandWithResultAsync);
                ((ICommandConsumer)replica).RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
                ((ICommandConsumer)replica).RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
                ((IEventConsumer)replica).Setup(serviceName, received.HandleEventAsync);
                ((IEventConsumer)replica).RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);
            }
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommandWithResult));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            ((ICommandConsumer)replica1).Open();
            ((IEventConsumer)replica1).Open();
            ((ICommandConsumer)replica2).Open();
            ((IEventConsumer)replica2).Open();
            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);
                //each replica has its own subscription, both have to be listening before the events are counted
                await RetryUntilReady("Event consumers", cancellationToken, async (attemptCancellationToken) =>
                {
                    await eventProducer.DispatchAsync(new TestEvent() { ID = Guid.NewGuid() }, source, attemptCancellationToken);
                    await WaitUntil(() => received1.EventCount > 0 && received2.EventCount > 0, readyAttemptTimeout, attemptCancellationToken);
                });

                var commands = Enumerable.Range(0, commandCount).Select(x => new TestCommand() { ID = Guid.NewGuid(), Value = x }).ToArray();
                var commandsWithResult = Enumerable.Range(0, commandWithResultCount).Select(x => new TestCommandWithResult() { ID = Guid.NewGuid(), Value = x }).ToArray();
                var events = Enumerable.Range(0, eventCount).Select(x => new TestEvent() { ID = Guid.NewGuid(), Value = x }).ToArray();

                var sendingCommands = Task.WhenAll(commands.Select(x => commandProducer.DispatchAsync(x, source, cancellationToken)));
                var sendingEvents = Task.WhenAll(events.Select(x => eventProducer.DispatchAsync(x, source, cancellationToken)));
                var results = await Task.WhenAll(commandsWithResult.Select(x => commandProducer.DispatchAwaitAsync(x, source, cancellationToken))).WaitAsync(loadTimeout, cancellationToken);
                await Task.WhenAll(sendingCommands, sendingEvents).WaitAsync(loadTimeout, cancellationToken);

                for (var i = 0; i < commandsWithResult.Length; i++)
                    Assert.Equal(commandsWithResult[i].Value * 2, results[i]);

                var commandIDs = commands.Select(x => x.ID).Concat(commandsWithResult.Select(x => x.ID)).ToArray();
                var eventIDs = events.Select(x => x.ID).ToArray();
                //at least, so a duplicate is caught by the assertions below rather than making the wait time out
                await WaitUntil(() => received1.CommandsReceived(commandIDs) + received2.CommandsReceived(commandIDs) >= commandIDs.Length, loadTimeout, cancellationToken);
                await WaitUntil(() => received1.EventsReceived(eventIDs) >= eventIDs.Length && received2.EventsReceived(eventIDs) >= eventIDs.Length, loadTimeout, cancellationToken);
                await Task.Delay(settleDelay, cancellationToken);

                foreach (var id in commandIDs)
                    Assert.Equal(1, received1.CommandTimes(id) + received2.CommandTimes(id));
                foreach (var id in eventIDs)
                {
                    Assert.Equal(1, received1.EventTimes(id));
                    Assert.Equal(1, received2.EventTimes(id));
                }

                foreach (var received in new[] { received1, received2 })
                {
                    Assert.InRange(received.MaxCommandsHandling, 0, maxConcurrent);
                    Assert.InRange(received.MaxEventsHandling, 0, maxConcurrent);
                }
                //handlers overlapped, how many depends on how fast the transport receives
                Assert.True(Math.Max(received1.MaxCommandsHandling, received2.MaxCommandsHandling) > 1);
                Assert.True(Math.Max(received1.MaxEventsHandling, received2.MaxEventsHandling) > 1);
            }
            finally
            {
                ((ICommandConsumer)replica1).Close();
                ((IEventConsumer)replica1).Close();
                ((ICommandConsumer)replica2).Close();
                ((IEventConsumer)replica2).Close();
            }
        }

        /// <summary>
        /// The command topic is deleted outside of Zerra while a consumer is running. The consumer fails, forgets the topic it had listed as existing,
        /// and creates it again, after which commands are received again.
        /// </summary>
        public static async Task TestConsumesAgainAfterTopicDeleted(ICommandProducer commandProducer, ICommandConsumer commandConsumer, string commandTopic, Func<Task> deleteTopicOutsideZerra, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));

            var receiver = new Receiver();

            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAwaitAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            commandConsumer.Open();
            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);

                await deleteTopicOutsideZerra();

                await RetryUntilReady("Command consumer after its topic was deleted", cancellationToken, (attemptCancellationToken) =>
                    commandProducer.DispatchAwaitAsync(new TestCommand() { ID = Guid.NewGuid() }, source, attemptCancellationToken));
            }
            finally
            {
                commandConsumer.Close();
            }
        }

        public static async Task TestHandlerErrorNotReceivedAgain(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestEvent));

            var receiver = new CountingReceiver();

            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventConsumer.Setup(ServiceAName, receiver.HandleEventAsync);
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerService);

            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            commandConsumer.Open();
            eventConsumer.Open();
            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);
                await RetryUntilReady("Event consumer", cancellationToken, async (attemptCancellationToken) =>
                {
                    var probe = new TestEvent() { ID = Guid.NewGuid() };
                    var received = receiver.Received(probe.ID);
                    await eventProducer.DispatchAsync(probe, source, attemptCancellationToken);
                    await received.WaitAsync(attemptCancellationToken);
                });

                var command = new TestCommand() { ID = Guid.NewGuid(), Throw = true };
                var @event = new TestEvent() { ID = Guid.NewGuid(), Value = CountingReceiver.ThrowValue };
                var commandReceived = receiver.Received(command.ID);
                var eventReceived = receiver.Received(@event.ID);
                await commandProducer.DispatchAsync(command, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);
                await eventProducer.DispatchAsync(@event, source, cancellationToken).WaitAsync(messageTimeout, cancellationToken);
                await commandReceived.WaitAsync(messageTimeout, cancellationToken);
                await eventReceived.WaitAsync(messageTimeout, cancellationToken);

                await Task.Delay(settleDelay, cancellationToken);
                Assert.Equal(1, receiver.Count(command.ID));
                Assert.Equal(1, receiver.Count(@event.ID));
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

        //the caller stops waiting as soon as it cancels, and the acknowledgement that still arrives afterwards doesn't disturb the next command
        private static async Task TestDispatchAwaitAsyncCanceled(ICommandProducer producer, Receiver receiver, CancellationToken cancellationToken)
        {
            var command = new TestCommand() { ID = Guid.NewGuid(), Text = "DispatchAwaitAsyncCanceled", DelayMilliseconds = 3000 };
            var received = receiver.Expect(command.ID);

            using var canceller = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var dispatch = producer.DispatchAwaitAsync(command, source, canceller.Token);
            _ = await received.WaitAsync(messageTimeout, cancellationToken);

            var timer = Stopwatch.StartNew();
            canceller.Cancel();
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch.WaitAsync(messageTimeout, cancellationToken));
            Assert.True(timer.Elapsed < TimeSpan.FromMilliseconds(1500), $"Cancelling took {timer.ElapsedMilliseconds}ms, it waited for the acknowledgement");

            await Task.Delay(command.DelayMilliseconds, cancellationToken);
            await TestDispatchAwaitAsync(producer, receiver, cancellationToken);
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

        /// <summary>
        /// Closing a consumer stops it receiving, and disposing it returns once the command and event it was still handling have finished.
        /// </summary>
        public static async Task TestFinishesProcessingOnClose(ICommandProducer commandProducer, IEventProducer eventProducer, ICommandConsumer commandConsumer, IEventConsumer eventConsumer, string commandTopic, string eventTopic, bool disposeAsync, CancellationToken cancellationToken)
        {
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestCommandWithResult));
            TypeFinder.Register(typeof(TestEvent));

            var receiver = new SlowReceiver();

            commandConsumer.Setup(null, receiver.HandleCommandAsync, receiver.HandleCommandAsync, receiver.HandleCommandWithResultAwaitAsync);
            commandConsumer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventConsumer.Setup(serviceName, receiver.HandleEventAsync);
            eventConsumer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent), EventConsumerMode.PerReplica);

            commandProducer.RegisterCommandType(maxConcurrent, commandTopic, typeof(TestCommand));
            eventProducer.RegisterEventType(maxConcurrent, eventTopic, typeof(TestEvent));

            commandConsumer.Open();
            eventConsumer.Open();
            try
            {
                await WaitForCommandConsumer(commandProducer, cancellationToken);
                await RetryUntilReady("Event consumer", cancellationToken, async (attemptCancellationToken) =>
                {
                    var probe = new TestEvent() { ID = Guid.NewGuid() };
                    var finished = receiver.Finished(probe.ID);
                    await eventProducer.DispatchAsync(probe, source, attemptCancellationToken);
                    _ = await finished.WaitAsync(attemptCancellationToken);
                });

                //Value is how long each handler takes, the sender doesn't wait for either of them
                var command = new TestCommand() { ID = Guid.NewGuid(), Value = 2000 };
                var @event = new TestEvent() { ID = Guid.NewGuid(), Value = 2000 };
                var commandFinished = receiver.Finished(command.ID);
                var eventFinished = receiver.Finished(@event.ID);
                await commandProducer.DispatchAsync(command, source, cancellationToken);
                await eventProducer.DispatchAsync(@event, source, cancellationToken);
                await receiver.Started(command.ID).WaitAsync(messageTimeout, cancellationToken);
                await receiver.Started(@event.ID).WaitAsync(messageTimeout, cancellationToken);

                commandConsumer.Close();
                eventConsumer.Close();
                Assert.False(commandFinished.IsCompleted, "the command was still being handled when the consumer closed");

                //the command and event consumer are the same object for every transport
                if (disposeAsync)
                    await commandConsumer.DisposeAsync().AsTask().WaitAsync(messageTimeout, cancellationToken);
                else
                    await Task.Run(commandConsumer.Dispose, cancellationToken).WaitAsync(messageTimeout, cancellationToken);

                //both finished before disposing returned, and closing didn't cancel them
                Assert.True(commandFinished.IsCompleted);
                Assert.True(eventFinished.IsCompleted);
                Assert.False(await commandFinished, "the command handler was not cancelled");
                Assert.False(await eventFinished, "the event handler was not cancelled");
            }
            finally
            {
                commandConsumer.Close();
                eventConsumer.Close();
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

        private sealed class CountingReceiver
        {
            public const int ThrowValue = -1;

            private readonly ConcurrentDictionary<Guid, int> counts = new();
            private readonly ConcurrentDictionary<Guid, TaskCompletionSource> received = new();

            public Task Received(Guid id) => GetCompletion(id).Task;
            public int Count(Guid id) => counts.TryGetValue(id, out var count) ? count : 0;

            private TaskCompletionSource GetCompletion(Guid id) => received.GetOrAdd(id, static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

            private void Handle(Guid id, bool throws)
            {
                _ = counts.AddOrUpdate(id, 1, static (_, count) => count + 1);
                _ = GetCompletion(id).TrySetResult();
                if (throws)
                    throw new InvalidOperationException(ErrorMessage(id));
            }

            public Task HandleCommandAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommand>(command);
                Handle(testCommand.ID, testCommand.Throw);
                return Task.CompletedTask;
            }

            public Task<object?> HandleCommandWithResultAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            public Task HandleEventAsync(IEvent @event, string source)
            {
                var testEvent = Assert.IsType<TestEvent>(@event);
                Handle(testEvent.ID, testEvent.Value == ThrowValue);
                return Task.CompletedTask;
            }
        }

        //handlers that take as long as the message's Value in milliseconds, recording when each message ID starts and finishes and whether it was cancelled
        private sealed class SlowReceiver
        {
            private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> started = new();
            private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> finished = new();

            public Task<bool> Started(Guid id) => GetCompletion(started, id).Task;
            //the result is true if the handler's token was cancelled
            public Task<bool> Finished(Guid id) => GetCompletion(finished, id).Task;

            private static TaskCompletionSource<bool> GetCompletion(ConcurrentDictionary<Guid, TaskCompletionSource<bool>> completions, Guid id) => completions.GetOrAdd(id, static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

            private async Task Handle(Guid id, int delay, CancellationToken cancellationToken)
            {
                _ = GetCompletion(started, id).TrySetResult(true);
                var cancelled = false;
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                _ = GetCompletion(finished, id).TrySetResult(cancelled);
            }

            public Task HandleCommandAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommand>(command);
                return Handle(testCommand.ID, testCommand.Value, cancellationToken);
            }

            public Task<object?> HandleCommandWithResultAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            public Task HandleEventAsync(IEvent @event, string source)
            {
                var testEvent = Assert.IsType<TestEvent>(@event);
                return Handle(testEvent.ID, testEvent.Value, CancellationToken.None);
            }
        }

        //counts how many times each message is handled and the most handlers running at once, each taking a moment so they overlap
        private sealed class LoadReceiver
        {
            private readonly ConcurrentDictionary<Guid, int> commands = new();
            private readonly ConcurrentDictionary<Guid, int> events = new();
            private int commandsHandling;
            private int eventsHandling;
            private int maxCommandsHandling;
            private int maxEventsHandling;

            public int EventCount => events.Count;
            public int MaxCommandsHandling => Volatile.Read(ref maxCommandsHandling);
            public int MaxEventsHandling => Volatile.Read(ref maxEventsHandling);

            public int CommandsReceived(Guid[] ids) => ids.Count(commands.ContainsKey);
            public int EventsReceived(Guid[] ids) => ids.Count(events.ContainsKey);
            public int CommandTimes(Guid id) => commands.TryGetValue(id, out var times) ? times : 0;
            public int EventTimes(Guid id) => events.TryGetValue(id, out var times) ? times : 0;

            private static async Task Handle(ConcurrentDictionary<Guid, int> received, Guid id, Func<int> begin, Action end)
            {
                _ = received.AddOrUpdate(id, 1, static (_, times) => times + 1);
                _ = begin();
                try
                {
                    await Task.Delay(1);
                }
                finally
                {
                    end();
                }
            }

            private static int Begin(ref int handling, ref int max)
            {
                var now = Interlocked.Increment(ref handling);
                for (var current = Volatile.Read(ref max); now > current; current = Volatile.Read(ref max))
                {
                    if (Interlocked.CompareExchange(ref max, now, current) == current)
                        break;
                }
                return now;
            }

            public Task HandleCommandAsync(ICommand command, string source, CancellationToken cancellationToken)
                => Handle(commands, command is TestCommandWithResult withResult ? withResult.ID : Assert.IsType<TestCommand>(command).ID, () => Begin(ref commandsHandling, ref maxCommandsHandling), () => Interlocked.Decrement(ref commandsHandling));

            public async Task<object?> HandleCommandWithResultAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommandWithResult>(command);
                await Handle(commands, testCommand.ID, () => Begin(ref commandsHandling, ref maxCommandsHandling), () => Interlocked.Decrement(ref commandsHandling));
                return testCommand.Value * 2;
            }

            public Task HandleEventAsync(IEvent @event, string source)
                => Handle(events, Assert.IsType<TestEvent>(@event).ID, () => Begin(ref eventsHandling, ref maxEventsHandling), () => Interlocked.Decrement(ref eventsHandling));
        }

        private sealed class BlockingReceiver
        {
            private readonly Task release;
            public readonly ConcurrentQueue<Guid> IDs = new();

            public BlockingReceiver(Task release) => this.release = release;

            public Task HandleCommandAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                IDs.Enqueue(Assert.IsType<TestCommand>(command).ID);
                return release;
            }

            public Task<object?> HandleCommandWithResultAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
                => throw new NotSupportedException();
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

            public async Task HandleCommandAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
            {
                var testCommand = Assert.IsType<TestCommand>(command);
                Complete(testCommand.ID, command, source, HandlerType.CommandAwaitAsync);
                if (testCommand.DelayMilliseconds > 0)
                    await Task.Delay(testCommand.DelayMilliseconds);
                if (testCommand.Throw)
                    throw new InvalidOperationException(ErrorMessage(testCommand.ID));
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
