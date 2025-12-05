// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
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
        public string ServiceName { get; init; }
        /// <summary>
        /// Gets the logger for this context, or null if logging is not configured.
        /// </summary>
        public ILog? Log { get; init; }

        private readonly Dictionary<Type, object>? dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusContext"/> class.
        /// </summary>
        /// <param name="bus">The bus instance for routing messages.</param>
        /// <param name="serviceName">The name of the current service.</param>
        /// <param name="log">The logger instance, or null if logging is not configured.</param>
        /// <param name="busServices">The bus servies containing registered dependencies.</param>
        internal BusContext(IBus bus, string serviceName, ILog? log, BusServices busServices)
        {
            this.Bus = bus;
            this.ServiceName = serviceName;
            this.Log = log;
            this.dependencies = busServices?.Dependencies;
        }

        /// <summary>
        /// Retrieves a registered dependency of the specified type from the dependency injection container.
        /// </summary>
        /// <typeparam name="TInterface">The type of the dependency to retrieve.</typeparam>
        /// <returns>The registered dependency instance.</returns>
        /// <exception cref="ArgumentException">Thrown if no dependency is registered for the specified type.</exception>
        public TInterface GetService<TInterface>() where TInterface : notnull
        {
            if (dependencies == null || !dependencies.TryGetValue(typeof(TInterface), out var instanceObject))
                throw new ArgumentException($"No dependency registered for type {typeof(TInterface).FullName}");
            return (TInterface)instanceObject;
        }

        /// <summary>
        /// Attempts to retrieve a registered dependency of the specified type from the dependency injection container.
        /// </summary>
        /// <typeparam name="TInterface">The type of the dependency to retrieve.</typeparam>
        /// <param name="instance">When this method returns, contains the registered dependency instance if found; otherwise, the default value. This parameter is passed uninitialized.</param>
        /// <returns>true if a dependency is registered for the specified type; otherwise, false.</returns>
        public bool TryGetService<TInterface>([MaybeNullWhen(false)] out TInterface? instance) where TInterface : notnull
        {
            if (dependencies == null || !dependencies.TryGetValue(typeof(TInterface), out var instanceObject))
            {
                instance = default;
                return false;
            }
            instance = (TInterface)instanceObject;
            return true;
        }
    }
}
