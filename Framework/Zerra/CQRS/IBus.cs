// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS
{
    /// <summary>
    /// Responsible for sending commands, events, and queries to the configured destination.
    /// A destination may be a registered handler or a remote service from a registered query client, command producer, or event producer.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Returns in instance of an interface that will route a query to the configured destination.
        /// A destination may be an implementation in the same assembly or calling a remote service.
        /// </summary>
        /// <typeparam name="TInterface">The interface type.</typeparam>
        /// <returns>An instance of the interface to route queries.</returns>
        TInterface Call<TInterface>();

        /// <summary>
        /// Send a command to the configured destination.
        /// This follows the eventual consistency pattern so the sender does not know when the command is processed.
        /// A destination may be a registered handler in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the command.</returns>
        Task DispatchAsync(ICommand command, CancellationToken? cancellationToken = null);
        /// <summary>
        /// Send a command to the configured destination and wait for it to process.
        /// This will await until the reciever has returned a signal or an exception that the command has been processed.
        /// A destination may be a registered handler in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchAwaitTimeout"/>.</param>
        /// <returns>A task to await processing of the command.</returns>
        Task DispatchAwaitAsync(ICommand command, CancellationToken? cancellationToken = null);
        /// <summary>
        /// Send an event to the configured destination.
        /// Events will go to any number of destinations that wish to recieve the event.
        /// It is not possible to recieve information back from destinations.
        /// A destination may be a registered handler in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="event">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchTimeout"/>.</param>
        /// <returns>A task to complete sending the event.</returns>
        Task DispatchAsync(IEvent @event, CancellationToken? cancellationToken = null);
        /// <summary>
        /// Send a command to the configured destination and wait for a result.
        /// This will await until the reciever has returned a result or an exception.
        /// A destination may be a registered handler in the same assembly or calling a remote service.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. This overrides <see cref="Bus.DefaultDispatchAwaitTimeout"/>.</param>
        /// <returns>A task to await the result of the command.</returns>
        Task<TResult> DispatchAwaitAsync<TResult>(ICommand<TResult> command, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Add a handler service to handle commands, events, or queries locally.
        /// </summary>
        /// <typeparam name="IInterface">An interface inheriting handler interface(s).</typeparam>
        /// <param name="handler">The handler service.</param>
        void AddHandler<IInterface>(IInterface handler);
        /// <summary>
        /// Add a command producer service to send commands to remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to send.</typeparam>
        /// <param name="commandProducer">The command producer service.</param>
        void AddCommandProducer<TInterface>(ICommandProducer commandProducer);
        /// <summary>
        /// Add a command consumer service to receive commands from remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting command handler interface(s) for the types of commands to receive.</typeparam>
        /// <param name="commandConsumer">The command consumer service.</param>
        void AddCommandConsumer<TInterface>(ICommandConsumer commandConsumer);
        /// <summary>
        /// Add an event producer service to send commands to remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to send.</typeparam>
        /// <param name="eventProducer">The event producer service.</param>
        void AddEventProducer<TInterface>(IEventProducer eventProducer);
        /// <summary>
        /// Add an event consumer service to receive commands from remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface inheriting event handler interface(s) for the types of events to receive.</typeparam>
        /// <param name="eventConsumer">The event consumer service.</param>
        void AddEventConsumer<TInterface>(IEventConsumer eventConsumer);
        /// <summary>
        /// Add a query client service to call for queries to remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryClient">The query client service.</param>
        void AddQueryClient<TInterface>(IQueryClient queryClient);
        /// <summary>
        /// Add a query server to receive and response to queries from remote services.
        /// </summary>
        /// <typeparam name="TInterface">An interface of queries.</typeparam>
        /// <param name="queryServer">The query server service.</param>
        void AddQueryServer<TInterface>(IQueryServer queryServer);

        /// <summary>
        /// Handle a remote query call and return the result.
        /// </summary>
        /// <param name="interfaceType">The interface type for the query.</param>
        /// <param name="methodName">The method name of the query.</param>
        /// <param name="arguments">The serialized arguments for the query method.</param>
        /// <param name="source">The source of the remote call.</param>
        /// <param name="isApi">Indicates whether the call is from an API.</param>
        /// <param name="serializer">The serializer to use for deserializing arguments and serializing results.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that returns the query call response.</returns>
        Task<RemoteQueryCallResponse> RemoteHandleQueryCallAsync(Type interfaceType, string methodName, string?[] arguments, string source, bool isApi, ISerializer serializer, CancellationToken cancellationToken);
        /// <summary>
        /// Handle a remote command dispatch without awaiting completion.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="source">The source of the remote call.</param>
        /// <param name="isApi">Indicates whether the call is from an API.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task to complete handling the dispatch.</returns>
        Task RemoteHandleCommandDispatchAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        /// <summary>
        /// Handle a remote command dispatch and await its completion.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="source">The source of the remote call.</param>
        /// <param name="isApi">Indicates whether the call is from an API.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task to complete awaiting the command processing.</returns>
        Task RemoteHandleCommandDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        /// <summary>
        /// Handle a remote command dispatch that returns a result and await its completion.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="source">The source of the remote call.</param>
        /// <param name="isApi">Indicates whether the call is from an API.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that returns the result of the command.</returns>
        Task<object?> RemoteHandleCommandWithResultDispatchAwaitAsync(ICommand command, string source, bool isApi, CancellationToken cancellationToken);
        /// <summary>
        /// Handle a remote event dispatch.
        /// </summary>
        /// <param name="event">The event to dispatch.</param>
        /// <param name="source">The source of the remote call.</param>
        /// <param name="isApi">Indicates whether the call is from an API.</param>
        /// <returns>A task to complete handling the dispatch.</returns>
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
