// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;
using Zerra.Repository.MariaDb;
using Zerra.Repository.Memory;
using Zerra.Repository.MsSql;
using Zerra.Repository.MySql;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //a relation's statement finds its parents by identity, so the joins its parents only read aren't in that subquery at any level
    public class RelationSubqueryJoinTests
    {
        [Entity("SubqueryLookup")]
        public sealed class SubqueryLookupModel
        {
            [Identity(true)]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Entity("SubqueryParent")]
        public sealed class SubqueryParentModel
        {
            [Identity(true)]
            public int Id { get; set; }
            public int? LookupId { get; set; }
            [Relation(nameof(LookupId))]
            public SubqueryLookupModel Lookup { get; set; }
            [Relation(nameof(SubqueryChildModel.ParentId))]
            public SubqueryChildModel[] Children { get; set; }
        }

        [Entity("SubqueryChild")]
        public sealed class SubqueryChildModel
        {
            [Identity(true)]
            public int Id { get; set; }
            public int ParentId { get; set; }
            public int? LookupId { get; set; }
            [Relation(nameof(LookupId))]
            public SubqueryLookupModel Lookup { get; set; }
            [Relation(nameof(SubqueryGrandChildModel.ChildId))]
            public SubqueryGrandChildModel[] GrandChildren { get; set; }
        }

        [Entity("SubqueryGrandChild")]
        public sealed class SubqueryGrandChildModel
        {
            [Identity(true)]
            public int Id { get; set; }
            public int ChildId { get; set; }
        }

        private static readonly IRepo repo = NewRepo();
        private static IRepo NewRepo()
        {
            var repo = Repo.New();
            var engine = new MemoryEngine();
            repo.AddProvider(new TransactStoreProvider<SubqueryLookupModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SubqueryParentModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SubqueryChildModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SubqueryGrandChildModel>(engine));
            return repo;
        }

        public static TheoryData<string> Engines => new() { "MsSql", "PostgreSql", "MySql", "MariaDb" };

        private static Graph<SubqueryParentModel> Tree()
        {
            var children = new Graph<SubqueryChildModel>(true, x => x.Lookup, x => x.GrandChildren);
            var graph = new Graph<SubqueryParentModel>(true, x => x.Lookup, x => x.Children);
            graph.AddChildGraph(nameof(SubqueryParentModel.Children), children);
            return graph;
        }

        private static LinqStatement Statement(string engine, Expression expression, Graph graph)
        {
            var query = new LinqQuery<SubqueryParentModel, object>(expression, null, null, graph);
            var modelDetail = ModelAnalyzer.GetModel(typeof(SubqueryParentModel));
            return engine switch
            {
                "MsSql" => LinqMsSqlConverter.ConvertLinq(query, modelDetail),
                "PostgreSql" => LinqPostgreSqlConverter.ConvertLinq(query, modelDetail),
                "MySql" => LinqMySqlConverter.ConvertLinq(query, modelDetail),
                "MariaDb" => LinqMariaDbConverter.ConvertLinq(query, modelDetail),
                _ => throw new NotSupportedException(engine)
            };
        }

        private static int Joins(string sql) => Count(sql, "JOIN");
        private static int Count(string sql, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = sql.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public void ReadOnlyJoinsAreLeftOutAtEveryLevel(string engine)
        {
            var graph = Tree();
            var statement = Statement(engine, repo.Query<SubqueryParentModel>(graph).Where(x => x.Id > 0).Expression, graph);

            Assert.Equal(1, Joins(statement.Sql));
            var children = Assert.Single(statement.Pieces!);
            Assert.Equal(1, Joins(children.Sql));
            var grandChildren = Assert.Single(children.Pieces!);
            Assert.Equal(0, Joins(grandChildren.Sql));
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public void AJoinTheConditionUsesStaysAtEveryLevel(string engine)
        {
            var graph = Tree();
            var statement = Statement(engine, repo.Query<SubqueryParentModel>(graph).Where(x => x.Lookup.Name == "a").Expression, graph);

            var children = Assert.Single(statement.Pieces!);
            Assert.Equal(2, Joins(children.Sql));
            Assert.DoesNotContain("LOOP", children.Sql);
            var grandChildren = Assert.Single(children.Pieces!);
            Assert.Equal(1, Joins(grandChildren.Sql));
            Assert.DoesNotContain("LOOP", grandChildren.Sql);
        }

        //a join hint fixes the order of every table, so a relation's statement has none and can start from its parents
        [Fact]
        public void ARelationsStatementHasNoJoinHint()
        {
            var graph = Tree();
            var statement = Statement("MsSql", repo.Query<SubqueryParentModel>(graph).Where(x => x.Id > 0).Expression, graph);

            var children = Assert.Single(statement.Pieces!);
            Assert.Contains("LEFT LOOP JOIN[SubqueryLookup]", statement.Sql);
            Assert.Contains("LEFT JOIN[SubqueryLookup]", children.Sql);
            Assert.DoesNotContain("LOOP", children.Sql);
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public void APagedParentLeavesOutReadOnlyJoins(string engine)
        {
            var graph = Tree();
            var statement = Statement(engine, repo.Query<SubqueryParentModel>(graph).OrderBy(x => x.Id).Skip(1).Take(2).Expression, graph);

            var children = Assert.Single(statement.Pieces!);
            Assert.Equal(1, Joins(children.Sql));
            var grandChildren = Assert.Single(children.Pieces!);
            Assert.Equal(0, Joins(grandChildren.Sql));
        }

        [Theory]
        [MemberData(nameof(Engines))]
        public void AJoinThePagedOrderUsesStays(string engine)
        {
            var graph = Tree();
            var statement = Statement(engine, repo.Query<SubqueryParentModel>(graph).OrderBy(x => x.Lookup.Name).Take(2).Expression, graph);

            var children = Assert.Single(statement.Pieces!);
            Assert.Equal(2, Joins(children.Sql));
            Assert.DoesNotContain("LOOP", children.Sql);
        }
    }
}
