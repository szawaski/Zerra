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
    internal class SocketClientPool : IDisposable
    {
        public static readonly SocketClientPool Shared = new();

        private readonly TimeSpan pooledConnectionLifetime = TimeSpan.FromMinutes(10);
        private readonly TimeSpan lifetimeTimeoutCheckInterval = TimeSpan.FromMinutes(2);

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
            _ = LifetimeTimeout(canceller.Token);
        }

        private readonly ConcurrentFactoryDictionary<HostAndPort, ConcurrentQueue<SocketHolder>> poolByHostAndPort = new();
        private readonly ConcurrentFactoryDictionary<HostAndPort, SemaphoreSlim> throttleByHostAndPort = new();

        public async Task<SocketPoolStream> BeginStreamAsync(string host, int port, ProtocolType protocol, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, maxConnectionsPerHost, static (maxConnectionsPerHost) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, static () => new());

        getstream:
            var noRelease = false;
            await throttle.WaitAsync(canceller.Token); //disposing stream releases throttle so we enter again
            try
            {
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
                if (stream is not null)
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
                            noRelease = true;
                            goto getstream;
                        }
                        noRelease = true;
                        return stream;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetBaseException() is SocketException)
                        {
                            stream.Dispose();
                            noRelease = true;
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

                    Socket? socket = null;
                    try
                    {
                        var endPoint = new IPEndPoint(ip, port);

                        socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
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
                            socket.Dispose();
                            stream.DisposeNoReturnSocket();
                            continue;
                        }
                        noRelease = true;
                        return stream;
                    }
                    catch (Exception ex)
                    {
                        socket?.Dispose();
                        stream?.DisposeNoReturnSocket();
                        lastex = ex;
                    }
                }

                if (lastex is not null)
                    throw new ConnectionFailedException(lastex);
                else
                    throw new ConnectionFailedException();
            }
            finally
            {
                if (!noRelease)
                    _ = throttle.Release();
            }
        }
#if !NETSTANDARD2_0
        public async Task<SocketPoolStream> BeginStreamAsync(string host, int port, ProtocolType protocol, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hostAndPort = new HostAndPort(host, port);

            var throttle = throttleByHostAndPort.GetOrAdd(hostAndPort, maxConnectionsPerHost, static (maxConnectionsPerHost) => new(maxConnectionsPerHost, maxConnectionsPerHost));
            var pool = poolByHostAndPort.GetOrAdd(hostAndPort, static (maxConnectionsPerHost) => new());

        getstream:
            var noRelease = false;
            await throttle.WaitAsync(canceller.Token); //disposing stream releases throttle so we enter again
            try
            {
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
                if (stream is not null)
                {
                    try
                    {
                        await stream.WriteAsync(buffer, cancellationToken);
                        if (!stream.Connected)
                        {
                            stream.Dispose();
                            noRelease = true;
                            goto getstream;
                        }
                        noRelease = true;
                        return stream;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetBaseException() is SocketException)
                        {
                            stream.Dispose();
                            noRelease = true;
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

                    Socket? socket = null;
                    try
                    {
                        var endPoint = new IPEndPoint(ip, port);

                        socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
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
                            socket.Dispose();
                            stream.DisposeNoReturnSocket();
                            continue;
                        }
                        noRelease = true;
                        return stream;
                    }
                    catch (Exception ex)
                    {
                        socket?.Dispose();
                        stream?.DisposeNoReturnSocket();
                        lastex = ex;
                    }
                }

                if (lastex is not null)
                    throw new ConnectionFailedException(lastex);
                else
                    throw new ConnectionFailedException();
            }
            finally
            {
                if (!noRelease)
                    _ = throttle.Release();
            }
        }
#endif

        private void ReturnSocket(Socket socket, HostAndPort hostAndPort, bool closeSocket)
        {
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

        private async Task LifetimeTimeout(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                await Task.Delay(lifetimeTimeoutCheckInterval, cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var pool in poolByHostAndPort.Values)
                {
                    foreach (var holder in pool)
                    {
                        lock (holder)
                        {
                            if (holder.Used)
                                continue;

                            if (!holder.Socket.Connected || holder.Timestamp.Add(pooledConnectionLifetime) < now)
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
