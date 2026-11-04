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
    /// <summary>A column of a LINQ statement: an expression over the model or the group, read at its index of each row.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqColumn
    {
        /// <summary>The index of the column in a row.</summary>
        public int Index { get; internal set; }
        /// <summary>What the column computes.</summary>
        public LinqColumnKind Kind { get; }
        /// <summary>The expression over the model, or the value a group aggregate takes.</summary>
        public Expression? Expression { get; }
        /// <summary>The parameter of <see cref="Expression"/>.</summary>
        public ParameterExpression? Parameter { get; }
        /// <summary>The part of the group key, for <see cref="LinqColumnKind.KeyPart"/>.</summary>
        public int KeyPart { get; }
        /// <summary>The type of the value the column gives.</summary>
        public Type Type { get; }
        /// <summary>The member of the statement's model the column is, read by name without an expression, or <see langword="null"/> for a column computed from <see cref="Expression"/>.</summary>
        public ModelMemberDetail? Member { get; }
        //a member of a related model the statement joins, which in memory comes with the related model rather than as a value of its own
        internal bool Joined { get; init; }
        /// <summary>The separator of a <see cref="LinqColumnKind.Join"/>.</summary>
        public string? Separator { get; init; }
        /// <summary>For a join, the column of the first or second query this column reads, its index there, or -1 for the column saying the query had a row; <see langword="null"/> for a column computed from <see cref="Expression"/>.</summary>
        public int? From { get; init; }
        /// <summary>For a join, whether <see cref="From"/> is of the second query.</summary>
        public bool FromRight { get; init; }
        /// <summary>Whether the column is of a query a join may have no row of, so it can be null whatever its member's type.</summary>
        public bool Optional { get; init; }
        /// <summary>The order a <see cref="LinqColumnKind.RowNumber"/> numbers the rows in, or <see langword="null"/> for the order the store reads them in.</summary>
        public LinqOrder[]? Orders { get; init; }

        internal LinqColumn(int index, LinqColumnKind kind, Expression? expression, ParameterExpression? parameter, int keyPart, Type type, ModelMemberDetail? member)
        {
            this.Index = index;
            this.Kind = kind;
            this.Expression = expression;
            this.Parameter = parameter;
            this.KeyPart = keyPart;
            this.Type = type;
            this.Member = member;
        }
    }
}
