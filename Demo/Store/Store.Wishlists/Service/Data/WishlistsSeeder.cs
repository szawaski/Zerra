using Store.Common;
using Zerra.Logging;
using Zerra.Repository;

namespace Store.Wishlists.Service.Data
{
    public static class WishlistsSeeder
    {
        /// <summary>
        /// Seeds a few wishlists the first time the service starts against an empty data store.
        /// Grace and Alan both want the Standing Desk, so it leads the most wished products.
        /// </summary>
        public static async Task SeedAsync(IRepo repo, ILogger log)
        {
            if (await repo.Query<WishlistItemDataModel>().AnyAsync())
                return;

            await repo.CreateManyAsync<WishlistItemDataModel>(
            [
                new() { ID = Guid.NewGuid(), CustomerID = DemoCustomerIds.Ada, ProductID = DemoProductIds.UltrawideMonitor, Note = "For the home office", AddedOn = DateTime.UtcNow.AddDays(-6) },
                new() { ID = Guid.NewGuid(), CustomerID = DemoCustomerIds.Ada, ProductID = DemoProductIds.ErgonomicChair, AddedOn = DateTime.UtcNow.AddDays(-2) },
                new() { ID = Guid.NewGuid(), CustomerID = DemoCustomerIds.Grace, ProductID = DemoProductIds.StandingDesk, Note = "Wait for a price drop", AddedOn = DateTime.UtcNow.AddDays(-3) },
                new() { ID = Guid.NewGuid(), CustomerID = DemoCustomerIds.Grace, ProductID = DemoProductIds.NoiseCancellingHeadphones, AddedOn = DateTime.UtcNow.AddHours(-30) },
                new() { ID = Guid.NewGuid(), CustomerID = DemoCustomerIds.Alan, ProductID = DemoProductIds.StandingDesk, AddedOn = DateTime.UtcNow.AddHours(-5) }
            ]);

            log.Info("Seeded 5 wishlist items");
        }
    }
}
