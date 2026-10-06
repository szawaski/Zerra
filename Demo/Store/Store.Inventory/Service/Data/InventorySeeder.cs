using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    public static class InventorySeeder
    {
        /// <summary>
        /// Seeds opening stock the first time the service starts against an empty data store.
        /// A few products start low or empty so the out-of-stock rules are easy to try.
        /// </summary>
        public static async Task SeedAsync(IRepo repo, ILogger log)
        {
            if (await repo.AnyAsync<StockItemDataModel>())
                return;

            (Guid ProductID, int Quantity)[] openingStock =
            [
                (DemoProductIds.MechanicalKeyboard, 25),
                (DemoProductIds.WirelessMouse, 40),
                (DemoProductIds.UsbCDock, 12),
                (DemoProductIds.UltrawideMonitor, 6),
                (DemoProductIds.NoiseCancellingHeadphones, 15),
                (DemoProductIds.Webcam4K, 3),
                (DemoProductIds.StandingDesk, 2),
                (DemoProductIds.ErgonomicChair, 8),
                (DemoProductIds.DeskLamp, 0)
            ];

            await repo.CreateAsync(openingStock.Select(x => new StockItemDataModel() { ProductID = x.ProductID, OnHand = x.Quantity }).ToArray());

            var now = DateTime.UtcNow;
            await repo.CreateAsync(openingStock.Where(x => x.Quantity > 0).Select(x => new StockMovementDataModel()
            {
                ID = Guid.NewGuid(),
                ProductID = x.ProductID,
                Kind = nameof(StockMovementKind.Restocked),
                Quantity = x.Quantity,
                OccurredOn = now
            }).ToArray());

            log.Info($"Seeded stock for {openingStock.Length} products");
        }
    }
}
