// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
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
        public void NoRelationHasNoJoin()
            => Assert.DoesNotContain("JOIN", Sql(repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0).Expression));
    }
}
