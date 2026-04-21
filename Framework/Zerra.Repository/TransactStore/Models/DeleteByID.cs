// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    /// <summary>
    /// A persist operation that removes records identified by their IDs rather than by full model instances.
    /// </summary>
    public sealed class DeleteByID : Persist
    {
        /// <summary>Initializes a new instance for a single ID; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(Type modelType, object id) : this(null, null, modelType, id, null) { }
        /// <summary>Initializes a new instance for a single ID with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, Type modelType, object id) : this(eventName, null, modelType, id, null) { }
        /// <summary>Initializes a new instance for a single ID with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, object? source, Type modelType, object id) : this(eventName, source, modelType, id, null) { }
        /// <summary>Initializes a new instance for a single ID with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public DeleteByID(Type modelType, object id, Graph? graph) : this(null, null, modelType, id, graph) { }
        /// <summary>Initializes a new instance for a single ID with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, Type modelType, object id, Graph? graph) : this(eventName, null, modelType, id, graph) { }
        /// <summary>
        /// Initializes a new instance for a single ID with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="modelType">The CLR type of the model to delete.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(string? eventName, object? source, Type modelType, object id, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, modelType, null, [id], graph)
        {
        }

        /// <summary>Initializes a new instance for a single ID using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(PersistEvent @event, Type modelType, object id) : this(@event, modelType, id, null) { }
        /// <summary>
        /// Initializes a new instance for a single ID using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="modelType">The CLR type of the model to delete.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(PersistEvent @event, Type modelType, object id, Graph? graph)
            : base(PersistOperation.Delete, @event, modelType, null, [id], graph)
        {
        }

        /// <summary>Initializes a new instance for multiple IDs; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(Type modelType, ICollection ids) : this(null, null, modelType, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, Type modelType, ICollection ids) : this(eventName, null, modelType, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, object? source, Type modelType, ICollection ids) : this(eventName, source, modelType, ids, null) { }
        /// <summary>Initializes a new instance for multiple IDs with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public DeleteByID(Type modelType, ICollection ids, Graph? graph) : this((string?)null, modelType, ids, graph) { }
        /// <summary>Initializes a new instance for multiple IDs with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(string? eventName, Type modelType, ICollection ids, Graph? graph) : this(eventName, null, modelType, ids, graph) { }
        /// <summary>
        /// Initializes a new instance for multiple IDs with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="modelType">The CLR type of the model to delete.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(string? eventName, object? source, Type modelType, ICollection ids, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, modelType, null, ids.Cast<object>().ToArray(), graph)
        {
        }

        /// <summary>Initializes a new instance for multiple IDs using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public DeleteByID(PersistEvent @event, Type modelType, ICollection ids) : this(@event, modelType, ids, null) { }
        /// <summary>
        /// Initializes a new instance for multiple IDs using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="modelType">The CLR type of the model to delete.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public DeleteByID(PersistEvent @event, Type modelType, ICollection ids, Graph? graph)
            : base(PersistOperation.Delete, @event, modelType, null, ids.Cast<object>().ToArray(), graph)
        {
        }
    }
}
