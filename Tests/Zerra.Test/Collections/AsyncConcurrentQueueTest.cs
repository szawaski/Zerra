// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class AsyncConcurrentQueueTest
    {
        [Fact]
        public void Constructor_Default()
        {
            var queue = new AsyncConcurrentQueue<int>();
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void Count_Property()
        {
            var queue = new AsyncConcurrentQueue<int>();
            Assert.Equal(0, queue.Count);
            queue.Enqueue(1);
            Assert.Equal(1, queue.Count);
            queue.Enqueue(2);
            Assert.Equal(2, queue.Count);
        }

        [Fact]
        public async Task Enqueue_And_DequeueAsync()
        {
            var queue = new AsyncConcurrentQueue<int>();
            queue.Enqueue(1);
            var result = await queue.DequeueAsync();
            Assert.Equal(1, result);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task DequeueAsync_WaitsForItem()
        {
            var queue = new AsyncConcurrentQueue<int>();
            var dequeuedValue = -1;
            var dequeueLowPriority = Task.Run(async () =>
            {
                dequeuedValue = await queue.DequeueAsync();
            });

            await Task.Delay(100);
            Assert.Equal(-1, dequeuedValue);

            queue.Enqueue(42);
            await dequeueLowPriority;
            Assert.Equal(42, dequeuedValue);
        }

        [Fact]
        public async Task DequeueAsync_WithCancellation()
        {
            var queue = new AsyncConcurrentQueue<int>();
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            _ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                _ = await queue.DequeueAsync(cts.Token);
            });
        }

        [Fact]
        public async Task DequeueAsync_WithAlreadyCancelledToken()
        {
            var queue = new AsyncConcurrentQueue<int>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                _ = await queue.DequeueAsync(cts.Token);
            });
        }

        [Fact]
        public async Task Enqueue_ServesMultipleWaiters()
        {
            var queue = new AsyncConcurrentQueue<int>();
            var task1 = queue.DequeueAsync();
            var task2 = queue.DequeueAsync();

            await Task.Delay(50);
            queue.Enqueue(1);
            queue.Enqueue(2);

            var result1 = await task1;
            var result2 = await task2;

            Assert.Equal(1, result1);
            Assert.Equal(2, result2);
        }

        [Fact]
        public async Task Enqueue_ToWaiter_DoesNotQueueItem()
        {
            var queue = new AsyncConcurrentQueue<int>();
            var dequeueLowPriority = queue.DequeueAsync();

            queue.Enqueue(42);
            var result = await dequeueLowPriority;

            Assert.Equal(42, result);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public async Task GetAsyncEnumerator_EnumeratesItems()
        {
            var queue = new AsyncConcurrentQueue<int>();
            var results = new List<int>();

            var enumerateTask = Task.Run(async () =>
            {
                await foreach (var item in queue)
                {
                    results.Add(item);
                    if (item == 3)
                        break;
                }
            });

            await Task.Delay(50);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            await enumerateTask;

            Assert.Equal(3, results.Count);
            Assert.Equal(1, results[0]);
            Assert.Equal(2, results[1]);
            Assert.Equal(3, results[2]);
        }

        [Fact]
        public async Task GetAsyncEnumerator_WithCancellation()
        {
            var queue = new AsyncConcurrentQueue<int>();
            using var cts = new CancellationTokenSource();

            var enumerateTask = Task.Run(async () =>
            {
                var count = 0;
                try
                {
                    var enumerator = queue.GetAsyncEnumerator(cts.Token);
                    while (await enumerator.MoveNextAsync())
                    {
                        count++;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancelled
                    return -1;
                }
                return count;
            });

            await Task.Delay(50);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            await Task.Delay(50);

            cts.Cancel();

            var count = await enumerateTask;
            Assert.Equal(-1, count);
        }

        [Fact]
        public async Task MultipleEnqueueDequeue_Sequence()
        {
            var queue = new AsyncConcurrentQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Assert.Equal(3, queue.Count);

            var result1 = await queue.DequeueAsync();
            Assert.Equal(1, result1);
            Assert.Equal(2, queue.Count);

            var result2 = await queue.DequeueAsync();
            Assert.Equal(2, result2);
            Assert.Equal(1, queue.Count);

            var result3 = await queue.DequeueAsync();
            Assert.Equal(3, result3);
            Assert.Equal(0, queue.Count);
        }
    }
}
