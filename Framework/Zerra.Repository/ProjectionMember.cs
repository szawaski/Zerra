// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A member a projection sets, and what it is set from: an expression over the model, a part of the group key, a calculation over the group, or the results built from the rows of a related collection.
    /// </summary>
    public sealed class ProjectionMember
    {
        /// <summary>The member of the result.</summary>
        public MemberDetail MemberDetail { get; }
        /// <summary>What the member is set from.</summary>
        public ProjectionMemberKind Kind { get; }
        /// <summary>The expression over the model, the group's models, or for a collection the related rows filtered and ordered; <see langword="null"/> for a key part or a count.</summary>
        public Expression? Expression { get; }
        /// <summary>The parameter of <see cref="Expression"/>.</summary>
        public ParameterExpression? Parameter { get; }
        /// <summary>For a key part, which part.</summary>
        public int KeyPart { get; }
        /// <summary>The type of the value, or for a collection the type of each result built from a row.</summary>
        public Type Type { get; }
        /// <summary>For <see cref="ProjectionMemberKind.Collection"/>, the members of the result built from each related row.</summary>
        public ProjectionMember[]? Members { get; }
        /// <summary>For <see cref="ProjectionMemberKind.Collection"/>, the related collection of the model the rows come from.</summary>
        public ModelMemberDetail? Relation { get; }
        /// <summary>For <see cref="ProjectionMemberKind.Collection"/>, the condition on the rows, or <see langword="null"/> for all of them.</summary>
        public LambdaExpression? Where { get; }
        /// <summary>For <see cref="ProjectionMemberKind.Collection"/>, the order of the rows, or <see langword="null"/>.</summary>
        public QueryOrder? Order { get; }

        internal ProjectionMember(MemberDetail memberDetail, ProjectionMemberKind kind, Expression? expression, ParameterExpression? parameter, int keyPart, Type type)
        {
            this.MemberDetail = memberDetail;
            this.Kind = kind;
            this.Expression = expression;
            this.Parameter = parameter;
            this.KeyPart = keyPart;
            this.Type = type;
        }
        internal ProjectionMember(MemberDetail memberDetail, Expression expression, ParameterExpression parameter, Type elementType, ProjectionMember[] members, ModelMemberDetail relation, LambdaExpression? where, QueryOrder? order)
            : this(memberDetail, ProjectionMemberKind.Collection, expression, parameter, 0, elementType)
        {
            this.Members = members;
            this.Relation = relation;
            this.Where = where;
            this.Order = order;
        }

        /// <summary>
        /// Creates the collection a <see cref="ProjectionMemberKind.Collection"/> member is set to from the results built from its rows: an array when the member is one, otherwise a <see cref="List{T}"/>, which every collection interface the member can be takes.
        /// </summary>
        /// <param name="elements">The results, in order.</param>
        /// <returns>The collection.</returns>
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Results are reference types so the array and list code is shared.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed.", Justification = "List<T> of a result type is always available.")]
        public object CreateCollection(IReadOnlyList<object> elements)
        {
            var memberType = MemberDetail.Type;
            if (memberType.IsArray)
            {
                var array = Array.CreateInstanceFromArrayType(memberType, elements.Count);
                for (var i = 0; i < elements.Count; i++)
                    array.SetValue(elements[i], i);
                return array;
            }
            var listDetail = TypeAnalyzer.GetTypeDetail(memberType.IsInterface ? typeof(List<>).MakeGenericType(Type) : memberType);
            var list = (IList)listDetail.CreatorBoxed!();
            for (var i = 0; i < elements.Count; i++)
                _ = list.Add(elements[i]);
            return list;
        }
    }
}
