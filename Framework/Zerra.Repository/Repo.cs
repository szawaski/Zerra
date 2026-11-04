// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Zerra.Repository.Linq;
using Zerra.Repository.Transactions;

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
            //an event query's elements are the events of its model
            var modelType = query.Temporal?.ModelType ?? query.ModelType;
            if (!providers.TryGetValue(modelType, out var provider))
                throw new Exception($"No provider for {modelType.FullName}");
            if (query.Combines && !CombinedInStore(query))
            {
                query.Join?.ReadPartsInOrder();
                var left = ((IRepoInternal)this).Query(query.Set?.Left ?? query.Join!.Left);
                var rightPart = query.Set?.Right ?? query.Join!.Right;
                var right = rightPart is null ? null : ((IRepoInternal)this).Query(rightPart);
                return query.Set is not null ? query.CombineSet((IEnumerable)left!, (IEnumerable)right!, true) : query.CombineJoin((IEnumerable)left!, (IEnumerable?)right, true);
            }
            return provider.Query(query);
        }

        //a set or a join is one statement when every query of it runs as it is on one engine: no layer, no query a condition looks in, and no relation on another engine; otherwise each query runs through its own providers
        private bool CombinedInStore(LinqQuery query)
        {
            object? engine = null;
            return CombinedInStore(query, ref engine);
        }
        private bool CombinedInStore(LinqQuery query, ref object? engine)
        {
            foreach (var part in (ReadOnlySpan<LinqQuery?>)[query.Set?.Left ?? query.Join!.Left, query.Set?.Right ?? query.Join!.Right])
            {
                if (part is null)
                    continue;
                if (part.Combines)
                {
                    if (!CombinedInStore(part, ref engine))
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
            //an event query's elements are the events of its model
            var modelType = query.Temporal?.ModelType ?? query.ModelType;
            if (!providers.TryGetValue(modelType, out var provider))
                throw new Exception($"No provider for {modelType.FullName}");
            if (query.Combines && !CombinedInStore(query))
                return QueryCombinedAsync(query, cancellationToken);
            return provider.QueryAsync(query, cancellationToken);
        }
        object IRepoInternal.QueryStream(LinqQuery query)
        {
            //an event query's elements are the events of its model
            var modelType = query.Temporal?.ModelType ?? query.ModelType;
            if (!providers.TryGetValue(modelType, out var provider))
                throw new Exception($"No provider for {modelType.FullName}");
            if (query.Combines && !CombinedInStore(query))
                return query.StreamFrom(() => ((IRepoInternal)this).Query(query), null);
            return provider.QueryStream(query);
        }
        object IRepoInternal.QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            //an event query's elements are the events of its model
            var modelType = query.Temporal?.ModelType ?? query.ModelType;
            if (!providers.TryGetValue(modelType, out var provider))
                throw new Exception($"No provider for {modelType.FullName}");
            if (query.Combines && !CombinedInStore(query))
                return query.StreamFromAsync(cancellation => QueryCombinedAsync(query, cancellation), null, cancellationToken);
            return provider.QueryStreamAsync(query, cancellationToken);
        }
        private async Task<object?> QueryCombinedAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            query.Join?.ReadPartsInOrder();
            var left = await ((IRepoInternal)this).QueryAsync(query.Set?.Left ?? query.Join!.Left, cancellationToken);
            var rightPart = query.Set?.Right ?? query.Join!.Right;
            var right = rightPart is null ? null : await ((IRepoInternal)this).QueryAsync(rightPart, cancellationToken);
            return query.Set is not null ? query.CombineSet((IEnumerable)left!, (IEnumerable)right!, true) : query.CombineJoin((IEnumerable)left!, (IEnumerable?)right, true);
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
            return new RepoQueryable<TModel>(new RepoQueryProvider<TModel>(this, graph, null), null);
        }

        IQueryable<TModel> IRepo.TemporalQuery<TModel>(TemporalOrder order, DateTime? dateFrom, DateTime? dateTo, ulong? numberFrom, ulong? numberTo, Graph? graph)
        {
            if (!providers.ContainsKey(typeof(TModel)))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            return new RepoQueryable<TModel>(new RepoQueryProvider<TModel>(this, graph, new LinqTemporal(typeof(TModel), false, order, dateFrom, dateTo, numberFrom, numberTo, null)), null);
        }

        IQueryable<EventModel<TModel>> IRepo.EventQuery<TModel>(TemporalOrder order, DateTime? dateFrom, DateTime? dateTo, ulong? numberFrom, ulong? numberTo, Graph? graph)
        {
            if (!providers.ContainsKey(typeof(TModel)))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            return new RepoQueryable<EventModel<TModel>>(new RepoQueryProvider<TModel>(this, null, new LinqTemporal(typeof(TModel), true, order, dateFrom, dateTo, numberFrom, numberTo, graph is null ? null : new Graph(graph))), null);
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
