// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// What a SelectMany starts a query of the related model with: the parents as a condition, and what the SelectMany itself says of the rows.
    /// </summary>
    internal sealed class LinqElements
    {
        public required MethodCallExpression Root { get; init; }
        public required LambdaExpression Condition { get; init; }
        public LambdaExpression? Where { get; init; }
        public OrderExpression[]? Orders { get; init; }
        public LambdaExpression? Select { get; init; }
        public Expression? SelectBody { get; init; }
        public ParameterExpression? SelectParameter { get; init; }
    }
}
