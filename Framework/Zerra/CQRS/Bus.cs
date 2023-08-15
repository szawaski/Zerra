// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Logging;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Serialization;
using System.IO;
using System.Threading;
using Zerra.CQRS.Settings;
using Zerra.CQRS.Relay;
using Zerra.Encryption;
using System.Data;

#pragma warning disable IDE1006 // Naming Styles

namespace Zerra.CQRS
{
    public static class Bus
    {
        private static readonly Type iCommandType = typeof(ICommand);
        private static readonly Type iEventType = typeof(IEvent);
        private static readonly Type iCommandHandlerType = typeof(ICommandHandler<>);
        private static readonly Type iEventHandlerType = typeof(IEventHandler<>);
        private static readonly Type iCacheProviderType = typeof(ICacheProvider);
        private static readonly Type streamType = typeof(Stream);

        public static async Task<RemoteQueryCallResponse> HandleRemoteQueryCallAsync(Type interfaceType, string method, string[] arguments)
        {
            var callerProvider = _Call(interfaceType, true);
            var methodDetails = TypeAnalyzer.GetMethodDetail(interfaceType, method);

            if (methodDetails.ParametersInfo.Count != (arguments != null ? arguments.Length : 0))
                throw new ArgumentException("Invalid number of arguments for this method");

            var returnTypeDetail = methodDetails.ReturnType;

            var args = new object[arguments != null ? arguments.Length : 0];
            if (arguments != null && arguments.Length > 0)
            {
                var i = 0;
                foreach (var argument in arguments)
                {
                    var parameter = JsonSerializer.Deserialize(argument, methodDetails.ParametersInfo[i].ParameterType);
                    args[i] = parameter;
                    i++;
                }
            }

            bool isStream;
            object model;
            if (returnTypeDetail.IsTask)
            {
                isStream = returnTypeDetail.InnerTypeDetails[0].BaseTypes.Contains(streamType);
                var result = (Task)methodDetails.Caller(callerProvider, args);
                await result;

                if (returnTypeDetail.Type.IsGenericType)
                    model = returnTypeDetail.TaskResultGetter(result);
                else
                    model = null;
            }
            else
            {
                isStream = returnTypeDetail.BaseTypes.Contains(streamType);
                model = methodDetails.Caller(callerProvider, args);
            }

            if (isStream)
                return new RemoteQueryCallResponse((Stream)model);
            else
                return new RemoteQueryCallResponse(model);
        }
        public static Task HandleRemoteCommandDispatchAsync(ICommand command)
        {
            return _DispatchAsync(command, false, true);
        }
        public static Task HandleRemoteCommandDispatchAwaitAsync(ICommand command)
        {
            return _DispatchAsync(command, true, true);
        }
        public static Task HandleRemoteEventDispatchAsync(IEvent @event)
        {
            return _DispatchAsync(@event, true);
        }

        public static Task DispatchAsync(ICommand command) { return _DispatchAsync(command, false, false); }
        public static Task DispatchAwaitAsync(ICommand command) { return _DispatchAsync(command, true, false); }
        public static Task DispatchAsync(IEvent @event) { return _DispatchAsync(@event, false); }

        private static readonly ConcurrentFactoryDictionary<Type, Func<ICommand, Task>> commandCacheProviders = new();
        private static Task _DispatchAsync(ICommand command, bool requireAffirmation, bool externallyReceived)
        {
            var commandType = command.GetType();
            if (externallyReceived)
            {
                var exposed = commandType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute attribute);
                if (!exposed)
                    throw new Exception($"Command {commandType.GetNiceName()} is not exposed");
            }

            var cacheProviderDispatchAsync = commandCacheProviders.GetOrAdd(commandType, (t) =>
            {
                var handlerType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);
                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType == null)
                    return null;

                var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                if (methodSetNextProvider == null)
                    return null;

