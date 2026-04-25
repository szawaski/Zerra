// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Specifies the date part to store for a date/time value.
    /// </summary>
    public enum StoreDatePart
    {
        /// <summary>
        /// Stores both the date and time components.
        /// </summary>
        DateTime,
        /// <summary>
        /// Stores only the date component, discarding the time.
        /// </summary>
        Date
    }
}
