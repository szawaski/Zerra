using Store.Common;
using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Models;
using Store.Orders.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Orders.Test
{
    public class OrdersCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static PlaceOrderCommand Order(Guid customerID, params (Guid ProductID, int Quantity)[] items) => new()
        {
            CustomerID = customerID,
            Items = items.Select(x => new OrderItemRequest() { ProductID = x.ProductID, Quantity = x.Quantity }).ToArray()
        };

        private static async Task<PlaceOrderResult> Placed(OrdersTestBus test)
        {
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            return await test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, 1)), Token);
        }

        [Fact]
        public async Task PlaceOrder_SavesTheOrderAtCatalogPricesAndReservesTheStock()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);

            var result = await test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, 2), (mouse.ID, 1)), Token);

            Assert.Equal(167.99m, result.Total);
            Assert.StartsWith("SO-", result.OrderNumber);

            var order = await test.GetOrder(result.OrderID);
            Assert.NotNull(order);
            Assert.Equal(nameof(OrderStatus.Placed), order.Status);
            Assert.Equal(customer.ID, order.CustomerID);
            Assert.Equal(result.OrderNumber, order.OrderNumber);
            var items = await test.Repo.ManyAsync<OrderItemDataModel>(x => x.OrderID == result.OrderID);
            Assert.Contains(items, x => x.ProductID == lamp.ID && x.ProductName == "Desk Lamp" && x.UnitPrice == 59.00m && x.Quantity == 2);
            Assert.Contains(items, x => x.ProductID == mouse.ID && x.UnitPrice == 49.99m && x.Quantity == 1);

            var reserve = Assert.Single(test.Inventory.Reserved);
            Assert.Equal(result.OrderID, reserve.OrderID);
            Assert.Equal(result.OrderNumber, reserve.OrderNumber);
            Assert.Contains(reserve.Items, x => x.ProductID == lamp.ID && x.ProductName == "Desk Lamp" && x.Quantity == 2);
        }

        [Fact]
        public async Task PlaceOrder_SameProductTwice_IsOneLine()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            var result = await test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, 2), (lamp.ID, 3)), Token);

            var item = Assert.Single(await test.Repo.ManyAsync<OrderItemDataModel>(x => x.OrderID == result.OrderID));
            Assert.Equal(5, item.Quantity);
            Assert.Equal(295.00m, result.Total);
        }

        [Fact]
        public async Task PlaceOrder_CustomerNotFound_Throws()
        {
            var test = new OrdersTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(Guid.NewGuid(), (lamp.ID, 1)), Token));
            Assert.Equal("Customer not found.", ex.Message);
            Assert.Empty(test.Inventory.Reserved);
        }

        [Fact]
        public async Task PlaceOrder_NoItems_Throws()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID), Token));
        }

        [Fact]
        public async Task PlaceOrder_TooManyProducts_Throws()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var items = Enumerable.Range(0, 21).Select(i => (test.AddProduct($"Product {i}", 1.00m).ID, 1)).ToArray();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID, items), Token));
        }

        [Fact]
        public async Task PlaceOrder_ProductNotInCatalog_Throws()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID, (Guid.NewGuid(), 1)), Token));
            Assert.Empty(test.Inventory.Reserved);
        }

        [Fact]
        public async Task PlaceOrder_DiscontinuedProduct_Throws()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m, isActive: false);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, 1)), Token));
            Assert.Contains("discontinued", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task PlaceOrder_QuantityOutOfRange_Throws(int quantity)
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, quantity)), Token));
            Assert.Empty(test.Inventory.Reserved);
        }

        [Fact]
        public async Task PlaceOrder_StockNotReserved_SavesNothing()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            test.Inventory.ReserveFailure = new DomainException("Desk Lamp is out of stock.");

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(Order(customer.ID, (lamp.ID, 1)), Token));
            Assert.Equal("Desk Lamp is out of stock.", ex.Message);

            Assert.False(await test.Repo.AnyAsync<OrderDataModel>());
        }

        [Fact]
        public async Task CancelOrder_ClosesTheOrderAndReleasesTheStock()
        {
            var test = new OrdersTestBus();
            var placed = await Placed(test);

            await test.Bus.DispatchAwaitAsync(new CancelOrderCommand() { OrderID = placed.OrderID }, Token);

            var order = (await test.GetOrder(placed.OrderID))!;
            Assert.Equal(nameof(OrderStatus.Cancelled), order.Status);
            Assert.NotNull(order.ClosedOn);
            var release = Assert.Single(test.Inventory.Released);
            Assert.Equal(placed.OrderID, release.OrderID);
            Assert.Equal(placed.OrderNumber, release.OrderNumber);
            Assert.Empty(test.OrderEvents.Shipped);
        }

        [Fact]
        public async Task ShipOrder_ClosesTheOrderAndAnnouncesIt()
        {
            var test = new OrdersTestBus();
            var placed = await Placed(test);

            await test.Bus.DispatchAwaitAsync(new ShipOrderCommand() { OrderID = placed.OrderID }, Token);

            var order = (await test.GetOrder(placed.OrderID))!;
            Assert.Equal(nameof(OrderStatus.Shipped), order.Status);
            var shipped = Assert.Single(test.OrderEvents.Shipped);
            Assert.Equal(placed.OrderID, shipped.OrderID);
            Assert.Equal(placed.OrderNumber, shipped.OrderNumber);
            Assert.Equal(order.ClosedOn, shipped.ShippedOn);
            //shipping settles the reservation through the event, not the release command
            Assert.Empty(test.Inventory.Released);
        }

        [Fact]
        public async Task ShipOrder_Cancelled_Throws()
        {
            var test = new OrdersTestBus();
            var placed = await Placed(test);
            await test.Bus.DispatchAwaitAsync(new CancelOrderCommand() { OrderID = placed.OrderID }, Token);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ShipOrderCommand() { OrderID = placed.OrderID }, Token));
            Assert.Contains("cancelled", ex.Message);
            Assert.Empty(test.OrderEvents.Shipped);
        }

        [Fact]
        public async Task CancelOrder_Shipped_Throws()
        {
            var test = new OrdersTestBus();
            var placed = await Placed(test);
            await test.Bus.DispatchAwaitAsync(new ShipOrderCommand() { OrderID = placed.OrderID }, Token);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new CancelOrderCommand() { OrderID = placed.OrderID }, Token));
            Assert.Empty(test.Inventory.Released);
        }

        [Fact]
        public async Task CancelOrder_OrderNotFound_Throws()
        {
            var test = new OrdersTestBus();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new CancelOrderCommand() { OrderID = Guid.NewGuid() }, Token));
            Assert.Equal("Order not found.", ex.Message);
        }

        [Fact]
        public async Task ShipOrder_OrderNotFound_Throws()
        {
            var test = new OrdersTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ShipOrderCommand() { OrderID = Guid.NewGuid() }, Token));
        }
    }
}
