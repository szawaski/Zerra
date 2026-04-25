// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Repository
{
    /// <summary>
    /// Provides extension methods for registering repository services with the bus services.
    /// </summary>
    public static class BusServicesExtensions
    {
        /// <summary>
        /// Adds a repository instance to the bus services.
        /// </summary>
        /// <param name="busServices">The bus services to register the repository with.</param>
        /// <param name="repo">The repository instance to register.</param>
        public static void AddRepo(this BusServices busServices, IRepo repo)
        {
            busServices.AddService<IRepo>(repo);
        }
    }
}
