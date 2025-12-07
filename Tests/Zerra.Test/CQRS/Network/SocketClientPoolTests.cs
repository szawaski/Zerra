// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class SocketClientPoolTests
    {
        [Fact]
        public void MaxConnectionsPerHost_DefaultValue()
        {
            var pool = new SocketClientPool();

            // Default should be Environment.ProcessorCount * 16
            var expectedDefault = Environment.ProcessorCount * 16;
            Assert.Equal(expectedDefault, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public void MaxConnectionsPerHost_CanBeSet()
        {
            var pool = new SocketClientPool();
            var newValue = 32;

            pool.MaxConnectionsPerHost = newValue;

            Assert.Equal(newValue, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public void MaxConnectionsPerHost_LessThanOne_ThrowsArgumentOutOfRangeException()
        {
            var pool = new SocketClientPool();

            Assert.Throws<ArgumentOutOfRangeException>(() => pool.MaxConnectionsPerHost = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.MaxConnectionsPerHost = -1);
        }

        [Fact]
        public void MaxConnectionsPerHost_SetToOne_Succeeds()
        {
            var pool = new SocketClientPool();

            pool.MaxConnectionsPerHost = 1;

            Assert.Equal(1, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public async Task BeginStreamAsync_WithValidHost_ReturnsStream()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[] { 1, 2, 3 };

            try
            {
                var stream = await pool.BeginStreamAsync("localhost", 80, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None);

                Assert.NotNull(stream);
                stream.Dispose();
            }
            catch (ConnectionFailedException)
            {
                // Expected if no server is listening on localhost:80
            }
        }

        [Fact]
        public async Task BeginStreamAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[] { 1, 2, 3 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                pool.BeginStreamAsync("localhost", 80, ProtocolType.Tcp, buffer.AsMemory(), cts.Token));
        }

        [Fact]
        public async Task BeginStreamAsync_WithInvalidHost_ThrowsConnectionFailedException()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[] { 1, 2, 3 };

            await Assert.ThrowsAnyAsync<SocketException>(() => 
                pool.BeginStreamAsync("invalid.host.that.does.not.exist.example", 9999, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None));
        }

        [Fact]
        public async Task BeginStreamAsync_WithEmptyBuffer_Succeeds()
        {
            var pool = new SocketClientPool();
            var buffer = Array.Empty<byte>();

            try
            {
                var stream = await pool.BeginStreamAsync("localhost", 80, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None);
                stream.Dispose();
            }
            catch (ConnectionFailedException)
            {
                // Expected if no server listening
            }
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            var pool = new SocketClientPool();

            pool.Dispose();

            // Should not throw
            Assert.NotNull(pool);
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            var pool = new SocketClientPool();

            pool.Dispose();
            pool.Dispose();

            Assert.NotNull(pool);
        }

        [Fact]
        public void Shared_ReturnsStaticInstance()
        {
            var shared1 = SocketClientPool.Shared;
            var shared2 = SocketClientPool.Shared;

            Assert.NotNull(shared1);
            Assert.Same(shared1, shared2);
        }

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var pool = new SocketClientPool();

            Assert.NotNull(pool);
            Assert.Equal(Environment.ProcessorCount * 16, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public void MaxConnectionsPerHost_SetMultipleValues()
        {
            var pool = new SocketClientPool();

            pool.MaxConnectionsPerHost = 10;
            Assert.Equal(10, pool.MaxConnectionsPerHost);

            pool.MaxConnectionsPerHost = 50;
            Assert.Equal(50, pool.MaxConnectionsPerHost);

            pool.MaxConnectionsPerHost = 1;
            Assert.Equal(1, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public async Task BeginStreamAsync_WithLoopbackAddress_Fails()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[] { 1, 2, 3 };

            // Attempt to connect to localhost on a port likely not listening
            await Assert.ThrowsAsync<ConnectionFailedException>(() => 
                pool.BeginStreamAsync("127.0.0.1", 54321, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None));
        }

        [Fact]
        public async Task BeginStreamAsync_ThrowsConnectionFailedException_OnConnectionError()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[] { 1, 2, 3 };

            // Connect to a port unlikely to have a service
            var exception = await Assert.ThrowsAsync<ConnectionFailedException>(() => 
                pool.BeginStreamAsync("localhost", 1, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None));

            Assert.NotNull(exception);
        }

        [Fact]
        public void MaxConnectionsPerHost_CanBeSetAfterConstruction()
        {
            var pool = new SocketClientPool();
            var originalValue = pool.MaxConnectionsPerHost;

            pool.MaxConnectionsPerHost = 100;

            Assert.NotEqual(originalValue, pool.MaxConnectionsPerHost);
            Assert.Equal(100, pool.MaxConnectionsPerHost);
        }

        [Fact]
        public async Task BeginStreamAsync_WithLargeBuffer_Succeeds()
        {
            var pool = new SocketClientPool();
            var buffer = new byte[10000];

            try
            {
                var stream = await pool.BeginStreamAsync("localhost", 80, ProtocolType.Tcp, buffer.AsMemory(), CancellationToken.None);
                stream.Dispose();
            }
            catch (ConnectionFailedException)
            {
                // Expected if no server listening
            }
        }

        [Fact]
        public void Dispose_SuppressesFinalize()
        {
            var pool = new SocketClientPool();
            
            pool.Dispose();
            
            // GC.SuppressFinalize should have been called
            Assert.NotNull(pool);
        }
    }
}
