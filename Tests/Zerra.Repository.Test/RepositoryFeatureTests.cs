// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class RepositoryFeatureTests
    {
        private sealed class DictionaryByteStoreEngine : IByteStoreEngine
        {
            private readonly Dictionary<string, byte[]> files = new();

            public bool Exists(string name) => files.ContainsKey(name);
            public Stream Get(string name) => new MemoryStream(files[name]);
            public void Save(string name, Stream stream)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                files[name] = ms.ToArray();
            }
            public Task<bool> ExistsAsync(string name) => Task.FromResult(Exists(name));
            public Task<Stream> GetAsync(string name) => Task.FromResult(Get(name));
            public Task SaveAsync(string name, Stream stream)
            {
                Save(name, stream);
                return Task.CompletedTask;
            }
        }

        //reverses the bytes on the way in and out, so what's stored differs from what's read
        private sealed class ReversingLayer(IByteStoreProvider next) : BaseByteStoreLayerProvider<IByteStoreProvider>(next)
        {
            private static Stream Reverse(Stream stream)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return new MemoryStream(ms.ToArray().Reverse().ToArray());
            }
            protected override Stream OnSave(Stream stream) => Reverse(stream);
            protected override Stream OnGet(Stream stream) => Reverse(stream);
            protected override Task<Stream> OnSaveAsync(Stream stream) => Task.FromResult(Reverse(stream));
            protected override Task<Stream> OnGetAsync(Stream stream) => Task.FromResult(Reverse(stream));
        }

        private sealed class PassThroughLayer(IByteStoreProvider next) : BaseByteStoreLayerProvider<IByteStoreProvider>(next)
        {
        }

        private static byte[] Read(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        [Fact]
        public async Task ByteStore_SavesAndReadsThroughLayers()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new ByteStoreProvider(null!));

            var engine = new DictionaryByteStoreEngine();
            var raw = new ByteStoreProvider(engine);
            var layered = new PassThroughLayer(new ReversingLayer(raw));

            Assert.False(layered.Exists("file"));
            layered.Save("file", new MemoryStream([1, 2, 3]));
            Assert.True(layered.Exists("file"));
            Assert.Equal([1, 2, 3], Read(layered.Get("file")));
            Assert.Equal([3, 2, 1], Read(raw.Get("file")));

            Assert.False(await layered.ExistsAsync("async"));
            await layered.SaveAsync("async", new MemoryStream([4, 5]));
            Assert.True(await layered.ExistsAsync("async"));
            Assert.Equal([4, 5], Read(await layered.GetAsync("async")));
            Assert.Equal([5, 4], Read(await raw.GetAsync("async")));
            Assert.True(await raw.ExistsAsync("async"));
            await raw.SaveAsync("raw", new MemoryStream([6]));
            Assert.Equal([6], Read(raw.Get("raw")));
        }

        [Entity("DualModel")]
        public sealed class DualModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
        }

        //changes go to both stores, temporal queries read the event store and the rest read the table
        private sealed class DualProvider(ITransactStoreProvider<DualModel> eventStore, ITransactStoreProvider<DualModel> table)
            : BaseDualEventStoreProvider<ITransactStoreProvider<DualModel>, ITransactStoreProvider<DualModel>, DualModel>(eventStore, table)
        {
        }

        [Fact]
        public async Task DualEventStore_WritesBothReadsEach()
        {
            var eventEngine = new MemoryEngine();
            var tableEngine = new MemoryEngine();
            var provider = new DualProvider(new EventStoreAsTransactStoreProvider<DualModel>(eventEngine), new TransactStoreProvider<DualModel>(tableEngine));
            Assert.Equal(typeof(DualModel), provider.ModelType);
            var repo = Repo.New();
            repo.AddProvider(provider);

            var model = new DualModel { ID = Guid.NewGuid(), Value = 1 };
            repo.Create(model);
            model.Value = 2;
            await repo.UpdateAsync(model);

            Assert.Equal(2, repo.Single<DualModel>(x => x.ID == model.ID)!.Value);
            Assert.Equal(2, (await repo.SingleAsync<DualModel>(x => x.ID == model.ID))!.Value);
            Assert.Equal([1, 2], repo.TemporalMany<DualModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID).Select(x => x.Value));
            Assert.Equal([1, 2], (await repo.TemporalManyAsync<DualModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID)).Select(x => x.Value));

            //each store has the change
            var table = Repo.New();
            table.AddProvider(new TransactStoreProvider<DualModel>(tableEngine));
            Assert.Equal(2, table.Single<DualModel>(x => x.ID == model.ID)!.Value);
            var events = Repo.New();
            events.AddProvider(new EventStoreAsTransactStoreProvider<DualModel>(eventEngine));
            Assert.Equal(2, events.EventCount<DualModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == model.ID));
        }

        public interface IDualQueryHandler : IQueryHandler
        {
            Task<int> GetValue(Guid id);
        }

        public sealed class DualQueryHandler : BaseHandlerWithRepo, IDualQueryHandler
        {
            public bool Initialized { get; private set; }
            protected override void InitializeBaseHandlerWithRepo() => Initialized = true;
            public async Task<int> GetValue(Guid id) => (await Repo.SingleAsync<DualModel>(x => x.ID == id))!.Value;
        }

        [Fact]
        public async Task HandlerWithRepo_UsesTheBusRepo()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<DualModel>(new MemoryEngine()));
            var model = new DualModel { ID = Guid.NewGuid(), Value = 7 };
            repo.Create(model);

            var services = new BusServices();
            services.AddRepo(repo);
            var bus = Bus.New("repo-test", null, null, services);
            var handler = new DualQueryHandler();
            bus.AddHandler<IDualQueryHandler>(handler);
            try
            {
                Assert.Equal(7, await bus.Call<IDualQueryHandler>().GetValue(model.ID));
                Assert.True(handler.Initialized);
                Assert.Same(repo, handler.Repo);
            }
            finally
            {
                await bus.StopServicesAsync();
            }
        }
    }
}
