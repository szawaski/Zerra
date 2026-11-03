// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// What a query of the history of models reads, from <see cref="IRepo.TemporalQuery{TModel}"/> or <see cref="IRepo.EventQuery{TModel}"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqTemporal
    {
        /// <summary>Gets the type of the models whose history is read.</summary>
        public Type ModelType { get; }
        /// <summary>Gets whether the query gives each state with its event, as <see cref="EventModel{TModel}"/>.</summary>
        public bool Events { get; }
        /// <summary>Gets the order of the states, which says what is first.</summary>
        public TemporalOrder Order { get; }
        /// <summary>Gets the earliest date of the events to read.</summary>
        public DateTime? DateFrom { get; }
        /// <summary>Gets the latest date of the events to read.</summary>
        public DateTime? DateTo { get; }
        /// <summary>Gets the first event number to read.</summary>
        public ulong? NumberFrom { get; }
        /// <summary>Gets the last event number to read.</summary>
        public ulong? NumberTo { get; }
        /// <summary>Gets the members and relations of the model of each event to read, or <see langword="null"/>; a temporal query of models has its graph on the query.</summary>
        public Graph? Graph { get; }

        /// <summary>Initializes what a query of the history of models reads.</summary>
        /// <param name="modelType">The type of the models.</param>
        /// <param name="events">Whether the query gives each state with its event.</param>
        /// <param name="order">The order of the states.</param>
        /// <param name="dateFrom">The earliest date of the events to read.</param>
        /// <param name="dateTo">The latest date of the events to read.</param>
        /// <param name="numberFrom">The first event number to read.</param>
        /// <param name="numberTo">The last event number to read.</param>
        /// <param name="graph">The members and relations of the model of each event, for an event query.</param>
        public LinqTemporal(Type modelType, bool events, TemporalOrder order, DateTime? dateFrom, DateTime? dateTo, ulong? numberFrom, ulong? numberTo, Graph? graph)
        {
            this.ModelType = modelType;
            this.Events = events;
            this.Order = order;
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
            this.NumberFrom = numberFrom;
            this.NumberTo = numberTo;
            this.Graph = graph;
        }
    }
}
