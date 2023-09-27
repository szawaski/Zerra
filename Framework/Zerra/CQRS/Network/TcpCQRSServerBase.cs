﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Net.Sockets;
using System.Threading;
using Zerra.Collections;
using Zerra.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCQRSServerBase : IQueryServer, ICommandConsumer, IDisposable
    {
        protected readonly ConcurrentReadWriteHashSet<Type> interfaceTypes;
        protected readonly ConcurrentReadWriteHashSet<Type> commandTypes;

        private SocketListener[] listeners = null;
        protected QueryHandlerDelegate providerHandlerAsync = null;
        protected HandleRemoteCommandDispatch handlerAsync = null;
        protected HandleRemoteCommandDispatch handlerAwaitAsync = null;

        private bool started = false;
        private bool disposed = false;

        private ReceiveCounter counter;
        private int? maxConcurrent = null;

        private readonly string serviceUrl;
        public string ServiceUrl => serviceUrl;

        public TcpCQRSServerBase(string serviceUrl)
        {
            this.serviceUrl = serviceUrl;
            this.interfaceTypes = new();
            this.commandTypes = new();
        }

        void IQueryServer.Setup(ReceiveCounter receiveCounter, QueryHandlerDelegate handlerAsync)
        {
            this.counter = receiveCounter;
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (commandTypes.Count > 0)
                throw new Exception($"Cannot register interface because this instance of {this.GetType().GetNiceName()} is already being used for commands");

            if (!this.maxConcurrent.HasValue || maxConcurrent < this.maxConcurrent)
                this.maxConcurrent = maxConcurrent;
            interfaceTypes.Add(type);
        }
        ICollection<Type> IQueryServer.GetInterfaceTypes()
        {
            return interfaceTypes.ToArray();
        }

        void ICommandConsumer.Setup(ReceiveCounter receiveCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            this.counter = receiveCounter;
            this.handlerAsync = handlerAsync;
            this.handlerAwaitAsync = handlerAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (interfaceTypes.Count > 0)
                throw new Exception($"Cannot register command because this instance of {this.GetType().GetNiceName()} is already being used for queries");
            if (!this.maxConcurrent.HasValue || maxConcurrent < this.maxConcurrent.Value)
                this.maxConcurrent = maxConcurrent;
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
            lock (interfaceTypes)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(TcpCQRSServerBase));
                if (started)
                    return;

                var endpoints = IPResolver.GetIPEndPoints(serviceUrl);
                this.listeners = new SocketListener[endpoints.Count];
                for (var i = 0; i < endpoints.Count; i++)
                {
                    var endpoint = endpoints[i];
                    var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.NoDelay = true;
                    socket.Bind(endpoint);
                    var listener = new SocketListener(socket, maxConcurrent.Value, counter, Handle);
                    this.listeners[i] = listener;
                }

                _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} started for {serviceUrl} resolved {String.Join(", ", endpoints.Select(x => x.ToString()))}");

                foreach (var listener in listeners)
                    listener.Open();

                started = true;
            }
        }

        protected abstract Task Handle(Socket socket, CancellationToken cancellationToken);

        void IQueryServer.Close()
        {
            Dispose();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Query Server Closed On {this.serviceUrl}");
        }
        void ICommandConsumer.Close()
        {
            Dispose();
            _ = Log.InfoAsync($"{nameof(TcpCQRSServerBase)} Command Consumer Closed On {this.serviceUrl}");
        }

        public void Dispose()
        {
            lock (interfaceTypes)
            {
                if (disposed)
                    return;
                if (listeners != null)
                {
                    foreach (var listener in listeners)
                        listener.Dispose();
                    listeners = null;
                }
                interfaceTypes.Dispose();
                commandTypes.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}