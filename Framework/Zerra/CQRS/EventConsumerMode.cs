// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Controls how many instances of a subscribing service receive each event.
    /// </summary>
    public enum EventConsumerMode : byte
    {
        /// <summary>
        /// Every replica of the service receives a copy of the event.
        /// A handler must be correct when all the replicas run it at the same time,
        /// so it can only do work that belongs to the replica itself, such as dropping a cache it holds in its own memory.
        /// </summary>
        PerReplica = 0,
        /// <summary>
        /// One replica of the service receives each event and the replicas compete for them, the same as a command consumer.
        /// Other services subscribed to the event still get their own copy.
        /// </summary>
        PerService = 1
    }
}
