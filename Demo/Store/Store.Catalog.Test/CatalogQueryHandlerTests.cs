using Store.Catalog.Domain;
using Store.Catalog.Service.Data;
using Xunit;

namespace Store.Catalog.Test
{
    public class CatalogQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new CatalogTestBus();

            Assert.Equal("Test data store", await test.Bus.Call<ICatalogQueryHandler>().GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Bus.Call<ICatalogQueryHandler>().GetMessagingName(Token));
        }

        [Fact]
        public async Task GetCategories_SortedByName()
        {
            var test = new CatalogTestBus();
            var seating = await test.AddCategory("Seating");
            var lighting = await test.AddCategory("Lighting");

            var categories = await test.Bus.Call<ICatalogQueryHandler>().GetCategories(Token);

            Assert.Equal([lighting.ID, seating.ID], categories.Select(x => x.ID).ToArray());
            Assert.Equal("Lighting", categories[0].Name);
        }

        [Fact]
        public async Task GetProducts_SortedByNameWithTheirCategory()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);
            var bulb = await test.AddProduct(category.ID, "Bulb", 5.00m, ProductStatus.Discontinued);

            var products = await test.Bus.Call<ICatalogQueryHandler>().GetProducts(Token);

            Assert.Equal([bulb.ID, lamp.ID], products.Select(x => x.ID).ToArray());
            Assert.All(products, x => Assert.Equal("Lighting", x.CategoryName));
            Assert.False(products[0].IsActive);
            Assert.True(products[1].IsActive);
            Assert.Equal(lamp.Sku, products[1].Sku);
            Assert.Equal(59.00m, products[1].Price);
        }

        [Fact]
        public async Task GetProductsByCategory_OnlyThatCategory()
        {
            var test = new CatalogTestBus();
            var lighting = await test.AddCategory("Lighting");
            var seating = await test.AddCategory("Seating");
            var lamp = await test.AddProduct(lighting.ID, "Desk Lamp", 59.00m);
            var bulb = await test.AddProduct(lighting.ID, "Bulb", 5.00m);
            _ = await test.AddProduct(seating.ID, "Chair", 429.00m);

            var products = await test.Bus.Call<ICatalogQueryHandler>().GetProductsByCategory(lighting.ID, Token);

            Assert.Equal([bulb.ID, lamp.ID], products.Select(x => x.ID).ToArray());
        }

        [Fact]
        public async Task GetProductsByIDs_OnlyThoseProducts()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);
            var bulb = await test.AddProduct(category.ID, "Bulb", 5.00m);
            _ = await test.AddProduct(category.ID, "Floor Lamp", 89.00m);

            var products = await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([lamp.ID, bulb.ID, Guid.NewGuid()], Token);

            Assert.Equal(2, products.Length);
            Assert.Contains(products, x => x.ID == lamp.ID && x.Name == "Desk Lamp" && x.CategoryName == "Lighting");
            Assert.Contains(products, x => x.ID == bulb.ID);
        }

        [Fact]
        public async Task GetProductsByIDs_NoIDs_IsEmpty()
        {
            var test = new CatalogTestBus();

            Assert.Empty(await test.Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([], Token));
        }
    }
}
