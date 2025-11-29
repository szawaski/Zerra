// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class SocketAbortMonitorTests
    {
        private async Task<(Socket client, Socket server)> CreateConnectedSocketPairAsync()
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(IPAddress.Loopback, port);

            var serverSocket = await listener.AcceptAsync();
            listener.Dispose();

            return (clientSocket, serverSocket);
        }

        [Fact]
        public async Task Constructor_CreatesInstance()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();

                using (var monitor = new SocketAbortMonitor(serverSocket, cts.Token))
                {
                    Assert.NotNull(monitor);
                    cts.Cancel();
                }
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Token_Property_ReturnsValidCancellationToken()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();

                using (var monitor = new SocketAbortMonitor(serverSocket, cts.Token))
                {
                    var token = monitor.Token;
                    Assert.False(token.IsCancellationRequested);
                    cts.Cancel();
                }
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Token_IsLinkedToSourceToken()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();

                using (var monitor = new SocketAbortMonitor(serverSocket, cts.Token))
                {
                    var monitorToken = monitor.Token;

                    // Cancel the source token
                    cts.Cancel();

                    // Monitor token should also be cancelled (linked)
                    Assert.True(monitorToken.IsCancellationRequested);
                }
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Dispose_ReleasesResources()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);
                
                // Cancel immediately to stop monitor task
                cts.Cancel();
                
                monitor.Dispose();

                // Should not throw
                Assert.NotNull(monitor);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task DisposeAndGetIsCancellationRequested_ReturnsFalseWhenNotRequested()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);
                
                // Cancel immediately to stop monitor task before dispose
                cts.Cancel();
                
                var result = monitor.DisposeAndGetIsCancellationRequested();

                Assert.False(result);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task DisposeAndGetIsCancellationRequested_ReturnsTrueWhenAbortReceived()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);

                // Send abort message from client (byte with value 0)
                var abortMessage = new byte[] { 0 };
                await clientSocket.SendAsync(new ArraySegment<byte>(abortMessage), SocketFlags.None);

                // Give monitor time to receive and process abort message
                await Task.Delay(200);

                var result = monitor.DisposeAndGetIsCancellationRequested();

                Assert.True(result);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendAndAcknowledgeAbort_WithValidHandshake_ReturnsTrue()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                
                // Start monitor on server to handle abort and send acknowledgment
                var monitorTask = Task.Run(() =>
                {
                    var monitor = new SocketAbortMonitor(serverSocket, cts.Token);
                    return monitor;
                });

                // Give monitor time to start
                await Task.Delay(100);

                // Client sends abort and waits for acknowledgment
                var clientStream = new NetworkStream(clientSocket, false);
                var result = await SocketAbortMonitor.SendAndAcknowledgeAbort(clientStream);

                // Give time for handshake to complete
                await Task.Delay(100);

                Assert.True(result);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task SendAndAcknowledgeAbort_WithTimeout_ReturnsFalse()
        {
            var stream = new MemoryStream();
            
            // No actual socket to respond, should timeout and return false
            var result = await SocketAbortMonitor.SendAndAcknowledgeAbort(stream);

            Assert.False(result);
        }

        [Fact]
        public async Task Constructor_WithCancelledToken()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                using (var monitor = new SocketAbortMonitor(serverSocket, cts.Token))
                {
                    // Token should be cancelled
                    Assert.True(monitor.Token.IsCancellationRequested);
                }
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Dispose_MultipleCalls_ThrowsOnSecondCall()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();
            var cts = new CancellationTokenSource();

            try
            {
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);
                
                // Cancel immediately to stop monitor task
                cts.Cancel();
                
                monitor.Dispose();
                
                // Second dispose should throw because semaphore is disposed
                Assert.Throws<ObjectDisposedException>(() => monitor.Dispose());
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Token_RemainsValidAfterConstruction()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();
            var cts = new CancellationTokenSource();

            try
            {
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);
                var token1 = monitor.Token;
                var token2 = monitor.Token;

                // Should return same token
                Assert.Equal(token1, token2);
                
                // Cancel before dispose
                cts.Cancel();
                monitor.Dispose();
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Token_CanBeCancelledDirectly()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();
            var cts = new CancellationTokenSource();

            try
            {
                using (var monitor = new SocketAbortMonitor(serverSocket, cts.Token))
                {
                    var token = monitor.Token;
                    Assert.False(token.IsCancellationRequested);

                    cts.Cancel();

                    Assert.True(token.IsCancellationRequested);
                }
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task Monitor_HandlesInvalidMessage_DoesNotCancelToken()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                var monitor = new SocketAbortMonitor(serverSocket, cts.Token);

                // Send invalid message (not 0)
                var invalidMessage = new byte[] { 1 };
                await clientSocket.SendAsync(new ArraySegment<byte>(invalidMessage), SocketFlags.None);

                // Give monitor time to process invalid message
                await Task.Delay(200);

                var result = monitor.DisposeAndGetIsCancellationRequested();

                // Invalid message should not result in cancellation request
                Assert.False(result);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task AbortProtocol_FullHandshake()
        {
            var (clientSocket, serverSocket) = await CreateConnectedSocketPairAsync();

            try
            {
                var cts = new CancellationTokenSource();
                var monitorTask = Task.Run(() => new SocketAbortMonitor(serverSocket, cts.Token));

                await Task.Delay(100);

                var clientStream = new NetworkStream(clientSocket, false);
                var handshakeResult = await SocketAbortMonitor.SendAndAcknowledgeAbort(clientStream);

                await Task.Delay(100);
                var monitor = await monitorTask;

                var cancellationRequested = monitor.DisposeAndGetIsCancellationRequested();

                // Both sides should indicate successful abort handshake
                Assert.True(handshakeResult);
                Assert.True(cancellationRequested);
            }
            finally
            {
                clientSocket.Dispose();
                serverSocket.Dispose();
            }
        }
    }
}
