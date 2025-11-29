// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.CQRS
{
    /// <summary>
    /// Provides context information and services for handlers during message processing.
    /// Contains references to the bus, the current service, logging, and dependency injection container.
    /// </summary>
    public sealed class BusContext
    {
        /// <summary>
        /// Gets the bus instance for routing commands, events, and queries.
        /// </summary>
        public IBus Bus { get; init; }
        /// <summary>
        /// Gets the name of the current service.
        /// </summary>
        public string Service { get; init; }
        /// <summary>
        /// Gets the logger for this context, or null if logging is not configured.
        /// </summary>
        public ILogger? Log { get; init; }

        private readonly Dictionary<Type, object>? dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusContext"/> class.
        /// </summary>
        /// <param name="bus">The bus instance for routing messages.</param>
        /// <param name="service">The name of the current service.</param>
        /// <param name="log">The logger instance, or null if logging is not configured.</param>
        /// <param name="busScopes">The bus scopes containing registered dependencies.</param>
        internal BusContext(IBus bus, string service, ILogger? log, BusScopes busScopes)
        {
            this.Bus = bus;
            this.Service = service;
            this.Log = log;
            this.dependencies = busScopes?.Dependencies;
        }

        /// <summary>
        /// Retrieves a registered dependency of the specified type from the dependency injection container.
        /// </summary>
        /// <typeparam name="TInterface">The type of the dependency to retrieve.</typeparam>
        /// <returns>The registered dependency instance.</returns>
        /// <exception cref="ArgumentException">Thrown if no dependency is registered for the specified type.</exception>
        public TInterface Get<TInterface>() where TInterface : notnull
        {
            if (dependencies == null || !dependencies.TryGetValue(typeof(TInterface), out var instance))
                throw new ArgumentException($"No dependency registered for type {typeof(TInterface).FullName}");
            return (TInterface)instance;
        }
    }
}
