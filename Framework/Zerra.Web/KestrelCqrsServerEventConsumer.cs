// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Web
{
    /// <summary>
    /// Kestrel-based implementation of event consumer for CQRS systems over HTTP.
    /// </summary>
    /// <remarks>
    /// Registers event types and handlers for processing event requests from CQRS clients.
    /// Works as part of the KestrelCqrsServerMiddleware to handle event dispatch and notification.
    /// </remarks>
    public sealed class KestrelCqrsServerEventConsumer : IEventConsumer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsServerEventConsumer"/> class.
        /// </summary>
        /// <param name="settings">The server settings for managing registered types and handlers.</param>
        public KestrelCqrsServerEventConsumer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Closes the event consumer connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based consumer as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Close() { }

        /// <summary>
        /// Opens the event consumer connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based consumer as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Open() { }

        /// <summary>
        /// Gets the message host identifier for the event consumer.
        /// </summary>
        /// <remarks>
        /// Returns a placeholder string as the actual host is determined by the ASP.NET Core configuration.
        /// </remarks>
        string IEventConsumer.MessageHost => "[Kestrel Host]";

        /// <summary>
        /// Sets up the event consumer with handlers for processing event requests.
        /// </summary>
        /// <remarks>
        /// Stores the event handler delegate for use during request processing.
        /// Called during initialization before the consumer starts receiving requests.
        /// </remarks>
        /// <param name="handlerAsync">The async delegate for event dispatch and notification.</param>
        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            settings.EventHandlerAsync = handlerAsync;
        }

        /// <summary>
        /// Registers an event type with the consumer.
        /// </summary>
        /// <remarks>
        /// Registers the type and creates a semaphore to control concurrent event processing.
        /// Thread-safe for concurrent registration of multiple types.
        /// </remarks>
        /// <param name="maxConcurrent">The maximum number of concurrent event handlers for this type.</param>
        /// <param name="topic">The topic or routing key for this event type (not used for Kestrel).</param>
        /// <param name="type">The event type to register.</param>
        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (settings.Types.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!settings.Types.TryAdd(type, throttle))
                throttle.Dispose();
        }
    }
}