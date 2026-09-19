// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Marks a type as an event that an <see cref="AggregateRoot"/> appends to its stream.
    /// </summary>
    /// <remarks>
    /// This is not <c>Zerra.CQRS.IEvent</c> and the two are not interchangeable. A CQRS event is a bus message, delivered to every subscriber
    /// and every replica of each. An aggregate event is the aggregate's state: it is written to one stream, replayed by
    /// <see cref="AggregateRoot.Rebuild(ulong?, DateTime?)"/>, and never sent anywhere. It belongs with the aggregate rather than in a shared
    /// contracts project, since no other service reads it.
    /// <para>
    /// The marker is what source generation looks for, so an aggregate event has the type detail it needs to be serialized into the stream
    /// in a trimmed or native AOT build.
    /// </para>
    /// </remarks>
    public interface IAggregateEvent
    {
    }
}
