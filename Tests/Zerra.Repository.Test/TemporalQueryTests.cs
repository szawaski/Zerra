// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //two models with histories: A as Value 1, 2, 3 and B as Value 10, 20
    public class TemporalQueryTests
    {
        [Entity("TemporalQueryModel")]
        public sealed class HistoryModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
            public string? Name { get; set; }
        }

        [Entity("TemporalQueryHolder")]
        public sealed class Holder
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid? ItemID { get; set; }
            [Relation(nameof(ItemID))]
            public Item? Item { get; set; }
        }

        [Entity("TemporalQueryItem")]
        public sealed class Item
        {
            [Identity]
            public Guid ID { get; set; }
            public int Value { get; set; }
        }

        //drops models after they're read rather than adding a condition
        private sealed class DroppingRules(ITransactStoreProvider<HistoryModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<HistoryModel>, HistoryModel>(next)
        {
            public override IEnumerable OnGet(IEnumerable models, Graph? graph) => models.Cast<HistoryModel>().Where(x => x.Name != "hidden").ToArray();
        }

        private sealed class HiddenRules(ITransactStoreProvider<HistoryModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<HistoryModel>, HistoryModel>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (HistoryModel x) => x.Name != "hidden";
        }

        public static TheoryData<bool> Snapshots => new() { false, true };

        //C is created, changed, then deleted, so it has events but no states
        private static Guid CreateDeleted(IRepo repo)
        {
            var c = new HistoryModel { ID = Guid.NewGuid(), Value = 100, Name = "c" };
            repo.Create(c);
            c.Value = 200;
            repo.Update(c);
            repo.Delete(c);
            return c.ID;
        }

        private static (IRepo, Guid, Guid) Create(bool snapshots, Func<ITransactStoreProvider<HistoryModel>, ITransactStoreProvider<HistoryModel>>? layer = null)
        {
            var repo = Repo.New();
            ITransactStoreProvider<HistoryModel> provider = new EventStoreAsTransactStoreProvider<HistoryModel>(new MemoryEngine(), snapshots ? 1UL : 100UL);
            repo.AddProvider(layer is null ? provider : layer(provider));
            var a = new HistoryModel { ID = Guid.NewGuid(), Value = 1, Name = "a" };
            repo.Create(a);
            a.Value = 2;
            repo.Update(a);
            a.Value = 3;
            repo.Update(a);
            var b = new HistoryModel { ID = Guid.NewGuid(), Value = 10, Name = "b" };
            repo.Create(b);
            b.Value = 20;
            repo.Update(b);
            return (repo, a.ID, b.ID);
        }

        private static int[] Values(IEnumerable<HistoryModel> models) => models.Select(x => x.Value).ToArray();

        //a where of identities alone reads only the states a page needs of each model; one that isn't reads them all, so the two agree
        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task PagesAsReadingEveryState(bool snapshots)
        {
            var (repo, a, b) = Create(snapshots);
            var c = CreateDeleted(repo);
            var ids = new[] { a, b, c };
            var cancellationToken = TestContext.Current.CancellationToken;
            var pages = new (int? Skip, int? Take)[] { (null, 1), (null, 2), (1, 1), (1, 2), (2, 3), (3, 1), (0, 5), (4, 4) };
            var ranges = new (ulong? From, ulong? To)[] { (null, null), (1, null), (null, 1), (1, 1) };
            foreach (var order in new[] { TemporalOrder.Newest, TemporalOrder.Oldest })
            {
                foreach (var (from, to) in ranges)
                {
                    foreach (var (skip, take) in pages)
                    {
                        var label = $"{order} {from}-{to} {skip}/{take}";
                        var everyState = Values(repo.TemporalQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.ID) && x.Value > 0).Page(skip, take).ToList());
                        Assert.True(Enumerable.SequenceEqual(everyState, Values(repo.TemporalQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => x.ID == a || x.ID == b || x.ID == c).Page(skip, take).ToList())), label);
                        Assert.True(Enumerable.SequenceEqual(everyState, Values(repo.TemporalQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.ID)).Page(skip, take).ToList())), label);
                        Assert.True(Enumerable.SequenceEqual(everyState, Values(await repo.TemporalQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.ID)).Page(skip, take).ToListAsync(cancellationToken))), label);

                        var everyEvent = repo.EventQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.Model.ID) && x.Number >= 0).Page(skip, take).ToList().Select(x => x.Number).ToArray();
                        Assert.True(Enumerable.SequenceEqual(everyEvent, repo.EventQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.Model.ID)).Page(skip, take).ToList().Select(x => x.Number)), label);
                        Assert.True(Enumerable.SequenceEqual(everyEvent, (await repo.EventQuery<HistoryModel>(order, numberFrom: from, numberTo: to).Where(x => ids.Contains(x.Model.ID)).Page(skip, take).ToListAsync(cancellationToken)).Select(x => x.Number)), label);
                    }
                }
                Assert.Equal(repo.TemporalQuery<HistoryModel>(order).Any(x => x.ID == a), repo.TemporalQuery<HistoryModel>(order).Any(x => x.ID == a && x.Value > 0));
                Assert.Equal(await repo.EventQuery<HistoryModel>(order).AnyAsync(x => x.Model.ID == a, cancellationToken), await repo.EventQuery<HistoryModel>(order).AnyAsync(x => x.Model.ID == a && x.Number >= 0, cancellationToken));
            }

            //a deleted model has no states, and its events are those before the delete
            Assert.Empty(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == c).ToList());
            Assert.Null(repo.TemporalQuery<HistoryModel>().FirstOrDefault(x => x.ID == c));
            Assert.Equal([1UL, 0UL], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == c).Select(x => x.Number).ToList());
            Assert.Equal(200, repo.EventQuery<HistoryModel>().FirstOrDefault(x => x.Model.ID == c)!.Model.Value);
            Assert.Equal(200, (await repo.EventQuery<HistoryModel>().FirstOrDefaultAsync(x => x.Model.ID == c, cancellationToken))!.Model.Value);
            Assert.Equal(100, repo.EventQuery<HistoryModel>(TemporalOrder.Oldest).FirstOrDefault(x => x.Model.ID == c)!.Model.Value);

            Assert.Equal([3, 2, 1, 20, 10], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == b).ToList()));
            Assert.Equal([1, 2, 3, 10, 20], Values(repo.TemporalQuery<HistoryModel>(TemporalOrder.Oldest).Where(x => x.ID == a || x.ID == b).ToList()));
            Assert.Equal([3, 2], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == b).Take(2).ToList()));
            Assert.Equal([1, 20], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == b).Skip(2).Take(2).ToList()));
        }

        //first is the first state in the order that matches the whole condition
        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task FirstMatchesTheWholeCondition(bool snapshots)
        {
            var (repo, a, _) = Create(snapshots);
            var cancellationToken = TestContext.Current.CancellationToken;

            Assert.Equal(3, repo.TemporalQuery<HistoryModel>().FirstOrDefault(x => x.ID == a)!.Value);
            Assert.Equal(2, repo.TemporalQuery<HistoryModel>().FirstOrDefault(x => x.ID == a && x.Value < 3)!.Value);
            Assert.Equal(2, (await repo.TemporalQuery<HistoryModel>(TemporalOrder.Oldest).FirstOrDefaultAsync(x => x.ID == a && x.Value > 1, cancellationToken))!.Value);
            Assert.Null(repo.TemporalQuery<HistoryModel>(numberTo: 1).FirstOrDefault(x => x.ID == a && x.Value == 3));
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task ComposesOverTheHistory(bool snapshots)
        {
            var (repo, a, b) = Create(snapshots);
            var cancellationToken = TestContext.Current.CancellationToken;

            Assert.Equal([3, 2, 1], repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).Select(x => x.Value).ToList());
            Assert.Equal([1, 2, 3], repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).OrderBy(x => x.Value).Select(x => x.Value).ToList());
            Assert.Equal(3, repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).Max(x => x.Value));
            Assert.Equal(25, await repo.TemporalQuery<HistoryModel>(numberFrom: 1).Where(x => x.ID == a || x.ID == b).SumAsync(x => x.Value, cancellationToken));
            Assert.Equal(5, repo.TemporalQuery<HistoryModel>().Count(x => x.ID == a || x.ID == b));
            Assert.Equal(2, repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == b).Select(x => x.Name).Distinct().Count());
            var groups = repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == b).GroupBy(x => x.Name).Select(g => new { Name = g.Key, States = g.Count() }).OrderBy(x => x.Name).ToList();
            Assert.Equal(["a:3", "b:2"], groups.Select(x => $"{x.Name}:{x.States}"));

            //an event query filters, orders, and selects on the event too
            Assert.Equal([1UL], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a && x.Number == 1).Select(x => x.Number).ToList());
            Assert.Equal([0UL, 1UL, 2UL], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a).OrderBy(x => x.Number).Select(x => x.Number).ToList());
            Assert.Equal([20, 10], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == b).Select(x => x.Model.Value).ToList());
            Assert.Equal(2, repo.EventQuery<HistoryModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == a && !x.Deleted).Skip(1).First().Model.Value);
            Assert.Equal(0, repo.EventQuery<HistoryModel>().Count(x => x.Model.ID == a && x.EventName == "none"));
        }

        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task Streams(bool snapshots)
        {
            var (repo, a, _) = Create(snapshots);
            var cancellationToken = TestContext.Current.CancellationToken;

            Assert.Equal([3, 2, 1], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).AsEnumerable()));
            var streamed = new List<int>();
            await foreach (var eventModel in repo.EventQuery<HistoryModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == a).AsAsyncEnumerable(cancellationToken))
                streamed.Add(eventModel.Model.Value);
            Assert.Equal([1, 2, 3], streamed);
            Assert.Equal([0UL, 1UL, 2UL], repo.EventQuery<HistoryModel>(TemporalOrder.Oldest).Where(x => x.Model.ID == a).AsEnumerable().Select(x => x.Number));
            streamed.Clear();
            await foreach (var model in repo.TemporalQuery<HistoryModel>(TemporalOrder.Oldest).Where(x => x.ID == a).Select(x => x.Value).AsAsyncEnumerable(cancellationToken))
                streamed.Add(model);
            Assert.Equal([1, 2, 3], streamed);
        }

        //a rule's condition is on the model of each state or event
        [Fact]
        public async Task RulesApply()
        {
            var (repo, a, _) = Create(false, provider => new HiddenRules(provider) { StreamChunkBytes = 1 });
            var cancellationToken = TestContext.Current.CancellationToken;
            var hidden = new HistoryModel { ID = Guid.NewGuid(), Value = 1, Name = "hidden" };
            repo.Create(hidden);

            Assert.Equal(3, repo.TemporalQuery<HistoryModel>().Count(x => x.ID == a));
            Assert.Empty(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == hidden.ID).ToList());
            Assert.Empty(repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == hidden.ID).ToList());
            Assert.Equal(3, await repo.EventQuery<HistoryModel>().CountAsync(x => x.Model.ID == a, cancellationToken));
            var streamed = new List<ulong>();
            await foreach (var eventModel in repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a || x.Model.ID == hidden.ID).AsAsyncEnumerable(cancellationToken))
                streamed.Add(eventModel.Number);
            Assert.Equal([2UL, 1UL, 0UL], streamed);

            //states stream through the rules too, and the rule's condition applies to the first state
            Assert.Equal([3, 2, 1], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == hidden.ID).AsEnumerable()));
            var states = new List<int>();
            await foreach (var model in repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == hidden.ID).AsAsyncEnumerable(cancellationToken))
                states.Add(model.Value);
            Assert.Equal([3, 2, 1], states);
            Assert.Null(repo.TemporalQuery<HistoryModel>().FirstOrDefault(x => x.ID == hidden.ID));
            Assert.Null(await repo.EventQuery<HistoryModel>().FirstOrDefaultAsync(x => x.Model.ID == hidden.ID, cancellationToken));
            Assert.Equal([2UL, 1UL, 0UL], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a || x.Model.ID == hidden.ID).AsEnumerable().Select(x => x.Number));

            //with the default chunk, what's left is given when the stream ends
            var (whole, b, _) = Create(false, provider => new HiddenRules(provider));
            Assert.Equal([2UL, 1UL, 0UL], whole.EventQuery<HistoryModel>().Where(x => x.Model.ID == b).AsEnumerable().Select(x => x.Number));
            var numbers = new List<ulong>();
            await foreach (var eventModel in whole.EventQuery<HistoryModel>().Where(x => x.Model.ID == b).AsAsyncEnumerable(cancellationToken))
                numbers.Add(eventModel.Number);
            Assert.Equal([2UL, 1UL, 0UL], numbers);
        }

        //a provider's own load of models by its parts reads their current state
        [Fact]
        public async Task ModelQueriesReadCurrentState()
        {
            var (repo, a, b) = Create(true);
            var cancellationToken = TestContext.Current.CancellationToken;
            var internalRepo = (IRepoInternal)repo;
            Expression<Func<HistoryModel, bool>> both = x => x.ID == a || x.ID == b;
            LinqModelQuery Query(LinqTerminal terminal) => new(typeof(HistoryModel), terminal, both, null, null, null, null);

            Assert.Equal([3, 20], Values((IReadOnlyCollection<HistoryModel>)internalRepo.Query(Query(LinqTerminal.Sequence))!));
            Assert.Equal(3, ((HistoryModel)internalRepo.Query(Query(LinqTerminal.FirstOrDefault))!).Value);
            _ = Assert.Throws<InvalidOperationException>(() => internalRepo.Query(Query(LinqTerminal.SingleOrDefault)));
            Assert.Equal(2L, internalRepo.Query(Query(LinqTerminal.LongCount)));
            Assert.Equal(true, internalRepo.Query(Query(LinqTerminal.Any)));

            Assert.Equal(new[] { 3, 20 }, Values((IReadOnlyCollection<HistoryModel>)(await internalRepo.QueryAsync(Query(LinqTerminal.Sequence), cancellationToken))!));
            Assert.Equal(3, ((HistoryModel)(await internalRepo.QueryAsync(Query(LinqTerminal.FirstOrDefault), cancellationToken))!).Value);
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => internalRepo.QueryAsync(Query(LinqTerminal.SingleOrDefault), cancellationToken));
            Assert.Equal(2L, await internalRepo.QueryAsync(Query(LinqTerminal.LongCount), cancellationToken));
            Assert.Equal(true, await internalRepo.QueryAsync(Query(LinqTerminal.Any), cancellationToken));
        }

        //a model related to one in an event store loads the related model's current state, not its history
        [Fact]
        public async Task RelatedModelInAnEventStore()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Holder>(engine));
            repo.AddProvider(new EventStoreAsTransactStoreProvider<Item>(new MemoryEngine()));
            var item = new Item { ID = Guid.NewGuid(), Value = 1 };
            repo.Create(item);
            item.Value = 2;
            repo.Update(item);
            item.Value = 3;
            repo.Update(item);
            var holder = new Holder { ID = Guid.NewGuid(), ItemID = item.ID };
            repo.Create(holder);

            var graph = new Graph<Holder>(true, x => x.Item);
            Assert.Equal(3, repo.Query<Holder>(graph).Single(x => x.ID == holder.ID).Item!.Value);
            Assert.Equal(3, (await repo.Query<Holder>(graph).SingleAsync(x => x.ID == holder.ID, cancellationToken)).Item!.Value);
            Assert.Equal([3], repo.Query<Holder>().Where(x => x.ID == holder.ID).Select(x => x.Item!.Value).ToList());
            Assert.Equal([3], await repo.Query<Holder>().Where(x => x.ID == holder.ID).Select(x => x.Item!.Value).ToListAsync(cancellationToken));
            //an event store finds models by identity only, so a condition on another member of the related model is refused
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Holder>().Count(x => x.Item!.Value == 3));
        }

        //models a rule drops after reading take their events with them
        [Fact]
        public async Task RulesDropEvents()
        {
            var (repo, a, _) = Create(false, provider => new DroppingRules(provider));
            var cancellationToken = TestContext.Current.CancellationToken;
            var hidden = new HistoryModel { ID = Guid.NewGuid(), Value = 1, Name = "hidden" };
            repo.Create(hidden);

            Assert.Equal([2UL, 1UL, 0UL], repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a || x.Model.ID == hidden.ID).ToList().Select(x => x.Number));
            Assert.Equal([2UL, 1UL, 0UL], (await repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a || x.Model.ID == hidden.ID).ToListAsync(cancellationToken)).Select(x => x.Number));
            Assert.Equal([3, 2, 1], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a || x.ID == hidden.ID).ToList()));
        }

        //a where with another query in it reads that query first, and an identity compared in other ways still pages as it's read
        [Theory]
        [MemberData(nameof(Snapshots))]
        public async Task OtherWhereForms(bool snapshots)
        {
            var (repo, a, b) = Create(snapshots);
            var cancellationToken = TestContext.Current.CancellationToken;

            var named = repo.Query<HistoryModel>().Where(y => y.ID == a).Select(y => y.ID);
            Assert.Equal([3, 2, 1], Values(repo.TemporalQuery<HistoryModel>().Where(x => named.Contains(x.ID)).ToList()));
            Assert.Equal(new[] { 3, 2, 1 }, Values(await repo.TemporalQuery<HistoryModel>().Where(x => named.Contains(x.ID)).ToListAsync(cancellationToken)));

            var list = new List<Guid> { a, b };
            Assert.Equal([3, 20], Values(repo.TemporalQuery<HistoryModel>().Where(x => list.Contains(x.ID)).Take(1).ToList()).Take(1).Concat(Values(repo.TemporalQuery<HistoryModel>().Where(x => list.Contains(x.ID)).Skip(3).Take(1).ToList())));
            Assert.Equal(Values(repo.TemporalQuery<HistoryModel>().Where(x => list.Contains(x.ID) && x.Value > 0).Take(4).ToList()), Values(repo.TemporalQuery<HistoryModel>().Where(x => list.Contains(x.ID)).Take(4).ToList()));
            Assert.Equal([3], Values(repo.TemporalQuery<HistoryModel>().Where(x => (Guid?)x.ID == (Guid?)a).Take(1).ToList()));
            Assert.Equal([3], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a && x.Name!.StartsWith("a")).Take(1).ToList()));
            Assert.Equal([2], Values(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a && x.Value != 3).Take(1).ToList()));

            //history can't be the other side of a set operation either
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<HistoryModel>().Where(x => x.ID == a).Union(repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a)).ToList());
        }

        [Fact]
        public async Task Refuses()
        {
            var (repo, a, _) = Create(false);
            var cancellationToken = TestContext.Current.CancellationToken;

            //an event store reads the streams its where names
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<HistoryModel>().Where(x => x.Value == 1).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<HistoryModel>().Where(x => x.Number == 1).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<HistoryModel>().ToList());
            //an identity or'd with a condition on the event doesn't say which streams
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<HistoryModel>().Where(x => x.Model.ID == a || x.EventName == "x").ToList());
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalQuery<HistoryModel>().Where(x => x.Value == 1).ToListAsync(cancellationToken));
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).SelectMany(x => new[] { x.Value }).ToList());
            //history is of current state queries only
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<HistoryModel>().Where(x => x.ID == a).Union(repo.Query<HistoryModel>().Where(x => x.ID == a)).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<HistoryModel>().Where(x => repo.TemporalQuery<HistoryModel>().Where(y => y.ID == a).Select(y => y.ID).Contains(x.ID)).ToList());

            //a model without a provider
            _ = Assert.Throws<Exception>(() => repo.TemporalQuery<Item>());
            _ = Assert.Throws<Exception>(() => repo.EventQuery<Item>());

            //a store that isn't an event store has no history
            var store = Repo.New();
            store.AddProvider(new TransactStoreProvider<HistoryModel>(new MemoryEngine()));
            _ = Assert.Throws<NotSupportedException>(() => store.TemporalQuery<HistoryModel>().Where(x => x.ID == a).ToList());
            _ = Assert.Throws<NotSupportedException>(() => store.EventQuery<HistoryModel>().Where(x => x.Model.ID == a).ToList());
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => store.TemporalQuery<HistoryModel>().Where(x => x.ID == a).ToListAsync(cancellationToken));
            _ = Assert.Throws<NotSupportedException>(() => store.TemporalQuery<HistoryModel>().Where(x => x.ID == a).AsEnumerable().ToList());
            _ = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await foreach (var _ in store.EventQuery<HistoryModel>().Where(x => x.Model.ID == a).AsAsyncEnumerable(cancellationToken)) { }
            });
        }
    }
}
