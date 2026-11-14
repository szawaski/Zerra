// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //related models on different engines: a condition through the relation runs on the related engine first, then its matches filter this one
    public class CrossEngineTests
    {
        [Entity("CrossParent")]
        public sealed class Parent
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            public int Rank { get; set; }
            [Relation(nameof(Child.ParentID))]
            public Child[]? Children { get; set; }
        }

        [Entity("CrossChild")]
        public sealed class Child
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
            public int Qty { get; set; }
            [StoreProperties(false, 50)]
            public string? Tag { get; set; }
            [Relation(nameof(ParentID))]
            public Parent? Parent { get; set; }
        }

        public sealed class ChildRow
        {
            public int Qty { get; set; }
            public string? ParentName { get; set; }
        }
        public sealed class ParentChildren
        {
            public string? Name { get; set; }
            public List<ChildRow>? Children { get; set; }
        }
        public sealed class ParentSummary
        {
            public string? ParentName { get; set; }
            public int Children { get; set; }
            public int Qty { get; set; }
        }

        //what crosses engines is read through its provider first
        //a query on another engine is read first, for values and for changes too, and the persist keeps its own condition to run again
        [Fact]
        public void SubqueryInValuesAndChangesOnOtherEngine()
        {
            var (repo, parents, children) = Repos();
            var tagged = repo.Query<Child>().Where(c => c.Tag == "x").Select(c => c.ParentID);
            var rows = repo.Query<Parent>().OrderBy(x => x.Rank).Select(x => new { x.Name, Has = tagged.Contains(x.ID) }).ToArray();
            Assert.Equal([true, true, false], rows.Select(r => r.Has));

            Assert.Equal(2, repo.Update<Parent>(x => tagged.Contains(x.ID), new Parent { Rank = 9 }, new Graph<Parent>(x => x.Rank)));
            Assert.Equal(3, repo.Query<Parent>().Single(x => x.ID == parents[2].ID).Rank);
            Assert.Equal(2, repo.Query<Parent>().Count(x => x.Rank == 9));

            var taggedChildren = repo.Query<Parent>().Where(x => x.Name == "a").Select(x => x.ID);
            Assert.Equal(children.Count(c => c.ParentID == parents[0].ID), repo.Delete<Child>(x => taggedChildren.Contains(x.ParentID)));
            Assert.Equal(1, repo.Query<Child>().Count());
        }

        [Fact]
        public async Task SubqueryInChangesOnOtherEngineAsync()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var (repo, parents, children) = Repos();
            var tagged = repo.Query<Child>().Where(c => c.Tag == "x").Select(c => c.ParentID);
            Assert.Equal(2, await repo.UpdateAsync<Parent>(x => tagged.Contains(x.ID), new Parent { Rank = 9 }, new Graph<Parent>(x => x.Rank), cancellationToken));
            Assert.Equal(2, await repo.Query<Parent>().CountAsync(x => x.Rank == 9, cancellationToken));
            Assert.Equal(2, await repo.UpdateAsync<Parent>(x => tagged.Contains(x.ID), x => new Parent { Rank = x.Rank + 1 }, cancellationToken));
            Assert.Equal(2, await repo.Query<Parent>().CountAsync(x => x.Rank == 10, cancellationToken));

            var taggedChildren = repo.Query<Parent>().Where(x => x.Name == "a").Select(x => x.ID);
            Assert.Equal(children.Count(c => c.ParentID == parents[0].ID), await repo.DeleteAsync<Child>(x => taggedChildren.Contains(x.ParentID), cancellationToken));
            Assert.Equal(1, await repo.Query<Child>().CountAsync(cancellationToken));
        }

        [Fact]
        public async Task SubqueryAndSelectManyOnOtherEngine()
        {
            foreach (var sameEngine in new[] { false, true })
            {
                var (repo, parents, children) = Repos(sameEngine);
                var tagged = repo.Query<Child>().Where(c => c.Tag == "x").Select(c => c.ParentID);
                Assert.Equal(["a", "b"], repo.Query<Parent>().Where(x => tagged.Contains(x.ID)).OrderBy(x => x.Rank).ToArray().Select(x => x.Name));
                Assert.Equal(["c"], (await repo.Query<Parent>().Where(x => !tagged.Contains(x.ID)).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(x => x.Name));

                Assert.Equal(children.Length, repo.Query<Parent>().SelectMany(x => x.Children!).Count());
                Assert.Equal(children.Where(c => c.Qty > 1).Select(c => c.Qty).Order(), repo.Query<Parent>().SelectMany(x => x.Children!).Where(c => c.Qty > 1).Select(c => c.Qty).ToArray().Order());
                Assert.Equal(children.Count(c => c.ParentID == parents[0].ID), await repo.Query<Parent>().Where(x => x.Rank == 1).SelectMany(x => x.Children!).CountAsync(cancellationToken: TestContext.Current.CancellationToken));
                var pairs = repo.Query<Parent>().OrderBy(x => x.Rank).Take(1).SelectMany(x => x.Children!, (x, c) => new { x.Name, c.Qty }).OrderBy(r => r.Qty).ToArray();
                Assert.Equal([1, 5], pairs.Select(r => r.Qty));
                Assert.All(pairs, r => Assert.Equal("a", r.Name));
            }
        }

        [Fact]
        public void RelationRowsOnOtherEngine()
        {
            foreach (var sameEngine in new[] { false, true })
            {
                var (repo, parents, children) = Repos(sameEngine);
                var paged = repo.Query<Parent>().OrderBy(x => x.Rank).Select(x => new { x.Name, Rows = x.Children!.OrderByDescending(c => c.Qty).Take(1).Select(c => c.Qty).ToArray() }).ToArray();
                Assert.Equal([5], paged[0].Rows);
                Assert.Equal([2], paged[1].Rows);
                Assert.Empty(paged[2].Rows);

                Assert.Equal(children.Select(c => c.Qty).Order(), repo.Query<Parent>().SelectMany(x => x.Children!.Select(c => c.Qty)).ToArray().Order());
                Assert.Equal(parents[^1].Rank, repo.Query<Parent>().OrderBy(x => x.Rank).Last().Rank);
                Assert.Equal(2, repo.Query<Parent>().OrderBy(x => x.Rank).Take(2).Count());
                Assert.Equal(parents[1].Rank, repo.Query<Parent>().OrderBy(x => x.Children!.Count()).ThenBy(x => x.Rank).ElementAt(1).Rank);
            }
        }

        [Fact]
        public async Task GraphRelationOnOtherEngine()
        {
            foreach (var sameEngine in new[] { false, true })
            {
                var (repo, parents, children) = Repos(sameEngine);
                var graph = new Graph<Parent>(true, x => x.Children);

                var ordered = repo.Query<Parent>(graph).Where(x => x.Rank < 3).OrderByDescending(x => x.Rank).ToArray();
                Assert.Equal(["b", "a"], ordered.Select(x => x.Name));
                Assert.Equal(children.Where(c => c.ParentID == ordered[1].ID).Select(c => c.Qty).Order(), ordered[1].Children!.Select(c => c.Qty).Order());
                Assert.Equal(children.Where(c => c.ParentID == ordered[0].ID).Select(c => c.Tag), ordered[0].Children!.Select(c => c.Tag));

                var none = (await repo.Query<Parent>(graph).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Single(x => x.Rank == 3);
                Assert.Empty(none.Children!);

                //only the child members the graph names, though the condition reads another
                var named = repo.Query<Parent>(new Graph<Parent>(x => x.Name, x => x.Children!.Select(c => c.Qty))).Where(x => x.Children!.Any(c => c.Tag == "y")).ToArray();
                Assert.Equal("a", Assert.Single(named).Name);
                Assert.Equal([1, 5], named[0].Children!.Select(c => c.Qty).Order());
                Assert.All(named[0].Children!, c => Assert.Null(c.Tag));
                Assert.Equal(0, named[0].Rank);

                var childGraph = new Graph<Child>(true, x => x.Parent);
                var withParent = await repo.Query<Child>(childGraph).Where(x => x.Qty == 5).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal("a", withParent!.Parent!.Name);
                _ = parents;
            }
        }

        //the queries of a set on different engines each run through their own providers, and their elements combine in memory by their values as rows would
        [Fact]
        public async Task SetsOnOtherEngine()
        {
            var (repo, parents, children) = Repos();
            var ranks = repo.Query<Parent>().Select(x => x.Rank);
            var quantities = repo.Query<Child>().Select(c => c.Qty);
            Assert.Equal(parents.Select(x => x.Rank).Union(children.Select(c => c.Qty)).Order(), ranks.Union(quantities).ToArray().Order());
            Assert.Equal(parents.Select(x => x.Rank).Intersect(children.Select(c => c.Qty)).Order(), ranks.Intersect(quantities).ToArray().Order());
            Assert.Equal(parents.Select(x => x.Rank).Except(children.Select(c => c.Qty)).Order(), (await ranks.Except(quantities).ToArrayAsync(TestContext.Current.CancellationToken)).Order());
            Assert.Equal(parents.Length + children.Length, await ranks.Concat(quantities).CountAsync(TestContext.Current.CancellationToken));
            Assert.True(ranks.Intersect(quantities).Any());
            Assert.Equal(parents.Select(x => x.Rank).Union(children.Select(c => c.Qty)).OrderByDescending(x => x).Take(2), ranks.Union(quantities).OrderByDescending(x => x).Take(2).ToArray());
            Assert.Equal(parents.Select(x => x.Rank).Concat(children.Select(c => c.Qty)).Count(x => x > 1), ranks.Concat(quantities).Count(x => x > 1));
            Assert.Equal(parents.Select(x => x.Rank).Union(children.Select(c => c.Qty)).Order().Select(x => x * 10), ranks.Union(quantities).OrderBy(x => x).Select(x => x * 10).ToArray());
            Assert.Equal(parents.Select(x => x.Rank).Union(children.Select(c => c.Qty)).Select(x => x * 10).Where(x => x > 10).OrderByDescending(x => x), ranks.Union(quantities).Select(x => x * 10).Where(x => x > 10).OrderByDescending(x => x).ToArray());

            //objects compare by their members, and a part reads a relation on another engine through its provider
            var named = repo.Query<Parent>().Select(x => new ChildRow { Qty = x.Rank, ParentName = x.Name });
            var tagged = repo.Query<Child>().Select(c => new ChildRow { Qty = c.Qty, ParentName = c.Parent!.Name });
            Assert.Equal(4, named.Union(tagged).Count());
            Assert.Equal(2, named.Intersect(tagged).Count());
        }

        //the queries of a join on different engines each run through their own providers, and their elements pair in memory by their keys
        [Fact]
        public async Task JoinsOnOtherEngine()
        {
            foreach (var sameEngine in new[] { false, true })
            {
                var (repo, parents, children) = Repos(sameEngine);
                var parentQuery = repo.Query<Parent>();
                var childQuery = repo.Query<Child>();
                Assert.Equal(parents.Join(children, x => x.ID, c => c.ParentID, (x, c) => $"{x.Name}:{c.Qty}").Order(), parentQuery.Join(childQuery, x => x.ID, c => c.ParentID, (x, c) => new { x.Name, c.Qty }).ToArray().Select(r => $"{r.Name}:{r.Qty}").Order());
                Assert.Equal(parents.Join(children, x => x.Rank, c => c.Qty, (x, c) => $"{x.Name}:{c.Tag}").Order(), (await parentQuery.Join(childQuery, x => x.Rank, c => c.Qty, (x, c) => new { x, c }).ToArrayAsync(TestContext.Current.CancellationToken)).Select(r => $"{r.x.Name}:{r.c.Tag}").Order());
                Assert.Equal(parents.LeftJoin(children, x => x.ID, c => c.ParentID, (x, c) => $"{x.Name}:{c?.Qty}").Order(), parentQuery.LeftJoin(childQuery, x => x.ID, c => c.ParentID, (x, c) => new { x.Name, c }).ToArray().Select(r => $"{r.Name}:{r.c?.Qty}").Order());
                Assert.Equal(parents.GroupJoin(children, x => x.ID, c => c.ParentID, (x, g) => $"{x.Name}:{g.Count()}:{g.Sum(c => c.Qty)}").Order(), parentQuery.GroupJoin(childQuery, x => x.ID, c => c.ParentID, (x, g) => new { x.Name, Count = g.Count(), Total = g.Sum(c => c.Qty) }).ToArray().Select(r => $"{r.Name}:{r.Count}:{r.Total}").Order());
                Assert.Equal(parents.Join(children, x => x.ID, c => c.ParentID, (x, c) => c.Qty).Where(q => q > 1).Order(), parentQuery.Join(childQuery, x => x.ID, c => c.ParentID, (x, c) => c.Qty).Where(q => q > 1).OrderBy(q => q).ToArray());
                Assert.Equal(children.Count(c => parents.Any(x => x.ID == c.ParentID)), await parentQuery.Join(childQuery, x => x.ID, c => c.ParentID, (x, c) => c).CountAsync(TestContext.Current.CancellationToken));

                //operations by position read each query in its order
                Assert.Equal(parents.OrderBy(x => x.Rank).Zip(children.OrderBy(c => c.Qty), (x, c) => $"{x.Name}:{c.Qty}"), parentQuery.OrderBy(x => x.Rank).Zip(childQuery.OrderBy(c => c.Qty), (x, c) => new { x.Name, c.Qty }).ToArray().Select(r => $"{r.Name}:{r.Qty}"));
                Assert.True(parentQuery.OrderBy(x => x.Rank).Take(2).Select(x => x.Rank).SequenceEqual(childQuery.OrderBy(c => c.Qty).Take(2).Select(c => c.Qty)));
                Assert.False(parentQuery.OrderBy(x => x.Rank).Select(x => x.Rank).SequenceEqual(childQuery.OrderBy(c => c.Qty).Select(c => c.Qty)));
                Assert.Equal(parents.OrderBy(x => x.Rank).IntersectBy(children.Select(c => c.ParentID), x => x.ID).Select(x => x.Name), parentQuery.OrderBy(x => x.Rank).IntersectBy(childQuery.Select(c => c.ParentID), x => x.ID).ToArray().Select(x => x.Name));
                Assert.Equal(parents.OrderBy(x => x.Rank).ExceptBy(children.Select(c => c.ParentID), x => x.ID).Select(x => x.Name), parentQuery.OrderBy(x => x.Rank).ExceptBy(childQuery.Select(c => c.ParentID), x => x.ID).ToArray().Select(x => x.Name));
                Assert.Equal(parents.OrderBy(x => x.Rank).Select(x => x.Rank).UnionBy(children.OrderByDescending(c => c.Qty).Select(c => c.Qty), n => n % 2), parentQuery.OrderBy(x => x.Rank).Select(x => x.Rank).UnionBy(childQuery.OrderByDescending(c => c.Qty).Select(c => c.Qty), n => n % 2).ToArray());
                Assert.Equal(parents.OrderByDescending(x => x.Rank).Select(x => x.Name).Index().Select(t => $"{t.Index}:{t.Item}"), parentQuery.OrderByDescending(x => x.Rank).Select(x => x.Name).Index().ToArray().Select(t => $"{t.Index}:{t.Item}"));
            }
        }

        //a count of the related rows on another engine compared either way round, and after a condition on the rows
        [Fact]
        public void CountsOnOtherEngine()
        {
            var (repo, parents, children) = Repos();
            int Count(Parent parent, Func<Child, bool>? where = null) => children.Count(c => c.ParentID == parent.ID && (where is null || where(c)));
            void Names(Func<Parent, bool> expected, IQueryable<Parent> actual) => Assert.Equal(parents.Where(expected).Select(x => x.Name).Order(), actual.Select(x => x.Name).ToArray().Order());

            Names(x => 1 < Count(x), repo.Query<Parent>().Where(x => 1 < x.Children!.Count()));
            Names(x => 2 >= Count(x), repo.Query<Parent>().Where(x => 2 >= x.Children!.Count()));
            Names(x => 1 > Count(x), repo.Query<Parent>().Where(x => 1 > x.Children!.Count()));
            Names(x => 1 <= Count(x), repo.Query<Parent>().Where(x => 1 <= x.Children!.Count()));
            Names(x => Count(x) != 2, repo.Query<Parent>().Where(x => x.Children!.Count() != 2));
            Names(x => Count(x) <= 1, repo.Query<Parent>().Where(x => x.Children!.Count() <= 1));
            Names(x => Count(x, c => c.Qty > 1) >= 1, repo.Query<Parent>().Where(x => x.Children!.Where(c => c.Qty > 1).Count() >= 1));
            Names(x => Count(x, c => c.Qty > 1 && c.Tag == "x") > 0, repo.Query<Parent>().Where(x => x.Children!.Where(c => c.Qty > 1).Where(c => c.Tag == "x").Count() > 0));
        }

        private static (IRepo Repo, Parent[] Parents, Child[] Children) Repos(bool sameEngine = false)
        {
            var parentEngine = new MemoryEngine();
            var childEngine = sameEngine ? parentEngine : new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Parent>(parentEngine));
            repo.AddProvider(new TransactStoreProvider<Child>(childEngine));

            var parents = new[]
            {
                new Parent { ID = Guid.NewGuid(), Name = "a", Rank = 1 },
                new Parent { ID = Guid.NewGuid(), Name = "b", Rank = 2 },
                new Parent { ID = Guid.NewGuid(), Name = "c", Rank = 3 },
            };
            var children = new[]
            {
                new Child { ID = Guid.NewGuid(), ParentID = parents[0].ID, Qty = 1, Tag = "x" },
                new Child { ID = Guid.NewGuid(), ParentID = parents[0].ID, Qty = 5, Tag = "y" },
                new Child { ID = Guid.NewGuid(), ParentID = parents[1].ID, Qty = 2, Tag = "x" },
            };
            repo.CreateMany<Parent>(parents);
            repo.CreateMany<Child>(children);
            return (repo, parents, children);
        }

        [Fact]
        public async Task ConditionThroughASingleRelation()
        {
            var (repo, parents, _) = Repos();

            Assert.Equal(2, repo.Query<Child>().Where(x => x.Parent!.Name == "a").ToArray().Length);
            Assert.Equal([2], repo.Query<Child>().Where(x => x.Parent!.Rank > 1 && x.Qty > 1).ToArray().Select(x => x.Qty));
            Assert.Equal(3, repo.Query<Child>().Where(x => x.Parent!.Name == "b" || x.Qty != 2).ToArray().Length);
            Assert.Equal(1, await repo.Query<Child>().CountAsync(x => x.Parent!.Name == "b", cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(6, repo.Query<Child>().Where(x => x.Parent!.Rank < 2).Sum(x => x.Qty));

            //the relation itself still loads through its own provider
            var withParent = repo.Query<Child>(new Graph<Child>(x => x.Qty, x => x.Parent)).Where(x => x.Qty == 5).SingleOrDefault();
            Assert.Equal(parents[0].Name, withParent!.Parent!.Name);
        }

        [Fact]
        public async Task ConditionThroughACollectionRelation()
        {
            var (repo, parents, _) = Repos();

            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Any(c => c.Tag == "y")).ToArray().Select(x => x.Name));
            Assert.Equal(["c"], repo.Query<Parent>().Where(x => !x.Children!.Any()).ToArray().Select(x => x.Name));
            Assert.Equal(["b", "c"], repo.Query<Parent>().Where(x => x.Children!.All(c => c.Qty < 3)).ToArray().Select(x => x.Name).Order());

            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Count() > 1).ToArray().Select(x => x.Name));
            //a parent with nothing related counts as zero, which the condition accepts
            Assert.Equal(["b", "c"], repo.Query<Parent>().Where(x => x.Children!.Count() < 2).ToArray().Select(x => x.Name).Order());
            Assert.Equal(["a", "b"], repo.Query<Parent>().Where(x => x.Children!.Count(c => c.Tag == "x") == 1).ToArray().Select(x => x.Name).Order());
            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Sum(c => c.Qty) >= 6).ToArray().Select(x => x.Name));
            Assert.Equal(["b", "c"], repo.Query<Parent>().Where(x => x.Children!.Sum(c => c.Qty) < 3).ToArray().Select(x => x.Name).Order());
            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Max(c => c.Qty) == 5).ToArray().Select(x => x.Name));
            Assert.Equal(["a"], repo.Query<Parent>().Where(x => 1 == x.Children!.Min(c => c.Qty)).ToArray().Select(x => x.Name));
            Assert.Equal(["a", "b"], repo.Query<Parent>().Where(x => x.Children!.Average(c => c.Qty) >= 2).ToArray().Select(x => x.Name).Order());
            Assert.Equal(["a", "b", "c"], (await repo.Query<Parent>().Where(x => x.Rank > 1 || x.Children!.Any(c => c.Qty == 5)).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(x => x.Name).Order());

            var withChildren = repo.Query<Parent>(new Graph<Parent>(x => x.ID, x => x.Children)).Where(x => x.ID == parents[0].ID).SingleOrDefault();
            Assert.Equal(2, withChildren!.Children!.Length);
        }

        [Fact]
        public async Task AggregatesProjectionsAndChangesUseTheSplitCondition()
        {
            var (repo, _, _) = Repos();

            Assert.Equal(3, repo.Query<Parent>().Where(x => x.Children!.Any()).Sum(x => x.Rank));
            var counts = repo.Query<Child>().Where(x => x.Parent!.Name == "a").GroupBy(x => x.Qty).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray();
            Assert.Equal(2, counts.Length);
            Assert.Equal([1, 5], repo.Query<Child>().Where(x => x.Parent!.Name == "a").Select(x => new ChildRow { Qty = x.Qty }).ToArray().Select(r => r.Qty).Order());

            Assert.Equal(1, await repo.UpdateAsync<Child>(x => x.Parent!.Name == "b", new Child { Tag = "z" }, new Graph<Child>(x => x.Tag), TestContext.Current.CancellationToken));
            Assert.Equal("z", repo.Query<Child>().SingleOrDefault(x => x.Qty == 2)!.Tag);
            Assert.Equal(1, repo.Delete<Parent>(x => !x.Children!.Any()));
            Assert.Equal(2, repo.Query<Parent>().Count());
        }

        [Fact]
        public async Task KeysValuesMembersAndOrdersThroughARelationRunInMemory()
        {
            var (repo, _, _) = Repos();

            //the models load with what the key, value, projected member, or order reads, the relation through its own provider, and the query runs in memory
            Assert.Equal([("a", 2L), ("b", 1L)], repo.Query<Child>().GroupBy(x => x.Parent!.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray().OrderBy(x => x.Key).ToArray());
            Assert.Equal(8, repo.Query<Parent>().Sum(x => x.Children!.Sum(c => c.Qty)));
            Assert.Equal(6, await repo.Query<Parent>().Where(x => x.Rank == 1).SumAsync(x => x.Children!.Sum(c => c.Qty), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(5, repo.Query<Parent>().Where(x => x.Children!.Any()).GroupBy(x => x.Name).Select(g => new { g.Key, Value = g.Max(x => x.Children!.Max(c => c.Qty)) }).ToArray().Select(r => (r.Key, r.Value)).ToArray().Single(x => x.Key == "a").Value);

            //a projection keeps its order and paging in the store when only the members cross
            var rows = repo.Query<Child>().Where(x => x.Qty > 1).OrderByDescending(x => x.Qty).Skip(0).Take(1).Select(x => new ChildRow { Qty = x.Qty, ParentName = x.Parent!.Name }).ToArray();
            Assert.Equal([(5, "a")], rows.Select(r => (r.Qty, r.ParentName)).ToArray());
            var summaries = await repo.Query<Child>().GroupBy(x => x.Parent!.Name).Select(g => new ParentSummary { ParentName = g.Key, Children = g.Count(), Qty = g.Sum(c => c.Qty) }).Where(r => r.Qty > 1).OrderByDescending(r => r.Qty).Take(1).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal([("a", 2, 6)], summaries.Select(r => (r.ParentName, r.Children, r.Qty)).ToArray());

            //an order through the relation sorts and pages in memory
            Assert.Equal([2, 1, 5], repo.Query<Child>().OrderByDescending(x => x.Parent!.Name).ThenBy(x => x.Qty).ToArray().Select(x => x.Qty));
            Assert.Equal([1, 5], (await repo.Query<Child>().OrderByDescending(x => x.Parent!.Name).ThenBy(x => x.Qty).Skip(1).Take(2).Select(x => new ChildRow { Qty = x.Qty, ParentName = x.Parent!.Name }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(r => r.Qty));
            Assert.Equal(2, repo.Query<Child>().OrderByDescending(x => x.Parent!.Name).ThenBy(x => x.Qty).First().Qty);
            var withTag = repo.Query<Child>(new Graph<Child>(x => x.Tag)).OrderByDescending(x => x.Parent!.Name).ThenBy(x => x.Qty).ToArray();
            Assert.Equal(["x", "x", "y"], withTag.Select(x => x.Tag));

            //one row of the related collection, picked in memory with the members it reads loaded, and null for a parent without one
            var tops = repo.Query<Parent>().OrderBy(x => x.Rank).Select(x => new ParentSummary { ParentName = x.Name, Qty = x.Children!.OrderByDescending(c => c.Qty).First().Qty }).ToArray();
            Assert.Equal([("a", 5), ("b", 2), ("c", 0)], tops.Select(r => (r.ParentName, r.Qty)).ToArray());
            Assert.Equal(["c", "a", "b"], repo.Query<Parent>().OrderBy(x => x.Children!.OrderBy(c => c.Qty).First().Tag).ThenBy(x => x.Rank).ToArray().Select(x => x.Name));

            //results built from the related rows, loaded with the members they read
            var withChildren = repo.Query<Parent>().OrderBy(x => x.Rank).Select(x => new ParentChildren { Name = x.Name, Children = x.Children!.OrderByDescending(c => c.Qty).Select(c => new ChildRow { Qty = c.Qty, ParentName = c.Tag }).ToList() }).ToArray();
            Assert.Equal([("a", "5y,1x"), ("b", "2x"), ("c", "")], withChildren.Select(r => (r.Name, String.Join(",", r.Children!.Select(c => $"{c.Qty}{c.ParentName}")))).ToArray());
        }

        [Fact]
        public async Task QueryableSplitsConditionsAndRunsTheRestInMemory()
        {
            var (repo, _, _) = Repos();

            //a condition through the relation runs on the related engine first
            Assert.Equal(2, repo.Query<Child>().Count(x => x.Parent!.Name == "a"));
            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Count() > 1).Select(x => x.Name).ToArray());

            //a member or an order through the relation loads the models with what it reads and runs in memory
            var rows = await repo.Query<Child>().Where(x => x.Qty > 1).OrderByDescending(x => x.Qty).Select(x => new { x.Qty, ParentName = x.Parent!.Name }).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal([(5, "a"), (2, "b")], rows.Select(r => (r.Qty, r.ParentName)));
            var parents = repo.Query<Parent>().OrderBy(x => x.Rank).Select(x => new { x.Name, Children = x.Children!.OrderBy(c => c.Qty).Select(c => c.Qty).ToArray(), Most = x.Children!.OrderByDescending(c => c.Qty).Select(c => c.Tag).FirstOrDefault() }).ToArray();
            Assert.Equal([("a", "1,5", "y"), ("b", "2", "x"), ("c", "", null)], parents.Select(r => (r.Name, String.Join(",", r.Children), r.Most)));
            Assert.Equal([2, 1, 5], repo.Query<Child>().OrderByDescending(x => x.Parent!.Name).ThenBy(x => x.Qty).ToArray().Select(x => x.Qty));
        }

        [Fact]
        public void WhatCannotBeSplitIsRefused()
        {
            var (repo, _, _) = Repos();

            //a leaf mixing the related model with this one
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Child>().Where(x => x.Qty > x.Parent!.Rank).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Parent>().Where(x => x.Children!.Count() > x.Rank).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Parent>().Where(x => x.Children!.Any(c => c.Qty > x.Rank)).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<Parent>().Where(x => x.Children!.OrderBy(c => c.Qty).First().Tag == "x").ToArray());
        }

        [Fact]
        public void SameEngineIsNotSplit()
        {
            var (repo, _, _) = Repos(sameEngine: true);

            Assert.Equal(2, repo.Query<Child>().Where(x => x.Parent!.Name == "a").ToArray().Length);
            Assert.Equal(["a"], repo.Query<Parent>().Where(x => x.Children!.Count() > 1).ToArray().Select(x => x.Name));
            Assert.Equal(["a"], repo.Query<Child>().Where(x => x.Qty == 5).Select(x => new ChildRow { Qty = x.Qty, ParentName = x.Parent!.Name }).ToArray().Select(r => r.ParentName));
            _ = Assert.Single(repo.Query<Child>().Where(x => x.Qty > x.Parent!.Rank).ToArray());
        }
    }
}
