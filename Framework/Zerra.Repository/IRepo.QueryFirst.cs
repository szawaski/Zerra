// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Retrieves the first model using the specified ordering.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <returns>The first model, or <c>null</c> if no model is found.</returns>
        TModel? QueryFirst<TModel>(QueryOrder<TModel>? order) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, null, order, null, null, null));

        /// <summary>
        /// Retrieves the first model matching the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? QueryFirst<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, where, null, null, null, null));

        /// <summary>
        /// Retrieves the first model matching the specified filter criteria and ordering.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? QueryFirst<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, where, order, null, null, null));

        /// <summary>
        /// Retrieves the first model with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The first model, or <c>null</c> if no model is found.</returns>
        TModel? QueryFirst<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, null, null, null, null, graph));

        /// <summary>
        /// Retrieves the first model using the specified ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">A graph specification for eager loading related data.</param>
        /// <returns>The first model, or <c>null</c> if no model is found.</returns>
        TModel? QueryFirst<TModel>(QueryOrder<TModel>? order, Graph<TModel> graph) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, null, order, null, null, graph));

        /// <summary>
        /// Retrieves the first model matching the specified filter criteria with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? QueryFirst<TModel>(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, where, null, null, null, graph));

        /// <summary>
        /// Retrieves the first model matching the specified filter criteria and ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? QueryFirst<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)Query(new Query<TModel>(QueryOperation.First, where, order, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves the first model using the specified ordering.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model, or <c>null</c> if no model is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(QueryOrder<TModel>? order) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, null, order, null, null, null));

        /// <summary>
        /// Asynchronously retrieves the first model matching the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, where, null, null, null, null));

        /// <summary>
        /// Asynchronously retrieves the first model matching the specified filter criteria and ordering.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, where, order, null, null, null));

        /// <summary>
        /// Asynchronously retrieves the first model with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model, or <c>null</c> if no model is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, null, null, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves the first model using the specified ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">A graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model, or <c>null</c> if no model is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(QueryOrder<TModel>? order, Graph<TModel> graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, null, order, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves the first model matching the specified filter criteria with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, where, null, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves the first model matching the specified filter criteria and ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> QueryFirstAsync<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, where, order, null, null, graph));
    }
}
