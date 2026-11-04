// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A query that loads models by its parts rather than from a LINQ chain: the models a provider loads with their relations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqModelQuery : LinqQuery
    {
        /// <summary>Initializes a query that loads models.</summary>
        /// <param name="modelType">The type of the models.</param>
        /// <param name="terminal">The call that ends the query: Sequence, FirstOrDefault, SingleOrDefault, LongCount, or Any.</param>
        /// <param name="where">The condition, or <see langword="null"/>.</param>
        /// <param name="order">The order, or <see langword="null"/>.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The maximum number of models.</param>
        /// <param name="graph">The members and relations to load, copied so a provider can add to it, or <see langword="null"/> for every member.</param>
        public LinqModelQuery(Type modelType, LinqTerminal terminal, LambdaExpression? where, LinqOrder[]? order, int? skip, int? take, Graph? graph)
            : base(modelType, modelType, where, order, skip, take, graph is null ? null : new Graph(graph), terminal)
        {
        }

        private protected override LambdaExpression ModelLambda(Expression body, ParameterExpression parameter) => throw new NotSupportedException("Models are loaded by their provider");
        private protected override LinqQuery SetPart(Expression expression) => throw new NotSupportedException("Models are loaded by their provider");
        private protected override LinqQuery NumberedPart(Expression expression, string operation) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object CombineSet(IEnumerable left, IEnumerable right, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object CombineJoin(IEnumerable left, IEnumerable? right, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => throw new NotSupportedException("Models are loaded by their provider");
        internal override Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object Stream(ITransactStoreEngine engine, ModelDetail modelDetail) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object StreamFrom(Func<object?> open, Action? opened) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object StreamAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object StreamFromAsync(Func<CancellationToken, Task<object?>> open, Action? opened, CancellationToken cancellationToken) => throw new NotSupportedException("Models are loaded by their provider");
        /// <inheritdoc/>
        public override object? Execute(IEnumerable<object> models, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object? ExecuteRead(IEnumerable<object> models, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
    }
}
