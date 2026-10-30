// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double Average<TModel>(Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double> AverageAsync<TModel>(Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double? Average<TModel>(Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double? Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double?> AverageAsync<TModel>(Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double? Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double Average<TModel>(Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double> AverageAsync<TModel>(Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double? Average<TModel>(Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double? Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double?> AverageAsync<TModel>(Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double? Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        float Average<TModel>(Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (float?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, float Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, float Value)>)Query(new CalculationGroupsQuery<TModel, TKey, float>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<float> AverageAsync<TModel>(Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (float?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, float Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, float Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, float>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        float? Average<TModel>(Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, float? Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, float? Value)>)Query(new CalculationGroupsQuery<TModel, TKey, float?>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<float?> AverageAsync<TModel>(Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, float? Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, float? Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, float?>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double Average<TModel>(Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double> AverageAsync<TModel>(Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        double? Average<TModel>(Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double? Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)Query(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<double?> AverageAsync<TModel>(Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double? Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, double? Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, double?>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        decimal Average<TModel>(Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (decimal?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, decimal Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, decimal Value)>)Query(new CalculationGroupsQuery<TModel, TKey, decimal>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<decimal> AverageAsync<TModel>(Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = (decimal?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
            if (result is null)
                throw new InvalidOperationException("Sequence contains no elements");
            return result.Value;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, decimal Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, decimal Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, decimal>(QueryOperation.Average, where, groupBy, value)))!;
        }

        /// <summary>Averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        decimal? Average<TModel>(Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal?)Query(new Query<TModel>(QueryOperation.Average, where, null, value));
        }

        /// <summary>Averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, decimal? Value)> Average<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, decimal? Value)>)Query(new CalculationGroupsQuery<TModel, TKey, decimal?>(QueryOperation.Average, where, groupBy, value))!;
        }

        /// <summary>Asynchronously averages <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The average, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<decimal?> AverageAsync<TModel>(Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal?)(await QueryAsync(new Query<TModel>(QueryOperation.Average, where, null, value)));
        }

        /// <summary>Asynchronously averages <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, decimal? Value)>> AverageAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, decimal? Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, decimal?>(QueryOperation.Average, where, groupBy, value)))!;
        }
    }
}
