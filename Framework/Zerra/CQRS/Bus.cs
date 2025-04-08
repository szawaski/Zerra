// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Serialization.Json;

#pragma warning disable IDE1006 // Naming Styles

namespace Zerra.CQRS
{
    /// <summary>
    /// Responsible for sending commands, events, and queries to the configured destination.
    /// A destination may be an implementation in the same assembly or calling a remote service.
    /// Discovery will host commands and events with the <see cref="ServiceExposedAttribute"/>.
    /// Discovery will also host queries whose interface has <see cref="ServiceExposedAttribute"/>.
    /// </summary>
    [Zerra.Reflection.GenerateTypeDetail]
    public static partial class Bus
    {
        private const SymmetricAlgorithmType encryptionAlgoritm = SymmetricAlgorithmType.AESwithShift;

        private static readonly Type iCommandWithResultType = typeof(ICommand<>);
        private static readonly Type iCommandHandlerType = typeof(ICommandHandler<>);
        private static readonly Type iCommandHandlerWithResultType = typeof(ICommandHandler<,>);
        private static readonly Type iEventHandlerType = typeof(IEventHandler<>);
        private static readonly Type iBusCacheType = typeof(IBusCache);
        private static readonly Type streamType = typeof(Stream);
        private static readonly Type cancellationTokenType = typeof(CancellationToken);

        private static readonly object exitLock = new();
        private static bool exited = false;

        private static readonly SemaphoreSlim setupLock = new(1, 1);

        private static SemaphoreSlim? processWaiter = null;
        private static int maxConcurrentQueries = Environment.ProcessorCount * 32;
        private static int maxConcurrentCommandsPerTopic = Environment.ProcessorCount * 8;
        private static int maxConcurrentEventsPerTopic = Environment.ProcessorCount * 16;
        private static CommandCounter commandCounter = new();

        private static readonly ConcurrentFactoryDictionary<Type, MessageMetadata> messageMetadata = new();
        private static readonly ConcurrentFactoryDictionary<Type, Func<ICommand, Task>?> commandCacheProviders = new();
        private static readonly ConcurrentFactoryDictionary<Type, Delegate?> commandWithResultCacheProviders = new();
        private static readonly ConcurrentFactoryDictionary<Type, Func<IEvent, Task>?> eventCacheProviders = new();
        private static readonly ConcurrentFactoryDictionary<Type, CallMetadata> callMetadata = new();
        private static readonly ConcurrentFactoryDictionary<Type, object?> cacheProviders = new();

        private static readonly Dictionary<Type, ICommandProducer> commandProducers = new();
        private static readonly HashSet<ICommandConsumer> commandConsumers = new();
        private static readonly Dictionary<Type, List<IEventProducer>> eventProducers = new();
        private static readonly HashSet<IEventConsumer> eventConsumers = new();
        private static readonly Dictionary<Type, IQueryClient> queryClients = new();
        private static readonly HashSet<IQueryServer> queryServers = new();
        private static readonly HashSet<Type> handledTypes = new();
        private static IBusLogger? busLogger = null;

        /// <summary>
        /// Gets or sets the maximum number of concurrent queries.
        /// </summary>
        public static int MaxConcurrentQueries
        {
            get => maxConcurrentQueries;
            set
            {
                setupLock.Wait();
                try
                {
                    if (HasServices)
                        throw new InvalidOperationException($"Cannot set {nameof(maxConcurrentQueries)} after services added");
                    maxConcurrentQueries = value;
                }
                finally
                {
                    setupLock.Release();
                }
            }
        }
        /// <summary>
        /// Gets or sets the maximum number of concurrent commands.
        /// </summary>
        public static int MaxConcurrentCommandsPerTopic
        {
            get => maxConcurrentCommandsPerTopic;
            set
            {
                setupLock.Wait();
                try
                {
                    if (HasServices)
                        throw new InvalidOperationException($"Cannot set {nameof(MaxConcurrentCommandsPerTopic)} after services added");
                    maxConcurrentCommandsPerTopic = value;
                }
                finally
                {
                    setupLock.Release();
                }
            }
        }
        /// <summary>
        /// Gets or sets the maximum number of concurrent events.
        /// </summary>
        public static int MaxConcurrentEventsPerTopic
        {
            get => maxConcurrentEventsPerTopic;
            set
            {
                setupLock.Wait();
                try
                {
                    if (HasServices)
                        throw new InvalidOperationException($"Cannot set {nameof(MaxConcurrentEventsPerTopic)} after services added");
                    maxConcurrentEventsPerTopic = value;
                }
                finally
                {
                    setupLock.Release();
                }
            }
        }
        /// <summary>
        /// Sets the number of commands to receive before the service terminates.
        /// This is useful for ephimeral services in features such as Kubernetes KEDA.
        /// </summary>
        public static int? ReceiveCommandsBeforeExit
        {
            get => commandCounter.ReceiveCountBeforeExit;
            set
            {
                setupLock.Wait();
                try
                {
                    if (HasServices)
                        throw new InvalidOperationException($"Cannot set {nameof(ReceiveCommandsBeforeExit)} after services added");
                    commandCounter = new CommandCounter(value, HandleProcessExit);
                }
                finally
                {
                    setupLock.Release();
                }
            }
        }
        private static bool HasServices
        {
            get => commandProducers.Count != 0 ||
                   commandConsumers.Count != 0 ||
                   eventProducers.Count != 0 ||
                   eventConsumers.Count != 0 ||
                   queryClients.Count != 0 ||
                   queryServers.Count != 0;
        }

        /// <summary>
        /// The time to wait on a dispatch before a cancellation request unless otherwise specified per dispatch. Null indicates infinite time. The default is 30 seconds.
        /// </summary>
        public static TimeSpan? DefaultDispatchTimeout { get; set; }

        /// <summary>
        /// The time to wait on a call before a cancellation request unless otherwise specified per dispatch. Null indicates infinite time. The default is 30 seconds.
        /// </summary>
        public static TimeSpan? DefaultCallTimeout { get; set; }

        internal static async Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, string?[] arguments, string source, bool isApi, CancellationToken cancellationToken)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            var callerProvider = BusRouters.GetProviderToCallMethodInternalInstance(interfaceType, networkType, false, source, null, cancellationToken);

            var methodDetail = TypeAnalyzer.GetMethodDetail(interfaceType, methodName);

            if (methodDetail.ParameterDetails.Count != (arguments is not null ? arguments.Length : 0))
                throw new ArgumentException($"{interfaceType.GetNiceName()}.{methodName} invalid number of arguments");

            object?[]? args = null;
            if (arguments is not null && arguments.Length > 0)
            {
                args = new object?[arguments.Length];

                var i = 0;
                foreach (var argument in arguments)
                {
                    if (i == args.Length - 1 && methodDetail.ParameterDetails[i].Type == cancellationTokenType)
                    {
                        args[i++] = cancellationToken;
                    }
                    else
                    {
                        var parameter = JsonSerializer.Deserialize(methodDetail.ParameterDetails[i].Type, argument);
                        args[i++] = parameter;
                    }
                }
            }

            bool isStream;
            object? model;
            if (methodDetail.ReturnTypeDetailBoxed.IsTask)
            {
                isStream = methodDetail.ReturnTypeDetailBoxed.Type.IsGenericType && (methodDetail.ReturnTypeDetailBoxed.InnerType == streamType || methodDetail.ReturnTypeDetailBoxed.InnerTypeDetail.BaseTypes.Contains(streamType));
                var task = (Task)methodDetail.CallerBoxed(callerProvider, args)!;
                await task;
                model = methodDetail.ReturnTypeDetailBoxed.TaskResultGetter(task);
            }
            else
            {
                isStream = methodDetail.ReturnTypeDetailBoxed.Type == streamType || methodDetail.ReturnTypeDetailBoxed.BaseTypes.Contains(streamType);
                model = methodDetail.CallerBoxed(callerProvider, args);
            }