                var providerCache = Instantiator.GetSingle($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                {
                    var instance = Instantiator.Create(providerCacheType);

                    var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                    var interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                    var messageHandlerToDispatchProvider = BusRouters.GetCommandHandlerToDispatchInternalInstance(interfaceType);
                    _ = methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                    return instance;
                });

                var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { commandType });
                Func<ICommand, Task> caller = (arg) =>
                {
                    var task = (Task)method.Caller(providerCache, new object[] { arg });
                    return task;
                };

                return caller;
            });

            if (cacheProviderDispatchAsync != null)
                return cacheProviderDispatchAsync(command);

            return _DispatchCommandInternalAsync(command, commandType, requireAffirmation, externallyReceived);
        }

        private static readonly ConcurrentFactoryDictionary<Type, Func<IEvent, Task>> eventCacheProviders = new();
        private static Task _DispatchAsync(IEvent @event, bool externallyReceived)
        {
            var eventType = @event.GetType();
            if (externallyReceived)
            {
                var exposed = eventType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute attribute);
                if (!exposed)
                    throw new Exception($"Event {eventType.GetNiceName()} is not exposed");
            }

            var cacheProviderDispatchAsync = eventCacheProviders.GetOrAdd(eventType, (t) =>
            {
                var handlerType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);
                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType == null)
                    return null;

                var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                if (methodSetNextProvider == null)
                    return null;

                var providerCache = Instantiator.GetSingle($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                {
                    var instance = Instantiator.Create(providerCacheType);

                    var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                    var interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                    var messageHandlerToDispatchProvider = BusRouters.GetEventHandlerToDispatchInternalInstance(interfaceType);
                    _ = methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                    return instance;
                });

                var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });
                Func<IEvent, Task> caller = (arg) =>
                {
                    var task = (Task)method.Caller(providerCache, new object[] { arg });
                    return task;
                };

                return caller;
            });

            if (cacheProviderDispatchAsync != null)
                return cacheProviderDispatchAsync(@event);

            return _DispatchEventInternalAsync(@event, eventType, externallyReceived);
        }

        public static Task _DispatchCommandInternalAsync(ICommand message, Type messageType, bool requireAffirmation, bool externallyReceived)
        {
            if (!externallyReceived || !commandConsumerTypes.Contains(messageType))
            {
                ICommandProducer producer = null;
                var messageBaseType = messageType;
                while (producer == null && messageBaseType != null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                    {
                        if (requireAffirmation)
                        {
                            return producer.DispatchAsyncAwait(message);
                        }
                        else
                        {
                            return producer.DispatchAsync(message);
                        }
                    }
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            if (requireAffirmation || externallyReceived)
            {
                return HandleCommandAsync((ICommand)message, messageType, true);
            }
            else
            {
                var principal = Thread.CurrentPrincipal.CloneClaimsPrincipal();
                return Task.Run(() =>
                {
                    Thread.CurrentPrincipal = principal;
                    _ = HandleCommandAsync((ICommand)message, messageType, true);
                });
            }
        }
        public static Task _DispatchEventInternalAsync(IEvent message, Type messageType, bool externallyReceived)
        {
            if (!externallyReceived || !eventConsumerTypes.Contains(messageType))
            {
                IEventProducer producer = null;
                var messageBaseType = messageType;
                while (producer == null && messageBaseType != null)
                {
                    if (eventProducers.TryGetValue(messageBaseType, out producer))
                    {
                        return producer.DispatchAsync(message);
                    }
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            if (externallyReceived)
            {
                return HandleEventAsync((IEvent)message, messageType, true);
            }
            else
            {
                var principal = Thread.CurrentPrincipal.CloneClaimsPrincipal();
                return Task.Run(() =>
                {
                    Thread.CurrentPrincipal = principal;
                    _ = HandleEventAsync((IEvent)message, messageType, true);
                });
            }
        }

        private static Task HandleCommandAsync(ICommand command, Type commandType, bool throwError)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);

            var providerType = Discovery.GetImplementationType(interfaceType, ProviderLayers.GetProviderInterfaceStack(), 0, throwError);
            if (providerType == null)
                return Task.CompletedTask;
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { commandType });

            var provider = Instantiator.GetSingle(providerType);

            var task = (Task)method.Caller(provider, new object[] { command });

            LogMessage(commandType, command);

            return task;
        }
        private static Task HandleEventAsync(IEvent @event, Type eventType, bool throwError)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);

            var providerType = Discovery.GetImplementationType(interfaceType, ProviderLayers.GetProviderInterfaceStack(), 0, throwError);
            if (providerType == null)
                return Task.CompletedTask;
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });

            var provider = Instantiator.GetSingle(providerType);

            var task = (Task)method.Caller(provider, new object[] { @event });

            LogMessage(eventType, @event);

            return task;
        }

        private static void LogMessage(Type messageType, IMessage message)
        {
            if (messageLoggers.Count > 0)
            {
                foreach (var messageLogger in messageLoggers)
                {
                    _ = messageLogger.SaveAsync(messageType, message);
                }
            }
        }

        public static TInterface Call<TInterface>() where TInterface : IBaseProvider
        {
            var interfaceType = typeof(TInterface);
            var callerProvider = _Call(interfaceType, false);
            return (TInterface)callerProvider;
        }
        public static object Call(Type interfaceType)
        {
            return _Call(interfaceType, false);
        }

        private static object _Call(Type interfaceType, bool externallyReceived)
        {
            if (externallyReceived)
            {
                var exposed = interfaceType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute attribute);
                if (!exposed)
                    throw new Exception($"Interface {interfaceType.GetNiceName()} is not exposed");
            }

            var callerProvider = BusRouters.GetProviderToCallInternalInstance(interfaceType);

            if (!queryClients.IsEmpty)
            {
                if (queryClients.ContainsKey(interfaceType))
                {
                    //Not a local call so apply cache layer at Bus level
                    var providerCacheType = Discovery.GetImplementationType(interfaceType, iCacheProviderType, false);
                    if (providerCacheType != null)
                    {
                        var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider)).MethodInfo;
                        if (methodSetNextProvider != null)
                        {
                            var providerCache = Instantiator.GetSingle($"{providerCacheType.FullName}_Bus.Call_Cache", () =>
                            {
                                var instance = Instantiator.Create(providerCacheType);
                                _ = methodSetNextProvider.Invoke(instance, new object[] { callerProvider });
                                return instance;
                            });

                            return providerCache;
                        }
                    }
                }
            }

            return callerProvider;
        }

        public static TReturn _CallInternal<TInterface, TReturn>(Type interfaceType, string methodName, object[] arguments) where TInterface : IBaseProvider
        {
            if (!queryClients.IsEmpty)
            {
                if (queryClients.TryGetValue(interfaceType, out var methodCaller))
                {
                    var result = methodCaller.Call<TReturn>(interfaceType, methodName, arguments);
                    return result;
                }
            }

            var provider = Resolver.GetSingle<TInterface>();
            var methodDetails = TypeAnalyzer.GetMethodDetail(interfaceType, methodName);
            if (methodDetails.ParametersInfo.Count != (arguments != null ? arguments.Length : 0))
                throw new ArgumentException("Invalid number of arguments for this method");

            var localresult = (TReturn)methodDetails.Caller(provider, arguments);
            return localresult;
        }

        public static ICollection<Type> GetCommandTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetails = TypeAnalyzer.GetTypeDetail(interfaceType);
            foreach (var item in typeDetails.Interfaces.Where(x => x.Name == iCommandHandlerType.Name))
            {
                var itemDetails = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetails.InnerTypes[0];
                _ = messageTypes.Add(messageType);
            }
            return messageTypes;
        }
        public static ICollection<Type> GetEventTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetails = TypeAnalyzer.GetTypeDetail(interfaceType);
            foreach (var item in typeDetails.Interfaces.Where(x => x.Name == iEventHandlerType.Name))
            {
                var itemDetails = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetails.InnerTypes[0];
                _ = messageTypes.Add(messageType);
            }
            return messageTypes;
        }

        private static readonly object serviceLock = new();
        private static readonly SemaphoreSlim asyncServiceLock = new(1, 1);

        private static readonly ConcurrentDictionary<Type, ICommandProducer> commandProducers = new();
        public static void AddCommandProducer<TInterface>(ICommandProducer commandProducer) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                var type = typeof(TInterface);
                var commandTypes = GetCommandTypesFromInterface(type);
                foreach (var commandType in commandTypes)
                {
                    if (commandConsumerTypes.Contains(commandType))
                        throw new InvalidOperationException($"Cannot add Command Producer: Command Consumer already registered for type {commandType.GetNiceName()}");
                    if (commandProducers.ContainsKey(commandType))
                        throw new InvalidOperationException($"Cannot add Command Producer: Command Producer already registered for type {commandType.GetNiceName()}");
                    _ = commandProducers.TryAdd(commandType, commandProducer);
                    _ = Log.InfoAsync($"{nameof(Bus)} Added Command Producer For {commandType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentReadWriteHashSet<ICommandConsumer> commandConsumers = new();
        private static readonly HashSet<Type> commandConsumerTypes = new();
        public static void AddCommandConsumer(ICommandConsumer commandConsumer)
        {
            lock (serviceLock)
            {
                var exposedTypes = Discovery.GetTypesFromAttribute(typeof(ServiceExposedAttribute));
                foreach (var commandType in exposedTypes)
                {
                    if (commandType.IsClass)
                    {
                        if (TypeAnalyzer.GetTypeDetail(commandType).Interfaces.Any(x => x == typeof(ICommand)))
                        {
                            var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                            var hasHandler = Discovery.HasImplementationType(TypeAnalyzer.GetGenericType(typeof(ICommandHandler<>), commandType), interfaceStack, interfaceStack.Length - 1);
                            if (hasHandler)
                            {
                                if (commandProducers.ContainsKey(commandType))
                                    throw new InvalidOperationException($"Cannot add Command Consumer: Command Producer already registered for type {commandType.GetNiceName()}");
                                if (!commandConsumerTypes.Contains(commandType))
                                    throw new InvalidOperationException($"Cannot add Command Consumer: Command Consumer already registered for type {commandType.GetNiceName()}");
                                _ = commandConsumerTypes.Add(commandType);
                                commandConsumer.RegisterCommandType(commandType);
                                _ = Log.InfoAsync($"{nameof(Bus)} Added Command Consumer For {commandType.GetNiceName()}");
                            }
                        }
                    }
                }

                commandConsumer.SetHandler(HandleRemoteCommandDispatchAsync, HandleRemoteCommandDispatchAwaitAsync);
                _ = commandConsumers.Add(commandConsumer);
                commandConsumer.Open();
            }
        }

        private static readonly ConcurrentDictionary<Type, IEventProducer> eventProducers = new();
        public static void AddEventProducer<TInterface>(IEventProducer eventProducer) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                var type = typeof(TInterface);
                var eventTypes = GetEventTypesFromInterface(type);
                foreach (var eventType in eventTypes)
                {
                    if (eventProducers.ContainsKey(eventType))
                        throw new InvalidOperationException($"Cannot add Event Producer: Event Producer already registered for type {eventType.GetNiceName()}");
                    _ = eventProducers.TryAdd(eventType, eventProducer);
                    _ = Log.InfoAsync($"{nameof(Bus)} Added Event Producer For {eventType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentReadWriteHashSet<IEventConsumer> eventConsumers = new();
        private static readonly HashSet<Type> eventConsumerTypes = new();
        public static void AddEventConsumer(IEventConsumer eventConsumer)
        {
            lock (serviceLock)
            {
                var exposedTypes = Discovery.GetTypesFromAttribute(typeof(ServiceExposedAttribute));
                foreach (var eventType in exposedTypes)
                {
                    if (eventType.IsClass)
                    {
                        if (TypeAnalyzer.GetTypeDetail(eventType).Interfaces.Any(x => x == typeof(IEvent)))
                        {
                            var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                            var hasHandler = Discovery.HasImplementationType(TypeAnalyzer.GetGenericType(typeof(IEventHandler<>), eventType), interfaceStack, interfaceStack.Length - 1);
                            if (hasHandler)
                            {
                                if (!eventConsumerTypes.Contains(eventType))
                                    throw new InvalidOperationException($"Cannot add Event Consumer: Event Consumer already registered for type {eventType.GetNiceName()}");
                                _ = eventConsumerTypes.Add(eventType);
                                eventConsumer.RegisterEventType(eventType);
                            }
                        }
                    }
                }

                eventConsumer.SetHandler(HandleRemoteEventDispatchAsync);
                _ = eventConsumers.Add(eventConsumer);
                eventConsumer.Open();
            }
        }

        private static readonly ConcurrentDictionary<Type, IQueryClient> queryClients = new();
        public static void AddQueryClient<TInterface>(IQueryClient queryClient) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                var interfaceType = typeof(TInterface);
                if (queryServerTypes.Contains(interfaceType))
                    throw new InvalidOperationException($"Cannot add Query Client: Query Server already registered for type {interfaceType.GetNiceName()}");
                if (queryClients.ContainsKey(interfaceType))
                    throw new InvalidOperationException($"Cannot add Query Client: Query Client already registered for type {interfaceType.GetNiceName()}");
                _ = queryClients.TryAdd(interfaceType, queryClient);
                _ = Log.InfoAsync($"{nameof(Bus)} Added Query Client For {interfaceType.GetNiceName()}");
            }
        }

        private static readonly ConcurrentReadWriteHashSet<IQueryServer> queryServers = new();
        private static readonly HashSet<Type> queryServerTypes = new();
        public static void AddQueryServer(IQueryServer queryServer)
        {
            lock (serviceLock)
            {
                var exposedTypes = Discovery.GetTypesFromAttribute(typeof(ServiceExposedAttribute));
                foreach (var interfaceType in exposedTypes)
                {
                    if (interfaceType.IsInterface && TypeAnalyzer.GetTypeDetail(interfaceType).Interfaces.Any(x => x == typeof(IBaseProvider)))
                    {
                        var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                        var hasImplementation = Discovery.HasImplementationType(interfaceType, interfaceStack, interfaceStack.Length - 1);
                        if (hasImplementation)
                        {
                            if (queryClients.ContainsKey(interfaceType))
                                throw new InvalidOperationException($"Cannot add Query Client: Query Server already registered for type {interfaceType.GetNiceName()}");
                            if (queryServerTypes.Contains(interfaceType))
                                throw new InvalidOperationException($"Cannot add Query Server: Query Server already registered for type {interfaceType.GetNiceName()}");
                            _ = queryServerTypes.Add(interfaceType);
                            queryServer.RegisterInterfaceType(interfaceType);
                        }
                    }
                }

                queryServer.SetHandler(HandleRemoteQueryCallAsync);
                _ = queryServers.Add(queryServer);
                queryServer.Open();
            }
        }

        private static readonly ConcurrentReadWriteHashSet<IMessageLogger> messageLoggers = new();
        public static void AddMessageLogger(IMessageLogger messageLogger)
        {
            lock (serviceLock)
            {
                _ = messageLoggers.Add(messageLogger);
            }
        }

        private static readonly HashSet<object> instanciations = new();

        public static async Task DisposeServices()
        {
            _ = Log.InfoAsync($"{nameof(Bus)} Shutting Down");
            await asyncServiceLock.WaitAsync();
            try
            {
                var asyncDisposed = new HashSet<IAsyncDisposable>();
                var disposed = new HashSet<IDisposable>();

                foreach (var commandProducer in commandProducers.Select(x => x.Value))
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

                foreach (var eventProducer in eventProducers.Select(x => x.Value))
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

                foreach (var messageLogger in messageLoggers)
                {
                    if (messageLogger is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (messageLogger is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                messageLoggers.Clear();

                foreach (var client in queryClients.Select(x => x.Value))
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

                foreach (var instance in instanciations)
                {
                    if (instance is IAsyncDisposable asyncDisposable && !asyncDisposed.Contains(asyncDisposable))
                    {
                        await asyncDisposable.DisposeAsync();
                        _ = asyncDisposed.Add(asyncDisposable);
                    }
                    else if (instance is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        _ = disposed.Add(disposable);
                    }
                }
                instanciations.Clear();
            }
            finally
            {
                _ = asyncServiceLock.Release();
                asyncServiceLock.Dispose();
            }
        }

        public static void WaitUntilExit()
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { _ = waiter.Release(); };
            waiter.Wait();
            DisposeServices().GetAwaiter().GetResult();
        }
        public static void WaitUntilExit(CancellationToken cancellationToken)
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { _ = waiter.Release(); };
            try
            {
                waiter.Wait(cancellationToken);
            }
            catch { }
            DisposeServices().GetAwaiter().GetResult();
        }

        public static async Task WaitUntilExitAsync()
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { _ = waiter.Release(); };
            await waiter.WaitAsync();
            _ = Log.InfoAsync($"{nameof(Bus)} Exiting");
            await DisposeServices();
        }
        public static async Task WaitUntilExitAsync(CancellationToken cancellationToken)
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { _ = waiter.Release(); };
            try
            {
                await waiter.WaitAsync(cancellationToken);
            }
            catch { }
            await DisposeServices();
        }

        private const SymmetricAlgorithmType encryptionAlgoritm = SymmetricAlgorithmType.AESwithShift;
        public static void StartServices(ServiceSettings serviceSettings, IServiceCreator serviceCreator, IRelayRegister relayRegister = null)
        {
            _ = Log.InfoAsync($"Starting {serviceSettings.ThisServiceName}");
            lock (serviceLock)
            {
                var serverSetting = serviceSettings.Services.FirstOrDefault(x => x.Name == serviceSettings.ThisServiceName);
                if (serverSetting == null)
                    throw new Exception($"Service {serviceSettings.ThisServiceName} not found in CQRS settings file");

                var serviceUrl = serverSetting.InternalUrl;

                ICommandConsumer commandConsumer = null;
                IEventConsumer eventConsumer = null;
                IQueryServer queryServer = null;

                var serverTypes = new HashSet<Type>();
                foreach (var clientSetting in serviceSettings.Services)
                {
                    if (clientSetting.Types == null || clientSetting.Types.Length == 0)
                        continue;
                    if (clientSetting != serverSetting)
                        continue;
                    foreach (var typeName in clientSetting.Types)
                    {
                        var type = Discovery.GetTypeFromName(typeName);
                        if (!type.IsInterface)
                            throw new Exception($"{type.GetNiceName()} is not an interface");
                        var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                        if (!typeDetails.Interfaces.Contains(typeof(IBaseProvider)))
                            throw new Exception($"{type.GetNiceName()} does not inherit {nameof(IBaseProvider)}");

                        var commandTypes = GetCommandTypesFromInterface(type);
                        foreach (var commandType in commandTypes)
                            _ = serverTypes.Add(commandType);

                        var eventTypes = GetEventTypesFromInterface(type);
                        foreach (var eventType in eventTypes)
                            _ = serverTypes.Add(eventType);

                        if (typeDetails.Attributes.Any(x => x is ServiceExposedAttribute))
                            _ = serverTypes.Add(type);
                    }
                }

                foreach (var serviceSetting in serviceSettings.Services)
                {
                    if (serviceSetting.Types == null || serviceSetting.Types.Length == 0)
                        continue;

                    ICommandProducer commandProducer = null;
                    IEventProducer eventProducer = null;
                    IQueryClient queryClient = null;

                    foreach (var typeName in serviceSetting.Types)
                    {
                        var interfaceType = Discovery.GetTypeFromName(typeName);
                        if (!interfaceType.IsInterface)
                            throw new Exception($"{interfaceType.GetNiceName()} is not an interface");
                        var interfaceTypeDetails = TypeAnalyzer.GetTypeDetail(interfaceType);
                        if (!interfaceTypeDetails.Interfaces.Contains(typeof(IBaseProvider)))
                            throw new Exception($"{interfaceType.GetNiceName()} does not inherit {nameof(IBaseProvider)}");

                        var commandTypes = GetCommandTypesFromInterface(interfaceType);
                        if (commandTypes.Count > 0)
                        {
                            if (serviceSetting == serverSetting)
                            {
                                if (commandConsumer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    commandConsumer = serviceCreator.CreateCommandConsumer(serviceUrl, symmetricConfig);
                                    commandConsumer.SetHandler(HandleRemoteCommandDispatchAsync, HandleRemoteCommandDispatchAwaitAsync);
                                    if (!commandConsumers.Contains(commandConsumer))
                                        _ = commandConsumers.Add(commandConsumer);
                                }
                                foreach (var commandType in commandTypes)
                                {
                                    if (!commandType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                        continue;
                                    if (commandProducers.ContainsKey(commandType))
                                        throw new InvalidOperationException($"Cannot add Command Consumer: Command Producer already registered for type {commandType.GetNiceName()}");
                                    if (commandConsumerTypes.Contains(commandType))
                                        throw new InvalidOperationException($"Cannot add Command Consumer: Command Consumer already registered for type {commandType.GetNiceName()}");
                                    _ = commandConsumerTypes.Add(commandType);
                                    commandConsumer.RegisterCommandType(commandType);
                                }
                            }
                            else
                            {
                                var clientCommandTypes = commandTypes.Where(x => !serverTypes.Contains(x)).ToArray();
                                if (clientCommandTypes.Length > 0)
                                {
                                    if (commandProducer == null)
                                    {
                                        var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                        var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                        commandProducer = serviceCreator.CreateCommandProducer(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                                    }
                                    foreach (var commandType in clientCommandTypes)
                                    {
                                        if (commandConsumerTypes.Contains(commandType))
                                            throw new InvalidOperationException($"Cannot add Command Producer: Command Consumer already registered for type {commandType.GetNiceName()}");
                                        if (commandProducers.ContainsKey(commandType))
                                            throw new InvalidOperationException($"Cannot add Command Producer: Command Producer already registered for type {commandType.GetNiceName()}");
                                        _ = commandProducers.TryAdd(commandType, commandProducer);
                                    }
                                }
                            }
                        }

                        var eventTypes = GetEventTypesFromInterface(interfaceType);
                        if (eventTypes.Count > 0)
                        {
                            //events fan out so can have producer and consumer on same service
                            if (serviceSetting == serverSetting)
                            {
                                if (eventConsumer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    eventConsumer = serviceCreator.CreateEventConsumer(serviceUrl, symmetricConfig);
                                    eventConsumer.SetHandler(HandleRemoteEventDispatchAsync);
                                    if (!eventConsumers.Contains(eventConsumer))
                                        _ = eventConsumers.Add(eventConsumer);
                                }
                                foreach (var eventType in eventTypes)
                                {
                                    if (eventConsumerTypes.Contains(eventType))
                                        throw new InvalidOperationException($"Cannot add Event Consumer: Event Consumer already registered for type {eventType.GetNiceName()}");
                                    _ = eventConsumerTypes.Add(eventType);
                                    eventConsumer.RegisterEventType(eventType);
                                }
                            }

                            if (eventProducer == null)
                            {
                                var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                eventProducer = serviceCreator.CreateEventProducer(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                            }

                            foreach (var eventType in eventTypes)
                            {
                                if (!eventType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                    continue;
                                if (eventProducers.ContainsKey(eventType))
                                    throw new InvalidOperationException($"Cannot add Event Producer: Event Producer already registered for type {eventType.GetNiceName()}");
                                _ = eventProducers.TryAdd(eventType, eventProducer);
                            }
                        }

                        if (interfaceTypeDetails.Attributes.Any(x => x is ServiceExposedAttribute))
                        {
                            if (serviceSetting == serverSetting)
                            {
                                if (queryServer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    queryServer = serviceCreator.CreateQueryServer(serviceUrl, symmetricConfig);
                                    queryServer.SetHandler(HandleRemoteQueryCallAsync);
                                    if (!queryServers.Contains(queryServer))
                                        _ = queryServers.Add(queryServer);
                                }
                                if (queryClients.ContainsKey(interfaceType))
                                    throw new InvalidOperationException($"Cannot add Query Server: Query Client already registered for type {interfaceType.GetNiceName()}");
                                if (queryServerTypes.Contains(interfaceType))
                                    throw new InvalidOperationException($"Cannot add Query Server: Query Server already registered for type {interfaceType.GetNiceName()}");
                                _ = queryServerTypes.Add(interfaceType);
                                queryServer.RegisterInterfaceType(interfaceType);
                            }
                            else
                            {
                                if (queryClient == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    queryClient = serviceCreator.CreateQueryClient(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                                }
                                if (!serverTypes.Contains(interfaceType))
                                {
                                    if (queryServerTypes.Contains(interfaceType))
                                        throw new InvalidOperationException($"Cannot add Query Client: Query Server already registered for type {interfaceType.GetNiceName()}");
                                    if (commandProducers.ContainsKey(interfaceType))
                                        throw new InvalidOperationException($"Cannot add Query Client: Query Client already registered for type {interfaceType.GetNiceName()}");
                                    _ = queryClients.TryAdd(interfaceType, queryClient);
                                }
                                else
                                {
                                    if (queryClient is IDisposable disposable)
                                        disposable.Dispose();
                                }
                            }
                        }
                    }
                }

                Dictionary<string, HashSet<string>> relayRegisterTypes = null;
                if (relayRegister != null)
                {
                    relayRegisterTypes = new Dictionary<string, HashSet<string>>();
                }

                if (commandConsumer != null)
                {
                    commandConsumer.Open();
                    if (relayRegister != null)
                    {
                        var commandTypes = commandConsumer.GetCommandTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(commandConsumer.ConnectionString, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(commandConsumer.ConnectionString, relayTypes);
                        }
                        foreach (var type in commandTypes)
                            _ = relayTypes.Add(type);
                    }
                }
                if (eventConsumer != null)
                {
                    eventConsumer.Open();
                    if (relayRegister != null)
                    {
                        var eventTypes = eventConsumer.GetEventTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(eventConsumer.ConnectionString, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(eventConsumer.ConnectionString, relayTypes);
                        }
                        foreach (var type in eventTypes)
                            _ = relayTypes.Add(type);
                    }
                }
                if (queryServer != null)
                {
                    queryServer.Open();
                    if (relayRegister != null)
                    {
                        var queryTypes = queryServer.GetInterfaceTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(queryServer.ConnectionString, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(queryServer.ConnectionString, relayTypes);
                        }
                        foreach (var type in queryTypes)
                            _ = relayTypes.Add(type);
                    }
                }

                if (relayRegister != null)
                {
                    foreach (var group in relayRegisterTypes)
                        _ = relayRegister.Register(group.Key, group.Value.ToArray());
                }

                if (serverSetting.InstantiateTypes != null && serverSetting.InstantiateTypes.Length > 0)
                {
                    foreach (var instantiation in serverSetting.InstantiateTypes)
                    {
                        try
                        {
                            var type = Discovery.GetTypeFromName(instantiation);
                            var instance = Activator.CreateInstance(type);
                            _ = instanciations.Add(instance);
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync($"Failed to instantiate {instantiation}", ex);
                        }
                    }
                }
            }
        }
    }
}