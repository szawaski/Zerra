// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// The provider of a repository LINQ query: it makes the queryables of a chain, and runs the whole query with the model and result types known.
    /// </summary>
    internal abstract class RepoQueryProvider : IQueryProvider
    {
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new RepoQueryable<TElement>(this, expression);
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException("A repository query is typed");
        public TResult Execute<TResult>(Expression expression)
        {
            var result = Execute<TResult>(expression, null, null);
            return result is null ? default! : (TResult)result;
        }
        public object? Execute(Expression expression) => throw new NotSupportedException("A repository query is typed");

        /// <summary>Reads a query into what a condition of another query looks in.</summary>
        internal abstract LinqSubquery ReadSubquery(Expression expression);
        internal abstract LinqSet ReadSet(LinqSetOperation operation, LinqQuery left, Expression expression);

        /// <summary>Runs a query ending in a value: a first or single result, a count, whether any, or a calculation.</summary>
        /// <typeparam name="TResult">The type of the value.</typeparam>
        /// <param name="expression">The query, the terminal call included unless given apart.</param>
        /// <param name="terminal">The terminal call when it isn't in the expression, as the Async methods pass it.</param>
        /// <param name="argument">The terminal call's condition or value when it has one.</param>
        /// <returns>The value, boxed, or <see langword="null"/> for no result.</returns>
        public abstract object? Execute<TResult>(Expression expression, LinqTerminal? terminal, LambdaExpression? argument);
        /// <summary>Asynchronously runs a query ending in a value.</summary>
        public abstract Task<object?> ExecuteAsync<TResult>(Expression expression, LinqTerminal? terminal, LambdaExpression? argument, CancellationToken cancellationToken);
        /// <summary>Runs a query giving a sequence: the models, projections of them, their different values, or results per group.</summary>
        /// <typeparam name="TElement">The type of the elements.</typeparam>
        /// <param name="expression">The query.</param>
        /// <returns>The elements.</returns>
        public abstract IEnumerable<TElement> ExecuteSequence<TElement>(Expression expression);
        /// <summary>Asynchronously runs a query giving a sequence.</summary>
        public abstract Task<IEnumerable<TElement>> ExecuteSequenceAsync<TElement>(Expression expression, CancellationToken cancellationToken);
    }
}
