// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Retrieves the first model matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? TemporalQueryFirst<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
           => (TModel?)Query(new Query<TModel>(QueryOperation.First, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, order, null, null, graph));

        /// <summary>
        /// Retrieves the first model matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>The first model matching the criteria, or <c>null</c> if no match is found.</returns>
        TModel? TemporalQueryFirst<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) where TModel : class, new()
           => (TModel?)Query(new Query<TModel>(QueryOperation.First, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, order, null, null, graph));


        /// <summary>
        /// Asynchronously retrieves the first model matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> TemporalTemporalQueryFirstAsync<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, null, null, null, graph));

        /// <summary>
        /// Asynchronously retrieves the first model matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="graph">An optional graph specification for eager loading related data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first model matching the criteria, or <c>null</c> if no match is found.</returns>
        async Task<TModel?> TemporalTemporalQueryFirstAsync<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) where TModel : class, new()
            => (TModel?)await QueryAsync(new Query<TModel>(QueryOperation.First, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, null, null, null, graph));
    }
}
