// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Provides setup and configuration capabilities for the bus infrastructure.
    /// Allows registration of handlers, producers, consumers, and clients/servers for commands, events, and queries.
    /// </summary>
    public interface IBusSetup : IBus
    {
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
