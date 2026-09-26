// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines an event consumer that can receive and process events
    /// After <see cref="Bus"/> calls Close it disposes the implementation, and disposing waits for everything already received to finish before it returns.
    /// </summary>
    public interface IEventConsumer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The host information.
        /// </summary>
        string MessageHost { get; }
        /// <summary>
        /// Registers an event type for this interface.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this event consumer.</param>
        /// <param name="topic">The message service topic.</param>
        /// <param name="type">The event type.</param>
        /// <param name="eventConsumerMode">Whether every replica of this service receives the event or the replicas compete so only one of them does.</param>
        void RegisterEventType(int maxConcurrent, string topic, Type type, EventConsumerMode eventConsumerMode);
        /// <summary>
        /// A method called from <see cref="Bus"/> on startup to provide parts needed for the server.
        /// </summary>
        /// <param name="serviceName">The name of this service, used to tell it apart from the other services subscribed to the same events.</param>
        /// <param name="handlerAsync">The hander delegate router that will link the acutal event methods.</param>
        void Setup(string serviceName, HandleRemoteEventDispatch handlerAsync);
        /// <summary>
        /// A method called from <see cref="Bus"/> to start receiving.
        /// </summary>
        void Open();
        /// <summary>
        /// A method called from <see cref="Bus"/> to stop receiving. The events already received keep processing, disposing waits for them to finish.
        /// </summary>
        void Close();
    }

    /// <summary>
    /// A delegate that an event consumer will use to handle a received event.
    /// <see cref="Bus"/> will provide the delegate.
    /// </summary>
    public delegate Task HandleRemoteEventDispatch(IEvent @event, string source);
}