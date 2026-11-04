// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //the aggregate queries through IRepo on the memory engine, the event store, the layers, and the shared pieces
    public class AggregateFunctionTests
    {
        [Entity("AggregateFunctionModel")]
        public sealed class AggregateFunctionModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public int? Maybe { get; set; }
            public decimal Money { get; set; }
            public double Ratio { get; set; }
            public bool Flag { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            public DateTime Date { get; set; }
        }

        private static AggregateFunctionModel[] Models()
        {
            var names = new string?[] { "apple", "banana", null, "Apple", "cherry" };
            var models = new AggregateFunctionModel[5];
            for (var i = 0; i < models.Length; i++)
            {
                models[i] = new AggregateFunctionModel
                {
                    ID = Guid.NewGuid(),
                    Number = i + 1,
                    Maybe = i % 2 == 0 ? i : null,
                    Money = 1.5m * i,
                    Ratio = 0.25 * i,
                    Flag = i % 2 == 0,
                    Name = names[i],
                    Date = new DateTime(2020 + i % 2, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc),
                };
            }
            return models;
        }

        private static (IRepo Repo, AggregateFunctionModel[] Models) MemoryRepo()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<AggregateFunctionModel>(new MemoryEngine()));
            var models = Models();
            repo.Create<AggregateFunctionModel>(models);
            return (repo, models);
        }

        [Fact]
        public async Task ScalarsMatchLinq()
        {
            var (repo, models) = MemoryRepo();

            Assert.Equal(models.Sum(x => x.Number), repo.Query<AggregateFunctionModel>().Sum(x => x.Number));
            Assert.Equal(models.Sum(x => x.Maybe), repo.Query<AggregateFunctionModel>().Sum(x => x.Maybe));
            Assert.Equal(models.Sum(x => x.Money), repo.Query<AggregateFunctionModel>().Sum(x => x.Money));
            Assert.Equal(models.Sum(x => x.Ratio), repo.Query<AggregateFunctionModel>().Sum(x => x.Ratio));
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Number * 2), repo.Query<AggregateFunctionModel>().Where(x => x.Flag).Sum(x => x.Number * 2));

            Assert.Equal(models.Average(x => x.Number), repo.Query<AggregateFunctionModel>().Average(x => x.Number));
            Assert.Equal(models.Average(x => x.Maybe), repo.Query<AggregateFunctionModel>().Average(x => x.Maybe));
            Assert.Equal(models.Average(x => x.Money), repo.Query<AggregateFunctionModel>().Average(x => x.Money));
            Assert.Equal(models.Average(x => x.Ratio), await repo.Query<AggregateFunctionModel>().AverageAsync(x => x.Ratio, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Equal(models.Min(x => x.Number), repo.Query<AggregateFunctionModel>().Min(x => x.Number));
            Assert.Equal(models.Max(x => x.Maybe), repo.Query<AggregateFunctionModel>().Max(x => x.Maybe));
            Assert.Equal(models.Min(x => x.Name), repo.Query<AggregateFunctionModel>().Min(x => x.Name));
            Assert.Equal(models.Max(x => x.Date), await repo.Query<AggregateFunctionModel>().MaxAsync(x => x.Date, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void EmptyMatchesLinq()
        {
            var (repo, _) = MemoryRepo();
            Expression<Func<AggregateFunctionModel, bool>> none = x => x.Number > 100;

            Assert.Equal(0, repo.Query<AggregateFunctionModel>().Where(none).Sum(x => x.Number));
            Assert.Equal(0, repo.Query<AggregateFunctionModel>().Where(none).Sum(x => x.Maybe));
            Assert.Null(repo.Query<AggregateFunctionModel>().Where(none).Average(x => x.Maybe));
            Assert.Null(repo.Query<AggregateFunctionModel>().Where(none).Max(x => x.Name));
            Assert.Throws<InvalidOperationException>(() => repo.Query<AggregateFunctionModel>().Where(none).Average(x => x.Number));
            Assert.Throws<InvalidOperationException>(() => repo.Query<AggregateFunctionModel>().Where(none).Min(x => x.Number));
            Assert.Empty(repo.Query<AggregateFunctionModel>().Where(none).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
        }

        [Fact]
        public async Task GroupsMatchLinq()
        {
            var (repo, models) = MemoryRepo();

            var counts = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, counts.Length);
            Assert.Equal(3, counts.Single(x => x.Key).Value);
            Assert.Equal(2, counts.Single(x => !x.Key).Value);

            var sums = (await repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Money) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Money), sums.Single(x => x.Key).Value);

            var averages = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Average(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => !x.Flag).Average(x => x.Number), averages.Single(x => !x.Key).Value);

            //a null key is a group, as in LINQ
            var byMaybe = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Maybe).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, byMaybe.Single(x => x.Key is null).Value);
            Assert.Equal(3, byMaybe.Count(x => x.Key is not null));

            //string keys group without case by default, like the memory engine's where
            var byName = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, byName.Single(x => x.Key == "apple").Value);
            Assert.Equal(1, byName.Single(x => x.Key is null).Value);

            var byYear = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Date.Year).Select(g => new { g.Key, Value = g.Max(x => x.Date) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => x.Date.Year == 2021).Max(x => x.Date), byYear.Single(x => x.Key == 2021).Value);

            var filtered = repo.Query<AggregateFunctionModel>().Where(x => x.Number > 1).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(3, filtered.Single(x => x.Key).Value);
            Assert.Equal(2, filtered.Single(x => !x.Key).Value);
        }

        [Fact]
        public void TupleAndAnonymousKeys()
        {
            var (repo, models) = MemoryRepo();

            //a key of several members is an anonymous object, expression trees can't hold a tuple literal
            var pairs = repo.Query<AggregateFunctionModel>().GroupBy(x => new { x.Flag, x.Maybe }).Select(g => new { g.Key.Flag, g.Key.Maybe, Value = (long)g.Count() }).ToArray();
            Assert.Equal(models.GroupBy(x => (x.Flag, x.Maybe)).Count(), pairs.Length);
            Assert.Equal(1, pairs.Single(x => x.Flag && x.Maybe == 2).Value);
            Assert.Equal(2, pairs.Single(x => !x.Flag && x.Maybe is null).Value);

            var anonymous = repo.Query<AggregateFunctionModel>().GroupBy(x => new { x.Flag, Year = x.Date.Year }).Select(g => new { g.Key.Flag, g.Key.Year, Value = g.Sum(x => x.Number) }).ToArray();
            Assert.Equal(models.Where(x => x.Flag && x.Date.Year == 2020).Sum(x => x.Number), anonymous.Single(x => x.Flag && x.Year == 2020).Value);

            var explicitTuple = repo.Query<AggregateFunctionModel>().GroupBy(x => new ValueTuple<bool, int?>(x.Flag, x.Maybe)).Select(g => new { Flag = g.Key.Item1, Maybe = g.Key.Item2, Value = (long)g.Count() }).ToArray();
            Assert.Equal(1, explicitTuple.Single(x => x.Flag && x.Maybe == 2).Value);
        }

        [Fact]
        public void CaseSensitiveEngineKeepsCasesApart()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<AggregateFunctionModel>(new MemoryEngine(true)));
            repo.Create<AggregateFunctionModel>(Models());

            var byName = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(1, byName.Single(x => x.Key == "apple").Value);
            Assert.Equal(1, byName.Single(x => x.Key == "Apple").Value);
        }

        [Entity("AggregateFunctionEvent")]
        public sealed class EventedModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
        }

        [Fact]
        public async Task EventStoreAggregatesCurrentState()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<EventedModel>(new MemoryEngine()));
            var model = new EventedModel { ID = Guid.NewGuid(), Value = 1 };
            repo.Create(model);
            model.Value = 5;
            repo.Update(model);
            model.Value = 10;
            await repo.UpdateAsync(model, TestContext.Current.CancellationToken);

            //a query of an event store reads the state each model has now
            Assert.Equal(1, repo.Query<EventedModel>().Count(x => x.ID == model.ID));
            Assert.Equal(10, repo.Query<EventedModel>().Where(x => x.ID == model.ID).Sum(x => x.Value));
            Assert.Equal(10, await repo.Query<EventedModel>().Where(x => x.ID == model.ID).MaxAsync(x => x.Value, cancellationToken: TestContext.Current.CancellationToken));
            var grouped = (await repo.Query<EventedModel>().Where(x => x.ID == model.ID).GroupBy(x => x.ID).Select(g => new { g.Key, Value = (long)g.Count() }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(1, Assert.Single(grouped).Value);
        }

        private sealed class EvenRules(ITransactStoreProvider<AggregateFunctionModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<AggregateFunctionModel>, AggregateFunctionModel>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<AggregateFunctionModel, bool>>)(x => x.Number % 2 == 0);
        }

        [Fact]
        public async Task RuleWhereApplies()
        {
            var repo = Repo.New();
            repo.AddProvider<AggregateFunctionModel>(new EvenRules(new TransactStoreProvider<AggregateFunctionModel>(new MemoryEngine())));
            var models = Models();
            repo.Create<AggregateFunctionModel>(models);

            Assert.Equal(2 + 4, repo.Query<AggregateFunctionModel>().Sum(x => x.Number));
            Assert.Equal(4, await repo.Query<AggregateFunctionModel>().Where(x => x.Number > 2).SumAsync(x => x.Number, cancellationToken: TestContext.Current.CancellationToken));
            var counts = repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, Assert.Single(counts).Value);
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("aggregate-function-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class Encrypting(ITransactStoreProvider<AggregateFunctionModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<AggregateFunctionModel>, AggregateFunctionModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
        }
        private sealed class Compressing(ITransactStoreProvider<AggregateFunctionModel> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<AggregateFunctionModel>, AggregateFunctionModel>(next)
        {
        }

        [Fact]
        public async Task EncryptedAndCompressedPropertiesRefuse()
        {
            foreach (var compression in new[] { false, true })
            {
                var repo = Repo.New();
                var root = new TransactStoreProvider<AggregateFunctionModel>(new MemoryEngine());
                repo.AddProvider<AggregateFunctionModel>(compression ? new Compressing(root) : new Encrypting(root));
                var models = Models();
                repo.Create<AggregateFunctionModel>(models);

                Assert.Equal(models.Max(x => x.Number), repo.Query<AggregateFunctionModel>().Max(x => x.Number));
                Assert.Equal(2, repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray().Length);
                _ = Assert.Throws<NotSupportedException>(() => repo.Query<AggregateFunctionModel>().Max(x => x.Name));
                _ = Assert.Throws<NotSupportedException>(() => repo.Query<AggregateFunctionModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
                _ = await Assert.ThrowsAsync<NotSupportedException>(async () => (await repo.Query<AggregateFunctionModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Name) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray());
                _ = await Assert.ThrowsAsync<NotSupportedException>(async () => (await repo.Query<AggregateFunctionModel>().GroupBy(x => new { x.Flag, x.Name }).Select(g => new { g.Key, Value = (long)g.Count() }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray());
            }
        }

        [Fact]
        public void ResultTypes()
        {
            Expression<Func<AggregateFunctionModel, int>> number = x => x.Number;
            Expression<Func<AggregateFunctionModel, int?>> maybe = x => x.Maybe;
            Expression<Func<AggregateFunctionModel, decimal>> money = x => x.Money;
            Assert.Equal(typeof(long), AggregateCommon.GetResultType(LinqColumnKind.Count, null));
            Assert.Equal(typeof(int), AggregateCommon.GetResultType(LinqColumnKind.Sum, number));
            Assert.Equal(typeof(int?), AggregateCommon.GetResultType(LinqColumnKind.Max, maybe));
            Assert.Equal(typeof(double), AggregateCommon.GetResultType(LinqColumnKind.Average, number));
            Assert.Equal(typeof(double?), AggregateCommon.GetResultType(LinqColumnKind.Average, maybe));
            Assert.Equal(typeof(decimal), AggregateCommon.GetResultType(LinqColumnKind.Average, money));
        }

    }
}
