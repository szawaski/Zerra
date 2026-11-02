// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A member an update by condition sets, and the expression over the model it is set from.
    /// </summary>
    public sealed class ProjectionMember
    {
        /// <summary>The member set.</summary>
        public MemberDetail MemberDetail { get; }
        /// <summary>The expression the member is set from.</summary>
        public Expression Expression { get; }
        /// <summary>The parameter of <see cref="Expression"/>.</summary>
        public ParameterExpression Parameter { get; }

        internal ProjectionMember(MemberDetail memberDetail, Expression expression, ParameterExpression parameter)
        {
            this.MemberDetail = memberDetail;
            this.Expression = expression;
            this.Parameter = parameter;
        }
    }
}
