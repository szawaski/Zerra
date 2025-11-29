// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS
{
    public interface IBus
    {
        TInterface Call<TInterface>();

        Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null);
        Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null);
        Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null);
        Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null);

        void AddHandler<IInterface>(IInterface handler);
        void AddCommandProducer<TInterface>(ICommandProducer commandProducer);
        void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer);
        void AddEventProducer<TInterface>(IEventProducer eventProducer);
        void AddEventConsumer<TInterface>(IEventConsumer eventConsumer);
        void AddQueryClient<TInterface>(IQueryClient queryClient);
        void AddQueryServer<TInterface>(IQueryServer queryServer);

        Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, string?[] arguments, string source, bool isApi, ISerializer serializer, CancellationToken cancellationToken);
        Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        Task RemoteHandleEventDispatchAsync(IEvent @event, string source, bool isApi);

        /// <summary>
        /// Manually stop all the services. This is not necessary if using <see cref="WaitForExit"/> or <see cref="WaitForExitAsync"/>. 
        /// </summary>
        void StopServices();
        /// <summary>
        /// Manually stop all the services. This is not necessary if using <see cref="WaitForExit"/> or <see cref="WaitForExitAsync"/>.
        /// </summary>
        Task StopServicesAsync();

        /// <summary>
        /// An awaiter to hold the assembly process until it receives a shutdown command.
        /// All the services will be stopped upon shutdown.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        void WaitForExit(CancellationToken cancellationToken = default);

        /// <summary>
        /// An awaiter to hold the assembly process until it receives a shutdown command.
        /// All the services will be stopped upon shutdown.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        Task WaitForExitAsync(CancellationToken cancellationToken = default);
    }
}
