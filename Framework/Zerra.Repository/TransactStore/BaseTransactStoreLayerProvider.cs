// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract base for layer providers that sit between callers and a next <typeparamref name="TNextProviderInterface"/> in the transact store chain.
    /// </summary>
    /// <typeparam name="TNextProviderInterface">The type of the next provider in the chain.</typeparam>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract class BaseTransactStoreLayerProvider<TNextProviderInterface, TModel> : LayerProvider<TNextProviderInterface>, ITransactStoreProvider<TModel>, IProviderRelation
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        /// <summary>The <see cref="Type"/> of <typeparamref name="TModel"/>.</summary>
        protected static readonly Type modelType = typeof(TModel);

        /// <summary>The relation provider for <typeparamref name="TModel"/>, resolved from the next provider in the chain.</summary>
        protected IProviderRelation<TModel>? ProviderRelation = null;

        /// <summary>Initializes a new instance of <see cref="BaseTransactStoreLayerProvider{TNextProviderInterface, TModel}"/> with the next provider in the chain.</summary>
        /// <param name="nextProvider">The next provider to delegate operations to.</param>
        public BaseTransactStoreLayerProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
            ProviderRelation = NextProvider as IProviderRelation<TModel>;
        }

        /// <summary>Gets the <see cref="Type"/> of the model managed by this provider.</summary>
        public Type ModelType => modelType;

        /// <summary>Returns the combined where expression from the next provider in the chain, including base type considerations.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A lambda where expression, or <see langword="null"/> if none applies.</returns>
        public virtual LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            var expression = ProviderRelation?.GetWhereExpressionIncludingBase(graph);
            return expression;
        }

        /// <summary>Propagates the query event to the next provider in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        public virtual void OnQueryIncludingBase(Graph? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        /// <summary>Propagates the post-retrieve event to the next provider in the chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>The processed models.</returns>
        public virtual IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is null)
                return models;
            return ProviderRelation.OnGetIncludingBase(models, graph);
        }

        /// <summary>Asynchronously propagates the post-retrieve event to the next provider in the chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>A task containing the processed models.</returns>
        public virtual Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is null)
                return Task.FromResult(models);
            return ProviderRelation.OnGetIncludingBaseAsync(models, graph);
        }

        /// <summary>Executes a synchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <see langword="null"/> if no result is found.</returns>
        public object? Query(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => Many(query),
                QueryOperation.First => First(query),
                QueryOperation.Single => Single(query),
                QueryOperation.Count => Count(query),
                QueryOperation.Any => Any(query),
                QueryOperation.EventMany => EventMany(query),
                _ => throw new NotImplementedException(),
            };
            ;
        }
        /// <summary>Executes an asynchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task containing the query result, or <see langword="null"/>.</returns>
        public Task<object?> QueryAsync(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => ManyAsync(query),
                QueryOperation.First => FirstAsync(query),
                QueryOperation.Single => SingleAsync(query),
                QueryOperation.Count => CountAsync(query),
                QueryOperation.Any => AnyAsync(query),
                QueryOperation.EventMany => EventManyAsync(query),
                _ => throw new NotImplementedException(),
            };
            ;
        }

        /// <summary>Executes a many query and returns the matching models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The matching models as an object.</returns>
        public abstract object Many(Query query);
        /// <summary>Executes a first query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The first matching model, or <see langword="null"/>.</returns>
        public abstract object? First(Query query);
        /// <summary>Executes a single query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The single matching model, or <see langword="null"/>.</returns>
        public abstract object? Single(Query query);
        /// <summary>Executes a count query and returns the count of matching models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The count as an object.</returns>
        public abstract object Count(Query query);
        /// <summary>Executes an any query and returns whether any matching models exist.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A boolean result as an object.</returns>
        public abstract object Any(Query query);
        /// <summary>Executes an event-many query and returns the matching event models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The matching event models as an object.</returns>
        public abstract object EventMany(Query query);

        /// <summary>Asynchronously executes a many query and returns the matching models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the matching models.</returns>
        public abstract Task<object?> ManyAsync(Query query);
        /// <summary>Asynchronously executes a first query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the first matching model, or <see langword="null"/>.</returns>
        public abstract Task<object?> FirstAsync(Query query);
        /// <summary>Asynchronously executes a single query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the single matching model, or <see langword="null"/>.</returns>
        public abstract Task<object?> SingleAsync(Query query);
        /// <summary>Asynchronously executes a count query and returns the count of matching models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the count.</returns>
        public abstract Task<object?> CountAsync(Query query);
        /// <summary>Asynchronously executes an any query and returns whether any matching models exist.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing a boolean result.</returns>
        public abstract Task<object?> AnyAsync(Query query);
        /// <summary>Asynchronously executes an event-many query and returns the matching event models.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the matching event models.</returns>
        public abstract Task<object?> EventManyAsync(Query query);

        /// <summary>Executes a synchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        public void Persist(Persist persist)
        {
            switch (persist.Operation)
            {
                case PersistOperation.Create:
                    Create(persist);
                    return;
                case PersistOperation.Update:
                    Update(persist);
                    return;
                case PersistOperation.Delete:
                    Delete(persist);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>Executes an asynchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task PersistAsync(Persist persist)
        {
            return persist.Operation switch
            {
                PersistOperation.Create => CreateAsync(persist),
                PersistOperation.Update => UpdateAsync(persist),
                PersistOperation.Delete => DeleteAsync(persist),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>Executes a create persist operation.</summary>
        /// <param name="persist">The persist operation containing the models to create.</param>
        public abstract void Create(Persist persist);
        /// <summary>Executes an update persist operation.</summary>
        /// <param name="persist">The persist operation containing the models to update.</param>
        public abstract void Update(Persist persist);
        /// <summary>Executes a delete persist operation.</summary>
        /// <param name="persist">The persist operation containing the models or IDs to delete.</param>
        public abstract void Delete(Persist persist);

        /// <summary>Asynchronously executes a create persist operation.</summary>
        /// <param name="persist">The persist operation containing the models to create.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task CreateAsync(Persist persist);
        /// <summary>Asynchronously executes an update persist operation.</summary>
        /// <param name="persist">The persist operation containing the models to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task UpdateAsync(Persist persist);
        /// <summary>Asynchronously executes a delete persist operation.</summary>
        /// <param name="persist">The persist operation containing the models or IDs to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task DeleteAsync(Persist persist);
    }
}
