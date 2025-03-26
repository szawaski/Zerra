// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    /// <summary>
    /// Defines a factory to create services for <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/>.
    /// Basic services include <see cref="TcpServiceCreator"/> or <see cref="HttpServiceCreator"/>.
    /// Add on other service implementations or custom build ones for more functionality.
    /// </summary>
    public interface IServiceCreator
    {
        /// <summary>
        /// Creates a new command consumer
        /// </summary>
        /// <param name="messageHost">The message host information.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new command consumer.</returns>
        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig);
        /// <summary>
        /// Creates a new event consumer
        /// </summary>
        /// <param name="messageHost">The message host information.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new event consumer.</returns>
        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig);
        /// <summary>
        /// Creates a new query server
        /// </summary>
        /// <param name="serviceUrl">The service url to bind.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new query server.</returns>
        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig);

        /// <summary>
        /// Creates a new command producer
        /// </summary>
        /// <param name="messageHost">The message host information.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new command producer.</returns>
        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig);
        /// <summary>
        /// Creates a new event producer
        /// </summary>
        /// <param name="messageHost">The message host information.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new event producer.</returns>
        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig);
        /// <summary>
        /// Creates a new query client
        /// </summary>
        /// <param name="serviceUrl">The service url to call.</param>
        /// <param name="symmetricConfig">The encryption information supplied by <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/></param>
        /// <returns>The new query client.</returns>
        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig);
    }
}