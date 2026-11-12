// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Zerra.Repository.Memory;
using Zerra.Repository.MongoDB;

namespace Zerra.Repository.Test.MongoDB
{
    //what the shared sequences don't reach, run on MongoDB and in memory alike
    public class MongoDBFeatureTests
    {
        [Entity("MongoFeature")]
        public sealed class FeatureModel
        {
            [Identity(true)]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public double Ratio { get; set; }
            public DateTime Date { get; set; }
        }

        [Entity("MongoFeatureDecimal")]
        public sealed class DecimalIdentityModel
        {
            [Identity(true)]
            public decimal ID { get; set; }
            public int Number { get; set; }
        }

        public enum StoredKind
        {
            [EnumName("first")]
            First,
            Second
        }

        [Entity("MongoFeatureComposite")]
        public sealed class CompositeModel
        {
            [Identity]
            public int Region { get; set; }
            [Identity]
            public int Number { get; set; }
            public string? Name { get; set; }
            public StoredKind Kind { get; set; }
        }

        private static IMongoCollection<BsonDocument> StoredCollection(string name)
        {
            var url = MongoUrl.Create(MongoDBTestDataContext.FeatureConnectionString);
            return new MongoClient(url).GetDatabase(url.DatabaseName).GetCollection<BsonDocument>(name);
        }

        private static FeatureModel[] NewModels() => Enumerable.Range(0, 6).Select(i => new FeatureModel
        {
            Number = i,
            Ratio = i * 0.75 + 0.5,
            Date = new DateTime(2023 + i % 2, 3, 4 + i, 5, 6, 7, DateTimeKind.Utc),
        }).ToArray();

        private static async Task TestSequence(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<FeatureModel>(engine));
            _ = repo.Delete<FeatureModel>(x => x.Number >= 0);

            //an identity the store generates is a new Guid for each model
            var models = NewModels();
            repo.Create<FeatureModel>(models.Take(3).ToArray());
            foreach (var model in models.Skip(3).Take(2))
                await repo.CreateAsync(model, cancellationToken: TestContext.Current.CancellationToken);
            await repo.CreateAsync<FeatureModel>(models.Skip(5).ToArray(), cancellationToken: TestContext.Current.CancellationToken);
            Assert.DoesNotContain(models, x => x.ID == Guid.Empty);
            Assert.Equal(models.Length, models.Select(x => x.ID).Distinct().Count());
            Assert.Equal(models.Select(x => x.ID).Order(), repo.Query<FeatureModel>().Select(x => x.ID).ToArray().Order());

            //the different values in an order, and paged
            Assert.Equal(models.Select(x => x.Number % 3).Distinct().OrderByDescending(n => n), repo.Query<FeatureModel>().Select(x => x.Number % 3).Distinct().OrderByDescending(n => n).ToArray());
            Assert.Equal(models.Select(x => x.Number % 3).Distinct().Order().Skip(1).Take(1), repo.Query<FeatureModel>().Select(x => x.Number % 3).Distinct().OrderBy(n => n).Skip(1).Take(1).ToArray());
            Assert.Equal(models.Select(x => x.Number % 3).Distinct().Skip(1).Count(), repo.Query<FeatureModel>().Select(x => x.Number % 3).Distinct().Skip(1).Count());

            //a member of the value a group is keyed by
            Assert.Equal(models.GroupBy(x => x.Date).Count(g => g.Key.Year == 2024), repo.Query<FeatureModel>().GroupBy(x => x.Date).Select(g => new { g.Key, Rows = g.Count() }).Where(r => r.Key.Year == 2024).ToArray().Length);
            Assert.Equal(models.GroupBy(x => x.Date).Select(g => g.Key.Day).Order(), repo.Query<FeatureModel>().GroupBy(x => x.Date).Select(g => new { g.Key, Rows = g.Count() }).OrderBy(r => r.Key.Day).ToArray().Select(r => r.Key.Day));

            //the Math functions the shared sequences don't use
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var q = repo.Query<FeatureModel>().OrderBy(x => x.Number);
            Assert.Equal(ordered.Select(x => Math.Truncate(x.Ratio)), q.Select(x => Math.Truncate(x.Ratio)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Max(x.Number, 3)), q.Select(x => Math.Max(x.Number, 3)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Min(x.Number, 3)), q.Select(x => Math.Min(x.Number, 3)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Sign(x.Number - 2)), q.Select(x => Math.Sign(x.Number - 2)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Round(Math.Exp(x.Ratio), 6)), q.Select(x => Math.Round(Math.Exp(x.Ratio), 6)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Round(Math.Log10(x.Ratio), 6)), q.Select(x => Math.Round(Math.Log10(x.Ratio), 6)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Round(Math.Log(x.Ratio), 6)), q.Select(x => Math.Round(Math.Log(x.Ratio), 6)).ToArray());
            Assert.Equal(ordered.Select(x => Math.Round(Math.Log(x.Ratio, 2), 6)), q.Select(x => Math.Round(Math.Log(x.Ratio, 2), 6)).ToArray());

