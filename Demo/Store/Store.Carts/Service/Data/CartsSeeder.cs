using Store.Carts.Service.Aggregates;
using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Carts.Service.Data
{
    public static class CartsSeeder
    {
        /// <summary>
        /// Starts Grace's cart the first time the service runs against an event store with no stream for it, so the Carts page has something to show.
        /// The events go through the aggregate like any command's would, so the cart is built the same way a customer would build it.
        /// </summary>
        public static async Task SeedAsync(IEventStoreEngine eventStore, ILogger log)
        {
            var cart = new CartAggregate(DemoCustomerIds.Grace, eventStore);
            if (await cart.Rebuild())
                return;

            //the names and prices match the Catalog seed data, since the Catalog service may not be running yet
            await cart.Append(new CartItemAddedEvent() { CustomerID = DemoCustomerIds.Grace, ProductID = DemoProductIds.Webcam4K, ProductName = "4K Webcam", UnitPrice = 159.00m, Quantity = 1 });
            await cart.Append(new CartItemAddedEvent() { CustomerID = DemoCustomerIds.Grace, ProductID = DemoProductIds.DeskLamp, ProductName = "LED Desk Lamp", UnitPrice = 59.00m, Quantity = 2 });

            log.Info("Seeded a cart for Grace Hopper");
        }
    }
}
