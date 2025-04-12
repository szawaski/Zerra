// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Threading
{
    public sealed class TaskThrottler : IAsyncDisposable, IDisposable
    {
        private static readonly int defaultMaxRunningTasks = Environment.ProcessorCount;

        private readonly CancellationTokenSource canceller;
        private readonly AsyncConcurrentQueue<Func<Task>> queue;
        private readonly ConcurrentReadWriteList<Task> running;
        private readonly SemaphoreSlim taskLimiter;
        private readonly List<TaskCompletionSource<object>> waiters;
        private readonly SemaphoreSlim locker;
        private readonly Task mainTask;


        private int active;

        public TaskThrottler() : this(defaultMaxRunningTasks) { }
        public TaskThrottler(int maxRunningTasks)
        {
            if (maxRunningTasks <= 0)
                throw new ArgumentException("maxRunningTasks must be greater than zero");

            this.canceller = new();
            this.queue = new();
            this.running = new();
            this.taskLimiter = new(maxRunningTasks, maxRunningTasks);
            this.waiters = new();
            this.locker = new(1, 1);

            this.active = 0;

            mainTask = Task.Run(RunningThread, canceller.Token);
        }

        public void Run(Func<Task> taskFunc)
        {
            if (taskFunc is null)
                throw new ArgumentNullException(nameof(taskFunc));

            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(TaskThrottler));

            locker.Wait();
            active++;
            locker.Release();
            queue.Enqueue(taskFunc);
        }

        public async ValueTask DisposeAsync()
        {
            if (!canceller.IsCancellationRequested)
                canceller.Cancel();

            await mainTask;

            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (!canceller.IsCancellationRequested)
                canceller.Cancel();

            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        ~TaskThrottler()
        {
            if (!canceller.IsCancellationRequested)
                canceller.Cancel();
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            running.Dispose();
            taskLimiter.Dispose();
        }

        private async Task RunningThread()
        {
            try
            {
                while (!canceller.Token.IsCancellationRequested)
                {
                    await taskLimiter.WaitAsync(canceller.Token);

                    var taskFunc = await queue.DequeueAsync(canceller.Token);

                    _ = Task.Run(async () =>
                    {
                        var task = taskFunc();
                        if (canceller.Token.IsCancellationRequested)
                            return;
                        running.Add(task);
                        await task;
                        _ = running.Remove(task);
                        _ = taskLimiter.Release();
                        lock (locker)
                        {
                            active--;

                            if (active == 0)
                            {
                                //SetResult will continue the waiter in this thread and could case a threadlock
                                waiters.ForEach(x => Task.Run(() => x.SetResult(null!)));
                                waiters.Clear();
                            }
                        }
                    }, canceller.Token);
                }
            }
            catch (OperationCanceledException) { }

            canceller.Dispose();

            if (running.Count > 0)
                await Task.WhenAll(running.ToArray());
        }

        public void Wait(CancellationToken cancellationToken = default)
        {
            lock (locker)
            {
                if (active == 0)
                    return;

                var waiter = new TaskCompletionSource<object>();
                _ = cancellationToken.Register(() => { _ = waiter.TrySetCanceled(cancellationToken); });
                waiters.Add(waiter);
                Task.Run(() => waiter.Task).GetAwaiter().GetResult();
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            lock (locker)
            {
                if (active == 0)
                    return Task.CompletedTask;

                var waiter = new TaskCompletionSource<object>();
                _ = cancellationToken.Register(() => { _ = waiter.TrySetCanceled(cancellationToken); });
                waiters.Add(waiter);
                return waiter.Task;
            }
        }

        //for .NET versions before Parallel.ForEachAsync which is more efficient
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body, CancellationToken cancellationToken) => ForEachAsync(defaultMaxRunningTasks, source, body, cancellationToken);
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) => ForEachAsync(defaultMaxRunningTasks, source, body, CancellationToken.None);
        public static Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) => ForEachAsync(maxRunningTasks, source, body, CancellationToken.None);
        public static async Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body, CancellationToken cancellationToken)
        {
            if (maxRunningTasks <= 0)
                throw new ArgumentException("maxRunningTasks must be greater than zero");
            var waiter = new TaskCompletionSource<object>();
            var runningCount = 0;
            var loopCompleted = false;
            using (var taskLimiter = new SemaphoreSlim(defaultMaxRunningTasks, defaultMaxRunningTasks))
            {
                foreach (var item in source)
                {
                    await taskLimiter.WaitAsync(cancellationToken);
                    lock (waiter)
                        runningCount++;
                    _ = Task.Run(async () =>
                  {

                      var task = body(item, cancellationToken);
                      await task;
                      _ = taskLimiter.Release();
                      lock (waiter)
                      {
                          runningCount--;
                          if (loopCompleted && runningCount == 0)
                              waiter.SetResult(null!);
                      }
                  }, cancellationToken);
                }
                lock (waiter)
                {
                    loopCompleted = true;
                }
                _ = await waiter.Task;
            }
        }

        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body, CancellationToken cancellationToken) { return ForEachAsync(defaultMaxRunningTasks, source, body, cancellationToken); }
        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, body, CancellationToken.None); }
        public static Task ForEachAsync<TSource>(int maxRunningTasks, IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(maxRunningTasks, source, body, CancellationToken.None); }
        public static async Task ForEachAsync<TSource>(int maxRunningTasks, IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body, CancellationToken cancellationToken)
        {
            if (maxRunningTasks <= 0)
                throw new ArgumentException("maxRunningTasks must be greater than zero");
            var waiter = new TaskCompletionSource<object>();
            var runningCount = 0;
            var loopCompleted = false;
            using (var taskLimiter = new SemaphoreSlim(defaultMaxRunningTasks, defaultMaxRunningTasks))
            {
                await foreach (var item in source)
                {
                    await taskLimiter.WaitAsync(cancellationToken);
                    lock (waiter)
                        runningCount++;
                    _ = Task.Run(async () =>
                    {

                        var task = body(item, cancellationToken);
                        await task;
                        _ = taskLimiter.Release();
                        lock (waiter)
                        {
                            runningCount--;
                            if (loopCompleted && runningCount == 0)
                                waiter.SetResult(null!);
                        }
                    }, cancellationToken);
                }
                lock (waiter)
                {
                    loopCompleted = true;
                }
                _ = await waiter.Task;
            }
        }
    }
}
