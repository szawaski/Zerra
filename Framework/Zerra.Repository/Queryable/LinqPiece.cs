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
    /// <summary>
    /// A relation a LINQ query reads: its rows, filtered and ordered, are read by a statement of their own for all the parents at once, matched by the parent's key, and built by the plan.
    /// </summary>
    public sealed class LinqPiece
    {
        /// <summary>The relation member on the parent model.</summary>
        public ModelMemberDetail Relation { get; }
        /// <summary>The parent model.</summary>
        public ModelDetail ParentModel { get; }
        /// <summary>The related model.</summary>
        public ModelDetail RelatedModel { get; }
        /// <summary>Whether the relation is one related model rather than a collection.</summary>
        public bool Single { get; }
        /// <summary>The member of the parent the rows are matched by: its identity for a collection, its foreign key for a single relation.</summary>
        public ModelMemberDetail ParentKey { get; }
        /// <summary>The member of the related model the rows are matched by: its foreign key for a collection, its identity for a single relation.</summary>
        public ModelMemberDetail RelatedKey { get; }
        /// <summary>The column of the parent's statement that holds <see cref="ParentKey"/>.</summary>
        public int ParentColumn { get; internal set; }
        /// <summary>An optional condition on the rows.</summary>
        public LambdaExpression? Where { get; }
        /// <summary>An optional order of the rows.</summary>
        public QueryOrder? Order { get; }
        /// <summary>How many of each parent's rows to skip, in their order.</summary>
        public int? Skip { get; }
        /// <summary>How many of each parent's rows to read, in their order.</summary>
        public int? Take { get; }
        /// <summary>How the rows are read and built; its <see cref="LinqPlan.ParentKeyColumn"/> holds <see cref="RelatedKey"/>.</summary>
        public LinqPlan Plan { get; }

        internal LinqPiece(ModelMemberDetail relation, ModelDetail parentModel, ModelDetail relatedModel, bool single, ModelMemberDetail parentKey, ModelMemberDetail relatedKey, LambdaExpression? where, QueryOrder? order, int? skip, int? take, LinqPlan plan)
        {
            this.Relation = relation;
            this.ParentModel = parentModel;
            this.RelatedModel = relatedModel;
            this.Single = single;
            this.ParentKey = parentKey;
            this.RelatedKey = relatedKey;
            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Plan = plan;
        }
    }
}
