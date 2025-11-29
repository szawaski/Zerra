// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Manages scoped dependencies for the bus initialization.
    /// Provides a container for registering service instances that are made available to handlers during message processing.
    /// Instances are retrieved from handlers via <see cref="BusContext.Get{TInterface}()"/>.
    /// </summary>
    public sealed class BusScopes
    {
        /// <summary>
        /// Gets the internal dictionary of registered dependencies.
        /// </summary>
        internal Dictionary<Type, object> Dependencies { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusScopes"/> class with an empty dependency container.
        /// </summary>
        public BusScopes()
        {
            this.Dependencies = new();
        }

        /// <summary>
        /// Registers a scoped dependency instance for the specified interface type to be available to handlers.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register. Must be an interface type.</typeparam>
        /// <param name="instance">The instance to register for the interface type.</param>
        /// <exception cref="ArgumentNullException">Thrown if the instance is null.</exception>
        /// <exception cref="ArgumentException">Thrown if TInterface is not an interface type.</exception>
        public void AddScope<TInterface>(TInterface instance) where TInterface : notnull
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));
            var type = typeof(TInterface);
            if (!type.IsInterface)
                throw new ArgumentException("TInterface must be an interface type");
            Dependencies[type] = instance;
        }
    }
}
