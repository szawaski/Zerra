using System.Net.Sockets;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketAbortMonitor : IDisposable
    {
        private static readonly TimeSpan sendAbortMessageTimeout = TimeSpan.FromMilliseconds(1000);

        private static readonly byte[] abortMessageBytes = new byte[1];

        private readonly Stream stream;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly SemaphoreSlim monitorCompleteWaiter;

        private bool isCancellationRequested;

        public SocketAbortMonitor(Socket socket, CancellationToken cancellationToken)
        {
            this.stream = new NetworkStream(socket, false);
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.monitorCompleteWaiter = new(0, 1);
            _ = Task.Run(Monitor);
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        private async Task Monitor()
        {
            try
            {
                //receive abort
                var buffer = new byte[2];
#if NET6_0_OR_GREATER
                var result = await stream.ReadAsync(buffer, cancellationTokenSource.Token);
#else
                var result = await stream.ReadAsync(buffer, 0, 2, cancellationTokenSource.Token);
#endif

                if (result != 1 || buffer[0] != 0)
                    return; //invalid message ignore, something else will throw

                isCancellationRequested = true;

                //send abort acknowledged
                try
                {
#if NET6_0_OR_GREATER
                    _ = stream.WriteAsync(abortMessageBytes, cancellationTokenSource.Token).AsTask();
#else
                    _ = stream.WriteAsync(abortMessageBytes, 0, 1, cancellationTokenSource.Token);
#endif
                }
                finally { }


#if NET8_0_OR_GREATER
                await cancellationTokenSource.CancelAsync();
#else
                cancellationTokenSource.Cancel();
#endif
            }
            finally
            {
                monitorCompleteWaiter.Release();
            }
        }

        public static async Task<bool> SendAndAcknowledgeAbort(Stream stream)
        {
            using var source = new CancellationTokenSource(sendAbortMessageTimeout);
            try
            {
                //send abort
#if NET6_0_OR_GREATER
                await stream.WriteAsync(abortMessageBytes, source.Token);
#else
                await stream.WriteAsync(abortMessageBytes, 0, 1, source.Token);
#endif

                //receive abort acknowledged
                var buffer = new byte[2];
#if NET6_0_OR_GREATER
                var result = await stream.ReadAsync(buffer, source.Token);
#else
                var result = await stream.ReadAsync(buffer, 0, 2, source.Token);
#endif
                if (result == 1 && buffer[0] == 0)
                    return true;
            }
            finally { }

            return false;
        }

        public bool DisposeAndGetIsCancellationRequested()
        {
            Dispose();
            return isCancellationRequested;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            monitorCompleteWaiter.Wait();

            stream.Dispose();
            cancellationTokenSource.Dispose();
            monitorCompleteWaiter.Dispose();
        }
    }
}
