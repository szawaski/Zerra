// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a strongly-typed ordering definition for queries against <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type that the ordering applies to.</typeparam>
    public class QueryOrder<TModel> : QueryOrder
        where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of <see cref="QueryOrder{TModel}"/> with the specified ordering expressions.
        /// </summary>
        /// <param name="expressions">The ordering expressions that define the sort criteria.</param>
        public QueryOrder(params OrderExpression[] expressions)
            : base(expressions) { }

        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with one ascending sort expression.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1>(Expression<Func<TModel, T1>> expression1)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with one sort expression.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1>(Expression<Func<TModel, T1>> expression1, bool descending1)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1)
            ]);
        }

        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with two ascending sort expressions.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with two sort expressions, controlling the direction of the second.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key (ascending).</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, descending2)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with two sort expressions, controlling the direction of the first.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key (ascending).</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with two sort expressions.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, descending2)
            ]);
        }

        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three ascending sort expressions.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="expression3">An expression selecting the third sort key.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, false),
                new OrderExpression<TModel, T3>(expression3, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the first.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key (ascending).</param>
        /// <param name="expression3">An expression selecting the third sort key (ascending).</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, false),
                new OrderExpression<TModel, T3>(expression3, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the first two.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <param name="expression3">An expression selecting the third sort key (ascending).</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, descending2),
                new OrderExpression<TModel, T3>(expression3, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the first and third.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key (ascending).</param>
        /// <param name="expression3">An expression selecting the third sort key.</param>
        /// <param name="descending3"><see langword="true"/> to sort the third key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, false),
                new OrderExpression<TModel, T3>(expression3, descending3)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the second and third.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key (ascending).</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <param name="expression3">An expression selecting the third sort key.</param>
        /// <param name="descending3"><see langword="true"/> to sort the third key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, descending2),
                new OrderExpression<TModel, T3>(expression3, descending3)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the second.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key (ascending).</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <param name="expression3">An expression selecting the third sort key (ascending).</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, descending2),
                new OrderExpression<TModel, T3>(expression3, false)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions, controlling the direction of the third.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key (ascending).</param>
        /// <param name="expression2">An expression selecting the second sort key (ascending).</param>
        /// <param name="expression3">An expression selecting the third sort key.</param>
        /// <param name="descending3"><see langword="true"/> to sort the third key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, false),
                new OrderExpression<TModel, T2>(expression2, false),
                new OrderExpression<TModel, T3>(expression3, descending3)
            ]);
        }
        /// <summary>
        /// Creates a <see cref="QueryOrder{TModel}"/> with three sort expressions.
        /// </summary>
        /// <typeparam name="T1">The type of the first sort key.</typeparam>
        /// <typeparam name="T2">The type of the second sort key.</typeparam>
        /// <typeparam name="T3">The type of the third sort key.</typeparam>
        /// <param name="expression1">An expression selecting the first sort key.</param>
        /// <param name="descending1"><see langword="true"/> to sort the first key descending; otherwise ascending.</param>
        /// <param name="expression2">An expression selecting the second sort key.</param>
        /// <param name="descending2"><see langword="true"/> to sort the second key descending; otherwise ascending.</param>
        /// <param name="expression3">An expression selecting the third sort key.</param>
        /// <param name="descending3"><see langword="true"/> to sort the third key descending; otherwise ascending.</param>
        /// <returns>A new <see cref="QueryOrder{TModel}"/> instance.</returns>
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(
            [
                new OrderExpression<TModel, T1>(expression1, descending1),
                new OrderExpression<TModel, T2>(expression2, descending2),
                new OrderExpression<TModel, T3>(expression3, descending3)
            ]);
        }
    }
}

