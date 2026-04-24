// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Retrieves a single model matching the specified filter criteria. Throws an exception if more than one match is found.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">A filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The single model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? Single<TModel>(Expression<Func<TModel, bool>> where, Graph<TModel>? graph = null) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.Single, where, null, null, null, graph));

        /// <summary>
        /// Retrieves a single model with eager loading of related data. Throws an exception if more than one model is found.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The single model, or <c>null</c> if no model is found.</returns>
        TModel? Single<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.Single, null, null, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves a single model matching the specified filter criteria. Throws an exception if more than one match is found.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">A filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the single model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> SingleAsync<TModel>(Expression<Func<TModel, bool>> where, Graph<TModel>? graph = null) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.Single, where, null, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves a single model with eager loading of related data. Throws an exception if more than one model is found.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the single model, or <c>null</c> if no model is found.</returns>
        async Task<TModel?> SingleAsync<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.Single, null, null, null, null, graph));
    }
}
