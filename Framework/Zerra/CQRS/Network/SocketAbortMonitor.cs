using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketAbortMonitor : IDisposable
    {
        private static readonly TimeSpan sendAbortMessageTimeout = TimeSpan.FromMilliseconds(1000);

        private readonly Stream stream;
        private readonly CancellationTokenSource cancellationTokenSource;

        public SocketAbortMonitor(Socket socket, CancellationToken cancellationToken)
        {
            this.stream = new NetworkStream(socket, false);
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(Monitor);
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        private async Task Monitor()
        {
            try
            {
                var buffer = new byte[1];
#if NET6_0_OR_GREATER
                var result = await stream.ReadAsync(buffer, cancellationTokenSource.Token);
#else
                var result = await stream.ReadAsync(buffer, 0, 1, cancellationTokenSource.Token);
#endif
                if (result == 1)
                {
#if NET8_0_OR_GREATER
                    await cancellationTokenSource.CancelAsync();
#else
                    cancellationTokenSource.Cancel();
#endif
                }
            }
            finally { }
        }

        private static readonly byte[] abortMessageBytes = new byte[1];
        public static async Task<bool> SendAbortMessage(Stream stream)
        {
            using var source = new CancellationTokenSource(sendAbortMessageTimeout);
            try
            {
#if NET6_0_OR_GREATER
                await stream.WriteAsync(abortMessageBytes, source.Token);
#else
                await stream.WriteAsync(abortMessageBytes, 0, 1, source.Token);
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            stream.Dispose();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
