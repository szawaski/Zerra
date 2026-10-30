// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// The parts of a calculation query (a count per group, or a sum, average, minimum, or maximum over all records or per group) that are the same for every engine:
    /// the key parts of a group expression, the CLR type of a result, building the typed keys, and aggregating values in memory (<see cref="LinqExtensions.Calculate"/> calculates over a sequence).
    /// </summary>
    /// <remarks>
    /// An engine returns a calculation over all records as the boxed result, or <see langword="null"/> when nothing matched (a sum returns 0),
    /// a calculation per group as an <see cref="IReadOnlyList{T}"/> of <c>(object? Key, object? Value)</c>, the key being the value of a one-member key or an <c>object?[]</c> of the parts of several, one per <see cref="GetKeyParts"/> element,
    /// and <see cref="QueryOperation.Distinct"/> as an <see cref="IReadOnlyList{T}"/> of the keys alone.
    /// </remarks>
    public static class CalculationCommon
    {
        /// <summary>
        /// Splits a group expression into the expressions of its key parts: the arguments of an anonymous object or a <c>new ValueTuple</c>, or the whole body for a single key.
        /// </summary>
        /// <param name="groupBy">The group key expression.</param>
        /// <returns>One expression per key part, in order.</returns>
        public static Expression[] GetKeyParts(LambdaExpression groupBy)
        {
            if (groupBy.Body is NewExpression newExpression)
                return newExpression.Arguments.ToArray();
            return [groupBy.Body];
        }

        /// <summary>
        /// Returns the CLR type an engine returns for a calculation: <see cref="long"/> for a count, <see cref="double"/> for an average of integers, and otherwise the type of the value expression.
        /// </summary>
        /// <param name="operation">The calculation.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        /// <returns>The result type, nullable when the value is.</returns>
        public static Type GetResultType(QueryOperation operation, LambdaExpression? value)
        {
            switch (operation)
            {
                case QueryOperation.Count:
                case QueryOperation.CountDistinct:
                    return typeof(long);
                case QueryOperation.Sum:
                case QueryOperation.Min:
                case QueryOperation.Max:
                    return value!.ReturnType;
                case QueryOperation.Average:
                    {
                        var type = value!.ReturnType;
                        var valueType = Nullable.GetUnderlyingType(type) ?? type;
                        if (valueType != typeof(int) && valueType != typeof(long) && valueType != typeof(short) && valueType != typeof(byte) && valueType != typeof(uint) && valueType != typeof(ulong) && valueType != typeof(ushort) && valueType != typeof(sbyte))
                            return type;
                        return valueType == type ? typeof(double) : typeof(double?);
                    }
                default:
                    throw new NotSupportedException($"{operation} is not a calculation");
            }
        }

        /// <summary>
        /// Returns a function that builds a <typeparamref name="TKey"/> from the key of a row: the value itself for a key of one member, or the tuple or anonymous object of the group expression from the <c>object?[]</c> of its parts.
        /// </summary>
        /// <typeparam name="TKey">The group key type.</typeparam>
        /// <param name="groupBy">The group key expression.</param>
        /// <returns>The key builder.</returns>
        public static Func<object?, TKey> GetKeyBuilder<TKey>(LambdaExpression groupBy)
        {
            if (groupBy.Body is not NewExpression newExpression)
                return static key => (TKey)key!;

            var constructor = newExpression.Constructor ?? throw new NotSupportedException($"Cannot group by {newExpression.Type.Name}");

            //the parts are already the constructor's parameter types, and the compiler keeps the constructor it put in the expression, so this is plain reflection that AOT allows
            return key => (TKey)constructor.Invoke((object?[])key!);
        }

        /// <summary>Compiles an expression over a model to a function returning the boxed value, with the memory engine's string rules.</summary>
        /// <param name="body">The expression.</param>
        /// <param name="parameter">The model parameter the expression uses.</param>
        /// <param name="caseSensitive">Whether string comparisons are case sensitive.</param>
        /// <returns>The compiled function.</returns>
        public static Func<TModel, object?> CompileBoxed<TModel>(Expression body, ParameterExpression parameter, bool caseSensitive)
        {
            var lambda = Expression.Lambda<Func<TModel, object?>>(Expression.Convert(body, typeof(object)), parameter);
            return MemoryExpressionRewriter.Rewrite(lambda, caseSensitive).Compile();
        }

        /// <summary>Compiles an expression over a model of any type to a function taking the model as an object and returning its boxed value, rewritten as <see cref="CompileBoxed{TModel}"/> rewrites it.</summary>
        /// <param name="body">The expression.</param>
        /// <param name="parameter">The parameter of the model.</param>
        /// <param name="caseSensitive">Indicates if string comparisons are case sensitive.</param>
        /// <returns>The compiled function.</returns>
        public static Func<object, object?> CompileBoxed(Expression body, ParameterExpression parameter, bool caseSensitive)
        {
            var model = Expression.Parameter(typeof(object), "model");
            var rebound = LinqRebinder.RebindExpression(body, parameter, Expression.Convert(model, parameter.Type));
            var lambda = Expression.Lambda<Func<object, object?>>(Expression.Convert(rebound, typeof(object)), model);
            return MemoryExpressionRewriter.Rewrite(lambda, caseSensitive).Compile();
        }

        /// <summary>Calculates one result over boxed values with the same empty and null rules as LINQ: a count of all of them or of the different ones that aren't null, or a sum, average, minimum, or maximum of the ones that aren't null.</summary>
        /// <param name="operation">The calculation.</param>
        /// <param name="values">The values.</param>
        /// <param name="valueType">The type of the values.</param>
        /// <returns>The boxed result, or <see langword="null"/> when nothing but nulls is averaged or compared.</returns>
        public static object? Aggregate(QueryOperation operation, List<object?> values, Type valueType)
        {
            if (operation == QueryOperation.Count)
                return (long)values.Count;
            if (operation == QueryOperation.CountDistinct)
            {
                var distinct = new HashSet<object>();
                foreach (var item in values)
                    if (item is not null)
                        _ = distinct.Add(item);
                return (long)distinct.Count;
            }

            var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
            switch (operation)
            {
                case QueryOperation.Sum:
                    {
                        if (type == typeof(int))
                        {
                            var sum = 0;
                            foreach (var item in values)
                                if (item is not null)
                                    sum = checked(sum + (int)item);
                            return sum;
                        }
                        if (type == typeof(long))
                        {
                            var sum = 0L;
                            foreach (var item in values)
                                if (item is not null)
                                    sum = checked(sum + (long)item);
                            return sum;
                        }
                        if (type == typeof(float))
                        {
                            var sum = 0d;
                            foreach (var item in values)
                                if (item is not null)
                                    sum += (float)item;
                            return (float)sum;
                        }
                        if (type == typeof(double))
                        {
                            var sum = 0d;
                            foreach (var item in values)
                                if (item is not null)
                                    sum += (double)item;
                            return sum;
                        }
                        if (type == typeof(decimal))
                        {
                            var sum = 0m;
                            foreach (var item in values)
                                if (item is not null)
                                    sum += (decimal)item;
                            return sum;
                        }
                        throw new NotSupportedException($"Cannot sum {valueType.Name}");
                    }
                case QueryOperation.Average:
                    {
                        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
                        {
                            var sum = 0L;
                            var count = 0L;
                            foreach (var item in values)
                            {
                                if (item is not null)
                                {
                                    sum = checked(sum + Convert.ToInt64(item, CultureInfo.InvariantCulture));
                                    count++;
                                }
                            }
                            return count == 0 ? null : (double)sum / count;
                        }
                        if (type == typeof(float) || type == typeof(double))
                        {
                            var sum = 0d;
                            var count = 0L;
                            foreach (var item in values)
                            {
                                if (item is not null)
                                {
                                    sum += Convert.ToDouble(item, CultureInfo.InvariantCulture);
                                    count++;
                                }
                            }
                            if (count == 0)
                                return null;
                            return type == typeof(float) ? (float)(sum / count) : sum / count;
                        }
                        if (type == typeof(decimal))
                        {
                            var sum = 0m;
                            var count = 0L;
                            foreach (var item in values)
                            {
                                if (item is not null)
                                {
                                    sum += (decimal)item;
                                    count++;
                                }
                            }
                            return count == 0 ? null : sum / count;
                        }
                        throw new NotSupportedException($"Cannot average {valueType.Name}");
                    }
                case QueryOperation.Min:
                case QueryOperation.Max:
                    {
                        object? best = null;
                        var comparer = Comparer<object>.Default;
                        foreach (var item in values)
                        {
                            if (item is null)
                                continue;
                            if (best is null)
                            {
                                best = item;
                                continue;
                            }
                            var comparison = comparer.Compare(item, best);
                            if (operation == QueryOperation.Min ? comparison < 0 : comparison > 0)
                                best = item;
                        }
                        return best;
                    }
                default:
                    throw new NotSupportedException($"{operation} is not a calculation");
            }
        }

        internal sealed class KeyComparer(bool caseSensitive) : IEqualityComparer<object?[]>
        {
            private readonly StringComparer stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            public bool Equals(object?[]? x, object?[]? y)
            {
                if (x is null || y is null || x.Length != y.Length)
                    return x == y;
                for (var i = 0; i < x.Length; i++)
                {
                    if (x[i] is string a && y[i] is string b)
                    {
                        if (!stringComparer.Equals(a, b))
                            return false;
                    }
                    else if (!Object.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(object?[] parts)
            {
                var hash = new HashCode();
                foreach (var part in parts)
                    hash.Add(part is string text ? stringComparer.GetHashCode(text) : part?.GetHashCode() ?? 0);
                return hash.ToHashCode();
            }
        }
    }
}
