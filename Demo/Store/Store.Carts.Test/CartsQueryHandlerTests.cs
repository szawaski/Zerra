using Store.Carts.Domain;
using Store.Carts.Domain.Commands;
using Store.Carts.Service.Aggregates;
using Store.Common;
using Xunit;

namespace Store.Carts.Test
{
    public class CartsQueryHandlerTests
    {
        private static readonly Guid customerID = DemoCustomerIds.Grace;
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new CartsTestBus();

            Assert.Equal("Test event store", await test.Bus.Call<ICartsQueryHandler>().GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Bus.Call<ICartsQueryHandler>().GetMessagingName(Token));
        }

        [Fact]
        public async Task GetCart_CustomerWithoutCart_IsEmpty()
        {
            var test = new CartsTestBus();

            var cart = await test.Bus.Call<ICartsQueryHandler>().GetCart(customerID, Token);

            Assert.Equal(customerID, cart.CustomerID);
            Assert.Empty(cart.Items);
            Assert.Equal(0, cart.ItemCount);
            Assert.Equal(0m, cart.Total);
            Assert.Null(cart.LastEventNumber);
            Assert.Null(cart.UpdatedOn);
            Assert.Null(cart.LastOrderNumber);
        }

        [Fact]
        public async Task GetCart_TotalsTheItems()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);
            await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);
            await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = customerID, ProductID = mouse.ID, Quantity = 3 }, Token);

            var cart = await test.Bus.Call<ICartsQueryHandler>().GetCart(customerID, Token);

            Assert.Equal(2, cart.Items.Length);
            Assert.Equal(118.00m, cart.Items.Single(x => x.ProductID == lamp.ID).Total);
            Assert.Equal(149.97m, cart.Items.Single(x => x.ProductID == mouse.ID).Total);
            Assert.Equal(5, cart.ItemCount);
            Assert.Equal(267.97m, cart.Total);
            Assert.NotNull(cart.LastEventNumber);
            Assert.NotNull(cart.UpdatedOn);
        }

        [Fact]
        public async Task GetCart_OnlyThatCustomersCart()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = DemoCustomerIds.Ada, ProductID = lamp.ID, Quantity = 1 }, Token);

            var cart = await test.Bus.Call<ICartsQueryHandler>().GetCart(customerID, Token);

            Assert.Empty(cart.Items);
        }

        [Fact]
        public async Task GetCartHistory_CustomerWithoutCart_IsEmpty()
        {
            var test = new CartsTestBus();

            var history = await test.Bus.Call<ICartsQueryHandler>().GetCartHistory(customerID, Token);

            Assert.Empty(history);
        }

        [Fact]
        public async Task GetCartHistory_NewestFirstWithTheCartAfterEachEvent()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);
            await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);
            await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = customerID, ProductID = mouse.ID, Quantity = 1 }, Token);
            await test.Bus.DispatchAwaitAsync(new RemoveFromCartCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token);
            await test.Bus.DispatchAwaitAsync(new EmptyCartCommand() { CustomerID = customerID }, Token);

            var history = await test.Bus.Call<ICartsQueryHandler>().GetCartHistory(customerID, Token);

            Assert.Equal(
                [nameof(CartEmptiedEvent), nameof(CartItemRemovedEvent), nameof(CartItemAddedEvent), nameof(CartItemAddedEvent)],
                history.Select(x => x.EventName).ToArray());
            Assert.Equal([0, 1, 3, 2], history.Select(x => x.ItemCount).ToArray());
            Assert.Equal([0m, 49.99m, 167.99m, 118.00m], history.Select(x => x.Total).ToArray());

            var cart = await test.Bus.Call<ICartsQueryHandler>().GetCart(customerID, Token);
            Assert.Equal(cart.LastEventNumber, history[0].EventNumber);
        }

        [Fact]
        public async Task GetCartHistory_OnlyTheMostRecentEvents()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            for (var i = 0; i < 30; i++)
                await test.Bus.DispatchAwaitAsync(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 1 }, Token);

            var history = await test.Bus.Call<ICartsQueryHandler>().GetCartHistory(customerID, Token);

            Assert.Equal(20, history.Length);
            //the replay starts from the cart as it was before the first event shown, not from an empty cart
            Assert.Equal(30, history[0].ItemCount);
            Assert.Equal(11, history[^1].ItemCount);
        }
    }
}
