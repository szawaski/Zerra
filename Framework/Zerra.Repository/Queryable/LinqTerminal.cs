// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// The call that ends a LINQ query and says what it gives.
    /// </summary>
    public enum LinqTerminal : byte
    {
        /// <summary>The elements, by enumerating the query.</summary>
        Sequence,
        /// <summary>The first element, which there has to be.</summary>
        First,
        /// <summary>The first element, or the default.</summary>
        FirstOrDefault,
        /// <summary>The only element, which there has to be.</summary>
        Single,
        /// <summary>The only element, or the default.</summary>
        SingleOrDefault,
        /// <summary>The last element, read as the first of the reversed order, which there has to be.</summary>
        Last,
        /// <summary>The last element, read as the first of the reversed order, or the default.</summary>
        LastOrDefault,
        /// <summary>The element at an index, read by skipping to it, which there has to be.</summary>
        ElementAt,
        /// <summary>The element at an index, read by skipping to it, or the default.</summary>
        ElementAtOrDefault,
        /// <summary>How many elements, as an int.</summary>
        Count,
        /// <summary>How many elements, as a long.</summary>
        LongCount,
        /// <summary>Whether there is any element.</summary>
        Any,
        /// <summary>The sum of a value.</summary>
        Sum,
        /// <summary>The smallest value.</summary>
        Min,
        /// <summary>The largest value.</summary>
        Max,
        /// <summary>The average of a value.</summary>
        Average
    }
}
