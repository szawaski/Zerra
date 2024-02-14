// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS.Relay;
using Zerra.CQRS.Settings;

namespace Zerra.CQRS
{
    public static partial class Bus
    {
        private static BusInstance instance = new();
        public static BusInstance Instance { get => instance; set => instance = value; }

        public static Task<RemoteQueryCallResponse> HandleRemoteQueryCallAsync(Type interfaceType, string methodName, string[] arguments, string source, bool isApi) => instance.HandleRemoteQueryCallAsync(interfaceType, methodName, arguments, source, isApi);
        public static Task HandleRemoteCommandDispatchAsync(ICommand command, string source, bool isApi) => instance.HandleRemoteCommandDispatchAsync(command, source, isApi);
        public static Task HandleRemoteCommandDispatchAwaitAsync(ICommand command, string source, bool isApi) => instance.HandleRemoteCommandDispatchAwaitAsync(command, source, isApi);
        public static Task HandleRemoteEventDispatchAsync(IEvent @event, string source, bool isApi) => instance.HandleRemoteEventDispatchAsync(@event, source, isApi);

        public static Task DispatchAsync(ICommand command) => instance.DispatchAsync(command);
        public static Task DispatchAwaitAsync(ICommand command) => instance.DispatchAwaitAsync(command);
        public static Task DispatchAsync(IEvent @event) => instance.DispatchAsync(@event);

        public static TInterface Call<TInterface>() => instance.Call<TInterface>();
        public static object Call(Type interfaceType) => instance.Call(interfaceType);

        public static void AddCommandProducer<TInterface>(ICommandProducer commandProducer) => instance.AddCommandProducer<TInterface>(commandProducer);
        public static void AddCommandConsumer(ICommandConsumer commandConsumer) => instance.AddCommandConsumer(commandConsumer);
        public static void AddEventProducer<TInterface>(IEventProducer eventProducer) => instance.AddEventProducer<TInterface>(eventProducer);
        public static void AddEventConsumer(IEventConsumer eventConsumer) => instance.AddEventConsumer(eventConsumer);
        public static void AddQueryClient<TInterface>(IQueryClient queryClient) => instance.AddQueryClient<TInterface>(queryClient);
        public static void AddQueryServer(IQueryServer queryServer) => instance.AddQueryServer(queryServer);
        public static void AddLogger(IBusLogger busLogger) => instance.AddLogger(busLogger);

        public static int MaxConcurrentQueries { get => instance.MaxConcurrentQueries; set => instance.MaxConcurrentQueries = value; }
        public static int MaxConcurrentCommandsPerTopic { get => instance.MaxConcurrentCommandsPerTopic; set => instance.MaxConcurrentCommandsPerTopic = value; }
        public static int MaxConcurrentEventsPerTopic { get => instance.MaxConcurrentEventsPerTopic; set => instance.MaxConcurrentEventsPerTopic = value; }
        public static int? ReceiveCommandsBeforeExit { get => instance.ReceiveCommandsBeforeExit; set => instance.ReceiveCommandsBeforeExit = value; }

        public static void StartServices(ServiceSettings serviceSettings, IServiceCreator serviceCreator, IRelayRegister? relayRegister = null) => instance.StartServices(serviceSettings, serviceCreator, relayRegister);

        public static void StopServices() => instance.StopServices();
        public static Task StopServicesAsync() => instance.StopServicesAsync();

        public static void WaitForExit() => instance.WaitForExit();
        public static void WaitForExit(CancellationToken cancellationToken) => instance.WaitForExit(cancellationToken);

        public static Task WaitForExitAsync() => instance.WaitForExitAsync();
        public static Task WaitForExitAsync(CancellationToken cancellationToken) => instance.WaitForExitAsync(cancellationToken);
    }
}