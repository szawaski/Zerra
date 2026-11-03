// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //a model saved three times, as Value 1, 2, then 3 in events 0, 1, and 2
    public class TemporalTests
    {
        [Entity("TemporalModel")]
        public sealed class TemporalModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
        }

        //saving the state with every event reads from the saved states instead of replaying from the start
        public static TheoryData<bool> Snapshots => new() { false, true };

        private static (IRepo, Guid, DateTime[]) Create(bool snapshots)
        {
            var repo = Repo.New();
            var engine = new MemoryEngine();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<TemporalModel>(engine, snapshots ? 1UL : 100UL));
            var model = new TemporalModel { ID = Guid.NewGuid(), Value = 1 };
            var dates = new DateTime[3];
            repo.Create(model);
            dates[0] = DateTime.UtcNow;
            for (var i = 1; i < 3; i++)
            {
                Thread.Sleep(20);
                model.Value = i + 1;
                repo.Update(model);
                dates[i] = DateTime.UtcNow;
            }
            return (repo, model.ID, dates);
        }

        private static int[] Values(IEnumerable<TemporalModel> models) => models.Select(x => x.Value).ToArray();
        private static int[] Values(IEnumerable<EventModel<TemporalModel>> models) => models.Select(x => x.Model.Value).ToArray();

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task NotTemporal_ReadsTheCurrentState(bool snapshots)
        {
            var (repo, id, _) = Create(snapshots);

            Assert.Equal([3], Values(repo.Query<TemporalModel>().Where(x => x.ID == id).ToArray()));
            Assert.Equal(1, repo.Query<TemporalModel>().Count(x => x.ID == id));
            Assert.Equal(3, repo.Query<TemporalModel>().SingleOrDefault(x => x.ID == id)!.Value);
            Assert.Equal(new[] { 3 }, Values(await repo.Query<TemporalModel>().Where(x => x.ID == id).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(1, await repo.Query<TemporalModel>().CountAsync(x => x.ID == id, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public void TemporalMany(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal([1, 2, 3], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.ID == id).ToList()));
            Assert.Equal([2, 3], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.ID == id).ToList()));
            Assert.Equal([1, 2], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, numberTo: 1).Where(x => x.ID == id).ToList()));
            Assert.Equal([2], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.ID == id).Skip(1).Take(1).ToList()));
            Assert.Equal([1, 2], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, dateTo: dates[1]).Where(x => x.ID == id).ToList()));
            Assert.Equal([2, 3], Values(repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, dateFrom: dates[0].AddMilliseconds(5)).Where(x => x.ID == id).ToList()));

            Assert.Equal([1, 2, 3], Values(repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).ToList()).Order());
            Assert.Equal([3], Values(repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).Take(1).ToList()));
            Assert.Equal([1, 2], Values(repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).Skip(1).ToList()).Order());
            Assert.Equal([2], Values(repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).Skip(1).Take(1).ToList()));
            Assert.Equal([1, 2], Values(repo.TemporalQuery<TemporalModel>(numberTo: 1).Where(x => x.ID == id).ToList()).Order());
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public void TemporalFirst(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal(3, repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(2, repo.TemporalQuery<TemporalModel>(numberTo: 1).Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(1, repo.TemporalQuery<TemporalModel>(numberTo: 0).Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(2, repo.TemporalQuery<TemporalModel>(dateTo: dates[1]).Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(1, repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(2, repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.ID == id).FirstOrDefault()!.Value);
            Assert.Equal(2, repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, dateFrom: dates[0].AddMilliseconds(5)).Where(x => x.ID == id).FirstOrDefault()!.Value);
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task TemporalAsync(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal(new[] { 2 }, Values(await repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.ID == id).Skip(1).Take(1).ToListAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(new[] { 2 }, Values(await repo.TemporalQuery<TemporalModel>().Where(x => x.ID == id).Skip(1).Take(1).ToListAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(2, (await repo.TemporalQuery<TemporalModel>(numberTo: 1).Where(x => x.ID == id).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Value);
            Assert.Equal(2, (await repo.TemporalQuery<TemporalModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.ID == id).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Value);
            Assert.Equal(2, (await repo.TemporalQuery<TemporalModel>(dateTo: dates[1]).Where(x => x.ID == id).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Value);
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task EventMany(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal([1, 2, 3], Values(repo.EventQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == id).ToList()));
            Assert.Equal([1, 2], Values(repo.EventQuery<TemporalModel>(TemporalOrder.Oldest, numberTo: 1).Where(x => x.Model.ID == id).ToList()));
            Assert.Equal([2], Values(repo.EventQuery<TemporalModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == id).Skip(1).Take(1).ToList()));
            Assert.Equal([3], Values(repo.EventQuery<TemporalModel>().Where(x => x.Model.ID == id).Take(1).ToList()));
            Assert.Equal([2], Values(repo.EventQuery<TemporalModel>().Where(x => x.Model.ID == id).Skip(1).Take(1).ToList()));
            Assert.Equal([1, 2], Values(repo.EventQuery<TemporalModel>().Where(x => x.Model.ID == id).Skip(1).ToList()).Order());
            Assert.Equal([1, 2], Values(repo.EventQuery<TemporalModel>(dateTo: dates[1]).Where(x => x.Model.ID == id).ToList()).Order());
            Assert.Equal(2, repo.EventQuery<TemporalModel>(numberTo: 1).Where(x => x.Model.ID == id).FirstOrDefault()!.Model.Value);
            Assert.Equal(2, repo.EventQuery<TemporalModel>(TemporalOrder.Oldest, numberFrom: 1).Where(x => x.Model.ID == id).FirstOrDefault()!.Model.Value);

            Assert.Equal(new[] { 2 }, Values(await repo.EventQuery<TemporalModel>().Where(x => x.Model.ID == id).Skip(1).Take(1).ToListAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(2, (await repo.EventQuery<TemporalModel>(numberTo: 1).Where(x => x.Model.ID == id).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken))!.Model.Value);
        }

        [Entity("TemporalPartsModel")]
        public sealed class PartsModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
            public string? Name { get; set; }
        }

        //saved states must give the same answers as replaying every event, including updates of only some members
        [Fact]
        public async Task SavedStates_MatchReplayingEveryEvent()
        {
            var engine = new MemoryEngine();
            var saved = Repo.New();
            saved.AddProvider(new EventStoreAsTransactStoreProvider<PartsModel>(engine, 3));
            var replayed = Repo.New();
            replayed.AddProvider(new EventStoreAsTransactStoreProvider<PartsModel>(engine, 0));

            var model = new PartsModel { ID = Guid.NewGuid(), Value = 0, Name = "0" };
            saved.Create(model);
            var dates = new List<DateTime> { DateTime.UtcNow };
            for (var i = 1; i < 10; i++)
            {
                Thread.Sleep(5);
                var change = new PartsModel { ID = model.ID, Value = i, Name = $"{i}" };
                if (i % 2 == 0)
                    await saved.UpdateAsync(change, new Graph<PartsModel>(x => x.ID, x => x.Value), TestContext.Current.CancellationToken);
                else
                    saved.Update(change, new Graph<PartsModel>(x => x.ID, x => x.Name));
                dates.Add(DateTime.UtcNow);
            }

            static string Text(IEnumerable<PartsModel> models) => String.Join(",", models.Select(x => $"{x.Value}:{x.Name}").Order());
            static string EventText(IEnumerable<EventModel<PartsModel>> models) => String.Join(",", models.Select(x => $"{x.Number}={x.Model.Value}:{x.Model.Name}").Order());
            var id = model.ID;

            foreach (var order in new[] { TemporalOrder.Oldest, TemporalOrder.Newest })
            {
                foreach (var (skip, take) in new (int?, int?)[] { (null, null), (1, 2), (null, 1) })
                {
                    for (ulong from = 0; from < 10; from++)
                    {
                        for (var to = from; to < 10; to++)
                        {
                            var label = $"{order} {from}-{to} {skip}/{take}";
                            Assert.True(Text(replayed.TemporalQuery<PartsModel>(order, numberFrom: from, numberTo: to).Where(x => x.ID == id).Page(skip, take).ToList()) == Text(saved.TemporalQuery<PartsModel>(order, numberFrom: from, numberTo: to).Where(x => x.ID == id).Page(skip, take).ToList()), label);
                            Assert.True(EventText(replayed.EventQuery<PartsModel>(order, numberFrom: from, numberTo: to).Where(x => x.Model.ID == id).Page(skip, take).ToList()) == EventText(saved.EventQuery<PartsModel>(order, numberFrom: from, numberTo: to).Where(x => x.Model.ID == id).Page(skip, take).ToList()), label);
                        }
                        Assert.True(Text([replayed.TemporalQuery<PartsModel>(order, numberTo: from).Where(x => x.ID == id).FirstOrDefault()!]) == Text([saved.TemporalQuery<PartsModel>(order, numberTo: from).Where(x => x.ID == id).FirstOrDefault()!]), $"{order} to {from}");
                        Assert.True(Text([replayed.TemporalQuery<PartsModel>(order, numberFrom: from).Where(x => x.ID == id).FirstOrDefault()!]) == Text([saved.TemporalQuery<PartsModel>(order, numberFrom: from).Where(x => x.ID == id).FirstOrDefault()!]), $"{order} from {from}");
                        Assert.True(EventText([replayed.EventQuery<PartsModel>(order, numberTo: from).Where(x => x.Model.ID == id).FirstOrDefault()!]) == EventText([saved.EventQuery<PartsModel>(order, numberTo: from).Where(x => x.Model.ID == id).FirstOrDefault()!]), $"{order} event to {from}");
                        Assert.True(EventText([replayed.EventQuery<PartsModel>(order, numberFrom: from).Where(x => x.Model.ID == id).FirstOrDefault()!]) == EventText([saved.EventQuery<PartsModel>(order, numberFrom: from).Where(x => x.Model.ID == id).FirstOrDefault()!]), $"{order} event from {from}");

                        var date = dates[(int)from];
                        Assert.True(Text(replayed.TemporalQuery<PartsModel>(order, dateFrom: date).Where(x => x.ID == id).ToList()) == Text(saved.TemporalQuery<PartsModel>(order, dateFrom: date).Where(x => x.ID == id).ToList()), $"{order} date from {from}");
                        Assert.True(Text([replayed.TemporalQuery<PartsModel>(order, dateTo: date).Where(x => x.ID == id).FirstOrDefault()!]) == Text([saved.TemporalQuery<PartsModel>(order, dateTo: date).Where(x => x.ID == id).FirstOrDefault()!]), $"{order} date to {from}");
                    }
                }
            }

            Assert.Equal("8:9", Text([saved.Query<PartsModel>().SingleOrDefault(x => x.ID == id)!]));
            Assert.Equal("8:9", Text([(await saved.Query<PartsModel>().SingleOrDefaultAsync(x => x.ID == id, cancellationToken: TestContext.Current.CancellationToken))!]));
        }
    }

    internal static class TemporalPaging
    {
        public static IQueryable<T> Page<T>(this IQueryable<T> query, int? skip, int? take)
        {
            if (skip.HasValue)
                query = query.Skip(skip.Value);
            if (take.HasValue)
                query = query.Take(take.Value);
            return query;
        }
    }
}
