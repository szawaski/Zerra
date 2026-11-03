// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class LayerProviderEdgeTests
    {
        [Entity("EdgeModel")]
        public sealed class EdgeModel
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Text { get; set; }
            public byte[]? Bytes { get; set; }
            public int Number { get; set; }
        }

        //a stored member with the name of a member of the event
        [Entity("EdgeEventNamedModel")]
        public sealed class EventNamedModel
        {
            [Identity]
            public Guid ID { get; set; }
            public string? EventName { get; set; }
        }

        [Entity("EdgePlainModel")]
        public sealed class PlainModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
        }

        [Entity("EdgeOwner")]
        public sealed class Owner
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }

            [Relation(nameof(Item.OwnerID))]
            public List<Item>? Items { get; set; }
        }

        [Entity("EdgeItem")]
        public sealed class Item
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid OwnerID { get; set; }
            public string? Text { get; set; }
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("edge-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class Encrypting<TModel>(ITransactStoreProvider<TModel> next, bool enabled = true, Graph<TModel>? properties = null)
            : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<TModel>, TModel>(next)
            where TModel : class, new()
        {
            public override bool Enabled => enabled;
            public override Graph<TModel>? Properties => properties;
            public override IEncryptor Encryptor => encryptor;
        }

        private sealed class Compressing<TModel>(ITransactStoreProvider<TModel> next, bool enabled = true, Graph<TModel>? properties = null)
            : BaseTransactStoreCompressionProvider<ITransactStoreProvider<TModel>, TModel>(next)
            where TModel : class, new()
        {
            public override bool Enabled => enabled;
            public override Graph<TModel>? Properties => properties;
        }

        private static ITransactStoreProvider<TModel> Layer<TModel>(bool compression, ITransactStoreProvider<TModel> next, bool enabled = true, Graph<TModel>? properties = null)
            where TModel : class, new()
            => compression ? new Compressing<TModel>(next, enabled, properties) : new Encrypting<TModel>(next, enabled, properties);

        public static TheoryData<bool> Compression => new() { false, true };

        private static EdgeModel NewModel() => new() { ID = Guid.NewGuid(), Text = new string('t', 100), Bytes = Enumerable.Repeat((byte)9, 100).ToArray(), Number = 1 };

        private sealed class RotatingKeySource : ISymmetricKeySource
        {
            private static readonly byte[] key = new byte[32];
            public (byte[] Key, DateTime Expiration) GetKey() => (key, DateTime.UtcNow.AddHours(1));
            public ValueTask<(byte[] Key, DateTime Expiration)> GetKeyAsync() => new(GetKey());
        }

        private sealed class EncryptingWithKeySource<TModel>(ITransactStoreProvider<TModel> next)
            : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<TModel>, TModel>(next)
            where TModel : class, new()
        {
            public override IEncryptor Encryptor { get; } = new ZerraEncryptor(new RotatingKeySource(), SymmetricAlgorithmType.AES_GCM);
        }

        //stored data outlives the two keys a key source keeps, so a rotating encryptor is refused
        [Fact]
        public async Task KeySource_IsRejected()
        {
            var repo = Repo.New();
            repo.AddProvider(new EncryptingWithKeySource<EdgeModel>(new TransactStoreProvider<EdgeModel>(new MemoryEngine())));
            _ = Assert.Throws<NotSupportedException>(() => repo.Create(NewModel()));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.CreateAsync(NewModel(), TestContext.Current.CancellationToken));
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public void Disabled_StoresAsIs(bool compression)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<EdgeModel>(engine), enabled: false));
            var raw = Repo.New();
            raw.AddProvider(new TransactStoreProvider<EdgeModel>(engine));

            var model = NewModel();
            repo.Create(model);
            var stored = raw.Query<EdgeModel>().SingleOrDefault(x => x.ID == model.ID)!;
            Assert.Equal(model.Text, stored.Text);
            Assert.Equal(model.Bytes, stored.Bytes);
            Assert.Equal(model.Text, repo.Query<EdgeModel>().SingleOrDefault(x => x.ID == model.ID)!.Text);
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public async Task Properties_LimitWhatIsTransformed(bool compression)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<EdgeModel>(engine), properties: new Graph<EdgeModel>(x => x.Text)));
            var raw = Repo.New();
            raw.AddProvider(new TransactStoreProvider<EdgeModel>(engine));

            var model = NewModel();
            await repo.CreateAsync(model, TestContext.Current.CancellationToken);
            var stored = raw.Query<EdgeModel>().SingleOrDefault(x => x.ID == model.ID)!;
            Assert.NotEqual(model.Text, stored.Text);
            Assert.Equal(model.Bytes, stored.Bytes);

            var read = (await repo.Query<EdgeModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken))!;
            Assert.Equal(model.Text, read.Text);
            Assert.Equal(model.Bytes, read.Bytes);
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public async Task NothingToTransform(bool compression)
        {
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<PlainModel>(new MemoryEngine())));
            var model = new PlainModel { ID = Guid.NewGuid(), Number = 3 };
            repo.Create(model);
            Assert.Equal(3, repo.Query<PlainModel>().SingleOrDefault(x => x.ID == model.ID)!.Number);
            Assert.Single(await repo.Query<PlainModel>().Where(x => x.ID == model.ID).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public void DataStoredBeforeTheLayer_ReadsAsIs(bool compression)
        {
            var engine = new MemoryEngine();
            var raw = Repo.New();
            raw.AddProvider(new TransactStoreProvider<EdgeModel>(engine));
            var model = NewModel();
            raw.Create(model);

            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<EdgeModel>(engine)));
            var read = repo.Query<EdgeModel>().SingleOrDefault(x => x.ID == model.ID)!;
            Assert.Equal(model.Text, read.Text);
            Assert.Equal(model.Bytes, read.Bytes);
            Assert.Equal(model.Text, Assert.Single(repo.Query<EdgeModel>().Where(x => x.ID == model.ID).ToArray()).Text);
        }

        //a change by condition sets transformed values without loading the rows, and refuses a condition or value through a transformed member
        [Theory]
        [MemberData(nameof(Compression))]
        public void ChangesByCondition(bool compression)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<EdgeModel>(engine)));
            var model = NewModel();
            repo.Create(model);

            Assert.Equal(1, repo.Update(x => x.ID == model.ID, new EdgeModel { Text = "set by condition" }, new Graph<EdgeModel>(x => x.Text)));
            Assert.Equal("set by condition", repo.Query<EdgeModel>().Single(x => x.ID == model.ID).Text);
            Assert.Equal(1, repo.Update<EdgeModel>(x => x.ID == model.ID, x => new EdgeModel { Number = x.Number + 5 }));
            Assert.Equal(model.Number + 5, repo.Query<EdgeModel>().Single(x => x.ID == model.ID).Number);

            _ = Assert.Throws<NotSupportedException>(() => repo.Update(x => x.Text == "a", new EdgeModel { Number = 1 }, new Graph<EdgeModel>(x => x.Number)));
            _ = Assert.Throws<NotSupportedException>(() => repo.Update<EdgeModel>(x => x.Text == "a", x => new EdgeModel { Number = 1 }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Update<EdgeModel>(x => x.ID == model.ID, x => new EdgeModel { Text = "computed" }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Delete<EdgeModel>(x => x.Text == "a"));

            Assert.Equal(1, repo.Delete<EdgeModel>(x => x.ID == model.ID));
            Assert.Empty(repo.Query<EdgeModel>().ToArray());
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public async Task UpdateWithGraph_TransformsOnlyThoseMembers(bool compression)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new TransactStoreProvider<EdgeModel>(engine)));
            var model = NewModel();
            repo.Create(model);

            model.Number = 2;
            model.Text = "changed but not in the graph";
            repo.Update(model, new Graph<EdgeModel>(x => x.Number));
            model.Bytes = [1, 2, 3];
            await repo.UpdateAsync(model, new Graph<EdgeModel>(x => x.Bytes), TestContext.Current.CancellationToken);

            var read = repo.Query<EdgeModel>().SingleOrDefault(x => x.ID == model.ID)!;
            Assert.Equal(2, read.Number);
            Assert.Equal(new string('t', 100), read.Text);
            Assert.Equal([1, 2, 3], read.Bytes);
        }

        [Theory]
        [MemberData(nameof(Compression))]
        public async Task EventQueries(bool compression)
        {
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new EventStoreAsTransactStoreProvider<EdgeModel>(new MemoryEngine())));
            var model = NewModel();
            repo.Create(model);
            var missing = Guid.NewGuid();

            Assert.Equal(model.Text, repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).SingleOrDefault()!.Model.Text);
            Assert.Equal(model.Text, (await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Model.Text);
            Assert.Null(repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).SingleOrDefault());
            Assert.Null(await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).FirstOrDefault());
            Assert.Null(await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Empty(repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).ToList());
            Assert.Empty(await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == missing).ToListAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(1, await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).LongCountAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(await repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).AnyAsync(cancellationToken: TestContext.Current.CancellationToken));

            //what is stored changed can't be selected or ordered by, through the event's model too
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).Select(x => x.Model.Text).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).OrderBy(x => x.Model.Text).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<EdgeModel>().Where(x => x.ID == model.ID).Select(x => x.Text).ToList());
            Assert.Equal([0UL], repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).OrderBy(x => x.Number).Select(x => x.Number).ToList());

            //streamed events and states are read plain
            Assert.Equal(model.Text, repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).AsEnumerable().Single().Model.Text);
            Assert.Equal(model.Text, repo.TemporalQuery<EdgeModel>().Where(x => x.ID == model.ID).AsEnumerable().Single().Text);
            await foreach (var eventModel in repo.EventQuery<EdgeModel>().Where(x => x.Model.ID == model.ID).AsAsyncEnumerable(TestContext.Current.CancellationToken))
                Assert.Equal(model.Text, eventModel.Model.Text);
        }

        //the event's own members aren't the model's, so only those read through Model are refused
        [Theory]
        [MemberData(nameof(Compression))]
        public async Task EventMembersAreNotTheModels(bool compression)
        {
            var repo = Repo.New();
            repo.AddProvider(Layer(compression, new EventStoreAsTransactStoreProvider<EventNamedModel>(new MemoryEngine())));
            var model = new EventNamedModel { ID = Guid.NewGuid(), EventName = new string('e', 100) };
            repo.Create(model);

            Assert.Single(repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).OrderBy(x => x.EventName).ToList());
            Assert.Single(await repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).Select(x => x.EventName).ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(model.EventName, repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).Single().Model.EventName);
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).OrderBy(x => x.Model.EventName).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).Select(x => x.Model.EventName).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<EventNamedModel>().Where(x => x.Model.ID == model.ID).GroupBy(x => x.Model.EventName).Select(g => g.Count()).ToList());
        }

        private sealed class OwnerRules(ITransactStoreProvider<Owner> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Owner>, Owner>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Owner x) => x.Name != "hidden";
        }

        //the layer is the provider of a related model, the parent asks it for its where and tells it about the query
        [Theory]
        [MemberData(nameof(Compression))]
        public async Task AsRelatedProvider(bool compression)
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new OwnerRules(new TransactStoreProvider<Owner>(engine)));
            repo.AddProvider(Layer(compression, new TransactStoreProvider<Item>(engine)));
            var graph = new Graph<Owner>(true, x => x.Items);

            var owner = new Owner { ID = Guid.NewGuid(), Name = "owner", Items = [new Item { ID = Guid.NewGuid(), Text = new string('i', 100) }] };
            repo.Create(owner, graph);
            repo.Create(new Owner { ID = Guid.NewGuid(), Name = "hidden" });

            var read = Assert.Single(repo.Query<Owner>(new Graph<Owner>(x => x.ID, x => x.Name, x => x.Items)).ToArray());
            Assert.Equal(new string('i', 100), Assert.Single(read.Items!).Text);
            read = Assert.Single(await repo.Query<Owner>(new Graph<Owner>(x => x.ID, x => x.Name, x => x.Items)).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(new string('i', 100), Assert.Single(read.Items!).Text);
        }
    }
}
