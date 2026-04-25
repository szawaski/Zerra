// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Immutable metadata describing the event associated with a persist operation.
    /// </summary>
    public sealed class PersistEvent
    {
        /// <summary>Gets the unique identifier of this event.</summary>
        public Guid ID { get; }
        /// <summary>Gets the human-readable name of this event.</summary>
        public string Name { get; }
        /// <summary>Gets the optional object that initiated this event.</summary>
        public object? Source { get; }

        /// <summary>
        /// Initializes a new persist event with the specified ID, name, and source.
        /// </summary>
        /// <param name="id">The unique identifier for this event.</param>
        /// <param name="name">The human-readable name of this event.</param>
        /// <param name="source">An optional object that initiated this event.</param>
        public PersistEvent(Guid id, string name, object? source)
        {
            this.ID = id;
            this.Name = name;
            this.Source = source;
        }
    }
}
