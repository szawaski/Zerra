// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>What a column of a LINQ statement computes.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum LinqColumnKind : byte
    {
        /// <summary>An expression over the model.</summary>
        Value,
        /// <summary>One part of the group key.</summary>
        KeyPart,
        /// <summary>The number of records in the group.</summary>
        Count,
        /// <summary>The number of records in the group that meet a condition.</summary>
        CountIf,
        /// <summary>The number of different values over the group's records.</summary>
        CountDistinct,
        /// <summary>The sum of a value over the group's records.</summary>
        Sum,
        /// <summary>The average of a value over the group's records.</summary>
        Average,
        /// <summary>The smallest value over the group's records.</summary>
        Min,
        /// <summary>The largest value over the group's records.</summary>
        Max,
        /// <summary>The values over the group's records joined into one text.</summary>
        Join
    }
}
