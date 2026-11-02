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
    public abstract class BaseTransactStoreLayerProvider<TNextProviderInterface, TModel> : BaseLayerProvider<TNextProviderInterface>, ITransactStoreProvider<TModel>, IProviderRelation
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
        public abstract LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph);

        /// <inheritdoc/>
        public object? Engine => ProviderRelation?.Engine;
        /// <inheritdoc/>
        public bool ReadsInStore => false;

        /// <summary>Propagates the query event to the next provider in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        public abstract void OnQueryIncludingBase(Graph? graph);
        /// <summary>Propagates the post-retrieve event to the next provider in the chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>The processed models.</returns>
        public abstract IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph);

        /// <summary>Asynchronously propagates the post-retrieve event to the next provider in the chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>A task containing the processed models.</returns>
        public abstract Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph);

        /// <summary>Runs a query. A layer swaps its condition onto <see cref="LinqQuery.Where"/> for the call and puts the original back, and treats the models or event models the query gives.</summary>
        /// <param name="query">The query.</param>
        /// <returns>The elements or the value.</returns>
        public abstract object? Query(LinqQuery query);

        /// <summary>Asynchronously runs a query. A layer swaps its condition onto <see cref="LinqQuery.Where"/> for the call and puts the original back, and treats the models or event models the query gives.</summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the elements or the value.</returns>
        public abstract Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken);

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
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task PersistAsync(Persist persist, CancellationToken cancellationToken)
        {
            return persist.Operation switch
            {
                PersistOperation.Create => CreateAsync(persist, cancellationToken),
                PersistOperation.Update => UpdateAsync(persist, cancellationToken),
                PersistOperation.Delete => DeleteAsync(persist, cancellationToken),
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
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task CreateAsync(Persist persist, CancellationToken cancellationToken);
        /// <summary>Asynchronously executes an update persist operation.</summary>
        /// <param name="persist">The persist operation containing the models to update.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task UpdateAsync(Persist persist, CancellationToken cancellationToken);
        /// <summary>Asynchronously executes a delete persist operation.</summary>
        /// <param name="persist">The persist operation containing the models or IDs to delete.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task DeleteAsync(Persist persist, CancellationToken cancellationToken);
    }
}
