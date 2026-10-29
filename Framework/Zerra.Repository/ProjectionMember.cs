// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// One member of a projection's result and what fills it.
    /// </summary>
    public sealed class ProjectionMember
    {
        /// <summary>Gets the member of the result that is set.</summary>
        public MemberDetail MemberDetail { get; }
        /// <summary>Gets what fills the member.</summary>
        public ProjectionMemberKind Kind { get; }
        /// <summary>Gets the expression over the record for a <see cref="ProjectionMemberKind.Value"/>, or the value an aggregate works on; <see langword="null"/> for a key part or a count.</summary>
        public Expression? Expression { get; }
        /// <summary>Gets the record parameter <see cref="Expression"/> uses.</summary>
        public ParameterExpression? Parameter { get; }
        /// <summary>Gets the index into <see cref="CalculationCommon.GetKeyParts"/> for a <see cref="ProjectionMemberKind.KeyPart"/>.</summary>
        public int KeyPart { get; }
        /// <summary>Gets the type the store returns for the member, as <see cref="CalculationCommon.GetResultType"/> gives it for an aggregate.</summary>
        public Type Type { get; }

        internal ProjectionMember(MemberDetail memberDetail, ProjectionMemberKind kind, Expression? expression, ParameterExpression? parameter, int keyPart, Type type)
        {
            this.MemberDetail = memberDetail;
            this.Kind = kind;
            this.Expression = expression;
            this.Parameter = parameter;
            this.KeyPart = keyPart;
            this.Type = type;
        }
    }
}
