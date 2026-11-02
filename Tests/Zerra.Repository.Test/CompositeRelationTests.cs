// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //a relation to a model identified by two members, with a foreign key of two members
    public static class CompositeRelationTests
    {
        [Entity("CompositeOrder")]
        public sealed class OrderModel
        {
            [Identity]
            public int Region { get; set; }
            [Identity]
            public int Number { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [Relation(nameof(LineModel.Region), nameof(LineModel.OrderNumber))]
            public LineModel[]? Lines { get; set; }
        }

        [Entity("CompositeLine")]
        public sealed class LineModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Region { get; set; }
            public int OrderNumber { get; set; }
            public int Quantity { get; set; }
            [Relation(nameof(Region), nameof(OrderNumber))]
            public OrderModel? Order { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(OrderModel), typeof(LineModel)];

        private static IRepo NewRepo(ITransactStoreEngine engine, ITransactStoreEngine? lineEngine = null)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<OrderModel>(engine));
            repo.AddProvider(new TransactStoreProvider<LineModel>(lineEngine ?? engine));
            return repo;
        }

        private static void Clear(IRepo repo)
        {
            _ = repo.Delete<LineModel>(x => x.Quantity >= 0);
            _ = repo.Delete<OrderModel>(x => x.Region >= 0);
        }

        //each order's lines, with orders whose region is one's and number another's
        private static OrderModel[] NewOrders() =>
        [
            new OrderModel { Region = 1, Number = 1, Name = "a", Lines = [new LineModel { ID = Guid.NewGuid(), Quantity = 1 }, new LineModel { ID = Guid.NewGuid(), Quantity = 5 }] },
            new OrderModel { Region = 1, Number = 2, Name = "b", Lines = [new LineModel { ID = Guid.NewGuid(), Quantity = 2 }] },
            new OrderModel { Region = 2, Number = 1, Name = "c", Lines = [new LineModel { ID = Guid.NewGuid(), Quantity = 3 }] },
        ];

        public static void TestSequence(ITransactStoreEngine engine, bool enforcesKeys)
        {
            var repo = NewRepo(engine);
            Clear(repo);

            var orders = NewOrders();
            repo.Create<OrderModel>(orders, new Graph<OrderModel>(true, x => x.Lines));
            //a line whose region is one order's and number another's matches neither, and a store with the foreign key's constraint refuses it
            var stray = new LineModel { ID = Guid.NewGuid(), Region = 2, OrderNumber = 2, Quantity = 9 };
            if (enforcesKeys)
            {
                _ = Assert.ThrowsAny<Exception>(() => repo.Create(stray));
                repo.Create(new OrderModel { Region = 2, Number = 2, Name = "z" });
            }
            repo.Create(stray);

            //the foreign key was set from the order's identity
            var lines = repo.Query<LineModel>().ToArray();
            Assert.Equal(5, lines.Length);
            Assert.Equal(2, lines.Count(x => x.Region == 1 && x.OrderNumber == 1));

            //one related model read with its row, joined on both members
            var withOrders = repo.Query<LineModel>(new Graph<LineModel>(true, x => x.Order)).ToArray();
            Assert.Equal("a", withOrders.Single(x => x.Quantity == 5).Order!.Name);
            Assert.Equal("c", withOrders.Single(x => x.Quantity == 3).Order!.Name);
            Assert.Equal(enforcesKeys ? "z" : null, withOrders.Single(x => x.Quantity == 9).Order?.Name);

            //a collection read after the rows, matched on both members
            var withLines = repo.Query<OrderModel>(new Graph<OrderModel>(true, x => x.Lines)).Where(x => x.Name != "z").ToArray().OrderBy(x => x.Name).ToArray();
            Assert.Equal([1, 5], withLines[0].Lines!.Select(x => x.Quantity).Order());
            Assert.Equal([2], withLines[1].Lines!.Select(x => x.Quantity));
            Assert.Equal([3], withLines[2].Lines!.Select(x => x.Quantity));
            //orders whose regions and numbers between them include another's read only their own lines
            var others = repo.Query<OrderModel>(new Graph<OrderModel>(true, x => x.Lines)).Where(x => x.Name == "b" || x.Name == "c").ToArray().OrderBy(x => x.Name).ToArray();
            Assert.Equal([2], others[0].Lines!.Select(x => x.Quantity));
            Assert.Equal([3], others[1].Lines!.Select(x => x.Quantity));
            var page = repo.Query<OrderModel>(new Graph<OrderModel>(true, x => x.Lines)).OrderBy(x => x.Name).Skip(1).Take(1).ToArray();
            Assert.Equal("b", Assert.Single(page).Name);
            Assert.Equal([2], page[0].Lines!.Select(x => x.Quantity));

            //conditions through the relation each way
            Assert.Equal([1, 5], repo.Query<LineModel>().Where(x => x.Order!.Name == "a").Select(x => x.Quantity).ToArray().Order());
            Names(enforcesKeys ? ["a", "z"] : ["a"], repo.Query<OrderModel>().Where(x => x.Lines!.Any(l => l.Quantity > 4)));
            Names(enforcesKeys ? ["a", "b", "c", "z"] : ["a", "b", "c"], repo.Query<OrderModel>().Where(x => x.Lines!.Any()));
            Names(["b", "c"], repo.Query<OrderModel>().Where(x => x.Name != "z" && x.Lines!.All(l => l.Quantity < 4)));
            Names(["a"], repo.Query<OrderModel>().Where(x => x.Name != "z" && !x.Lines!.All(l => l.Quantity < 4)));
            Names(["a"], repo.Query<OrderModel>().Where(x => x.Lines!.Count() == 2));
            Names(["a", "c"], repo.Query<OrderModel>().Where(x => x.Name != "z" && x.Lines!.Sum(l => l.Quantity) > 2));

            //the lines of orders, matched by the order's identity of both members
            Assert.Equal([1, 3, 5], repo.Query<OrderModel>().Where(x => x.Name != "b" && x.Name != "z").SelectMany(x => x.Lines!).Select(l => l.Quantity).ToArray().Order());
            Assert.Equal([1, 5], repo.Query<OrderModel>().OrderBy(x => x.Name).Take(1).SelectMany(x => x.Lines!).Select(l => l.Quantity).ToArray().Order());

            //values of the related rows
            var projected = repo.Query<OrderModel>().Where(x => x.Name != "z").OrderBy(x => x.Name).Select(x => new { x.Name, Quantities = x.Lines!.Select(l => l.Quantity).ToList(), First = x.Lines!.OrderBy(l => l.Quantity).First().Quantity, Top = x.Lines!.OrderByDescending(l => l.Quantity).Take(1).Select(l => l.Quantity).ToList() }).ToArray();
            Assert.Equal([1, 5], projected[0].Quantities.Order());
            Assert.Equal([1, 2, 3], projected.Select(x => x.First));
            Assert.Equal([5], projected[0].Top);
            Assert.Equal(["1,5", "2", "3"], repo.Query<OrderModel>().Where(x => x.Name != "z").OrderBy(x => x.Name).Select(x => String.Join(",", x.Lines!.OrderBy(l => l.Quantity).Select(l => l.Quantity.ToString()))).ToArray());

            //a change of the lines adds, keeps, and removes them by the order's identity
            var first = withLines[0];
            first.Lines = [first.Lines!.Single(x => x.Quantity == 5), new LineModel { ID = Guid.NewGuid(), Quantity = 7 }];
            repo.Update(first, new Graph<OrderModel>(true, x => x.Lines));
            Assert.Equal([5, 7], repo.Query<LineModel>().Where(x => x.Region == 1 && x.OrderNumber == 1).Select(x => x.Quantity).ToArray().Order());
            Assert.Equal([2], repo.Query<LineModel>().Where(x => x.Region == 1 && x.OrderNumber == 2).Select(x => x.Quantity).ToArray());

            //deleting an order with its lines removes only its own
            repo.Delete(first, new Graph<OrderModel>(true, x => x.Lines));
            Assert.Equal(3, repo.Query<LineModel>().Count());
            Assert.True(repo.Query<LineModel>().Any(x => x.ID == stray.ID));

            //changes of orders by a condition through their lines, the orders matched by their identity of both members
            Assert.Equal(1, repo.Update<OrderModel>(x => x.Lines!.Any(l => l.Quantity == 3), new OrderModel { Name = "c3" }, new Graph<OrderModel>(x => x.Name)));
            Names(enforcesKeys ? ["b", "c3", "z"] : ["b", "c3"], repo.Query<OrderModel>());
            _ = repo.Delete<LineModel>(x => x.Quantity == 2);
            Assert.Equal(1, repo.Delete<OrderModel>(x => x.Name != "z" && !x.Lines!.Any()));
            Names(enforcesKeys ? ["c3", "z"] : ["c3"], repo.Query<OrderModel>());

            Clear(repo);
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var repo = NewRepo(engine);
            Clear(repo);

            var orders = NewOrders();
            await repo.CreateAsync<OrderModel>(orders, new Graph<OrderModel>(true, x => x.Lines));
            var withLines = (await repo.Query<OrderModel>(new Graph<OrderModel>(true, x => x.Lines)).ToArrayAsync()).OrderBy(x => x.Name).ToArray();
            Assert.Equal([1, 5], withLines[0].Lines!.Select(x => x.Quantity).Order());
            var withOrders = await repo.Query<LineModel>(new Graph<LineModel>(true, x => x.Order)).ToArrayAsync();
            Assert.Equal("a", withOrders.Single(x => x.Quantity == 5).Order!.Name);

            var first = withLines[0];
            first.Lines = [new LineModel { ID = Guid.NewGuid(), Quantity = 8 }];
            await repo.UpdateAsync(first, new Graph<OrderModel>(true, x => x.Lines));
            Assert.Equal(new[] { 8 }, (await repo.Query<LineModel>().Where(x => x.Region == 1 && x.OrderNumber == 1).Select(x => x.Quantity).ToArrayAsync()));
            await repo.DeleteAsync(first, new Graph<OrderModel>(true, x => x.Lines));
            Assert.Equal(2, await repo.Query<LineModel>().CountAsync());

            Clear(repo);
        }

        //orders and lines on different engines, so a condition through the relation runs on the other engine first and the keys it matched filter the query
        public static void TestAcrossEngines(ITransactStoreEngine orderEngine, ITransactStoreEngine lineEngine)
        {
            var repo = NewRepo(orderEngine, lineEngine);
            Clear(repo);

            var orders = NewOrders();
            repo.Create<OrderModel>(orders, new Graph<OrderModel>(true, x => x.Lines));
            Names(["a"], repo.Query<OrderModel>().Where(x => x.Lines!.Any(l => l.Quantity > 4)));
            Names(["b", "c"], repo.Query<OrderModel>().Where(x => x.Lines!.All(l => l.Quantity < 4)));
            Names(["a", "c"], repo.Query<OrderModel>().Where(x => x.Lines!.Sum(l => l.Quantity) > 2));
            Assert.Equal([1, 5], repo.Query<LineModel>().Where(x => x.Order!.Name == "a").Select(x => x.Quantity).ToArray().Order());
            Assert.Equal(1, repo.Update<OrderModel>(x => x.Lines!.Any(l => l.Quantity == 3), new OrderModel { Name = "c3" }, new Graph<OrderModel>(x => x.Name)));
            Names(["a", "b", "c3"], repo.Query<OrderModel>());

            Clear(repo);
        }

        private static void Names(string[] expected, IQueryable<OrderModel> query)
            => Assert.Equal(expected, query.Select(x => x.Name!).ToArray().Order());
    }

    public class CompositeRelationMemoryTests
    {
        [Fact]
        public async Task Memory()
        {
            CompositeRelationTests.TestSequence(new MemoryEngine(), false);
            await CompositeRelationTests.TestSequenceAsync(new MemoryEngine());
            CompositeRelationTests.TestAcrossEngines(new MemoryEngine(), new MemoryEngine());
        }
    }
}
