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
                int i = 0;
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
            Type messageType = command.GetType();
            var messageTypeInfo = TypeAnalyzer.GetTypeDetail(messageType);
            if (!commandClients.IsEmpty)
            {
                //Not a local call so apply cache layer at Bus level
                var handlerType = TypeAnalyzer.GetGenericType(iCommandHandlerType, messageType);

                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType != null)
                {
                    var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                    if (methodSetNextProvider != null)
                    {
                        var providerCache = Instantiator.GetSingleInstance($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                        {
                            var instance = Instantiator.CreateInstance(providerCacheType);

                            var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                            Type interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                            object messageHandlerToDispatchProvider = BusRouters.GetCommandHandlerToDispatchInternalInstance(interfaceType);
                            methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                            return instance;
                        });

                        var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { messageType });
                        method.Caller(providerCache, new object[] { command });

                        return Task.CompletedTask;
                    }
                }
            }

            return _DispatchCommandInternalAsync(command, messageType, requireAffirmation);
        }

        public static Task DispatchAsync(IEvent message)
        {
            Type messageType = message.GetType();
            var messageTypeInfo = TypeAnalyzer.GetTypeDetail(messageType);
            if (!commandClients.IsEmpty)
            {
                //Not a local call so apply cache layer at Bus level
                var handlerType = TypeAnalyzer.GetGenericType(iEventHandlerType, messageType);

                var providerCacheType = Discovery.GetImplementationType(handlerType, iCacheProviderType, false);
                if (providerCacheType != null)
                {
                    var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.SetNextProvider));
                    if (methodSetNextProvider != null)
                    {
                        var providerCache = Instantiator.GetSingleInstance($"{providerCacheType.FullName}_Bus.DispatchAsync_Cache", () =>
                        {
                            var instance = Instantiator.CreateInstance(providerCacheType);

                            var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(BaseLayerProvider<IBaseProvider>.GetProviderInterfaceType));
                            Type interfaceType = (Type)methodGetProviderInterfaceType.Caller(instance, null);

                            object messageHandlerToDispatchProvider = BusRouters.GetEventHandlerToDispatchInternalInstance(interfaceType);
                            methodSetNextProvider.Caller(instance, new object[] { messageHandlerToDispatchProvider });

                            return instance;
                        });

                        var method = TypeAnalyzer.GetMethodDetail(providerCacheType, nameof(IEventHandler<IEvent>.Handle), new Type[] { messageType });
                        method.Caller(providerCache, new object[] { message });

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
            ICommandClient client = null;
            Type messageBaseType = messageType;
            while (client == null && messageBaseType != null)
            {
                if (commandClients.TryGetValue(messageBaseType, out client))
                {
                    if (requireAffirmation)
                    {
                        return client.DispatchAsyncAwait(message);
                    }
                    else
                    {
                        return client.DispatchAsync(message);
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
                return TaskSafePrincipal.Run(async () => { await HandleCommandAsync((ICommand)message, messageType, true); });
            }
        }
#pragma warning disable IDE1006 // Naming Styles
        public static async Task _DispatchEventInternalAsync(IEvent message, Type messageType)
#pragma warning restore IDE1006 // Naming Styles
        {
            IEventClient client = null;
            Type messageBaseType = messageType;
            while (client == null && messageBaseType != null)
            {
                if (eventClients.TryGetValue(messageBaseType, out client))
                {
                    await client.DispatchAsync(message);
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

            var provider = Instantiator.GetSingleInstance(providerType);

            var task = (Task)method.Caller(provider, new object[] { command });

            LogMessage(commandType, command);

            return task;
        }
        private static Task HandleEventAsync(IEvent @event, Type eventType, bool throwError)
        {
            Type interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);

            var providerType = Discovery.GetImplementationType(interfaceType, ProviderLayers.GetProviderInterfaceStack(), 0, throwError);
            if (providerType == null)
                return Task.CompletedTask;
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });

            var provider = Instantiator.GetSingleInstance(providerType);

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
                            var providerCache = Instantiator.GetSingleInstance($"{providerCacheType.FullName}_Bus.Call_Cache", () =>
                            {
                                var instance = Instantiator.CreateInstance(providerCacheType);
                                methodSetNextProvider.Invoke(instance, new object[] { callerProvider });
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
                if (queryClients.TryGetValue(interfaceType, out IQueryClient methodCaller))
                {
                    var result = methodCaller.Call<TReturn>(interfaceType, methodName, arguments);
                    return result;
                }
            }

            var provider = Resolver.Get<TInterface>();
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
                messageTypes.Add(messageType);
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
                messageTypes.Add(messageType);
            }
            return messageTypes;
        }

        private static readonly object serviceLock = new object();

        private static readonly ConcurrentDictionary<Type, ICommandClient> commandClients = new ConcurrentDictionary<Type, ICommandClient>();
        public static void AddCommandClient<TInterface>(ICommandClient commandClient) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                Type type = typeof(TInterface);
                var commandTypes = GetCommandTypesFromInterface(type);
                foreach (var commandType in commandTypes)
                {
                    if (commandServerTypes.Contains(commandType))
                        throw new InvalidOperationException($"Cannot create loopback. Command Server already registered for type {commandType.GetNiceName()}");
                    if (commandClients.ContainsKey(commandType))
                        throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                    commandClients.TryAdd(commandType, commandClient);
                    //_ = Log.InfoAsync($"{nameof(Bus)} Added Command Client For {commandType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentHashSet<ICommandServer> commandServers = new ConcurrentHashSet<ICommandServer>();
        private static readonly HashSet<Type> commandServerTypes = new HashSet<Type>();
        public static void AddCommandServer(ICommandServer commandServer)
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
                                if (commandClients.ContainsKey(type))
                                    throw new InvalidOperationException($"Cannot create loopback. Command Client already registered for type {type.GetNiceName()}");
                                if (!commandServerTypes.Contains(type))
                                    commandServerTypes.Add(type);
                                commandServer.RegisterCommandType(type);
                            }
                        }
                    }
                }

                commandServer.SetHandler(HandleRemoteCommandDispatchAsync, HandleRemoteCommandDispatchAwaitAsync);
                commandServers.Add(commandServer);
                commandServer.Open();
            }
        }

        private static readonly ConcurrentDictionary<Type, IEventClient> eventClients = new ConcurrentDictionary<Type, IEventClient>();
        public static void AddEventClient<TInterface>(IEventClient eventClient) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                Type type = typeof(TInterface);
                var eventTypes = GetEventTypesFromInterface(type);
                foreach (var eventType in eventTypes)
                {
                    if (eventServerTypes.Contains(type))
                        throw new InvalidOperationException($"Cannot create loopback. Event Server already registered for type {type.GetNiceName()}");
                    if (eventClients.ContainsKey(eventType))
                        throw new InvalidOperationException($"Event Client already registered for type {eventType.GetNiceName()}");
                    eventClients.TryAdd(eventType, eventClient);
                    _ = Log.InfoAsync($"{nameof(Bus)} Added Event Client For {eventType.GetNiceName()}");
                }
            }
        }

        private static readonly ConcurrentHashSet<IEventServer> eventServers = new ConcurrentHashSet<IEventServer>();
        private static readonly HashSet<Type> eventServerTypes = new HashSet<Type>();
        public static void AddEventServer(IEventServer eventServer)
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
                                if (eventClients.ContainsKey(type))
                                    throw new InvalidOperationException($"Cannot create loopback. Event Client already registered for type {type.GetNiceName()}");
                                if (!eventServerTypes.Contains(type))
                                    eventServerTypes.Add(type);
                                eventServer.RegisterEventType(type);
                            }
                        }
                    }
                }

                eventServer.SetHandler(HandleRemoteEventDispatchAsync);
                eventServers.Add(eventServer);
                eventServer.Open();
            }
        }

        private static readonly ConcurrentDictionary<Type, IQueryClient> queryClients = new ConcurrentDictionary<Type, IQueryClient>();
        public static void AddQueryClient<TInterface>(IQueryClient queryClient) where TInterface : IBaseProvider
        {
            lock (serviceLock)
            {
                Type interfaceType = typeof(TInterface);
                if (queryServerTypes.Contains(interfaceType))
                    throw new InvalidOperationException($"Cannot create loopback. Query Server already registered for type {interfaceType.GetNiceName()}");
                if (commandClients.ContainsKey(interfaceType))
                    throw new InvalidOperationException($"Query Client already registered for type {interfaceType.GetNiceName()}");
                queryClients.TryAdd(interfaceType, queryClient);
                _ = Log.InfoAsync($"{nameof(Bus)} Added Query Client For {interfaceType.GetNiceName()}");
            }
        }

        private static readonly ConcurrentHashSet<IQueryServer> queryServers = new ConcurrentHashSet<IQueryServer>();
        private static readonly HashSet<Type> queryServerTypes = new HashSet<Type>();
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
                                queryServerTypes.Add(type);
                            queryServer.RegisterInterfaceType(type);
                        }
                    }
                }

                queryServer.SetHandler(HandleRemoteQueryCallAsync);
                queryServers.Add(queryServer);
                queryServer.Open();
            }
        }

        private static readonly ConcurrentHashSet<IMessageLogger> messageLoggers = new ConcurrentHashSet<IMessageLogger>();
        public static void AddMessageLogger(IMessageLogger messageLogger)
        {
            lock (serviceLock)
            {
                messageLoggers.Add(messageLogger);
            }
        }

        private static readonly HashSet<object> instanciations = new HashSet<object>();

        public static void DisposeServices()
        {
            _ = Log.InfoAsync($"{nameof(Bus)} Shutting Down");
            lock (serviceLock)
            {
                var disposed = new HashSet<IDisposable>();

                foreach (var client in commandClients.Select(x => x.Value))
                {
                    if (client is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                commandClients.Clear();

                foreach (var server in commandServers)
                {
                    server.Close();
                    if (server is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                commandServers.Clear();

                foreach (var client in eventClients.Select(x => x.Value))
                {
                    if (client is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                commandClients.Clear();

                foreach (var server in eventServers)
                {
                    server.Close();
                    if (server is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                commandServers.Clear();

                foreach (var messageLogger in messageLoggers)
                {
                    if (messageLogger is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                messageLoggers.Clear();

                foreach (var client in queryClients.Select(x => x.Value))
                {
                    if (client is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                queryClients.Clear();

                foreach (var server in queryServers)
                {
                    server.Close();
                    if (server is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                queryClients.Clear();

                foreach (var instance in instanciations)
                {
                    if (instance is IDisposable disposable && !disposed.Contains(disposable))
                    {
                        disposable.Dispose();
                        disposed.Add(disposable);
                    }
                }
                instanciations.Clear();
            }
        }

        public static void WaitUntilExit()
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { waiter.Release(); };
            waiter.Wait();
            DisposeServices();
        }
        public static void WaitUntilExit(CancellationToken cancellationToken)
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { waiter.Release(); };
            try
            {
                waiter.Wait(cancellationToken);
            }
            catch { }
            DisposeServices();
        }

        public static async Task WaitUntilExitAsync()
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { waiter.Release(); };
            await waiter.WaitAsync();
            _ = Log.InfoAsync($"{nameof(Bus)} Exiting");
            DisposeServices();
        }
        public static async Task WaitUntilExitAsync(CancellationToken cancellationToken)
        {
            var waiter = new SemaphoreSlim(0, 1);
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => { waiter.Release(); };
            try
            {
                await waiter.WaitAsync(cancellationToken);
            }
            catch { }
            DisposeServices();
        }

        public static void StartServices(string serviceName, ServiceSettings serviceSettings, IServiceCreator serviceCreator, IRelayRegister relayRegister = null)
        {
            _ = Log.InfoAsync($"Starting {serviceName}");
            lock (serviceLock)
            {
                var serverSetting = serviceSettings.Services.FirstOrDefault(x => x.Name == serviceName);
                if (serverSetting == null)
                    throw new Exception($"Service {serviceName} not found in {CQRSSettings.SettingsFileName}");

                var serverUrl = Config.GetSetting("urls");
                if (String.IsNullOrWhiteSpace(serverUrl))
                    serverUrl = Config.GetSetting("ASPNETCORE_URLS");
                if (String.IsNullOrWhiteSpace(serverUrl))
                    serverUrl = Config.GetSetting("DOTNET_URLS");
                if (String.IsNullOrWhiteSpace(serverUrl))
                    serverUrl = serverSetting.ExternalUrl;

                ICommandServer commandServer = null;
                IEventServer eventServer = null;
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
                            serverTypes.Add(commandType);

                        var eventTypes = GetEventTypesFromInterface(type);
                        foreach (var eventType in eventTypes)
                            serverTypes.Add(eventType);

                        if (typeDetails.Attributes.Any(x => x is ServiceExposedAttribute))
                            serverTypes.Add(type);
                    }
                }

                foreach (var serviceSetting in serviceSettings.Services)
                {
                    if (serviceSetting.Types == null || serviceSetting.Types.Length == 0)
                        continue;

                    ICommandClient commandClient = null;
                    IEventClient eventClient = null;
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
                                if (commandServer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    commandServer = serviceCreator.CreateCommandServer(serverUrl, encryptionKey);
                                    commandServer.SetHandler(HandleRemoteCommandDispatchAsync, HandleRemoteCommandDispatchAwaitAsync);
                                    if (!commandServers.Contains(commandServer))
                                        commandServers.Add(commandServer);
                                }
                                foreach (var commandType in commandTypes)
                                {
                                    if (commandClients.ContainsKey(commandType))
                                        throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                                    if (commandServerTypes.Contains(commandType))
                                        throw new InvalidOperationException($"Command Server already registered for type {commandType.GetNiceName()}");
                                    commandServerTypes.Add(commandType);
                                    commandServer.RegisterCommandType(commandType);
                                }
                            }
                            else
                            {
                                if (commandClient == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    commandClient = serviceCreator.CreateCommandClient(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, encryptionKey);
                                }
                                var clientCommandTypes = commandTypes.Where(x => !serverTypes.Contains(x)).ToArray();
                                if (clientCommandTypes.Length > 0)
                                {
                                    foreach (var commandType in clientCommandTypes)
                                    {
                                        if (commandServerTypes.Contains(commandType))
                                            throw new InvalidOperationException($"Command Server already registered for type {commandType.GetNiceName()}");
                                        if (commandClients.ContainsKey(commandType))
                                            throw new InvalidOperationException($"Command Client already registered for type {commandType.GetNiceName()}");
                                        commandClients.TryAdd(commandType, commandClient);
                                    }
                                }
                                else
                                {
                                    if (commandClient is IDisposable disposable)
                                        disposable.Dispose();
                                }
                            }
                        }

                        var eventTypes = GetEventTypesFromInterface(type);
                        if (eventTypes.Count > 0)
                        {
                            if (serviceSetting == serverSetting)
                            {
                                if (eventServer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    eventServer = serviceCreator.CreateEventServer(serverUrl, encryptionKey);
                                    eventServer.SetHandler(HandleRemoteEventDispatchAsync);
                                    if (!eventServers.Contains(eventServer))
                                        eventServers.Add(eventServer);
                                }
                                foreach (var eventType in eventTypes)
                                {
                                    if (eventClients.ContainsKey(eventType))
                                        throw new InvalidOperationException($"Event Client already registered for type {eventType.GetNiceName()}");
                                    if (eventServerTypes.Contains(eventType))
                                        throw new InvalidOperationException($"Event Server already registered for type {eventType.GetNiceName()}");
                                    eventServerTypes.Add(eventType);
                                    eventServer.RegisterEventType(eventType);
                                }
                            }
                            else
                            {
                                if (eventClient == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    eventClient = serviceCreator.CreateEventClient(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, encryptionKey);
                                }
                                var clientEventTypes = eventTypes.Where(x => !serverTypes.Contains(x)).ToArray();
                                if (clientEventTypes.Length > 0)
                                {
                                    foreach (var eventType in eventTypes)
                                    {
                                        if (eventServerTypes.Contains(eventType))
                                            throw new InvalidOperationException($"Event Server already registered for type {eventType.GetNiceName()}");
                                        if (!eventClients.ContainsKey(eventType))
                                        {
                                            eventClients.TryAdd(eventType, eventClient);
                                        }
                                    }
                                }
                                else
                                {
                                    if (eventClient is IDisposable disposable)
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
                                    queryServer = serviceCreator.CreateQueryServer(serverUrl, encryptionKey);
                                    queryServer.SetHandler(HandleRemoteQueryCallAsync);
                                    if (!queryServers.Contains(queryServer))
                                        queryServers.Add(queryServer);
                                }
                                if (queryClients.ContainsKey(type))
                                    throw new InvalidOperationException($"Query Client already registered for type {type.GetNiceName()}");
                                if (queryServerTypes.Contains(type))
                                    throw new InvalidOperationException($"Query Server already registered for type {type.GetNiceName()}");
                                queryServerTypes.Add(type);
                                queryServer.RegisterInterfaceType(type);
                            }
                            else
                            {
                                if (queryClient == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    queryClient = serviceCreator.CreateQueryClient(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, encryptionKey);
                                }
                                if (!serverTypes.Contains(type))
                                {
                                    if (queryServerTypes.Contains(type))
                                        throw new InvalidOperationException($"Query Server already registered for type {type.GetNiceName()}");
                                    if (commandClients.ContainsKey(type))
                                        throw new InvalidOperationException($"Query Client already registered for type {type.GetNiceName()}");
                                    queryClients.TryAdd(type, queryClient);
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

                if (commandServer != null)
                {
                    commandServer.Open();
                    if (relayRegister != null)
                    {
                        var commandTypes = commandServer.GetCommandTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(commandServer.ConnectionString, out HashSet<string> relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(commandServer.ConnectionString, relayTypes);
                        }
                        foreach (var type in commandTypes)
                            relayTypes.Add(type);
                    }
                }
                if (eventServer != null)
                {
                    eventServer.Open();
                    if (relayRegister != null)
                    {
                        var eventTypes = eventServer.GetEventTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(eventServer.ConnectionString, out HashSet<string> relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(eventServer.ConnectionString, relayTypes);
                        }
                        foreach (var type in eventTypes)
                            relayTypes.Add(type);
                    }
                }
                if (queryServer != null)
                {
                    queryServer.Open();
                    if (relayRegister != null)
                    {
                        var queryTypes = queryServer.GetInterfaceTypes().Select(x => x.GetNiceName()).ToArray();
                        if (!relayRegisterTypes.TryGetValue(queryServer.ConnectionString, out HashSet<string> relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(queryServer.ConnectionString, relayTypes);
                        }
                        foreach (var type in queryTypes)
                            relayTypes.Add(type);
                    }
                }

                if (relayRegister != null)
                {
                    foreach (var group in relayRegisterTypes)
                        relayRegister.Register(group.Key, group.Value.ToArray());
                }

                if (serverSetting.InstantiateTypes != null && serverSetting.InstantiateTypes.Length > 0)
                {
                    foreach (var instantiation in serverSetting.InstantiateTypes)
                    {
                        try
                        {
                            var type = Discovery.GetTypeFromName(instantiation);
                            var instance = Activator.CreateInstance(type);
                            instanciations.Add(instance);
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