using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Common.Data
{
    public static class DataStoreSetup
    {
        /// <summary>
        /// Resolves the service's data store, creates or updates its schema from the data models, and describes the store that was chosen.
        /// </summary>
        /// <typeparam name="TContext">The service's <see cref="DataContextSelector"/>, listing the preferred database first and the in-memory store last.</typeparam>
        /// <param name="preferredStore">Display name of the preferred database, e.g. "PostgreSQL".</param>
        /// <param name="dataModels">The data models the service persists.</param>
        /// <param name="log">Receives the schema generation output.</param>
        public static IDataStoreInfo Prepare<TContext>(string preferredStore, Type[] dataModels, ILogger log)
            where TContext : DataContext, new()
        {
            var context = new TContext();
            if (!context.TryGetEngine(out var engine))
                throw new InvalidOperationException($"{typeof(TContext).Name} has no available data store");

            //creates the database and tables on first run and adds new columns later, the in-memory store needs no schema
            CodeFirstGeneration.Generate<TContext>(DataStoreGenerationType.CodeFirst | DataStoreGenerationType.NoDelete, dataModels, log);

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
    }
}
