using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public sealed class SocketListener : IDisposable
    {
        private const int backlog = 512; //Kestrel uses this value

        private readonly Socket socket;
        private readonly SemaphoreSlim waiter;
        private readonly Action<TcpClient, CancellationToken> handler;

        private bool started;
        private bool disposed;
        private CancellationTokenSource canceller;

        public SocketListener(Socket socket, Action<TcpClient, CancellationToken> handler)
        {
            this.socket = socket;
            this.waiter = new SemaphoreSlim(0, 1);
            this.handler = handler;
            this.started = false;
            this.disposed = false;
        }

        public void Open()
        {
            lock (this)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(SocketListener));
                if (started)
                    return;

                socket.Listen(backlog);

                this.canceller = new CancellationTokenSource();

                _ = Task.Run(() =>
                {
                    AcceptConnections();
                });

                started = true;
            }
        }

        private void AcceptConnections()
        {
            while (!canceller.Token.IsCancellationRequested)
            {
                _ = socket.BeginAccept(BeginAcceptCallback, null);
                waiter.Wait();
            }

            socket.Close();

            started = false;
            canceller.Dispose();
            canceller = null;

            lock (this)
            {
                if (disposed)
                {
                    socket.Dispose();
                    waiter.Dispose();
                }
            }
        }

        private void BeginAcceptCallback(IAsyncResult result)
        {
            if (!started)
                return;

            _ = waiter.Release();

            var incommingSocket = socket.EndAccept(result);

            var client = new TcpClient(socket.AddressFamily);
            client.NoDelay = true;
            client.Client = incommingSocket;

            handler(client, canceller.Token);
        }

        public void Close()
        {
            lock (this)
            {
                if (canceller != null)
                {
                    canceller.Cancel();
                    _ = waiter.Release();
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
            lock (this)
            {
                if (disposed)
                    return;
                started = false;
                disposed = true;
            }
            Close();
        }
    }
}
