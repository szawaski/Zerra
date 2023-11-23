// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Net.Sockets;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public class SocketPoolStream : StreamWrapper
    {
        public bool Connected => socket?.Connected ?? false;
        public bool NewConnection { get; private set; }

        private Socket socket;
        private bool closeSocket;
        private readonly HostAndPort hostAndPort;
        private readonly Action<Socket, HostAndPort, bool> returnSocket;
        public SocketPoolStream(Socket socket, HostAndPort hostAndPort, Action<Socket, HostAndPort, bool> returnSocket, bool newConnection)
            : base(new NetworkStream(socket, false), false)
        {
            this.socket = socket;
            this.closeSocket = false;
            this.hostAndPort = hostAndPort;
            this.returnSocket = returnSocket;
            this.NewConnection = newConnection;
        }

        public void DisposeSocket()
        {
            closeSocket = true;
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (socket != null)
            {
                returnSocket(socket, hostAndPort, closeSocket);
                socket = null;
                base.Dispose(disposing);
            }
        }
    }
}
