using Store.Common;
using Store.Orders.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Orders.Test
{
    public class OrdersQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static async Task<OrderDataModel> AddOrder(OrdersTestBus test, Guid customerID, OrderStatus status, DateTime placedOn, params (Guid ProductID, string ProductName, decimal UnitPrice, int Quantity)[] items)
        {
            var order = new OrderDataModel()
            {
                ID = Guid.NewGuid(),
                OrderNumber = $"SO-{Guid.NewGuid().ToString()[..8]}",
                CustomerID = customerID,
                PlacedOn = placedOn,
                Status = status.ToString(),
                ClosedOn = status == OrderStatus.Placed ? null : placedOn.AddHours(1)
            };
            await test.Repo.CreateAsync(order);
            await test.Repo.CreateAsync(items.Select(x => new OrderItemDataModel()
            {
                ID = Guid.NewGuid(),
                OrderID = order.ID,
                ProductID = x.ProductID,
                ProductName = x.ProductName,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity
            }).ToArray());
            return order;
        }

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new OrdersTestBus();

            Assert.Equal("Test data store", await test.Queries.GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Queries.GetMessagingName(Token));
        }

        [Fact]
        public async Task GetCustomers_SortedByName()
        {
            var test = new OrdersTestBus();
            var grace = await test.AddCustomer("Grace Hopper");
            var ada = await test.AddCustomer("Ada Lovelace");

            var customers = await test.Queries.GetCustomers(Token);

            Assert.Equal([ada.ID, grace.ID], customers.Select(x => x.ID).ToArray());
            Assert.Equal("ada.lovelace@example.com", customers[0].Email);
        }

        [Fact]
        public async Task GetOrder_WithCustomerAndItems()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var lampID = Guid.NewGuid();
            var mouseID = Guid.NewGuid();
            var order = await AddOrder(test, customer.ID, OrderStatus.Placed, DateTime.UtcNow, (mouseID, "Mouse", 49.99m, 1), (lampID, "Desk Lamp", 59.00m, 2));

            var model = await test.Queries.GetOrder(order.ID, Token);

            Assert.Equal(order.OrderNumber, model.OrderNumber);
            Assert.Equal("Ada Lovelace", model.CustomerName);
            Assert.Equal(nameof(OrderStatus.Placed), model.Status);
            Assert.Equal(167.99m, model.Total);
            //items by product name
            Assert.Equal([lampID, mouseID], model.Items!.Select(x => x.ProductID).ToArray());
            Assert.Equal(118.00m, model.Items![0].Total);
        }

        [Fact]
        public async Task GetOrder_NotFound_Throws()
        {
            var test = new OrdersTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Queries.GetOrder(Guid.NewGuid(), Token));
        }

        [Fact]
        public async Task GetOrders_NewestFirst()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var now = DateTime.UtcNow;
            var older = await AddOrder(test, customer.ID, OrderStatus.Shipped, now.AddSeconds(1), (Guid.NewGuid(), "Desk Lamp", 59.00m, 1));
            var newer = await AddOrder(test, customer.ID, OrderStatus.Placed, now.AddSeconds(2), (Guid.NewGuid(), "Mouse", 49.99m, 2));

            var orders = await test.Queries.GetOrders(Token);

            Assert.Equal([newer.ID, older.ID], orders.Select(x => x.ID).ToArray());
            Assert.Equal("Ada Lovelace", orders[0].CustomerName);
            Assert.Equal(99.98m, orders[0].Total);
            Assert.Single(orders[0].Items!);
        }

        [Fact]
        public async Task GetOrders_AtMostFifty()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var now = DateTime.UtcNow;
            for (var i = 0; i < 51; i++)
                _ = await AddOrder(test, customer.ID, OrderStatus.Placed, now.AddSeconds(-i), (Guid.NewGuid(), "Desk Lamp", 59.00m, 1));

            var orders = await test.Queries.GetOrders(Token);

            Assert.Equal(50, orders.Length);
        }

        [Fact]
        public async Task HasPurchased_OnlyWhenAShippedOrderHasTheProduct()
        {
            var test = new OrdersTestBus();
            var customer = await test.AddCustomer("Ada Lovelace");
            var shippedProductID = Guid.NewGuid();
            var placedProductID = Guid.NewGuid();
            var cancelledProductID = Guid.NewGuid();
            _ = await AddOrder(test, customer.ID, OrderStatus.Shipped, DateTime.UtcNow, (shippedProductID, "Desk Lamp", 59.00m, 1));
            _ = await AddOrder(test, customer.ID, OrderStatus.Placed, DateTime.UtcNow, (placedProductID, "Mouse", 49.99m, 1));
            _ = await AddOrder(test, customer.ID, OrderStatus.Cancelled, DateTime.UtcNow, (cancelledProductID, "Chair", 429.00m, 1));

            Assert.True(await test.Queries.HasPurchased(customer.ID, shippedProductID, Token));
            Assert.False(await test.Queries.HasPurchased(customer.ID, placedProductID, Token));
            Assert.False(await test.Queries.HasPurchased(customer.ID, cancelledProductID, Token));
            Assert.False(await test.Queries.HasPurchased(Guid.NewGuid(), shippedProductID, Token));
        }
    }
}
