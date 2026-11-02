// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Asynchronous endings for a query from <see cref="IRepo.Query{TModel}"/>.
    /// </summary>
    public static class RepoQueryableExtensions
    {
        private static RepoQueryProvider Provider<T>(IQueryable<T> source)
        {
            if (source.Provider is not RepoQueryProvider provider)
                throw new NotSupportedException($"The query isn't from {nameof(IRepo)}.{nameof(IRepo.Query)}");
            return provider;
        }
        private static async Task<TResult> Value<TSource, TResult>(IQueryable<TSource> source, LinqTerminal terminal, LambdaExpression? argument, CancellationToken cancellationToken)
        {
            var result = await Provider(source).ExecuteAsync<TResult>(source.Expression, terminal, argument, cancellationToken);
            return result is null ? default! : (TResult)result;
        }

        /// <summary>Asynchronously runs the query and returns its elements as an array.</summary>
        public static async Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToArray();
        /// <summary>Asynchronously runs the query and returns its elements as a list.</summary>
        public static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToList();

        /// <summary>Asynchronously runs the query and returns its elements as a set.</summary>
        public static async Task<HashSet<TSource>> ToHashSetAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToHashSet();
        /// <summary>Asynchronously runs the query and returns its elements as a set with a comparer.</summary>
        public static async Task<HashSet<TSource>> ToHashSetAsync<TSource>(this IQueryable<TSource> source, IEqualityComparer<TSource>? comparer, CancellationToken cancellationToken = default) => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToHashSet(comparer);
        /// <summary>Asynchronously runs the query and returns its elements by a key taken from each after it is read.</summary>
        public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default) where TKey : notnull => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToDictionary(keySelector);
        /// <summary>Asynchronously runs the query and returns its elements by a key taken from each after it is read, with a comparer.</summary>
        public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, CancellationToken cancellationToken = default) where TKey : notnull => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToDictionary(keySelector, comparer);
        /// <summary>Asynchronously runs the query and returns a value of each element by a key, both taken after it is read.</summary>
        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default) where TKey : notnull => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToDictionary(keySelector, elementSelector);
        /// <summary>Asynchronously runs the query and returns a value of each element by a key, both taken after it is read, with a comparer.</summary>
        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer, CancellationToken cancellationToken = default) where TKey : notnull => (await Provider(source).ExecuteSequenceAsync<TSource>(source.Expression, cancellationToken)).ToDictionary(keySelector, elementSelector, comparer);

        /// <summary>Asynchronously returns the first element, and throws when there is none.</summary>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.First, null, cancellationToken);
        /// <summary>Asynchronously returns the first element meeting the condition, and throws when there is none.</summary>
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.First, predicate, cancellationToken);
        /// <summary>Asynchronously returns the first element, or the default when there is none.</summary>
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.FirstOrDefault, null, cancellationToken);
        /// <summary>Asynchronously returns the first element meeting the condition, or the default when there is none.</summary>
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.FirstOrDefault, predicate, cancellationToken);
        /// <summary>Asynchronously returns the only element, and throws when there is none or more than one.</summary>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.Single, null, cancellationToken);
        /// <summary>Asynchronously returns the only element meeting the condition, and throws when there is none or more than one.</summary>
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.Single, predicate, cancellationToken);
        /// <summary>Asynchronously returns the only element, or the default when there is none, and throws when there is more than one.</summary>
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.SingleOrDefault, null, cancellationToken);
        /// <summary>Asynchronously returns the only element meeting the condition, or the default when there is none, and throws when there is more than one.</summary>
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.SingleOrDefault, predicate, cancellationToken);

        /// <summary>Asynchronously returns the last element of an ordered query, and throws when there is none.</summary>
        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.Last, null, cancellationToken);
        /// <summary>Asynchronously returns the last element of an ordered query meeting the condition, and throws when there is none.</summary>
        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource>(source, LinqTerminal.Last, predicate, cancellationToken);
        /// <summary>Asynchronously returns the last element of an ordered query, or the default when there is none.</summary>
        public static Task<TSource?> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.LastOrDefault, null, cancellationToken);
        /// <summary>Asynchronously returns the last element of an ordered query meeting the condition, or the default when there is none.</summary>
        public static Task<TSource?> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, TSource?>(source, LinqTerminal.LastOrDefault, predicate, cancellationToken);
        /// <summary>Asynchronously returns the element at an index, and throws when there is none.</summary>
        public static async Task<TSource> ElementAtAsync<TSource>(this IQueryable<TSource> source, int index, CancellationToken cancellationToken = default)
            => (TSource)(await Provider(source).ExecuteAsync<TSource>(Expression.Call(null, new Func<IQueryable<TSource>, int, TSource>(System.Linq.Queryable.ElementAt).Method, source.Expression, Expression.Constant(index)), null, null, cancellationToken))!;
        /// <summary>Asynchronously returns the element at an index, or the default when there is none.</summary>
        public static async Task<TSource?> ElementAtOrDefaultAsync<TSource>(this IQueryable<TSource> source, int index, CancellationToken cancellationToken = default)
        {
            var result = await Provider(source).ExecuteAsync<TSource?>(Expression.Call(null, new Func<IQueryable<TSource>, int, TSource?>(System.Linq.Queryable.ElementAtOrDefault).Method, source.Expression, Expression.Constant(index)), null, null, cancellationToken);
            return result is null ? default : (TSource)result;
        }
        /// <summary>Asynchronously returns whether a value selected by the query is the one given.</summary>
        public static async Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken cancellationToken = default)
            => (bool)(await Provider(source).ExecuteAsync<bool>(Expression.Call(null, new Func<IQueryable<TSource>, TSource, bool>(System.Linq.Queryable.Contains).Method, source.Expression, Expression.Constant(item, typeof(TSource))), null, null, cancellationToken))!;

        /// <summary>Asynchronously returns whether there is any element.</summary>
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, bool>(source, LinqTerminal.Any, null, cancellationToken);
        /// <summary>Asynchronously returns whether any element meets the condition.</summary>
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, bool>(source, LinqTerminal.Any, predicate, cancellationToken);
        /// <summary>Asynchronously returns whether every element meets the condition.</summary>
        public static async Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => !await Value<TSource, bool>(source, LinqTerminal.Any, Expression.Lambda<Func<TSource, bool>>(Expression.Not(predicate.Body), predicate.Parameters), cancellationToken);
        /// <summary>Asynchronously returns the number of elements.</summary>
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, int>(source, LinqTerminal.Count, null, cancellationToken);
        /// <summary>Asynchronously returns the number of elements meeting the condition.</summary>
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, int>(source, LinqTerminal.Count, predicate, cancellationToken);
        /// <summary>Asynchronously returns the number of elements as a long.</summary>
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => Value<TSource, long>(source, LinqTerminal.LongCount, null, cancellationToken);
        /// <summary>Asynchronously returns the number of elements meeting the condition as a long.</summary>
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => Value<TSource, long>(source, LinqTerminal.LongCount, predicate, cancellationToken);

        /// <summary>Asynchronously returns the smallest value over the elements.</summary>
        public static Task<TResult?> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) => Value<TSource, TResult?>(source, LinqTerminal.Min, selector, cancellationToken);
        /// <summary>Asynchronously returns the largest value over the elements.</summary>
        public static Task<TResult?> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) => Value<TSource, TResult?>(source, LinqTerminal.Max, selector, cancellationToken);

        /// <summary>Asynchronously adds the value of each element.</summary>
        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) => Value<TSource, int>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element, nulls left out.</summary>
        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) => Value<TSource, int?>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element.</summary>
        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) => Value<TSource, long>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element, nulls left out.</summary>
        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) => Value<TSource, long?>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element.</summary>
        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) => Value<TSource, decimal>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element, nulls left out.</summary>
        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) => Value<TSource, decimal?>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element.</summary>
        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) => Value<TSource, double>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element, nulls left out.</summary>
        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) => Value<TSource, double?>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element.</summary>
        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) => Value<TSource, float>(source, LinqTerminal.Sum, selector, cancellationToken);
        /// <summary>Asynchronously adds the value of each element, nulls left out.</summary>
        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) => Value<TSource, float?>(source, LinqTerminal.Sum, selector, cancellationToken);

        /// <summary>Asynchronously averages the value of each element.</summary>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) => Value<TSource, double>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element, nulls left out.</summary>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) => Value<TSource, double?>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element.</summary>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) => Value<TSource, double>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element, nulls left out.</summary>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) => Value<TSource, double?>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element.</summary>
        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) => Value<TSource, decimal>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element, nulls left out.</summary>
        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) => Value<TSource, decimal?>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element.</summary>
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) => Value<TSource, double>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element, nulls left out.</summary>
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) => Value<TSource, double?>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element.</summary>
        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) => Value<TSource, float>(source, LinqTerminal.Average, selector, cancellationToken);
        /// <summary>Asynchronously averages the value of each element, nulls left out.</summary>
        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) => Value<TSource, float?>(source, LinqTerminal.Average, selector, cancellationToken);
    }
}
