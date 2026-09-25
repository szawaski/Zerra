using Store.Carts.Domain.Commands;
using Store.Carts.Service.Aggregates;
using Store.Catalog.Domain.Events;
using Store.Common;
using Store.Orders.Domain.Models;
using Xunit;

namespace Store.Carts.Test
{
    public class CartsCommandHandlerTests
    {
        private static readonly Guid customerID = DemoCustomerIds.Ada;
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AddToCart_AddsItemAtCatalogNameAndPrice()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);

            var cart = await test.Queries.GetCart(customerID, Token);
            var item = Assert.Single(cart.Items);
            Assert.Equal(lamp.ID, item.ProductID);
            Assert.Equal("Desk Lamp", item.ProductName);
            Assert.Equal(59.00m, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(118.00m, cart.Total);
        }

        [Fact]
        public async Task AddToCart_SameProductAddsToQuantity()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 3 }, Token);

            var cart = await test.Queries.GetCart(customerID, Token);
            var item = Assert.Single(cart.Items);
            Assert.Equal(5, item.Quantity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task AddToCart_QuantityOutOfRange_Throws(int quantity)
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = quantity }, Token));
        }

        [Fact]
        public async Task AddToCart_OverMaxQuantityOfOneProduct_Throws()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 60 }, Token);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 41 }, Token));
            Assert.Contains("Desk Lamp", ex.Message);
        }

        [Fact]
        public async Task AddToCart_OverMaxProducts_Throws()
        {
            var test = new CartsTestBus();
            for (var i = 0; i < 20; i++)
            {
                var product = test.AddProduct($"Product {i}", 1.00m);
                await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = product.ID, Quantity = 1 }, Token);
            }
            var oneTooMany = test.AddProduct("One Too Many", 1.00m);

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = oneTooMany.ID, Quantity = 1 }, Token));
        }

        [Fact]
        public async Task AddToCart_ProductNotInCatalog_Throws()
        {
            var test = new CartsTestBus();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = Guid.NewGuid(), Quantity = 1 }, Token));
            Assert.Equal("Product not found.", ex.Message);
        }

        [Fact]
        public async Task AddToCart_DiscontinuedProduct_Throws()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m, isActive: false);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 1 }, Token));
            Assert.Contains("discontinued", ex.Message);
        }

        [Fact]
        public async Task AddToCart_CachesTheProductUntilTheCatalogSaysItChanged()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);

            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 1 }, Token);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = DemoCustomerIds.Grace, ProductID = lamp.ID, Quantity = 1 }, Token);
            Assert.Equal(1, test.Catalog.GetProductsByIDsCalls);

            lamp.Price = 49.00m;
            await test.CatalogEvents.Handle(new ProductPriceChangedEvent() { ProductID = lamp.ID, Name = lamp.Name!, OldPrice = 59.00m, NewPrice = 49.00m });
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = DemoCustomerIds.Alan, ProductID = lamp.ID, Quantity = 1 }, Token);

            Assert.Equal(2, test.Catalog.GetProductsByIDsCalls);
            var cart = await test.Queries.GetCart(DemoCustomerIds.Alan, Token);
            Assert.Equal(49.00m, Assert.Single(cart.Items).UnitPrice);
        }

        [Fact]
        public async Task RemoveFromCart_RemovesTheProduct()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 1 }, Token);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = mouse.ID, Quantity = 1 }, Token);

            await test.Commands.Handle(new RemoveFromCartCommand() { CustomerID = customerID, ProductID = lamp.ID }, Token);

            var cart = await test.Queries.GetCart(customerID, Token);
            Assert.Equal(mouse.ID, Assert.Single(cart.Items).ProductID);
        }

        [Fact]
        public async Task RemoveFromCart_ProductNotInCart_Throws()
        {
            var test = new CartsTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new RemoveFromCartCommand() { CustomerID = customerID, ProductID = Guid.NewGuid() }, Token));
        }

        [Fact]
        public async Task EmptyCart_RemovesEverything()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 3 }, Token);

            await test.Commands.Handle(new EmptyCartCommand() { CustomerID = customerID }, Token);

            var cart = await test.Queries.GetCart(customerID, Token);
            Assert.Empty(cart.Items);
            Assert.Equal(0, cart.ItemCount);
            Assert.Equal(0m, cart.Total);
        }

        [Fact]
        public async Task EmptyCart_AlreadyEmpty_Throws()
        {
            var test = new CartsTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new EmptyCartCommand() { CustomerID = customerID }, Token));
        }

        [Fact]
        public async Task CheckoutCart_PlacesTheOrderAndEmptiesTheCart()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = mouse.ID, Quantity = 1 }, Token);

            var result = await test.Commands.Handle(new CheckoutCartCommand() { CustomerID = customerID }, Token);

            var placed = Assert.Single(test.Orders.PlacedOrders);
            Assert.Equal(customerID, placed.CustomerID);
            Assert.Equal(2, placed.Items.Length);
            Assert.Contains(placed.Items, x => x.ProductID == lamp.ID && x.Quantity == 2);
            Assert.Contains(placed.Items, x => x.ProductID == mouse.ID && x.Quantity == 1);

            Assert.Equal("SO-TEST-1", result.OrderNumber);
            var cart = await test.Queries.GetCart(customerID, Token);
            Assert.Empty(cart.Items);
            Assert.Equal("SO-TEST-1", cart.LastOrderNumber);
        }

        [Fact]
        public async Task CheckoutCart_EmptyCart_Throws()
        {
            var test = new CartsTestBus();

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new CheckoutCartCommand() { CustomerID = customerID }, Token));
            Assert.Empty(test.Orders.PlacedOrders);
        }

        [Fact]
        public async Task CheckoutCart_OrderRefused_LeavesTheCartAsItWas()
        {
            var test = new CartsTestBus();
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 2 }, Token);
            test.Orders.PlaceOrderFailure = new DomainException("Desk Lamp is out of stock.");

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new CheckoutCartCommand() { CustomerID = customerID }, Token));
            Assert.Equal("Desk Lamp is out of stock.", ex.Message);

            var cart = await test.Queries.GetCart(customerID, Token);
            Assert.Equal(2, Assert.Single(cart.Items).Quantity);
            Assert.Null(cart.LastOrderNumber);
        }

        [Fact]
        public async Task RepriceCartItems_RepricesOnlyTheCartsHoldingTheProduct()
        {
            var test = new CartsTestBus();
            test.Orders.Customers.Add(new CustomerModel() { ID = DemoCustomerIds.Ada });
            test.Orders.Customers.Add(new CustomerModel() { ID = DemoCustomerIds.Grace });
            test.Orders.Customers.Add(new CustomerModel() { ID = DemoCustomerIds.Alan });
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            var mouse = test.AddProduct("Mouse", 49.99m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = DemoCustomerIds.Ada, ProductID = lamp.ID, Quantity = 1 }, Token);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = DemoCustomerIds.Grace, ProductID = mouse.ID, Quantity = 1 }, Token);
            //Alan has never had a cart
            var graceBefore = await test.Queries.GetCart(DemoCustomerIds.Grace, Token);

            await test.Commands.Handle(new RepriceCartItemsCommand() { ProductID = lamp.ID, ProductName = "Desk Lamp", NewPrice = 45.00m }, Token);

            var adaCart = await test.Queries.GetCart(DemoCustomerIds.Ada, Token);
            Assert.Equal(45.00m, Assert.Single(adaCart.Items).UnitPrice);
            var graceCart = await test.Queries.GetCart(DemoCustomerIds.Grace, Token);
            Assert.Equal(graceBefore.LastEventNumber, graceCart.LastEventNumber);
            var alanCart = await test.Queries.GetCart(DemoCustomerIds.Alan, Token);
            Assert.Null(alanCart.LastEventNumber);
        }

        [Fact]
        public async Task RepriceCartItems_DeliveredTwice_AppendsOnce()
        {
            var test = new CartsTestBus();
            test.Orders.Customers.Add(new CustomerModel() { ID = customerID });
            var lamp = test.AddProduct("Desk Lamp", 59.00m);
            await test.Commands.Handle(new AddToCartCommand() { CustomerID = customerID, ProductID = lamp.ID, Quantity = 1 }, Token);

            var command = new RepriceCartItemsCommand() { ProductID = lamp.ID, ProductName = "Desk Lamp", NewPrice = 45.00m };
            await test.Commands.Handle(command, Token);
            var once = await test.Queries.GetCart(customerID, Token);
            await test.Commands.Handle(command, Token);
            var twice = await test.Queries.GetCart(customerID, Token);

            Assert.Equal(once.LastEventNumber, twice.LastEventNumber);
            var history = await test.Queries.GetCartHistory(customerID, Token);
            Assert.Equal(nameof(CartItemRepricedEvent), history[0].EventName);
        }
    }
}
