using Store.Common;
using Store.Inventory.Domain.Commands;
using Store.Inventory.Domain.Models;
using Store.Inventory.Service.Data;
using Store.Orders.Domain.Events;
using Xunit;
using Zerra.Repository;

namespace Store.Inventory.Test
{
    public class InventoryCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static async Task<Guid> Restocked(InventoryTestBus test, int quantity)
        {
            var productID = Guid.NewGuid();
            await test.Bus.DispatchAwaitAsync(new RestockProductCommand() { ProductID = productID, Quantity = quantity }, Token);
            return productID;
        }

        private static ReserveStockCommand Reserve(Guid orderID, params (Guid ProductID, int Quantity)[] items) => new()
        {
            OrderID = orderID,
            OrderNumber = $"SO-{orderID.ToString()[..8]}",
            Items = items.Select(x => new StockReservationItem() { ProductID = x.ProductID, ProductName = "Desk Lamp", Quantity = x.Quantity }).ToArray()
        };

        [Fact]
        public async Task RestockProduct_NewProduct_CreatesItsStock()
        {
            var test = new InventoryTestBus();

            var productID = await Restocked(test, 10);

            var item = await test.GetStockItem(productID);
            Assert.NotNull(item);
            Assert.Equal(10, item.OnHand);
            Assert.Equal(0, item.Reserved);
            var movement = Assert.Single(await test.GetMovements(productID));
            Assert.Equal(nameof(StockMovementKind.Restocked), movement.Kind);
            Assert.Equal(10, movement.Quantity);
        }

