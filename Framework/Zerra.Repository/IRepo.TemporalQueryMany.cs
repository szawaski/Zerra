// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Retrieves a collection of models matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> TemporalQueryMany<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, order, skip, take, graph))!;

        /// <summary>
        /// Retrieves a collection of models matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A read-only collection of models matching the criteria.</returns>
        IReadOnlyCollection<TModel> TemporalQueryMany<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)Query(new Query<TModel>(QueryOperation.Many, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, order, skip, take, graph))!;


        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> TemporalQueryManyAsync<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, order, skip, take, graph)))!;

        /// <summary>
        /// Asynchronously retrieves a collection of models matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of models matching the criteria.</returns>
        async Task<IReadOnlyCollection<TModel>> TemporalQueryManyAsync<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null) where TModel : class, new()
            => (IReadOnlyCollection<TModel>)(await QueryAsync(new Query<TModel>(QueryOperation.Many, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, order, skip, take, graph)))!;
    }
}
