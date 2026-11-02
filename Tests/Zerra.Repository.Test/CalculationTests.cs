// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //the calculation queries through IRepo on the memory engine, the event store, the layers, and the shared pieces
    public class CalculationTests
    {
        [Entity("CalculationModel")]
        public sealed class CalculationModel
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

        private static CalculationModel[] Models()
        {
            var names = new string?[] { "apple", "banana", null, "Apple", "cherry" };
            var models = new CalculationModel[5];
            for (var i = 0; i < models.Length; i++)
            {
                models[i] = new CalculationModel
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

        private static (IRepo Repo, CalculationModel[] Models) MemoryRepo()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<CalculationModel>(new MemoryEngine()));
            var models = Models();
            repo.Create<CalculationModel>(models);
            return (repo, models);
        }

        [Fact]
        public async Task ScalarsMatchLinq()
        {
            var (repo, models) = MemoryRepo();

            Assert.Equal(models.Sum(x => x.Number), repo.Query<CalculationModel>().Sum(x => x.Number));
            Assert.Equal(models.Sum(x => x.Maybe), repo.Query<CalculationModel>().Sum(x => x.Maybe));
            Assert.Equal(models.Sum(x => x.Money), repo.Query<CalculationModel>().Sum(x => x.Money));
            Assert.Equal(models.Sum(x => x.Ratio), repo.Query<CalculationModel>().Sum(x => x.Ratio));
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Number * 2), repo.Query<CalculationModel>().Where(x => x.Flag).Sum(x => x.Number * 2));

            Assert.Equal(models.Average(x => x.Number), repo.Query<CalculationModel>().Average(x => x.Number));
            Assert.Equal(models.Average(x => x.Maybe), repo.Query<CalculationModel>().Average(x => x.Maybe));
            Assert.Equal(models.Average(x => x.Money), repo.Query<CalculationModel>().Average(x => x.Money));
            Assert.Equal(models.Average(x => x.Ratio), await repo.Query<CalculationModel>().AverageAsync(x => x.Ratio, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Equal(models.Min(x => x.Number), repo.Query<CalculationModel>().Min(x => x.Number));
            Assert.Equal(models.Max(x => x.Maybe), repo.Query<CalculationModel>().Max(x => x.Maybe));
            Assert.Equal(models.Min(x => x.Name), repo.Query<CalculationModel>().Min(x => x.Name));
            Assert.Equal(models.Max(x => x.Date), await repo.Query<CalculationModel>().MaxAsync(x => x.Date, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public void EmptyMatchesLinq()
        {
            var (repo, _) = MemoryRepo();
            Expression<Func<CalculationModel, bool>> none = x => x.Number > 100;

            Assert.Equal(0, repo.Query<CalculationModel>().Where(none).Sum(x => x.Number));
            Assert.Equal(0, repo.Query<CalculationModel>().Where(none).Sum(x => x.Maybe));
            Assert.Null(repo.Query<CalculationModel>().Where(none).Average(x => x.Maybe));
            Assert.Null(repo.Query<CalculationModel>().Where(none).Max(x => x.Name));
            Assert.Throws<InvalidOperationException>(() => repo.Query<CalculationModel>().Where(none).Average(x => x.Number));
            Assert.Throws<InvalidOperationException>(() => repo.Query<CalculationModel>().Where(none).Min(x => x.Number));
            Assert.Empty(repo.Query<CalculationModel>().Where(none).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
        }

        [Fact]
        public async Task GroupsMatchLinq()
        {
            var (repo, models) = MemoryRepo();

            var counts = repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, counts.Length);
            Assert.Equal(3, counts.Single(x => x.Key).Value);
            Assert.Equal(2, counts.Single(x => !x.Key).Value);

            var sums = (await repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Money) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Money), sums.Single(x => x.Key).Value);

            var averages = repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Average(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => !x.Flag).Average(x => x.Number), averages.Single(x => !x.Key).Value);

            //a null key is a group, as in LINQ
            var byMaybe = repo.Query<CalculationModel>().GroupBy(x => x.Maybe).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, byMaybe.Single(x => x.Key is null).Value);
            Assert.Equal(3, byMaybe.Count(x => x.Key is not null));

            //string keys group without case by default, like the memory engine's where
            var byName = repo.Query<CalculationModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, byName.Single(x => x.Key == "apple").Value);
            Assert.Equal(1, byName.Single(x => x.Key is null).Value);

            var byYear = repo.Query<CalculationModel>().GroupBy(x => x.Date.Year).Select(g => new { g.Key, Value = g.Max(x => x.Date) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(models.Where(x => x.Date.Year == 2021).Max(x => x.Date), byYear.Single(x => x.Key == 2021).Value);

            var filtered = repo.Query<CalculationModel>().Where(x => x.Number > 1).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(3, filtered.Single(x => x.Key).Value);
            Assert.Equal(2, filtered.Single(x => !x.Key).Value);
        }

        [Fact]
        public void TupleAndAnonymousKeys()
        {
            var (repo, models) = MemoryRepo();

            //a key of several members is an anonymous object, expression trees can't hold a tuple literal
            var pairs = repo.Query<CalculationModel>().GroupBy(x => new { x.Flag, x.Maybe }).Select(g => new { g.Key.Flag, g.Key.Maybe, Value = (long)g.Count() }).ToArray();
            Assert.Equal(models.GroupBy(x => (x.Flag, x.Maybe)).Count(), pairs.Length);
            Assert.Equal(1, pairs.Single(x => x.Flag && x.Maybe == 2).Value);
            Assert.Equal(2, pairs.Single(x => !x.Flag && x.Maybe is null).Value);

            var anonymous = repo.Query<CalculationModel>().GroupBy(x => new { x.Flag, Year = x.Date.Year }).Select(g => new { g.Key.Flag, g.Key.Year, Value = g.Sum(x => x.Number) }).ToArray();
            Assert.Equal(models.Where(x => x.Flag && x.Date.Year == 2020).Sum(x => x.Number), anonymous.Single(x => x.Flag && x.Year == 2020).Value);

            var explicitTuple = repo.Query<CalculationModel>().GroupBy(x => new ValueTuple<bool, int?>(x.Flag, x.Maybe)).Select(g => new { Flag = g.Key.Item1, Maybe = g.Key.Item2, Value = (long)g.Count() }).ToArray();
            Assert.Equal(1, explicitTuple.Single(x => x.Flag && x.Maybe == 2).Value);
        }

        [Fact]
        public void CaseSensitiveEngineKeepsCasesApart()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<CalculationModel>(new MemoryEngine(true)));
            repo.Create<CalculationModel>(Models());

            var byName = repo.Query<CalculationModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(1, byName.Single(x => x.Key == "apple").Value);
            Assert.Equal(1, byName.Single(x => x.Key == "Apple").Value);
        }

        [Entity("CalculationEvent")]
        public sealed class EventedModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
        }

        [Fact]
        public async Task EventStoreCalculatesOnCurrentState()
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

        private sealed class EvenRules(ITransactStoreProvider<CalculationModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<CalculationModel>, CalculationModel>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<CalculationModel, bool>>)(x => x.Number % 2 == 0);
        }

        [Fact]
        public async Task RuleWhereApplies()
        {
            var repo = Repo.New();
            repo.AddProvider<CalculationModel>(new EvenRules(new TransactStoreProvider<CalculationModel>(new MemoryEngine())));
            var models = Models();
            repo.Create<CalculationModel>(models);

            Assert.Equal(2 + 4, repo.Query<CalculationModel>().Sum(x => x.Number));
            Assert.Equal(4, await repo.Query<CalculationModel>().Where(x => x.Number > 2).SumAsync(x => x.Number, cancellationToken: TestContext.Current.CancellationToken));
            var counts = repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, Assert.Single(counts).Value);
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("calculation-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class Encrypting(ITransactStoreProvider<CalculationModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<CalculationModel>, CalculationModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
        }
        private sealed class Compressing(ITransactStoreProvider<CalculationModel> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<CalculationModel>, CalculationModel>(next)
        {
        }

        [Fact]
        public async Task EncryptedAndCompressedPropertiesRefuse()
        {
            foreach (var compression in new[] { false, true })
            {
                var repo = Repo.New();
                var root = new TransactStoreProvider<CalculationModel>(new MemoryEngine());
                repo.AddProvider<CalculationModel>(compression ? new Compressing(root) : new Encrypting(root));
                var models = Models();
                repo.Create<CalculationModel>(models);

                Assert.Equal(models.Max(x => x.Number), repo.Query<CalculationModel>().Max(x => x.Number));
                Assert.Equal(2, repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray().Length);
                _ = Assert.Throws<NotSupportedException>(() => repo.Query<CalculationModel>().Max(x => x.Name));
                _ = Assert.Throws<NotSupportedException>(() => repo.Query<CalculationModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
                _ = await Assert.ThrowsAsync<NotSupportedException>(async () => (await repo.Query<CalculationModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Name) }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray());
                _ = await Assert.ThrowsAsync<NotSupportedException>(async () => (await repo.Query<CalculationModel>().GroupBy(x => new { x.Flag, x.Name }).Select(g => new { g.Key, Value = (long)g.Count() }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => (r.Key, r.Value)).ToArray());
            }
        }

        [Fact]
        public void ResultTypes()
        {
            Expression<Func<CalculationModel, int>> number = x => x.Number;
            Expression<Func<CalculationModel, int?>> maybe = x => x.Maybe;
            Expression<Func<CalculationModel, decimal>> money = x => x.Money;
            Assert.Equal(typeof(long), CalculationCommon.GetResultType(LinqColumnKind.Count, null));
            Assert.Equal(typeof(int), CalculationCommon.GetResultType(LinqColumnKind.Sum, number));
            Assert.Equal(typeof(int?), CalculationCommon.GetResultType(LinqColumnKind.Max, maybe));
            Assert.Equal(typeof(double), CalculationCommon.GetResultType(LinqColumnKind.Average, number));
            Assert.Equal(typeof(double?), CalculationCommon.GetResultType(LinqColumnKind.Average, maybe));
            Assert.Equal(typeof(decimal), CalculationCommon.GetResultType(LinqColumnKind.Average, money));
        }

    }
}
