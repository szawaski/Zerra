// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    /// <summary>
    /// A strongly-typed persist operation that removes records identified by their IDs;
    /// the model type is inferred from <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of model being deleted.</typeparam>
    public sealed class DeleteByID<TModel> : Persist<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance for a single ID; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(object id) : this(null, null, id, null) { }
        /// <summary>Initializes a new instance for a single ID with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, object id) : this(eventName, null, id, null) { }
        /// <summary>Initializes a new instance for a single ID with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName,object? source, object id) : this(eventName, source, id, null) { }
        /// <summary>Initializes a new instance for a single ID with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public DeleteByID(object id, Graph<TModel>? graph) : this(null, null, id, graph) { }
        /// <summary>Initializes a new instance for a single ID with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, object id, Graph<TModel>? graph) : this(eventName, null, id, graph) { }
        /// <summary>
        /// Initializes a new instance for a single ID with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(string? eventName, object? source, object id, Graph<TModel>? graph)
            : base(PersistOperation.Delete, eventName, source, null, [id], graph)
        {
        }

        /// <summary>Initializes a new instance for a single ID using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(PersistEvent @event, object id) : this(@event, id, null) { }
        /// <summary>
        /// Initializes a new instance for a single ID using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(PersistEvent @event, object id, Graph<TModel>? graph)
            : base(PersistOperation.Delete, @event, null, [id], graph)
        {
        }

        /// <summary>Initializes a new instance for multiple IDs; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(ICollection ids) : this(null, null, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, ICollection ids) : this(eventName, null, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, object? source, ICollection ids) : this(eventName, source, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public DeleteByID(ICollection ids, Graph<TModel>? graph) : this((string?)null, ids, graph) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, ICollection ids, Graph<TModel>? graph) : this(eventName, null, ids, graph) { }
        /// <summary>
        /// Initializes a new instance for multiple IDs with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(string? eventName, object? source, ICollection ids, Graph<TModel>? graph)
            : base(PersistOperation.Delete, eventName, source, null, ids.Cast<object>().ToArray(), graph)
        {
        }

        /// <summary>Initializes a new instance for multiple IDs using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(PersistEvent @event, ICollection ids) : this(@event, ids, null) { }
        /// <summary>
        /// Initializes a new instance for multiple IDs using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(PersistEvent @event, ICollection ids, Graph<TModel>? graph)
            : base(PersistOperation.Delete, @event, null, ids.Cast<object>().ToArray(), graph)
        {
        }
    }
}
