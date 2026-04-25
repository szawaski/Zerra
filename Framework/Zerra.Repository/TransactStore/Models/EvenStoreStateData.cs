// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents the current reconstructed state of a model derived from its event history.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by the event store.</typeparam>
    public sealed class EvenStoreStateData<TModel> where TModel : class, new()
    {
        /// <summary>The sequential position of the most recently applied event, or <see langword="null"/> if no events have been applied.</summary>
        public ulong? Number { get; set; }
        /// <summary>Indicates whether the model has been marked as deleted by its event history.</summary>
        public bool Deleted { get; set; }
        /// <summary>The reconstructed model instance, or <see langword="null"/> if no state has been built yet.</summary>
        public TModel? Model { get; set; }
    }
}
