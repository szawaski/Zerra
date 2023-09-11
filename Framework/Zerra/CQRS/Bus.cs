// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.CQRS.Relay;
using Zerra.CQRS.Settings;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Serialization;

#pragma warning disable IDE1006 // Naming Styles

namespace Zerra.CQRS
{
    public static class Bus
    {
        private static readonly Type iCommandType = typeof(ICommand);
        private static readonly Type iEventType = typeof(IEvent);
        private static readonly Type iCommandHandlerType = typeof(ICommandHandler<>);
        private static readonly Type iEventHandlerType = typeof(IEventHandler<>);
        private static readonly Type iBusCacheType = typeof(IBusCache);
        private static readonly Type streamType = typeof(Stream);

        public static async Task<RemoteQueryCallResponse> HandleRemoteQueryCallAsync(Type interfaceType, string methodName, string[] arguments, string source, bool isApi)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            var callerProvider = CallInternal(interfaceType, networkType, source);

            var methodDetail = TypeAnalyzer.GetMethodDetail(interfaceType, methodName);

            if (methodDetail.ParametersInfo.Count != (arguments != null ? arguments.Length : 0))
                throw new ArgumentException("Invalid number of arguments for this method");

            var args = new object[arguments != null ? arguments.Length : 0];
            if (arguments != null && arguments.Length > 0)
            {
                var i = 0;
                foreach (var argument in arguments)
                {
                    var parameter = JsonSerializer.Deserialize(methodDetail.ParametersInfo[i].ParameterType, argument);
                    args[i] = parameter;
                    i++;
                }
            }

            bool isStream;
            object model;
            if (methodDetail.ReturnType.IsTask)
            {
                isStream = methodDetail.ReturnType.Type.IsGenericType && methodDetail.ReturnType.InnerTypeDetails[0].BaseTypes.Contains(streamType);
                var result = (Task)methodDetail.Caller(callerProvider, args);
                await result;

                if (methodDetail.ReturnType.Type.IsGenericType)
                    model = methodDetail.ReturnType.TaskResultGetter(result);
                else
                    model = null;
            }
            else
            {
                isStream = methodDetail.ReturnType.BaseTypes.Contains(streamType);
                model = methodDetail.Caller(callerProvider, args);
            }

