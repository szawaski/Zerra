// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents the deserialized payload of a single event entry stored in the event store.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by the event store.</typeparam>
    public sealed class EventStoreEventModelData<TModel> where TModel : class, new()
    {
        /// <summary>The source object that initiated the event, or <see langword="null"/> if not recorded.</summary>
        public object? Source { get; set; }
        /// <summary>The type name of the source that initiated the event, or <see langword="null"/> if not recorded.</summary>
        public string? SourceType { get; set; }
        /// <summary>The partial model instance representing the properties changed by this event.</summary>
        public TModel Model { get; set; } = null!;
        /// <summary>The graph describing which properties are included in <see cref="Model"/>, or <see langword="null"/> if all properties are included.</summary>
        public Graph? Graph { get; set; }
    }
}
