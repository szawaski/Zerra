using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketListener : IDisposable
    {
        private const int backlog = 512; //Kestrel uses this value

        private readonly Socket socket;
        private readonly Func<Socket, CancellationToken, Task> handler;
        private readonly SemaphoreSlim beginAcceptWaiter;
        private readonly SocketClientPool socketPool;

        private bool started;
        private bool disposed;

        private CancellationTokenSource? canceller;

        public SocketListener(Socket socket, Func<Socket, CancellationToken, Task> handler)
        {
            this.socket = socket;
            this.handler = handler;
            this.beginAcceptWaiter = new SemaphoreSlim(0, 1);
            this.socketPool = SocketClientPool.Shared;
            this.started = false;
            this.disposed = false;

            socket.NoDelay = true;
            socket.ReceiveTimeout = (int)socketPool.ConnectionTimeout.TotalMilliseconds;
            socket.SendTimeout = (int)socketPool.ConnectionTimeout.TotalMilliseconds;
        }

        public void Open()
        {
            lock (socket)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(SocketListener));
                if (started)
                    return;

                socket.Listen(backlog);

                this.canceller = new CancellationTokenSource();

                _ = Task.Run(AcceptConnections);

                started = true;
            }
        }

        private async Task AcceptConnections()
        {
            Exception? error = null;
            try
            {
                for (; ; )
                {
                    _ = socket.BeginAccept(BeginAcceptCallback, null);

                    await beginAcceptWaiter.WaitAsync(canceller!.Token);
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

                canceller!.Dispose();
                canceller = null;

                socket.Dispose();
                beginAcceptWaiter.Dispose();
            }

            if (error is not null)
                throw error;
        }

        private void BeginAcceptCallback(IAsyncResult result)
        {
            if (!started)
                return;

            _ = beginAcceptWaiter.Release();

            var incommingSocket = socket.EndAccept(result);
            //incommingSocket copies settings of socket (ReceiveTimeout,SendTimeout,NoDelay,etc)

            _ = handler(incommingSocket, canceller!.Token);
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

                if (canceller is not null)
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
