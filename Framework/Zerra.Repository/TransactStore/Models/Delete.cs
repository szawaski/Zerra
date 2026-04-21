// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    /// <summary>
    /// A persist operation that removes one or more model records from the data store.
    /// </summary>
    public sealed class Delete : Persist
    {
        /// <summary>Initializes a new instance for a single model; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public Delete(object model) : this(null, null, model, null) { }
        /// <summary>Initializes a new instance for a single model with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public Delete(string? eventName, object model) : this(eventName, null, model, null) { }
        /// <summary>Initializes a new instance for a single model with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public Delete(string? eventName, object? source, object model) : this(eventName, source, model, null) { }
        /// <summary>Initializes a new instance for a single model with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public Delete(object model, Graph? graph) : this(null, null, model, graph) { }
        /// <summary>Initializes a new instance for a single model with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public Delete(string? eventName, object model, Graph? graph) : this(eventName, null, model, graph) { }
        /// <summary>
        /// Initializes a new instance for a single model with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to remove.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public Delete(string? eventName, object? source, object model, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, model.GetType(), [model], null, graph)
        {
        }

        /// <summary>Initializes a new instance for a single model using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public Delete(PersistEvent @event, object model) : this(@event, model, null) { }
        /// <summary>
        /// Initializes a new instance for a single model using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to remove.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public Delete(PersistEvent @event, object model, Graph? graph)
            : base(PersistOperation.Delete, @event, model.GetType(), [model], null, graph)
        {
        }

        /// <summary>Initializes a new instance for multiple models; <c>eventName</c>, <c>source</c>, and <c>graph</c> default to <see langword="null"/>.</summary>
        public Delete(IEnumerable models) : this(null, null, models, null) { }
        /// <summary>Initializes a new instance for multiple models with an event name; <c>source</c> and <c>graph</c> default to <see langword="null"/>.</summary>
        public Delete(string? eventName, IEnumerable models) : this(eventName, null, models, null) { }
        /// <summary>Initializes a new instance for multiple models with an event name and source; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public Delete(string? eventName, object? source, IEnumerable models) : this(eventName, source, models, null) { }
        /// <summary>Initializes a new instance for multiple models with a property graph; <c>eventName</c> and <c>source</c> default to <see langword="null"/>.</summary>
        public Delete(IEnumerable models, Graph? graph) : this(null, null, models, graph) { }
        /// <summary>Initializes a new instance for multiple models with an event name and property graph; <c>source</c> defaults to <see langword="null"/>.</summary>
        public Delete(string? eventName, IEnumerable models, Graph? graph) : this(eventName, null, models, graph) { }
        /// <summary>
        /// Initializes a new instance for multiple models with full parameters.
        /// </summary>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to remove.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public Delete(string? eventName, object? source, IEnumerable models, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }

        /// <summary>Initializes a new instance for multiple models using a pre-built event; <c>graph</c> defaults to <see langword="null"/>.</summary>
        public Delete(PersistEvent @event, IEnumerable models) : this(@event, models, null) { }
        /// <summary>
        /// Initializes a new instance for multiple models using a pre-built event with full parameters.
        /// </summary>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to remove.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        public Delete(PersistEvent @event, IEnumerable models, Graph? graph)
            : base(PersistOperation.Delete, @event, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }
    }
}
