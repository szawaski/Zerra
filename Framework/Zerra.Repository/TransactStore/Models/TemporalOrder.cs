// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the temporal ordering direction used when querying event history.
    /// </summary>
    public enum TemporalOrder : byte
    {
        /// <summary>Returns results starting from the most recent event. This is the default.</summary>
        Newest = 0, //Default
        /// <summary>Returns results starting from the oldest event.</summary>
        Oldest = 1
    }
}
