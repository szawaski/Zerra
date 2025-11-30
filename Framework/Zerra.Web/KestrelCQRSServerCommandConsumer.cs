// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Web
{
    /// <summary>
    /// Kestrel-based implementation of command consumer for CQRS systems over HTTP.
    /// </summary>
    /// <remarks>
    /// Registers command types and handlers for processing command requests from CQRS clients.
    /// Works as part of the KestrelCqrsServerMiddleware to handle command dispatch and execution.
    /// </remarks>
    public sealed class KestrelCqrsServerCommandConsumer : ICommandConsumer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsServerCommandConsumer"/> class.
        /// </summary>
        /// <param name="settings">The server settings for managing registered types and handlers.</param>
        public KestrelCqrsServerCommandConsumer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Closes the command consumer connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based consumer as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Close() { }

        /// <summary>
        /// Opens the command consumer connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based consumer as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Open() { }

        /// <summary>
        /// Gets the message host identifier for the command consumer.
        /// </summary>
        /// <remarks>
        /// Returns a placeholder string as the actual host is determined by the ASP.NET Core configuration.
        /// </remarks>
        string ICommandConsumer.MessageHost => "[Kestrel Host]";

        /// <summary>
        /// Sets up the command consumer with handlers for processing command requests.
        /// </summary>
        /// <remarks>
        /// Stores the command counter and command handler delegates for use during request processing.
        /// Called during initialization before the consumer starts receiving requests.
        /// </remarks>
        /// <param name="commandCounter">The counter for tracking command processing limits.</param>
        /// <param name="handlerAsync">The async delegate for fire-and-forget command dispatch.</param>
        /// <param name="handlerAwaitAsync">The async delegate for command dispatch with await semantics.</param>
        /// <param name="handlerWithResultAwaitAsync">The async delegate for command dispatch with result return.</param>
        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            settings.CommandCounter = commandCounter;
            settings.CommandHandlerAsync = handlerAsync;
            settings.CommandHandlerAwaitAsync = handlerAwaitAsync;
            settings.CommandHandlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }

        /// <summary>
        /// Registers a command type with the consumer.
        /// </summary>
        /// <remarks>
        /// Registers the type and creates a semaphore to control concurrent command processing.
        /// Thread-safe for concurrent registration of multiple types.
        /// </remarks>
        /// <param name="maxConcurrent">The maximum number of concurrent command handlers for this type.</param>
        /// <param name="topic">The topic or routing key for this command type (not used for Kestrel).</param>
        /// <param name="type">The command type to register.</param>
        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (settings.Types.ContainsKey(type))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!settings.Types.TryAdd(type, throttle))
                throttle.Dispose();
        }
    }
}