// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Zerra.Logging;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// The base class for a CQRS client.
    /// </summary>
    public abstract class CqrsClientBase : IQueryClient, ICommandProducer, IEventProducer, IDisposable
    {
        private readonly string serviceUrl;
        /// <summary>
        /// The logging provider.
        /// </summary>
        protected readonly ILogger? log;
        /// <summary>
        /// The URI of the target server.
        /// </summary>
        protected readonly Uri serviceUri;
        /// <summary>
        /// The server host.
        /// </summary>
        protected readonly string host;
        /// <summary>
        /// The server port.
        /// </summary>
        protected readonly int port;
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> throttleByInterfaceType;
        private readonly ConcurrentDictionary<Type, string> topicsByMessageType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;

        /// <summary>
        /// Initializes a new instance of the <see cref="CqrsClientBase"/> class.
        /// Required by the inheriting class to call this constructor for information the connection needs.
        /// </summary>
        /// <param name="serviceUrl">The URL of the server to connect to.</param>
        /// <param name="log">The optional logging provider.</param>
        public CqrsClientBase(string serviceUrl, ILogger? log)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentNullException(nameof(serviceUrl));

            this.serviceUrl = serviceUrl;
            this.log = log;

            if (!serviceUrl.Contains("://"))
                this.serviceUri = new Uri($"http://{serviceUrl}"); //hacky way to make it parse without scheme, http so HttpClient based clients accept it
            else
                this.serviceUri = new Uri(serviceUrl, UriKind.RelativeOrAbsolute);
            host = this.serviceUri.Host;
            port = this.serviceUri.Port >= 0 ? this.serviceUri.Port : (String.Equals(this.serviceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            this.throttleByInterfaceType = new();
            this.topicsByMessageType = new();
            this.throttleByTopic = new();
        }

        private static readonly Type streamType = typeof(Stream);

        string IQueryClient.ServiceUrl => serviceUrl;
        string ICommandProducer.MessageHost => serviceUrl;
        string IEventProducer.MessageHost => serviceUrl;

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByMessageType.ContainsKey(type))
                return;
            _ = topicsByMessageType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByMessageType.ContainsKey(type))
                return;
            _ = topicsByMessageType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }

        void IQueryClient.RegisterInterfaceType(int maxConcurrent, Type type)
        {
            if (throttleByInterfaceType.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByInterfaceType.TryAdd(type, throttle))
                throttle.Dispose();
        }

        TReturn IQueryClient.Call<TReturn>(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source)
        {
            if (!throttleByInterfaceType.TryGetValue(interfaceType, out var throttle))
                throw new Exception($"{interfaceType.Name} is not registered with {this.GetType().Name}");

            try
            {
                var isStream = typeof(TReturn) == streamType;
                var result = CallInternal<TReturn>(throttle, isStream, interfaceType, methodName, argumentTypes, arguments, source);
                return result;
            }
            catch (Exception ex)
            {
                log?.Error($"Call Failed", ex);
                throw;
            }
        }
        Task IQueryClient.CallTask(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken)
        {
            if (!throttleByInterfaceType.TryGetValue(interfaceType, out var throttle))
                throw new Exception($"{interfaceType.Name} is not registered with {this.GetType().Name}");

            try
            {
                return LogFailure(CallInternalAsync<object>(throttle, false, interfaceType, methodName, argumentTypes, arguments, source, cancellationToken), "Call Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Call Failed", ex);
                throw;
            }
        }
        Task<TReturn> IQueryClient.CallTaskGeneric<TReturn>(Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken)
        {
            if (!throttleByInterfaceType.TryGetValue(interfaceType, out var throttle))
                throw new Exception($"{interfaceType.Name} is not registered with {this.GetType().Name}");

            try
            {
                var isStream = typeof(TReturn) == streamType;
                return LogFailure(CallInternalAsync<TReturn>(throttle, isStream, interfaceType, methodName, argumentTypes, arguments, source, cancellationToken), "Call Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Call Failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Sends CQRS queries and returns the result from the server synchronously.
        /// </summary>
        /// <typeparam name="TReturn">The type returned from the server.</typeparam>
        /// <param name="throttle">Used to limit simultaneous requests.</param>
        /// <param name="isStream">Indicates the result is a stream.</param>
        /// <param name="interfaceType">The interface type of the query.</param>
        /// <param name="methodName">The query method to call in the interface type.</param>
        /// <param name="argumentTypes">The types of the arguments for the query method.</param>
        /// <param name="arguments">The raw arguments for the query method.</param>
        /// <param name="source">A description of where the request came from.</param>
        /// <returns>The result from the server.</returns>
        protected abstract TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source);

        /// <summary>
        /// Sends CQRS queries and returns the result from the server asynchronously.
        /// </summary>
        /// <typeparam name="TReturn">The type returned from the server.</typeparam>
        /// <param name="throttle">Used to limit simultaneous requests.</param>
        /// <param name="isStream">Indicates the result is a stream.</param>
        /// <param name="interfaceType">The interface type of the query.</param>
        /// <param name="methodName">The query method to call in the interface type.</param>
        /// <param name="argumentTypes">The types of the arguments for the query method.</param>
        /// <param name="arguments">The raw arguments for the query method.</param>
        /// <param name="source">A description of where the request came from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await the result from the server.</returns>
        protected abstract Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken);

        Task ICommandProducer.DispatchAsync(ICommand command, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");

            try
            {
                return LogFailure(DispatchInternal(throttle, commandType, command, false, source, cancellationToken), "Dispatch Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Dispatch Failed", ex);
                throw;
            }
        }
        Task ICommandProducer.DispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");

            try
            {
                return LogFailure(DispatchInternal(throttle, commandType, command, true, source, cancellationToken), "Dispatch Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Dispatch Failed", ex);
                throw;
            }
        }
        Task<TResult> ICommandProducer.DispatchAwaitAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default
        {
            var commandType = command.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");

            var isStream = typeof(TResult) == streamType;

            try
            {
                return LogFailure(DispatchInternal<TResult>(throttle, isStream, commandType, command, source, cancellationToken), "Dispatch Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Dispatch Failed", ex);
                throw;
            }
        }

        Task IEventProducer.DispatchAsync(IEvent @event, string source, CancellationToken cancellationToken)
        {
            var commandType = @event.GetType();
            if (!topicsByMessageType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {this.GetType().Name}");

            try
            {
                return LogFailure(DispatchInternal(throttle, commandType, @event, source, cancellationToken), "Dispatch Failed");
            }
            catch (Exception ex)
            {
                log?.Error($"Dispatch Failed", ex);
                throw;
            }
        }

        //async failures happen in the returned task instead of when it's created, the extra await is skipped without a logger
        private Task LogFailure(Task task, string message) => log is null ? task : LogFailureAsync(task, message);
        private Task<T> LogFailure<T>(Task<T> task, string message) => log is null ? task : LogFailureAsync(task, message);
        private async Task LogFailureAsync(Task task, string message)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                log?.Error(message, ex);
                throw;
            }
        }
        private async Task<T> LogFailureAsync<T>(Task<T> task, string message)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                log?.Error(message, ex);
                throw;
            }
        }

        /// <summary>
        /// Sends a CQRS command.
        /// </summary>
        /// <param name="throttle">Used to limit simultaneous requests.</param>
        /// <param name="commandType">The command type.</param>
        /// <param name="command">The command object itself.</param>
        /// <param name="messageAwait">If the request will wait for a response from the server when the command is completed.</param>
        /// <param name="source">A description of where the request came from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await sending or await completion of the command.</returns>
        protected abstract Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a CQRS command and gets a result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned from the server.</typeparam>
        /// <param name="throttle">Used to limit simultaneous requests.</param>
        /// <param name="isStream">Indicates the result is a stream.</param>
        /// <param name="commandType">The command type.</param>
        /// <param name="command">The command object itself.</param>
        /// <param name="source">A description of where the request came from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await the result of the command from the server.</returns>
        protected abstract Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a CQRS event.
        /// </summary>
        /// <param name="throttle">Used to limit simultaneous requests.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="event">The event object itself.</param>
        /// <param name="source">A description of where the request came from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await sending the event.</returns>
        protected abstract Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void Dispose()
        {
            //the throttles are not disposed: calls still running after Dispose release them, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
            //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
            //the rest is managed memory with no finalizer that the GC reclaims once nothing references them
            //if AvailableWaitHandle is ever used, dispose them once every call that could release them has finished
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual ValueTask DisposeAsync()
        {
            //nothing to release asynchronously, Dispose is overridden by the clients that hold resources
            Dispose();
            return default;
        }
    }
}