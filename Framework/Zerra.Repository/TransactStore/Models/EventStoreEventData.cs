// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a raw event record stored in the event store, including its serialized payload.
    /// </summary>
    public sealed class EventStoreEventData
    {
        /// <summary>The unique identifier of the event.</summary>
        public Guid EventID { get; set; }
        /// <summary>The name of the event that produced this record.</summary>
        public string EventName { get; set; } = null!;
        /// <summary>The serialized payload of the event.</summary>
        public ReadOnlyMemory<byte> Data { get; set; }

        /// <summary>The date and time at which the event occurred.</summary>
        public DateTime Date { get; set; }
        /// <summary>The sequential position of this event in the event stream.</summary>
        public ulong Number { get; set; }
        /// <summary>Indicates whether this event marks the model as deleted.</summary>
        public bool Deleted { get; set; }
    }
}
