// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Gets the count of models matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The count of models matching the criteria.</returns>
        long TemporalCount<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)Query(new Query<TModel>(QueryOperation.Count, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, null, null, null, null))!;

        /// <summary>
        /// Gets the count of models matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The count of models matching the criteria.</returns>
        long TemporalCount<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)Query(new Query<TModel>(QueryOperation.Count, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, null, null, null, null))!;


        /// <summary>
        /// Asynchronously gets the count of models matching the specified date-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalDateFrom">The starting date for the temporal range.</param>
        /// <param name="temporalDateTo">The ending date for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of models matching the criteria.</returns>
        async Task<long> TemporalCountAsync<TModel>(TemporalOrder temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)(await QueryAsync(new Query<TModel>(QueryOperation.Count, temporalOrder, temporalDateFrom, temporalDateTo, null, null, temporalSkip, temporalTake, where, null, null, null, null)))!;

        /// <summary>
        /// Asynchronously gets the count of models matching the specified number-based temporal query criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="temporalOrder">The order in which to process temporal data.</param>
        /// <param name="temporalNumberFrom">The starting number for the temporal range.</param>
        /// <param name="temporalNumberTo">The ending number for the temporal range.</param>
        /// <param name="temporalSkip">The number of temporal entries to skip.</param>
        /// <param name="temporalTake">The number of temporal entries to take.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of models matching the criteria.</returns>
        async Task<long> TemporalCountAsync<TModel>(TemporalOrder temporalOrder, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)(await QueryAsync(new Query<TModel>(QueryOperation.Count, temporalOrder, null, null, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, null, null, null, null)))!;
    }
}