        [Fact]
        public async Task RestockProduct_ExistingProduct_AddsToItsStock()
        {
            var test = new InventoryTestBus();
            var productID = await Restocked(test, 10);

            await test.Bus.DispatchAwaitAsync(new RestockProductCommand() { ProductID = productID, Quantity = 5 }, Token);

            Assert.Equal(15, (await test.GetStockItem(productID))!.OnHand);
            Assert.Equal(2, (await test.GetMovements(productID)).Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10_001)]
        public async Task RestockProduct_QuantityOutOfRange_Throws(int quantity)
        {
            var test = new InventoryTestBus();
            var productID = Guid.NewGuid();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new RestockProductCommand() { ProductID = productID, Quantity = quantity }, Token));
            Assert.Null(await test.GetStockItem(productID));
        }

        [Fact]
        public async Task ReserveStock_ReservesEveryItem()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var mouse = await Restocked(test, 5);
            var orderID = Guid.NewGuid();

            await test.Bus.DispatchAwaitAsync(Reserve(orderID, (lamp, 3), (mouse, 5)), Token);

            Assert.Equal(3, (await test.GetStockItem(lamp))!.Reserved);
            Assert.Equal(5, (await test.GetStockItem(mouse))!.Reserved);
            Assert.Equal(10, (await test.GetStockItem(lamp))!.OnHand);
            var reservations = await test.Repo.ManyAsync<StockReservationDataModel>(x => x.OrderID == orderID);
            Assert.Equal(2, reservations.Count);
            Assert.Equal(nameof(StockMovementKind.Reserved), (await test.GetMovements(lamp))[^1].Kind);
        }

        [Fact]
        public async Task ReserveStock_SameProductTwice_ReservesTheTotal()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var orderID = Guid.NewGuid();

            await test.Bus.DispatchAwaitAsync(Reserve(orderID, (lamp, 2), (lamp, 3)), Token);

            Assert.Equal(5, (await test.GetStockItem(lamp))!.Reserved);
            var reservation = Assert.Single(await test.Repo.ManyAsync<StockReservationDataModel>(x => x.OrderID == orderID));
            Assert.Equal(5, reservation.Quantity);
        }

        [Fact]
        public async Task ReserveStock_OneItemShort_ReservesNothing()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var mouse = await Restocked(test, 2);
            var orderID = Guid.NewGuid();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Reserve(orderID, (lamp, 3), (mouse, 5)), Token));
            Assert.Contains("Only 2", ex.Message);

            Assert.Equal(0, (await test.GetStockItem(lamp))!.Reserved);
            Assert.Equal(0, (await test.GetStockItem(mouse))!.Reserved);
            Assert.Empty(await test.Repo.ManyAsync<StockReservationDataModel>(x => x.OrderID == orderID));
        }

        [Fact]
        public async Task ReserveStock_OutOfStock_Throws()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 1);
            await test.Bus.DispatchAwaitAsync(Reserve(Guid.NewGuid(), (lamp, 1)), Token);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Reserve(Guid.NewGuid(), (lamp, 1)), Token));
            Assert.Contains("out of stock", ex.Message);
        }

        [Fact]
        public async Task ReserveStock_ProductNeverStocked_Throws()
        {
            var test = new InventoryTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Reserve(Guid.NewGuid(), (Guid.NewGuid(), 1)), Token));
        }

        [Fact]
        public async Task ReserveStock_NoItems_Throws()
        {
            var test = new InventoryTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Reserve(Guid.NewGuid()), Token));
        }

        [Fact]
        public async Task ReserveStock_QuantityBelowOne_Throws()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Reserve(Guid.NewGuid(), (lamp, 0)), Token));
            Assert.Equal(0, (await test.GetStockItem(lamp))!.Reserved);
        }

        [Fact]
        public async Task ReserveStock_DeliveredTwice_ReservesOnce()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var command = Reserve(Guid.NewGuid(), (lamp, 3));

            await test.Bus.DispatchAwaitAsync(command, Token);
            await test.Bus.DispatchAwaitAsync(command, Token);

            Assert.Equal(3, (await test.GetStockItem(lamp))!.Reserved);
        }

        [Fact]
        public async Task ReleaseReservedStock_PutsTheUnitsBack()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var reserve = Reserve(Guid.NewGuid(), (lamp, 3));
            await test.Bus.DispatchAwaitAsync(reserve, Token);

            await test.Bus.DispatchAwaitAsync(new ReleaseReservedStockCommand() { OrderID = reserve.OrderID, OrderNumber = reserve.OrderNumber }, Token);

            var item = (await test.GetStockItem(lamp))!;
            Assert.Equal(0, item.Reserved);
            Assert.Equal(10, item.OnHand);
            Assert.Empty(await test.Repo.ManyAsync<StockReservationDataModel>(x => x.OrderID == reserve.OrderID));
            Assert.Equal(nameof(StockMovementKind.Released), (await test.GetMovements(lamp))[^1].Kind);
        }

        [Fact]
        public async Task ReleaseReservedStock_NothingReserved_DoesNothing()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);

            await test.Bus.DispatchAwaitAsync(new ReleaseReservedStockCommand() { OrderID = Guid.NewGuid(), OrderNumber = "SO-NONE" }, Token);

            Assert.Single(await test.GetMovements(lamp));
        }

        [Fact]
        public async Task OrderShipped_TakesTheReservedUnitsOffTheShelf()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var reserve = Reserve(Guid.NewGuid(), (lamp, 3));
            await test.Bus.DispatchAwaitAsync(reserve, Token);

            await test.Bus.DispatchAsync(new OrderShippedEvent() { OrderID = reserve.OrderID, OrderNumber = reserve.OrderNumber, ShippedOn = DateTime.UtcNow });

            var item = (await test.GetStockItem(lamp))!;
            Assert.Equal(0, item.Reserved);
            Assert.Equal(7, item.OnHand);
            Assert.Equal(nameof(StockMovementKind.Shipped), (await test.GetMovements(lamp))[^1].Kind);
        }

        [Fact]
        public async Task OrderShipped_DeliveredTwice_ShipsOnce()
        {
            var test = new InventoryTestBus();
            var lamp = await Restocked(test, 10);
            var reserve = Reserve(Guid.NewGuid(), (lamp, 3));
            await test.Bus.DispatchAwaitAsync(reserve, Token);
            var shipped = new OrderShippedEvent() { OrderID = reserve.OrderID, OrderNumber = reserve.OrderNumber, ShippedOn = DateTime.UtcNow };

            await test.Bus.DispatchAsync(shipped);
            await test.Bus.DispatchAsync(shipped);

            Assert.Equal(7, (await test.GetStockItem(lamp))!.OnHand);
        }
    }
}