            if (isStream)
                return new RemoteQueryCallResponse((Stream)model!);
            else
                return new RemoteQueryCallResponse(model);
        }
        internal static Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            => _DispatchCommandInternalAsync(command, command.GetType(), false, isApi ? NetworkType.Api : NetworkType.Internal, false, source, cancellationToken);
        internal static Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
            => _DispatchCommandInternalAsync(command, command.GetType(), true, isApi ? NetworkType.Api : NetworkType.Internal, false, source, cancellationToken);
        private static readonly MethodDetail dispatchCommandWithResultInternalAsyncMethod = typeof(Bus).GetMethodDetail(nameof(_DispatchCommandWithResultInternalAsync));
        internal static async Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            var commandType = command.GetType().GetTypeDetail();
            var commandInterfaceType = commandType.Interfaces.First(x => x.Name == iCommandWithResultType.Name).GetTypeDetail();

            var dispatchCommandWithResultInternalAsyncMethodGeneric = dispatchCommandWithResultInternalAsyncMethod.GetGenericMethodDetail([commandInterfaceType.InnerType]);

            var task = (Task)dispatchCommandWithResultInternalAsyncMethodGeneric.CallerBoxed(null, [command, command.GetType(), networkType, false, source, cancellationToken])!;
            await task;
            var model = dispatchCommandWithResultInternalAsyncMethodGeneric.ReturnTypeDetailBoxed.TaskResultGetter(task);
            return model;
        }
        internal static Task RemoteHandleEventDispatchAsync(IEvent @event, string source, bool isApi, CancellationToken cancellationToken)
            => _DispatchEventInternalAsync(@event, @event.GetType(), isApi ? NetworkType.Api : NetworkType.Internal, false, source, cancellationToken);

        /// <summary>
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to complete sending the command.</returns>
        public static Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), false, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationToken.Value);
            if (!DefaultDispatchTimeout.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), false, NetworkType.Local, false, Config.ApplicationIdentifier, default);

            var cancellationTokenSource = new CancellationTokenSource(DefaultDispatchTimeout.Value);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), false, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await processing of the command.</returns>
        public static Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), true, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationToken.Value);
            if (!DefaultDispatchTimeout.HasValue)
                return _DispatchCommandInternalAsync(command, command.GetType(), true, NetworkType.Local, false, Config.ApplicationIdentifier, default);

            var cancellationTokenSource = new CancellationTokenSource(DefaultDispatchTimeout.Value);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), true, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to complete sending the event.</returns>
        public static Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken.HasValue)
                return _DispatchEventInternalAsync(@event, @event.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationToken.Value);
            if (!DefaultDispatchTimeout.HasValue)
                return _DispatchEventInternalAsync(@event, @event.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, default);

            var cancellationTokenSource = new CancellationTokenSource(DefaultDispatchTimeout.Value);
            var task = _DispatchEventInternalAsync(@event, @event.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task to await the result of the command.</returns>
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken.HasValue)
                return _DispatchCommandWithResultInternalAsync(command, command.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationToken.Value);
            if (!DefaultDispatchTimeout.HasValue)
                return _DispatchCommandWithResultInternalAsync(command, command.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, default);

            var cancellationTokenSource = new CancellationTokenSource(DefaultDispatchTimeout.Value);
            var task = _DispatchCommandWithResultInternalAsync(command, command.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }

        /// <summary>
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>A task to complete sending the command.</returns>
        public static Task DispatchAsync(ICommand command, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), false, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>A task to await processing of the command.</returns>
        public static Task DispatchAwaitAsync(ICommand command, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandInternalAsync(command, command.GetType(), true, NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>A task to complete sending the event.</returns>
        public static Task DispatchAsync(IEvent @event, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchEventInternalAsync(@event, @event.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>A task to await the result of the command.</returns>
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            var task = _DispatchCommandWithResultInternalAsync(command, command.GetType(), NetworkType.Local, false, Config.ApplicationIdentifier, cancellationTokenSource.Token);
            _ = task.ContinueWith((_) => cancellationTokenSource.Dispose());
            return task;
        }

        /// <summary>
        /// Internal Use Only.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, NetworkType networkType, bool isFinalLayer, string source, CancellationToken cancellationToken)
        {
            var metadata = messageMetadata.GetOrAdd(commandType, (Func<Type, MessageMetadata>)(static (commandType) =>
            {
                NetworkType exposedNetworkType = NetworkType.Local;
                var busLogging = BusLogging.SenderAndHandler;
                var authenticate = false;
                string[]? roles = null;
                foreach (var attribute in commandType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute)
                    {
                        exposedNetworkType = serviceExposedAttribute.NetworkType;
                    }
                    else if (attribute is ServiceLogAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                    else if (attribute is ServiceSecureAttribute serviceSecureAttribute)
                    {
                        authenticate = true;
                        roles = serviceSecureAttribute.Roles;
                    }
                }
                return new MessageMetadata(exposedNetworkType, busLogging, authenticate, roles);
            }));

            if (metadata.ExposedNetworkType < networkType)
                throw new SecurityException($"Not Exposed Command {commandType.GetNiceName()} for {nameof(NetworkType)}.{networkType.EnumName()}");
            if (metadata.Authenticate)
                Authenticate(Thread.CurrentPrincipal, metadata.Roles, () => $"Access Denied for Command {commandType.GetNiceName()}");

            Func<ICommand, Task>? cacheProviderDispatchAsync = null;
            if (!isFinalLayer)
            {
                cacheProviderDispatchAsync = commandCacheProviders.GetOrAdd(commandType, requireAffirmation, networkType, source, cancellationToken, metadata, static (commandType, requireAffirmation, networkType, source, cancellationToken, metadata) =>
                {
                    var handlerTypeDetail = TypeAnalyzer.GetGenericTypeDetail(iCommandHandlerType, commandType);

                    var busCacheType = Discovery.GetClassByInterface(handlerTypeDetail.Type, iBusCacheType, false);
                    if (busCacheType is null)
                        return null;

                    var busCacheTypeDetail = busCacheType.GetTypeDetail();

                    if (!busCacheTypeDetail.TryGetMethodBoxed(nameof(LayerProvider<object>.SetNextProvider), out var methodSetNextProvider))
                        return null;

                    var cacheInstance = Instantiator.Create(busCacheType);

                    var methodGetProviderInterfaceType = busCacheTypeDetail.GetMethodBoxed(nameof(LayerProvider<object>.GetProviderInterfaceType));
                    var interfaceType = (Type)methodGetProviderInterfaceType.CallerBoxed(cacheInstance, null)!;

                    var messageHandlerToDispatchProvider = BusRouters.GetHandlerToDispatchInternalInstance(interfaceType, requireAffirmation, networkType, true, source, cancellationToken);
                    _ = methodSetNextProvider.CallerBoxed(cacheInstance, [messageHandlerToDispatchProvider]);

                    var method = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(ICommandHandler<ICommand>.Handle), [commandType, cancellationTokenType]);
                    Task caller(ICommand arg) => (Task)method.CallerBoxed(cacheInstance, [arg, cancellationToken])!;

                    return caller;
                });
            }

            ICommandProducer? producer = null;
            if (commandProducers.Count != 0 && (networkType == NetworkType.Local || !handledTypes.Contains(commandType)))
            {
                var messageBaseType = commandType;
                while (messageBaseType is not null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            var handled = producer is null;

            Task result;
            if (isFinalLayer || busLogger is null || metadata.BusLogging == BusLogging.None || (metadata.BusLogging == BusLogging.HandlerOnly && !handled) || (metadata.BusLogging == BusLogging.SenderOnly && handled))
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    result = cacheProviderDispatchAsync(command);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand>.Handle), [commandType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    result = (Task)method.CallerBoxed(provider, [command, cancellationToken])!;
                }
                else
                {
                    if (requireAffirmation)
                        return producer!.DispatchAwaitAsync(command, source, cancellationToken);
                    else
                        return producer!.DispatchAsync(command, source, cancellationToken);
                }
            }
            else
            {
                result = HandleCommandTaskLogged(handled, producer, cacheProviderDispatchAsync, command, commandType, requireAffirmation, networkType, source, cancellationToken);
            }

            if (!requireAffirmation && networkType == NetworkType.Local)
                result = Task.CompletedTask;

            return result;
        }
        /// <summary>
        /// Internal Use Only.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Task<TResult> _DispatchCommandWithResultInternalAsync<TResult>(ICommand<TResult> command, Type commandType, NetworkType networkType, bool isFinalLayer, string source, CancellationToken cancellationToken)
        {
            var metadata = messageMetadata.GetOrAdd(commandType, networkType, (Func<Type, NetworkType, MessageMetadata>)(static (commandType, networkType) =>
            {
                NetworkType exposedNetworkType = NetworkType.Local;
                var busLogging = BusLogging.SenderAndHandler;
                var authenticate = false;
                string[]? roles = null;
                foreach (var attribute in commandType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute)
                    {
                        exposedNetworkType = serviceExposedAttribute.NetworkType;
                    }
                    else if (attribute is ServiceLogAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                    else if (attribute is ServiceSecureAttribute serviceSecureAttribute)
                    {
                        authenticate = true;
                        roles = serviceSecureAttribute.Roles;
                    }
                }
                return new MessageMetadata(exposedNetworkType, busLogging, authenticate, roles);
            }));

            if (metadata.ExposedNetworkType < networkType)
                throw new SecurityException($"Not Exposed Command {commandType.GetNiceName()} for {nameof(NetworkType)}.{networkType.EnumName()}");
            if (metadata.Authenticate)
                Authenticate(Thread.CurrentPrincipal, metadata.Roles, () => $"Access Denied for Command {commandType.GetNiceName()}");

            Func<ICommand<TResult>, Task<TResult>>? cacheProviderDispatchAsync = null;
            if (!isFinalLayer)
            {
                cacheProviderDispatchAsync = (Func<ICommand<TResult>, Task<TResult>>?)commandWithResultCacheProviders.GetOrAdd(commandType, networkType, source, cancellationToken, metadata, static (commandType, networkType, source, cancellationToken, metadata) =>
                {
                    var handlerTypeDetail = TypeAnalyzer.GetGenericTypeDetail(iCommandHandlerWithResultType, commandType, typeof(TResult));

                    var busCacheType = Discovery.GetClassByInterface(handlerTypeDetail.Type, iBusCacheType, false);
                    if (busCacheType is null)
                        return null;

                    var busCacheTypeDetail = busCacheType.GetTypeDetail();

                    if (!busCacheTypeDetail.TryGetMethodBoxed(nameof(LayerProvider<object>.SetNextProvider), out var methodSetNextProvider))
                        return null;

                    var cacheInstance = Instantiator.Create(busCacheType);

                    var methodGetProviderInterfaceType = busCacheTypeDetail.GetMethodBoxed(nameof(LayerProvider<object>.GetProviderInterfaceType));
                    var interfaceType = (Type)methodGetProviderInterfaceType.CallerBoxed(cacheInstance, null)!;

                    var messageHandlerToDispatchProvider = BusRouters.GetHandlerToDispatchInternalInstance(interfaceType, false, networkType, true, source, cancellationToken);
                    _ = methodSetNextProvider.CallerBoxed(cacheInstance, [messageHandlerToDispatchProvider]);

                    var method = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle), [commandType, cancellationTokenType]);
                    Task<TResult?> caller(ICommand<TResult> arg) => (Task<TResult?>)method.CallerBoxed(cacheInstance, [arg, cancellationToken])!;

                    return caller;
                });
            }

            ICommandProducer? producer = null;
            if (commandProducers.Count != 0 && (networkType == NetworkType.Local || !handledTypes.Contains(commandType)))
            {
                var messageBaseType = commandType;
                while (messageBaseType is not null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            var handled = producer is null;

            Task<TResult>? result;
            if (isFinalLayer || busLogger is null || metadata.BusLogging == BusLogging.None || (metadata.BusLogging == BusLogging.HandlerOnly && !handled) || (metadata.BusLogging == BusLogging.SenderOnly && handled))
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    result = cacheProviderDispatchAsync(command);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerWithResultType, commandType, typeof(TResult));
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle), [commandType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    result = (Task<TResult>)method.CallerBoxed(provider, [command, cancellationToken])!;
                }
                else
                {
                    result = producer!.DispatchAwaitAsync(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken);
                }
            }
            else
            {
                result = HandleCommandWithResultTaskLogged(handled, producer, cacheProviderDispatchAsync, command, commandType, networkType, source, cancellationToken);
            }

            return result;
        }
        /// <summary>
        /// Internal Use Only.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Task _DispatchEventInternalAsync(IEvent @event, Type eventType, NetworkType networkType, bool isFinalLayer, string source, CancellationToken cancellationToken)
        {
            var metadata = messageMetadata.GetOrAdd(eventType, networkType, (Func<Type, NetworkType, MessageMetadata>)(static (eventType, networkType) =>
            {
                NetworkType exposedNetworkType = NetworkType.Local;
                var busLogging = BusLogging.SenderAndHandler;
                var authenticate = false;
                string[]? roles = null;
                foreach (var attribute in eventType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute)
                    {
                        exposedNetworkType = serviceExposedAttribute.NetworkType;
                    }
                    else if (attribute is ServiceLogAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                    else if (attribute is ServiceSecureAttribute serviceSecureAttribute)
                    {
                        authenticate = true;
                        roles = serviceSecureAttribute.Roles;
                    }
                }
                return new MessageMetadata(exposedNetworkType, busLogging, authenticate, roles);
            }));

            if (metadata.ExposedNetworkType < networkType)
                throw new SecurityException($"Not Exposed Event {eventType.GetNiceName()} for {nameof(NetworkType)}.{networkType.EnumName()}");
            if (metadata.Authenticate)
                Authenticate(Thread.CurrentPrincipal, metadata.Roles, () => $"Access Denied for Event {eventType.GetNiceName()}");

            Func<IEvent, Task>? cacheProviderDispatchAsync = null;
            if (!isFinalLayer)
            {
                cacheProviderDispatchAsync = eventCacheProviders.GetOrAdd(eventType, networkType, source, cancellationToken, metadata, static (eventType, networkType, source, cancellationToken, metadata) =>
                {
                    var handlerTypeDetail = TypeAnalyzer.GetGenericTypeDetail(iEventHandlerType, eventType);

                    var busCacheType = Discovery.GetClassByInterface(handlerTypeDetail.Type, iBusCacheType, false);
                    if (busCacheType is null)
                        return null;

                    var busCacheTypeDetail = busCacheType.GetTypeDetail();

                    if (!busCacheTypeDetail.TryGetMethodBoxed(nameof(LayerProvider<object>.SetNextProvider), out var methodSetNextProvider))
                        return null;

                    var cacheInstance = Instantiator.Create(busCacheType);

                    var methodGetProviderInterfaceType = busCacheTypeDetail.GetMethodBoxed(nameof(LayerProvider<object>.GetProviderInterfaceType));
                    var interfaceType = (Type)methodGetProviderInterfaceType.CallerBoxed(cacheInstance, null)!;

                    var messageHandlerToDispatchProvider = BusRouters.GetHandlerToDispatchInternalInstance(interfaceType, false, networkType, true, source, cancellationToken);
                    _ = methodSetNextProvider.CallerBoxed(cacheInstance, [messageHandlerToDispatchProvider]);

                    var method = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(IEventHandler<IEvent>.Handle), [eventType, cancellationTokenType]);
                    Task caller(IEvent arg) => (Task)method.CallerBoxed(cacheInstance, [arg, cancellationToken])!;

                    return caller;
                });
            }

            List<IEventProducer>? producerList = null;
            if (eventProducers.Count != 0 && (networkType == NetworkType.Local || !handledTypes.Contains(eventType)))
            {
                var messageBaseType = eventType;
                while (messageBaseType is not null)
                {
                    if (eventProducers.TryGetValue(messageBaseType, out producerList))
                        break;
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            var handled = producerList is null || producerList.Count == 0;

            Task result;

            if (isFinalLayer || busLogger is null || metadata.BusLogging == BusLogging.None || (metadata.BusLogging == BusLogging.HandlerOnly && !handled) || (metadata.BusLogging == BusLogging.SenderOnly && handled))
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    result = cacheProviderDispatchAsync(@event);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), [eventType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    result = (Task)method.CallerBoxed(provider, [@event, cancellationToken])!;
                }
                else
                {
                    if (producerList!.Count > 1)
                    {
                        var tasks = new Task[producerList.Count];
                        var i = 0;
                        var newSource = networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source;
                        foreach (var producer in producerList)
                        {
                            tasks[i++] = producer!.DispatchAsync(@event, newSource, cancellationToken);
                        }
                        result = Task.WhenAll(tasks);
                    }
                    else
                    {
                        var producer = producerList.First();
                        result = producer!.DispatchAsync(@event, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken);
                    }
                }
            }
            else
            {
                if (producerList?.Count > 1)
                {
                    var tasks = new Task[producerList.Count];
                    var i = 0;
                    foreach (var producer in producerList)
                    {
                        tasks[i++] = HandleEventTaskLogged(handled, producer, cacheProviderDispatchAsync, @event, eventType, networkType, source, cancellationToken);
                    }
                    result = Task.WhenAll(tasks);
                }
                else
                {
                    var producer = producerList?.FirstOrDefault();
                    result = HandleEventTaskLogged(handled, producer, cacheProviderDispatchAsync, @event, eventType, networkType, source, cancellationToken);
                }
            }

            return result;
        }

        private static async Task HandleCommandTaskLogged(bool handled, ICommandProducer? producer, Func<ICommand, Task>? cacheProviderDispatchAsync, ICommand command, Type commandType, bool requireAffirmation, NetworkType networkType, string source, CancellationToken cancellationToken)
        {
            busLogger?.BeginCommand(commandType, command, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    await cacheProviderDispatchAsync(command);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand>.Handle), [commandType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    await (Task)method.CallerBoxed(provider, [command, cancellationToken])!;
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
                busLogger?.EndCommand(commandType, command, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLogger?.EndCommand(commandType, command, source, handled, timer.ElapsedMilliseconds, null);
        }
        private static async Task<TResult> HandleCommandWithResultTaskLogged<TResult>(bool handled, ICommandProducer? producer, Func<ICommand<TResult>, Task<TResult>>? cacheProviderDispatchAsync, ICommand<TResult> command, Type commandType, NetworkType networkType, string source, CancellationToken cancellationToken)
        {
            busLogger?.BeginCommand(commandType, command, source, handled);

            var timer = Stopwatch.StartNew();
            TResult result;
            try
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    result = await cacheProviderDispatchAsync(command);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerWithResultType, commandType, typeof(TResult));
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle), [commandType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    result = await (Task<TResult>)method.CallerBoxed(provider, [command, cancellationToken])!;
                }
                else
                {
                    result = await producer!.DispatchAwaitAsync(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLogger?.EndCommand(commandType, command, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLogger?.EndCommand(commandType, command, source, handled, timer.ElapsedMilliseconds, null);

            return result;
        }
        private static async Task HandleEventTaskLogged(bool handled, IEventProducer? producer, Func<IEvent, Task>? cacheProviderDispatchAsync, IEvent @event, Type eventType, NetworkType networkType, string source, CancellationToken cancellationToken)
        {
            busLogger?.BeginEvent(eventType, @event, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                if (cacheProviderDispatchAsync is not null)
                {
                    await cacheProviderDispatchAsync(@event);
                }
                else if (handled)
                {
                    var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);
                    var providerType = ProviderResolver.GetTypeFirst(interfaceType);
                    var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), [eventType, cancellationTokenType]);
                    var provider = Instantiator.GetSingle(providerType);
                    await (Task)method.CallerBoxed(provider, [@event, cancellationToken])!;
                }
                else
                {
                    await producer!.DispatchAsync(@event, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLogger?.EndEvent(eventType, @event, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLogger?.EndEvent(eventType, @event, source, handled, timer.ElapsedMilliseconds, null);
        }

        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An instance of the interface to route queries.</returns>
        public static TInterface Call<TInterface>(CancellationToken? cancellationToken = null)
        {
            return (TInterface)BusRouters.GetProviderToCallMethodInternalInstance(typeof(TInterface), NetworkType.Local, false, Config.ApplicationIdentifier, null, cancellationToken);
        }
        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>An instance of the interface to route queries.</returns>
        public static object Call(Type interfaceType, CancellationToken? cancellationToken = null)
        {
            return BusRouters.GetProviderToCallMethodInternalInstance(interfaceType, NetworkType.Local, false, Config.ApplicationIdentifier, null, cancellationToken);
        }

        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>An instance of the interface to route queries.</returns>
        public static TInterface Call<TInterface>(TimeSpan timeout)
        {
            return (TInterface)BusRouters.GetProviderToCallMethodInternalInstance(typeof(TInterface), NetworkType.Local, false, Config.ApplicationIdentifier, timeout, null);
        }
        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="timeout">The time to wait before a cancellation request.</param>
        /// <returns>An instance of the interface to route queries.</returns>
        public static object Call(Type interfaceType, TimeSpan timeout)
        {
            return BusRouters.GetProviderToCallMethodInternalInstance(interfaceType, NetworkType.Local, false, Config.ApplicationIdentifier, timeout, null);
        }

        /// <summary>
        /// Internal Use Only.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, bool isFinalLayer, string source, TimeSpan? timeout, CancellationToken? cancellationTokenNullable)
        {
            var metadata = callMetadata.GetOrAdd(interfaceType, networkType, (Func<Type, NetworkType, CallMetadata>)(static (interfaceType, networkType) =>
            {
                NetworkType exposedNetworkType = NetworkType.Local;
                var busLogging = BusLogging.SenderAndHandler;
                var authenticate = false;
                string[]? roles = null;
                foreach (var attribute in interfaceType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute)
                    {
                        exposedNetworkType = serviceExposedAttribute.NetworkType;
                        break;
                    }
                    else if (attribute is ServiceLogAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                    else if (attribute is ServiceSecureAttribute serviceSecureAttribute)
                    {
                        authenticate = true;
                        roles = serviceSecureAttribute.Roles;
                    }
                }
                return new CallMetadata(exposedNetworkType, busLogging, authenticate, roles);
            }));

            var methodMetadata = metadata.MethodMetadata.GetOrAdd(methodName, interfaceType, networkType, metadata, (Func<string, Type, NetworkType, CallMetadata, MethodMetadata>)(static (methodName, interfaceType, networkType, metadata) =>
            {
                var methodDetail = TypeAnalyzer.GetMethodDetail(interfaceType, methodName);

                NetworkType blockedNetworkType = NetworkType.Local;
                var busLogging = metadata.BusLogging;
                var authenticate = false;
                string[]? roles = null;
                foreach (var attribute in methodDetail.Attributes)
                {
                    if (attribute is ServiceBlockedAttribute serviceBlockedAttribute)
                    {
                        blockedNetworkType = serviceBlockedAttribute.NetworkType;
                    }
                    else if (attribute is ServiceLogAttribute busLoggedAttribute && busLoggedAttribute.BusLogging > busLogging)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                    else if (attribute is ServiceSecureAttribute serviceSecureAttribute)
                    {
                        authenticate = true;
                        roles = serviceSecureAttribute.Roles;
                    }
                }
                return new MethodMetadata(methodDetail, blockedNetworkType, busLogging, authenticate, roles);
            }));

            if (methodMetadata.MethodDetail.ParameterDetails.Count != arguments.Length)
                throw new ArgumentException($"{interfaceType.GetNiceName()}.{methodName} invalid number of arguments");

            if (metadata.ExposedNetworkType < networkType)
                throw new Exception($"Not Exposed Interface {interfaceType.GetNiceName()} for {nameof(NetworkType)}.{networkType.EnumName()}");
            if (methodMetadata.BlockedNetworkType != NetworkType.Local && methodMetadata.BlockedNetworkType < networkType)
                throw new Exception($"Blocked Method {interfaceType.GetNiceName()}.{methodName} for {nameof(NetworkType)}.{networkType.EnumName()}");

            if (metadata.Authenticate)
                Authenticate(Thread.CurrentPrincipal, metadata.Roles, () => $"Access Denied for Interface {interfaceType.GetNiceName()}");
            if (methodMetadata.Authenticate)
                Authenticate(Thread.CurrentPrincipal, methodMetadata.Roles, () => $"Access Denied for Method {interfaceType.GetNiceName()}.{methodName}");

            CancellationTokenSource? cancellationTokenSource = null;
            CancellationToken cancellationToken;
            if (timeout.HasValue)
            {
                cancellationTokenSource = new CancellationTokenSource(timeout.Value);
                cancellationToken = cancellationTokenSource.Token;
                if (methodMetadata.MethodDetail.ParameterDetails.Count > 0 && methodMetadata.MethodDetail.ParameterDetails[methodMetadata.MethodDetail.ParameterDetails.Count - 1].Type == cancellationTokenType)
                    arguments[arguments.Length - 1] = cancellationToken;
            }
            else if (cancellationTokenNullable.HasValue)
            {
                cancellationToken = cancellationTokenNullable.Value;
                if (methodMetadata.MethodDetail.ParameterDetails.Count > 0 && methodMetadata.MethodDetail.ParameterDetails[methodMetadata.MethodDetail.ParameterDetails.Count - 1].Type == cancellationTokenType)
                    arguments[arguments.Length - 1] = cancellationToken;
            }
            else if (methodMetadata.MethodDetail.ParameterDetails.Count > 0 && methodMetadata.MethodDetail.ParameterDetails[methodMetadata.MethodDetail.ParameterDetails.Count - 1].Type == cancellationTokenType)
            {
                var argumentCancellationToken = (CancellationToken)arguments[arguments.Length - 1];
                if (argumentCancellationToken != CancellationToken.None)
                {
                    cancellationToken = argumentCancellationToken;
                }
                else if (DefaultCallTimeout.HasValue)
                {
                    cancellationTokenSource = new CancellationTokenSource(DefaultCallTimeout.Value);
                    cancellationToken = cancellationTokenSource.Token;
                }
                else
                {
                    cancellationToken = default;
                }
            }
            else if (DefaultCallTimeout.HasValue)
            {
                cancellationTokenSource = new CancellationTokenSource(DefaultCallTimeout.Value);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                cancellationToken = default;
            }

            object? cacheProvider = null;
            if (!isFinalLayer)
            {
                cacheProvider = cacheProviders.GetOrAdd(interfaceType, networkType, source, cancellationToken, static (interfaceType, networkType, source, cancellationToken) =>
                {
                    var busCacheType = Discovery.GetClassByInterface(interfaceType, iBusCacheType, false);
                    if (busCacheType is not null)
                    {
                        var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.SetNextProvider)).MethodInfo;
                        if (methodSetNextProvider is not null)
                        {
                            var callerProvider = BusRouters.GetProviderToCallMethodInternalInstance(interfaceType, networkType, true, source, null, cancellationToken);

                            var cacheInstance = Instantiator.Create(busCacheType);

                            _ = methodSetNextProvider.Invoke(cacheInstance, [callerProvider]);

                            return cacheInstance;
                        }
                    }

                    return null;
                });
            }

            IQueryClient? queryClient = null;
            if (queryClients.Count != 0 && (networkType == NetworkType.Local || !handledTypes.Contains(interfaceType)))
            {
                _ = queryClients.TryGetValue(interfaceType, out queryClient);
            }

            var handled = queryClient is null;

            TReturn result;
            if (isFinalLayer || busLogger is null || metadata.BusLogging == BusLogging.None || (metadata.BusLogging == BusLogging.HandlerOnly && !handled) || (metadata.BusLogging == BusLogging.SenderOnly && handled))
            {
                if (cacheProvider is not null)
                {
                    result = (TReturn)methodMetadata.MethodDetail.CallerBoxed(cacheProvider, arguments)!;
                }
                else
                {
                    if (handled)
                    {
                        var provider = ProviderResolver.GetFirst(interfaceType);
                        result = (TReturn)methodMetadata.MethodDetail.CallerBoxed(provider, arguments)!;
                    }
                    else
                    {
                        result = queryClient!.Call<TReturn>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken)!;
                    }
                }
            }
            else if (methodMetadata.MethodDetail.ReturnTypeDetailBoxed.IsTask)
            {
                if (methodMetadata.MethodDetail.ReturnTypeDetailBoxed.Type.IsGenericType)
                {
                    var method = callMethodTaskGenericLoggedMethod.GetGenericMethodDetail(methodMetadata.MethodDetail.ReturnTypeDetailBoxed.InnerType);
                    result = (TReturn)method.CallerBoxed(null, [handled, queryClient, cacheProvider, interfaceType, methodMetadata.MethodDetail, arguments, networkType, source, cancellationToken])!;
                }
                else
                {
                    result = (TReturn)(object)CallMethodTaskLogged(handled, queryClient, cacheProvider, interfaceType, methodMetadata.MethodDetail, arguments, networkType, source, cancellationToken);
                }
            }
            else
            {
                busLogger?.BeginCall(interfaceType, methodName, arguments, null, source, handled);

                var timer = Stopwatch.StartNew();
                try
                {
                    if (cacheProvider is not null)
                    {
                        result = (TReturn)methodMetadata.MethodDetail.CallerBoxed(cacheProvider, arguments)!;
                    }
                    else if (handled)
                    {
                        var provider = ProviderResolver.GetFirst(interfaceType);
                        result = (TReturn)methodMetadata.MethodDetail.CallerBoxed(provider, arguments)!;
                    }
                    else
                    {
                        result = queryClient!.Call<TReturn>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken)!;
                    }
                }
                catch (Exception ex)
                {
                    timer.Stop();
                    busLogger?.EndCall(interfaceType, methodName, arguments, null, source, handled, timer.ElapsedMilliseconds, ex);
                    throw;
                }

                timer.Stop();
                busLogger?.EndCall(interfaceType, methodName, arguments, result, source, handled, timer.ElapsedMilliseconds, null);
            }

            if (cancellationTokenSource is not null)
            {
                if (methodMetadata.MethodDetail.ReturnTypeDetailBoxed.IsTask)
                {
                    _ = ((Task)(object)result).ContinueWith((_) => cancellationTokenSource.Dispose());
                }
                else
                {
                    cancellationTokenSource.Dispose();
                }
            }

            return result;
        }

        private static async Task CallMethodTaskLogged(bool handled, IQueryClient? queryClient, object? cacheProvider, Type interfaceType, MethodDetail methodDetail, object[] arguments, NetworkType networkType, string source, CancellationToken cancellationToken)
        {
            busLogger?.BeginCall(interfaceType, methodDetail.Name, arguments, null, source, handled);

            var timer = Stopwatch.StartNew();
            try
            {
                Task task;
                if (cacheProvider is not null)
                {
                    task = (Task)methodDetail.CallerBoxed(cacheProvider, arguments)!;
                }
                else if (handled)
                {
                    var provider = ProviderResolver.GetFirst(interfaceType);
                    task = (Task)methodDetail.CallerBoxed(provider, arguments)!;
                }
                else
                {
                    task = queryClient!.Call<Task>(interfaceType, methodDetail.Name, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken)!;
                }

                await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLogger?.EndCall(interfaceType, methodDetail.Name, arguments, null, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLogger?.EndCall(interfaceType, methodDetail.Name, arguments, null, source, handled, timer.ElapsedMilliseconds, null);
        }
        private static readonly MethodDetail callMethodTaskGenericLoggedMethod = typeof(Bus).GetMethodDetail(nameof(CallMethodTaskGenericLogged));
        private static async Task<TReturn> CallMethodTaskGenericLogged<TReturn>(bool handled, IQueryClient? queryClient, object? cacheProvider, Type interfaceType, MethodDetail methodDetail, object[] arguments, NetworkType networkType, string source, CancellationToken cancellationToken)
        {
            busLogger?.BeginCall(interfaceType, methodDetail.Name, arguments, null, source, handled);

            TReturn taskresult;
            var timer = Stopwatch.StartNew();
            try
            {
                Task<TReturn> task;
                if (cacheProvider is not null)
                {
                    task = (Task<TReturn>)methodDetail.CallerBoxed(cacheProvider, arguments)!;
                }
                else if (handled)
                {
                    var provider = ProviderResolver.GetFirst(interfaceType);
                    task = (Task<TReturn>)methodDetail.CallerBoxed(provider, arguments)!;
                }
                else
                {
                    task = queryClient!.Call<Task<TReturn>>(interfaceType, methodDetail.Name, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source, cancellationToken)!;
                }

                taskresult = await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                busLogger?.EndCall(interfaceType, methodDetail.Name, arguments, null, source, handled, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            busLogger?.EndCall(interfaceType, methodDetail.Name, arguments, taskresult, source, handled, timer.ElapsedMilliseconds, null);

            return taskresult;
        }

        private static HashSet<Type> GetExposedCommandTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(interfaceType);
            if (typeDetail.Type.Name == iCommandHandlerType.Name || typeDetail.Type.Name == iCommandHandlerWithResultType.Name)
            {
                var messageType = typeDetail.InnerTypeDetails[0];
                if (messageType.Attributes.Any(x => x is ServiceExposedAttribute))
                    _ = messageTypes.Add(messageType.Type);
            }
            foreach (var item in typeDetail.Interfaces.Where(x => x.Name == iCommandHandlerType.Name || x.Name == iCommandHandlerWithResultType.Name))
            {
                var itemDetail = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetail.InnerTypeDetails[0];
                if (messageType.Attributes.Any(x => x is ServiceExposedAttribute))
                    _ = messageTypes.Add(messageType.Type);
            }
            return messageTypes;
        }
        private static HashSet<Type> GetExposedEventTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(interfaceType);
            if (typeDetail.Type.Name == iEventHandlerType.Name)
            {
                var messageType = typeDetail.InnerTypeDetail;
                if (messageType.Attributes.Any(x => x is ServiceExposedAttribute))
                    _ = messageTypes.Add(messageType.Type);
            }
            foreach (var item in typeDetail.Interfaces.Where(x => x.Name == iEventHandlerType.Name))
            {
                var itemDetail = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetail.InnerTypeDetail;
                if (messageType.Attributes.Any(x => x is ServiceExposedAttribute))
                    _ = messageTypes.Add(messageType.Type);
            }
            return messageTypes;
        }

        //don't need to cache these here, the message services will hold the values
        private static string GetCommandTopic(Type commandType)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);
            for (; ; )
            {
                var implementationTypes = Discovery.GetTypesByInterface(interfaceType).Where(x => x.IsInterface).ToArray();
                if (implementationTypes.Length == 0)
                    return interfaceType.GetNiceName().Replace('<', '_').Replace(">", String.Empty);
                else if (implementationTypes.Length == 1)
                    interfaceType = implementationTypes[0];
                else
                    throw new Exception($"More than one interface inherits {interfaceType.GetNiceName()} so cannot determine the topic for {commandType.GetNiceName()}");
            }
        }
        private static string GetEventTopic(Type eventType)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);
            for (; ; )
            {
                var implementationTypes = Discovery.GetTypesByInterface(interfaceType).Where(x => x.IsInterface).ToArray();
                if (implementationTypes.Length == 0)
                    return interfaceType.GetNiceName().Replace('<', '_').Replace(">", String.Empty);
                else if (implementationTypes.Length == 1)
                    interfaceType = implementationTypes[0];
                else
                    throw new Exception($"More than one interface inherits {interfaceType.GetNiceName()} so cannot determine the topic for {eventType.GetNiceName()}");
            }
        }

        /// <summary>
        /// Manually add a command producer service to send commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host commands with the <see cref="ServiceExposedAttribute"/>.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to send.</typeparam>
        /// <param name="commandProducer">The command producer service.</param>
        public static void AddCommandProducer<TInterface>(ICommandProducer commandProducer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                var commandTypes = GetExposedCommandTypesFromInterface(interfaceType);
                foreach (var commandType in commandTypes)
                {
                    if (handledTypes.Contains(commandType))
                    {
                        _ = Log.ErrorAsync($"Cannot add Command Producer: type already added as Consumer {commandType.GetNiceName()}");
                        continue;
                    }
                    if (commandProducers.ContainsKey(commandType))
                    {
                        _ = Log.ErrorAsync($"Cannot add Command Producer: type already added as Producer {commandType.GetNiceName()}");
                        continue;
                    }
                    var topic = GetCommandTopic(commandType);
                    commandProducer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                    commandProducers.Add(commandType, commandProducer);
                    _ = Log.InfoAsync($"{commandProducer.GetType().GetNiceName()}c - {commandType.GetNiceName()}");
                }
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually add a command consumer service to receive commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to receive.</typeparam>
        /// <param name="commandConsumer">The command consumer service.</param>
        public static void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                commandConsumer.Setup(commandCounter, RemoteHandleCommandDispatchAsync, RemoteHandleCommandDispatchAwaitAsync, RemoteHandleCommandWithResultDispatchAwaitAsync);
                _ = commandConsumers.Add(commandConsumer);

                var commandTypes = GetExposedCommandTypesFromInterface(interfaceType);
                foreach (var commandType in commandTypes)
                {
                    if (TypeAnalyzer.GetTypeDetail(commandType).Interfaces.Any(x => x == typeof(ICommand)))
                    {
                        var hasHandler = ProviderResolver.HasBase(TypeAnalyzer.GetGenericType(typeof(ICommandHandler<>), commandType));
                        if (hasHandler)
                        {
                            if (commandProducers.ContainsKey(commandType))
                            {
                                _ = Log.ErrorAsync($"Cannot add Command Consumer: type already added as Producer {commandType.GetNiceName()}");
                                continue;
                            }
                            var topic = GetCommandTopic(commandType);
                            commandConsumer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                            _ = handledTypes.Add(commandType);
                            _ = Log.InfoAsync($"{commandConsumer.GetType().GetNiceName()} at {commandConsumer.MessageHost} - {commandType.GetNiceName()}");
                        }
                    }
                }

                commandConsumer.Open();
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually add an event producer service to send commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host events with the <see cref="ServiceExposedAttribute"/>.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to send.</typeparam>
        /// <param name="eventProducer">The event producer service.</param>
        public static void AddEventProducer<TInterface>(IEventProducer eventProducer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                var eventTypes = GetExposedEventTypesFromInterface(interfaceType);
                foreach (var eventType in eventTypes)
                {
                    if (eventProducers.ContainsKey(eventType))
                    {
                        _ = Log.ErrorAsync($"Cannot add Event Producer: type already added as Producer {eventType.GetNiceName()}");
                        continue;
                    }
                    var topic = GetEventTopic(eventType);
                    eventProducer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                    if (!eventProducers.TryGetValue(eventType, out var eventProducerList))
                    {
                        eventProducerList = new();
                        eventProducers.Add(eventType, eventProducerList);
                    }
                    eventProducerList.Add(eventProducer);
                    _ = Log.InfoAsync($"{eventProducers.GetType().GetNiceName()} at {eventProducer.MessageHost} - {eventType.GetNiceName()}");
                }
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually add an event consumer service to receive commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to receive.</typeparam>
        /// <param name="eventConsumer">The event consumer service.</param>
        public static void AddEventConsumer<TInterface>(IEventConsumer eventConsumer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                eventConsumer.Setup(RemoteHandleEventDispatchAsync);
                _ = eventConsumers.Add(eventConsumer);

                var eventTypes = GetExposedEventTypesFromInterface(interfaceType);
                foreach (var eventType in eventTypes)
                {
                    if (TypeAnalyzer.GetTypeDetail(eventType).Interfaces.Any(x => x == typeof(IEvent)))
                    {
                        var hasHandler = ProviderResolver.HasBase(TypeAnalyzer.GetGenericType(typeof(IEventHandler<>), eventType));
                        if (hasHandler)
                        {
                            var topic = GetEventTopic(eventType);
                            eventConsumer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                            _ = handledTypes.Add(eventType);
                            _ = Log.InfoAsync($"{eventConsumer.GetType().GetNiceName()} at {eventConsumer.MessageHost} - {eventType.GetNiceName()}");
                        }
                    }
                }

                eventConsumer.Open();
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually add a query client service to call for queries.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryClient">The query client service.</param>
        public static void AddQueryClient<TInterface>(IQueryClient queryClient)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                if (handledTypes.Contains(interfaceType))
                {
                    _ = Log.ErrorAsync($"Cannot add Query Client: type already added as Server {interfaceType.GetNiceName()}");
                    return;
                }
                if (queryClients.ContainsKey(interfaceType))
                {
                    _ = Log.ErrorAsync($"Cannot add Query Client: type already added as Client {interfaceType.GetNiceName()}");
                    return;
                }
                queryClient.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
                queryClients.Add(interfaceType, queryClient);
                _ = Log.InfoAsync($"{queryClients.GetType().GetNiceName()} at {queryClient.ServiceUrl} - {interfaceType.GetNiceName()}");
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually add a query server to receive and response to queries.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host implementations with the <see cref="ServiceExposedAttribute"/> on the interface.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryServer">The query server service.</param>
        public static void AddQueryServer<TInterface>(IQueryServer queryServer)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

            setupLock.Wait();
            try
            {
                queryServer.Setup(commandCounter, RemoteHandleQueryCallAsync);
                _ = queryServers.Add(queryServer);

                if (interfaceType.IsInterface)
                {
                    var hasImplementation = ProviderResolver.HasBase(interfaceType);
                    if (hasImplementation)
                    {
                        if (queryClients.ContainsKey(interfaceType))
                        {
                            _ = Log.ErrorAsync($"Cannot add Query Client: type already added as Client {interfaceType.GetNiceName()}");
                            return;
                        }
                        queryServer.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
                        _ = handledTypes.Add(interfaceType);
                        _ = Log.InfoAsync($"{queryServer.GetType().GetNiceName()} at {queryServer.ServiceUrl} - {interfaceType.GetNiceName()}");
                    }
                }

                queryServer.Open();
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Adds a logger for the bus.
        /// This will intercept for logging any command, event, or query with the <see cref="ServiceLogAttribute"/>
        /// Note that this has a slightly degraded performance impact.
        /// </summary>
        /// <param name="busLogger">The bus logger instance.</param>
        public static void AddLogger(IBusLogger busLogger)
        {
            setupLock.Wait();
            try
            {
                if (Bus.busLogger is not null)
                    throw new InvalidOperationException("Bus already has a logger");
                Bus.busLogger = busLogger;
            }
            finally
            {
                setupLock.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Authenticate(IPrincipal? principal, string[]? roles, Func<string> message)
        {
            if (principal is null || principal.Identity is null || !principal.Identity.IsAuthenticated)
                throw new SecurityException(message());
            if (roles is not null && roles.Length > 0)
            {
                foreach (var role in roles)
                {
                    if (principal.IsInRole(role))
                        return;
                }
                throw new SecurityException(message());
            }
        }

        /// <summary>
        /// Starts all the services defined by a ServiceSettings instance and a service creator.
        /// The static class <see cref="Zerra.CQRS.Settings.CQRSSettings"/> can assist in getting these settings from a json file.
        /// Each type of type service has a creator class to select what to create. The best service for direct queries and commands is <see cref="Zerra.CQRS.TcpServiceCreator"/> however events need a message service.
        /// See other Zerra.CQRS libraries to add other types of services for commands and events.
        /// </summary>
        /// <param name="serviceSettings">The settings definining all the services.</param>
        /// <param name="serviceCreator">An instance that selects the services to create.</param>
        public static void StartServices(ServiceSettings serviceSettings, IServiceCreator serviceCreator)
        {
            setupLock.Wait();
            try
            {
                if (HasServices)
                    throw new InvalidOperationException($"Cannot {nameof(StartServices)} after services added");

                _ = Log.InfoAsync($"Starting {serviceSettings.ThisServiceName}");

                List<ICommandConsumer>? newCommandConsumers = null;
                List<IEventConsumer>? newEventConsumers = null;
                List<IQueryServer>? newQueryServers = null;

                //Find all the types that will be hosted so we don't create producers for them
                var thisServerTypes = new HashSet<Type>();
                if (serviceSettings.Queries is not null)
                {
                    foreach (var clientQuerySetting in serviceSettings.Queries)
                    {
                        if (clientQuerySetting.Service != serviceSettings.ThisServiceName)
                            continue;
                        if (clientQuerySetting.Types is null)
                            continue;
                        foreach (var serviceType in clientQuerySetting.Types)
                        {
                            if (String.IsNullOrEmpty(serviceType))
                                throw new Exception($"{serviceType ?? "null"} is not an interface");
                            var interfaceType = Discovery.GetTypeFromName(serviceType);
                            if (!interfaceType.IsInterface)
                                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

                            if (interfaceType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                _ = thisServerTypes.Add(interfaceType);
                        }
                    }
                }
                if (serviceSettings.Messages is not null)
                {
                    foreach (var clientMessageSetting in serviceSettings.Messages)
                    {
                        if (clientMessageSetting.Service != serviceSettings.ThisServiceName)
                            continue;
                        if (clientMessageSetting.Types is null)
                            continue;
                        foreach (var serviceType in clientMessageSetting.Types)
                        {
                            if (String.IsNullOrEmpty(serviceType))
                                throw new Exception($"{serviceType ?? "null"} is not an interface");
                            var interfaceType = Discovery.GetTypeFromName(serviceType);
                            if (!interfaceType.IsInterface)
                                throw new Exception($"{interfaceType.GetNiceName()} is not an interface");

                            var commandTypes = GetExposedCommandTypesFromInterface(interfaceType);
                            foreach (var commandType in commandTypes)
                                _ = thisServerTypes.Add(commandType);

                            var eventTypes = GetExposedEventTypesFromInterface(interfaceType);
                            foreach (var eventType in eventTypes)
                                _ = thisServerTypes.Add(eventType);
                        }
                    }
                }

                //Find all the client/server services
                if (serviceSettings.Queries is not null)
                {
                    foreach (var serviceQuerySetting in serviceSettings.Queries)
                    {
                        if (serviceQuerySetting.Types is null || serviceQuerySetting.Types.Length == 0)
                            continue;

                        //Each service may have one of each client/server

                        IQueryClient? queryClient = null;
                        Type? queryClientType = null;

                        IQueryServer? queryServer = null;
                        Type? queryServerType = null;

                        foreach (var serviceType in serviceQuerySetting.Types)
                        {
                            if (String.IsNullOrEmpty(serviceType))
                                throw new Exception($"{serviceType ?? "null"} is not an interface");

                            var interfaceType = Discovery.GetTypeFromName(serviceType);
                            if (!interfaceType.IsInterface)
                            {
                                _ = Log.ErrorAsync($"{interfaceType.GetNiceName()} is not an interface");
                                continue;
                            }

                            if (!interfaceType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                continue;

                            if (serviceQuerySetting.Service == serviceSettings.ThisServiceName)
                            {
                                if (String.IsNullOrWhiteSpace(serviceQuerySetting.BindingUrl))
                                {
                                    _ = Log.ErrorAsync($"Cannot add Query Server: missing {nameof(serviceQuerySetting.BindingUrl)} {interfaceType.GetNiceName()}");
                                    continue;
                                }

                                if (queryClients.ContainsKey(interfaceType))
                                {
                                    _ = Log.ErrorAsync($"Cannot add Query Server: type already added as Client {interfaceType.GetNiceName()}");
                                    continue;
                                }

                                try
                                {
                                    if (queryServer is null)
                                    {
                                        var encryptionKey = String.IsNullOrWhiteSpace(serviceQuerySetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceQuerySetting.EncryptionKey);
                                        var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                        queryServer = serviceCreator.CreateQueryServer(serviceQuerySetting.BindingUrl, symmetricConfig);
                                        if (queryServer is not null)
                                        {
                                            queryServerType = queryServer.GetType();
                                            queryServer.Setup(commandCounter, RemoteHandleQueryCallAsync);
                                            _ = queryServers.Add(queryServer);
                                            newQueryServers ??= new();
                                            newQueryServers.Add(queryServer);
                                        }
                                    }
                                    if (queryServer is not null && queryServerType is not null)
                                    {
                                        _ = handledTypes.Add(interfaceType);
                                        queryServer.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
                                        _ = Log.InfoAsync($"Hosting - {queryServerType.GetNiceName()} at {queryServer.ServiceUrl} - {interfaceType.GetNiceName()}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = Log.ErrorAsync($"Failed to create Query Server for {serviceQuerySetting.Service}", ex);
                                }
                            }
                            else
                            {
                                if (thisServerTypes.Contains(interfaceType))
                                    continue;

                                if (String.IsNullOrWhiteSpace(serviceQuerySetting.ExternalUrl))
                                {
                                    _ = Log.ErrorAsync($"Cannot add Query Client: missing {nameof(serviceQuerySetting.ExternalUrl)} {interfaceType.GetNiceName()}");
                                    continue;
                                }

                                if (handledTypes.Contains(interfaceType))
                                {
                                    _ = Log.ErrorAsync($"Cannot add Query Client: type already added as Server {interfaceType.GetNiceName()}");
                                    continue;
                                }
                                if (queryClients.ContainsKey(interfaceType))
                                {
                                    _ = Log.ErrorAsync($"Cannot add Query Client: type already added as Client {interfaceType.GetNiceName()}");
                                    continue;
                                }

                                try
                                {
                                    if (queryClient is null)
                                    {
                                        var encryptionKey = String.IsNullOrWhiteSpace(serviceQuerySetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceQuerySetting.EncryptionKey);
                                        var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                        queryClient = serviceCreator.CreateQueryClient(serviceQuerySetting.ExternalUrl, symmetricConfig);
                                        if (queryClient is not null)
                                        {
                                            queryClientType = queryClient.GetType();
                                        }
                                    }
                                    if (queryClient is not null && queryClientType is not null)
                                    {
                                        queryClient.RegisterInterfaceType(maxConcurrentQueries, interfaceType);
                                        queryClients.Add(interfaceType, queryClient);
                                        _ = Log.InfoAsync($"{serviceQuerySetting.Service} - {queryClientType.GetNiceName()} at {queryClient.ServiceUrl} - {interfaceType.GetNiceName()}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = Log.ErrorAsync($"Failed to create Query Client for {serviceQuerySetting.Service}", ex);
                                }
                            }
                        }
                    }
                }

                //Find all the consumer/producer services
                if (serviceSettings.Messages is not null)
                {
                    foreach (var serviceMessageSetting in serviceSettings.Messages)
                    {
                        if (serviceMessageSetting.Types is null)
                            continue;

                        if (String.IsNullOrEmpty(serviceMessageSetting.MessageHost))
                        {
                            _ = Log.ErrorAsync($"{serviceMessageSetting.Service} has no {nameof(serviceMessageSetting.MessageHost)}");
                            continue;
                        }

                        //Each service may have one of each producer/consumer.

                        ICommandProducer? commandProducer = null;
                        Type? commandProducerType = null;

                        Dictionary<string, IEventProducer>? eventProducerDictionary = null;
                        Type? eventProducerType = null;

                        ICommandConsumer? commandConsumer = null;
                        Type? commandConsumerType = null;

                        IEventConsumer? eventConsumer = null;
                        Type? eventConsumerType = null;

                        foreach (var serviceType in serviceMessageSetting.Types)
                        {
                            if (String.IsNullOrEmpty(serviceType))
                                throw new Exception($"{serviceType ?? "null"} is not an interface");
                            var interfaceType = Discovery.GetTypeFromName(serviceType);
                            if (!interfaceType.IsInterface)
                            {
                                _ = Log.ErrorAsync($"{interfaceType.GetNiceName()} is not an interface");
                                continue;
                            }

                            var commandTypes = GetExposedCommandTypesFromInterface(interfaceType);
                            if (commandTypes.Count > 0)
                            {
                                if (serviceMessageSetting.Service == serviceSettings.ThisServiceName)
                                {
                                    try
                                    {
                                        if (commandConsumer is null)
                                        {
                                            var encryptionKey = String.IsNullOrWhiteSpace(serviceMessageSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceMessageSetting.EncryptionKey);
                                            var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                            commandConsumer = serviceCreator.CreateCommandConsumer(serviceMessageSetting.MessageHost, symmetricConfig);
                                            if (commandConsumer is not null)
                                            {
                                                commandConsumerType = commandConsumer.GetType();
                                                commandConsumer.Setup(commandCounter, RemoteHandleCommandDispatchAsync, RemoteHandleCommandDispatchAwaitAsync, RemoteHandleCommandWithResultDispatchAwaitAsync);
                                                _ = commandConsumers.Add(commandConsumer);
                                                newCommandConsumers ??= new();
                                                newCommandConsumers.Add(commandConsumer);
                                            }
                                        }
                                        if (commandConsumer is not null && commandConsumerType is not null)
                                        {
                                            foreach (var commandType in commandTypes)
                                            {
                                                if (!commandType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                                    continue;
                                                if (commandProducers.ContainsKey(commandType))
                                                {
                                                    _ = Log.ErrorAsync($"Cannot add Command Consumer: type already added as Producer {commandType.GetNiceName()}");
                                                    continue;
                                                }
                                                var topic = GetCommandTopic(commandType);
                                                commandConsumer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                                                _ = handledTypes.Add(commandType);
                                                _ = Log.InfoAsync($"Hosting - {commandConsumerType.GetNiceName()} at {commandConsumer.MessageHost} - {commandType.GetNiceName()}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = Log.ErrorAsync($"Failed to create Command Consumer for {serviceMessageSetting.Service}", ex);
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        var clientCommandTypes = commandTypes.Where(x => !thisServerTypes.Contains(x)).ToArray();
                                        if (clientCommandTypes.Length > 0)
                                        {
                                            if (commandProducer is null)
                                            {
                                                var encryptionKey = String.IsNullOrWhiteSpace(serviceMessageSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceMessageSetting.EncryptionKey);
                                                var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                                commandProducer = serviceCreator.CreateCommandProducer(serviceMessageSetting.MessageHost, symmetricConfig);
                                                if (commandProducer is not null)
                                                {
                                                    commandProducerType = commandProducer.GetType();
                                                }
                                            }
                                            if (commandProducer is not null && commandProducerType is not null)
                                            {
                                                foreach (var commandType in clientCommandTypes)
                                                {
                                                    if (handledTypes.Contains(commandType))
                                                    {
                                                        _ = Log.ErrorAsync($"Cannot add Command Producer: type already added as Consumer {commandType.GetNiceName()}");
                                                        continue;
                                                    }
                                                    if (commandProducers.ContainsKey(commandType))
                                                    {
                                                        _ = Log.ErrorAsync($"Cannot add Command Producer: type already added as Producer {commandType.GetNiceName()}");
                                                        continue;
                                                    }
                                                    var topic = GetCommandTopic(commandType);
                                                    commandProducer.RegisterCommandType(maxConcurrentCommandsPerTopic, topic, commandType);
                                                    commandProducers.Add(commandType, commandProducer);
                                                    _ = Log.InfoAsync($"{serviceMessageSetting.Service} - {commandProducerType.GetNiceName()} at {commandProducer.MessageHost} - {commandType.GetNiceName()}");
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = Log.ErrorAsync($"Failed to create Command Producer for {serviceMessageSetting.Service}", ex);
                                    }
                                }
                            }

                            var eventTypes = GetExposedEventTypesFromInterface(interfaceType);
                            if (eventTypes.Count > 0)
                            {
                                //events fan out so can have producer and consumer on same service
                                if (serviceMessageSetting.Service == serviceSettings.ThisServiceName)
                                {
                                    try
                                    {
                                        if (eventConsumer is null)
                                        {
                                            var encryptionKey = String.IsNullOrWhiteSpace(serviceMessageSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceMessageSetting.EncryptionKey);
                                            var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                            eventConsumer = serviceCreator.CreateEventConsumer(serviceMessageSetting.MessageHost, symmetricConfig);
                                            if (eventConsumer is not null)
                                            {
                                                eventConsumerType = eventConsumer.GetType();
                                                eventConsumer.Setup(RemoteHandleEventDispatchAsync);
                                                _ = eventConsumers.Add(eventConsumer);
                                                newEventConsumers ??= new();
                                                newEventConsumers.Add(eventConsumer);
                                            }
                                        }
                                        if (eventConsumer is not null && eventConsumerType is not null)
                                        {
                                            foreach (var eventType in eventTypes)
                                            {
                                                var topic = GetEventTopic(eventType);
                                                eventConsumer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                                                _ = handledTypes.Add(eventType);
                                                _ = Log.InfoAsync($"Hosting - {eventConsumerType.GetNiceName()} at {eventConsumer.MessageHost} - {eventType.GetNiceName()}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = Log.ErrorAsync($"Failed to create Event Consumer for {serviceMessageSetting.Service}", ex);
                                    }
                                }

                                try
                                {
                                    eventProducerDictionary ??= new();
                                    if (!eventProducerDictionary.TryGetValue(serviceMessageSetting.MessageHost, out var eventProducer))
                                    {
                                        var encryptionKey = String.IsNullOrWhiteSpace(serviceMessageSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceMessageSetting.EncryptionKey);
                                        var symmetricConfig = encryptionKey is null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                        eventProducer = serviceCreator.CreateEventProducer(serviceMessageSetting.MessageHost, symmetricConfig);
                                        if (eventProducer is not null)
                                        {
                                            eventProducerType = eventProducer.GetType();
                                            eventProducerDictionary.Add(serviceMessageSetting.MessageHost, eventProducer);
                                        }
                                    }

                                    if (eventProducer is not null && eventProducerType is not null)
                                    {
                                        foreach (var eventType in eventTypes)
                                        {
                                            var topic = GetEventTopic(eventType);
                                            eventProducer.RegisterEventType(maxConcurrentEventsPerTopic, topic, eventType);
                                            if (!eventProducers.TryGetValue(eventType, out var eventProducerList))
                                            {
                                                eventProducerList = new();
                                                eventProducers.Add(eventType, eventProducerList);
                                            }
                                            eventProducerList.Add(eventProducer);
                                            _ = Log.InfoAsync($"{serviceMessageSetting.Service} - {eventProducerType.GetNiceName()} at {eventProducer.MessageHost} - {eventType.GetNiceName()}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = Log.ErrorAsync($"Failed to create Event Producer for {serviceMessageSetting.Service}", ex);
                                }
                            }
                        }
                    }
                }

                //Start all the services
                if (newCommandConsumers is not null)
                {
                    foreach (var newCommandConsumer in newCommandConsumers)
                    {
                        try
                        {
                            newCommandConsumer.Open();
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync($"Failed to open Command Consumer", ex);
                        }
                    }
                }
                if (newEventConsumers is not null)
                {
                    foreach (var newEventConsumer in newEventConsumers)
                    {
                        try
                        {
                            newEventConsumer.Open();
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync($"Failed to open Event Consumer", ex);
                        }
                    }
                }
                if (newQueryServers is not null)
                {
                    foreach (var newQueryServer in newQueryServers)
                    {
                        try
                        {
                            newQueryServer.Open();
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync($"Failed to open Query Server", ex);
                        }
                    }
                }
            }
            finally
            {
                setupLock.Release();
            }
        }

        /// <summary>
        /// Manually stop all the services. This is not necessary if using <see cref="WaitForExit"/> or <see cref="WaitForExitAsync"/>. 
        /// </summary>
        public static void StopServices()
        {
            StopServicesAsync().GetAwaiter().GetResult();
        }
        /// <summary>
        /// Manually stop all the services. This is not necessary if using <see cref="WaitForExit"/> or <see cref="WaitForExitAsync"/>.
        /// </summary>
        public static async Task StopServicesAsync()
        {
            _ = Log.InfoAsync($"{nameof(Bus)} Shutting Down");

            if (processWaiter is not null)
                processWaiter.Dispose();

            await setupLock.WaitAsync();

            try
            {
                var asyncDisposed = new HashSet<IAsyncDisposable>();
                var disposed = new HashSet<IDisposable>();

                foreach (var commandProducer in commandProducers.Values)
                {
                    if (commandProducer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (commandProducer is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                commandProducers.Clear();

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

                foreach (var eventProducer in eventProducers.Values)
                {
                    if (eventProducer is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (eventProducer is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                commandProducers.Clear();

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
                commandConsumers.Clear();

                if (busLogger is not null)
                {
                    if (busLogger is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (busLogger is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                busLogger = null;

                foreach (var client in queryClients.Values)
                {
                    if (client is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (client is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                queryClients.Clear();

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
                queryClients.Clear();
            }
            finally
            {
                _ = setupLock.Release();
                setupLock.Dispose();
            }
        }

        private static void HandleProcessExit() => HandleProcessExit(null, null);
        private static void HandleProcessExit(object? sender, EventArgs? e)
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

        /// <summary>
        /// An awaiter to hold the assembly process until it receives a shutdown command.
        /// All the services will be stopped upon shutdown.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        public static void WaitForExit(CancellationToken cancellationToken = default)
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
            StopServicesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// An awaiter to hold the assembly process until it receives a shutdown command.
        /// All the services will be stopped upon shutdown.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        public static async Task WaitForExitAsync(CancellationToken cancellationToken = default)
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
    }
}