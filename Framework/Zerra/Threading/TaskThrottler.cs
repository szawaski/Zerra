// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Threading
{
    public class TaskThrottler : IDisposable
    {
        private const int defaultMaxRunningTasks = 20;

        private readonly CancellationToken cancellationToken;
        private readonly BlockingCollection<Task> queue;
        private readonly ConcurrentList<Task> running;
        private readonly SemaphoreSlim taskLimiter;
        private readonly Thread thread;

        public TaskThrottler(int maxRunningTasks = defaultMaxRunningTasks, CancellationToken cancellationToken = default)
        {
            if (maxRunningTasks <= 0) throw new ArgumentException("maxRunningTasks must be greater than zero");

            this.cancellationToken = cancellationToken;
            this.queue = new BlockingCollection<Task>();
            this.running = new ConcurrentList<Task>();
            this.taskLimiter = new SemaphoreSlim(maxRunningTasks);

            thread = new Thread(new ThreadStart(RunningThread));
            thread.Start();
        }

        public void Run(Task task)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(TaskThrottler));

            if (task.Status != TaskStatus.Created)
                throw new InvalidOperationException($"{nameof(TaskThrottler)} cannot run a task that is already started or scheduled.");

            task.ContinueWith(t =>
            {
                running.Remove(t);
                taskLimiter.Release();
            });

            running.Add(task);
            queue.Add(task);
        }

        public void Dispose()
        {
            if (!cancellationToken.IsCancellationRequested)
                cancellationToken.Cancel();

            thread.Join();
            running.Dispose();
            taskLimiter.Dispose();
            GC.SuppressFinalize(this);
        }

        private void RunningThread()
        {
            try
            {
                foreach (var task in queue.GetConsumingEnumerable(cancellationToken.Token))
                {
                    taskLimiter.Wait(cancellationToken.Token);
                    task.Start();
                }
            }
            catch (OperationCanceledException) { }

            cancellationToken.Dispose();

            try
            {
                Task.WaitAll(running.ToArray());
            }
            catch { }
        }

        public void Wait()
        {
            var tasks = running.ToArray();
            while (tasks.Length > 0)
            {
                Task.WaitAll(tasks);
                tasks = running.ToArray();
            }
        }

        public async ValueTask WaitAsync()
        {
            var tasks = running.ToArray();
            while (tasks.Length > 0)
            {
                await Task.WhenAll(tasks);
                tasks = running.ToArray();
            }
        }
    }
}
