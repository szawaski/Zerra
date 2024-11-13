// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// Defines an event producer to send event.
    /// </summary>
    public interface IEventProducer
    {
        /// <summary>
        /// Registers an event for this producer.
        /// </summary>
        /// <param name="maxConcurrent">The max number of concurrent requests for this event producer.</param>
        /// <param name="topic">The message service topic.</param>
        /// <param name="type">The event type.</param>
        void RegisterEventType(int maxConcurrent, string topic, Type type);
        /// <summary>
        /// Executes sending an event.
        /// </summary>
        /// <param name="event">The event to send.</param>
        /// <param name="source">A description of where the command came from.</param>
        /// <returns>A task to await sending the event.</returns>
        Task DispatchAsync(IEvent @event, string source);
    }
}