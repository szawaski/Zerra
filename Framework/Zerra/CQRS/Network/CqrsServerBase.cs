// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using Zerra.Collections;
using Zerra.Logging;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// The base class for a CQRS server using sockets.
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
        /// A counter to limit the number of commands the running service will receive before termination, null for no limit.
        /// </summary>
        protected CommandCounter? commandCounter = null;

        /// <summary>
        /// The connection and handler tasks still running, each removed once it completes, so disposing can wait for the rest.
        /// A connection's task ends when it's left idle after closing, a command or event handled after responding is added as its own task.
        /// </summary>
        protected readonly ConcurrentHashSet<Task> running = new();
        /// <summary>
        /// Removes a completed task from <see cref="running"/>, given as the continuation of each task added with <see cref="running"/> as its state.
        /// </summary>
        protected static readonly Action<Task, object?> removeRunning = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
        /// <summary>
        /// A throttle to limit the number of requests processed simultaneously.
        /// </summary>
        protected SemaphoreSlim? throttle = null;

        private readonly string serviceUrl;
        /// <summary>
        /// The logging provider.
        /// </summary>
        protected readonly ILogger? log;

        /// <summary>
        /// Initializes a new instance of the <see cref="CqrsServerBase"/> class.
        /// Required by the inheriting class to call this constructor for information the socket needs.
        /// </summary>
        /// <param name="serviceUrl">The URL under which the socket will be listening.</param>
        /// <param name="log">The optional logging provider.</param>
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

        void IQueryServer.Setup(QueryHandlerDelegate handlerAsync)
        {
            this.providerHandlerAsync = handlerAsync;
        }

        void IQueryServer.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            _ = types.Add(type);
        }

        void ICommandConsumer.Setup(CommandCounter? commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (commandHandlerAsync is not null)
                throw new InvalidOperationException("Command consumer already setup");
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

        void IEventConsumer.Setup(string serviceName, HandleRemoteEventDispatch handlerAsync)
        {
            if (eventHandlerAsync is not null)
                throw new InvalidOperationException("Event consumer already setup");
            this.eventHandlerAsync = handlerAsync;
        }

        //the producer decides who gets a copy for a direct connection: a client per replica url reaches every replica, one load balanced url reaches one of them,
        //so the server has nothing to change for the mode
        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type, EventConsumerMode eventConsumerMode)
        {
            if (throttle is not null)
                throttle.Dispose();
            throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            _ = types.Add(type);
        }

        void IQueryServer.Open()
        {
            Open();
            log?.Info($"{thisType.Name} Query Server Started On {this.serviceUrl}");
        }
        void ICommandConsumer.Open()
        {
            Open();
            log?.Info($"{thisType.Name} Command Consumer Started On {this.serviceUrl}");
        }
        void IEventConsumer.Open()
        {
            Open();
            log?.Info($"{thisType.Name} Event Consumer Started On {this.serviceUrl}");
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
                var listeners = new SocketListener[endpoints.Count];
                for (var i = 0; i < endpoints.Count; i++)
                {
                    var endpoint = endpoints[i];
                    var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        socket.NoDelay = true;
                        socket.Bind(endpoint);
                    }
                    catch
                    {
                        socket.Dispose();
                        for (var j = 0; j < i; j++)
                            listeners[j].Dispose();
                        throw;
                    }
                    listeners[i] = new SocketListener(socket, HandleConnection);
                }
                this.listeners = listeners;

                log?.Info($"{thisType.Name} resolved {serviceUrl} as {String.Join(", ", endpoints.Select(x => x.ToString()))}");

                foreach (var listener in listeners)
                    listener.Open();

                started = true;
            }
        }

        /// <summary>
        /// Handles all incoming CQRS requests.
        /// </summary>
        /// <param name="socket">The raw socket connection.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>A task to await completion of handling the request.</returns>
        protected abstract Task Handle(Socket socket, CancellationToken cancellationToken);

        private Task HandleConnection(Socket socket, CancellationToken cancellationToken)
        {
            //tracked before handling starts, a request already received runs its handler before Handle returns its task
            var tracked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = running.Add(tracked.Task);
            _ = tracked.Task.ContinueWith(removeRunning, running, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            var task = Handle(socket, cancellationToken);
            _ = task.ContinueWith(static (_, state) => ((TaskCompletionSource<bool>)state!).SetResult(true), tracked, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return task;
        }

        void IQueryServer.Close()
        {
            Close();
            log?.Info($"{thisType.Name} Query Server Closed On {this.serviceUrl}");
        }
        void ICommandConsumer.Close()
        {
            Close();
            log?.Info($"{thisType.Name} Command Consumer Closed On {this.serviceUrl}");
        }
        void IEventConsumer.Close()
        {
            Close();
            log?.Info($"{thisType.Name} Event Consumer Closed On {this.serviceUrl}");
        }

        //stops accepting connections and cancels the token the connections wait for their next request with, the requests already begun keep going
        private void Close()
        {
            lock (types)
            {
                if (listeners is not null)
                {
                    foreach (var listener in listeners)
                        listener.Dispose();
                    listeners = null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (types)
            {
                if (disposed)
                    return;
                disposed = true;
                if (listeners is not null)
                {
                    foreach (var listener in listeners)
                        listener.Dispose();
                    listeners = null;
                }
            }

            //waits for the connections and handlers still running, with the listeners closed the idle connections end and nothing new starts
            for (; ; )
            {
                var pending = running.Where(x => !x.IsCompleted).ToArray();
                if (pending.Length == 0)
                    break;
                try
                {
                    Task.WaitAll(pending);
                }
                catch (AggregateException)
                {
                    //disposing only waits, a connection or handler that failed reports that itself
                }
            }

            types.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            lock (types)
            {
                if (disposed)
                    return;
                disposed = true;
                if (listeners is not null)
                {
                    foreach (var listener in listeners)
                        listener.Dispose();
                    listeners = null;
                }
            }

            //waits for the connections and handlers still running, with the listeners closed the idle connections end and nothing new starts
            for (; ; )
            {
                var pending = running.Where(x => !x.IsCompleted).ToArray();
                if (pending.Length == 0)
                    break;
                try
                {
                    await Task.WhenAll(pending);
                }
                catch
                {
                    //disposing only waits, a connection or handler that failed reports that itself
                }
            }

            types.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}