// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Xunit;
using Zerra.Repository.Linq;
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
            //a datetime column converts the text
            var statement = Convert(repo.Query<TestTypesModel>().Where(x => x.DateTimeDefaultPrecisionThing > value).Expression);
            Assert.Contains(">(@p0)", statement.Sql);
            Assert.StartsWith("2024-05-06", Assert.IsType<string>(Assert.Single(statement.Parameters!)));
            //a datetime2 column has it cast to its precision, rounding as the value saved
            statement = Convert(repo.Query<TestTypesModel>().Where(x => x.DateTimeThing > value).Expression);
            Assert.Contains(">(CAST(@p0 AS datetime2(6)))", statement.Sql);
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

        private sealed class ParameterSwap(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
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

            //values of every kind past the limit, compared with what the store kept
            var stored = repo.Query<TestTypesModel>().Single(x => x.KeyA == model.KeyA);
            void PastTheLimit(Expression<Func<TestTypesModel, bool>> condition, string name)
            {
                Expression<Func<TestTypesModel, bool>> contains = x => values.Contains(x.Int32Thing);
                var where = Expression.Lambda<Func<TestTypesModel, bool>>(Expression.AndAlso(new ParameterSwap(contains.Parameters[0], condition.Parameters[0]).Visit(contains.Body), condition.Body), condition.Parameters);
                Assert.True(repo.Query<TestTypesModel>().Where(where).Select(x => x.KeyA).ToArray().Contains(model.KeyA), name);
            }
            PastTheLimit(x => x.ByteThing == stored.ByteThing, "byte");
            PastTheLimit(x => x.Int16Thing == stored.Int16Thing, "short");
            PastTheLimit(x => x.Int64Thing == stored.Int64Thing, "long");
            PastTheLimit(x => x.SingleThing < 0, "float");
            PastTheLimit(x => x.DoubleThing < 0, "double");
            PastTheLimit(x => x.DecimalThing == stored.DecimalThing, "decimal");
            PastTheLimit(x => x.CharThing == stored.CharThing, "char");
            PastTheLimit(x => x.CharThing != '\'', "quote char");
            PastTheLimit(x => x.DateTimeThing == stored.DateTimeThing, "DateTime");
            PastTheLimit(x => x.DateTimeOffsetThing <= stored.DateTimeOffsetThing, "DateTimeOffset");
            PastTheLimit(x => x.TimeSpanThing == stored.TimeSpanThing, "TimeSpan");
            PastTheLimit(x => x.DateOnlyThing == stored.DateOnlyThing, "DateOnly");
            PastTheLimit(x => x.TimeOnlyThing == stored.TimeOnlyThing, "TimeOnly");
            PastTheLimit(x => x.GuidThing == stored.GuidThing, "Guid");
            PastTheLimit(x => x.StringThing == stored.StringThing, "string");
            PastTheLimit(x => x.StringThing!.Contains("World"), "like");
            PastTheLimit(x => x.BytesThing == stored.BytesThing, "bytes");

            //a change by condition past the limit writes its values in the statement, and stores what parameters store
            var other = TestTypesModel.Create();
            other.Int32Thing = 900_002;
            repo.Create(other);
            var changes = TestTypesModel.Create();
            changes.CharThing = '\'';
            changes.StringThing = "it's";
            changes.Int32NullableThing = null;
            var names = ModelAnalyzer.GetModel(typeof(TestTypesModel)).Members.Where(x => !x.IsIdentity && !x.IsRelated && x.Name is not nameof(TestTypesModel.Int32Thing) and not nameof(TestTypesModel.RelationAKey)).Select(x => x.Name).ToArray();
            var graph = new Graph<TestTypesModel>();
            graph.AddMembers(names);
            var many = Enumerable.Range(1_000_000, 2500).Append(900_001).ToArray();
            Assert.Equal(1, repo.Update(x => many.Contains(x.Int32Thing), changes, graph));
            Assert.Equal(1, repo.Update(x => x.Int32Thing == 900_002, changes, graph));
            var literal = repo.Query<TestTypesModel>().Single(x => x.KeyA == model.KeyA);
            var parameter = repo.Query<TestTypesModel>().Single(x => x.KeyA == other.KeyA);
            foreach (var name in names)
            {
                var member = ModelAnalyzer.GetModel(typeof(TestTypesModel)).GetMember(name);
                var a = member.GetterBoxed(literal);
                var b = member.GetterBoxed(parameter);
                Assert.True(a is byte[] bytes ? bytes.SequenceEqual((byte[])b!) : Equals(a, b), $"{name}: {a} {b}");
            }
            Assert.Equal('\'', literal.CharThing);
            Assert.Equal("it's", literal.StringThing);
            Assert.Null(literal.Int32NullableThing);
            repo.Delete(other);

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

        //the parameters of a finished command are pooled for the next, so commands on many threads at once must never share one
        internal static async Task TestConcurrentAsync(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));

            var models = Enumerable.Range(0, 48).Select(i =>
            {
                var model = TestTypesModel.Create();
                model.Int32Thing = 910_000 + i;
                model.StringThing = "Concurrent" + i;
                return model;
            }).ToArray();
            await repo.CreateManyAsync<TestTypesModel>(models);

            for (var round = 0; round < 20; round++)
            {
                var tasks = new List<Task>();
                foreach (var model in models)
                {
                    var key = model.KeyA;
                    var number = model.Int32Thing;
                    var text = model.StringThing;
                    tasks.Add(Task.Run(async () =>
                    {
                        for (var i = 0; i < 20; i++)
                        {
                            var found = await repo.Query<TestTypesModel>().Where(x => x.KeyA == key && x.Int32Thing == number && x.StringThing == text).Select(x => x.KeyA).ToArrayAsync();
                            Assert.Equal(key, Assert.Single(found));
                        }
                    }));
                    tasks.Add(Task.Run(() =>
                    {
                        for (var i = 0; i < 20; i++)
                            Assert.Equal(key, repo.Query<TestTypesModel>().Where(x => x.KeyA == key && x.Int32Thing == number).Select(x => x.KeyA).Single());
                    }));
                    tasks.Add(Task.Run(async () =>
                    {
                        for (var i = 0; i < 5; i++)
                            Assert.Equal(1, await repo.UpdateAsync(x => x.KeyA == key, new TestTypesModel() { KeyA = key, Int32Thing = number }, new Graph<TestTypesModel>(x => x.Int32Thing)));
                    }));
                }
                await Task.WhenAll(tasks);
            }

            await repo.DeleteManyAsync<TestTypesModel>(models);
        }
    }
}
