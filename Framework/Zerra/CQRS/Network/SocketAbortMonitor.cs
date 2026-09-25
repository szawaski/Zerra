using System.Net.Sockets;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketAbortMonitor : IDisposable
    {
        private static readonly TimeSpan sendAbortMessageTimeout = TimeSpan.FromMilliseconds(1000);

        private static readonly byte[] abortMessageBytes = new byte[1];

        private readonly Stream stream;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Task monitorTask;

        private bool isCancellationRequested;
        private bool disposed;

        public SocketAbortMonitor(Socket socket, CancellationToken cancellationToken)
        {
            this.stream = new NetworkStream(socket, false);
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.monitorTask = Monitor(); //runs until the read waits, no thread or waiter needed
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        private async Task Monitor()
        {
            try
            {
                //receive abort
                var buffer = new byte[2];
#if !NETSTANDARD2_0
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
#if !NETSTANDARD2_0
                    _ = stream.WriteAsync(abortMessageBytes, cancellationTokenSource.Token).AsTask();
#else
                    _ = stream.WriteAsync(abortMessageBytes, 0, 1, cancellationTokenSource.Token);
#endif
                }
                catch { }


#if !NETSTANDARD2_0
                await cancellationTokenSource.CancelAsync();
#else
                cancellationTokenSource.Cancel();
#endif
            }
            catch { } //the read is canceled on dispose or fails when the connection closes, the monitor just ends
        }

        public static async Task<bool> SendAndAcknowledgeAbortAsync(Stream stream)
        {
            using var source = new CancellationTokenSource(sendAbortMessageTimeout);

            try
            {
                //send abort
#if !NETSTANDARD2_0
                await stream.WriteAsync(abortMessageBytes, source.Token);
#else
                await stream.WriteAsync(abortMessageBytes, 0, 1, source.Token);
#endif

                //receive abort acknowledged
                var buffer = new byte[2];
#if !NETSTANDARD2_0
                var result = await stream.ReadAsync(buffer, source.Token);
#else
                var result = await stream.ReadAsync(buffer, 0, 2, source.Token);
#endif
                if (result == 1 && buffer[0] == 0)
                    return true;
            }
            catch { }

            return false;
        }

        public static bool SendAndAcknowledgeAbort(Stream stream)
        {
            var originalReadTimeout = stream.CanTimeout ? stream.ReadTimeout : -1;
            var originalWriteTimeout = stream.CanTimeout ? stream.WriteTimeout : -1;

            try
            {
                if (stream.CanTimeout)
                {
                    stream.ReadTimeout = (int)sendAbortMessageTimeout.TotalMilliseconds;
                    stream.WriteTimeout = (int)sendAbortMessageTimeout.TotalMilliseconds;
                }

                //send abort
                stream.Write(abortMessageBytes, 0, 1);

                //receive abort acknowledged
                var buffer = new byte[2];
                var result = stream.Read(buffer, 0, 2);
                if (result == 1 && buffer[0] == 0)
                    return true;
            }
            catch { }
            finally
            {
                if (stream.CanTimeout)
                {
                    stream.ReadTimeout = originalReadTimeout;
                    stream.WriteTimeout = originalWriteTimeout;
                }
            }

            return false;
        }

        //awaits the monitor ending instead of blocking a thread
        public async Task<bool> DisposeAndGetIsCancellationRequestedAsync()
        {
            cancellationTokenSource.Cancel();
            await monitorTask;

            stream.Dispose();
            cancellationTokenSource.Dispose();
            disposed = true;
            return isCancellationRequested;
        }

        //releases without waiting for the monitor, the cancel ends its read and it catches anything that follows
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            cancellationTokenSource.Cancel();

            stream.Dispose();
            cancellationTokenSource.Dispose();
        }
    }
}
