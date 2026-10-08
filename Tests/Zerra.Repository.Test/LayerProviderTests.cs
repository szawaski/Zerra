// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class LayerProviderTests
    {
        [Entity("LayerModel")]
        public sealed class LayerModel
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Text { get; set; }
            public byte[]? Bytes { get; set; }
            public int Number { get; set; }
        }

        private static readonly SymmetricKey key = SymmetricEncryptor.GetKey("layer-test");

        private sealed class EncryptionProvider(ITransactStoreProvider<LayerModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<LayerModel>, LayerModel>(next)
        {
            public override SymmetricKey EncryptionKey => key;
            public override SymmetricAlgorithmType EncryptionAlgorithm => SymmetricAlgorithmType.AES;
        }

        private sealed class CompressionProvider(ITransactStoreProvider<LayerModel> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<LayerModel>, LayerModel>(next)
        {
        }

        public static TheoryData<bool, bool> Layers => new()
        {
            { false, false },
            { false, true },
            { true, false },
            { true, true },
        };

        private static (IRepo Layered, IRepo Raw) CreateRepos(bool compression, bool eventStore)
        {
            var engine = new MemoryEngine();
            ITransactStoreProvider<LayerModel> Root() => eventStore ? new EventStoreAsTransactStoreProvider<LayerModel>(engine) : new TransactStoreProvider<LayerModel>(engine);

            var layered = Repo.New();
            layered.AddProvider<LayerModel>(compression ? new CompressionProvider(Root()) : new EncryptionProvider(Root()));
            var raw = Repo.New();
            raw.AddProvider(Root());
            return (layered, raw);
        }

        private static LayerModel NewModel() => new()
        {
            ID = Guid.NewGuid(),
            Text = new string('x', 200) + "plain text",
            Bytes = Enumerable.Repeat((byte)5, 200).ToArray(),
            Number = 7,
        };

        private static void AssertPlain(LayerModel expected, LayerModel? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.ID, actual.ID);
            Assert.Equal(expected.Text, actual.Text);
            Assert.Equal(expected.Bytes, actual.Bytes);
            Assert.Equal(expected.Number, actual.Number);
        }

        [Theory]
        [MemberData(nameof(Layers))]
        public void Sync_StoresTransformedAndReadsPlain(bool compression, bool eventStore)
        {
            var (layered, raw) = CreateRepos(compression, eventStore);
            var model = NewModel();
            var expected = NewModel();
            expected.ID = model.ID;

            layered.Create("Created", model);

            //the caller's model isn't changed by the layer
            AssertPlain(expected, model);

            var stored = raw.Single<LayerModel>(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.NotEqual(expected.Text, stored.Text);
            Assert.NotEqual(expected.Bytes, stored.Bytes);
            Assert.Equal(expected.Number, stored.Number);

            AssertPlain(expected, layered.Single<LayerModel>(x => x.ID == model.ID));
            AssertPlain(expected, layered.First<LayerModel>(x => x.ID == model.ID));
            AssertPlain(expected, Assert.Single(layered.Many<LayerModel>(x => x.ID == model.ID)));
            Assert.Equal(1, layered.Count<LayerModel>(x => x.ID == model.ID));
            Assert.True(layered.Any<LayerModel>(x => x.ID == model.ID));
            Assert.Null(layered.Single<LayerModel>(x => x.ID == Guid.Empty));
            Assert.Null(layered.First<LayerModel>(x => x.ID == Guid.Empty));
            Assert.Empty(layered.Many<LayerModel>(x => x.ID == Guid.Empty));

            //a graph only transforms its members
            var textOnly = layered.Single<LayerModel>(x => x.ID == model.ID, new Graph<LayerModel>(x => x.ID, x => x.Text));
            Assert.Equal(expected.Text, textOnly!.Text);

            expected.Text = "updated";
            model.Text = "updated";
            layered.Update("Updated", model);
            AssertPlain(expected, model);
            AssertPlain(expected, layered.Single<LayerModel>(x => x.ID == model.ID));
            Assert.NotEqual(expected.Text, raw.Single<LayerModel>(x => x.ID == model.ID)!.Text);

            if (eventStore)
            {
                var events = layered.EventMany<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID);
                Assert.Equal(2, events.Count);
                Assert.Equal(new string('x', 200) + "plain text", events.First().Model.Text);
                Assert.Equal("updated", events.Last().Model.Text);
                Assert.All(events, x => Assert.Equal(expected.Bytes, x.Model.Bytes));
                Assert.Equal("updated", layered.EventFirst<LayerModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID)!.Model.Text);
                Assert.Equal(2, layered.EventCount<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID));
                Assert.True(layered.EventAny<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID));
            }

            layered.Delete("Deleted", model);
            Assert.Null(layered.Single<LayerModel>(x => x.ID == model.ID));
        }

        [Theory]
        [MemberData(nameof(Layers))]
        public async Task Async_StoresTransformedAndReadsPlain(bool compression, bool eventStore)
        {
            var (layered, raw) = CreateRepos(compression, eventStore);
            var model = NewModel();
            var expected = NewModel();
            expected.ID = model.ID;

            await layered.CreateAsync("Created", model);

            AssertPlain(expected, model);

            var stored = await raw.SingleAsync<LayerModel>(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.NotEqual(expected.Text, stored.Text);
            Assert.NotEqual(expected.Bytes, stored.Bytes);

            AssertPlain(expected, await layered.SingleAsync<LayerModel>(x => x.ID == model.ID));
            AssertPlain(expected, await layered.FirstAsync<LayerModel>(x => x.ID == model.ID));
            AssertPlain(expected, Assert.Single(await layered.ManyAsync<LayerModel>(x => x.ID == model.ID)));
            Assert.Equal(1, await layered.CountAsync<LayerModel>(x => x.ID == model.ID));
            Assert.True(await layered.AnyAsync<LayerModel>(x => x.ID == model.ID));
            Assert.Null(await layered.SingleAsync<LayerModel>(x => x.ID == Guid.Empty));
            Assert.Null(await layered.FirstAsync<LayerModel>(x => x.ID == Guid.Empty));
            Assert.Empty(await layered.ManyAsync<LayerModel>(x => x.ID == Guid.Empty));

            expected.Text = "updated";
            model.Text = "updated";
            await layered.UpdateAsync("Updated", model);
            AssertPlain(expected, model);
            AssertPlain(expected, await layered.SingleAsync<LayerModel>(x => x.ID == model.ID));

            if (eventStore)
            {
                var events = await layered.EventManyAsync<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID);
                Assert.Equal(2, events.Count);
                Assert.Equal(new string('x', 200) + "plain text", events.First().Model.Text);
                Assert.Equal("updated", events.Last().Model.Text);
                Assert.Equal("updated", (await layered.EventFirstAsync<LayerModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == model.ID))!.Model.Text);
                Assert.Equal(2, await layered.EventCountAsync<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID));
                Assert.True(await layered.EventAnyAsync<LayerModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID));
            }

            await layered.DeleteAsync("Deleted", model);
            Assert.Null(await layered.SingleAsync<LayerModel>(x => x.ID == model.ID));
        }
    }
}
