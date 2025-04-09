using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketAliveMonitor : IDisposable
    {
        private readonly TimeSpan frequency = TimeSpan.FromMilliseconds(250);
        private readonly int pollWaitMicroseconds = 1000;

        private readonly Socket socket;
        private readonly CancellationToken externalCancellationToken;
        private readonly CancellationTokenSource cancellationTokenSource;

        private bool disposed;

        public SocketAliveMonitor(Socket socket, CancellationToken cancellationToken)
        {
            this.socket = socket;
            this.externalCancellationToken = cancellationToken;
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(Monitor);
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        private async Task Monitor()
        {
            for (; ; )
            {
                await Task.Delay(frequency, externalCancellationToken);
                if (disposed)
                    return;

                try
                {
                    var connected = !(socket.Poll(pollWaitMicroseconds, SelectMode.SelectRead) && socket.Available == 0);
                    if (disposed)
                        return;

                    if (!connected)
                    {
#if NET8_0_OR_GREATER
                        await cancellationTokenSource.CancelAsync();
#else
                        cancellationTokenSource.Cancel();
#endif
                        return;
                    }
                }
                catch
                {
#if NET8_0_OR_GREATER
                    await cancellationTokenSource.CancelAsync();
#else
                    cancellationTokenSource.Cancel();
#endif
                    return;
                }
            }
        }

        public void Dispose()
        {
            disposed = true;
            cancellationTokenSource.Dispose();
        }
    }
}
