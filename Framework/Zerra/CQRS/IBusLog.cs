// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Methods to implement <see cref="Bus"/> logging. The implementaion must be added to the <see cref="Bus"/> using <see cref="Bus.New" />
    /// </summary>
    public interface IBusLog
    {
        /// <summary>
        /// Called at the beginning of command processing.
        /// </summary>
        /// <param name="commandType">The type of the command being processed.</param>
        /// <param name="command">The command instance.</param>
        /// <param name="service">The name of the current service processing the command.</param>
        /// <param name="source">The name of the service the command came from.</param>
        /// <param name="handled">Indicates whether the command was handled locally or remotely.</param>
        void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled);
        /// <summary>
        /// Called at the beginning of event processing.
        /// </summary>
        /// <param name="eventType">The type of the event being processed.</param>
        /// <param name="event">The event instance.</param>
        /// <param name="service">The name of the current service processing the event.</param>
        /// <param name="source">The name of the service the event came from.</param>
        /// <param name="handled">Indicates whether the event was handled locally or remotely.</param>
        void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled);
        /// <summary>
        /// Called at the beginning of a query call.
        /// </summary>
        /// <param name="interfaceType">The interface type of the query.</param>
        /// <param name="methodName">The name of the query method being called.</param>
        /// <param name="arguments">The arguments passed to the query method.</param>
        /// <param name="service">The name of the current service processing the query.</param>
        /// <param name="source">The name of the service the query call came from.</param>
        /// <param name="handled">Indicates whether the query was handled locally or remotely.</param>
        void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled);

        /// <summary>
        /// Called at the end of command processing.
        /// </summary>
        /// <param name="commandType">The type of the command that was processed.</param>
        /// <param name="command">The command instance.</param>
        /// <param name="service">The name of the current service that processed the command.</param>
        /// <param name="source">The name of the service the command came from.</param>
        /// <param name="handled">Indicates whether the command was handled locally or remotely.</param>
        /// <param name="milliseconds">The time in milliseconds taken to process the command.</param>
        /// <param name="ex">Any exception that occurred during processing, or null if successful.</param>
        void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex);
        /// <summary>
        /// Called at the end of event processing.
        /// </summary>
        /// <param name="eventType">The type of the event that was processed.</param>
        /// <param name="event">The event instance.</param>
        /// <param name="service">The name of the current service that processed the event.</param>
        /// <param name="source">The name of the service the event came from.</param>
        /// <param name="handled">Indicates whether the event was handled locally or remotely.</param>
        /// <param name="milliseconds">The time in milliseconds taken to process the event.</param>
        /// <param name="ex">Any exception that occurred during processing, or null if successful.</param>
        void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex);
        /// <summary>
        /// Called at the end of a query call.
        /// </summary>
        /// <param name="interfaceType">The interface type of the query.</param>
        /// <param name="methodName">The name of the query method that was called.</param>
        /// <param name="arguments">The arguments passed to the query method.</param>
        /// <param name="result">The result returned from the query call, or null if no result.</param>
        /// <param name="service">The name of the current service that processed the query.</param>
        /// <param name="source">The name of the service the query call came from.</param>
        /// <param name="handled">Indicates whether the query was handled locally or remotely.</param>
        /// <param name="milliseconds">The time in milliseconds taken to process the query.</param>
        /// <param name="ex">Any exception that occurred during processing, or null if successful.</param>
        void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex);
    }
}
