// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Xunit;
using Zerra.Repository.Memory;
using Zerra.Repository.MsSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.MsSql
{
    //values are parameters so SQL Server reuses a statement's plan for any value
    public class MsSqlParameterTests
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
        private static ModelDetail Detail => ModelAnalyzer.GetModel(typeof(TestTypesModel));
        private static LinqStatement Convert(Expression expression, Graph? graph = null)
            => LinqMsSqlConverter.ConvertLinq(new LinqQuery<TestTypesModel, object>(expression, null, null, graph), Detail);

        [Fact]
        public void AValueIsAParameter()
        {
            var value = 987654;
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.Int32Thing == value).Expression);
            Assert.Contains("=(@p0)", statement.Sql);
            Assert.DoesNotContain("987654", statement.Sql);
            Assert.Equal(new object[] { 987654 }, statement.Parameters!);
        }

        [Fact]
        public void TheTextOfALikeIsAParameter()
        {
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.StringThing.StartsWith("a_b")).Expression);
            Assert.Contains("LIKE @p0", statement.Sql);
            Assert.Equal(new object[] { "a!_b%" }, statement.Parameters!);
        }

        [Fact]
        public void ADateIsItsText()
        {
            var value = new DateTime(2024, 5, 6, 7, 8, 9);
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.DateTimeThing > value).Expression);
            Assert.Contains(">(@p0)", statement.Sql);
            Assert.StartsWith("2024-05-06", Assert.IsType<string>(Assert.Single(statement.Parameters!)));
        }

        [Fact]
        public void ABooleanStaysInTheStatement()
        {
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.BooleanThing == true).Expression);
            Assert.DoesNotContain("@p", statement.Sql);
            Assert.Empty(statement.Parameters!);
        }

        [Fact]
        public void APageIsParameters()
        {
            var statement = Convert(repo.Query<TestTypesModel>().OrderBy(x => x.KeyA).Skip(20).Take(10).Expression);
            Assert.Contains("OFFSET @p0 ROWS", statement.Sql);
            Assert.Contains("FETCH NEXT @p1 ROWS ONLY", statement.Sql);
            Assert.Equal(new object[] { 20, 10 }, statement.Parameters!);

            statement = Convert(repo.Query<TestTypesModel>().Take(5).Expression);
            Assert.Contains("TOP(@p0)", statement.Sql);
        }

        [Fact]
        public void TheSameValueIsTheSameParameter()
        {
            //a group key is written in the select and the group by, which SQL Server matches only when they read the same parameter
            var statement = Convert(repo.Query<TestTypesModel>().GroupBy(x => x.Int32Thing / 10).Select(g => new { g.Key, Count = g.Count() }).Expression);
            Assert.Equal(new object[] { 10 }, statement.Parameters!);
            Assert.True(statement.Sql.Split("@p0").Length > 2);
        }

        [Fact]
        public void ANestedQueryAddsToTheParameters()
        {
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.Int32Thing == 5 && repo.Query<TestRelationsModel>().Where(r => r.SomeValue == "b").Select(r => r.RelationAKey).Contains(x.RelationAKey!.Value)).Expression);
            Assert.Equal(new object[] { 5, "b" }, statement.Parameters!);
            Assert.Contains("@p0", statement.Sql);
            Assert.Contains("@p1", statement.Sql);
        }

        [Fact]
        public void TheStatementsOfRelationsShareTheParameters()
        {
            var graph = new Graph<TestTypesModel>(true, x => x.RelationB);
            var statement = Convert(repo.Query<TestTypesModel>(graph).Where(x => x.Int32Thing == 5).Expression, graph);
            var piece = Assert.Single(statement.Pieces!);
            Assert.Same(statement.Parameters, piece.Parameters);
            Assert.Contains("@p0", piece.Sql);
        }

        [Fact]
        public void PastTheLimitValuesAreInTheStatement()
        {
            var values = Enumerable.Range(100_000, 2500).ToArray();
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => values.Contains(x.Int32Thing)).Expression);
            Assert.Equal(2000, statement.Parameters!.Count);
            Assert.Contains("@p1999,102000,", statement.Sql);
        }

        [Fact]
        public void AConditionOfAnUpdateAddsToTheParameters()
        {
            var parameters = new List<object>() { "first" };
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == 7;
            var sql = LinqMsSqlConverter.ConvertPersistWhere(where, Detail, parameters);
            Assert.Contains("=(@p1)", sql);
            Assert.Equal(new object[] { "first", 7 }, parameters);
        }

        internal static void TestSequence(ITransactStoreEngine engine, string connectionString)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));

            var model = TestTypesModel.Create();
            model.Int32Thing = 900_001;
            model.DateTimeThing = new DateTime(2024, 5, 6, 7, 8, 9).AddTicks(1234567);
            model.TimeOnlyThing = new TimeOnly(7, 8, 9).Add(TimeSpan.FromTicks(3_984_990));
            model.TimeSpanThing = new TimeSpan(7, 8, 9).Add(TimeSpan.FromTicks(3_984_990));
            repo.Create(model);

            //a time keeps the digits of its column, 6 here, which a parameter rounded to 3 lost
            var times = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA).Select(x => new { x.TimeOnlyThing, x.TimeSpanThing }).Single();
            Assert.Equal(model.TimeOnlyThing, times.TimeOnlyThing);
            Assert.Equal(model.TimeSpanThing, times.TimeSpanThing);

            //a date compares as the text did, rounded to the column the same way as the value saved
            Assert.Equal(model.KeyA, repo.Query<TestTypesModel>().Where(x => x.DateTimeThing == model.DateTimeThing && x.KeyA == model.KeyA).Select(x => x.KeyA).Single());

            //past the limit of parameters the values are in the statement
            var values = Enumerable.Range(1_000_000, 2500).Append(900_001).ToArray();
            Assert.Equal(model.KeyA, repo.Query<TestTypesModel>().Where(x => values.Contains(x.Int32Thing)).Select(x => x.KeyA).Single());

            //an update of different values and the reads of them run one plan each
            for (var i = 0; i < 3; i++)
            {
                model.Int16Thing = (short)(1000 + i);
                model.StringThing = "Changed" + i;
                repo.Update(model);
                var value = (short)(1000 + i);
                Assert.Equal("Changed" + i, repo.Query<TestTypesModel>().Where(x => x.Int16Thing == value && x.ByteThing == model.ByteThing).Select(x => x.StringThing).Single());
            }
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT [cp].[usecounts] FROM sys.dm_exec_cached_plans [cp] CROSS APPLY sys.dm_exec_sql_text([cp].[plan_handle]) [t] WHERE [t].[text] LIKE '%Int16Thing%=(@p_)%ByteThing%=(@p_)%' AND [t].[text] NOT LIKE '%dm_exec_cached_plans%'";
                var uses = new List<int>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        uses.Add(reader.GetInt32(0));
                }
                Assert.True(Assert.Single(uses) >= 3);
            }

            repo.Delete(model);
        }
    }
}
