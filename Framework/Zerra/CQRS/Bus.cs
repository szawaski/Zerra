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
using Zerra.Threading;
using Zerra.Serialization;
using System.IO;
using System.Threading;
using Zerra.CQRS.Settings;
using Zerra.CQRS.Relay;
using Zerra.Encryption;

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
            var callerProvider = Call(interfaceType);
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
            return DispatchAsync(command);
        }
        public static Task HandleRemoteCommandDispatchAwaitAsync(ICommand command)
        {
            return DispatchAwaitAsync(command);
        }
        public static Task HandleRemoteEventDispatchAsync(IEvent @event)
        {
            return DispatchAsync(@event);
        }

        public static Task DispatchAsync(ICommand command) { return DispatchAsync(command, false); }
        public static Task DispatchAwaitAsync(ICommand command) { return DispatchAsync(command, true); }
        private static Task DispatchAsync(ICommand command, bool requireAffirmation)
        {
            var messageType = command.GetType();
            var messageTypeInfo = TypeAnalyzer.GetTypeDetail(messageType);
            if (!commandProducers.IsEmpty)
            {
                //Not a local call so apply cache layer at Bus level
                var handlerType = TypeAnalyzer.GetGenericType(iCommandHandlerType, messageType);

                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType != null)
                {
                    var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                    if (methodSetNextProvider != null)
                    {
                        var providerCache = Instantiator.GetSingle($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                        {
                            var instance = Instantiator.Create(providerCacheType);

                            var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                            var interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                            var messageHandlerToDispatchProvider = BusRouters.GetCommandHandlerToDispatchInternalInstance(interfaceType);
                            _ = methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                            return instance;
                        });

                        var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { messageType });
                        _ = method.Caller(providerCache, new object[] { command });

                        return Task.CompletedTask;
                    }
                }
            }

            return _DispatchCommandInternalAsync(command, messageType, requireAffirmation);
        }

        public static Task DispatchAsync(IEvent message)
        {
            var messageType = message.GetType();
            var messageTypeInfo = TypeAnalyzer.GetTypeDetail(messageType);
            if (!commandProducers.IsEmpty)
            {
                //Not a local call so apply cache layer at Bus level
                var handlerType = TypeAnalyzer.GetGenericType(iEventHandlerType, messageType);

                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType != null)
                {
                    var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                    if (methodSetNextProvider != null)
                    {
                        var providerCache = Instantiator.GetSingle($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                        {
                            var instance = Instantiator.Create(providerCacheType);

                            var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                            var interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                            var messageHandlerToDispatchProvider = BusRouters.GetEventHandlerToDispatchInternalInstance(interfaceType);
                            _ = methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                            return instance;
                        });

                        var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(IEventHandler<IEvent>.Handle), new Type[] { messageType });
                        _ = method.Caller(providerCache, new object[] { message });

                        return Task.CompletedTask;
                    }
                }
            }

            return _DispatchEventInternalAsync(message, messageType);
        }

#pragma warning disable IDE1006 // Naming Styles
        public static Task _DispatchCommandInternalAsync(ICommand message, Type messageType, bool requireAffirmation)
#pragma warning restore IDE1006 // Naming Styles
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

            if (requireAffirmation)
            {
                return HandleCommandAsync((ICommand)message, messageType, true);
            }
            else
            {
                //Task.Run execues on shared principal which can mess things up
                return TaskCopyPrincipal.Run(async () => { await HandleCommandAsync((ICommand)message, messageType, true); });
            }
        }
#pragma warning disable IDE1006 // Naming Styles
        public static async Task _DispatchEventInternalAsync(IEvent message, Type messageType)
