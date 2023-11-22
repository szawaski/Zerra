// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public class SocketClientPool : IDisposable
    {
        public static readonly SocketClientPool Default = new();

        private const int pollMicroseconds = 1000;
        private const SelectMode pollSelectMode = SelectMode.SelectWrite;

        private TimeSpan pooledConnectionLifetime = TimeSpan.FromMinutes(10);
        public TimeSpan PooledConnectionLifetime
        {
            get => pooledConnectionLifetime;
            set
            {
                if (value.TotalMilliseconds < 0)
                    throw new ArgumentOutOfRangeException(nameof(PooledConnectionLifetime));
                pooledConnectionLifetime = value;
            }
        }

        private TimeSpan pooledConnectionIdleTimeout = TimeSpan.FromMinutes(2);
        public TimeSpan PooledConnectionIdleTimeout
        {
            get => pooledConnectionIdleTimeout;
            set
            {
                if (value.TotalMilliseconds < 0)
                    throw new ArgumentOutOfRangeException(nameof(PooledConnectionIdleTimeout));
                pooledConnectionIdleTimeout = value;
            }
        }

        private int maxConnectionsPerHost = Environment.ProcessorCount * 8;
        public int MaxConnectionsPerHost
        {
            get => maxConnectionsPerHost;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxConnectionsPerHost));
                maxConnectionsPerHost = value;
            }
        }

        private readonly CancellationTokenSource canceller;
        public SocketClientPool()
        {
            this.canceller = new();
            _ = Timeout(canceller.Token);
        }

        private readonly ConcurrentFactoryDictionary<HostAndPort, ConcurrentQueue<SocketHolder>> poolByHostAndPort = new();
        private readonly ConcurrentFactoryDictionary<string, SemaphoreSlim> throttleByHost = new();

        public Stream BeginStream(string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            throttle.Wait(canceller.Token);

        getstream:
            SocketStream stream = null;
            while (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    if (holder.Used)
                        continue;
                    holder.MarkUsed();
                }
                if (holder.Socket.Connected)
                {
                    stream = new SocketStream(holder.Socket, hostAndPort, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    stream.Write(buffer, offset, count);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    if (ex.GetBaseException() is SocketException)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    throw;
                }
            }

            var ips = Dns.GetHostAddresses(hostAndPort.Host);

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
                    socket.Connect(endPoint);
                    stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    stream.Write(buffer, offset, count);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#if !NETSTANDARD2_0
        public Stream BeginStream(string host, int port, ProtocolType protocol, Span<byte> buffer)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            throttle.Wait(canceller.Token);

        getstream:
            SocketStream stream = null;
            while (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    if (holder.Used)
                        continue;
                    holder.MarkUsed();
                }
                if (holder.Socket.Connected)
                {
                    stream = new SocketStream(holder.Socket, hostAndPort, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    stream.Write(buffer);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    if (ex.GetBaseException() is SocketException)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    throw;
                }
            }

            var ips = Dns.GetHostAddresses(hostAndPort.Host);

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
                    socket.Connect(endPoint);
                    stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    stream.Write(buffer);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#endif

        public async Task<Stream> BeginStreamAsync(string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            await throttle.WaitAsync(canceller.Token);

        getstream:
            SocketStream stream = null;
            while (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    if (holder.Used)
                        continue;
                    holder.MarkUsed();
                }
                if (holder.Socket.Connected)
                {
                    stream = new SocketStream(holder.Socket, hostAndPort, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    if (ex.GetBaseException() is SocketException)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    throw;
                }
            }

#if NET6_0_OR_GREATER
            var ips = await Dns.GetHostAddressesAsync(host, cancellationToken);
#else
            var ips = await Dns.GetHostAddressesAsync(host);
#endif

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
#if NET5_0_OR_GREATER
                    await socket.ConnectAsync(endPoint, cancellationToken);
#else
                    await socket.ConnectAsync(endPoint);
#endif
                    stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#if !NETSTANDARD2_0
        public async Task<Stream> BeginStreamAsync(string host, int port, ProtocolType protocol, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            await throttle.WaitAsync(canceller.Token);

        getstream:
            SocketStream stream = null;
            while (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    if (holder.Used)
                        continue;
                    holder.MarkUsed();
                }
                if (holder.Socket.Connected)
                {
                    stream = new SocketStream(holder.Socket, hostAndPort, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    await stream.WriteAsync(buffer, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    if (ex.GetBaseException() is SocketException)
                    {
                        stream.Dispose();
                        goto getstream;
                    }
                    throw;
                }
            }

#if NET6_0_OR_GREATER
            var ips = await Dns.GetHostAddressesAsync(host, cancellationToken);
#else
            var ips = await Dns.GetHostAddressesAsync(host);
#endif

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
#if NET5_0_OR_GREATER
                    await socket.ConnectAsync(endPoint, cancellationToken);
#else
                    await socket.ConnectAsync(endPoint);
#endif
                    stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    await stream.WriteAsync(buffer, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#endif

        public Stream ReconnectBeginStream(Stream returnStream, string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            returnStream.Dispose();
            throttle.Wait(canceller.Token);

            var ips = Dns.GetHostAddresses(host);

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
                    socket.Connect(endPoint);
                    var stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    stream.Write(buffer, offset, count);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#if !NETSTANDARD2_0
        public Stream ReconnectBeginStream(Stream returnStream, string host, int port, ProtocolType protocol, Span<byte> buffer)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            returnStream.Dispose();
            throttle.Wait(canceller.Token);

            var ips = Dns.GetHostAddresses(host);

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
                    socket.Connect(endPoint);
                    var stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    stream.Write(buffer);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#endif

        public async Task<Stream> ReconnectBeginStreamAsync(Stream returnStream, string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            returnStream.Dispose();
            throttle.Wait(canceller.Token);

#if NET6_0_OR_GREATER
            var ips = await Dns.GetHostAddressesAsync(host, cancellationToken);
#else
            var ips = await Dns.GetHostAddressesAsync(host);
#endif

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
#if NET5_0_OR_GREATER
                    await socket.ConnectAsync(endPoint, cancellationToken);
#else
                    await socket.ConnectAsync(endPoint);
#endif
                    var stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#if !NETSTANDARD2_0
        public async Task<Stream> ReconnectBeginStreamAsync(Stream returnStream, string host, int port, ProtocolType protocol, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            host = host.ToLower();
            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHost.GetOrAdd(hostAndPort.Host, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

            returnStream.Dispose();
            throttle.Wait(canceller.Token);

#if NET6_0_OR_GREATER
            var ips = await Dns.GetHostAddressesAsync(host, cancellationToken);
#else
            var ips = await Dns.GetHostAddressesAsync(host);
#endif

            Exception lastex = null;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;
                try
                {
                    var endPoint = new IPEndPoint(ip, port);

                    var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
                    socket.NoDelay = true;
#if NET5_0_OR_GREATER
                    await socket.ConnectAsync(endPoint, cancellationToken);
#else
                    await socket.ConnectAsync(endPoint);
#endif
                    var stream = new SocketStream(socket, hostAndPort, ReturnSocket);
                    await stream.WriteAsync(buffer, cancellationToken);
                    if (!stream.Connected)
                    {
                        stream.Dispose();
                        continue;
                    }
                    return stream;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                }
            }
            throw new ConnectionFailedException(lastex);
        }
#endif

        private void ReturnSocket(Socket socket, HostAndPort hostAndPort)
        {
            if (canceller.IsCancellationRequested)
            {
                socket.Dispose();
                return;
            }

            if (socket.Connected)
            {
                if (poolByHostAndPort.TryGetValue(hostAndPort, out var pool))
                {
                    lock (pool)
                    {
                        if (canceller.IsCancellationRequested)
                        {
                            socket.Dispose();
                            return;
                        }

                        pool.Enqueue(new SocketHolder(socket));
                    }
                }
                else
                {
                    socket.Dispose();
                }
            }
            else
            {
                socket.Dispose();
            }

            if (throttleByHost.TryGetValue(hostAndPort.Host, out var throttle))
                throttle.Release();
        }

        private async Task Timeout(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                await Task.Delay(pooledConnectionIdleTimeout, cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var pool in poolByHostAndPort.Values)
                {
                    foreach (var holder in pool)
                    {
                        lock (holder)
                        {
                            if (holder.Used)
                                continue;

                            if (holder.Timestamp.Add(pooledConnectionLifetime) < now)
                            {
                                holder.Socket.Dispose();
                                holder.MarkUsed();
                            }
                            else if (!holder.Socket.Connected)
                            {
                                holder.Socket.Dispose();
                                holder.MarkUsed();
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (canceller.IsCancellationRequested)
                return;

            canceller.Cancel();

            foreach (var pool in poolByHostAndPort.Values)
            {
                lock (pool)
                {
                    while (pool.TryDequeue(out var holder))
                        holder.Socket.Dispose();
                }
            }

            foreach (var throttle in throttleByHost.Values)
                throttle.Dispose();

            canceller.Dispose();
        }

        private class SocketHolder
        {
            public Socket Socket => socket;
            public DateTime Timestamp => timestamp;
            public bool Used => used;

            private bool used;

            private readonly Socket socket;
            private readonly DateTime timestamp;
            public SocketHolder(Socket socket)
            {
                this.socket = socket;
                this.timestamp = DateTime.UtcNow;
            }

            public void MarkUsed() => used = true;
        }

        private class SocketStream : StreamWrapper
        {
            public bool Connected => socket?.Connected ?? false;

            private Socket socket;
            private readonly HostAndPort hostAndPort;
            private readonly Action<Socket, HostAndPort> returnSocket;
            public SocketStream(Socket socket, HostAndPort hostAndPort, Action<Socket, HostAndPort> returnSocket)
                : base(new NetworkStream(socket, false), false)
            {
                this.socket = socket;
                this.hostAndPort = hostAndPort;
                this.returnSocket = returnSocket;
            }

            protected override void Dispose(bool disposing)
            {
                if (socket != null)
                {
                    returnSocket(socket, hostAndPort);
                    socket = null;
                    base.Dispose(disposing);
                }
            }
        }

        private class HostAndPort
        {
            public string Host { get; private set; }
            public int Port { get; private set; }
            public HostAndPort(string host, int port)
            {
                this.Host = host;
                this.Port = port;
            }

            public override bool Equals(object obj)
            {
                if (obj is not HostAndPort casted)
                        return false;
                return casted.Port == this.Port && casted.Host.Equals(this.Host, StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode()
            {
#if NETSTANDARD2_0
                unchecked
                {
                    return (int)Math.Pow(Host.GetHashCode(), Port);
                }
#else
                return HashCode.Combine(Host, Port);
#endif
            }
        }
    }
}
