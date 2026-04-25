// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Collections
{
    /// <summary>
    /// An async queue that is thread safe.
    /// </summary>
    public class AsyncConcurrentQueue<T> : IAsyncEnumerable<T>
    {
        private readonly Queue<T> queue;
        private readonly Queue<TaskCompletionSource<T>> waiters;

        /// <summary>
        /// Creates a new empty queue.
        /// </summary>
        public AsyncConcurrentQueue()
        {
            this.queue = new Queue<T>();
            this.waiters = new Queue<TaskCompletionSource<T>>();
        }

        /// <summary>
        /// The current number of items in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }

        /// <summary>
        /// Waits for the next available item to dequeue. If there are no items it will wait until one is enqueued.
        /// </summary>
        /// <param name="cancellationToken">The token to cancel waiting.</param>
        /// <returns>A task that awaits the next item from the queue.</returns>
        public Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T>(cancellationToken);

            lock (queue)
            {
                if (queue.Count > 0)
                    return Task.FromResult(queue.Dequeue());
                var waiter = new TaskCompletionSource<T>();
                _ = cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken));
                waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        /// <summary>
        /// Adds an item to the queue. If there is a process awaiting a dequeue it may be removed quickly.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Enqueue(T item)
        {
            lock (queue)
            {
                while (waiters.Count > 0)
                {
                    var waiter = waiters.Dequeue();
                    if (waiter.Task.IsCompleted)
                        continue;
                    waiter.SetResult(item);
                    return;
                }
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// Gets an async enumerator that will dequeue items until stopped by the cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The token to stop the enumerator.</param>
        /// <returns>The async enumerator.</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncQueueEnumerator(this, cancellationToken);
        }

        private sealed class AsyncQueueEnumerator : IAsyncEnumerator<T>
        {
            private readonly AsyncConcurrentQueue<T> queue;
            private readonly CancellationToken cancellationToken;
            public AsyncQueueEnumerator(AsyncConcurrentQueue<T> queue, CancellationToken cancellationToken)
            {
                this.queue = queue;
                this.cancellationToken = cancellationToken;
                this.current = default;
            }

            private T? current;
            public T Current => current ?? throw new NotSupportedException();

            public async ValueTask<bool> MoveNextAsync()
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                current = await queue.DequeueAsync(cancellationToken);
                return true;
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }
        }
    }
}
