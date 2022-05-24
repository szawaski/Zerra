// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Threading
{
    public class TaskThrottler : IAsyncDisposable, IDisposable
    {
        private static readonly int defaultMaxRunningTasks = Environment.ProcessorCount;

        private readonly CancellationTokenSource canceller;
        private readonly AsyncConcurrentQueue<Func<Task>> queue;
        private readonly ConcurrentList<Task> running;
        private readonly SemaphoreSlim taskLimiter;
        private readonly Queue<TaskCompletionSource<object>> waiters;
        private readonly Task mainTask;

        public TaskThrottler() : this(defaultMaxRunningTasks) { }
        public TaskThrottler(int maxRunningTasks)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");

            this.canceller = new CancellationTokenSource();
            this.queue = new AsyncConcurrentQueue<Func<Task>>();
            this.running = new ConcurrentList<Task>();
            this.taskLimiter = new SemaphoreSlim(maxRunningTasks, maxRunningTasks);
            this.waiters = new Queue<TaskCompletionSource<object>>();

            mainTask = Task.Run(RunningThread, canceller.Token);
        }

        public void Run(Func<Task> taskFunc)
        {
            if (taskFunc == null) throw new ArgumentNullException(nameof(taskFunc));

            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(TaskThrottler));

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
                        running.Remove(task);
                        taskLimiter.Release();
                        if (queue.Count == 0 && running.Count == 0)
                        {
                            lock (waiters)
                            {
                                waiters.ToArray().ForEach(x => x.SetResult(null));
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
            if (queue.Count == 0 && running.Count == 0)
                return;

            lock (waiters)
            {
                var waiter = new TaskCompletionSource<object>();
                cancellationToken.Register(() => { _ = waiter.TrySetCanceled(cancellationToken); });
                waiters.Enqueue(waiter);
                waiter.Task.GetAwaiter().GetResult();
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (queue.Count == 0 && running.Count == 0)
                return Task.CompletedTask;

            lock (waiters)
            {
                var waiter = new TaskCompletionSource<object>();
                cancellationToken.Register(() => { _ = waiter.TrySetCanceled(cancellationToken); });
                waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        //for .NET versions before Parallel.ForEachAsync which is more efficient
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, cancellationToken, body); }
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, CancellationToken.None, body); }
        public static Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(maxRunningTasks, source, CancellationToken.None, body); }
        public static async Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");
            var waiter = new TaskCompletionSource<object>();
            var runningCount = 0;
            var loopCompleted = false;
            using (var taskLimiter = new SemaphoreSlim(defaultMaxRunningTasks, defaultMaxRunningTasks))
            {
                foreach (var item in source)
                {
                    await taskLimiter.WaitAsync();
                    lock (waiter)
                        runningCount++;
                    _ = Task.Run(async () =>
                  {

                      var task = body(item, cancellationToken);
                      await task;
                      taskLimiter.Release();
                      lock (waiter)
                      {
                          runningCount--;
                          if (loopCompleted && runningCount == 0)
                              waiter.SetResult(null);
                      }
                  }, cancellationToken);
                }
                lock (waiter)
                {
                    loopCompleted = true;
                }
                await waiter.Task;
            }
        }

        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, cancellationToken, body); }
        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, CancellationToken.None, body); }
        public static Task ForEachAsync<TSource>(int maxRunningTasks, IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(maxRunningTasks, source, CancellationToken.None, body); }
        public static async Task ForEachAsync<TSource>(int maxRunningTasks, IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");
            var waiter = new TaskCompletionSource<object>();
            var runningCount = 0;
            var loopCompleted = false;
            using (var taskLimiter = new SemaphoreSlim(defaultMaxRunningTasks, defaultMaxRunningTasks))
            {
                await foreach (var item in source)
                {
                    await taskLimiter.WaitAsync();
                    lock (waiter)
                        runningCount++;
                    _ = Task.Run(async () =>
                    {

                        var task = body(item, cancellationToken);
                        await task;
                        taskLimiter.Release();
                        lock (waiter)
                        {
                            runningCount--;
                            if (loopCompleted && runningCount == 0)
                                waiter.SetResult(null);
                        }
                    }, cancellationToken);
                }
                lock (waiter)
                {
                    loopCompleted = true;
                }
                await waiter.Task;
            }
        }
    }
}
