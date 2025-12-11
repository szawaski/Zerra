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
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the command.</returns>
        public static Task DispatchAsync(ICommand command, TimeSpan timeout)
            => StaticBus.DispatchAsync(command, timeout);
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to await processing of the command.</returns>
        public static Task DispatchAwaitAsync(ICommand command, TimeSpan timeout)
            => StaticBus.DispatchAwaitAsync(command, timeout);
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the event.</returns>
        public static Task DispatchAsync(IEvent @event, TimeSpan timeout)
            => StaticBus.DispatchAsync(@event, timeout);
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="timeout">The time to wait before a cancellation request. Use <see cref="Timeout.InfiniteTimeSpan"/> for no timeout. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to await the result of the command.</returns>
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, TimeSpan timeout)
            => StaticBus.DispatchAwaitAsync<TResult>(command, timeout);

        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <returns>An instance of the interface to route queries.</returns>
        [Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static TInterface Call<TInterface>()
            => StaticBus.Call<TInterface>();
    }
}