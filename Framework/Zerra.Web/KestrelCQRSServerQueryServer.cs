// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Web
{
    /// <summary>
    /// Kestrel-based implementation of query server for CQRS systems over HTTP.
    /// </summary>
    /// <remarks>
    /// Registers query interface types and handlers for processing query requests from CQRS clients.
    /// Works as part of the KestrelCqrsServerMiddleware to handle type-safe query invocations.
    /// </remarks>
    public sealed class KestrelCqrsServerQueryServer : IQueryServer
    {
        private readonly KestrelCqrsServerLinkedSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsServerQueryServer"/> class.
        /// </summary>
        /// <param name="settings">The server settings for managing registered types and handlers.</param>
        public KestrelCqrsServerQueryServer(KestrelCqrsServerLinkedSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Closes the query server connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based server as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Close() { }

        /// <summary>
        /// Opens the query server connection.
        /// </summary>
        /// <remarks>
        /// No-op for Kestrel-based server as connections are managed by ASP.NET Core.
        /// </remarks>
        public void Open() { }

        /// <summary>
        /// Gets the service URL for the query server.
        /// </summary>
        /// <remarks>
        /// Returns a placeholder string as the actual URL is determined by the ASP.NET Core host configuration.
        /// </remarks>
        string IQueryServer.ServiceUrl => "[Kestrel Host]";

        /// <summary>
        /// Registers a query interface type with the server.
        /// </summary>
        /// <remarks>
        /// Registers the type and creates a semaphore to control concurrent query processing.
        /// Thread-safe for concurrent registration of multiple types.
        /// </remarks>
        /// <param name="maxConcurrent">The maximum number of concurrent queries for this type.</param>
        /// <param name="type">The query interface type to register.</param>
        public void RegisterInterfaceType(int maxConcurrent, Type type)
        {
            lock (settings.Types)
            {
                if (settings.Types.ContainsKey(type))
                    return;
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                if (!settings.Types.TryAdd(type, throttle))
                    throttle.Dispose();
            }
        }

        /// <summary>
        /// Sets up the query server with handlers for processing query requests.
        /// </summary>
        /// <remarks>
        /// Stores the command counter and query handler delegate for use during request processing.
        /// Called during initialization before the server starts receiving requests.
        /// </remarks>
        /// <param name="commandCounter">The counter for tracking command processing limits.</param>
        /// <param name="providerHandlerAsync">The async delegate for handling query method invocations.</param>
        void IQueryServer.Setup(CommandCounter commandCounter, QueryHandlerDelegate providerHandlerAsync)
        {
            settings.CommandCounter = commandCounter;
            settings.ProviderHandlerAsync = providerHandlerAsync;
        }
    }
}