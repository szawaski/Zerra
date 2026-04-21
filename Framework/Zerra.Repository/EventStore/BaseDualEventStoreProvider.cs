// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract base for a provider that combines an event store with a next (transactional) store.
    /// Temporal queries are routed to the event store provider; all other queries and persists are forwarded to both providers.
    /// </summary>
    /// <typeparam name="TThisProviderInterface">The interface type of the event store (this) provider.</typeparam>
    /// <typeparam name="TNextProviderInterface">The interface type of the next (transactional) provider.</typeparam>
    /// <typeparam name="TModel">The model type this provider operates on.</typeparam>
    public abstract class BaseDualEventStoreProvider<TThisProviderInterface, TNextProviderInterface, TModel> : LayerProvider<TNextProviderInterface>, ITransactStoreProvider<TModel>
        where TThisProviderInterface : ITransactStoreProvider<TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        /// <summary>
        /// The <see cref="Type"/> of <typeparamref name="TModel"/>.
        /// </summary>
        protected static readonly Type modelType = typeof(TModel);
        /// <inheritdoc/>
        public Type ModelType => modelType;

        private readonly ITransactStoreProvider thisProvider;

        /// <summary>
        /// Initializes a new instance wiring the event store provider and the next provider.
        /// </summary>
        /// <param name="thisProvider">The event store provider used for temporal queries and persists.</param>
        /// <param name="nextProvider">The next (transactional) provider used for non-temporal queries and persists.</param>
        public BaseDualEventStoreProvider(TThisProviderInterface thisProvider, TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
            this.thisProvider = thisProvider;
        }

        /// <summary>
        /// Gets the event store (this) provider.
        /// </summary>
        protected ITransactStoreProvider ThisProvider => thisProvider;

        /// <summary>
        /// Routes the query to the event store provider for temporal queries, or to the next provider for standard queries.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <see langword="null"/> if no result is produced.</returns>
        public object? Query(Query query)
        {
            if (query.IsTemporal)
            {
                return ThisProvider.Query(query);
            }
            else
            {
                return NextProvider.Query(query);
            }
        }

        /// <summary>
        /// Asynchronously routes the query to the event store provider for temporal queries, or to the next provider for standard queries.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that resolves to the query result, or <see langword="null"/> if no result is produced.</returns>
        public Task<object?> QueryAsync(Query query)
        {
            if (query.IsTemporal)
            {
                return ThisProvider.QueryAsync(query);
            }
            else
            {
                return NextProvider.QueryAsync(query);
            }
        }

        /// <summary>
        /// Persists changes through both the event store provider and the next provider.
        /// </summary>
        /// <param name="persist">The persist operation to apply.</param>
        public void Persist(Persist persist)
        {
            ThisProvider.Persist(persist);
            NextProvider.Persist(persist);
        }

        /// <summary>
        /// Asynchronously persists changes through both the event store provider and the next provider.
        /// </summary>
        /// <param name="persist">The persist operation to apply.</param>
        public async Task PersistAsync(Persist persist)
        {
            await ThisProvider.PersistAsync(persist);
            await NextProvider.PersistAsync(persist);
        }
    }
}