// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the type of read operation to perform against the data store.
    /// </summary>
    public enum QueryOperation : byte
    {
        /// <summary>Retrieves multiple matching records.</summary>
        Many,
        /// <summary>Retrieves the first matching record, or <see langword="null"/> if none.</summary>
        First,
        /// <summary>Retrieves exactly one matching record.</summary>
        Single,
        /// <summary>Determines whether any records match the criteria.</summary>
        Any,
        /// <summary>Returns the count of matching records.</summary>
        Count,

        /// <summary>Retrieves multiple records from the event store.</summary>
        EventMany,
        /// <summary>Retrieves the first record from the event store.</summary>
        EventFirst,
        /// <summary>Retrieves exactly one record from the event store.</summary>
        EventSingle,
        /// <summary>Determines whether any event-store records match the criteria.</summary>
        EventAny,
        /// <summary>Returns the count of matching event-store records.</summary>
        EventCount,

        /// <summary>Adds the value of each matching record, in all or per group.</summary>
        Sum,
        /// <summary>Averages the value of each matching record, in all or per group.</summary>
        Average,
        /// <summary>Returns the smallest value of the matching records, in all or per group.</summary>
        Min,
        /// <summary>Returns the largest value of the matching records, in all or per group.</summary>
        Max,

        /// <summary>Returns a projection of each matching record.</summary>
        Select,
        /// <summary>Returns a projection of each group of matching records, with the key and aggregates.</summary>
        Group
    }
}
