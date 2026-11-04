// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    /// <summary>How a join pairs the elements of two queries.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum LinqJoinKind : byte
    {
        /// <summary>Each pair of elements whose keys are equal, <c>Join</c>.</summary>
        Inner,
        /// <summary>Each element of the first query with those of the second whose keys are equal, or with none, <c>LeftJoin</c> or <c>GroupJoin</c> then <c>SelectMany</c> of <c>DefaultIfEmpty</c>.</summary>
        Left,
        /// <summary>Each element of the second query with those of the first whose keys are equal, or with none, <c>RightJoin</c>.</summary>
        Right,
        /// <summary>Each element of the first query with the group of elements of the second whose keys are equal, <c>GroupJoin</c>.</summary>
        Group,
        /// <summary>The elements of two queries paired by their place in each order, <c>Zip</c>.</summary>
        Zip,
        /// <summary>Whether two queries have the same elements in the same order, <c>SequenceEqual</c>, read as whether any pair differs.</summary>
        SequenceEqual,
        /// <summary>The elements of a query with their place in its order, <c>Index</c>, or <c>Select</c> and <c>Where</c> taking it.</summary>
        Index,
        /// <summary>The elements of a query before the first that fails a condition, <c>TakeWhile</c>, or from it on, <c>SkipWhile</c>.</summary>
        While,
        /// <summary>The last elements of a query, <c>TakeLast</c>, all but them, <c>SkipLast</c>, or one counted from the end.</summary>
        Last,
        /// <summary>The first element of a query with each key, <c>DistinctBy</c>, <c>IntersectBy</c>, or <c>ExceptBy</c>.</summary>
        DistinctBy,
        /// <summary>The elements of a query with one more at the end, <c>Append</c>, or at the start, <c>Prepend</c>.</summary>
        Append,
        /// <summary>The first element of either query with each key, those of the first query before those of the second, <c>UnionBy</c>.</summary>
        UnionBy
    }

    /// <summary>
    /// Two queries a join pairs by their keys, the first of the query's model and the second of any model, or the queries an operation by position reads: one statement when they run as they are in the same store, otherwise each runs on its own and the elements are paired in memory.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqJoin
    {
        private readonly IRepoInternal repo;

        /// <summary>How the elements are paired.</summary>
        public LinqJoinKind Kind { get; }
        /// <summary>The first query, of the query's model.</summary>
        public LinqQuery Left { get; }
        /// <summary>The second query, of any model, or <see langword="null"/> for an operation by position on one query.</summary>
        public LinqQuery? Right { get; }
        /// <summary>The key of an element of the first query, or of either query for <see cref="LinqJoinKind.UnionBy"/>.</summary>
        public LambdaExpression? LeftKey { get; }
        /// <summary>The key of an element of the second query.</summary>
        public LambdaExpression? RightKey { get; }
        /// <summary>The element built from a pair, of <see cref="LeftElement"/> and <see cref="RightElement"/>, or <see langword="null"/> for the <see cref="TupleType"/> of them.</summary>
        public Expression? Result { get; }
        /// <summary>The ValueTuple an element is when <see cref="Result"/> is <see langword="null"/>: the place and the element for <see cref="LinqJoinKind.Index"/>, the pair for <see cref="LinqJoinKind.Zip"/>.</summary>
        public Type? TupleType { get; init; }
        /// <summary>For a join, a condition on the pair of <see cref="LeftElement"/> and <see cref="RightElement"/> besides the keys, or in place of them; for <see cref="LinqJoinKind.While"/>, and <c>Where</c> of <see cref="LinqJoinKind.Index"/>, the condition of <see cref="LeftElement"/> and its place <see cref="RightElement"/>.</summary>
        public Expression? Condition { get; init; }
        /// <summary>Whether the elements are those skipped rather than taken, for <see cref="LinqJoinKind.While"/> and <see cref="LinqJoinKind.Last"/>.</summary>
        public bool Skips { get; init; }
        /// <summary>How many elements from the end, for <see cref="LinqJoinKind.Last"/>.</summary>
        public int Count { get; init; }
        /// <summary>Whether <see cref="LinqJoinKind.Last"/> is the one element <see cref="Count"/> from the end.</summary>
        public bool At { get; init; }
        /// <summary>The element added by <see cref="LinqJoinKind.Append"/>.</summary>
        public object? Value { get; init; }
        /// <summary>Whether <see cref="Value"/> comes first.</summary>
        public bool Prepends { get; init; }
        /// <summary>The element of the first query in <see cref="Result"/>.</summary>
        public ParameterExpression LeftElement { get; }
        /// <summary>The element of the second query in <see cref="Result"/>, or for <see cref="LinqJoinKind.Group"/> the group of them.</summary>
        public ParameterExpression RightElement { get; }

        internal LinqJoin(IRepoInternal repo, LinqJoinKind kind, LinqQuery left, LinqQuery? right, LambdaExpression? leftKey, LambdaExpression? rightKey, Expression? result, ParameterExpression leftElement, ParameterExpression rightElement)
        {
            this.repo = repo;
            this.Kind = kind;
            this.Left = left;
            this.Right = right;
            this.LeftKey = leftKey;
            this.RightKey = rightKey;
            this.Result = result;
            this.LeftElement = leftElement;
            this.RightElement = rightElement;
        }

        //a store running the first query in memory reads the second through the repository, as it reads a query a condition looks in
        internal IEnumerable? ReadRight()
        {
            if (Right is null)
                return null;
            Right.ReadInNumberOrder();
            return (IEnumerable)repo.Query(Right)!;
        }
        //each query read on its own gives its rows in the order they are numbered in
        internal void ReadPartsInOrder()
        {
            Left.ReadInNumberOrder();
            Right?.ReadInNumberOrder();
        }
    }
}
