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

            Assert.Equal(models.Sum(x => x.Number), repo.Sum<CalculationModel>(x => x.Number));
            Assert.Equal(models.Sum(x => x.Maybe), repo.Sum<CalculationModel>(x => x.Maybe));
            Assert.Equal(models.Sum(x => x.Money), repo.Sum<CalculationModel>(x => x.Money));
            Assert.Equal(models.Sum(x => x.Ratio), repo.Sum<CalculationModel>(x => x.Ratio));
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Number * 2), repo.Sum<CalculationModel>(x => x.Number * 2, x => x.Flag));

            Assert.Equal(models.Average(x => x.Number), repo.Average<CalculationModel>(x => x.Number));
            Assert.Equal(models.Average(x => x.Maybe), repo.Average<CalculationModel>(x => x.Maybe));
            Assert.Equal(models.Average(x => x.Money), repo.Average<CalculationModel>(x => x.Money));
            Assert.Equal(models.Average(x => x.Ratio), await repo.AverageAsync<CalculationModel>(x => x.Ratio));

            Assert.Equal(models.Min(x => x.Number), repo.Min<CalculationModel, int>(x => x.Number));
            Assert.Equal(models.Max(x => x.Maybe), repo.Max<CalculationModel, int?>(x => x.Maybe));
            Assert.Equal(models.Min(x => x.Name), repo.Min<CalculationModel, string?>(x => x.Name));
            Assert.Equal(models.Max(x => x.Date), await repo.MaxAsync<CalculationModel, DateTime>(x => x.Date));
        }

        [Fact]
        public void EmptyMatchesLinq()
        {
            var (repo, _) = MemoryRepo();
            Expression<Func<CalculationModel, bool>> none = x => x.Number > 100;

            Assert.Equal(0, repo.Sum<CalculationModel>(x => x.Number, none));
            Assert.Equal(0, repo.Sum<CalculationModel>(x => x.Maybe, none));
            Assert.Null(repo.Average<CalculationModel>(x => x.Maybe, none));
            Assert.Null(repo.Max<CalculationModel, string?>(x => x.Name, none));
            Assert.Throws<InvalidOperationException>(() => repo.Average<CalculationModel>(x => x.Number, none));
            Assert.Throws<InvalidOperationException>(() => repo.Min<CalculationModel, int>(x => x.Number, none));
            Assert.Empty(repo.Count<CalculationModel, bool>(x => x.Flag, none));
        }

        [Fact]
        public async Task GroupsMatchLinq()
        {
            var (repo, models) = MemoryRepo();

            var counts = repo.Count<CalculationModel, bool>(x => x.Flag);
            Assert.Equal(2, counts.Count);
            Assert.Equal(3, counts.Single(x => x.Key).Value);
            Assert.Equal(2, counts.Single(x => !x.Key).Value);

            var sums = await repo.SumAsync<CalculationModel, bool>(x => x.Flag, x => x.Money);
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Money), sums.Single(x => x.Key).Value);

            var averages = repo.Average<CalculationModel, bool>(x => x.Flag, x => x.Number);
            Assert.Equal(models.Where(x => !x.Flag).Average(x => x.Number), averages.Single(x => !x.Key).Value);

            //a null key is a group, as in LINQ
            var byMaybe = repo.Count<CalculationModel, int?>(x => x.Maybe);
            Assert.Equal(2, byMaybe.Single(x => x.Key is null).Value);
            Assert.Equal(3, byMaybe.Count(x => x.Key is not null));

            //string keys group without case by default, like the memory engine's where
            var byName = repo.Count<CalculationModel, string?>(x => x.Name);
            Assert.Equal(2, byName.Single(x => x.Key == "apple").Value);
            Assert.Equal(1, byName.Single(x => x.Key is null).Value);

            var byYear = repo.Max<CalculationModel, int, DateTime>(x => x.Date.Year, x => x.Date);
            Assert.Equal(models.Where(x => x.Date.Year == 2021).Max(x => x.Date), byYear.Single(x => x.Key == 2021).Value);

            var filtered = repo.Min<CalculationModel, bool, int>(x => x.Flag, x => x.Number, x => x.Number > 1);
            Assert.Equal(3, filtered.Single(x => x.Key).Value);
            Assert.Equal(2, filtered.Single(x => !x.Key).Value);
        }

        [Fact]
        public void TupleAndAnonymousKeys()
        {
            var (repo, models) = MemoryRepo();

            //a key of several members is an anonymous object, expression trees can't hold a tuple literal
            var pairs = repo.Count((CalculationModel x) => new { x.Flag, x.Maybe });
            Assert.Equal(models.GroupBy(x => (x.Flag, x.Maybe)).Count(), pairs.Count);
            Assert.Equal(1, pairs.Single(x => x.Key.Flag && x.Key.Maybe == 2).Value);
            Assert.Equal(2, pairs.Single(x => !x.Key.Flag && x.Key.Maybe is null).Value);

            var anonymous = repo.Sum(x => new { x.Flag, Year = x.Date.Year }, (CalculationModel x) => x.Number);
            Assert.Equal(models.Where(x => x.Flag && x.Date.Year == 2020).Sum(x => x.Number), anonymous.Single(x => x.Key.Flag && x.Key.Year == 2020).Value);

            var explicitTuple = repo.Count<CalculationModel, ValueTuple<bool, int?>>(x => new ValueTuple<bool, int?>(x.Flag, x.Maybe));
            Assert.Equal(1, explicitTuple.Single(x => x.Key == (true, 2)).Value);
        }

        [Fact]
        public void CaseSensitiveEngineKeepsCasesApart()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<CalculationModel>(new MemoryEngine(true)));
            repo.Create<CalculationModel>(Models());

            var byName = repo.Count<CalculationModel, string?>(x => x.Name);
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
            await repo.UpdateAsync(model);

            //Count on an event store counts every state the model had, a calculation uses the state it has now
            Assert.Equal(3, repo.Count<EventedModel>(x => x.ID == model.ID));
            Assert.Equal(10, repo.Sum<EventedModel>(x => x.Value, x => x.ID == model.ID));
            Assert.Equal(10, await repo.MaxAsync<EventedModel, int>(x => x.Value, x => x.ID == model.ID));
            var grouped = await repo.CountAsync<EventedModel, Guid>(x => x.ID, x => x.ID == model.ID);
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

            Assert.Equal(2 + 4, repo.Sum<CalculationModel>(x => x.Number));
            Assert.Equal(4, await repo.SumAsync<CalculationModel>(x => x.Number, x => x.Number > 2));
            var counts = repo.Count<CalculationModel, bool>(x => x.Flag);
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

                Assert.Equal(models.Max(x => x.Number), repo.Max<CalculationModel, int>(x => x.Number));
                Assert.Equal(2, repo.Count<CalculationModel, bool>(x => x.Flag).Count);
                _ = Assert.Throws<NotSupportedException>(() => repo.Max<CalculationModel, string?>(x => x.Name));
                _ = Assert.Throws<NotSupportedException>(() => repo.Count<CalculationModel, string?>(x => x.Name));
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.MinAsync<CalculationModel, bool, string?>(x => x.Flag, x => x.Name));
                _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.CountAsync((CalculationModel x) => new { x.Flag, x.Name }));
            }
        }

        private sealed class RecordingProvider : ITransactStoreProvider<CalculationModel>
        {
            private static readonly CalculationModel[] models = Models();
            public Query? Query { get; private set; }
            private object? Result(Query query)
            {
                Query = query;
                return models.Calculate(query.Operation, query.GroupBy, query.Value);
            }
            object? ITransactStoreProvider.Query(Query query) => Result(query);
            Task<object?> ITransactStoreProvider.QueryAsync(Query query) => Task.FromResult(Result(query));
            void ITransactStoreProvider.Persist(Persist persist) => throw new NotSupportedException();
            Task ITransactStoreProvider.PersistAsync(Persist persist) => throw new NotSupportedException();
        }

        [Fact]
        public async Task OverloadsPassTheirArguments()
        {
            var provider = new RecordingProvider();
            var repo = Repo.New();
            repo.AddProvider<CalculationModel>(provider);
            Expression<Func<CalculationModel, bool>> where = x => x.Number > 1;
            Expression<Func<CalculationModel, bool>> groupBy = x => x.Flag;
            Expression<Func<CalculationModel, int>> number = x => x.Number;
            Expression<Func<CalculationModel, decimal?>> money = x => x.Money;

            _ = repo.Sum(number, where);
            Assert.Equal(QueryOperation.Sum, provider.Query!.Operation);
            Assert.Same(where, provider.Query.Where);
            Assert.Null(provider.Query.GroupBy);
            Assert.Same(number, provider.Query.Value);

            _ = await repo.SumAsync(groupBy, number);
            Assert.Equal(QueryOperation.Sum, provider.Query!.Operation);
            Assert.Null(provider.Query.Where);
            Assert.Same(groupBy, provider.Query.GroupBy);
            Assert.Same(number, provider.Query.Value);

            decimal? average = repo.Average(money);
            Assert.Equal(QueryOperation.Average, provider.Query!.Operation);
            Assert.Same(money, provider.Query.Value);
            Assert.Equal(3m, average);

            //the recording provider calculates over all its models, the where is only recorded
            double integerAverage = await repo.AverageAsync(number, where);
            Assert.Equal(3.0, integerAverage);
            Assert.Same(where, provider.Query!.Where);

            _ = repo.Min(number, where);
            Assert.Equal(QueryOperation.Min, provider.Query!.Operation);
            Assert.Same(where, provider.Query.Where);

            _ = await repo.MaxAsync(groupBy, money, where);
            Assert.Equal(QueryOperation.Max, provider.Query!.Operation);
            Assert.Same(groupBy, provider.Query.GroupBy);
            Assert.Same(money, provider.Query.Value);
            Assert.Same(where, provider.Query.Where);

            _ = await repo.CountAsync(groupBy, where);
            Assert.Equal(QueryOperation.Count, provider.Query!.Operation);
            Assert.Same(groupBy, provider.Query.GroupBy);
            Assert.Null(provider.Query.Value);
            Assert.Same(where, provider.Query.Where);
        }

        [Fact]
        public void ResultTypes()
        {
            Expression<Func<CalculationModel, int>> number = x => x.Number;
            Expression<Func<CalculationModel, int?>> maybe = x => x.Maybe;
            Expression<Func<CalculationModel, decimal>> money = x => x.Money;
            Assert.Equal(typeof(long), CalculationCommon.GetResultType(QueryOperation.Count, null));
            Assert.Equal(typeof(int), CalculationCommon.GetResultType(QueryOperation.Sum, number));
            Assert.Equal(typeof(int?), CalculationCommon.GetResultType(QueryOperation.Max, maybe));
            Assert.Equal(typeof(double), CalculationCommon.GetResultType(QueryOperation.Average, number));
            Assert.Equal(typeof(double?), CalculationCommon.GetResultType(QueryOperation.Average, maybe));
            Assert.Equal(typeof(decimal), CalculationCommon.GetResultType(QueryOperation.Average, money));
        }

    }
}
