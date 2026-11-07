using Store.Wishlists.Domain;
using Store.Wishlists.Domain.Commands;
using Xunit;
using Zerra.CQRS;

namespace Store.Wishlists.Test
{
    public class WishlistsQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Task Add(WishlistsTestBus test, Guid customerID, Guid productID, string? note = null)
            => test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = productID, Note = note }, Token);

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheService()
        {
            var test = new WishlistsTestBus();

            Assert.Equal("Test data store", await test.Bus.Call<IWishlistsQueryHandler>().GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Bus.Call<IWishlistsQueryHandler>().GetMessagingName(Token));
        }

        [Fact]
        public async Task GetWishlist_OnlyThatCustomerNewestFirst()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var chair = test.AddProduct("Ergonomic Chair");
            var customerID = Guid.NewGuid();
            await Add(test, customerID, lamp.ID, "For the desk");
            await Task.Delay(10, Token);
            await Add(test, customerID, chair.ID);
            await Add(test, Guid.NewGuid(), lamp.ID);

            var wishlist = await test.Bus.Call<IWishlistsQueryHandler>().GetWishlist(customerID, Token);

            Assert.Equal(["Ergonomic Chair", "Desk Lamp"], wishlist.Select(x => x.ProductName));
            Assert.Equal("For the desk", wishlist[1].Note);
        }

        [Fact]
        public async Task GetWishlist_HasTheCatalogsCurrentPriceAndStatus()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 30.00m);
            var customerID = Guid.NewGuid();
            await Add(test, customerID, lamp.ID);

            lamp.Price = 25.00m;
            lamp.IsActive = false;
            var item = Assert.Single(await test.Bus.Call<IWishlistsQueryHandler>().GetWishlist(customerID, Token));

            Assert.Equal(25.00m, item.Price);
            Assert.False(item.IsActive);
        }

        [Fact]
        public async Task GetWishlist_ProductGoneFromTheCatalog_IsStillListed()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var customerID = Guid.NewGuid();
            await Add(test, customerID, lamp.ID);

            _ = test.Catalog.Products.Remove(lamp);
            var item = Assert.Single(await test.Bus.Call<IWishlistsQueryHandler>().GetWishlist(customerID, Token));

            Assert.Equal("No longer in the catalog", item.ProductName);
            Assert.False(item.IsActive);
        }

        [Fact]
        public async Task GetWishlist_NothingSaved_IsEmpty()
        {
            var test = new WishlistsTestBus();

            Assert.Empty(await test.Bus.Call<IWishlistsQueryHandler>().GetWishlist(Guid.NewGuid(), Token));
        }

        [Fact]
        public async Task GetMostWished_CountsCustomersPerProductMostFirst()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var chair = test.AddProduct("Ergonomic Chair");
            var desk = test.AddProduct("Standing Desk");
            for (var i = 0; i < 3; i++)
                await Add(test, Guid.NewGuid(), desk.ID);
            for (var i = 0; i < 2; i++)
                await Add(test, Guid.NewGuid(), lamp.ID);
            await Add(test, Guid.NewGuid(), chair.ID);

            var wished = await test.Bus.Call<IWishlistsQueryHandler>().GetMostWished(2, Token);

            Assert.Equal([("Standing Desk", 3), ("Desk Lamp", 2)], wished.Select(x => (x.ProductName, x.Customers)));
        }
    }
}
