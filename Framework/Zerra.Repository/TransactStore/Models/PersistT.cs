// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Strongly-typed base class for a write operation targeting a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The type of model being persisted.</typeparam>
    public class Persist<TModel> : Persist where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new strongly-typed persist operation; the model type is inferred from <typeparamref name="TModel"/>.
        /// </summary>
        /// <param name="operation">The persist operation type.</param>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to persist; <see langword="null"/> when deleting by ID.</param>
        /// <param name="ids">The IDs to delete; <see langword="null"/> when persisting full model instances.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        public Persist(PersistOperation operation, string? eventName, object? source, object[]? models, object[]? ids, Graph? graph)
            : base(operation, eventName, source, typeof(TModel), models, ids, graph)
        {
        }

        /// <summary>
        /// Initializes a new strongly-typed persist operation with a pre-built event; the model type is inferred from <typeparamref name="TModel"/>.
        /// </summary>
        /// <param name="operation">The persist operation type.</param>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to persist; <see langword="null"/> when deleting by ID.</param>
        /// <param name="ids">The IDs to delete; <see langword="null"/> when persisting full model instances.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        public Persist(PersistOperation operation, PersistEvent @event, object[]? models, object[]? ids, Graph? graph)
            : base(operation, @event, typeof(TModel), models, ids, graph)
        {
        }

        /// <summary>
        /// Initializes a new instance as a deep copy of an existing strongly-typed persist operation.
        /// </summary>
        /// <param name="persist">The persist operation to copy.</param>
        public Persist(Persist<TModel> persist)
            : base(persist)
        {
        }
    }
}