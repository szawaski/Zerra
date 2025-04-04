// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines an event consumer that can receive and process events
    /// </summary>
    public interface IEventConsumer
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
        void RegisterEventType(int maxConcurrent, string topic, Type type);
        /// <summary>
        /// A method called from <see cref="Bus"/> on startup to provide parts needed for the server.
        /// </summary>
        /// <param name="handlerAsync">The hander delegate router that will link the acutal event methods.</param>
        void Setup(HandleRemoteEventDispatch handlerAsync);
        /// <summary>
        /// A method called from <see cref="Bus"/> to start receiving.
        /// </summary>
        void Open();
        /// <summary>
        /// A method called from <see cref="Bus"/> to stop receiving.
        /// </summary>
        void Close();
    }

    /// <summary>
    /// A delegate that an event consumer will use to handle a received event.
    /// <see cref="Bus"/> will provide the delegate.
    /// </summary>
    public delegate Task HandleRemoteEventDispatch(IEvent @event, string source, bool isApi, CancellationToken cancellationToken);
}