// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    /// <summary>
    /// A handler method for an event.
    /// One practice is a collective interface for a set of these so an implementation can handle multiples and configuration is easy.
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    public interface IEventHandler<T> where T : IEvent
    {
        /// <summary>
        /// Handles processing the event.
        /// </summary>
        /// <param name="event">The event to process.</param>
        /// <returns>A <see cref="Task"/> to await processing the event.</returns>
        Task Handle(T @event);
    }
}