// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>A statement a store runs for a LINQ query, with the plan that reads its rows and the statements of the relations the plan reads, one per piece, whose rows are those of the parents this statement selects.</summary>
    public sealed class LinqStatement
    {
        /// <summary>The SQL.</summary>
        public string Sql { get; }
        /// <summary>The plan of the rows.</summary>
        public LinqPlan Plan { get; }
        /// <summary>The statements of <see cref="LinqPlan.Pieces"/> in order, or <see langword="null"/> for none; each is run before this statement so each row builds its element whole.</summary>
        public LinqStatement[]? Pieces { get; }
        /// <summary>Initializes a statement.</summary>
        public LinqStatement(string sql, LinqPlan plan, LinqStatement[]? pieces)
        {
            this.Sql = sql;
            this.Plan = plan;
            this.Pieces = pieces;
        }
    }
}
