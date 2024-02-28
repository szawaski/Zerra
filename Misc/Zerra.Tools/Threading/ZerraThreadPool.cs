// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;

namespace Zerra.Threading
{
    public sealed class ZerraThreadPool : IDisposable
    {
        private const int defaultMaxThreadCount = 25;

        private static readonly object currentLock = new();
        private static ZerraThreadPool current = null;
        public static ZerraThreadPool Current
        {
            get
            {
                if (current == null)
                {
                    lock (currentLock)
                    {
                        current ??= new ZerraThreadPool();
                    }
                }
                return current;
            }
        }

        private volatile int maxThreadCount;
        public int MaxThreadCount
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("Thread pool has been disposed.");
                return maxThreadCount;
            }
            set
            {
                if (disposed)
                    throw new ObjectDisposedException("Thread pool has been disposed.");
                maxThreadCount = value;
            }
        }
        private readonly Queue<WorkItem> workItems = new();
        private readonly List<WorkerThread> workerThreads = new();
        private readonly AutoResetEvent checkQueuedWorkItems = new(false);
        private volatile bool disposed;

        public ZerraThreadPool()
        {
            this.maxThreadCount = defaultMaxThreadCount;
            new Thread(MainExecutionThread).Start();
        }

        public void Run(Action work, IPrincipal principal = null)
        {
            if (disposed)
                throw new ObjectDisposedException("Thread pool has been disposed.");

            lock (workItems)
            {
                workItems.Enqueue(new WorkItem(work, null, principal));
            }

            _ = checkQueuedWorkItems.Set();
        }

        public IAsyncResult Run<TResult>(Func<TResult> work, AsyncCallback asyncCallback, IPrincipal principal = null)
        {
            if (disposed)
                throw new ObjectDisposedException("Thread pool has been disposed.");

            if (principal == null)
            {
                principal = Thread.CurrentPrincipal;

                if (Thread.CurrentPrincipal is ClaimsPrincipal claimsPrincipal)
                {
                    if (claimsPrincipal.Identity is ClaimsIdentity claimsIdentity)
                    {
                        var newClaimsIdentity = new ClaimsIdentity(claimsIdentity.Claims.ToArray(), claimsIdentity.AuthenticationType, claimsIdentity.NameClaimType, claimsIdentity.RoleClaimType);
                        var newClaimsPrincipal = new ClaimsPrincipal(new[] { newClaimsIdentity });
                        principal = newClaimsPrincipal;
                    }
                }
            }

            var asyncResult = new ZerraAsyncResult(asyncCallback);
            lock (workItems)
            {
                workItems.Enqueue(new WorkItem(work, asyncResult, principal));
            }
            _ = checkQueuedWorkItems.Set();
            return asyncResult;
        }

        private void MainExecutionThread()
        {
            while (true)
            {
                if (disposed)
                    return;
                while (workerThreads.Count < maxThreadCount)
                {
                    workerThreads.Add(new WorkerThread(checkQueuedWorkItems));
                }
                while (workerThreads.Count > maxThreadCount)
                {
                    var availableThread = workerThreads.FirstOrDefault(x => x.IsAvailable);
                    if (availableThread == null)
                        break;
                    _ = workerThreads.Remove(availableThread);
                    availableThread.Dispose();
                }
                lock (workItems)
                {
                    while (workItems.Count > 0)
                    {
                        if (disposed)
                            return;
                        var availableThread = workerThreads.FirstOrDefault(x => x.IsAvailable);
                        if (availableThread == null)
                            break;
                        var workItem = workItems.Dequeue();
                        availableThread.ExecuteWorkItem(workItem);
                    }
                }
                if (disposed)
                    return;
                _ = checkQueuedWorkItems.WaitOne();
                if (disposed)
                    return;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Unload();
        }
        ~ZerraThreadPool()
        {
            Unload();
        }
        private void Unload()
        {
            if (disposed)
                return;
            disposed = true;

            _ = checkQueuedWorkItems.Set(); //let background thread proceed and exit
            (checkQueuedWorkItems as IDisposable).Dispose();

            foreach (var workerThread in workerThreads)
            {
                workerThread.Dispose();
            }
        }

        private sealed class WorkItem
        {
            public MulticastDelegate Delegate { get; }
            public ZerraAsyncResult AsyncResult { get; }
            public IPrincipal Principal { get; }

            public WorkItem(MulticastDelegate del, ZerraAsyncResult asyncResult, IPrincipal princical)
            {
                this.Delegate = del;
                this.AsyncResult = asyncResult;
                this.Principal = princical;
            }
        }

        private sealed class WorkerThread : IDisposable
        {
            private readonly Thread thread = null;
            private IPrincipal principal = null;
            private readonly AutoResetEvent startNewWorkItem = new(false);
            private readonly AutoResetEvent workItemComplete;
            private volatile bool isAvailable = true;
            private MulticastDelegate task = null;
            private ZerraAsyncResult asyncResult = null;
            private volatile bool disposed = false;
            public WorkerThread(AutoResetEvent workItemComplete)
            {
                this.workItemComplete = workItemComplete;
                thread = new Thread(MainWorkerThread);
                thread.Start();
            }

            public bool IsAvailable
            {
                get
                {
                    if (disposed)
                        throw new ObjectDisposedException("Worker thread has been disposed.");
                    return isAvailable;
                }
            }
            private void MainWorkerThread()
            {
                while (true)
                {
                    if (disposed)
                        return;
                    _ = startNewWorkItem.WaitOne();
                    if (disposed)
                        return;

                    try
                    {
                        Thread.CurrentPrincipal = principal;
                        var result = task.DynamicInvoke(null);
                        asyncResult?.SetCompleted(result);
                    }
                    catch (Exception ex)
                    {
                        asyncResult?.SetCompletedException(ex);
                    }
                    isAvailable = true;
                    asyncResult = null;
                    task = null;
                    if (disposed)
                        return;
                    _ = workItemComplete.Set();
                }
            }
            public void ExecuteWorkItem(WorkItem workItem)
            {
                if (disposed)
                    throw new ObjectDisposedException("Worker thread has been disposed.");
                if (!isAvailable)
                    throw new InvalidOperationException("Worker thread is busy");

                principal = workItem.Principal;
                task = workItem.Delegate;
                asyncResult = workItem.AsyncResult;
                isAvailable = false;
                _ = startNewWorkItem.Set();
            }
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Unload();
            }
            ~WorkerThread()
            {
                Unload();
            }
            private void Unload()
            {
                if (disposed)
                    return;
                disposed = true;
                if (startNewWorkItem != null)
                {
                    _ = startNewWorkItem.Set();
                    (startNewWorkItem as IDisposable).Dispose();
                }
            }
        }

        private sealed class ZerraAsyncResult : IAsyncResult
        {
            private bool isCompleted = false;
            private readonly ManualResetEvent waitHandle;
            private readonly AsyncCallback asyncCallback;
            private Exception ex = null;
            private readonly object syncObject = new();

            public ZerraAsyncResult(AsyncCallback asyncCallback)
            {
                this.waitHandle = new ManualResetEvent(false);
                this.asyncCallback = asyncCallback;
            }

            public object AsyncState { get; set; }
            WaitHandle IAsyncResult.AsyncWaitHandle { get { return waitHandle; } }
            bool IAsyncResult.CompletedSynchronously { get { return false; } }
            bool IAsyncResult.IsCompleted { get
                {
                    lock (syncObject)
                    {
                        return isCompleted; }
                }
            }

            public bool ExceptionOccured
            {
                get
                {
                    lock (syncObject)
                    {
                        return ex != null;
                    }
                }
            }
            public Exception Exception
            {
                get
                {
                    lock (syncObject)
                    {
                        return ex;
                    }
                }
            }
            public void SetCompleted(object result)
            {
                lock (syncObject)
                {
                    AsyncState = result;
                    isCompleted = true;
                    _ = waitHandle.Set();
                    if (asyncCallback != null)
                    {
                        asyncCallback(this);
                    }
                }
            }
            public void SetCompletedException(Exception ex)
            {
                lock (syncObject)
                {
                    this.ex = ex;
                    isCompleted = true;
                    _ = waitHandle.Set();
                    asyncCallback?.Invoke(this);
                }
            }
        }
    }
}
