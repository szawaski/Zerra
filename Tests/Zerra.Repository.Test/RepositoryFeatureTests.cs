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
