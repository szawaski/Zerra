// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe queue with DequeueAsync
    /// </summary>
    public class AsyncConcurrentQueue<T> : IAsyncEnumerable<T>
    {
        private readonly Queue<T> queue;
        private readonly Queue<TaskCompletionSource<T>> waiters;
        public AsyncConcurrentQueue()
        {
            this.queue = new Queue<T>();
            this.waiters = new Queue<TaskCompletionSource<T>>();
        }

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

        public Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T>(cancellationToken);

            lock (queue)
            {
                if (queue.Count > 0)
                    return Task.FromResult(queue.Dequeue());
                var waiter = new TaskCompletionSource<T>();
                _ = cancellationToken.Register(() => { _ = waiter.TrySetCanceled(cancellationToken); });
                waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

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
