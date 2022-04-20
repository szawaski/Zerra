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
    public class TaskThrottler : IAsyncDisposable, IDisposable
    {
        private static readonly int defaultMaxRunningTasks = Environment.ProcessorCount;

        private readonly CancellationTokenSource canceller;
        private readonly Queue<Func<Task>> queue;
        private readonly ConcurrentList<Task> running;
        private readonly SemaphoreSlim taskLimiter;
        private readonly SemaphoreSlim queueLocker;
        private readonly SemaphoreSlim emptyQueueAwaiter;
        private readonly EventWaitHandle startingTaskEvent;
        private readonly Task mainTask;

        public TaskThrottler() : this(defaultMaxRunningTasks) { }
        public TaskThrottler(int maxRunningTasks)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");

            this.canceller = canceller ?? new CancellationTokenSource();
            this.queue = new Queue<Func<Task>>();
            this.running = new ConcurrentList<Task>();
            this.taskLimiter = new SemaphoreSlim(maxRunningTasks, maxRunningTasks);
            this.queueLocker = new SemaphoreSlim(1, 1);
            this.emptyQueueAwaiter = new SemaphoreSlim(0, 1);
            this.startingTaskEvent = new EventWaitHandle(true, EventResetMode.AutoReset);

            mainTask = Task.Run(RunningThread, canceller.Token);
        }

        public void Run(Func<Task> taskFunc)
        {
            if (taskFunc == null) throw new ArgumentNullException(nameof(taskFunc));

            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(TaskThrottler));

            queueLocker.Wait();
            queue.Enqueue(taskFunc);
            if (emptyQueueAwaiter.CurrentCount == 0)
                emptyQueueAwaiter.Release();
            queueLocker.Release();
        }

        public async ValueTask DisposeAsync()
        {
            if (!canceller.IsCancellationRequested)
                canceller.Cancel();

            await mainTask;

            running.Dispose();
            taskLimiter.Dispose();
            queueLocker.Dispose();
            emptyQueueAwaiter.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (!canceller.IsCancellationRequested)
                canceller.Cancel();

            running.Dispose();
            taskLimiter.Dispose();
            queueLocker.Dispose();
            emptyQueueAwaiter.Dispose();
            startingTaskEvent.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task RunningThread()
        {
            try
            {
                while (!canceller.IsCancellationRequested)
                {
                    await taskLimiter.WaitAsync(canceller.Token);

                    Func<Task> taskFunc;
                    Task waiter;
                    await queueLocker.WaitAsync(canceller.Token);
                    if (queue.Count > 0)
                    {
                        taskFunc = queue.Dequeue();
                        waiter = null;
                    }
                    else
                    {
                        taskFunc = null;
                        waiter = emptyQueueAwaiter.WaitAsync(canceller.Token);
                    }
                    queueLocker.Release();

                    if (waiter != null)
                    {
                        await waiter;
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        var task = taskFunc();
                        running.Add(task);
                        startingTaskEvent.Set();
                        await task;
                        running.Remove(task);
                        taskLimiter.Release();
                    }, canceller.Token);
                }
            }
            catch (OperationCanceledException) { }

            canceller.Dispose();

            await Task.WhenAll(running.ToArray());
        }

        public void Wait()
        {
            queueLocker.Wait();
            var queueCount = queue.Count;
            queueLocker.Release();
            var tasks = running.ToArray();

            while (queueCount > 0 || tasks.Length > 0)
            {
                if (queueCount > 0 && tasks.Length == 0)
                    startingTaskEvent.WaitOne();
                else
                    Task.WaitAll(tasks);

                queueLocker.Wait();
                queueCount = queue.Count;
                queueLocker.Release();
                tasks = running.ToArray();
            }
        }

        public async ValueTask WaitAsync()
        {
            await queueLocker.WaitAsync();
            var queueCount = queue.Count;
            queueLocker.Release();
            var tasks = running.ToArray();

            while (queueCount > 0 || tasks.Length > 0)
            {
                if (queueCount > 0 && tasks.Length == 0)
                    startingTaskEvent.WaitOne();
                else
                    await Task.WhenAll(tasks);

                await queueLocker.WaitAsync();
                queueCount = queue.Count;
                queueLocker.Release();
                tasks = running.ToArray();
            }
        }

        //for .NET versions before Parallel.ForEachAsync
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, cancellationToken, body); }
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(defaultMaxRunningTasks, source, CancellationToken.None, body); }
        public static Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body) { return ForEachAsync(maxRunningTasks, source, CancellationToken.None, body); }
        public static async Task ForEachAsync<TSource>(int maxRunningTasks, IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");
            using (var taskLimiter = new SemaphoreSlim(defaultMaxRunningTasks, defaultMaxRunningTasks))
            {
                foreach (var item in source)
                {
                    await taskLimiter.WaitAsync();
                    _ = Task.Run(async () =>
                      {
                          var task = body(item, cancellationToken);
                          await task;
                          taskLimiter.Release();
                      }, cancellationToken);
                }
            }
        }
    }
}
