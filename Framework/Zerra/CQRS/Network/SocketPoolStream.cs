// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    internal class SocketPoolStream : StreamWrapper
    {
        public bool Connected => socket?.Connected ?? false;
        public bool IsNewConnection { get; }

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
