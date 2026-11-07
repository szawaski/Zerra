using Store.Wishlists.Domain.Commands;
using Store.Wishlists.Service.Data;
using Xunit;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Wishlists.Test
{
    public class WishlistsCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AddToWishlist_SavesTheProductWithItsNote()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var customerID = Guid.NewGuid();

            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID, Note = "  For the desk.  " }, Token);

            var item = Assert.Single(await test.Repo.Query<WishlistItemDataModel>().Where(x => x.CustomerID == customerID).ToArrayAsync(Token));
            Assert.Equal(lamp.ID, item.ProductID);
            Assert.Equal("For the desk.", item.Note);
        }

        [Fact]
        public async Task AddToWishlist_BlankNote_IsNotStored()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var customerID = Guid.NewGuid();

            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID, Note = "   " }, Token);

            Assert.Null(Assert.Single(await test.Repo.Query<WishlistItemDataModel>().Where(x => x.CustomerID == customerID).ToArrayAsync(Token)).Note);
        }

        [Fact]
        public async Task AddToWishlist_SameProductTwice_Throws()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var customerID = Guid.NewGuid();
            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token);

            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token));
            Assert.Contains("already on the wishlist", ex.Message);
            Assert.Single(await test.Repo.Query<WishlistItemDataModel>().Where(x => x.CustomerID == customerID).ToArrayAsync(Token));
        }

        [Fact]
        public async Task AddToWishlist_SameProductForAnotherCustomer_IsSaved()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");

            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = lamp.ID }, Token);
            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = lamp.ID }, Token);

            Assert.Equal(2, await test.Repo.Query<WishlistItemDataModel>().CountAsync(x => x.ProductID == lamp.ID, Token));
        }

        [Fact]
        public async Task AddToWishlist_DiscontinuedProduct_Throws()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp", isActive: false);

            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = lamp.ID }, Token));
            Assert.Equal("Discontinued products can't be added to a wishlist.", ex.Message);
        }

        [Fact]
        public async Task AddToWishlist_ProductNotInCatalog_Throws()
        {
            var test = new WishlistsTestBus();

            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = Guid.NewGuid() }, Token));
            Assert.Equal("Product not found.", ex.Message);
        }

        [Fact]
        public async Task AddToWishlist_NoteTooLong_Throws()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");

            _ = await Assert.ThrowsAsync<CqrsServiceException>(() => test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = lamp.ID, Note = new string('a', 201) }, Token));
        }

        [Fact]
        public async Task RemoveFromWishlist_RemovesOnlyThatProduct()
        {
            var test = new WishlistsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var chair = test.AddProduct("Ergonomic Chair");
            var customerID = Guid.NewGuid();
            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token);
            await test.Bus.DispatchAwaitAsync(new AddToWishlistCommand() { CustomerID = customerID, ProductID = chair.ID }, Token);

            await test.Bus.DispatchAwaitAsync(new RemoveFromWishlistCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token);

            Assert.Equal(chair.ID, Assert.Single(await test.Repo.Query<WishlistItemDataModel>().Where(x => x.CustomerID == customerID).ToArrayAsync(Token)).ProductID);
        }

        [Fact]
        public async Task RemoveFromWishlist_NotOnTheWishlist_Throws()
        {
            var test = new WishlistsTestBus();

            var ex = await Assert.ThrowsAsync<CqrsServiceException>(() => test.Bus.DispatchAwaitAsync(new RemoveFromWishlistCommand() { CustomerID = Guid.NewGuid(), ProductID = Guid.NewGuid() }, Token));
            Assert.Equal("That product isn't on the wishlist.", ex.Message);
        }
    }
}
