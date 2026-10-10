using System.Net.Sockets;

namespace Zerra.CQRS.Network
{
    internal sealed class SocketAbortMonitor : IDisposable
    {
        private static readonly TimeSpan sendAbortMessageTimeout = TimeSpan.FromMilliseconds(1000);

        private static readonly byte[] abortMessageBytes = new byte[1];
#if NETSTANDARD2_0
        private static readonly ArraySegment<byte> noBytes = new(Array.Empty<byte>());
#endif

        private const int handling = 0;
        private const int finished = 1;
        private const int aborting = 2;

        private readonly Socket socket;
        private readonly Stream stream;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Task monitorTask;

        private int state;
        private bool isCancellationRequested;
        private bool disposed;

        public SocketAbortMonitor(Socket socket, CancellationToken cancellationToken)
        {
            this.socket = socket;
            this.stream = new NetworkStream(socket, false);
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.monitorTask = Monitor(); //runs until the read waits, no thread or waiter needed
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        //A zero byte receive waits for data without taking it and is never canceled, canceling a socket read throws, which cost every request.
        //Whoever moves the state first owns what arrives: the monitor during handling, where the client only sends an abort,
        //or the server once handling finished, where it's the next request and the receive just ends when it comes.
        private async Task Monitor()
        {
            try
            {
#if !NETSTANDARD2_0
                _ = await socket.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None);
#else
                _ = await socket.ReceiveAsync(noBytes, SocketFlags.None);
#endif
                if (Interlocked.CompareExchange(ref state, aborting, handling) != handling)
                    return;

                //receive abort, it's already here
                var buffer = new byte[2];
#if !NETSTANDARD2_0
                var result = await stream.ReadAsync(buffer);
#else
                var result = await stream.ReadAsync(buffer, 0, 2);
#endif

                if (result != 1 || buffer[0] != 0)
                    return; //invalid message ignore, something else will throw

                isCancellationRequested = true;

                //send abort acknowledged
                try
                {
#if !NETSTANDARD2_0
                    _ = stream.WriteAsync(abortMessageBytes).AsTask();
#else
                    _ = stream.WriteAsync(abortMessageBytes, 0, 1);
#endif
                }
                catch { }

#if !NETSTANDARD2_0
                await cancellationTokenSource.CancelAsync();
#else
                cancellationTokenSource.Cancel();
#endif
            }
            catch { } //the receive fails when the connection closes, the monitor just ends
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

        //only waits when the monitor took an abort, which is already here, otherwise its receive is left for the next request
        public async Task<bool> DisposeAndGetIsCancellationRequestedAsync()
        {
            if (Interlocked.CompareExchange(ref state, finished, handling) == aborting)
                await monitorTask;

            stream.Dispose();
            cancellationTokenSource.Dispose();
            disposed = true;
            return isCancellationRequested;
        }

        //releases without waiting for the monitor, one taking an abort is left to finish with the stream and token
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            if (Interlocked.CompareExchange(ref state, finished, handling) == aborting)
                return;

            stream.Dispose();
            cancellationTokenSource.Dispose();
        }
    }
}
