// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class SocketPoolStreamTests
    {
        private static async Task<(Socket client, Socket server, int port)> CreateConnectedSocketPairAsync()
        {
            // Create a listener socket
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // Bind to any available port
            listener.Listen(1);
            
            var endPoint = (IPEndPoint)listener.LocalEndPoint!;
            var port = endPoint.Port;

            // Create a client socket and connect
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            var acceptTask = listener.AcceptAsync();
            
            await Task.WhenAll(connectTask, acceptTask);
            
            var server = await acceptTask;
            listener.Dispose();

            return (client, server, port);
        }

        [Fact]
        public async Task IsNewConnection_Property_True()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);

            var stream = new SocketPoolStream(client, hostAndPort, (s, h, c) => { }, isNewConnection: true);

            Assert.True(stream.IsNewConnection);
            
            server.Dispose();
        }

        [Fact]
        public async Task IsNewConnection_Property_False()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);

            var stream = new SocketPoolStream(client, hostAndPort, (s, h, c) => { }, isNewConnection: false);

            Assert.False(stream.IsNewConnection);
            
            server.Dispose();
        }

        [Fact]
        public async Task DisposeSocket_CallsReturnSocketCallback_WithCloseSocketTrue()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var returnSocketCalled = false;
            var closeSocketValue = false;
            var receivedSocket = (Socket?)null;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                returnSocketCalled = true;
                closeSocketValue = c;
                receivedSocket = s;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.DisposeSocket();

            Assert.True(returnSocketCalled);
            Assert.True(closeSocketValue);
            Assert.Same(client, receivedSocket);
            
            server.Dispose();
        }

        [Fact]
        public async Task DisposeNoReturnSocket_DoesNotCallReturnSocketCallback()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var returnSocketCalled = false;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                returnSocketCalled = true;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.DisposeNoReturnSocket();

            Assert.False(returnSocketCalled);
            
            server.Dispose();
        }

        [Fact]
        public async Task Dispose_CallsReturnSocketCallback()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var returnSocketCalled = false;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                returnSocketCalled = true;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.Dispose();

            Assert.True(returnSocketCalled);
            
            server.Dispose();
        }

        [Fact]
        public async Task DisposeNoReturnSocket_ThenDispose_DoesNotCallReturnSocketCallback()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var returnSocketCalled = false;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                returnSocketCalled = true;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.DisposeNoReturnSocket();
            stream.Dispose();

            Assert.False(returnSocketCalled);
            
            server.Dispose();
        }

        [Fact]
        public async Task NormalDispose_DoesNotSetCloseSocketFlag()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var closeSocketValue = true;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                closeSocketValue = c;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.Dispose();

            Assert.False(closeSocketValue);
            
            server.Dispose();
        }

        [Fact]
        public async Task ReturnSocket_ReceivesCorrectHostAndPort()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            HostAndPort? receivedHostAndPort = null;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                receivedHostAndPort = h;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.Dispose();

            Assert.NotNull(receivedHostAndPort);
            Assert.Equal(hostAndPort.Host, receivedHostAndPort.Host);
            Assert.Equal(hostAndPort.Port, receivedHostAndPort.Port);
            
            server.Dispose();
        }

        [Fact]
        public async Task MultipleCalls_ToDispose_OnlyCallsReturnSocketOnce()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            var returnSocketCallCount = 0;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                returnSocketCallCount++;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.Dispose();
            stream.Dispose();

            Assert.Equal(1, returnSocketCallCount);
            
            server.Dispose();
        }

        [Fact]
        public async Task DisposeSocket_PassesSocketToReturnSocketCallback()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);
            Socket? capturedSocket = null;

            Action<Socket, HostAndPort, bool> returnSocket = (s, h, c) =>
            {
                capturedSocket = s;
            };

            var stream = new SocketPoolStream(client, hostAndPort, returnSocket, isNewConnection: true);
            stream.DisposeSocket();

            Assert.Same(client, capturedSocket);
            
            server.Dispose();
        }

        [Fact]
        public async Task Connected_Property_ReflectsSocketState()
        {
            var (client, server, port) = await CreateConnectedSocketPairAsync();
            var hostAndPort = new HostAndPort("127.0.0.1", port);

            var stream = new SocketPoolStream(client, hostAndPort, (s, h, c) => { }, isNewConnection: true);

            Assert.True(stream.Connected);
            
            stream.Dispose();
            server.Dispose();
        }
    }
}
