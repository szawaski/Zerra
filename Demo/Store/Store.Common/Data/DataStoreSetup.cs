using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Common.Data
{
    public static class DataStoreSetup
    {
        /// <summary>
        /// Creates or updates the service's data store schema from the data models, and describes the store that was chosen.
        /// </summary>
        /// <param name="engine">The engine of the preferred database, or the in-memory engine when the database isn't reachable.</param>
        /// <param name="preferredStore">Display name of the preferred database, e.g. "PostgreSQL".</param>
        /// <param name="dataModels">The data models the service persists.</param>
        /// <param name="log">Receives the schema generation output.</param>
        public static IDataStoreInfo Prepare(ITransactStoreEngine engine, string preferredStore, Type[] dataModels, ILogger log)
        {
            //creates the database and tables on first run and adds new columns later, the in-memory store needs no schema
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst | DataStoreGenerationType.NoDelete, dataModels, log);

            IDataStoreInfo info;
            if (engine is MemoryEngine)
            {
                info = new DataStoreInfo(StoreSettings.InMemoryOnly ? "In-memory" :$"In-memory ({preferredStore} not reachable)");
                log.Warn($"Data store: {info.Description}, data resets when the service restarts");
            }
            else
            {
                info = new DataStoreInfo(preferredStore);
                log.Info($"Data store: {info.Description}");
            }
            return info;
        }

        /// <summary>
        /// Describes the service's event store. An event store has no schema, a stream is created by its first event.
        /// </summary>
        /// <param name="engine">The engine of the preferred event store, or the in-memory engine when the event store isn't reachable.</param>
        /// <param name="preferredStore">Display name of the preferred event store, e.g. "KurrentDB".</param>
        /// <param name="log">Receives which store was chosen.</param>
        public static IDataStoreInfo PrepareEventStore(IEventStoreEngine engine, string preferredStore, ILogger log)
        {
            IDataStoreInfo info;
            if (engine is MemoryEngine)
            {
                info = new DataStoreInfo(StoreSettings.InMemoryOnly ? "In-memory event store" : $"In-memory event store ({preferredStore} not reachable)");
                log.Warn($"Data store: {info.Description}, data resets when the service restarts");
            }
            else
            {
                info = new DataStoreInfo(preferredStore);
                log.Info($"Data store: {info.Description}");
            }
            return info;
        }
    }
}
