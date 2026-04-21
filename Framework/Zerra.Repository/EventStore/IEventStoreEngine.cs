// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the low-level storage operations for an append-only event store.
    /// </summary>
    public interface IEventStoreEngine : IDataStoreEngine
    {
        /// <summary>
        /// Appends a new event with data to the specified stream.
        /// </summary>
        /// <param name="eventID">The unique identifier of the event.</param>
        /// <param name="eventName">The name of the event type.</param>
        /// <param name="streamName">The name of the stream to append to.</param>
        /// <param name="expectedEventNumber">The event number expected to be the last in the stream for optimistic concurrency, or <see langword="null"/> to skip the check.</param>
        /// <param name="expectedState">The expected existence state of the stream.</param>
        /// <param name="data">The serialized event payload.</param>
        /// <returns>The event number assigned to the appended event.</returns>
        ulong Append(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data);
        /// <summary>
        /// Appends a terminating (deletion) marker event to the specified stream, marking it as closed.
        /// </summary>
        /// <param name="eventID">The unique identifier of the termination event.</param>
        /// <param name="eventName">The name of the termination event type.</param>
        /// <param name="streamName">The name of the stream to terminate.</param>
        /// <param name="expectedEventNumber">The event number expected to be the last in the stream for optimistic concurrency, or <see langword="null"/> to skip the check.</param>
        /// <param name="expectedState">The expected existence state of the stream.</param>
        /// <returns>The event number assigned to the termination event.</returns>
        ulong Terminate(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState);
        /// <summary>
        /// Reads events from the specified stream in forward order.
        /// </summary>
        /// <param name="streamName">The name of the stream to read from.</param>
        /// <param name="startEventNumber">The event number to start reading from, or <see langword="null"/> to start from the beginning.</param>
        /// <param name="eventCount">The maximum number of events to read, or <see langword="null"/> for no limit.</param>
        /// <param name="endEventNumber">The event number to stop reading at (inclusive), or <see langword="null"/> for no limit.</param>
        /// <param name="startEventDate">The earliest event date to include, or <see langword="null"/> for no lower bound.</param>
        /// <param name="endEventDate">The latest event date to include, or <see langword="null"/> for no upper bound.</param>
        /// <returns>An array of <see cref="EventStoreEventData"/> representing the matched events.</returns>
        EventStoreEventData[] Read(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
        /// <summary>
        /// Reads events from the specified stream in reverse order.
        /// </summary>
        /// <param name="streamName">The name of the stream to read from.</param>
        /// <param name="startEventNumber">The event number to start reading backwards from, or <see langword="null"/> to start from the end.</param>
        /// <param name="eventCount">The maximum number of events to read, or <see langword="null"/> for no limit.</param>
        /// <param name="endEventNumber">The event number to stop reading at (inclusive), or <see langword="null"/> for no limit.</param>
        /// <param name="startEventDate">The earliest event date to include, or <see langword="null"/> for no lower bound.</param>
        /// <param name="endEventDate">The latest event date to include, or <see langword="null"/> for no upper bound.</param>
        /// <returns>An array of <see cref="EventStoreEventData"/> representing the matched events in reverse order.</returns>
        EventStoreEventData[] ReadBackwards(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);

        /// <summary>
        /// Asynchronously appends a new event with data to the specified stream.
        /// </summary>
        /// <param name="eventID">The unique identifier of the event.</param>
        /// <param name="eventName">The name of the event type.</param>
        /// <param name="streamName">The name of the stream to append to.</param>
        /// <param name="expectedEventNumber">The event number expected to be the last in the stream for optimistic concurrency, or <see langword="null"/> to skip the check.</param>
        /// <param name="expectedState">The expected existence state of the stream.</param>
        /// <param name="data">The serialized event payload.</param>
        /// <returns>A task that resolves to the event number assigned to the appended event.</returns>
        Task<ulong> AppendAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data);
        /// <summary>
        /// Asynchronously appends a terminating (deletion) marker event to the specified stream, marking it as closed.
        /// </summary>
        /// <param name="eventID">The unique identifier of the termination event.</param>
        /// <param name="eventName">The name of the termination event type.</param>
        /// <param name="streamName">The name of the stream to terminate.</param>
        /// <param name="expectedEventNumber">The event number expected to be the last in the stream for optimistic concurrency, or <see langword="null"/> to skip the check.</param>
        /// <param name="expectedState">The expected existence state of the stream.</param>
        /// <returns>A task that resolves to the event number assigned to the termination event.</returns>
        Task<ulong> TerminateAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState);
        /// <summary>
        /// Asynchronously reads events from the specified stream in forward order.
        /// </summary>
        /// <param name="streamName">The name of the stream to read from.</param>
        /// <param name="startEventNumber">The event number to start reading from, or <see langword="null"/> to start from the beginning.</param>
        /// <param name="eventCount">The maximum number of events to read, or <see langword="null"/> for no limit.</param>
        /// <param name="endEventNumber">The event number to stop reading at (inclusive), or <see langword="null"/> for no limit.</param>
        /// <param name="startEventDate">The earliest event date to include, or <see langword="null"/> for no lower bound.</param>
        /// <param name="endEventDate">The latest event date to include, or <see langword="null"/> for no upper bound.</param>
        /// <returns>A task that resolves to an array of <see cref="EventStoreEventData"/> representing the matched events.</returns>
        Task<EventStoreEventData[]> ReadAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
        /// <summary>
        /// Asynchronously reads events from the specified stream in reverse order.
        /// </summary>
        /// <param name="streamName">The name of the stream to read from.</param>
        /// <param name="startEventNumber">The event number to start reading backwards from, or <see langword="null"/> to start from the end.</param>
        /// <param name="eventCount">The maximum number of events to read, or <see langword="null"/> for no limit.</param>
        /// <param name="endEventNumber">The event number to stop reading at (inclusive), or <see langword="null"/> for no limit.</param>
        /// <param name="startEventDate">The earliest event date to include, or <see langword="null"/> for no lower bound.</param>
        /// <param name="endEventDate">The latest event date to include, or <see langword="null"/> for no upper bound.</param>
        /// <returns>A task that resolves to an array of <see cref="EventStoreEventData"/> representing the matched events in reverse order.</returns>
        Task<EventStoreEventData[]> ReadBackwardsAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
    }
}
