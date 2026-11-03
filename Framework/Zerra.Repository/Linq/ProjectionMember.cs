// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.ComponentModel;
using Zerra.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A member an update by condition sets, and the expression over the model it is set from.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
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

        /// <summary>
        /// Reads what an update by condition sets, <c>new TModel { Member = ..., ... }</c>, into its members.
        /// </summary>
        /// <param name="set">The members to set and their values in terms of the model.</param>
        /// <returns>The members in the order they are set.</returns>
        public static ProjectionMember[] FromSet(LambdaExpression set)
        {
            if (set.Body is not MemberInitExpression init)
                throw new NotSupportedException($"An update sets its members with new {set.ReturnType.Name} {{ Member = ..., ... }}");

            var resultDetail = TypeAnalyzer.GetTypeDetail(set.ReturnType);
            var parameter = set.Parameters[0];
            var members = new ProjectionMember[init.Bindings.Count];
            for (var i = 0; i < members.Length; i++)
            {
                if (init.Bindings[i] is not MemberAssignment assignment)
                    throw new NotSupportedException($"An update sets each member of {set.ReturnType.Name} directly");
                members[i] = new ProjectionMember(resultDetail.GetMember(assignment.Member.Name), assignment.Expression, parameter);
            }
            return members;
        }
    }
}
