// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.CQRS.Network
{
    public class SocketClientPool : IDisposable
    {
        public static readonly SocketClientPool Shared = new();

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
        private readonly ConcurrentFactoryDictionary<HostAndPort, SemaphoreSlim> throttleByHostAndPort = new();

        public SocketPoolStream BeginStream(string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

        getstream:
            throttle.Wait(canceller.Token); //disposing releases throttle so we enter again
            SocketPoolStream? stream = null;
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
                    stream = new SocketPoolStream(holder.Socket, hostAndPort, ReturnSocket, false);
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

            Exception? lastex = null;
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
                    stream = new SocketPoolStream(socket, hostAndPort, ReturnSocket, true);
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
            if (lastex != null)
                throw new ConnectionFailedException(lastex);
            else
                throw new ConnectionFailedException();
        }
#if !NETSTANDARD2_0
        public SocketPoolStream BeginStream(string host, int port, ProtocolType protocol, Span<byte> buffer)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

        getstream:
            throttle.Wait(canceller.Token); //disposing releases throttle so we enter again
            SocketPoolStream? stream = null;
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
                    stream = new SocketPoolStream(holder.Socket, hostAndPort, ReturnSocket, false);
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

            Exception? lastex = null;
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
                    stream = new SocketPoolStream(socket, hostAndPort, ReturnSocket, true);
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
            if (lastex != null)
                throw new ConnectionFailedException(lastex);
            else
                throw new ConnectionFailedException();
        }
#endif

        public async Task<SocketPoolStream> BeginStreamAsync(string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

        getstream:
            await throttle.WaitAsync(canceller.Token); //disposing releases throttle so we enter again
            SocketPoolStream? stream = null;
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
                    stream = new SocketPoolStream(holder.Socket, hostAndPort, ReturnSocket, false);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
#if !NETSTANDARD2_0
                    await stream.WriteAsync(buffer, cancellationToken);
#else
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
#endif
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

            Exception? lastex = null;
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
                    stream = new SocketPoolStream(socket, hostAndPort, ReturnSocket, true);

#if !NETSTANDARD2_0
                    await stream.WriteAsync(buffer, cancellationToken);
#else
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
#endif

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
            if (lastex != null)
                throw new ConnectionFailedException(lastex);
            else
                throw new ConnectionFailedException();
        }
#if !NETSTANDARD2_0
        public async Task<SocketPoolStream> BeginStreamAsync(string host, int port, ProtocolType protocol, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, (_) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, (_) => new());

        getstream:
            await throttle.WaitAsync(canceller.Token); //disposing releases throttle so we enter again
            SocketPoolStream? stream = null;
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
                    stream = new SocketPoolStream(holder.Socket, hostAndPort, ReturnSocket, false);
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

            Exception? lastex = null;
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
                    stream = new SocketPoolStream(socket, hostAndPort, ReturnSocket, true);
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
            if (lastex != null)
                throw new ConnectionFailedException(lastex);
            else
                throw new ConnectionFailedException();
        }
#endif

        private void ReturnSocket(Socket socket, HostAndPort hostAndPort, bool closeSocket)
        {
            if (canceller.IsCancellationRequested)
            {
                socket.Dispose();
                return;
            }

            if (!closeSocket && socket.Connected)
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

            if (throttleByHostAndPort.TryGetValue(hostAndPort, out var throttle))
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

            foreach (var throttle in throttleByHostAndPort.Values)
                throttle.Dispose();

            canceller.Dispose();
            GC.SuppressFinalize(this);
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
    }
}
