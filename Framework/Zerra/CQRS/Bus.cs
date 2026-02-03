// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;
using Zerra.CQRS.Reflection;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS
{
    /// <summary>
    /// A CQRS (Command Query Responsibility Segregation) message bus that manages distributed command, event, and query patterns.
    /// Supports local in-process handlers as well as remote producers and consumers for commands and events.
    /// Also supports query client/server patterns for request-response scenarios.
    /// </summary>
    public sealed partial class Bus : IBusSetup, IBusInternal
    {
        private static readonly Type streamType = typeof(Stream);
        private static readonly Type cancellationTokenType = typeof(CancellationToken);

        private Dictionary<Type, object>? handlers = null;
        private Dictionary<Type, ICommandProducer>? commandProducers = null;
        private HashSet<ICommandConsumer>? commandConsumers = null;
        private Dictionary<Type, List<IEventProducer>>? eventProducers = null;
        private HashSet<IEventConsumer>? eventConsumers = null;
        private Dictionary<Type, IQueryClient>? queryClients = null;
        private HashSet<IQueryServer>? queryServers = null;
        private HashSet<Type>? handledTypes = null;

        private static readonly Lock exitLock = new();
        private static bool exited = false;
        private static SemaphoreSlim? processWaiter = null;

        private static readonly ConcurrentDictionary<string, ConcurrentReadWriteList<Type?>> typeByName = new();
        private static readonly ConcurrentFactoryDictionary<Type, HandlerMetadata> handlerMetadata = new();

        private readonly BusContext context;
        private readonly IBusLogger? busLog;
        private readonly CommandCounter commandCounter;
        private readonly TimeSpan? defaultCallTimeout;
        private readonly TimeSpan? defaultDispatchTimeout;
        private readonly TimeSpan? defaultDispatchAwaitTimeout;
        private readonly int maxConcurrentQueries;
        private readonly int maxConcurrentCommandsPerTopic;
        private readonly int maxConcurrentEventsPerTopic;

        /// <summary>
        /// Creates a new bus instance with the specified configuration.
        /// </summary>
        /// <param name="service">The logical service name for this bus.</param>
        /// <param name="log">Optional logger for bus operations.</param>
        /// <param name="busLog">Optional bus logger for detailed message logging.</param>
        /// <param name="busServices">Optional services provider for handler dependency resolution.</param>
        /// <param name="commandToReceiveUntilExit">Optional number of commands to receive before automatically exiting.</param>
        /// <param name="defaultCallTimeout">Optional default timeout for query calls.</param>
        /// <param name="defaultDispatchTimeout">Optional default timeout for command dispatch without waiting for acknowledgment.</param>
        /// <param name="defaultDispatchAwaitTimeout">Optional default timeout for command dispatch while waiting for acknowledgment.</param>
        /// <param name="maxConcurrentQueries">Optional maximum concurrent queries; defaults to ProcessorCount * 32.</param>
        /// <param name="maxConcurrentCommandsPerTopic">Optional maximum concurrent commands per topic; defaults to ProcessorCount * 8.</param>
        /// <param name="maxConcurrentEventsPerTopic">Optional maximum concurrent events per topic; defaults to ProcessorCount * 16.</param>
        /// <returns>A configured bus setup instance ready for handler and producer/consumer registration.</returns>
        public static IBusSetup New(string service, ILogger? log, IBusLogger? busLog, BusServices? busServices, int? commandToReceiveUntilExit = null,
            TimeSpan? defaultCallTimeout = null, TimeSpan? defaultDispatchTimeout = null, TimeSpan? defaultDispatchAwaitTimeout = null,
            int? maxConcurrentQueries = null, int? maxConcurrentCommandsPerTopic = null, int? maxConcurrentEventsPerTopic = null)
        {
            var bus = new Bus(service, log, busLog, busServices, commandToReceiveUntilExit,
                defaultCallTimeout, defaultDispatchTimeout, defaultDispatchAwaitTimeout,
                maxConcurrentQueries, maxConcurrentCommandsPerTopic, maxConcurrentEventsPerTopic);
            Bus.staticBus = bus;
            return bus;
        }

        private Bus(string service, ILogger? log, IBusLogger? busLog, BusServices? busServices, int? commandToReceiveUntilExit = null,
            TimeSpan? defaultCallTimeout = null, TimeSpan? defaultDispatchTimeout = null, TimeSpan? defaultDispatchAwaitTimeout = null,
            int? maxConcurrentQueries = null, int? maxConcurrentCommandsPerTopic = null, int? maxConcurrentEventsPerTopic = null)
        {
            this.context = new BusContext(this, service, log, busServices);
            this.busLog = busLog;
            this.commandCounter = new CommandCounter(commandToReceiveUntilExit, HandleProcessExit);
            this.defaultCallTimeout = defaultCallTimeout;
            this.defaultDispatchTimeout = defaultDispatchTimeout;
            this.defaultDispatchAwaitTimeout = defaultDispatchAwaitTimeout;
            this.maxConcurrentQueries = maxConcurrentQueries ?? Environment.ProcessorCount * 32;
            this.maxConcurrentCommandsPerTopic = maxConcurrentCommandsPerTopic ?? Environment.ProcessorCount * 8;
            this.maxConcurrentEventsPerTopic = maxConcurrentEventsPerTopic ?? Environment.ProcessorCount * 16;
        }

        private void HandleProcessExit() => HandleProcessExit(null, null);
        private void HandleProcessExit(object? sender, EventArgs? e)
        {
            lock (exitLock)
            {
                exited = true;
                if (processWaiter is not null)
                {
                    AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
                    _ = processWaiter.Release();
                }
            }
        }

        /// <inheritdoc />
        public void StopServices()
        {
            Task.Run(StopServicesAsync).GetAwaiter().GetResult();
        }
        /// <inheritdoc />
        public async Task StopServicesAsync()
        {
            context.Log?.Info($"{nameof(Bus)} Shutting Down");

            if (processWaiter is not null)
                processWaiter.Dispose();

            var asyncDisposed = new HashSet<IAsyncDisposable>();
            var disposed = new HashSet<IDisposable>();

            //if (commandProducers != null)
            //{
            //    foreach (var commandProducer in commandProducers.Values)
            //    {
            //        if (commandProducer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
            //        {
            //            await asyncDisposable.DisposeAsync();
            //            _ = asyncDisposed.Add(asyncDisposable);
            //        }
            //        else if (commandProducer is IDisposable disposable && !disposed.Contains(disposable))
            //        {
            //            disposable.Dispose();
            //            _ = disposed.Add(disposable);
            //        }
            //    }
            //    commandProducers.Clear();
            //}

            if (commandConsumers != null)
            {
                foreach (var commandConsumer in commandConsumers)
                {
                    commandConsumer.Close();
                    if (commandConsumer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (commandConsumer is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                commandConsumers.Clear();
            }

            //if (eventProducers != null)
            //{
            //    foreach (var eventProducer in eventProducers.Values)
            //    {
            //        if (eventProducer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
            //        {
            //            await asyncDisposable.DisposeAsync();
            //            _ = asyncDisposed.Add(asyncDisposable);
            //        }
            //        else if (eventProducer is IDisposable disposable && !disposed.Contains(disposable))
            //        {
            //            disposable.Dispose();
            //            _ = disposed.Add(disposable);
            //        }
            //    }
            //    eventProducers.Clear();
            //}

            if (eventConsumers != null)
            {
                foreach (var eventConsumer in eventConsumers)
                {
                    eventConsumer.Close();
                    if (eventConsumer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (eventConsumer is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                eventConsumers.Clear();
            }

            //if (queryClients != null)
            //{
            //    foreach (var client in queryClients.Values)
            //    {
            //        if (client is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
            //        {
            //            await asyncDisposable.DisposeAsync();
            //            _ = asyncDisposed.Add(asyncDisposable);
            //        }
            //        else if (client is IDisposable disposable && !disposed.Contains(disposable))
            //        {
            //            disposable.Dispose();
            //            _ = disposed.Add(disposable);
            //        }
            //    }
            //    queryClients.Clear();
            //}

            if (queryServers != null)
            {
                foreach (var server in queryServers)
                {
                    server.Close();
                    if (server is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (server is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                queryServers.Clear();
            }
        }

        /// <inheritdoc />
        public void WaitForExit(CancellationToken cancellationToken = default)
        {
            lock (exitLock)
            {
                if (exited)
                    return;
                processWaiter = new SemaphoreSlim(0, 1);
                AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
            }
            try
            {
                processWaiter.Wait(cancellationToken);
            }
            catch { }
            Task.Run(StopServicesAsync).GetAwaiter().GetResult();
        }
        /// <inheritdoc />
        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            lock (exitLock)
            {
                if (exited)
                    return;
                processWaiter = new SemaphoreSlim(0, 1);
                AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
            }
            try
            {
                await processWaiter.WaitAsync(cancellationToken);
            }
            catch { }
            await StopServicesAsync();
        }

        /// <inheritdoc />
        public async Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, byte[]?[] arguments, string source, bool isApi, ISerializer serializer, CancellationToken cancellationToken)
        {
            var info = BusHandlers.GetMethod(interfaceType, methodName);

            if (info.ParameterTypes.Count != (arguments is not null ? arguments.Length : 0))
                throw new ArgumentException($"{interfaceType.Name}.{methodName} invalid number of arguments");

            object?[]? args = null;
            if (arguments is not null && arguments.Length > 0)
            {
                args = new object?[arguments.Length];

                var i = 0;
                foreach (var argument in arguments)
                {
                    if (i == args.Length - 1 && info.ParameterTypes[i] == cancellationTokenType)
                    {
                        args[i++] = cancellationToken;
                    }
                    else
                    {
                        var parameter = serializer.Deserialize(argument, info.ParameterTypes[i]);
                        args[i++] = parameter;
                    }
                }
            }

            var callerProvider = BusRouters.GetBusCaller(interfaceType, this, source);
            var result = info.Method(callerProvider, args);

            if (info.IsTask)
            {
                var task = (Task)result!;
                await task;
                if (info.TaskResult != null)
                    result = info.TaskResult(task);
                else
                    result = null;
            }

            if (result is Stream stream)
                return new RemoteQueryCallResponse(stream);
            else
                return new RemoteQueryCallResponse(result);
        }
        /// <inheritdoc />
        public Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            => _DispatchCommandInternalAsync(command, command.GetType(), false, source, cancellationToken);
        /// <inheritdoc />
        public Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            => _DispatchCommandInternalAsync(command, command.GetType(), true, source, cancellationToken);
        /// <inheritdoc />
        public async Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            var dispatcher = BusRouters.GetBusDispatcher(commandType);
            var task = dispatcher.Method(this, command, commandType, source, cancellationToken);
            await task;
            var result = dispatcher.TaskResult!(task);
            return result;
        }
        /// <inheritdoc />
        public Task RemoteHandleEventDispatchAsync(IEvent @event, string source, bool isApi)
                => _DispatchEventInternalAsync(@event, @event.GetType(), source, CancellationToken.None);

        /// <inheritdoc />
        TInterface IBus.Call<TInterface>()
            => (TInterface)BusRouters.GetBusCaller(typeof(TInterface), this, context.ServiceName);

        /// <inheritdoc />
        Task IBus.DispatchAsync(ICommand command, CancellationToken? cancellationToken)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), false, context.ServiceName, cancellationToken.Value);
            if (!defaultDispatchTimeout.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), false, context.ServiceName, CancellationToken.None);

            var cancellationTokenSource = new CancellationTokenSource(defaultDispatchTimeout.Value);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), false, context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAsync)} for {command.GetType()} has timed out");
            });
            return task;
        }
        /// <inheritdoc />
        Task IBus.DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), true, context.ServiceName, cancellationToken.Value);
            if (!defaultDispatchAwaitTimeout.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), true, context.ServiceName, CancellationToken.None);

            var cancellationTokenSource = new CancellationTokenSource(defaultDispatchAwaitTimeout.Value);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), true, context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAwaitAsync)} for {command.GetType()} has timed out");
            });
            return task;
        }
        /// <inheritdoc />
        Task IBus.DispatchAsync(IEvent @event, CancellationToken? cancellationToken)
        {
            if (cancellationToken.HasValue)
                return _DispatchEventInternalAsync(@event, @event.GetType(), context.ServiceName, cancellationToken.Value);
            if (!defaultDispatchTimeout.HasValue)
                return _DispatchEventInternalAsync(@event, @event.GetType(), context.ServiceName, CancellationToken.None);

            var cancellationTokenSource = new CancellationTokenSource(defaultDispatchTimeout.Value);
            var task = _DispatchEventInternalAsync(@event, @event.GetType(), context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAsync)} for {@event.GetType()} has timed out");
            });
            return task;
        }
        /// <inheritdoc />
        Task<TResult> IBus.DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandWithResultInternalAsync(command, command.GetType(), context.ServiceName, cancellationToken.Value);
            if (!defaultDispatchAwaitTimeout.HasValue)
                return _DispatchCommandWithResultInternalAsync(command, command.GetType(), context.ServiceName, CancellationToken.None);

            var cancellationTokenSource = new CancellationTokenSource(defaultDispatchAwaitTimeout.Value);
            var task = _DispatchCommandWithResultInternalAsync(command, command.GetType(), context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAwaitAsync)} for {command.GetType()} has timed out");
                return x.Result;
            });
            return task;
        }

        /// <summary>
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.</param>
        /// <returns>A task to complete sending the command.</returns>
        Task IBus.DispatchAsync(ICommand command, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return _DispatchCommandInternalAsync(command, command.GetType(), false, context.ServiceName, default);
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), false, context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAsync)} for {command.GetType()} has timed out");
            });
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.</param>
        /// <returns>A task to await processing of the command.</returns>
        Task IBus.DispatchAwaitAsync(ICommand command, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return _DispatchCommandInternalAsync(command, command.GetType(), true, context.ServiceName, default);
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), true, context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAwaitAsync)} for {command.GetType()} has timed out");
            });
            return task;
        }
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.</param>
        /// <returns>A task to complete sending the event.</returns>
        Task IBus.DispatchAsync(IEvent @event, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return _DispatchEventInternalAsync(@event, @event.GetType(), context.ServiceName, default);
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchEventInternalAsync(@event, @event.GetType(), context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAsync)} for {@event.GetType()} has timed out");
            });
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout.</param>
        /// <returns>A task to await the result of the command.</returns>
        Task<TResult> IBus.DispatchAwaitAsync<TResult>(ICommand<TResult> command, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return _DispatchCommandWithResultInternalAsync(command, command.GetType(), context.ServiceName, default);
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandWithResultInternalAsync(command, command.GetType(), context.ServiceName, cancellationTokenSource.Token);
            task = task.ContinueWith(x =>
            {
                cancellationTokenSource.Dispose();
                if ((x.IsCanceled || x.IsFaulted) && cancellationTokenSource.IsCancellationRequested && (x.Exception == null || x.Exception.InnerExceptions.Any(e => e is OperationCanceledException)))
                    throw new TimeoutException($"{nameof(DispatchAwaitAsync)} for {command.GetType()} has timed out");
                return x.Result;
            });
            return task;
        }


        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, string source)
        {
            var metadata = BusMetadata.GetByType(interfaceType);

            CancellationTokenSource? cancellationTokenSource = null;
            CancellationToken cancellationToken;
            if (arguments.Length > 0 && arguments[^1] is CancellationToken argumentCancellationToken)
            {
                if (argumentCancellationToken != CancellationToken.None)
                {
                    cancellationToken = argumentCancellationToken;
                }
                else if (defaultCallTimeout.HasValue)
                {
                    cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                    cancellationToken = cancellationTokenSource.Token;
                }
                else
                {
                    cancellationToken = CancellationToken.None;
                }
            }
            else if (defaultCallTimeout.HasValue)
            {
                cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                cancellationToken = CancellationToken.None;
            }

            TReturn result;

            if (handlers != null && handlers.TryGetValue(interfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                    result = HandleMethodLogged<TReturn>(handler, null, interfaceType, methodName, arguments, source, cancellationToken);
                else
                    result = (TReturn)BusHandlers.Invoke(interfaceType, handler, methodName, arguments)!;
            }
            else if (queryClients != null && queryClients.TryGetValue(interfaceType, out var queryClient))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                {
                    result = HandleMethodLogged<TReturn>(null, queryClient, interfaceType, methodName, arguments, source, cancellationToken);
                }
                else
                {
                    var into = BusHandlers.GetMethod(interfaceType, methodName);
                    result = queryClient.Call<TReturn>(interfaceType, methodName, into.ParameterTypes, arguments, source, cancellationToken)!;
                }
            }
            else
            {
                throw new InvalidOperationException($"No handler or client registered for {interfaceType.FullName}");
            }

            if (cancellationTokenSource is not null)
                cancellationTokenSource.Dispose();

            return result;
        }
        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task _CallMethodTask(Type interfaceType, string methodName, object[] arguments, string source)
        {
            var metadata = BusMetadata.GetByType(interfaceType);

            CancellationTokenSource? cancellationTokenSource = null;
            CancellationToken cancellationToken;
            if (arguments.Length > 0 && arguments[^1] is CancellationToken argumentCancellationToken)
            {
                if (argumentCancellationToken != CancellationToken.None)
                {
                    cancellationToken = argumentCancellationToken;
                }
                else if (defaultCallTimeout.HasValue)
                {
                    cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                    cancellationToken = cancellationTokenSource.Token;
                }
                else
                {
                    cancellationToken = CancellationToken.None;
                }
            }
            else if (defaultCallTimeout.HasValue)
            {
                cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                cancellationToken = CancellationToken.None;
            }

            Task result;

            if (handlers != null && handlers.TryGetValue(interfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                    result = HandleMethodTaskLogged(handler, null, interfaceType, methodName, arguments, source, cancellationToken);
                else
                    result = (Task)BusHandlers.Invoke(interfaceType, handler, methodName, arguments)!;
            }
            else if (queryClients != null && queryClients.TryGetValue(interfaceType, out var queryClient))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                {
                    result = HandleMethodTaskLogged(null, queryClient, interfaceType, methodName, arguments, source, cancellationToken);
                }
                else
                {
                    var into = BusHandlers.GetMethod(interfaceType, methodName);
                    result = queryClient.CallTask(interfaceType, methodName, into.ParameterTypes, arguments, source, cancellationToken)!;
                }
            }
            else
            {
                throw new InvalidOperationException($"No handler or client registered for {interfaceType.FullName}");
            }

            if (cancellationTokenSource is not null)
                _ = result.ContinueWith((_) => cancellationTokenSource.Dispose());

            return result;
        }
        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task<TReturn> _CallMethodTaskGeneric<TReturn>(Type interfaceType, string methodName, object[] arguments, string source)
        {
            var metadata = BusMetadata.GetByType(interfaceType);

            CancellationTokenSource? cancellationTokenSource = null;
            CancellationToken cancellationToken;
            if (arguments.Length > 0 && arguments[^1] is CancellationToken argumentCancellationToken)
            {
                if (argumentCancellationToken != CancellationToken.None)
                {
                    cancellationToken = argumentCancellationToken;
                }
                else if (defaultCallTimeout.HasValue)
                {
                    cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                    cancellationToken = cancellationTokenSource.Token;
                }
                else
                {
                    cancellationToken = CancellationToken.None;
                }
            }
            else if (defaultCallTimeout.HasValue)
            {
                cancellationTokenSource = new CancellationTokenSource(defaultCallTimeout.Value);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                cancellationToken = CancellationToken.None;
            }

            Task<TReturn> result;

            if (handlers != null && handlers.TryGetValue(interfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                    result = HandleMethodTaskGenericLogged<TReturn>(handler, null, interfaceType, methodName, arguments, source, cancellationToken);
                else
                    result = (Task<TReturn>)BusHandlers.Invoke(interfaceType, handler, methodName, arguments)!;
            }
            else if (queryClients != null && queryClients.TryGetValue(interfaceType, out var queryClient))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                {
                    result = HandleMethodTaskGenericLogged<TReturn>(null, queryClient, interfaceType, methodName, arguments, source, cancellationToken);
                }
                else
                {
                    var into = BusHandlers.GetMethod(interfaceType, methodName);
                    result = queryClient.CallTaskGeneric<TReturn>(interfaceType, methodName, into.ParameterTypes, arguments, source, cancellationToken)!;
                }
            }
            else
            {
                throw new InvalidOperationException($"No handler or client registered for {interfaceType.FullName}");
            }

            if (cancellationTokenSource is not null)
                _ = result.ContinueWith((_) => cancellationTokenSource.Dispose());

            return result;
        }

        private TReturn HandleMethodLogged<TReturn>(object? handler, IQueryClient? queryClient, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginCall(interfaceType, methodName, arguments, context.ServiceName, source, handled);

            TReturn result;
            var timer = Stopwatch.StartNew();
            try
            {
                if (handled)
                {
                    result = (TReturn)BusHandlers.Invoke(interfaceType, handler!, methodName, arguments)!;
                }
                else
                {
                    var info = BusHandlers.GetMethod(interfaceType, methodName);
                    result = queryClient!.Call<TReturn>(interfaceType, methodName, info.ParameterTypes, arguments, source, cancellationToken)!;
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndCall(interfaceType, methodName, arguments, null, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndCall(interfaceType, methodName, arguments, result, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);

            return result;
        }
        private async Task HandleMethodTaskLogged(object? handler, IQueryClient? queryClient, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginCall(interfaceType, methodName, arguments, context.ServiceName, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                Task task;
                if (handled)
                {
                    task = (Task)BusHandlers.Invoke(interfaceType, handler!, methodName, arguments)!;
                }
                else
                {
                    var info = BusHandlers.GetMethod(interfaceType, methodName);
                    task = queryClient!.CallTask(interfaceType, methodName, info.ParameterTypes, arguments, source, cancellationToken)!;
                }

                await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndCall(interfaceType, methodName, arguments, null, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndCall(interfaceType, methodName, arguments, null, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);
        }
        private async Task<TReturn> HandleMethodTaskGenericLogged<TReturn>(object? handler, IQueryClient? queryClient, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginCall(interfaceType, methodName, arguments, context.ServiceName, source, handled);

            TReturn taskresult;
            var timer = Stopwatch.StartNew();
            try
            {
                Task<TReturn> task;
                if (handled)
                {
                    task = (Task<TReturn>)BusHandlers.Invoke(interfaceType, handler!, methodName, arguments)!;
                }
                else
                {
                    var info = BusHandlers.GetMethod(interfaceType, methodName);
                    task = queryClient!.CallTaskGeneric<TReturn>(interfaceType, methodName, info.ParameterTypes, arguments, source, cancellationToken)!;
                }

                taskresult = await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndCall(interfaceType, methodName, arguments, null, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndCall(interfaceType, methodName, arguments, taskresult, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);

            return taskresult;
        }

        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, string source, CancellationToken cancellationToken)
        {
            var info = BusCommandOrEventInfo.GetByType(commandType, handledTypes);
            var metadata = BusMetadata.GetByType(commandType);

            Task result;

            if (handlers != null && handlers.TryGetValue(info.InterfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                {
                    result = HandleCommandTaskLogged(handler, null, info.InterfaceType, command, commandType, requireAffirmation, source, cancellationToken);
                }
                else
                {
                    var methodName = $"{nameof(ICommandHandler<>.Handle)}-{commandType.Name}";
                    var invokeResult = BusHandlers.Invoke(info.InterfaceType, handler!, methodName, [command, cancellationToken])!;
                    if (requireAffirmation)
                        result = (Task)invokeResult;
                    else
                        result = Task.CompletedTask;
                }
            }
            else if (commandProducers != null)
            {
                ICommandProducer? producer = null;
                var messageBaseType = commandType;
                while (messageBaseType is not null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
                if (producer == null)
                    throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");

                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                {
                    result = HandleCommandTaskLogged(null, producer, info.InterfaceType, command, commandType, requireAffirmation, source, cancellationToken);
                }
                else
                {
                    if (requireAffirmation)
                        result = producer.DispatchAwaitAsync(command, source, cancellationToken);
                    else
                        result = producer.DispatchAsync(command, source, cancellationToken);
                }
            }
            else
            {
                throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");
            }

            return result;
        }
        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task<TResult> _DispatchCommandWithResultInternalAsync<TResult>(ICommand<TResult> command, Type commandType, string source, CancellationToken cancellationToken)
        {
            var info = BusCommandOrEventInfo.GetByType(commandType, handledTypes);
            var metadata = BusMetadata.GetByType(commandType);

            Task<TResult> result;

            if (handlers != null && handlers.TryGetValue(info.InterfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                {
                    result = HandleCommandWithResultTaskLogged(handler, null, info.InterfaceType, command, commandType, source, cancellationToken);
                }
                else
                {
                    var methodName = $"{nameof(ICommandHandler<,>.Handle)}-{commandType.Name}";
                    result = (Task<TResult>)BusHandlers.Invoke(info.InterfaceType, handler!, methodName, [command, cancellationToken])!;
                }
            }
            else if (commandProducers != null)
            {
                ICommandProducer? producer = null;
                var messageBaseType = commandType;
                while (messageBaseType is not null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
                if (producer == null)
                    throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");

                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                    result = HandleCommandWithResultTaskLogged(null, producer, info.InterfaceType, command, commandType, source, cancellationToken);
                else
                    result = producer.DispatchAwaitAsync(command, source, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");
            }

            return result;
        }
        /// <inheritdoc />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task _DispatchEventInternalAsync(IEvent @event, Type eventType, string source, CancellationToken cancellationToken)
        {
            var info = BusCommandOrEventInfo.GetByType(eventType, handledTypes);
            var metadata = BusMetadata.GetByType(eventType);

            Task result;

            if (handlers != null && handlers.TryGetValue(info.InterfaceType, out var handler))
            {
                if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.HandlerOnly))
                {
                    result = HandleEventTaskLogged(handler, null, info.InterfaceType, @event, eventType, source, cancellationToken);
                }
                else
                {
                    var methodName = $"{nameof(IEventHandler<>.Handle)}-{eventType.Name}";
                    _ = BusHandlers.Invoke(info.InterfaceType, handler!, methodName, [@event])!;
                    return Task.CompletedTask;
                }
            }
            else if (eventProducers != null)
            {
                List<IEventProducer>? producers = null;
                var messageBaseType = eventType;
                while (messageBaseType is not null)
                {
                    if (eventProducers.TryGetValue(messageBaseType, out producers))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
                if (producers == null)
                    throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");

                var tasks = new Task[producers.Count];
                var i = 0;
                foreach (var producer in producers)
                {
                    if (busLog != null && (metadata.BusLogging == BusLogging.SenderAndHandler || metadata.BusLogging == BusLogging.SenderOnly))
                        tasks[i++] = HandleEventTaskLogged(null, producer, info.InterfaceType, @event, eventType, source, cancellationToken);
                    else
                        tasks[i++] = producer.DispatchAsync(@event, source, cancellationToken);
                }
                result = Task.WhenAll(tasks);
            }
            else
            {
                throw new InvalidOperationException($"No handler registered for {info.InterfaceType.FullName}");
            }

            return result;
        }

        private async Task HandleCommandTaskLogged(object? handler, ICommandProducer? producer, Type interfaceType, ICommand command, Type commandType, bool requireAffirmation, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginCommand(commandType, command, context.ServiceName, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                if (handled)
                {
                    var methodName = $"{nameof(ICommandHandler<>.Handle)}-{commandType.Name}";
                    await (Task)BusHandlers.Invoke(interfaceType, handler!, methodName, [command, cancellationToken])!;
                }
                else
                {
                    if (requireAffirmation)
                        await producer!.DispatchAwaitAsync(command, source, cancellationToken);
                    else
                        await producer!.DispatchAsync(command, source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndCommand(commandType, command, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndCommand(commandType, command, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);
        }
        private async Task<TResult> HandleCommandWithResultTaskLogged<TResult>(object? handler, ICommandProducer? producer, Type interfaceType, ICommand<TResult> command, Type commandType, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginCommand(commandType, command, context.ServiceName, source, handled);

            var timer = Stopwatch.StartNew();
            TResult result;
            try
            {
                if (handled)
                {
                    var methodName = $"{nameof(ICommandHandler<>.Handle)}-{commandType.Name}";
                    result = await (Task<TResult>)BusHandlers.Invoke(interfaceType, handler!, methodName, [command, cancellationToken])!;
                }
                else
                {
                    result = await producer!.DispatchAwaitAsync(command, source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndCommand(commandType, command, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndCommand(commandType, command, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);

            return result;
        }
        private async Task HandleEventTaskLogged(object? handler, IEventProducer? producer, Type interfaceType, IEvent @event, Type eventType, string source, CancellationToken cancellationToken)
        {
            var handled = handler != null;

            busLog?.BeginEvent(eventType, @event, context.ServiceName, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                if (handled)
                {
                    var methodName = $"{nameof(ICommandHandler<>.Handle)}-{eventType.Name}";
                    await (Task)BusHandlers.Invoke(interfaceType, handler!, methodName, [@event])!;
                }
                else
                {
                    await producer!.DispatchAsync(@event, source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLog?.EndEvent(eventType, @event, context.ServiceName, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLog?.EndEvent(eventType, @event, context.ServiceName, source, handled, timer.ElapsedMilliseconds, null);
        }

        /// <inheritdoc />
        void IBusSetup.AddHandler<TInterface>(TInterface handler)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(handler);
            if (handler is not IHandler iHandler)
                throw new Exception($"{handler.GetType().Name} does not implement IHandler");

            iHandler.Initialize(context);

            handlers ??= new();
            handlers.Add(typeof(TInterface), handler!);
            handledTypes ??= new();
            _ = handledTypes.Add(interfaceType);
        }

        /// <inheritdoc />
        void IBusSetup.AddCommandProducer<TInterface>(ICommandProducer commandProducer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(commandProducer);

            var info = BusCommandOrEventInfo.GetByType(interfaceType, handledTypes);
            if (info.CommandTypes.Count == 0)
            {
                context.Log?.Error($"Cannot add Command Producer: no command types found in interface {interfaceType.Name}");
                return;
            }
            var topic = info.InterfaceName;
            foreach (var commandType in info.CommandTypes)
            {
                if (handledTypes != null && handledTypes.Contains(commandType))
                {
                    context.Log?.Error($"Cannot add Command Producer: type already added as Consumer {commandType.Name}");
                    continue;
                }
                if (commandProducers != null && commandProducers.ContainsKey(commandType))
                {
                    context.Log?.Error($"Cannot add Command Producer: type already added as Producer {commandType.Name}");
                    continue;
                }
                commandProducer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                commandProducers ??= new();
                commandProducers.Add(commandType, commandProducer);
                handledTypes ??= new();
                _ = handledTypes.Add(commandType);
                context.Log?.Info($"{commandProducer.GetType().Name} - {commandType.Name}");
            }
        }

        /// <inheritdoc />
        void IBusSetup.AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(commandConsumer);

            commandConsumer.Setup(commandCounter, RemoteHandleCommandDispatchAsync, RemoteHandleCommandDispatchAwaitAsync, RemoteHandleCommandWithResultDispatchAwaitAsync);
            commandConsumers ??= new();
            _ = commandConsumers.Add(commandConsumer);

            var info = BusCommandOrEventInfo.GetByType(interfaceType, handledTypes);
            if (info.CommandTypes.Count == 0)
            {
                context.Log?.Error($"Cannot add Command Consumer: no command types found in interface {interfaceType.Name}");
                return;
            }
            var topic = info.InterfaceName;
            foreach (var commandType in info.CommandTypes)
            {
                if (commandProducers != null && commandProducers.ContainsKey(commandType))
                {
                    context.Log?.Error($"Cannot add Command Consumer: type already added as Producer {commandType.Name}");
                    continue;
                }
                commandConsumer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                handledTypes ??= new();
                _ = handledTypes.Add(commandType);
                context.Log?.Info($"{commandConsumer.GetType().Name} at {commandConsumer.MessageHost} - {commandType.Name}");
            }

            commandConsumer.Open();
        }

        /// <inheritdoc />
        void IBusSetup.AddEventProducer<TInterface>(IEventProducer eventProducer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(eventProducer);

            var info = BusCommandOrEventInfo.GetByType(interfaceType, handledTypes);
            if (info.EventTypes.Count == 0)
            {
                context.Log?.Error($"Cannot add Event Producer: no event types found in interface {interfaceType.Name}");
                return;
            }
            var topic = info.InterfaceName;
            foreach (var eventType in info.EventTypes)
            {
                if (eventProducers != null && eventProducers.ContainsKey(eventType))
                {
                    context.Log?.Error($"Cannot add Event Producer: type already added as Producer {eventType.Name}");
                    continue;
                }
                eventProducer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                eventProducers ??= new();
                if (!eventProducers.TryGetValue(eventType, out var eventProducerList))
                {
                    eventProducerList = new();
                    eventProducers.Add(eventType, eventProducerList);
                }
                eventProducerList.Add(eventProducer);
                handledTypes ??= new();
                _ = handledTypes.Add(eventType);
                context.Log?.Info($"{eventProducers.GetType().Name} at {eventProducer.MessageHost} - {eventType.Name}");
            }
        }

        /// <inheritdoc />
        void IBusSetup.AddEventConsumer<TInterface>(IEventConsumer eventConsumer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(eventConsumer);

            eventConsumer.Setup(RemoteHandleEventDispatchAsync);
            eventConsumers ??= new();
            _ = eventConsumers.Add(eventConsumer);

            var info = BusCommandOrEventInfo.GetByType(interfaceType, handledTypes);
            if (info.EventTypes.Count == 0)
            {
                context.Log?.Error($"Cannot add Event Consumer: no event types found in interface {interfaceType.Name}");
                return;
            }
            var topic = info.InterfaceName;
            foreach (var eventType in info.EventTypes)
            {
                eventConsumer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                handledTypes ??= new();
                _ = handledTypes.Add(eventType);
                context.Log?.Info($"{eventConsumer.GetType().Name} at {eventConsumer.MessageHost} - {eventType.Name}");
            }

            eventConsumer.Open();
        }

        /// <inheritdoc />
        void IBusSetup.AddQueryClient<TInterface>(IQueryClient queryClient)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(queryClient);

            if (handledTypes != null && handledTypes.Contains(interfaceType))
            {
                context.Log?.Error($"Cannot add Query Client: type already added as Server {interfaceType.Name}");
                return;
            }
            if (queryClients != null && queryClients.ContainsKey(interfaceType))
            {
                context.Log?.Error($"Cannot add Query Client: type already added as Client {interfaceType.Name}");
                return;
            }
            queryClient.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
            queryClients ??= new();
            queryClients.Add(interfaceType, queryClient);
            handledTypes ??= new();
            _ = handledTypes.Add(interfaceType);
            context.Log?.Info($"{queryClient.GetType().Name} at {queryClient.ServiceUrl} - {interfaceType.Name}");
        }


        /// <inheritdoc />
        void IBusSetup.AddQueryServer<TInterface>(IQueryServer queryServer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.Name} is not an interface");
            ArgumentNullException.ThrowIfNull(queryServer);

            queryServer.Setup(commandCounter, RemoteHandleQueryCallAsync);
            queryServers ??= new();
            _ = queryServers.Add(queryServer);

            if (queryClients != null && queryClients.ContainsKey(interfaceType))
            {
                context.Log?.Error($"Cannot add Query Client: type already added as Client {interfaceType.Name}");
                return;
            }
            queryServer.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
            handledTypes ??= new();
            _ = handledTypes.Add(interfaceType);
            context.Log?.Info($"{queryServer.GetType().Name} at {queryServer.ServiceUrl} - {interfaceType.Name}");

            queryServer.Open();
        }

        /// <inheritdoc />
        public ILogger? Log => context.Log;

        /// <inheritdoc />
        public string ServiceName => context.ServiceName;

        /// <inheritdoc />
        public TInterface GetService<TInterface>() where TInterface : notnull
            => context.GetService<TInterface>();

        /// <inheritdoc />
        public bool TryGetService<TInterface>([MaybeNullWhen(false)] out TInterface instance) where TInterface : notnull
            => context.TryGetService<TInterface>(out instance);
    }
}
