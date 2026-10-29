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
            repo.Create<Parent>(parents);
            repo.Create<Child>(children);
            return (repo, parents, children);
        }

        [Fact]
        public async Task ConditionThroughASingleRelation()
        {
            var (repo, parents, _) = Repos();

            Assert.Equal(2, repo.Many<Child>(x => x.Parent!.Name == "a").Count);
            Assert.Equal([2], repo.Many<Child>(x => x.Parent!.Rank > 1 && x.Qty > 1).Select(x => x.Qty));
            Assert.Equal(3, repo.Many<Child>(x => x.Parent!.Name == "b" || x.Qty != 2).Count);
            Assert.Equal(1, await repo.CountAsync<Child>(x => x.Parent!.Name == "b"));
            Assert.Equal(6, repo.Sum<Child>(x => x.Qty, x => x.Parent!.Rank < 2));

            //the relation itself still loads through its own provider
            var withParent = repo.Single<Child>(x => x.Qty == 5, new Graph<Child>(true, x => x.Parent));
            Assert.Equal(parents[0].Name, withParent!.Parent!.Name);
        }

        [Fact]
        public async Task ConditionThroughACollectionRelation()
        {
            var (repo, parents, _) = Repos();

            Assert.Equal(["a"], repo.Many<Parent>(x => x.Children!.Any(c => c.Tag == "y")).Select(x => x.Name));
            Assert.Equal(["c"], repo.Many<Parent>(x => !x.Children!.Any()).Select(x => x.Name));
            Assert.Equal(["b", "c"], repo.Many<Parent>(x => x.Children!.All(c => c.Qty < 3)).Select(x => x.Name).Order());

            Assert.Equal(["a"], repo.Many<Parent>(x => x.Children!.Count() > 1).Select(x => x.Name));
            //a parent with nothing related counts as zero, which the condition accepts
            Assert.Equal(["b", "c"], repo.Many<Parent>(x => x.Children!.Count() < 2).Select(x => x.Name).Order());
            Assert.Equal(["a", "b"], repo.Many<Parent>(x => x.Children!.Count(c => c.Tag == "x") == 1).Select(x => x.Name).Order());
            Assert.Equal(["a"], repo.Many<Parent>(x => x.Children!.Sum(c => c.Qty) >= 6).Select(x => x.Name));
            Assert.Equal(["b", "c"], repo.Many<Parent>(x => x.Children!.Sum(c => c.Qty) < 3).Select(x => x.Name).Order());
            Assert.Equal(["a"], repo.Many<Parent>(x => x.Children!.Max(c => c.Qty) == 5).Select(x => x.Name));
            Assert.Equal(["a"], repo.Many<Parent>(x => 1 == x.Children!.Min(c => c.Qty)).Select(x => x.Name));
            Assert.Equal(["a", "b"], repo.Many<Parent>(x => x.Children!.Average(c => c.Qty) >= 2).Select(x => x.Name).Order());
            Assert.Equal(["a", "b", "c"], (await repo.ManyAsync<Parent>(x => x.Rank > 1 || x.Children!.Any(c => c.Qty == 5))).Select(x => x.Name).Order());

            var withChildren = repo.Single<Parent>(x => x.ID == parents[0].ID, new Graph<Parent>(true, x => x.Children));
            Assert.Equal(2, withChildren!.Children!.Length);
        }

        [Fact]
        public async Task CalculationsProjectionsAndChangesUseTheSplitCondition()
        {
            var (repo, _, _) = Repos();

            Assert.Equal(3, repo.Sum<Parent>(x => x.Rank, x => x.Children!.Any()));
            var counts = repo.Count<Child, int>(x => x.Qty, x => x.Parent!.Name == "a");
            Assert.Equal(2, counts.Count);
            Assert.Equal([1, 5], repo.Select((Child x) => new ChildRow { Qty = x.Qty }, x => x.Parent!.Name == "a").Select(r => r.Qty).Order());

            Assert.Equal(1, await repo.UpdateAsync<Child>(x => x.Parent!.Name == "b", new Child { Tag = "z" }, new Graph<Child>(x => x.Tag)));
            Assert.Equal("z", repo.Single<Child>(x => x.Qty == 2)!.Tag);
            Assert.Equal(1, repo.Delete<Parent>(x => !x.Children!.Any()));
            Assert.Equal(2, repo.Count<Parent>());
        }

        [Fact]
        public void WhatCannotBeSplitIsRefused()
        {
            var (repo, _, _) = Repos();

            //a leaf mixing the related model with this one, a key, a value, a projected member, or an order through the relation
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<Child>(x => x.Qty > x.Parent!.Rank));
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<Parent>(x => x.Children!.Count() > x.Rank));
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<Parent>(x => x.Children!.Any(c => c.Qty > x.Rank)));
            _ = Assert.Throws<NotSupportedException>(() => repo.Count<Child, string?>(x => x.Parent!.Name));
            _ = Assert.Throws<NotSupportedException>(() => repo.Sum<Parent>(x => x.Children!.Sum(c => c.Qty)));
            _ = Assert.Throws<NotSupportedException>(() => repo.Select((Child x) => new ChildRow { ParentName = x.Parent!.Name }));
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<Child>(x => true, QueryOrder<Child>.Create(x => x.Parent!.Name)));
        }

        [Fact]
        public void SameEngineIsNotSplit()
        {
            var (repo, _, _) = Repos(sameEngine: true);

            Assert.Equal(2, repo.Many<Child>(x => x.Parent!.Name == "a").Count);
            Assert.Equal(["a"], repo.Many<Parent>(x => x.Children!.Count() > 1).Select(x => x.Name));
            Assert.Equal(["a"], repo.Select((Child x) => new ChildRow { Qty = x.Qty, ParentName = x.Parent!.Name }, x => x.Qty == 5).Select(r => r.ParentName));
            _ = Assert.Single(repo.Many<Child>(x => x.Qty > x.Parent!.Rank));
        }
    }
}
