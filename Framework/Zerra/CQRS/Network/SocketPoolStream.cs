// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    internal class SocketPoolStream : StreamWrapper
    {
        public bool Connected => socket?.Connected ?? false;
        public bool IsNewConnection { get; }

        //true when a read returns right away because bytes arrived or the peer closed, false when the peer sent nothing in the time given
        public bool WaitForRead(int waitMicroseconds) => socket is not null && socket.Poll(waitMicroseconds, SelectMode.SelectRead);

        //the same wait without holding the thread
        public async ValueTask<bool> WaitForReadAsync(int waitMicroseconds, CancellationToken cancellationToken)
        {
            var waitUntil = Stopwatch.StartNew();
            var waitMilliseconds = waitMicroseconds / 1000;
            for (; ; )
            {
                if (socket is null)
                    return false;
                if (socket.Poll(0, SelectMode.SelectRead))
                    return true;
                if (waitUntil.ElapsedMilliseconds >= waitMilliseconds)
                    return false;
                await Task.Delay(pollIntervalMilliseconds, cancellationToken);
            }
        }
        private const int pollIntervalMilliseconds = 20;

        private Socket? socket;
        private bool closeSocket;
        private bool noReturnSocket;
        private readonly HostAndPort hostAndPort;
        private readonly Action<Socket, HostAndPort, bool> returnSocket;
        public SocketPoolStream(Socket socket, HostAndPort hostAndPort, Action<Socket, HostAndPort, bool> returnSocket, bool isNewConnection)
            : base(new NetworkStream(socket, false), false)
        {
            this.socket = socket;
            this.closeSocket = false;
            this.noReturnSocket = false;
            this.hostAndPort = hostAndPort;
            this.returnSocket = returnSocket;
            this.IsNewConnection = isNewConnection;
        }

        public void DisposeSocket()
        {
            closeSocket = true;
            base.Dispose();
        }

        public void DisposeNoReturnSocket()
        {
            noReturnSocket = true;
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (socket is not null)
            {
                if (!noReturnSocket)
                    returnSocket(socket, hostAndPort, closeSocket);
                socket = null;
                base.Dispose(disposing);
            }
        }
    }
}
