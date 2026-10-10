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
            var stored = raw.Single<EdgeModel>(x => x.ID == model.ID)!;
            Assert.Equal(model.Text, stored.Text);
            Assert.Equal(model.Bytes, stored.Bytes);
            Assert.Equal(model.Text, repo.Single<EdgeModel>(x => x.ID == model.ID)!.Text);
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
            await repo.CreateAsync(model);
            var stored = raw.Single<EdgeModel>(x => x.ID == model.ID)!;
            Assert.NotEqual(model.Text, stored.Text);
            Assert.Equal(model.Bytes, stored.Bytes);

            var read = (await repo.SingleAsync<EdgeModel>(x => x.ID == model.ID))!;
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
            Assert.Equal(3, repo.Single<PlainModel>(x => x.ID == model.ID)!.Number);
            Assert.Single(await repo.ManyAsync<PlainModel>(x => x.ID == model.ID));
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
            var read = repo.Single<EdgeModel>(x => x.ID == model.ID)!;
            Assert.Equal(model.Text, read.Text);
            Assert.Equal(model.Bytes, read.Bytes);
            Assert.Equal(model.Text, Assert.Single(repo.Many<EdgeModel>(x => x.ID == model.ID)).Text);
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
            await repo.UpdateAsync(model, new Graph<EdgeModel>(x => x.Bytes));

            var read = repo.Single<EdgeModel>(x => x.ID == model.ID)!;
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

            Assert.Equal(model.Text, repo.EventSingle<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID)!.Model.Text);
            Assert.Equal(model.Text, (await repo.EventSingleAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID))!.Model.Text);
            Assert.Null(repo.EventSingle<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Null(await repo.EventSingleAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Null(repo.EventFirst<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Null(await repo.EventFirstAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Empty(repo.EventMany<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Empty(await repo.EventManyAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == missing));
            Assert.Equal(1, await repo.EventCountAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID));
            Assert.True(await repo.EventAnyAsync<EdgeModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID));
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

            var read = Assert.Single(repo.Many<Owner>(x => true, graph));
            Assert.Equal(new string('i', 100), Assert.Single(read.Items!).Text);
            read = Assert.Single(await repo.ManyAsync<Owner>(x => true, graph));
            Assert.Equal(new string('i', 100), Assert.Single(read.Items!).Text);
        }
    }
}
