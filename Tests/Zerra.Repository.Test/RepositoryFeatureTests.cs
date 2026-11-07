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
            private readonly Dictionary<(string, string), byte[]> files = new();

            public Stream Get(string category, string name) => new MemoryStream(files[(category, name)]);
            public void Save(string category, string name, Stream stream)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                files[(category, name)] = ms.ToArray();
            }
            public void Save(string category, string name, byte[] bytes) => files[(category, name)] = bytes;
            public bool Delete(string category, string name) => files.Remove((category, name));
            public bool Exists(string category, string name) => files.ContainsKey((category, name));

            public Task<Stream> GetAsync(string category, string name, CancellationToken cancellationToken) => Task.FromResult(Get(category, name));
            public Task SaveAsync(string category, string name, Stream stream, CancellationToken cancellationToken)
            {
                Save(category, name, stream);
                return Task.CompletedTask;
            }
            public Task SaveAsync(string category, string name, byte[] bytes, CancellationToken cancellationToken)
            {
                Save(category, name, bytes);
                return Task.CompletedTask;
            }
            public Task<bool> DeleteAsync(string category, string name, CancellationToken cancellationToken) => Task.FromResult(Delete(category, name));
            public Task<bool> ExistsAsync(string category, string name, CancellationToken cancellationToken) => Task.FromResult(Exists(category, name));
        }

        //reverses the bytes on the way in and out, so what's stored differs from what's read; only the stream hooks, so bytes go through them by default
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
            protected override Task<Stream> OnSaveAsync(Stream stream, CancellationToken cancellationToken) => Task.FromResult(Reverse(stream));
            protected override Task<Stream> OnGetAsync(Stream stream, CancellationToken cancellationToken) => Task.FromResult(Reverse(stream));
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
            var cancellationToken = TestContext.Current.CancellationToken;

            var engine = new DictionaryByteStoreEngine();
            var raw = new ByteStoreProvider(engine);
            var layered = new PassThroughLayer(new ReversingLayer(raw));

            Assert.False(layered.Exists("docs", "file"));
            layered.Save("docs", "file", new MemoryStream([1, 2, 3]));
            Assert.True(layered.Exists("docs", "file"));
            Assert.False(layered.Exists("other", "file"));
            Assert.Equal([1, 2, 3], Read(layered.Get("docs", "file")));
            Assert.Equal([3, 2, 1], Read(raw.Get("docs", "file")));

            Assert.False(await layered.ExistsAsync("docs", "async", cancellationToken));
            await layered.SaveAsync("docs", "async", new MemoryStream([4, 5]), cancellationToken);
            Assert.True(await layered.ExistsAsync("docs", "async", cancellationToken));
            Assert.Equal([4, 5], Read(await layered.GetAsync("docs", "async", cancellationToken)));
            Assert.Equal([5, 4], Read(await raw.GetAsync("docs", "async", cancellationToken)));
            Assert.True(await raw.ExistsAsync("docs", "async", cancellationToken));
            await raw.SaveAsync("docs", "raw", new MemoryStream([6]), cancellationToken);
            Assert.Equal([6], Read(raw.Get("docs", "raw")));

            //the same name in another category is another entry
            layered.Save("other", "file", [7]);
            Assert.Equal([7], Read(layered.Get("other", "file")));
            Assert.Equal([1, 2, 3], Read(layered.Get("docs", "file")));

            //bytes go through the stream hooks of a layer that only has those
            layered.Save("docs", "bytes", [1, 2]);
            Assert.Equal([2, 1], Read(raw.Get("docs", "bytes")));
            Assert.Equal([1, 2], Read(layered.Get("docs", "bytes")));
            await layered.SaveAsync("docs", "bytesAsync", [3, 4], cancellationToken);
            Assert.Equal([4, 3], Read(raw.Get("docs", "bytesAsync")));
            Assert.Equal([3, 4], Read(await layered.GetAsync("docs", "bytesAsync", cancellationToken)));
            raw.Save("docs", "rawBytes", [8]);
            await raw.SaveAsync("docs", "rawBytesAsync", [9], cancellationToken);
            Assert.Equal([8], Read(raw.Get("docs", "rawBytes")));
            Assert.Equal([9], Read(raw.Get("docs", "rawBytesAsync")));

            //several at once, each through the layers
            layered.Save("many", [new("a", [1, 2]), new("b", [3, 4, 5])]);
            Assert.Equal([2, 1], Read(raw.Get("many", "a")));
            Assert.Equal([1, 2], Read(layered.Get("many", "a")));
            Assert.Equal([3, 4, 5], Read(layered.Get("many", "b")));
            await layered.SaveAsync("manyAsync", [new("c", [6, 7]), new("d", [8])], cancellationToken);
            Assert.Equal([7, 6], Read(raw.Get("manyAsync", "c")));
            Assert.Equal([6, 7], Read(await layered.GetAsync("manyAsync", "c", cancellationToken)));
            Assert.Equal([8], Read(await layered.GetAsync("manyAsync", "d", cancellationToken)));
            raw.Save("rawMany", [new("e", [1])]);
            await raw.SaveAsync("rawMany", [new("f", [2])], cancellationToken);
            Assert.Equal([1], Read(raw.Get("rawMany", "e")));
            Assert.Equal([2], Read(raw.Get("rawMany", "f")));

            Assert.True(layered.Delete("docs", "file"));
            Assert.False(layered.Exists("docs", "file"));
            Assert.False(layered.Delete("docs", "file"));
            Assert.True(layered.Exists("other", "file"));
            Assert.True(await layered.DeleteAsync("docs", "async", cancellationToken));
            Assert.False(await layered.ExistsAsync("docs", "async", cancellationToken));
            Assert.False(await layered.DeleteAsync("docs", "async", cancellationToken));
            Assert.True(raw.Delete("docs", "raw"));
            Assert.True(await raw.DeleteAsync("docs", "rawBytes", cancellationToken));
            Assert.False(raw.Exists("docs", "rawBytes"));
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
            await repo.UpdateAsync(model, TestContext.Current.CancellationToken);

            Assert.Equal(2, repo.Query<DualModel>().SingleOrDefault(x => x.ID == model.ID)!.Value);
            Assert.Equal(2, (await repo.Query<DualModel>().SingleOrDefaultAsync(x => x.ID == model.ID, cancellationToken: TestContext.Current.CancellationToken))!.Value);
            Assert.Equal([1, 2], repo.TemporalQuery<DualModel>(TemporalOrder.Oldest).Where(x => x.ID == model.ID).ToList().Select(x => x.Value));
            Assert.Equal([1, 2], (await repo.TemporalQuery<DualModel>(TemporalOrder.Oldest).Where(x => x.ID == model.ID).ToListAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(x => x.Value));

            //each store has the change
            var table = Repo.New();
            table.AddProvider(new TransactStoreProvider<DualModel>(tableEngine));
            Assert.Equal(2, table.Query<DualModel>().SingleOrDefault(x => x.ID == model.ID)!.Value);
            var events = Repo.New();
            events.AddProvider(new EventStoreAsTransactStoreProvider<DualModel>(eventEngine));
            Assert.Equal(2, events.EventQuery<DualModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == model.ID).LongCount());
        }

        public interface IDualQueryHandler : IQueryHandler
        {
            Task<int> GetValue(Guid id);
        }

        public sealed class DualQueryHandler : BaseHandlerWithRepo, IDualQueryHandler
        {
            public bool Initialized { get; private set; }
            protected override void InitializeBaseHandlerWithRepo() => Initialized = true;
            public async Task<int> GetValue(Guid id) => (await Repo.Query<DualModel>().SingleOrDefaultAsync(x => x.ID == id))!.Value;
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
