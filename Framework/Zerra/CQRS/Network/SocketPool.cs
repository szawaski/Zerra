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
    public class SocketPool : IDisposable
    {
        public static readonly SocketPool Default = new();

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

        private int maxConnectionsPerServer = Environment.ProcessorCount * 32;
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
        public SocketPool()
        {
            this.canceller = new();
            _ = Timeout(canceller.Token);
        }

        private readonly ConcurrentFactoryDictionary<IPEndPoint, ConcurrentQueue<SocketHolder>> poolByEndpoint = new();
        private readonly ConcurrentFactoryDictionary<IPAddress, SemaphoreSlim> throttleByServer = new();
        public Stream GetStream(IPEndPoint endpoint)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketPool));

            var throttle = throttleByServer.GetOrAdd(endpoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            throttle.Wait();
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketPool));

            var pool = poolByEndpoint.GetOrAdd(endpoint, (_) => new());

            void Completed(Socket socket)
            {
                if (canceller.IsCancellationRequested)
                {
                    socket.Dispose();
                    return;
                }

                if (socket.Connected)
                {
                    try
                    {
                        if (socket.Poll(pollMicroseconds, pollSelectMode))
                            pool.Enqueue(new SocketHolder(socket));
                        else
                            socket.Dispose();
                    }
                    catch
                    {
                        socket.Dispose();
                    }
                }
                else
                {
                    socket.Dispose();
                }

                throttle.Release();
            }

        tryagain:
            if (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    holder.MarkUsed();
                    if (holder.Socket.Connected)
                    {
                        try
                        {
                            if (holder.Socket.Poll(pollMicroseconds, pollSelectMode))
                                return new SocketStream(holder.Socket, Completed);
                            else
                                holder.Socket.Dispose();
                        }
                        catch
                        {
                            holder.Socket.Dispose();
                        }
                    }
                    else
                    {
                        holder.Socket.Dispose();
                    }
                    goto tryagain;
                }
            }

            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.Connect(endpoint.Address, endpoint.Port);

            return new SocketStream(socket, Completed);
        }
        public async ValueTask<Stream> GetStreamAsync(IPEndPoint endpoint)
        {
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketPool));

            var throttle = throttleByServer.GetOrAdd(endpoint.Address, (_) => new(maxConnectionsPerServer, maxConnectionsPerServer));
            await throttle.WaitAsync();
            if (canceller.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SocketPool));

            var pool = poolByEndpoint.GetOrAdd(endpoint, (_) => new());

            void Completed(Socket socket)
            {
                if (canceller.IsCancellationRequested)
                {
                    socket.Dispose();
                    return;
                }

                if (socket.Connected)
                {
                    try
                    {
                        if (socket.Poll(pollMicroseconds, pollSelectMode))
                            pool.Enqueue(new SocketHolder(socket));
                        else
                            socket.Dispose();
                    }
                    catch
                    {
                        socket.Dispose();
                    }
                }
                else
                {
                    socket.Dispose();
                }

                throttle.Release();
            }


        tryagain:
            if (pool.TryDequeue(out var holder))
            {
                lock (holder)
                {
                    holder.MarkUsed();
                    if (holder.Socket.Connected)
                    {
                        try
                        {
                            if (holder.Socket.Poll(pollMicroseconds, pollSelectMode))
                                return new SocketStream(holder.Socket, Completed);
                            else
                                holder.Socket.Dispose();
                        }
                        catch
                        {
                            holder.Socket.Dispose();
                        }
                    }
                    else
                    {
                        holder.Socket.Dispose();
                    }
                    goto tryagain;
                }
            }

            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            await socket.ConnectAsync(endpoint.Address, endpoint.Port);

            return new SocketStream(socket, Completed);
        }

        private async Task Timeout(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                await Task.Delay(pooledConnectionIdleTimeout, cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var kvp in poolByEndpoint)
                {
                    var pool = kvp.Value;
                    if (pool.Count == 0)
                    {
                        if (poolByEndpoint.TryRemove(kvp.Key, out _))
                        {
                            if (pool.Count == 0)
                                continue;

                            //pool was used after Count == 0 before TryRemove
                            var newpool = poolByEndpoint.GetOrAdd(kvp.Key, (_) => new());
                            foreach (var holder in pool)
                                newpool.Enqueue(holder);
                        }
                    }
                    foreach (var holder in pool)
                    {
                        lock (holder)
                        {
                            if (holder.Used)
                                continue;

                            if (holder.Timestamp.Add(pooledConnectionLifetime) < now)
                            {
                                holder.Socket.Dispose();
                            }
                            else
                            {
                                if (holder.Socket.Connected)
                                {
                                    try
                                    {
                                        if (!holder.Socket.Poll(pollMicroseconds, pollSelectMode))
                                            holder.Socket.Dispose();
                                    }
                                    catch
                                    {
                                        holder.Socket.Dispose();
                                    }
                                }
                                else
                                {
                                    holder.Socket.Dispose();
                                }
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
            foreach (var kvp in poolByEndpoint)
            {
                foreach (var holder in kvp.Value)
                    holder.Socket.Dispose();
            }
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
            private readonly Action<Socket> completed;
            public SocketStream(Socket socket, Action<Socket> completed)
                : base(new NetworkStream(socket, false), false)
            {
                this.socket = socket;
                this.completed = completed;
            }

            protected override void Dispose(bool disposing)
            {
                if (socket != null)
                {
                    completed(socket);
                    socket = null;
                    base.Dispose(disposing);
                }
            }
        }
    }
}