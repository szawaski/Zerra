using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    public static class CatalogSeeder
    {
        private static readonly Guid peripherals = Guid.Parse("5c1e7d2a-3b44-4f7e-8a21-7f0c9b1a0001");
        private static readonly Guid displays = Guid.Parse("5c1e7d2a-3b44-4f7e-8a21-7f0c9b1a0002");
        private static readonly Guid audioVideo = Guid.Parse("5c1e7d2a-3b44-4f7e-8a21-7f0c9b1a0003");
        private static readonly Guid furniture = Guid.Parse("5c1e7d2a-3b44-4f7e-8a21-7f0c9b1a0004");

        /// <summary>
        /// Seeds the catalog the first time the service starts against an empty data store.
        /// </summary>
        public static async Task SeedAsync(IRepo repo, ILogger log)
        {
            if (await repo.AnyAsync<CategoryDataModel>())
                return;

            await repo.CreateAsync<CategoryDataModel>(
            [
                new() { ID = peripherals, Name = "Peripherals" },
                new() { ID = displays, Name = "Displays" },
                new() { ID = audioVideo, Name = "Audio & Video" },
                new() { ID = furniture, Name = "Furniture" }
            ]);

            await repo.CreateAsync<ProductDataModel>(
            [
                Product(DemoProductIds.MechanicalKeyboard, peripherals, "KB-100", "Mechanical Keyboard", "Tenkeyless, hot-swappable switches", 129.00m),
                Product(DemoProductIds.WirelessMouse, peripherals, "MS-200", "Wireless Mouse", "Ergonomic, 70 day battery", 49.99m),
                Product(DemoProductIds.UsbCDock, peripherals, "DK-300", "USB-C Dock", "Dual display, 100 W pass-through", 189.00m),
                Product(DemoProductIds.UltrawideMonitor, displays, "MN-340", "34\" Ultrawide Monitor", "3440 x 1440, 144 Hz", 549.00m),
                Product(DemoProductIds.NoiseCancellingHeadphones, audioVideo, "HP-500", "Noise Cancelling Headphones", "Over-ear, 30 hour battery", 299.00m),
                Product(DemoProductIds.Webcam4K, audioVideo, "WC-400", "4K Webcam", "Auto framing, dual microphones", 159.00m),
                Product(DemoProductIds.StandingDesk, furniture, "SD-600", "Standing Desk", "Dual motor, 48\" x 30\" top", 649.00m),
                Product(DemoProductIds.ErgonomicChair, furniture, "CH-700", "Ergonomic Chair", "Mesh back, adjustable lumbar", 429.00m),
                Product(DemoProductIds.DeskLamp, furniture, "LP-800", "LED Desk Lamp", "Dimmable, adjustable color temperature", 59.00m)
            ]);

            log.Info("Seeded 4 categories and 9 products");
        }

        private static ProductDataModel Product(Guid id, Guid categoryID, string sku, string name, string description, decimal price) => new()
        {
            ID = id,
            CategoryID = categoryID,
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            Status = nameof(ProductStatus.Active)
        };
    }
}
