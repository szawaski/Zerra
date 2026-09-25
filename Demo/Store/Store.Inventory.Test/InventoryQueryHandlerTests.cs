using Store.Inventory.Domain;
using Store.Inventory.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Inventory.Test
{
    public class InventoryQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new InventoryTestBus();

            Assert.Equal("Test data store", await test.Bus.Call<IInventoryQueryHandler>().GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Bus.Call<IInventoryQueryHandler>().GetMessagingName(Token));
        }

        [Fact]
        public async Task GetStockLevels_AvailableIsOnHandLessReserved()
        {
            var test = new InventoryTestBus();
            var productID = Guid.NewGuid();
            await test.Repo.CreateAsync(new StockItemDataModel() { ProductID = productID, OnHand = 10, Reserved = 3 });

            var levels = await test.Bus.Call<IInventoryQueryHandler>().GetStockLevels(Token);

            var level = Assert.Single(levels);
            Assert.Equal(productID, level.ProductID);
            Assert.Equal(10, level.OnHand);
            Assert.Equal(3, level.Reserved);
            Assert.Equal(7, level.Available);
        }

        [Fact]
        public async Task GetRecentMovements_NewestFirst()
        {
            var test = new InventoryTestBus();
            var productID = Guid.NewGuid();
            var now = DateTime.UtcNow;
            await test.Repo.CreateAsync<StockMovementDataModel>(
            [
                new() { ID = Guid.NewGuid(), ProductID = productID, Kind = nameof(StockMovementKind.Restocked), Quantity = 10, OccurredOn = now.AddSeconds(1) },
                new() { ID = Guid.NewGuid(), ProductID = productID, Kind = nameof(StockMovementKind.Shipped), Quantity = 1, OrderNumber = "SO-2", OccurredOn = now.AddSeconds(3) },
                new() { ID = Guid.NewGuid(), ProductID = productID, Kind = nameof(StockMovementKind.Reserved), Quantity = 1, OrderNumber = "SO-2", OccurredOn = now.AddSeconds(2) }
            ]);

            var movements = await test.Bus.Call<IInventoryQueryHandler>().GetRecentMovements(3, Token);

            Assert.Equal([nameof(StockMovementKind.Shipped), nameof(StockMovementKind.Reserved), nameof(StockMovementKind.Restocked)], movements.Select(x => x.Kind).ToArray());
            Assert.All(movements, x => Assert.Equal(productID, x.ProductID));
            Assert.Equal("SO-2", movements[0].OrderNumber);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(1000, 100)]
        public async Task GetRecentMovements_CountIsKeptBetweenOneAndOneHundred(int count, int expected)
        {
            var test = new InventoryTestBus();
            var now = DateTime.UtcNow;
            await test.Repo.CreateAsync(Enumerable.Range(0, 101).Select(i => new StockMovementDataModel()
            {
                ID = Guid.NewGuid(),
                ProductID = Guid.NewGuid(),
                Kind = nameof(StockMovementKind.Restocked),
                Quantity = 1,
                OccurredOn = now.AddSeconds(-i)
            }).ToArray());

            var movements = await test.Bus.Call<IInventoryQueryHandler>().GetRecentMovements(count, Token);

            Assert.Equal(expected, movements.Length);
        }
    }
}
