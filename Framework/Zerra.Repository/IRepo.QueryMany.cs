// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Retrieves a collection of models with pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(int? skip, int? take) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, null, skip, take, null))!;

        /// <summary>
        /// Retrieves a collection of models using the specified ordering with optional pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(QueryOrder<TModel>? order, int? skip = null, int? take = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, order, skip, take, null))!;

        /// <summary>
        /// Retrieves a collection of models matching the specified filter criteria with optional ordering and pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, where, order, skip, take, null))!;


        /// <summary>
        /// Retrieves a collection of models with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, null, null, null, graph))!;

        /// <summary>
        /// Retrieves a collection of models with pagination and eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, null, skip, take, graph))!;

        /// <summary>
        /// Retrieves a collection of models using the specified ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, order, null, null, graph))!;

        /// <summary>
        /// Retrieves a collection of models using the specified ordering with pagination and eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, null, order, skip, take, null))!;

        /// <summary>
        /// Retrieves a collection of models matching the specified filter criteria with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, where, null, null, null, graph))!;

        /// <summary>
        /// Retrieves a collection of models matching the specified filter criteria and ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, where, order, null, null, graph))!;

        /// <summary>
        /// Retrieves a collection of models matching the specified filter criteria with ordering, pagination, and eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> QueryMany<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, where, order, skip, take, graph))!;


        /// <summary>
        /// Asynchronously retrieves a collection of models with pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(int? skip, int? take) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, null, skip, take, null)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models using the specified ordering with optional pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(QueryOrder<TModel>? order, int? skip = null, int? take = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, order, skip, take, null)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified filter criteria with optional ordering and pagination.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, where, order, skip, take, null)))!;


        /// <summary>
        /// Asynchronously retrieves a collection of models with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, null, null, null, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models with pagination and eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, null, skip, take, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models using the specified ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, order, null, null, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models using the specified ordering with pagination and eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, null, order, skip, take, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified filter criteria with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, where, null, null, null, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified filter criteria and ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, where, order, null, null, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified filter criteria and ordering with eager loading of related data.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> QueryManyAsync<TModel>(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, where, order, skip, take, graph)))!;
    }
}
