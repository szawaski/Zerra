// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Specifies the expected existence state of an event stream when appending or terminating events.
    /// </summary>
    public enum EventStoreState : byte
    {
        /// <summary>
        /// No constraint on stream state; the operation proceeds regardless of whether the stream exists.
        /// </summary>
        Any = 0,
        /// <summary>
        /// The stream must not already exist; the operation will fail if the stream is found.
        /// </summary>
        NotExisting = 1,
        /// <summary>
        /// The stream must already exist; the operation will fail if the stream is not found.
        /// </summary>
        Existing = 2
    }
}
