using Store.Catalog.Domain;
using Store.Catalog.Domain.Commands;
using Store.Catalog.Service.Data;
using Store.Common;
using Xunit;

namespace Store.Catalog.Test
{
    public class CatalogCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AddProduct_SavesAnActiveProduct()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var sku = CatalogTestBus.NewSku();

            var result = await test.Bus.DispatchAwaitAsync(new AddProductCommand()
            {
                CategoryID = category.ID,
                Sku = $"  {sku.ToLowerInvariant()} ",
                Name = " Desk Lamp ",
                Description = " Dimmable ",
                Price = 59.00m
            }, Token);

            var product = Assert.Single(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([result.ProductID], Token));
            Assert.Equal(sku, product.Sku);
            Assert.Equal("Desk Lamp", product.Name);
            Assert.Equal("Dimmable", product.Description);
            Assert.Equal(59.00m, product.Price);
            Assert.Equal("Lighting", product.CategoryName);
            Assert.True(product.IsActive);
        }

        [Fact]
        public async Task AddProduct_BlankDescription_IsNotStored()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            var result = await test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = CatalogTestBus.NewSku(), Name = "Desk Lamp", Description = "   ", Price = 59.00m }, Token);

            var product = Assert.Single(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([result.ProductID], Token));
            Assert.Null(product.Description);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456")]
        [InlineData("KB 100")]
        [InlineData("KB_100")]
        public async Task AddProduct_InvalidSku_Throws(string sku)
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = sku, Name = "Desk Lamp", Price = 59.00m }, Token));
            Assert.Contains("SKU", ex.Message);
        }

        [Fact]
        public async Task AddProduct_SkuInUse_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var existing = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = existing.Sku!, Name = "Floor Lamp", Price = 89.00m }, Token));
            Assert.Contains("already in use", ex.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddProduct_NoName_Throws(string name)
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = CatalogTestBus.NewSku(), Name = name, Price = 59.00m }, Token));
        }

        [Fact]
        public async Task AddProduct_NameTooLong_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = CatalogTestBus.NewSku(), Name = new string('a', 129), Price = 59.00m }, Token));
        }

        [Fact]
        public async Task AddProduct_DescriptionTooLong_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = CatalogTestBus.NewSku(), Name = "Desk Lamp", Description = new string('a', 513), Price = 59.00m }, Token));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("100000.01")]
        [InlineData("1.001")]
        public async Task AddProduct_InvalidPrice_Throws(string price)
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = category.ID, Sku = CatalogTestBus.NewSku(), Name = "Desk Lamp", Price = Decimal.Parse(price) }, Token));
        }

        [Fact]
        public async Task AddProduct_CategoryNotFound_Throws()
        {
            var test = new CatalogTestBus();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new AddProductCommand() { CategoryID = Guid.NewGuid(), Sku = CatalogTestBus.NewSku(), Name = "Desk Lamp", Price = 59.00m }, Token));
            Assert.Equal("Category not found.", ex.Message);
        }

        [Fact]
        public async Task ChangeProductPrice_SavesThePriceAndTellsTheSubscribersAndTheCarts()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);

            await test.Bus.DispatchAwaitAsync(new ChangeProductPriceCommand() { ProductID = lamp.ID, Price = 45.00m }, Token);

            var product = Assert.Single(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([lamp.ID], Token));
            Assert.Equal(45.00m, product.Price);

            var priceChanged = Assert.Single(test.CatalogEvents.PriceChanges);
            Assert.Equal(lamp.ID, priceChanged.ProductID);
            Assert.Equal(59.00m, priceChanged.OldPrice);
            Assert.Equal(45.00m, priceChanged.NewPrice);

            var reprice = Assert.Single(test.CartRepricing.Commands);
            Assert.Equal(lamp.ID, reprice.ProductID);
            Assert.Equal("Desk Lamp", reprice.ProductName);
            Assert.Equal(45.00m, reprice.NewPrice);
        }

        [Fact]
        public async Task ChangeProductPrice_SamePrice_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ChangeProductPriceCommand() { ProductID = lamp.ID, Price = 59.00m }, Token));
            Assert.Empty(test.CatalogEvents.PriceChanges);
            Assert.Empty(test.CartRepricing.Commands);
        }

        [Fact]
        public async Task ChangeProductPrice_InvalidPrice_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ChangeProductPriceCommand() { ProductID = lamp.ID, Price = 0m }, Token));

            var product = Assert.Single(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([lamp.ID], Token));
            Assert.Equal(59.00m, product.Price);
        }

        [Fact]
        public async Task ChangeProductPrice_Discontinued_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m, ProductStatus.Discontinued);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ChangeProductPriceCommand() { ProductID = lamp.ID, Price = 45.00m }, Token));
        }

        [Fact]
        public async Task ChangeProductPrice_ProductNotFound_Throws()
        {
            var test = new CatalogTestBus();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new ChangeProductPriceCommand() { ProductID = Guid.NewGuid(), Price = 45.00m }, Token));
            Assert.Equal("Product not found.", ex.Message);
        }

        [Fact]
        public async Task DiscontinueProduct_MarksItInactiveAndTellsTheSubscribers()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);

            await test.Bus.DispatchAwaitAsync(new DiscontinueProductCommand() { ProductID = lamp.ID }, Token);

            var product = Assert.Single(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([lamp.ID], Token));
            Assert.False(product.IsActive);
            Assert.Equal(lamp.ID, Assert.Single(test.CatalogEvents.Discontinued).ProductID);
            //a discontinued product leaves the carts' prices alone, the carts refuse it when it's added
            Assert.Empty(test.CartRepricing.Commands);
        }

        [Fact]
        public async Task DiscontinueProduct_AlreadyDiscontinued_Throws()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m, ProductStatus.Discontinued);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new DiscontinueProductCommand() { ProductID = lamp.ID }, Token));
            Assert.Empty(test.CatalogEvents.Discontinued);
        }

        [Fact]
        public async Task DiscontinueProduct_ProductNotFound_Throws()
        {
            var test = new CatalogTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new DiscontinueProductCommand() { ProductID = Guid.NewGuid() }, Token));
        }
    }
}
