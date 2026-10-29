// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// What fills one member of a projection's result.
    /// </summary>
    public enum ProjectionMemberKind : byte
    {
        /// <summary>An expression over the record.</summary>
        Value,
        /// <summary>One part of the group key.</summary>
        KeyPart,
        /// <summary>The number of records in the group.</summary>
        Count,
        /// <summary>The sum of a value over the group's records.</summary>
        Sum,
        /// <summary>The average of a value over the group's records.</summary>
        Average,
        /// <summary>The smallest value over the group's records.</summary>
        Min,
        /// <summary>The largest value over the group's records.</summary>
        Max
    }
}
