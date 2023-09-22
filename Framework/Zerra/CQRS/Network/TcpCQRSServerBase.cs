// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Net.Sockets;
using System.Threading;
using Zerra.Collections;
using Zerra.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCQRSServerBase : IDisposable, IQueryServer, ICommandConsumer
    {
        protected readonly ConcurrentReadWriteList<Type> interfaceTypes;
        protected readonly ConcurrentReadWriteList<Type> commandTypes;

        private readonly SocketListener[] listeners;
        protected QueryHandlerDelegate providerHandlerAsync = null;
        protected HandleRemoteCommandDispatch handlerAsync = null;
        protected HandleRemoteCommandDispatch handlerAwaitAsync = null;

        private bool started = false;
        private bool disposed = false;

        private readonly string serviceUrl;
        public string ServiceUrl => serviceUrl;

        public TcpCQRSServerBase(string serverUrl)
        {
            this.serviceUrl = serverUrl;

            var endpoints = IPResolver.GetIPEndPoints(serverUrl);

            this.listeners = new SocketListener[endpoints.Count];
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;
                socket.Bind(endpoint);
                var listener = new SocketListener(socket, Handle);
                this.listeners[i] = listener;
            }

            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} started for {serviceUrl} resolved {String.Join(", ", endpoints.Select(x => x.ToString()))}");

            this.interfaceTypes = new ConcurrentReadWriteList<Type>();
            this.commandTypes = new ConcurrentReadWriteList<Type>();
        }

        void IQueryServer.SetHandler(QueryHandlerDelegate handlerAsync)
        {
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(Type type)
        {
            interfaceTypes.Add(type);
        }
        ICollection<Type> IQueryServer.GetInterfaceTypes()
        {
            return interfaceTypes.ToArray();
        }

        void ICommandConsumer.SetHandler(HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            this.handlerAsync = handlerAsync;
            this.handlerAwaitAsync = handlerAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(Type type)
        {
            commandTypes.Add(type);
        }
        IEnumerable<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandTypes;
        }

        void IQueryServer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Query Server Started On {this.serviceUrl}");
        }
        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Command Consumer Started On {this.serviceUrl}");
        }
        protected void Open()
        {
            lock (listeners)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(TcpCQRSServerBase));
                if (started)
                    return;

                foreach (var listener in listeners)
                    listener.Open();

                started = true;
            }
        }

        protected abstract void Handle(TcpClient client, CancellationToken cancellationToken);

        void IQueryServer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Query Server Closed On {this.serviceUrl}");
        }
        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Command Consumer Closed On {this.serviceUrl}");
        }
        protected void Close()
        {
            lock (listeners)
            {
                foreach (var listener in listeners)
                    listener.Close();
            }
        }

        public void Dispose()
        {
            lock (listeners)
            {
                if (disposed)
                    return;
                foreach (var listener in listeners)
                    listener.Dispose();
                interfaceTypes.Dispose();
                commandTypes.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}