#pragma warning restore IDE1006 // Naming Styles
        {
            IEventProducer producer = null;
            var messageBaseType = messageType;
            while (producer == null && messageBaseType != null)
            {
                if (eventProducers.TryGetValue(messageBaseType, out producer))
                {
                    await producer.DispatchAsync(message);
                }
                messageBaseType = messageBaseType.BaseType;
            }

            await HandleEventAsync((IEvent)message, messageType, true);
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
            var callerProvider = Call(interfaceType);
            return (TInterface)callerProvider;
        }
        public static object Call(Type interfaceType)
        {
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

#pragma warning disable IDE1006 // Naming Styles
        public static TReturn _CallInternal<TInterface, TReturn>(string methodName, object[] arguments) where TInterface : IBaseProvider
#pragma warning restore IDE1006 // Naming Styles
        {
            var interfaceType = typeof(TInterface);

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
        public static void AddCommandClient<TInterface>(ICommandProducer commandProducer) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                var type = typeof(TInterface);
                var commandTypes = GetCommandTypesFromInterface(type);
                foreach (var commandType in commandTypes)
                {
                    if (commandConsumerTypes.Contains(commandType))
                        throw new InvalidOperationException($"Cannot create loopback. Command Server already registered for type {commandType.GetNiceName()}");
                    if (commandProducers.ContainsKey(commandType))
                        throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                    _ = commandProducers.TryAdd(commandType, commandProducer);
                    //_ = Log.InfoAsync($"{nameof(Bus)} Added Command Client For {commandType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentReadWriteHashSet<ICommandConsumer> commandConsumers = new();
        private static readonly HashSet<Type> commandConsumerTypes = new();
        public static void AddCommandServer(ICommandConsumer commandConsumer)
        {
            lock (serviceLock)
            {
                var exposedTypes = Discovery.GetTypesFromAttribute(typeof(ServiceExposedAttribute));
                foreach (var type in exposedTypes)
                {
                    if (type.IsClass)
                    {
                        if (TypeAnalyzer.GetTypeDetail(type).Interfaces.Any(x => x == typeof(ICommand)))
                        {
                            var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                            var hasHandler = Discovery.HasImplementationType(TypeAnalyzer.GetGenericType(typeof(ICommandHandler<>), type), interfaceStack, interfaceStack.Length - 1);
                            if (hasHandler)
                            {
                                if (commandProducers.ContainsKey(type))
                                    throw new InvalidOperationException($"Cannot create loopback. Command Client already registered for type {type.GetNiceName()}");
                                if (!commandConsumerTypes.Contains(type))
                                    _ = commandConsumerTypes.Add(type);
                                commandConsumer.RegisterCommandType(type);
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
        public static void AddEventClient<TInterface>(IEventProducer eventProducer) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                var type = typeof(TInterface);
                var eventTypes = GetEventTypesFromInterface(type);
                foreach (var eventType in eventTypes)
                {
                    if (eventConsumerTypes.Contains(type))
                        throw new InvalidOperationException($"Cannot create loopback. Event Server already registered for type {type.GetNiceName()}");
                    if (eventProducers.ContainsKey(eventType))
                        throw new InvalidOperationException($"Event Client already registered for type {eventType.GetNiceName()}");
                    _ = eventProducers.TryAdd(eventType, eventProducer);
                    _ = Log.InfoAsync($"{nameof(Bus)} Added Event Client For {eventType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentReadWriteHashSet<IEventConsumer> eventConsumers = new();
        private static readonly HashSet<Type> eventConsumerTypes = new();
        public static void AddEventServer(IEventConsumer eventConsumer)
        {
            lock (serviceLock)
            {
                var exposedTypes = Discovery.GetTypesFromAttribute(typeof(ServiceExposedAttribute));
                foreach (var type in exposedTypes)
                {
                    if (type.IsClass)
                    {
                        if (TypeAnalyzer.GetTypeDetail(type).Interfaces.Any(x => x == typeof(IEvent)))
                        {
                            var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                            var hasHandler = Discovery.HasImplementationType(TypeAnalyzer.GetGenericType(typeof(IEventHandler<>), type), interfaceStack, interfaceStack.Length - 1);
                            if (hasHandler)
                            {
                                if (eventProducers.ContainsKey(type))
                                    throw new InvalidOperationException($"Cannot create loopback. Event Client already registered for type {type.GetNiceName()}");
                                if (!eventConsumerTypes.Contains(type))
                                    _ = eventConsumerTypes.Add(type);
                                eventConsumer.RegisterEventType(type);
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
                    throw new InvalidOperationException($"Cannot create loopback. Query Server already registered for type {interfaceType.GetNiceName()}");
                if (commandProducers.ContainsKey(interfaceType))
                    throw new InvalidOperationException($"Query Client already registered for type {interfaceType.GetNiceName()}");
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
                foreach (var type in exposedTypes)
                {
                    if (type.IsInterface && TypeAnalyzer.GetTypeDetail(type).Interfaces.Any(x => x == typeof(IBaseProvider)))
                    {
                        var interfaceStack = ProviderLayers.GetProviderInterfaceStack();
                        var hasImplementation = Discovery.HasImplementationType(type, interfaceStack, interfaceStack.Length - 1);
                        if (hasImplementation)
                        {
                            if (queryClients.ContainsKey(type))
                                throw new InvalidOperationException($"Cannot create loopback. Query Client already registered for type {type.GetNiceName()}");
                            if (!queryServerTypes.Contains(type))
                                _ = queryServerTypes.Add(type);
                            queryServer.RegisterInterfaceType(type);
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
                        var type = Discovery.GetTypeFromName(typeName);
                        if (!type.IsInterface)
                            throw new Exception($"{type.GetNiceName()} is not an interface");
                        var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                        if (!typeDetails.Interfaces.Contains(typeof(IBaseProvider)))
                            throw new Exception($"{type.GetNiceName()} does not inherit {nameof(IBaseProvider)}");

                        var commandTypes = GetCommandTypesFromInterface(type);
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
                                    if (commandProducers.ContainsKey(commandType))
                                        throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                                    if (commandConsumerTypes.Contains(commandType))
                                        throw new InvalidOperationException($"Command Server already registered for type {commandType.GetNiceName()}");
                                    _ = commandConsumerTypes.Add(commandType);
                                    commandConsumer.RegisterCommandType(commandType);
                                }
                            }
                            else
                            {
                                if (commandProducer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    commandProducer = serviceCreator.CreateCommandProducer(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                                }
                                var clientCommandTypes = commandTypes.Where(x => !serverTypes.Contains(x)).ToArray();
                                if (clientCommandTypes.Length > 0)
                                {
                                    foreach (var commandType in clientCommandTypes)
                                    {
                                        if (commandConsumerTypes.Contains(commandType))
                                            throw new InvalidOperationException($"Command Server already registered for type {commandType.GetNiceName()}");
                                        if (commandProducers.ContainsKey(commandType))
                                            throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                                        _ = commandProducers.TryAdd(commandType, commandProducer);
                                    }
                                }
                                else
                                {
                                    if (commandProducer is IDisposable disposable)
                                        disposable.Dispose();
                                }
                            }
                        }

                        var eventTypes = GetEventTypesFromInterface(type);
                        if (eventTypes.Count > 0)
                        {
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
                                    if (eventProducers.ContainsKey(eventType))
                                        throw new InvalidOperationException($"Event Client already registered for type {eventType.GetNiceName()}");
                                    if (eventConsumerTypes.Contains(eventType))
                                        throw new InvalidOperationException($"Event Server already registered for type {eventType.GetNiceName()}");
                                    _ = eventConsumerTypes.Add(eventType);
                                    eventConsumer.RegisterEventType(eventType);
                                }
                            }
                            else
                            {
                                if (eventProducer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    eventProducer = serviceCreator.CreateEventProducer(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                                }
                                var clientEventTypes = eventTypes.Where(x => !serverTypes.Contains(x)).ToArray();
                                if (clientEventTypes.Length > 0)
                                {
                                    foreach (var eventType in eventTypes)
                                    {
                                        if (eventConsumerTypes.Contains(eventType))
                                            throw new InvalidOperationException($"Event Server already registered for type {eventType.GetNiceName()}");
                                        if (!eventProducers.ContainsKey(eventType))
                                        {
                                            _ = eventProducers.TryAdd(eventType, eventProducer);
                                        }
                                    }
                                }
                                else
                                {
                                    if (eventProducer is IDisposable disposable)
                                        disposable.Dispose();
                                }
                            }
                        }

                        if (typeDetails.Attributes.Any(x => x is ServiceExposedAttribute))
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
                                if (queryClients.ContainsKey(type))
                                    throw new InvalidOperationException($"Query Client already registered for type {type.GetNiceName()}");
                                if (queryServerTypes.Contains(type))
                                    throw new InvalidOperationException($"Query Server already registered for type {type.GetNiceName()}");
                                _ = queryServerTypes.Add(type);
                                queryServer.RegisterInterfaceType(type);
                            }
                            else
                            {
                                if (queryClient == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                    queryClient = serviceCreator.CreateQueryClient(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                                }
                                if (!serverTypes.Contains(type))
                                {
                                    if (queryServerTypes.Contains(type))
                                        throw new InvalidOperationException($"Query Server already registered for type {type.GetNiceName()}");
                                    if (commandProducers.ContainsKey(type))
                                        throw new InvalidOperationException($"Query Client already registered for type {type.GetNiceName()}");
                                    _ = queryClients.TryAdd(type, queryClient);
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