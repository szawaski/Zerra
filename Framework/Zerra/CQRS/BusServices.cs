// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Manages service dependencies for the bus initialization.
    /// Provides a container for registering service instances that are made available to handlers during message processing.
    /// Instances are retrieved from handlers via <see cref="BusContext.GetService{TInterface}()"/> or <see cref="BusContext.TryGetService{TInterface}(out TInterface)"/>.
    /// </summary>
    public sealed class BusServices
    {
        /// <summary>
        /// Gets the internal dictionary of registered dependencies.
        /// </summary>
        internal Dictionary<Type, object> Dependencies { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusServices"/> class with an empty dependency container.
        /// </summary>
        public BusServices()
        {
            this.Dependencies = new();
        }

        /// <summary>
        /// Registers a service dependency instance for the specified interface type to be available to handlers.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to register. Must be an interface type.</typeparam>
        /// <param name="instance">The instance to register for the interface type.</param>
        /// <exception cref="ArgumentNullException">Thrown if the instance is null.</exception>
        /// <exception cref="ArgumentException">Thrown if TInterface is not an interface type.</exception>
        public void AddService<TInterface>(TInterface instance) where TInterface : notnull
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("TInterface must be an interface type");
            Dependencies[interfaceType] = instance;
        }

        /// <summary>
        /// Registers a service dependency instance for the specified interface type to be available to handlers.
        /// </summary>
        /// <param name="interfaceType">The interface type to register. Must be an interface type.</param>
        /// <param name="instance">The instance to register for the interface type.</param>
        /// <exception cref="ArgumentNullException">Thrown if the instance is null.</exception>
        /// <exception cref="ArgumentException">Thrown if interfaceType is not an interface type or if instance is not of the provided interface type.</exception>
        public void AddService(Type interfaceType, object instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));
            if (!interfaceType.IsInterface)
                throw new ArgumentException("TInterface must be an interface type");
            if (!interfaceType.IsInstanceOfType(instance))
                throw new ArgumentException("Instance must be of the provided interface type");
            Dependencies[interfaceType] = instance;
        }
    }
}
