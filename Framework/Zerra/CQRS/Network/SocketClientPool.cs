﻿// Copyright © KaKush LLC
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

        private int maxConnectionsPerServer = Environment.ProcessorCount * 8;
        public int MaxConnectionsPerServer
        {
            get => maxConnectionsPerServer;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxConnectionsPerServer));
                maxConnectionsPerServer = value;
            }
        }

        private readonly CancellationTokenSource canceller;
        public SocketClientPool()
        {
            this.canceller = new();
            _ = Timeout(canceller.Token);
        }

        private readonly ConcurrentFactoryDictionary<IPEndPoint, ConcurrentQueue<SocketHolder>> poolByEndpoint = new();
        private readonly ConcurrentFactoryDictionary<IPAddress, SemaphoreSlim> throttleByServer = new();

        public Stream BeginStream(IPEndPoint endPoint, ProtocolType protocol, byte[] buffer, int offset, int count)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var throttle = throttleByServer.GetOrAdd(endPoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            var pool = poolByEndpoint.GetOrAdd(endPoint, (_) => new());

        getstream:
            Stream stream = null;
            throttle.Wait(canceller.Token);
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
                    stream = new SocketStream(holder.Socket, endPoint, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    stream.Write(buffer, offset, count);
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

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
            socket.NoDelay = true;
            socket.Connect(endPoint.Address, endPoint.Port);
            stream = new SocketStream(socket, endPoint, ReturnSocket);
            stream.Write(buffer, offset, count);
            return stream;
        }
#if !NETSTANDARD2_0
        public Stream BeginStream(IPEndPoint endPoint, ProtocolType protocol, Span<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var throttle = throttleByServer.GetOrAdd(endPoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            var pool = poolByEndpoint.GetOrAdd(endPoint, (_) => new());

        getstream:
            Stream stream = null;
            throttle.Wait(canceller.Token);
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
                    stream = new SocketStream(holder.Socket, endPoint, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    stream.Write(buffer);
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

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
            socket.NoDelay = true;
            socket.Connect(endPoint.Address, endPoint.Port);
            stream = new SocketStream(socket, endPoint, ReturnSocket);
            stream.Write(buffer);
            return stream;
        }
#endif

        public async Task<Stream> BeginStreamAsync(IPEndPoint endPoint, ProtocolType protocol, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var throttle = throttleByServer.GetOrAdd(endPoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            var pool = poolByEndpoint.GetOrAdd(endPoint, (_) => new());

        getstream:
            Stream stream = null;
            await throttle.WaitAsync(canceller.Token);
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
                    stream = new SocketStream(holder.Socket, endPoint, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    await stream.WriteAsync(buffer, offset, count, cancellationToken);
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

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
            socket.NoDelay = true;
#if NET5_0_OR_GREATER
            await socket.ConnectAsync(endPoint.Address, endPoint.Port, cancellationToken);
#else
            await socket.ConnectAsync(endPoint.Address, endPoint.Port);
#endif
            stream = new SocketStream(socket, endPoint, ReturnSocket);
            await stream.WriteAsync(buffer, offset, count, cancellationToken);
            return stream;
        }
#if !NETSTANDARD2_0
        public async Task<Stream> BeginStreamAsync(IPEndPoint endPoint, ProtocolType protocol, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketClientPool));

            var throttle = throttleByServer.GetOrAdd(endPoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            var pool = poolByEndpoint.GetOrAdd(endPoint, (_) => new());

        getstream:
            Stream stream = null;
            await throttle.WaitAsync(canceller.Token);
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
                    stream = new SocketStream(holder.Socket, endPoint, ReturnSocket);
                    break;
                }
                holder.Socket.Dispose();
            }
            if (stream != null)
            {
                try
                {
                    await stream.WriteAsync(buffer, cancellationToken);
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

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocol);
            socket.NoDelay = true;
#if NET5_0_OR_GREATER
            await socket.ConnectAsync(endPoint.Address, endPoint.Port, cancellationToken);
#else
            await socket.ConnectAsync(endPoint.Address, endPoint.Port);
#endif
            stream = new SocketStream(socket, endPoint, ReturnSocket);
            await stream.WriteAsync(buffer, cancellationToken);
            return stream;
        }
#endif

        private void ReturnSocket(Socket socket, IPEndPoint endPoint)
        {
            if (canceller.IsCancellationRequested)
            {
                socket.Dispose();
                return;
            }

            if (socket.Connected)
            {
                if (poolByEndpoint.TryGetValue(endPoint, out var pool))
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

            if (throttleByServer.TryGetValue(endPoint.Address, out var throttle))
                throttle.Release();
        }

        private async Task Timeout(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                await Task.Delay(pooledConnectionIdleTimeout, cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var pool in poolByEndpoint.Values)
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

            foreach (var pool in poolByEndpoint.Values)
            {
                lock (pool)
                {
                    while (pool.TryDequeue(out var holder))
                        holder.Socket.Dispose();
                }
            }

            foreach (var throttle in throttleByServer.Values)
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
            private Socket socket;
            private readonly IPEndPoint endPoint;
            private readonly Action<Socket, IPEndPoint> returnSocket;
            public SocketStream(Socket socket, IPEndPoint endPoint, Action<Socket, IPEndPoint> returnSocket)
                : base(new NetworkStream(socket, false), false)
            {
                this.socket = socket;
                this.endPoint = endPoint;
                this.returnSocket = returnSocket;
            }

            protected override void Dispose(bool disposing)
            {
                if (socket != null)
                {
                    returnSocket(socket, endPoint);
                    socket = null;
                    base.Dispose(disposing);
                }
            }
        }
    }
}