            //a side known before the query decides the condition or leaves the other side
            foreach (var flag in new[] { true, false })
            {
                Assert.Equal(models.Count(x => x.Number > 2 || flag), repo.Query<FeatureModel>().Where(x => x.Number > 2 || flag).Count());
                Assert.Equal(models.Count(x => x.Number > 2 && flag), repo.Query<FeatureModel>().Where(x => x.Number > 2 && flag).Count());
                Assert.Equal(models.Count(x => x.Number > 0 && (x.Ratio > 2 || !flag)), repo.Query<FeatureModel>().Where(x => x.Number > 0 && (x.Ratio > 2 || !flag)).Count());
                Assert.Equal(models.Count(x => !(x.Number > 2 || flag)), repo.Query<FeatureModel>().Where(x => !(x.Number > 2 || flag)).Count());
                Assert.Equal(0, repo.Delete<FeatureModel>(x => x.Number < 0 && (x.Ratio > 100 || flag)));
            }

            _ = repo.Delete<FeatureModel>(x => x.Number >= 0);
        }

        [Fact]
        public Task MongoDB()
        {
            var engine = new MongoDBEngine(MongoDBTestDataContext.FeatureConnectionString);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(FeatureModel), typeof(DecimalIdentityModel)]);
            return TestSequence(engine);
        }

        [Fact]
        public Task Memory() => TestSequence(new MemoryEngine());

        //the documents as they're stored, which reading them back through the engine doesn't show
        [Fact]
        public void MongoDBStoresDocuments()
        {
            var engine = new MongoDBEngine(MongoDBTestDataContext.FeatureConnectionString);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(CompositeModel)]);
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<CompositeModel>(engine));
            _ = repo.Delete<CompositeModel>(x => x.Region >= 0);
            var stored = StoredCollection("MongoFeatureComposite");

            //several identities are the _id, and only they are, so the model is found by them when it's changed
            var model = new CompositeModel { Region = 1, Number = 2, Name = "a", Kind = StoredKind.Second };
            repo.Create(model);
            var document = stored.Find(FilterDefinition<BsonDocument>.Empty).Single(TestContext.Current.CancellationToken);
            Assert.Equal(new BsonDocument { { "Region", 1 }, { "Number", 2 } }, document["_id"]);
            Assert.Equal("a", document["Name"].AsString);
            model.Name = "b";
            repo.Update(model);
            Assert.Equal("b", repo.Query<CompositeModel>().Single(x => x.Region == 1 && x.Number == 2).Name);

            //an enum set from a number by an update of many is stored as its name, as one written with the model is
            Assert.Equal("Second", document["Kind"].AsString);
            Assert.Equal(1, repo.Update<CompositeModel>(x => x.Region == 1, x => new CompositeModel { Kind = (StoredKind)(x.Number - 2) }));
            Assert.Equal("first", stored.Find(FilterDefinition<BsonDocument>.Empty).Single(TestContext.Current.CancellationToken)["Kind"].AsString);
            Assert.Single(repo.Query<CompositeModel>().Where(x => x.Kind == StoredKind.First).ToArray());

            _ = repo.Delete<CompositeModel>(x => x.Region >= 0);
        }

        //MongoDB has no identity of its own to generate, so only a Guid or a number from a counter is generated
        [Fact]
        public async Task MongoDBRefusesDecimalIdentity()
        {
            var engine = new MongoDBEngine(MongoDBTestDataContext.FeatureConnectionString);
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<DecimalIdentityModel>(engine));
            _ = Assert.Throws<NotSupportedException>(() => repo.Create(new DecimalIdentityModel { Number = 1 }));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.CreateAsync(new DecimalIdentityModel { Number = 1 }, cancellationToken: TestContext.Current.CancellationToken));
            var url = MongoUrl.Create(MongoDBTestDataContext.FeatureConnectionString);
            var stored = new MongoClient(url).GetDatabase(url.DatabaseName).GetCollection<global::MongoDB.Bson.BsonDocument>("MongoFeatureDecimal");
            Assert.Equal(0, await stored.CountDocumentsAsync(FilterDefinition<global::MongoDB.Bson.BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken));
        }
    }
}
