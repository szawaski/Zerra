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
        public async Task NotTemporal_ManyIsEveryStateAndSingleTheCurrent(bool snapshots)
        {
            var (repo, id, _) = Create(snapshots);

            Assert.Equal([1, 2, 3], Values(repo.Many<TemporalModel>(x => x.ID == id)));
            Assert.Equal(3, repo.Count<TemporalModel>(x => x.ID == id));
            Assert.Equal(3, repo.Single<TemporalModel>(x => x.ID == id)!.Value);
            Assert.Equal(new[] { 1, 2, 3 }, Values(await repo.ManyAsync<TemporalModel>(x => x.ID == id)));
            Assert.Equal(3, await repo.CountAsync<TemporalModel>(x => x.ID == id));
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public void TemporalMany(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal([1, 2, 3], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == id)));
            Assert.Equal([2, 3], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)1, null, null, null, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, 1, null, null, x => x.ID == id)));
            Assert.Equal([2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, (DateTime?)null, dates[1], null, null, x => x.ID == id)));
            Assert.Equal([2, 3], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Oldest, dates[0].AddMilliseconds(5), null, null, null, x => x.ID == id)));

            Assert.Equal([1, 2, 3], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == id)).Order());
            Assert.Equal([3], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, null, 1, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, null, x => x.ID == id)).Order());
            Assert.Equal([2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.TemporalMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 1, null, null, x => x.ID == id)).Order());
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public void TemporalFirst(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal(3, repo.TemporalFirst<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(2, repo.TemporalFirst<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 1, null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(1, repo.TemporalFirst<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 0, null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(2, repo.TemporalFirst<TemporalModel>(TemporalOrder.Newest, (DateTime?)null, dates[1], null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(1, repo.TemporalFirst<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(2, repo.TemporalFirst<TemporalModel>(TemporalOrder.Oldest, (ulong?)1, null, null, null, x => x.ID == id, null, null)!.Value);
            Assert.Equal(2, repo.TemporalFirst<TemporalModel>(TemporalOrder.Oldest, dates[0].AddMilliseconds(5), null, null, null, x => x.ID == id, null, null)!.Value);
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task TemporalAsync(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal(new[] { 2 }, Values(await repo.TemporalManyAsync<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal(new[] { 2 }, Values(await repo.TemporalManyAsync<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal(2, (await repo.TemporalFirstAsync<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 1, null, null, x => x.ID == id, null))!.Value);
            Assert.Equal(2, (await repo.TemporalFirstAsync<TemporalModel>(TemporalOrder.Oldest, (ulong?)1, null, null, null, x => x.ID == id, null))!.Value);
            Assert.Equal(2, (await repo.TemporalFirstAsync<TemporalModel>(TemporalOrder.Newest, (DateTime?)null, dates[1], null, null, x => x.ID == id, null))!.Value);
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task EventMany(bool snapshots)
        {
            var (repo, id, dates) = Create(snapshots);

            Assert.Equal([1, 2, 3], Values(repo.EventMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, null, null, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.EventMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, 1, null, null, x => x.ID == id)));
            Assert.Equal([2], Values(repo.EventMany<TemporalModel>(TemporalOrder.Oldest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal([3], Values(repo.EventMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, null, 1, x => x.ID == id)));
            Assert.Equal([2], Values(repo.EventMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal([1, 2], Values(repo.EventMany<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, null, x => x.ID == id)).Order());
            Assert.Equal([1, 2], Values(repo.EventMany<TemporalModel>(TemporalOrder.Newest, (DateTime?)null, dates[1], null, null, x => x.ID == id)).Order());
            Assert.Equal(2, repo.EventFirst<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 1, null, null, x => x.ID == id)!.Model.Value);
            Assert.Equal(2, repo.EventFirst<TemporalModel>(TemporalOrder.Oldest, (ulong?)1, null, null, null, x => x.ID == id)!.Model.Value);

            Assert.Equal(new[] { 2 }, Values(await repo.EventManyAsync<TemporalModel>(TemporalOrder.Newest, (ulong?)null, null, 1, 1, x => x.ID == id)));
            Assert.Equal(2, (await repo.EventFirstAsync<TemporalModel>(TemporalOrder.Newest, (ulong?)null, 1, null, null, x => x.ID == id))!.Model.Value);
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
                    await saved.UpdateAsync(change, new Graph<PartsModel>(x => x.ID, x => x.Value));
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
                            Assert.True(Text(replayed.TemporalMany<PartsModel>(order, (ulong?)from, to, skip, take, x => x.ID == id)) == Text(saved.TemporalMany<PartsModel>(order, (ulong?)from, to, skip, take, x => x.ID == id)), label);
                            Assert.True(EventText(replayed.EventMany<PartsModel>(order, (ulong?)from, to, skip, take, x => x.ID == id)) == EventText(saved.EventMany<PartsModel>(order, (ulong?)from, to, skip, take, x => x.ID == id)), label);
                        }
                        Assert.True(Text([replayed.TemporalFirst<PartsModel>(order, (ulong?)null, from, null, null, x => x.ID == id, null, null)!]) == Text([saved.TemporalFirst<PartsModel>(order, (ulong?)null, from, null, null, x => x.ID == id, null, null)!]), $"{order} to {from}");
                        Assert.True(Text([replayed.TemporalFirst<PartsModel>(order, (ulong?)from, null, null, null, x => x.ID == id, null, null)!]) == Text([saved.TemporalFirst<PartsModel>(order, (ulong?)from, null, null, null, x => x.ID == id, null, null)!]), $"{order} from {from}");
                        Assert.True(EventText([replayed.EventFirst<PartsModel>(order, (ulong?)null, from, null, null, x => x.ID == id)!]) == EventText([saved.EventFirst<PartsModel>(order, (ulong?)null, from, null, null, x => x.ID == id)!]), $"{order} event to {from}");
                        Assert.True(EventText([replayed.EventFirst<PartsModel>(order, (ulong?)from, null, null, null, x => x.ID == id)!]) == EventText([saved.EventFirst<PartsModel>(order, (ulong?)from, null, null, null, x => x.ID == id)!]), $"{order} event from {from}");

                        var date = dates[(int)from];
                        Assert.True(Text(replayed.TemporalMany<PartsModel>(order, date, null, null, null, x => x.ID == id)) == Text(saved.TemporalMany<PartsModel>(order, date, null, null, null, x => x.ID == id)), $"{order} date from {from}");
                        Assert.True(Text([replayed.TemporalFirst<PartsModel>(order, (DateTime?)null, date, null, null, x => x.ID == id, null, null)!]) == Text([saved.TemporalFirst<PartsModel>(order, (DateTime?)null, date, null, null, x => x.ID == id, null, null)!]), $"{order} date to {from}");
                    }
                }
            }

            Assert.Equal("8:9", Text([saved.Single<PartsModel>(x => x.ID == id)!]));
            Assert.Equal("8:9", Text([(await saved.SingleAsync<PartsModel>(x => x.ID == id))!]));
        }
    }
}
