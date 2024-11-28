// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Net.Sockets;
using System.Threading;
using Zerra.Collections;
using Zerra.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Zerra.CQRS.Network
{
    public abstract class CqrsServerBase : IQueryServer, ICommandConsumer, IEventConsumer, IDisposable
    {
        protected readonly ConcurrentReadWriteHashSet<Type> types;
        private readonly Type thisType;
        
        private SocketListener[]? listeners = null;
        protected QueryHandlerDelegate? providerHandlerAsync = null;
        protected HandleRemoteCommandDispatch? commandHandlerAsync = null;
        protected HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        protected HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync = null;
        protected HandleRemoteEventDispatch? eventHandlerAsync = null;

        private bool started = false;
        private bool disposed = false;

        protected CommandCounter? commandCounter = null;
        protected SemaphoreSlim? throttle = null;

        private readonly string serviceUrl;

        public CqrsServerBase(string serviceUrl)
        {
            this.serviceUrl = serviceUrl;
            this.types = new();
            this.thisType = this.GetType();
        }

        void IQueryServer.Setup(CommandCounter commandCounter, QueryHandlerDelegate handlerAsync)
        {
            this.commandCounter = commandCounter;
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (types.Count > 0)
                throw new Exception($"Cannot register interface because this instance of {thisType.GetNiceName()} is already being used for commands");

            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            types.Add(type);
        }

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            this.commandCounter = commandCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
            this.commandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            types.Add(type);
        }

        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            this.eventHandlerAsync = handlerAsync;
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            types.Add(type);
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
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Event Consumer Started On {this.serviceUrl}");
        }
        protected void Open()
        {
            lock (types)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(CqrsServerBase));
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
        void IEventConsumer.Close()
        {
            Dispose();
            _ = Log.InfoAsync($"{thisType.GetNiceName()} Event Consumer Closed On {this.serviceUrl}");
        }

        public void Dispose()
        {
            lock (types)
            {
                if (disposed)
                    return;
                if (listeners is not null)
                {
                    foreach (var listener in listeners)
                        listener.Dispose();
                    listeners = null;
                }
                types.Dispose();
                types.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}