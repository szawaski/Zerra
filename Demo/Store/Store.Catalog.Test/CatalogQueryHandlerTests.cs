using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
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

        [Fact]
        public async Task ExportProductsCsv_OneLinePerProduct()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp, Brass", 59.00m);

            using var stream = await test.Bus.Call<ICatalogQueryHandler>().ExportProductsCsv(Token);
            using var reader = new StreamReader(stream);
            var lines = (await reader.ReadToEndAsync(Token)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(["Sku,Name,Category,Price,Status", $"{lamp.Sku},\"Desk Lamp, Brass\",Lighting,59.00,Active"], lines);
        }

        [Fact]
        public async Task PreviewProductImport_ComparesEachRowToTheCatalog()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 59.00m);
            var bulb = await test.AddProduct(category.ID, "Bulb", 5.00m);

            var csv = $"Sku,Name,Category,Price,Status\n{lamp.Sku},Desk Lamp,Lighting,64.50,Active\n{bulb.Sku},Bulb,Lighting,5.00,Active\nNEW-1,\"Lamp, Floor\",Lighting,89,Active\nBAD-1,Broken,Lighting,free,Active\n";
            var preview = await test.Bus.Call<ICatalogQueryHandler>().PreviewProductImport("import.csv", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv)), 50, Token);

            Assert.Equal("import.csv", preview.FileName);
            Assert.Equal(1, preview.PriceChangeCount);
            Assert.Equal(1, preview.UnchangedCount);
            Assert.Equal(1, preview.NewCount);
            Assert.Equal(1, preview.ErrorCount);
            Assert.Equal([ProductImportChange.PriceChange, ProductImportChange.Unchanged, ProductImportChange.New, ProductImportChange.Error], preview.Rows!.Select(x => x.Change).ToArray());
            Assert.Equal(59.00m, preview.Rows![0].CurrentPrice);
            Assert.Equal(64.50m, preview.Rows![0].Price);
            Assert.Equal("Lamp, Floor", preview.Rows![2].Name);
            Assert.Equal(5, preview.Rows![3].Line);
        }

        [Fact]
        public async Task PreviewProductImport_PriceChangeOverTheLimit_IsAnError()
        {
            var test = new CatalogTestBus();
            var category = await test.AddCategory("Lighting");
            var lamp = await test.AddProduct(category.ID, "Desk Lamp", 50.00m);

            var csv = $"{lamp.Sku},Desk Lamp,Lighting,60.00,Active\n";
            var preview = await test.Bus.Call<ICatalogQueryHandler>().PreviewProductImport("import.csv", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv)), 10, Token);

            Assert.Equal(ProductImportChange.Error, preview.Rows!.Single().Change);
            Assert.Equal("The price changes 20%, more than 10%", preview.Rows!.Single().Error);
        }
    }
}
