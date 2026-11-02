// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    /// <summary>
    /// Default implementation of <see cref="IRepoSetup"/> that dispatches queries and persist operations to registered <see cref="ITransactStoreProvider{TModel}"/> instances.
    /// </summary>
    public sealed partial class Repo : IRepoSetup, IRepoInternal
    {
        private readonly RepoContext context;
        private readonly Dictionary<Type, ITransactStoreProvider> providers;

        /// <summary>
        /// Creates a new <see cref="Repo"/> instance and sets it as the static repository.
        /// </summary>
        /// <returns>The new <see cref="IRepoSetup"/> instance.</returns>
        public static IRepoSetup New()
        {
            var repo = new Repo();
            return repo;
        }

        private Repo()
        {
            this.context = new RepoContext(this);
            this.providers = new();
        }

        object? IRepoInternal.Query(LinqQuery query)
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            if (query.Set is not null && !SetInStore(query.Set))
            {
                var left = ((IRepoInternal)this).Query(query.Set.Left);
                var right = ((IRepoInternal)this).Query(query.Set.Right);
                return query.CombineSet((IEnumerable)left!, (IEnumerable)right!, true);
            }
            return provider.Query(query);
        }

        //a set is one statement when every query of it runs as it is on one engine: no layer, no query a condition looks in, and no relation on another engine; otherwise each query runs through its own providers
        private bool SetInStore(LinqSet set)
        {
            object? engine = null;
            return SetInStore(set, ref engine);
        }
        private bool SetInStore(LinqSet set, ref object? engine)
        {
            foreach (var part in (ReadOnlySpan<LinqQuery>)[set.Left, set.Right])
            {
                if (part.Set is not null)
                {
                    if (!SetInStore(part.Set, ref engine))
                        return false;
                    continue;
                }
                if (part.HasSubquery || !providers.TryGetValue(part.ModelType, out var provider) || provider is not IProviderRelation { ReadsInStore: true, RelatesAcrossEngines: false } relation || relation.Engine is null)
                    return false;
                if (engine is null)
                    engine = relation.Engine;
                else if (!ReferenceEquals(engine, relation.Engine))
                    return false;
            }
            return true;
        }

        void IRepoInternal.Persist(Persist persist)
        {
            if (!providers.TryGetValue(persist.ModelType, out var provider))
                throw new Exception($"No provider for {persist.ModelType.FullName}");
            provider.Persist(persist);
        }

        Task<object?> IRepoInternal.QueryAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            if (query.Set is not null && !SetInStore(query.Set))
                return QuerySetAsync(query, cancellationToken);
            return provider.QueryAsync(query, cancellationToken);
        }
        private async Task<object?> QuerySetAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            var left = await ((IRepoInternal)this).QueryAsync(query.Set!.Left, cancellationToken);
            var right = await ((IRepoInternal)this).QueryAsync(query.Set.Right, cancellationToken);
            return query.CombineSet((IEnumerable)left!, (IEnumerable)right!, true);
        }

        Task IRepoInternal.PersistAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (!providers.TryGetValue(persist.ModelType, out var provider))
                throw new Exception($"No provider for {persist.ModelType.FullName}");
            return provider.PersistAsync(persist, cancellationToken);
        }

        IQueryable<TModel> IRepo.Query<TModel>(Graph? graph)
        {
            if (!providers.ContainsKey(typeof(TModel)))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            return new RepoQueryable<TModel>(new RepoQueryProvider<TModel>(this, graph), null);
        }

        IRepoTransaction IRepo.BeginTransaction(System.Data.IsolationLevel isolationLevel) => RepoTransaction.Begin(isolationLevel);

        void IRepoSetup.AddProvider<TModel>(ITransactStoreProvider<TModel> provider)
        {
            var modelType = typeof(TModel);
            if (!providers.TryAdd(modelType, provider))
                throw new Exception($"Provider for {modelType.FullName} already exists");
            if (provider is BaseStore baseStore)
                baseStore.Initialize(context);
        }

        bool IRepoInternal.TryGetProvider(Type modelType, [MaybeNullWhen(false)] out ITransactStoreProvider provider)
        {
            return providers.TryGetValue(modelType, out provider);
        }
    }
}
