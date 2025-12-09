// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public sealed partial class Bus
    {
        private static IBusSetup? staticBus;
        private static IBusSetup StaticBus => staticBus ?? throw new InvalidOperationException("Bus not initialized. Call Bus.New to initialize.");

        /// <summary>
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the command.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAsync(command, cancellationToken);
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchAwaitTimeout"/>.</param>
        /// <returns>A task to await processing of the command.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAwaitAsync(command, cancellationToken);
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the event.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAsync(@event, cancellationToken);
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchAwaitTimeout"/>.</param>
        /// <returns>A task to await the result of the command.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAwaitAsync<TResult>(command, cancellationToken);

        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <returns>An instance of the interface to route queries.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static TInterface Call<TInterface>()
            => StaticBus.Call<TInterface>();

        /// <summary>
        /// Manually add a command producer service to send commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host commands with the <see cref="ServiceExposedAttribute"/>.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to send.</typeparam>
        /// <param name="commandProducer">The command producer service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddCommandProducer<TInterface>(ICommandProducer commandProducer)
            => StaticBus.AddCommandProducer<TInterface>(commandProducer);

        /// <summary>
        /// Manually add a command consumer service to receive commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to receive.</typeparam>
        /// <param name="commandConsumer">The command consumer service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer)
            => StaticBus.AddCommandConsumer<TInterface>(commandConsumer);

        /// <summary>
        /// Manually add an event producer service to send commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host events with the <see cref="ServiceExposedAttribute"/>.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to send.</typeparam>
        /// <param name="eventProducer">The event producer service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddEventProducer<TInterface>(IEventProducer eventProducer)
            => StaticBus.AddEventProducer<TInterface>(eventProducer);

        /// <summary>
        /// Manually add an event consumer service to receive commands.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to receive.</typeparam>
        /// <param name="eventConsumer">The event consumer service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddEventConsumer<TInterface>(IEventConsumer eventConsumer)
            => StaticBus.AddEventConsumer<TInterface>(eventConsumer);

        /// <summary>
        /// Manually add a query client service to call for queries.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryClient">The query client service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddQueryClient<TInterface>(IQueryClient queryClient)
            => StaticBus.AddQueryClient<TInterface>(queryClient);

        /// <summary>
        /// Manually add a query server to receive and response to queries.
        /// It's recommended to use <see cref="StartServices"/> instead unless there is some special case.
        /// Discovery will host implementations with the <see cref="ServiceExposedAttribute"/> on the interface.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryServer">The query server service.</param>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static void AddQueryServer<TInterface>(IQueryServer queryServer)
            => StaticBus.AddQueryServer<TInterface>(queryServer);
    }
}