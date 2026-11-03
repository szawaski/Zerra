// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a materialized view of a single event record, combining metadata, the change delta, and the reconstructed model state.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by the event store.</typeparam>
    public sealed class EventModel<TModel> where TModel : class, new()
    {
        /// <summary>The unique identifier of the event.</summary>
        public Guid EventID { get; set; }
        /// <summary>The name of the event that produced this record.</summary>
        public string EventName { get; set; } = null!;

        /// <summary>The date and time at which the event occurred.</summary>
        public DateTime Date { get; set; }
        /// <summary>The sequential position of this event in the event stream.</summary>
        public ulong Number { get; set; }

        /// <summary>Indicates whether this event marks the model as deleted.</summary>
        public bool Deleted { get; set; }

        /// <summary>The partial model instance representing only the properties changed by this event, or <see langword="null"/> when it deleted the model.</summary>
        public TModel? ModelChange { get; set; }
        /// <summary>The graph describing which properties are included in <see cref="ModelChange"/>, or <see langword="null"/> if all properties are included.</summary>
        public Graph? GraphChange { get; set; }

        /// <summary>The full reconstructed model state after applying this event. When the event deleted the model, the state it deleted.</summary>
        public TModel Model { get; set; } = null!;
        /// <summary>The source object that initiated the event, or <see langword="null"/> if not recorded.</summary>
        public object? Source { get; set; }
        /// <summary>The type name of the source that initiated the event, or <see langword="null"/> if not recorded.</summary>
        public string? SourceType { get; set; }
    }
}
