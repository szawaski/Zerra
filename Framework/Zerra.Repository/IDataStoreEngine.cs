// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the contract for a data store engine that can validate a data source and build store generation plans.
    /// </summary>
    public interface IDataStoreEngine
    {
        /// <summary>
        /// Validates that the data source is accessible and properly configured.
        /// </summary>
        /// <returns><c>true</c> if the data source is valid; otherwise, <c>false</c>.</returns>
        bool ValidateDataSource();

        /// <summary>
        /// Builds a plan for generating or modifying the data store structure based on the provided model details.
        /// </summary>
        /// <param name="create">Indicates whether to include creation of new store structures.</param>
        /// <param name="update">Indicates whether to include updates to existing store structures.</param>
        /// <param name="delete">Indicates whether to include deletion of obsolete store structures.</param>
        /// <param name="modelDetail">The collection of model details used to build the generation plan.</param>
        /// <returns>An <see cref="IDataStoreGenerationPlan"/> representing the planned store changes.</returns>
        IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail);
    }
}
