// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Logging;
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
        /// Gets the logger instance for recording diagnostic and operational events for this handler.
        /// May be <see langword="null"/> if logging is not configured.
        /// </summary>
        ILog? Log { get; }

        /// <summary>
        /// Gets the logical service name for this handler, used for routing and identification.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Resolves a service dependency from the current context.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type to resolve.</typeparam>
        /// <returns>An instance implementing <typeparamref name="TInterface"/>.</returns>
        TInterface GetService<TInterface>() where TInterface : notnull;

        /// <summary>
        /// Attempts to resolve a service dependency from the current context.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type to resolve.</typeparam>
        /// <param name="instance">When this method returns, contains an instance implementing <typeparamref name="TInterface"/> if found; otherwise, the default value. This parameter is passed uninitialized.</param>
        /// <returns>true if a service is registered for the specified type; otherwise, false.</returns>
        bool TryGetService<TInterface>([MaybeNullWhen(false)] out TInterface? instance) where TInterface : notnull;
    }
}
