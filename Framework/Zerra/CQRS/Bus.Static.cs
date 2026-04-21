// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public sealed partial class Bus
    {
        private static IBusSetup? staticBus;
        private static IBusSetup StaticBus => staticBus ?? throw new InvalidOperationException("Bus not initialized. Call Bus.New to initialize.");

        /// <inheritdoc cref="IBus.DispatchAsync(ICommand, CancellationToken?)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAsync(command, cancellationToken);
        /// <inheritdoc cref="IBus.DispatchAwaitAsync(ICommand, CancellationToken?)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAwaitAsync(command, cancellationToken);
        /// <inheritdoc cref="IBus.DispatchAsync(IEvent, CancellationToken?)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAsync(@event, cancellationToken);
        /// <inheritdoc cref="IBus.DispatchAwaitAsync{TResult}(ICommand{TResult}, CancellationToken?)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null)
            => StaticBus.DispatchAwaitAsync<TResult>(command, cancellationToken);

        /// <inheritdoc cref="IBus.DispatchAsync(ICommand, TimeSpan)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(ICommand command, TimeSpan timeout)
            => StaticBus.DispatchAsync(command, timeout);
        /// <inheritdoc cref="IBus.DispatchAwaitAsync(ICommand, TimeSpan)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAwaitAsync(ICommand command, TimeSpan timeout)
            => StaticBus.DispatchAwaitAsync(command, timeout);
        /// <inheritdoc cref="IBus.DispatchAsync(IEvent, TimeSpan)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task DispatchAsync(IEvent @event, TimeSpan timeout)
            => StaticBus.DispatchAsync(@event, timeout);
        /// <inheritdoc cref="IBus.DispatchAwaitAsync{TResult}(ICommand{TResult}, TimeSpan)"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, TimeSpan timeout)
            => StaticBus.DispatchAwaitAsync<TResult>(command, timeout);

        /// <inheritdoc cref="IBus.Call{TInterface}()"/>
        //[Obsolete("Use IBus instance with depencendy injection instead", false)]
        public static TInterface Call<TInterface>()
            => StaticBus.Call<TInterface>();
    }
}