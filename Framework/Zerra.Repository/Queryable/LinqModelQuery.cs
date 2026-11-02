// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that loads models by its parts rather than from a LINQ chain: the history queries of an event store, and the models a provider loads with their relations.
    /// </summary>
    public sealed class LinqModelQuery : LinqQuery
    {
        /// <summary>Gets whether the query gives each model with its event rather than the model alone.</summary>
        public bool Events { get; }
        /// <summary>Gets the order of the temporal query, or <see langword="null"/> when the query isn't temporal.</summary>
        public TemporalOrder? TemporalOrder { get; }
        /// <summary>Gets the earliest date of the events to read.</summary>
        public DateTime? TemporalDateFrom { get; }
        /// <summary>Gets the latest date of the events to read.</summary>
        public DateTime? TemporalDateTo { get; }
        /// <summary>Gets the first event number to read.</summary>
        public ulong? TemporalNumberFrom { get; }
        /// <summary>Gets the last event number to read.</summary>
        public ulong? TemporalNumberTo { get; }
        /// <summary>Gets the number of events to skip.</summary>
        public int? TemporalSkip { get; }
        /// <summary>Gets the maximum number of events to read.</summary>
        public int? TemporalTake { get; }
        /// <summary>Gets whether the query reads the history of the models.</summary>
        public bool IsTemporal => TemporalOrder.HasValue;

        /// <summary>Initializes a query that loads models.</summary>
        /// <param name="modelType">The type of the models.</param>
        /// <param name="terminal">The call that ends the query: Sequence, FirstOrDefault, SingleOrDefault, LongCount, or Any.</param>
        /// <param name="where">The condition, or <see langword="null"/>.</param>
        /// <param name="order">The order, or <see langword="null"/>.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The maximum number of models.</param>
        /// <param name="graph">The members and relations to load, copied so a provider can add to it, or <see langword="null"/> for every member.</param>
        public LinqModelQuery(Type modelType, LinqTerminal terminal, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph)
            : base(modelType, modelType, where, order, skip, take, graph is null ? null : new Graph(graph), terminal)
        {
        }

        /// <summary>Initializes a query that reads the history of models.</summary>
        /// <param name="modelType">The type of the models.</param>
        /// <param name="terminal">The call that ends the query: Sequence, FirstOrDefault, SingleOrDefault, LongCount, or Any.</param>
        /// <param name="events">Whether the query gives each model with its event.</param>
        /// <param name="temporalOrder">The order of the events, or <see langword="null"/> when the query isn't temporal.</param>
        /// <param name="temporalDateFrom">The earliest date of the events to read.</param>
        /// <param name="temporalDateTo">The latest date of the events to read.</param>
        /// <param name="temporalNumberFrom">The first event number to read.</param>
        /// <param name="temporalNumberTo">The last event number to read.</param>
        /// <param name="temporalSkip">The number of events to skip.</param>
        /// <param name="temporalTake">The maximum number of events to read.</param>
        /// <param name="where">The condition, or <see langword="null"/>.</param>
        /// <param name="order">The order, or <see langword="null"/>.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The maximum number of models.</param>
        /// <param name="graph">The members and relations to load, copied so a provider can add to it, or <see langword="null"/> for every member.</param>
        public LinqModelQuery(Type modelType, LinqTerminal terminal, bool events, TemporalOrder? temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph)
            : this(modelType, terminal, where, order, skip, take, graph)
        {
            this.Events = events;
            this.TemporalOrder = temporalOrder;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;
            this.TemporalSkip = temporalSkip;
            this.TemporalTake = temporalTake;
        }

        private protected override LambdaExpression ModelLambda(Expression body, ParameterExpression parameter) => throw new NotSupportedException("Models are loaded by their provider");
        private protected override LinqQuery SetPart(Expression expression) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object CombineSet(IEnumerable left, IEnumerable right, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => throw new NotSupportedException("Models are loaded by their provider");
        internal override Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => throw new NotSupportedException("Models are loaded by their provider");
        /// <inheritdoc/>
        public override object? Execute(IEnumerable<object> models, bool caseSensitive) => throw new NotSupportedException("Models are loaded by their provider");
    }
}
