// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// Finds whether an expression reads given members of its parameter, so a layer can refuse to work on a column it transforms.
    /// </summary>
    internal static class LinqMemberFinder
    {
        /// <summary>
        /// Returns whether <paramref name="expression"/> reads any of <paramref name="members"/> directly from its first parameter.
        /// A member of a related model, a nested lambda's parameter, or a captured variable doesn't count.
        /// </summary>
        /// <param name="expression">The expression to search, or <see langword="null"/>.</param>
        /// <param name="members">The members to look for.</param>
        /// <returns><see langword="true"/> if the expression reads one of the members.</returns>
        public static bool ReadsMember(LambdaExpression? expression, MemberDetail[] members)
        {
            if (expression is null || members.Length == 0)
                return false;
            var finder = new MemberFinder(expression.Parameters[0], null, members);
            _ = finder.Visit(expression.Body);
            return finder.Found;
        }

        /// <summary>
        /// Returns whether <paramref name="expression"/> reads any of <paramref name="members"/> from a parameter of <paramref name="parameterType"/>, its own or a nested lambda's,
        /// such as the model inside a group projection's <c>g.Sum(x => x.Price)</c>.
        /// </summary>
        /// <param name="expression">The expression to search, or <see langword="null"/>.</param>
        /// <param name="parameterType">The type of the parameters to look at.</param>
        /// <param name="members">The members to look for.</param>
        /// <returns><see langword="true"/> if the expression reads one of the members.</returns>
        public static bool ReadsMember(LambdaExpression? expression, Type parameterType, MemberDetail[] members)
        {
            if (expression is null || members.Length == 0)
                return false;
            var finder = new MemberFinder(null, parameterType, members);
            _ = finder.Visit(expression.Body);
            return finder.Found;
        }

        /// <summary>Returns whether a projection of a model to itself, as an update by condition sets its values with, assigns any of the members.</summary>
        /// <param name="set">The projection, or <see langword="null"/> for none.</param>
        /// <param name="members">The members to look for.</param>
        /// <returns><see langword="true"/> if a member is assigned; otherwise <see langword="false"/>.</returns>
        public static bool SetsMember(LambdaExpression? set, MemberDetail[] members)
        {
            if (set is null || members.Length == 0 || set.Body is not MemberInitExpression init)
                return false;
            foreach (var binding in init.Bindings)
            {
                foreach (var member in members)
                {
                    if (member.Name == binding.Member.Name)
                        return true;
                }
            }
            return false;
        }

        private sealed class MemberFinder(ParameterExpression? parameter, Type? parameterType, MemberDetail[] members) : ExpressionVisitor
        {
            public bool Found;
            protected override Expression VisitMember(MemberExpression node)
            {
                //the model of an event query is read through the event's Model, and the event's own members aren't the model's
                var eventParameter = node.Expression is MemberExpression { Member.Name: "Model", Expression: ParameterExpression modelOf } && IsEvent(modelOf.Type) ? modelOf : null;
                bool reads;
                if (parameter is not null)
                    reads = eventParameter is not null ? eventParameter == parameter : node.Expression == parameter && !IsEvent(parameter.Type);
                else
                    reads = eventParameter is not null ? node.Expression!.Type == parameterType : node.Expression is ParameterExpression p && p.Type == parameterType;
                if (reads)
                {
                    foreach (var member in members)
                    {
                        if (member.Name == node.Member.Name)
                        {
                            Found = true;
                            break;
                        }
                    }
                }
                return base.VisitMember(node);
            }
            private static bool IsEvent(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventModel<>);
        }
    }
}
