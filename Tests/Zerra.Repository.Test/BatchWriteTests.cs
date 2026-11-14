// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    //many models created or updated together, in as few commands as the store takes and in one transaction when there isn't one
    public static class BatchWriteTests
    {
        [Entity("BatchItem")]
        public sealed class ItemModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            public decimal? Amount { get; set; }
        }

        [Entity("BatchGenerated")]
        public sealed class GeneratedModel
        {
            [Identity(true)]
            public int ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
        }

        [Entity("BatchParent")]
        public sealed class ParentModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public int? GeneratedID { get; set; }
            [Relation(nameof(GeneratedID))]
            public GeneratedModel? Generated { get; set; }
            [Relation(nameof(ChildModel.ParentID))]
            public ChildModel[]? Children { get; set; }
        }

        [Entity("BatchChild")]
        public sealed class ChildModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
            public int Quantity { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ItemModel), typeof(GeneratedModel), typeof(ParentModel), typeof(ChildModel)];

        //more than one command takes on every store
        private const int count = 2500;

        private static IRepo NewRepo(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ItemModel>(engine));
            repo.AddProvider(new TransactStoreProvider<GeneratedModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParentModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ChildModel>(engine));
            return repo;
        }
        private static void Clear(IRepo repo)
        {
            _ = repo.Delete<ChildModel>(x => x.Quantity >= 0);
            _ = repo.Delete<ParentModel>(x => x.Number >= 0);
            _ = repo.Delete<GeneratedModel>(x => x.Number >= 0);
            _ = repo.Delete<ItemModel>(x => x.Number >= 0);
        }
        //every other model leaves its nullable members null, so the rows write different columns
        private static ItemModel[] NewItems() => Enumerable.Range(0, count).Select(i => new ItemModel
        {
            ID = Guid.NewGuid(),
            Number = i,
            Name = i % 2 == 0 ? "Item" + i : null,
            Amount = i % 3 == 0 ? null : i * 1.25m,
        }).ToArray();
        private static void AssertItems(IRepo repo, ItemModel[] items)
        {
            var stored = repo.Query<ItemModel>().ToArray().ToDictionary(x => x.ID);
            Assert.Equal(items.Length, stored.Count);
            foreach (var item in items)
            {
                var row = stored[item.ID];
                Assert.Equal(item.Number, row.Number);
                Assert.Equal(item.Name, row.Name);
                Assert.Equal(item.Amount, row.Amount);
            }
        }
        private static void AssertGenerated(IRepo repo, GeneratedModel[] generated)
        {
            Assert.Equal(generated.Length, generated.Select(x => x.ID).Distinct().Count());
            var stored = repo.Query<GeneratedModel>().ToArray().ToDictionary(x => x.ID);
            Assert.Equal(generated.Length, stored.Count);
            //each model has the identity of its own row
            foreach (var model in generated)
                Assert.Equal(model.Number, stored[model.ID].Number);
        }
        private static ParentModel[] NewParents() => Enumerable.Range(0, 5).Select(i => new ParentModel
        {
            ID = Guid.NewGuid(),
            Number = i,
            Generated = new GeneratedModel() { Number = 100 + i },
            Children = Enumerable.Range(0, 3).Select(c => new ChildModel() { ID = Guid.NewGuid(), Quantity = i * 10 + c }).ToArray(),
        }).ToArray();
        private static void AssertParents(IRepo repo, ParentModel[] parents)
        {
            var graph = new Graph<ParentModel>(true, x => x.Generated, x => x.Children);
            var stored = repo.Query<ParentModel>(graph).ToArray().ToDictionary(x => x.ID);
            foreach (var parent in parents)
            {
                var row = stored[parent.ID];
                Assert.Equal(parent.Generated!.ID, row.GeneratedID);
                Assert.Equal(parent.Generated.Number, row.Generated!.Number);
                Assert.Equal(parent.Children!.Select(x => x.Quantity).Order(), row.Children!.Select(x => x.Quantity).Order());
            }
        }

        public static void TestSequence(ITransactStoreEngine engine, bool rejectsDuplicates)
        {
            var repo = NewRepo(engine);
            Clear(repo);

            var items = NewItems();
            repo.CreateMany<ItemModel>(items);
            AssertItems(repo, items);

            foreach (var item in items)
            {
                item.Number += count;
                item.Name = item.Name is null ? "Named" + item.Number : null;
            }
            repo.UpdateMany<ItemModel>(items);
            AssertItems(repo, items);

            var generated = Enumerable.Range(0, count).Select(i => new GeneratedModel() { Number = i, Name = i % 2 == 0 ? null : "Generated" + i }).ToArray();
            repo.CreateMany<GeneratedModel>(generated);
            AssertGenerated(repo, generated);

            //a model that isn't stored fails the update, and the others aren't changed
            var changed = items.Take(10).Select(x => new ItemModel() { ID = x.ID, Number = -1, Name = x.Name, Amount = x.Amount }).Append(new ItemModel() { ID = Guid.NewGuid(), Number = 1 }).ToArray();
            _ = Assert.ThrowsAny<Exception>(() => repo.UpdateMany<ItemModel>(changed));
            AssertItems(repo, items);

            //a model that is already stored fails the create, and the others aren't created
            if (rejectsDuplicates)
            {
                var duplicate = new[] { new ItemModel() { ID = Guid.NewGuid(), Number = 1 }, new ItemModel() { ID = items[0].ID, Number = 2 } };
                _ = Assert.ThrowsAny<Exception>(() => repo.CreateMany<ItemModel>(duplicate));
                Assert.Null(repo.Query<ItemModel>().Where(x => x.ID == duplicate[0].ID).FirstOrDefault());
            }

            //in a transaction the models are part of it
            using (repo.BeginTransaction())
            {
                repo.CreateMany<GeneratedModel>([new GeneratedModel() { Number = count + 1 }, new GeneratedModel() { Number = count + 2 }]);
            }
            Assert.Equal(0, repo.Query<GeneratedModel>().Count(x => x.Number > count));

            var parents = NewParents();
            repo.CreateMany<ParentModel>(parents, new Graph<ParentModel>(true, x => x.Generated, x => x.Children));
            AssertParents(repo, parents);

            Clear(repo);
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine, bool rejectsDuplicates)
        {
            var repo = NewRepo(engine);
            Clear(repo);

            var items = NewItems();
            await repo.CreateManyAsync<ItemModel>(items);
            AssertItems(repo, items);

            foreach (var item in items)
            {
                item.Number += count;
                item.Name = item.Name is null ? "Named" + item.Number : null;
            }
            await repo.UpdateManyAsync<ItemModel>(items);
            AssertItems(repo, items);

            var generated = Enumerable.Range(0, count).Select(i => new GeneratedModel() { Number = i, Name = i % 2 == 0 ? null : "Generated" + i }).ToArray();
            await repo.CreateManyAsync<GeneratedModel>(generated);
            AssertGenerated(repo, generated);

            var changed = items.Take(10).Select(x => new ItemModel() { ID = x.ID, Number = -1, Name = x.Name, Amount = x.Amount }).Append(new ItemModel() { ID = Guid.NewGuid(), Number = 1 }).ToArray();
            _ = await Assert.ThrowsAnyAsync<Exception>(() => repo.UpdateManyAsync<ItemModel>(changed));
            AssertItems(repo, items);

            if (rejectsDuplicates)
            {
                var duplicate = new[] { new ItemModel() { ID = Guid.NewGuid(), Number = 1 }, new ItemModel() { ID = items[0].ID, Number = 2 } };
                _ = await Assert.ThrowsAnyAsync<Exception>(() => repo.CreateManyAsync<ItemModel>(duplicate));
                Assert.Null(repo.Query<ItemModel>().Where(x => x.ID == duplicate[0].ID).FirstOrDefault());
            }

            using (repo.BeginTransaction())
            {
                await repo.CreateManyAsync<GeneratedModel>([new GeneratedModel() { Number = count + 1 }, new GeneratedModel() { Number = count + 2 }]);
            }
            Assert.Equal(0, repo.Query<GeneratedModel>().Count(x => x.Number > count));

            var parents = NewParents();
            await repo.CreateManyAsync<ParentModel>(parents, new Graph<ParentModel>(true, x => x.Generated, x => x.Children));
            AssertParents(repo, parents);

            Clear(repo);
        }
    }
}
