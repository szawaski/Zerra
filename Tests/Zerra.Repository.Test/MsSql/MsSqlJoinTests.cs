// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;
using Zerra.Repository.MsSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.MsSql
{
    //a relation only being read is a loop join to its primary key, anything using it leaves the choice to the optimizer
    public class MsSqlJoinTests
    {
        private static readonly IRepo repo = NewRepo();
        private static IRepo NewRepo()
        {
            var repo = Repo.New();
            var engine = new MemoryEngine();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repo.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            return repo;
        }
        private static string Sql(Expression expression, Graph? graph = null)
            => LinqMsSqlConverter.ConvertLinq(new LinqQuery<TestTypesModel, object>(expression, null, null, graph), ModelAnalyzer.GetModel(typeof(TestTypesModel))).Sql;

        [Fact]
        public void AGraphRelationIsALoopJoin()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationA);
            Assert.Contains("LEFT LOOP JOIN[TestRelations]", Sql(repo.Query<TestTypesModel>(graph).Expression, graph));
        }

        [Fact]
        public void ASelectedRelationValueIsALoopJoin()
            => Assert.Contains("LEFT LOOP JOIN[TestRelations]", Sql(repo.Query<TestTypesModel>().Select(x => x.RelationA.SomeValue).Expression));

        [Fact]
        public void AConditionOnlyOnTheModelIsALoopJoin()
            => Assert.Contains("LEFT LOOP JOIN[TestRelations]", Sql(repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0).Select(x => x.RelationA.SomeValue).Expression));

        [Fact]
        public void AConditionOnTheRelationIsTheOptimizersChoice()
        {
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => x.RelationA.SomeValue == "a").Select(x => x.KeyA).Expression);
            Assert.Contains("LEFT JOIN[TestRelations]", sql);
            Assert.DoesNotContain("LOOP", sql);
        }

        [Fact]
        public void AnOrderOnTheRelationIsTheOptimizersChoice()
        {
            var sql = Sql(repo.Query<TestTypesModel>().OrderBy(x => x.RelationA.SomeValue).Select(x => x.KeyA).Expression);
            Assert.Contains("LEFT JOIN[TestRelations]", sql);
            Assert.DoesNotContain("LOOP", sql);
        }

        [Fact]
        public void ASubqueryConditionIsTheOptimizersChoice()
        {
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => x.RelationB.Any(r => r.SomeValue == "a")).Select(x => x.RelationA.SomeValue).Expression);
            Assert.Contains("LEFT JOIN[TestRelations]", sql);
            Assert.DoesNotContain("LOOP", sql);
        }

        [Fact]
        public void NoRelationHasNoJoin()
            => Assert.DoesNotContain("JOIN", Sql(repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0).Expression));

        [Fact]
        public void AConditionDecidedByAClosureValueIsFoldedAway()
        {
            var any = true;
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0 && (x.RelationA.SomeValue == "a" || any)).Select(x => x.KeyA).Expression);
            Assert.DoesNotContain("JOIN", sql);
            Assert.DoesNotContain("1=1", sql);
        }

        [Fact]
        public void AConditionKeptByAClosureValueStays()
        {
            var any = false;
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0 && (x.RelationA.SomeValue == "a" || any)).Select(x => x.KeyA).Expression);
            Assert.Contains("LEFT JOIN[TestRelations]", sql);
            Assert.DoesNotContain("1=0", sql);
        }

        [Fact]
        public void AConditionDecidedFalseIsFalse()
        {
            var all = false;
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => x.RelationA.SomeValue == "a" && all).Select(x => x.KeyA).Expression);
            Assert.DoesNotContain("JOIN", sql);
            Assert.Contains("1=0", sql);
        }

        [Fact]
        public void AnInvertedDecidedConditionIsInverted()
        {
            var any = true;
            var sql = Sql(repo.Query<TestTypesModel>().Where(x => !(x.RelationA.SomeValue == "a" || any)).Select(x => x.KeyA).Expression);
            Assert.DoesNotContain("JOIN", sql);
            Assert.Contains("1=0", sql);
        }

        [Fact]
        public void ARelationsParentSubqueryLeavesOutAFoldedJoin()
        {
            var any = true;
            var graph = new Graph<TestTypesModel>(true, x => x.RelationB);
            var statement = Statement(repo.Query<TestTypesModel>(graph).Where(x => x.Int32Thing > 0 && (x.RelationA.SomeValue == "a" || any)).Expression, graph);
            Assert.DoesNotContain("JOIN", statement.Sql);
            var piece = Assert.Single(statement.Pieces!);
            Assert.DoesNotContain("JOIN", piece.Sql);
        }

        private static LinqStatement Statement(Expression expression, Graph? graph)
            => LinqMsSqlConverter.ConvertLinq(new LinqQuery<TestTypesModel, object>(expression, null, null, graph), ModelAnalyzer.GetModel(typeof(TestTypesModel)));

        [Fact]
        public void ARelationsParentSubqueryLeavesOutAReadOnlyJoin()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationA, x => x.RelationB);
            var statement = Statement(repo.Query<TestTypesModel>(graph).Where(x => x.Int32Thing > 0).Expression, graph);

            Assert.Contains("LEFT LOOP JOIN[TestRelations]", statement.Sql);
            var piece = Assert.Single(statement.Pieces!);
            Assert.DoesNotContain("JOIN", piece.Sql);
        }

        [Fact]
        public void ARelationsParentSubqueryKeepsAJoinItsConditionUses()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationA, x => x.RelationB);
            var statement = Statement(repo.Query<TestTypesModel>(graph).Where(x => x.RelationA.SomeValue == "a").Expression, graph);

            var piece = Assert.Single(statement.Pieces!);
            Assert.Contains("LEFT JOIN[TestRelations]", piece.Sql);
            Assert.DoesNotContain("LOOP", piece.Sql);
        }

        [Fact]
        public void ARelationsParentSubqueryKeepsThePaging()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationA, x => x.RelationB);
            var statement = Statement(repo.Query<TestTypesModel>(graph).OrderBy(x => x.KeyA).Skip(5).Take(10).Expression, graph);

            var piece = Assert.Single(statement.Pieces!);
            Assert.Contains("OFFSET", piece.Sql);
            Assert.Contains("FETCH NEXT", piece.Sql);
            Assert.DoesNotContain("JOIN", piece.Sql);
        }

        [Fact]
        public void ARelationsParentSubqueryKeepsAJoinItsOrderUses()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationA, x => x.RelationB);
            var statement = Statement(repo.Query<TestTypesModel>(graph).OrderBy(x => x.RelationA.SomeValue).Take(10).Expression, graph);

            var piece = Assert.Single(statement.Pieces!);
            Assert.Contains("LEFT JOIN[TestRelations]", piece.Sql);
            Assert.DoesNotContain("LOOP", piece.Sql);
        }
    }
}
