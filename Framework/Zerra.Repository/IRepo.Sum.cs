// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        int Sum<TModel>(Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (int)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, int Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, int Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (int)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<int> SumAsync<TModel>(Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (int)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, int Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, int Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (int)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        int? Sum<TModel>(Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (int?)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, int? Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, int? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (int?)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<int?> SumAsync<TModel>(Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (int?)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, int? Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, int?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, int? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (int?)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        long Sum<TModel>(Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, long Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (long)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<long> SumAsync<TModel>(Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, long Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (long)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        long? Sum<TModel>(Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long?)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, long? Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (long?)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<long?> SumAsync<TModel>(Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long?)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, long? Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, long?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (long?)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        float Sum<TModel>(Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, float Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, float Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (float)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<float> SumAsync<TModel>(Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, float Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, float Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (float)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        float? Sum<TModel>(Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float?)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, float? Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, float? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (float?)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<float?> SumAsync<TModel>(Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (float?)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, float? Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, float?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, float? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (float?)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        double Sum<TModel>(Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, double Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (double)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<double> SumAsync<TModel>(Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, double Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (double)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        double? Sum<TModel>(Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, double? Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, double? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (double?)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<double?> SumAsync<TModel>(Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (double?)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, double? Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, double?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, double? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (double?)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        decimal Sum<TModel>(Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, decimal Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, decimal Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (decimal)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<decimal> SumAsync<TModel>(Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, decimal Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, decimal Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (decimal)result);
            }
            return groups;
        }

        /// <summary>Adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The sum, or 0 when no models match.</returns>
        decimal? Sum<TModel>(Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal?)Query(new Query<TModel>(QueryOperation.Sum, where, null, value))!;
        }

        /// <summary>Adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, decimal? Value)> Sum<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.Sum, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, decimal? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (decimal?)result);
            }
            return groups;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The sum, or 0 when no models match.</returns>
        async Task<decimal?> SumAsync<TModel>(Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (decimal?)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, null, value)))!;
        }

        /// <summary>Asynchronously adds <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, decimal? Value)>> SumAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, decimal?>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.Sum, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, decimal? Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), result is null ? default! : (decimal?)result);
            }
            return groups;
        }
    }
}
