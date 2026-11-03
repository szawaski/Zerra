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

        private static readonly IEncryptor encryptor = new ZerraEncryptor("layer-test", SymmetricAlgorithmType.AES_GCM);

        private sealed class EncryptionProvider(ITransactStoreProvider<LayerModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<LayerModel>, LayerModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
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

            var stored = raw.Query<LayerModel>().SingleOrDefault(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.NotEqual(expected.Text, stored.Text);
            Assert.NotEqual(expected.Bytes, stored.Bytes);
            Assert.Equal(expected.Number, stored.Number);

            AssertPlain(expected, layered.Query<LayerModel>().SingleOrDefault(x => x.ID == model.ID));
            AssertPlain(expected, layered.Query<LayerModel>().FirstOrDefault(x => x.ID == model.ID));
            AssertPlain(expected, Assert.Single(layered.Query<LayerModel>().Where(x => x.ID == model.ID).ToArray()));
            Assert.Equal(1, layered.Query<LayerModel>().Count(x => x.ID == model.ID));
            Assert.True(layered.Query<LayerModel>().Any(x => x.ID == model.ID));
            Assert.Null(layered.Query<LayerModel>().SingleOrDefault(x => x.ID == Guid.Empty));
            Assert.Null(layered.Query<LayerModel>().FirstOrDefault(x => x.ID == Guid.Empty));
            Assert.Empty(layered.Query<LayerModel>().Where(x => x.ID == Guid.Empty).ToArray());

            expected.Text = "updated";
            model.Text = "updated";
            layered.Update("Updated", model);
            AssertPlain(expected, model);
            AssertPlain(expected, layered.Query<LayerModel>().SingleOrDefault(x => x.ID == model.ID));
            Assert.NotEqual(expected.Text, raw.Query<LayerModel>().SingleOrDefault(x => x.ID == model.ID)!.Text);

            if (eventStore)
            {
                var events = layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).ToList();
                Assert.Equal(2, events.Count);
                Assert.Equal(new string('x', 200) + "plain text", events.First().Model.Text);
                Assert.Equal("updated", events.Last().Model.Text);
                Assert.All(events, x => Assert.Equal(expected.Bytes, x.Model.Bytes));
                Assert.Equal("updated", layered.EventQuery<LayerModel>().Where(x => x.Model.ID == model.ID).FirstOrDefault()!.Model.Text);
                Assert.Equal(2, layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).LongCount());
                Assert.True(layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).Any());
            }

            layered.Delete("Deleted", model);
            Assert.Null(layered.Query<LayerModel>().SingleOrDefault(x => x.ID == model.ID));
        }

        [Theory]
        [MemberData(nameof(Layers))]
        public async Task Async_StoresTransformedAndReadsPlain(bool compression, bool eventStore)
        {
            var (layered, raw) = CreateRepos(compression, eventStore);
            var model = NewModel();
            var expected = NewModel();
            expected.ID = model.ID;

            await layered.CreateAsync("Created", model, TestContext.Current.CancellationToken);

            AssertPlain(expected, model);

            var stored = await raw.Query<LayerModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.NotEqual(expected.Text, stored.Text);
            Assert.NotEqual(expected.Bytes, stored.Bytes);

            AssertPlain(expected, await layered.Query<LayerModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));
            AssertPlain(expected, await layered.Query<LayerModel>().FirstOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));
            AssertPlain(expected, Assert.Single(await layered.Query<LayerModel>().Where(x => x.ID == model.ID).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(1, await layered.Query<LayerModel>().CountAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(await layered.Query<LayerModel>().AnyAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await layered.Query<LayerModel>().SingleOrDefaultAsync(x => x.ID == Guid.Empty, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await layered.Query<LayerModel>().FirstOrDefaultAsync(x => x.ID == Guid.Empty, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Empty(await layered.Query<LayerModel>().Where(x => x.ID == Guid.Empty).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken));

            expected.Text = "updated";
            model.Text = "updated";
            await layered.UpdateAsync("Updated", model, TestContext.Current.CancellationToken);
            AssertPlain(expected, model);
            AssertPlain(expected, await layered.Query<LayerModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));

            if (eventStore)
            {
                var events = await layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal(2, events.Count);
                Assert.Equal(new string('x', 200) + "plain text", events.First().Model.Text);
                Assert.Equal("updated", events.Last().Model.Text);
                Assert.Equal("updated", (await layered.EventQuery<LayerModel>().Where(x => x.Model.ID == model.ID).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Model.Text);
                Assert.Equal(2, await layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).LongCountAsync(cancellationToken: TestContext.Current.CancellationToken));
                Assert.True(await layered.EventQuery<LayerModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
            }

            await layered.DeleteAsync("Deleted", model, TestContext.Current.CancellationToken);
            Assert.Null(await layered.Query<LayerModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken));
        }
    }
}
