// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Internal interface for bus operations used by generated proxy methods.
    /// This interface is not intended for public use and should not be used directly in application code.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface IBusInternal
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Internal method to call a query method and return a synchronous result.
        /// </summary>
        /// <typeparam name="TReturn">The return type of the query method.</typeparam>
        /// <param name="interfaceType">The interface type containing the query method.</param>
        /// <param name="methodName">The name of the query method to call.</param>
        /// <param name="arguments">The arguments to pass to the query method.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <returns>The result returned by the query method.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, string source);
        /// <summary>
        /// Internal method to call a query method that returns a non-generic task.
        /// </summary>
        /// <param name="interfaceType">The interface type containing the query method.</param>
        /// <param name="methodName">The name of the query method to call.</param>
        /// <param name="arguments">The arguments to pass to the query method.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <returns>A task representing the asynchronous query operation.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _CallMethodTask(Type interfaceType, string methodName, object[] arguments, string source);
        /// <summary>
        /// Internal method to call a query method that returns a generic task with a result.
        /// </summary>
        /// <typeparam name="TReturn">The return type of the query method.</typeparam>
        /// <param name="interfaceType">The interface type containing the query method.</param>
        /// <param name="methodName">The name of the query method to call.</param>
        /// <param name="arguments">The arguments to pass to the query method.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <returns>A task that returns the result of the query method.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task<TReturn> _CallMethodTaskGeneric<TReturn>(Type interfaceType, string methodName, object[] arguments, string source);

        /// <summary>
        /// Internal method to dispatch a command asynchronously.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="commandType">The type of the command.</param>
        /// <param name="requireAffirmation">Indicates whether to wait for the command to be processed before returning.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous dispatch operation.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, string source, CancellationToken cancellationToken);
        /// <summary>
        /// Internal method to dispatch a command asynchronously and return a result.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the command.</typeparam>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="commandType">The type of the command.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that returns the result of the command.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task<TResult> _DispatchCommandWithResultInternalAsync<TResult>(ICommand<TResult> command, Type commandType, string source, CancellationToken cancellationToken);
        /// <summary>
        /// Internal method to dispatch an event asynchronously.
        /// </summary>
        /// <param name="event">The event to dispatch.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The name of the service making the call.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous dispatch operation.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _DispatchEventInternalAsync(IEvent @event, Type eventType, string source, CancellationToken cancellationToken);
#pragma warning restore IDE1006 // Naming Styles
    }
}
