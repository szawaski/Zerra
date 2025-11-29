// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;

namespace Zerra.Test.CQRS
{
    public class CommandCounterTests
    {
        [Fact]
        public void ReceiveCountBeforeExit_Property_WhenUnlimited()
        {
            var counter = new CommandCounter();
            Assert.Null(counter.ReceiveCountBeforeExit);
        }

        [Fact]
        public void ReceiveCountBeforeExit_Property_WhenLimited()
        {
            var exitCalled = false;
            var counter = new CommandCounter(5, () => exitCalled = true);
            Assert.Equal(5, counter.ReceiveCountBeforeExit);
        }

        [Fact]
        public void BeginReceive_ReturnsTrue_WhenNoLimit()
        {
            var counter = new CommandCounter();
            Assert.True(counter.BeginReceive());
            Assert.True(counter.BeginReceive());
            Assert.True(counter.BeginReceive());
        }

        [Fact]
        public void BeginReceive_ReturnsTrue_UntilLimitReached()
        {
            var counter = new CommandCounter(3, () => { });
            Assert.True(counter.BeginReceive());
            Assert.True(counter.BeginReceive());
            Assert.True(counter.BeginReceive());
            Assert.False(counter.BeginReceive());
            Assert.False(counter.BeginReceive());
        }

        [Fact]
        public void CancelReceive_ReleasesThrottle_WhenNoLimit()
        {
            var counter = new CommandCounter();
            var throttle = new SemaphoreSlim(1);

            throttle.Wait();
            Assert.Equal(0, throttle.CurrentCount);

            counter.CancelReceive(throttle);
            Assert.Equal(1, throttle.CurrentCount);
        }

        [Fact]
        public void CancelReceive_DecrementsCounterAndReleasesThrottle_WhenLimited()
        {
            var counter = new CommandCounter(3, () => { });
            var throttle = new SemaphoreSlim(1);

            _ = counter.BeginReceive();
            _ = counter.BeginReceive();
            _ = counter.BeginReceive();

            // Now at limit, next should fail
            Assert.False(counter.BeginReceive());

            throttle.Wait();
            counter.CancelReceive(throttle);

            // Should now be able to receive one more
            Assert.True(counter.BeginReceive());
            Assert.False(counter.BeginReceive());
        }

        [Fact]
        public void CompleteReceive_ReleasesThrottle_WhenNoLimit()
        {
            var counter = new CommandCounter();
            var throttle = new SemaphoreSlim(0);

            counter.CompleteReceive(throttle);
            Assert.Equal(1, throttle.CurrentCount);
        }

        [Fact]
        public void CompleteReceive_CallsProcessExit_WhenAllCommandsCompleted()
        {
            var exitCalled = false;
            var counter = new CommandCounter(2, () => exitCalled = true);
            var throttle = new SemaphoreSlim(10);

            _ = counter.BeginReceive();
            _ = counter.BeginReceive();

            counter.CompleteReceive(throttle);
            Assert.False(exitCalled);

            counter.CompleteReceive(throttle);
            Assert.True(exitCalled);
        }

        [Fact]
        public void CompleteReceive_DoesNotOverReleaseThrottle()
        {
            var counter = new CommandCounter(3, () => { });
            var throttle = new SemaphoreSlim(0);

            _ = counter.BeginReceive();
            _ = counter.BeginReceive();
            _ = counter.BeginReceive();

            counter.CompleteReceive(throttle);
            var count1 = throttle.CurrentCount;

            counter.CompleteReceive(throttle);
            var count2 = throttle.CurrentCount;

            counter.CompleteReceive(throttle);
            var count3 = throttle.CurrentCount;

            // Should not exceed the number of commands started
            Assert.True(count1 <= 3);
            Assert.True(count2 <= 3);
            Assert.True(count3 <= 3);
        }

        [Fact]
        public void CommandCounter_Constructor_InvalidReceiveCount()
        {
            _ = Assert.Throws<ArgumentException>(() => new CommandCounter(0, () => { }));
            _ = Assert.Throws<ArgumentException>(() => new CommandCounter(-1, () => { }));
        }

        [Fact]
        public async Task BeginReceive_ThreadSafety_ConcurrentCalls()
        {
            var counter = new CommandCounter(100, () => { });
            var successCount = 0;
            var tasks = new Task[200];

            for (int i = 0; i < 200; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    if (counter.BeginReceive())
                        _ = System.Threading.Interlocked.Increment(ref successCount);
                });
            }

            await Task.WhenAll(tasks);

            // Only 100 should succeed
            Assert.Equal(100, successCount);
        }

        [Fact]
        public async Task CompleteReceive_ThreadSafety_ConcurrentCalls()
        {
            var exitCalled = false;
            var counter = new CommandCounter(10, () => exitCalled = true);
            var throttle = new SemaphoreSlim(0);

            // Start 10 receives
            for (int i = 0; i < 10; i++)
                _ = counter.BeginReceive();

            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    counter.CompleteReceive(throttle);
                });
            }

            await Task.WhenAll(tasks);

            Assert.True(exitCalled);
        }
    }
}
