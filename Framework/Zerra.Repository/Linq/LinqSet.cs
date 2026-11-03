// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;

namespace Zerra.Repository.Linq
{
    /// <summary>How a set operation combines the elements of two queries.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum LinqSetOperation : byte
    {
        /// <summary>The different elements of both queries, <c>Union</c>.</summary>
        Union,
        /// <summary>Every element of the first query then of the second, <c>Concat</c>.</summary>
        Concat,
        /// <summary>The different elements of the first query that the second has, <c>Intersect</c>.</summary>
        Intersect,
        /// <summary>The different elements of the first query that the second doesn't have, <c>Except</c>.</summary>
        Except
    }

    /// <summary>
    /// Two queries a set operation combines, the first of the query's model and the second of any model building the same elements: one statement when both run as they are in the same store, otherwise each runs on its own and the elements are combined in memory by their values, as the store compares rows.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqSet
    {
        private readonly IRepoInternal repo;

        /// <summary>How the elements are combined.</summary>
        public LinqSetOperation Operation { get; }
        /// <summary>The first query, of the query's model.</summary>
        public LinqQuery Left { get; }
        /// <summary>The second query, of any model.</summary>
        public LinqQuery Right { get; }

        internal LinqSet(IRepoInternal repo, LinqSetOperation operation, LinqQuery left, LinqQuery right)
        {
            this.repo = repo;
            this.Operation = operation;
            this.Left = left;
            this.Right = right;
        }

        //a store running the first query in memory reads the second through the repository, as it reads a query a condition looks in
        internal IEnumerable ReadRight() => (IEnumerable)repo.Query(Right)!;
    }
}