            if (isStream)
                return new RemoteQueryCallResponse((Stream)model);
            else
                return new RemoteQueryCallResponse(model);
        }
        public static Task HandleRemoteCommandDispatchAsync(ICommand command, string source, bool isApi)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            return DispatchCommandInternalAsync(command, false, networkType, source);
        }
        public static Task HandleRemoteCommandDispatchAwaitAsync(ICommand command, string source, bool isApi)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            return DispatchCommandInternalAsync(command, true, networkType, source);
        }
        public static Task HandleRemoteEventDispatchAsync(IEvent @event, string source, bool isApi)
        {
            var networkType = isApi ? NetworkType.Api : NetworkType.Internal;
            return DispatchEventInternalAsync(@event, networkType, source);
        }

        public static Task DispatchAsync(ICommand command) => DispatchCommandInternalAsync(command, false, NetworkType.Local, Config.ApplicationIdentifier);
        public static Task DispatchAwaitAsync(ICommand command) => DispatchCommandInternalAsync(command, true, NetworkType.Local, Config.ApplicationIdentifier);
        public static Task DispatchAsync(IEvent @event) => DispatchEventInternalAsync(@event, NetworkType.Local, Config.ApplicationIdentifier);

        private static readonly ConcurrentFactoryDictionary<Type, Func<ICommand, Task>> commandCacheProviders = new();
        private static Task DispatchCommandInternalAsync(ICommand command, bool requireAffirmation, NetworkType networkType, string source)
        {
            var commandType = command.GetType();
            var busLogging = BusLogging.Logged;
            if (networkType != NetworkType.Local)
            {
                var exposed = false;
                foreach (var attribute in commandType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute && serviceExposedAttribute.NetworkType >= networkType)
                    {
                        exposed = true;
                        break;
                    }
                    else if (attribute is BusLoggedAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                }
                if (!exposed)
                    throw new Exception($"Command {commandType.GetNiceName()} is not exposed for {nameof(NetworkType)}.{networkType.EnumName()}");
            }

            var cacheProviderDispatchAsync = commandCacheProviders.GetOrAdd(commandType, (t) =>
            {
                var handlerTypeDetail = TypeAnalyzer.GetGenericTypeDetail(iCommandHandlerType, commandType);

                var busCacheType = Discovery.GetImplementationType(handlerTypeDetail.Type, iBusCacheType, false);
                if (busCacheType == null)
                    return null;

                var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.SetNextProvider));
                if (methodSetNextProvider == null)
                    return null;

                var cacheInstance = Instantiator.Create(busCacheType);

                var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.GetProviderInterfaceType));
                var interfaceType = (Type)methodGetProviderInterfaceType.Caller(cacheInstance, null);

                var messageHandlerToDispatchProvider = BusRouters.GetCommandHandlerToDispatchInternalInstance(interfaceType, requireAffirmation, networkType, source, busLogging);
                _ = methodSetNextProvider.Caller(cacheInstance, new object[] { messageHandlerToDispatchProvider });

                var method = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { commandType });
                Task caller(ICommand arg)
                {
                    var task = (Task)method.Caller(cacheInstance, new object[] { arg });
                    return task;
                }

                return caller;
            });

            if (cacheProviderDispatchAsync != null)
                return cacheProviderDispatchAsync(command);

            return _DispatchCommandInternalAsync(command, commandType, requireAffirmation, networkType, source, busLogging);
        }

        private static readonly ConcurrentFactoryDictionary<Type, Func<IEvent, Task>> eventCacheProviders = new();
        private static Task DispatchEventInternalAsync(IEvent @event, NetworkType networkType, string source)
        {
            var eventType = @event.GetType();
            var busLogging = BusLogging.Logged;
            if (networkType != NetworkType.Local)
            {
                var exposed = false;
                foreach (var attribute in eventType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute && serviceExposedAttribute.NetworkType >= networkType)
                    {
                        exposed = true;
                        break;
                    }
                    else if (attribute is BusLoggedAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                }
                if (!exposed)
                    throw new Exception($"Event {eventType.GetNiceName()} is not exposed for {nameof(NetworkType)}.{networkType.EnumName()}");
            }

            var cacheProviderDispatchAsync = eventCacheProviders.GetOrAdd(eventType, (t) =>
            {
                var handlerTypeDetail = TypeAnalyzer.GetGenericTypeDetail(iEventHandlerType, eventType);

                var busCacheType = Discovery.GetImplementationType(handlerTypeDetail.Type, iBusCacheType, false);
                if (busCacheType == null)
                    return null;

                var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.SetNextProvider));
                if (methodSetNextProvider == null)
                    return null;

                var cacheInstance = Instantiator.Create(busCacheType);

                var methodGetProviderInterfaceType = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.GetProviderInterfaceType));
                var interfaceType = (Type)methodGetProviderInterfaceType.Caller(cacheInstance, null);

                var messageHandlerToDispatchProvider = BusRouters.GetEventHandlerToDispatchInternalInstance(interfaceType, networkType, source, busLogging);
                _ = methodSetNextProvider.Caller(cacheInstance, new object[] { messageHandlerToDispatchProvider });

                var method = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });
                Task caller(IEvent arg)
                {
                    var task = (Task)method.Caller(cacheInstance, new object[] { arg });
                    return task;
                }

                return caller;
            });

            if (cacheProviderDispatchAsync != null)
                return cacheProviderDispatchAsync(@event);

            return _DispatchEventInternalAsync(@event, eventType, networkType, source, busLogging);
        }

        public static Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, NetworkType networkType, string source, BusLogging busLogging)
        {
            if (networkType == NetworkType.Local || !commandConsumerTypes.Contains(commandType))
            {
                ICommandProducer producer = null;
                var messageBaseType = commandType;
                while (producer == null && messageBaseType != null)
                {
                    if (commandProducers.TryGetValue(messageBaseType, out producer))
                    {
                        if (busLogger != null)
                        {
                            return SendCommandLoggedAsync(command, commandType, requireAffirmation, networkType, source, producer);
                        }
                        else
                        {
                            if (requireAffirmation)
                                return producer.DispatchAsyncAwait(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                            else
                                return producer.DispatchAsync(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                        }
                    }
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            if (requireAffirmation || networkType != NetworkType.Local)
            {
                if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                    return HandleCommandAsync((ICommand)command, commandType, source);
                else
                    return HandleCommandLoggedAsync((ICommand)command, commandType, source);
            }
            else
            {
                var principal = Thread.CurrentPrincipal.CloneClaimsPrincipal();
                return Task.Run(() =>
                {
                    Thread.CurrentPrincipal = principal;
                    if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                        _ = HandleCommandAsync((ICommand)command, commandType, source);
                    else
                        _ = HandleCommandLoggedAsync((ICommand)command, commandType, source);
                });
            }
        }
        public static Task _DispatchEventInternalAsync(IEvent @event, Type eventType, NetworkType networkType, string source, BusLogging busLogging)
        {
            if (networkType == NetworkType.Local || !eventConsumerTypes.Contains(eventType))
            {
                IEventProducer producer = null;
                var messageBaseType = eventType;
                while (producer == null && messageBaseType != null)
                {
                    if (eventProducers.TryGetValue(messageBaseType, out producer))
                    {
                        if (busLogger != null)
                            return SendEventLoggedAsync(@event, eventType, networkType, source, producer);
                        else
                            return producer.DispatchAsync(@event, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                    }
                    messageBaseType = messageBaseType.BaseType;
                }
            }

            if (networkType != NetworkType.Local)
            {
                if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                    return HandleEventAsync((IEvent)@event, eventType, source);
                else
                    return HandleEventLoggedAsync((IEvent)@event, eventType, source);
            }
            else
            {
                var principal = Thread.CurrentPrincipal.CloneClaimsPrincipal();
                return Task.Run(() =>
                {
                    Thread.CurrentPrincipal = principal;
                    if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                        _ = HandleEventAsync((IEvent)@event, eventType, source);
                    else
                        _ = HandleEventLoggedAsync((IEvent)@event, eventType, source);

                });
            }
        }

        private static async Task SendCommandLoggedAsync(ICommand command, Type commandType, bool requireAffirmation, NetworkType networkType, string source, ICommandProducer producer)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                if (requireAffirmation)
                {
                    await producer.DispatchAsyncAwait(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                }
                else
                {
                    await producer.DispatchAsync(command, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCommandAsync(commandType, command, source, false, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCommandAsync(commandType, command, source, false, timer.ElapsedMilliseconds, null);
        }
        private static async Task SendEventLoggedAsync(IEvent @event, Type eventType, NetworkType networkType, string source, IEventProducer producer)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                await producer.DispatchAsync(@event, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogEventAsync(eventType, @event, source, false, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogEventAsync(eventType, @event, source, false, timer.ElapsedMilliseconds, null);
        }

        private static Task HandleCommandAsync(ICommand command, Type commandType, string source)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);

            var providerType = ProviderResolver.GetFirstType(interfaceType);
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { commandType });

            var provider = Instantiator.GetSingle(providerType);

            return (Task)method.Caller(provider, new object[] { command });
        }
        private static async Task HandleCommandLoggedAsync(ICommand command, Type commandType, string source)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iCommandHandlerType, commandType);

            var providerType = ProviderResolver.GetFirstType(interfaceType);
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(ICommandHandler<ICommand>.Handle), new Type[] { commandType });

            var provider = Instantiator.GetSingle(providerType);

            var timer = Stopwatch.StartNew();
            try
            {
                await (Task)method.Caller(provider, new object[] { command });
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCommandAsync(commandType, command, source, true, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCommandAsync(commandType, command, source, true, timer.ElapsedMilliseconds, null);
        }
        private static Task HandleEventAsync(IEvent @event, Type eventType, string source)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);

            var providerType = ProviderResolver.GetFirstType(interfaceType);
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });

            var provider = Instantiator.GetSingle(providerType);

            return (Task)method.Caller(provider, new object[] { @event });
        }
        private static async Task HandleEventLoggedAsync(IEvent @event, Type eventType, string source)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iEventHandlerType, eventType);

            var providerType = ProviderResolver.GetFirstType(interfaceType);
            if (providerType == null)
                return;
            var method = TypeAnalyzer.GetMethodDetail(providerType, nameof(IEventHandler<IEvent>.Handle), new Type[] { eventType });

            var provider = Instantiator.GetSingle(providerType);

            var timer = Stopwatch.StartNew();
            try
            {
                await (Task)method.Caller(provider, new object[] { @event });
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogEventAsync(eventType, @event, source, true, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogEventAsync(eventType, @event, source, true, timer.ElapsedMilliseconds, null);
        }

        public static TInterface Call<TInterface>() => (TInterface)CallInternal(typeof(TInterface), NetworkType.Local, Config.ApplicationIdentifier);
        public static object Call(Type interfaceType) => CallInternal(interfaceType, NetworkType.Local, Config.ApplicationIdentifier);

        private static readonly ConcurrentFactoryDictionary<Type, object> callCacheProviders = new();
        private static object CallInternal(Type interfaceType, NetworkType networkType, string source)
        {
            if (networkType != NetworkType.Local)
            {
                var exposed = interfaceType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute);
                if (!exposed)
                    throw new Exception($"Interface {interfaceType.GetNiceName()} is not exposed");
            }

            var callerProvider = BusRouters.GetProviderToCallMethodInternalInstance(interfaceType, networkType, source);

            var cacheCallProvider = callCacheProviders.GetOrAdd(interfaceType, (t) =>
            {
                var busCacheType = Discovery.GetImplementationType(interfaceType, iBusCacheType, false);
                if (busCacheType == null)
                    return null;

                var methodSetNextProvider = TypeAnalyzer.GetMethodDetail(busCacheType, nameof(LayerProvider<object>.SetNextProvider)).MethodInfo;
                if (methodSetNextProvider == null)
                    return null;

                var cacheInstance = Instantiator.Create(busCacheType);
                _ = methodSetNextProvider.Invoke(cacheInstance, new object[] { callerProvider });

                return cacheInstance;
            });

            if (cacheCallProvider != null)
                return cacheCallProvider;

            return callerProvider;
        }

        public static TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, string source)
        {
            var methodDetail = TypeAnalyzer.GetMethodDetail(interfaceType, methodName);
            if (methodDetail.ParametersInfo.Count != (arguments != null ? arguments.Length : 0))
                throw new ArgumentException("Invalid number of arguments for this method");

            var busLogging = BusLogging.Logged;
            if (networkType != NetworkType.Local)
            {
                var exposed = false;
                var blockedMethod = false;
                foreach (var attribute in interfaceType.GetTypeDetail().Attributes)
                {
                    if (attribute is ServiceExposedAttribute serviceExposedAttribute && serviceExposedAttribute.NetworkType >= networkType)
                    {
                        exposed = true;
                        break;
                    }
                    else if (attribute is BusLoggedAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                }
                if (!exposed)
                    throw new Exception($"Interface {interfaceType.GetNiceName()} is not exposed for {nameof(NetworkType)}.{networkType.EnumName()}");

                foreach (var attribute in methodDetail.Attributes)
                {
                    if (attribute is ServiceBlockedAttribute serviceBlockedAttribute && serviceBlockedAttribute.NetworkType < networkType)
                    {
                        blockedMethod = true;
                        break;
                    }
                    else if (attribute is BusLoggedAttribute busLoggedAttribute)
                    {
                        busLogging = busLoggedAttribute.BusLogging;
                    }
                }
                if (blockedMethod)
                    throw new Exception($"Method {interfaceType.GetNiceName()}.{methodDetail.Name} is blocked for {nameof(NetworkType)}.{networkType.EnumName()}");
            }

            object result;

            if (!queryClients.IsEmpty && queryClients.TryGetValue(interfaceType, out var methodCaller))
            {
                if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                {
                    result = methodCaller.Call<TReturn>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                }
                else if (methodDetail.ReturnType.IsTask)
                {
                    if (methodDetail.ReturnType.Type.IsGenericType)
                    {
                        var method = sendMethodLoggedGenericAsyncMethod.GetGenericMethodDetail(methodDetail.ReturnType.InnerTypes[0]);
                        result = method.Caller(null, new object[] { interfaceType, methodName, arguments, networkType, source, methodDetail, methodCaller });
                    }
                    else
                    {
                        result = SendMethodLoggedAsync<TReturn>(interfaceType, methodName, arguments, networkType, source, methodDetail, methodCaller);
                    }
                }
                else
                {
                    var provider = ProviderResolver.GetFirst(interfaceType);

                    var timer = Stopwatch.StartNew();
                    try
                    {
                        result = methodCaller.Call<TReturn>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                    }
                    catch (Exception ex)
                    {
                        timer.Stop();
                        _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, false, timer.ElapsedMilliseconds, ex);
                        throw;
                    }

                    timer.Stop();
                    _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, result, source, false, timer.ElapsedMilliseconds, null);
                }
            }
            else
            {
                if (busLogger == null || busLogging == BusLogging.None || (busLogging == BusLogging.HandlerOnly && networkType != NetworkType.Local))
                {
                    var provider = ProviderResolver.GetFirst(interfaceType);

                    result = methodDetail.Caller(provider, arguments);
                }
                else if (methodDetail.ReturnType.IsTask)
                {
                    if (methodDetail.ReturnType.Type.IsGenericType)
                    {
                        var method = callInternalLoggedGenericAsyncMethod.GetGenericMethodDetail(methodDetail.ReturnType.InnerTypes[0]);
                        result = method.Caller(null, new object[] { interfaceType, methodName, arguments, source, methodDetail });
                    }
                    else
                    {
                        result = CallMethodInternalLoggedAsync(interfaceType, methodName, arguments, source, methodDetail);
                    }
                }
                else
                {
                    var provider = ProviderResolver.GetFirst(interfaceType);

                    var timer = Stopwatch.StartNew();
                    try
                    {
                        result = methodDetail.Caller(provider, arguments);
                    }
                    catch (Exception ex)
                    {
                        timer.Stop();
                        _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, true, timer.ElapsedMilliseconds, ex);
                        throw;
                    }

                    timer.Stop();
                    _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, result, source, true, timer.ElapsedMilliseconds, null);
                }
            }

            return (TReturn)result;
        }

        private static readonly MethodDetail sendMethodLoggedGenericAsyncMethod = typeof(Bus).GetMethodDetail(nameof(SendMethodLoggedGenericAsync));
        private static async Task<TReturn> SendMethodLoggedGenericAsync<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, string source, MethodDetail methodDetail, IQueryClient methodCaller)
        {
            object taskresult;
            var timer = Stopwatch.StartNew();
            try
            {
                var localresult = methodCaller.Call<Task<TReturn>>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                var task = localresult;
                await task;
                taskresult = methodDetail.ReturnType.TaskResultGetter(task);
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, false, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, taskresult, source, false, timer.ElapsedMilliseconds, null);

            return (TReturn)taskresult;
        }
        private static async Task SendMethodLoggedAsync<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, string source, MethodDetail methodDetail, IQueryClient methodCaller)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                var localresult = methodCaller.Call<Task>(interfaceType, methodName, arguments, networkType != NetworkType.Local ? $"{source} - {Config.ApplicationIdentifier}" : source);
                var task = localresult;
                await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, false, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, false, timer.ElapsedMilliseconds, null);
        }

        private static readonly MethodDetail callInternalLoggedGenericAsyncMethod = typeof(Bus).GetMethodDetail(nameof(CallMethodInternalLoggedGenericAsync));
        private static async Task<TReturn> CallMethodInternalLoggedGenericAsync<TReturn>(Type interfaceType, string methodName, object[] arguments, string source, MethodDetail methodDetail)
        {
            var providerType = ProviderResolver.GetFirstType(interfaceType);
            var provider = Instantiator.GetSingle(providerType);

            object taskresult;
            var timer = Stopwatch.StartNew();
            try
            {
                var localresult = methodDetail.Caller(provider, arguments);
                var task = (Task)localresult;
                await task;
                taskresult = methodDetail.ReturnType.TaskResultGetter(localresult);
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, true, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, taskresult, source, true, timer.ElapsedMilliseconds, null);

            return (TReturn)taskresult;
        }
        private static async Task CallMethodInternalLoggedAsync(Type interfaceType, string methodName, object[] arguments, string source, MethodDetail methodDetail)
        {
            var providerType = ProviderResolver.GetFirstType(interfaceType);
            var provider = Instantiator.GetSingle(providerType);

            var timer = Stopwatch.StartNew();
            try
            {
                var localresult = methodDetail.Caller(provider, arguments);
                var task = (Task)localresult;
                await task;
            }
            catch (Exception ex)
            {
                timer.Stop();
                _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, true, timer.ElapsedMilliseconds, ex);
                throw;
            }

            timer.Stop();
            _ = busLogger?.LogCallAsync(interfaceType, methodName, arguments, null, source, true, timer.ElapsedMilliseconds, null);
        }

        public static ICollection<Type> GetCommandTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(interfaceType);
            foreach (var item in typeDetail.Interfaces.Where(x => x.Name == iCommandHandlerType.Name))
            {
                var itemDetail = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetail.InnerTypes[0];
                _ = messageTypes.Add(messageType);
            }
            return messageTypes;
        }
        public static ICollection<Type> GetEventTypesFromInterface(Type interfaceType)
        {
            var messageTypes = new HashSet<Type>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(interfaceType);
            foreach (var item in typeDetail.Interfaces.Where(x => x.Name == iEventHandlerType.Name))
            {
                var itemDetail = TypeAnalyzer.GetTypeDetail(item);
                var messageType = itemDetail.InnerTypes[0];
                _ = messageTypes.Add(messageType);
            }
            return messageTypes;
        }

        private static readonly object serviceLock = new();
        private static readonly SemaphoreSlim asyncServiceLock = new(1, 1);

        private static readonly ConcurrentDictionary<Type, ICommandProducer> commandProducers = new();
        public static void AddCommandProducer<TInterface>(ICommandProducer commandProducer)
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
                            var hasHandler = ProviderResolver.HasBase(TypeAnalyzer.GetGenericType(typeof(ICommandHandler<>), commandType));
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
        public static void AddEventProducer<TInterface>(IEventProducer eventProducer)
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
                            var hasHandler = ProviderResolver.HasBase(TypeAnalyzer.GetGenericType(typeof(IEventHandler<>), eventType));
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
        public static void AddQueryClient<TInterface>(IQueryClient queryClient)
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
                    if (interfaceType.IsInterface)
                    {
                        var hasImplementation = ProviderResolver.HasBase(interfaceType);
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

        private static IBusLogger busLogger = null;
        public static void AddLogger(IBusLogger busLogger)
        {
            lock (serviceLock)
            {
                if (Bus.busLogger != null)
                    throw new InvalidOperationException("Bus already has a logger");
                Bus.busLogger = busLogger;
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

                if (busLogger != null)
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

                        var commandTypes = GetCommandTypesFromInterface(type);
                        foreach (var commandType in commandTypes)
                            _ = serverTypes.Add(commandType);

                        var eventTypes = GetEventTypesFromInterface(type);
                        foreach (var eventType in eventTypes)
                            _ = serverTypes.Add(eventType);

                        var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                        if (typeDetail.Attributes.Any(x => x is ServiceExposedAttribute))
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

                        var commandTypes = GetCommandTypesFromInterface(interfaceType);
                        if (commandTypes.Count > 0)
                        {
                            if (serviceSetting == serverSetting)
                            {
                                if (commandConsumer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
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
                                        var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
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
                                    var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
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
                                var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
                                eventProducer = serviceCreator.CreateEventProducer(relayRegister?.RelayUrl ?? serviceSetting.ExternalUrl, symmetricConfig);
                            }

                            foreach (var eventType in eventTypes)
                            {
                                if (!eventType.GetTypeDetail().Attributes.Any(x => x is ServiceExposedAttribute))
                                    continue;
                                //multiple services handle events
                                //TODO do we need different encryption keys for events, commands, and queries??
                                //if (eventProducers.ContainsKey(eventType))
                                //    throw new InvalidOperationException($"Cannot add Event Producer: Event Producer already registered for type {eventType.GetNiceName()}");
                                _ = eventProducers.TryAdd(eventType, eventProducer);
                            }
                        }

                        var interfaceTypeDetail = TypeAnalyzer.GetTypeDetail(interfaceType);
                        if (interfaceTypeDetail.Attributes.Any(x => x is ServiceExposedAttribute))
                        {
                            if (serviceSetting == serverSetting)
                            {
                                if (queryServer == null)
                                {
                                    var encryptionKey = String.IsNullOrWhiteSpace(serviceSetting.EncryptionKey) ? null : SymmetricEncryptor.GetKey(serviceSetting.EncryptionKey);
                                    var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
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
                                    var symmetricConfig = encryptionKey == null ? null : new SymmetricConfig(encryptionAlgoritm, encryptionKey);
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
                        if (!relayRegisterTypes.TryGetValue(commandConsumer.ServiceUrl, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(commandConsumer.ServiceUrl, relayTypes);
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
                        if (!relayRegisterTypes.TryGetValue(eventConsumer.ServiceUrl, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(eventConsumer.ServiceUrl, relayTypes);
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
                        if (!relayRegisterTypes.TryGetValue(queryServer.ServiceUrl, out var relayTypes))
                        {
                            relayTypes = new HashSet<string>();
                            relayRegisterTypes.Add(queryServer.ServiceUrl, relayTypes);
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