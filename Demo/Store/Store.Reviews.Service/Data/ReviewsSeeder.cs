using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Reviews.Service.Data
{
    public static class ReviewsSeeder
    {
        /// <summary>
        /// Seeds a few reviews the first time the service starts against an empty data store.
        /// Ada and Grace are reviewing products the Orders seed data has them actually buying and receiving, so those reviews are verified.
        /// Alan is reviewing something he never ordered, showing the unverified case too.
        /// </summary>
        public static async Task SeedAsync(IRepo repo, ILogger log)
        {
            if (await repo.AnyAsync<ReviewDataModel>())
                return;

            await repo.CreateAsync<ReviewDataModel>(
            [
                new()
                {
                    ID = Guid.NewGuid(),
                    ProductID = DemoProductIds.MechanicalKeyboard,
                    ProductName = "Mechanical Keyboard",
                    CustomerID = DemoCustomerIds.Ada,
                    CustomerName = "Ada Lovelace",
                    Rating = 5,
                    Comment = "Tactile, quiet enough for calls, and the hot-swap sockets made trying new switches easy.",
                    VerifiedPurchase = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-4)
                },
                new()
                {
                    ID = Guid.NewGuid(),
                    ProductID = DemoProductIds.UltrawideMonitor,
                    ProductName = "34\" Ultrawide Monitor",
                    CustomerID = DemoCustomerIds.Grace,
                    CustomerName = "Grace Hopper",
                    Rating = 4,
                    Comment = "Great for side-by-side work. Docked over one cable with the USB-C Dock too.",
                    VerifiedPurchase = true,
                    CreatedOn = DateTime.UtcNow.AddHours(-20)
                },
                new()
                {
                    ID = Guid.NewGuid(),
                    ProductID = DemoProductIds.NoiseCancellingHeadphones,
                    ProductName = "Noise Cancelling Headphones",
                    CustomerID = DemoCustomerIds.Alan,
                    CustomerName = "Alan Turing",
                    Rating = 3,
                    Comment = "Tried a friend's pair, decent cancellation but the case is bulky.",
                    VerifiedPurchase = false,
                    CreatedOn = DateTime.UtcNow.AddHours(-3)
                }
            ]);

            log.Info("Seeded 3 reviews");
        }
    }
}
