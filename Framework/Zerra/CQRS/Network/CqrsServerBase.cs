// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using Zerra.Collections;
using Zerra.Logging;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// The base class for a CQRS Server using Sockets.
    /// </summary>
    public abstract class CqrsServerBase : IQueryServer, ICommandConsumer, IEventConsumer, IDisposable
    {
        /// <summary>
        /// The types registered for this server to handle.
        /// </summary>
        protected readonly ConcurrentReadWriteHashSet<Type> types;
        private readonly Type thisType;
        
        private SocketListener[]? listeners = null;
        /// <summary>
        /// Delegate to the <see cref="Bus"/> to handle queries.
        /// </summary>
        protected QueryHandlerDelegate? providerHandlerAsync = null;
        /// <summary>
        /// Delegate to the <see cref="Bus"/> to handle commands without response.
        /// </summary>
        protected HandleRemoteCommandDispatch? commandHandlerAsync = null;
        /// <summary>
        /// Delegate to the <see cref="Bus"/> to handle commands that will wait to respond when completed.
        /// </summary>
        protected HandleRemoteCommandDispatch? commandHandlerAwaitAsync = null;
        /// <summary>
        /// Delegate to the <see cref="Bus"/> to handle commands that will respond with a result.
        /// </summary>
        protected HandleRemoteCommandWithResultDispatch? commandHandlerWithResultAwaitAsync = null;
        /// <summary>
        /// Delegate to the <see cref="Bus"/> to handle events.
        /// </summary>
        protected HandleRemoteEventDispatch? eventHandlerAsync = null;

        private bool started = false;
        private bool disposed = false;

        /// <summary>
        /// A counter to limit the number commands the running service will receive before termination.
        /// </summary>
        protected CommandCounter? commandCounter = null;
        /// <summary>
        /// A throttle to limit the number of requests processed simulataneously.
        /// </summary>
        protected SemaphoreSlim? throttle = null;

        private readonly string serviceUrl;
        /// <summary>
        /// The logging provider.
        /// </summary>
        protected readonly ILogger? log;

        /// <summary>
        /// Required by the inheriting class to call this constructor for information the Socket needs.
        /// </summary>
        /// <param name="serviceUrl">The url under which the Socket will be listening.</param>
        public CqrsServerBase(string serviceUrl, ILogger? log)
        {
            this.serviceUrl = serviceUrl;
            this.log = log;
            this.types = new();
            this.thisType = this.GetType();
        }

        string IQueryServer.ServiceUrl => serviceUrl;
        string ICommandConsumer.MessageHost => serviceUrl;
        string IEventConsumer.MessageHost => serviceUrl;

        void IQueryServer.Setup(CommandCounter commandCounter, QueryHandlerDelegate handlerAsync)
        {
            this.commandCounter = commandCounter;
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            _ = types.Add(type);
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

            _ = types.Add(type);
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

            _ = types.Add(type);
        }

        void IQueryServer.Open()
        {
            Open();
            log?.Info($"{thisType.GetNiceName()} Query Server Started On {this.serviceUrl}");
        }
        void ICommandConsumer.Open()
        {
            Open();
            log?.Info($"{thisType.GetNiceName()} Command Consumer Started On {this.serviceUrl}");
        }
        void IEventConsumer.Open()
        {
            Open();
            log?.Info($"{thisType.GetNiceName()} Event Consumer Started On {this.serviceUrl}");
        }
        private void Open()
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

                log?.Info($"{thisType.GetNiceName()} resolved {serviceUrl} as {String.Join(", ", endpoints.Select(x => x.ToString()))}");

                foreach (var listener in listeners)
                    listener.Open();

                started = true;
            }
        }

        /// <summary>
        /// Handles all incomming CQRS requests.
        /// </summary>
        /// <param name="socket">The raw socket connection</param>
        /// <param name="cancellationToken">The Cancellation Token to observe.</param>
        /// <returns>A Task to await completion of handling the request.</returns>
        protected abstract Task Handle(Socket socket, CancellationToken cancellationToken);

        void IQueryServer.Close()
        {
            Dispose();
            log?.Info($"{thisType.GetNiceName()} Query Server Closed On {this.serviceUrl}");
        }
        void ICommandConsumer.Close()
        {
            Dispose();
            log?.Info($"{thisType.GetNiceName()} Command Consumer Closed On {this.serviceUrl}");
        }
        void IEventConsumer.Close()
        {
            Dispose();
            log?.Info($"{thisType.GetNiceName()} Event Consumer Closed On {this.serviceUrl}");
        }

        /// <inheritdoc />
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