// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //a create, update, or delete with a graph naming relations persists the related models too
    public class RelationPersistTests
    {
        [Entity("RelationParent")]
        public sealed class Parent
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }
            public int? ChildID { get; set; }

            [Relation(nameof(ChildID))]
            public Child? Child { get; set; }

            [Relation(nameof(Item.ParentID))]
            public Item[]? Items { get; set; }
        }

        [Entity("RelationChild")]
        public sealed class Child
        {
            [Identity(true)]
            public int ID { get; set; }
            public string? Name { get; set; }
        }

        [Entity("RelationItem")]
        public sealed class Item
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
            public string? Name { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(Parent), typeof(Child), typeof(Item)];

        private static IRepo CreateRepo(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Parent>(engine));
            repo.AddProvider(new TransactStoreProvider<Child>(engine));
            repo.AddProvider(new TransactStoreProvider<Item>(engine));
            return repo;
        }

        private static readonly Graph<Parent> withRelations = new(true, x => x.Child, x => x.Items);

        private static Parent NewParent() => new()
        {
            ID = Guid.NewGuid(),
            Name = "parent",
            Child = new Child { Name = "child" },
            Items = [new Item { ID = Guid.NewGuid(), Name = "one" }, new Item { ID = Guid.NewGuid(), Name = "two" }],
        };

        private static void AssertStored(IRepo repo, Parent expected)
        {
            var stored = repo.Single<Parent>(x => x.ID == expected.ID, withRelations);
            Assert.NotNull(stored);
            Assert.Equal(expected.Name, stored.Name);
            Assert.Equal(expected.Child?.Name, stored.Child?.Name);
            Assert.Equal(expected.Child?.ID, stored.ChildID);
            Assert.Equal(expected.Items!.Select(x => (x.ID, x.Name)).OrderBy(x => x.ID), stored.Items!.Select(x => (x.ID, x.Name)).OrderBy(x => x.ID));
            Assert.All(stored.Items!, x => Assert.Equal(expected.ID, x.ParentID));
        }

        [Fact]
        public void Sync_PersistsRelations() => TestSequence(new MemoryEngine());

        [Fact]
        public Task Async_PersistsRelations() => TestSequenceAsync(new MemoryEngine());

        internal static void TestSequence(ITransactStoreEngine engine)
        {
            var repo = CreateRepo(engine);
            var parent = NewParent();

            repo.Create(parent, withRelations);
            Assert.NotEqual(0, parent.Child!.ID);
            Assert.Equal(parent.Child.ID, parent.ChildID);
            AssertStored(repo, parent);

            //the first item is changed, the second removed, and a third added
            parent.Name = "parent updated";
            parent.Child.Name = "child updated";
            parent.Items![0].Name = "one updated";
            var removed = parent.Items[1];
            parent.Items = [parent.Items[0], new Item { ID = Guid.NewGuid(), Name = "three" }];
            repo.Update(parent, withRelations);
            AssertStored(repo, parent);
            Assert.Null(repo.Single<Item>(x => x.ID == removed.ID));
            Assert.Equal(1, repo.Count<Child>(x => x.ID == parent.Child.ID));

            repo.Delete(parent, withRelations);
            Assert.Null(repo.Single<Parent>(x => x.ID == parent.ID));
            Assert.False(repo.Any<Item>(x => x.ParentID == parent.ID));
        }

        internal static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var repo = CreateRepo(engine);
            var parent = NewParent();

            await repo.CreateAsync(parent, withRelations);
            Assert.NotEqual(0, parent.Child!.ID);
            Assert.Equal(parent.Child.ID, parent.ChildID);
            AssertStored(repo, parent);

            parent.Name = "parent updated";
            parent.Child.Name = "child updated";
            parent.Items![0].Name = "one updated";
            var removed = parent.Items[1];
            parent.Items = [parent.Items[0], new Item { ID = Guid.NewGuid(), Name = "three" }];
            await repo.UpdateAsync(parent, withRelations);
            AssertStored(repo, parent);
            Assert.Null(await repo.SingleAsync<Item>(x => x.ID == removed.ID));
            Assert.Equal(1, await repo.CountAsync<Child>(x => x.ID == parent.Child.ID));

            await repo.DeleteAsync(parent, withRelations);
            Assert.Null(await repo.SingleAsync<Parent>(x => x.ID == parent.ID));
            Assert.False(await repo.AnyAsync<Item>(x => x.ParentID == parent.ID));
        }
    }
}
