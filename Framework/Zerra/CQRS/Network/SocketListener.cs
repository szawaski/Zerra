using System;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public sealed class SocketListener : IDisposable
    {
        private const int backlog = 512; //Kestrel uses this value

        private readonly Socket socket;
        private readonly int maxConcurrent;
        private readonly int? maxReceive;
        private readonly Action processExit;
        private readonly Func<TcpClient, CancellationToken, Task> handler;
        private readonly SemaphoreSlim beginAcceptWaiter;

        private bool started;
        private bool disposed;

        private SemaphoreSlim throttle;
        private CancellationTokenSource canceller;

        private readonly object countLocker = new();
        private int receivedCount;
        private int completedCount;

        public SocketListener(Socket socket, int maxConcurrent, int? maxReceive, Action processExit, Func<TcpClient, CancellationToken, Task> handler)
        {
            if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));
            if (maxReceive.HasValue && maxReceive.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(maxReceive));

            this.maxConcurrent = maxReceive.HasValue ? Math.Min(maxReceive.Value, maxConcurrent) : maxConcurrent;
            this.maxReceive = maxReceive;
            this.processExit = processExit;

            this.socket = socket;
            this.handler = handler;
            this.beginAcceptWaiter = new SemaphoreSlim(0, 1);
            this.started = false;
            this.disposed = false;
        }

        public void Open()
        {
            lock (socket)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(SocketListener));
                if (started)
                    return;

                this.throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                this.receivedCount = 0;
                this.completedCount = 0;

                socket.Listen(backlog);

                this.canceller = new CancellationTokenSource();

                _ = AcceptConnections();

                started = true;
            }
        }

        private async Task AcceptConnections()
        {
            Exception error = null;
            try
            {
                for (; ; )
                {
                    await throttle.WaitAsync(canceller.Token);

                    if (maxReceive.HasValue)
                    {
                        lock (countLocker)
                        {
                            if (receivedCount == maxReceive.Value)
                                continue; //fill throttle, don't receive anymore, externally will be shutdown (shouldn't hit this line)
                            receivedCount++;
                        }
                    }

                    _ = socket.BeginAccept(BeginAcceptCallback, null);

                    await beginAcceptWaiter.WaitAsync(canceller.Token);
                }
            }
            catch (OperationCanceledException)
            {
                //shutdown, no issues
            }
            catch (Exception ex)
            {
                error = ex;
            }

            lock (socket)
            {
                started = false;
                socket.Close();

                throttle.Dispose();
                canceller.Dispose();
                canceller = null;

                socket.Dispose();
                beginAcceptWaiter.Dispose();
            }

            if (error != null)
                throw error;
        }

        private void BeginAcceptCallback(IAsyncResult result)
        {
            if (!started)
                return;

            _ = beginAcceptWaiter.Release();

            try
            {
                var incommingSocket = socket.EndAccept(result);

                var client = new TcpClient(socket.AddressFamily);
                client.NoDelay = true;
                client.Client = incommingSocket;

                _ = handler(client, canceller.Token);
            }
            finally
            {
                if (maxReceive.HasValue)
                {
                    lock (countLocker)
                    {
                        completedCount++;

                        if (completedCount == maxReceive.Value)
                            processExit?.Invoke();
                        else if (throttle.CurrentCount < maxReceive.Value - receivedCount)
                            _ = throttle.Release(); //don't release more than needed to reach maxReceive
                    }
                }
                else
                {
                    _ = throttle.Release();
                }
            }
        }

        ~SocketListener()
        {
            DisposeInternal();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private void DisposeInternal()
        {
            lock (socket)
            {
                if (disposed)
                    return;
                disposed = true;

                if (canceller != null)
                {
                    canceller.Cancel();
                }
                else
                {
                    socket.Dispose();
                }
            }
        }
    }
}
