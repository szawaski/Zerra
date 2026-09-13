using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    public static class OrdersSeeder
    {
        private static readonly Guid ada = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0001");
        private static readonly Guid grace = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0002");
        private static readonly Guid alan = Guid.Parse("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0003");

        /// <summary>
        /// Seeds customers and a little order history the first time the service starts against an empty data store.
        /// </summary>
        public static async Task SeedAsync(IRepo repo, ILogger log)
        {
            if (await repo.AnyAsync<CustomerDataModel>())
                return;

            await repo.CreateAsync<CustomerDataModel>(
            [
                new() { ID = ada, Name = "Ada Lovelace", Email = "ada@example.com" },
                new() { ID = grace, Name = "Grace Hopper", Email = "grace@example.com" },
                new() { ID = alan, Name = "Alan Turing", Email = "alan@example.com" }
            ]);

            //shipped orders never touch current stock, so the history doesn't need the Inventory service
            var firstPlacedOn = DateTime.UtcNow.AddDays(-6);
            await AddShippedOrderAsync(repo, ada, firstPlacedOn, firstPlacedOn.AddDays(1),
            [
                (DemoProductIds.MechanicalKeyboard, "Mechanical Keyboard", 129.00m, 1),
                (DemoProductIds.WirelessMouse, "Wireless Mouse", 49.99m, 1)
            ]);

            var secondPlacedOn = DateTime.UtcNow.AddDays(-2);
            await AddShippedOrderAsync(repo, grace, secondPlacedOn, secondPlacedOn.AddHours(20),
            [
                (DemoProductIds.UltrawideMonitor, "34\" Ultrawide Monitor", 549.00m, 2),
                (DemoProductIds.UsbCDock, "USB-C Dock", 189.00m, 1)
            ]);

            log.Info("Seeded 3 customers and 2 shipped orders");
        }

        private static async Task AddShippedOrderAsync(IRepo repo, Guid customerID, DateTime placedOn, DateTime shippedOn, (Guid ProductID, string ProductName, decimal UnitPrice, int Quantity)[] lines)
        {
            var order = new OrderDataModel()
            {
                ID = Guid.NewGuid(),
                OrderNumber = $"SO-{placedOn:yyMMdd}-{Random.Shared.Next(1000, 10000)}",
                CustomerID = customerID,
                PlacedOn = placedOn,
                Status = nameof(OrderStatus.Shipped),
                ClosedOn = shippedOn
            };
            await repo.CreateAsync(order);
            await repo.CreateAsync(lines.Select(x => new OrderLineDataModel()
            {
                ID = Guid.NewGuid(),
                OrderID = order.ID,
                ProductID = x.ProductID,
                ProductName = x.ProductName,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity
            }).ToArray());
        }
    }
}
