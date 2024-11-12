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
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public abstract class TcpCqrsServerBase : IQueryServer, ICommandConsumer, IDisposable
    {
        protected readonly ConcurrentReadWriteHashSet<Type> interfaceTypes;
        protected readonly ConcurrentReadWriteHashSet<Type> commandTypes;
        private readonly Type thisType;

        private SocketListener[]? listeners = null;
        protected QueryHandlerDelegate? providerHandlerAsync = null;
        protected HandleRemoteCommandDispatch? handlerAsync = null;
        protected HandleRemoteCommandDispatch? handlerAwaitAsync = null;
        protected HandleRemoteCommandWithResultDispatch? handlerWithResultAwaitAsync = null;

        private bool started = false;
        private bool disposed = false;

        protected CommandCounter? commandCounter = null;
        protected SemaphoreSlim? throttle = null;

        private readonly string serviceUrl;

        public TcpCqrsServerBase(string serviceUrl)
        {
            this.serviceUrl = serviceUrl;
            this.interfaceTypes = new();
            this.commandTypes = new();
            this.thisType = this.GetType();
        }

        void IQueryServer.Setup(CommandCounter commandCounter, QueryHandlerDelegate handlerAsync)
        {
            this.commandCounter = commandCounter;
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (commandTypes.Count > 0)
                throw new Exception($"Cannot register interface because this instance of {thisType.GetNiceName()} is already being used for commands");

            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            interfaceTypes.Add(type);
        }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            this.commandCounter = commandCounter;
            this.handlerAsync = handlerAsync;
            this.handlerAwaitAsync = handlerAwaitAsync;
            this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (interfaceTypes.Count > 0)
                throw new Exception($"Cannot register command because this instance of {thisType.GetNiceName()} is already being used for queries");

            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            commandTypes.Add(type);
        }

        void IQueryServer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Query Server Started On {this.serviceUrl}");
        }
        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Command Consumer Started On {this.serviceUrl}");
        }
        protected void Open()
        {
            lock (interfaceTypes)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(TcpCqrsServerBase));
                if (started)
                    return;

#if NETSTANDARD2_0
                var urls = serviceUrl.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
#else
                var urls = serviceUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
#endif
                var endpoints = IPResolver.GetIPEndPoints(urls);
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

                _ = Log.InfoAsync($"{thisType.GetNiceName()} resolved {serviceUrl} as {String.Join(", ", endpoints.Select(x => x.ToString()))}");

                foreach (var listener in listeners)
                    listener.Open();

                started = true;
            }
        }

        protected abstract Task Handle(Socket socket, CancellationToken cancellationToken);

        void IQueryServer.Close()
        {
            Dispose();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Query Server Closed On {this.serviceUrl}");
        }
        void ICommandConsumer.Close()
        {
            Dispose();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Command Consumer Closed On {this.serviceUrl}");
        }

        public void Dispose()
        {
            lock (interfaceTypes)
            {
                if (disposed)
                    return;
                if (listeners is not null)
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