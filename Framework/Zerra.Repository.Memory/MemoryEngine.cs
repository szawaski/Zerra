// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Reflection;

namespace Zerra.Repository.Memory
{
    /// <summary>
    /// The core in-memory data store engine, implementing query, insert, update, and delete operations for a transact store as well as the append and read operations for an event store.
    /// </summary>
    public sealed partial class MemoryEngine : ITransactStoreEngine, IEventStoreEngine
    {
        /// <inheritdoc />
        public bool ValidateDataSource() => true;

        /// <inheritdoc />
        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail)
        {
            return new EmptyDataStoreGenerationPlan();
        }
    }
}
