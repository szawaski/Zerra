// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates the level of logging for the Bus.
    /// </summary>
    public enum BusLogging : byte
    {
        /// <summary>
        /// The sender and handler will both log commands, events, and queries.
        /// </summary>
        SenderAndHandler = 0,
        /// <summary>
        /// Only the handler will log commands, events, and queries.
        /// </summary>
        HandlerOnly = 1,
        /// <summary>
        /// No loggging will occur.
        /// </summary>
        None = 2
    }
}
