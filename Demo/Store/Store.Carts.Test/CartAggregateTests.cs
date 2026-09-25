using Store.Carts.Service.Aggregates;
using Xunit;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Carts.Test
{
    public class CartAggregateTests
    {
        private static readonly Guid customerID = Guid.NewGuid();
        private static readonly Guid lampID = Guid.NewGuid();
        private static readonly Guid mouseID = Guid.NewGuid();

        private static CartItemAddedEvent Added(Guid productID, string name, decimal price, int quantity)
            => new() { CustomerID = customerID, ProductID = productID, ProductName = name, UnitPrice = price, Quantity = quantity };

        [Fact]
        public async Task Rebuild_NoStream_ReturnsFalseAndStaysEmpty()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());

            Assert.False(await cart.Rebuild());
            Assert.Empty(cart.Items);
            Assert.Null(cart.LastEventNumber);
        }

        [Fact]
        public async Task Append_AppliesTheEvent()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());

            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 2));

            var item = Assert.Single(cart.Items);
            Assert.Equal("Desk Lamp", item.ProductName);
            Assert.Equal(2, cart.ItemCount);
            Assert.Equal(118.00m, cart.Total);
            Assert.Equal(nameof(CartItemAddedEvent), cart.LastEventName);
        }

        [Fact]
        public async Task Rebuild_ReplaysTheStreamToTheSameState()
        {
            IEventStoreEngine eventStore = new MemoryEngine();
            var cart = new CartAggregate(customerID, eventStore);
            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 2));
            await cart.Append(Added(mouseID, "Mouse", 49.99m, 1));
            await cart.Append(new CartItemRepricedEvent() { CustomerID = customerID, ProductID = mouseID, ProductName = "Mouse", OldUnitPrice = 49.99m, NewUnitPrice = 39.99m });
            await cart.Append(new CartItemRemovedEvent() { CustomerID = customerID, ProductID = lampID });

            var rebuilt = new CartAggregate(customerID, eventStore);
            Assert.True(await rebuilt.Rebuild());

            var item = Assert.Single(rebuilt.Items);
            Assert.Equal(mouseID, item.ProductID);
            Assert.Equal(39.99m, item.UnitPrice);
            Assert.Equal(cart.LastEventNumber, rebuilt.LastEventNumber);
        }

        [Fact]
        public async Task AddingAProductAgain_KeepsItsFirstPrice()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());

            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 1));
            await cart.Append(Added(lampID, "Desk Lamp", 45.00m, 2));

            var item = Assert.Single(cart.Items);
            Assert.Equal(3, item.Quantity);
            Assert.Equal(59.00m, item.UnitPrice);
        }

        [Fact]
        public async Task Repricing_AProductNoLongerInTheCart_ChangesNothing()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());
            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 1));

            await cart.Append(new CartItemRepricedEvent() { CustomerID = customerID, ProductID = mouseID, ProductName = "Mouse", OldUnitPrice = 49.99m, NewUnitPrice = 39.99m });

            Assert.Equal(lampID, Assert.Single(cart.Items).ProductID);
            Assert.Equal(59.00m, cart.Total);
        }

        [Fact]
        public async Task Emptied_ClearsTheItems()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());
            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 1));
            await cart.Append(Added(mouseID, "Mouse", 49.99m, 1));

            await cart.Append(new CartEmptiedEvent() { CustomerID = customerID });

            Assert.Empty(cart.Items);
            Assert.Equal(0m, cart.Total);
            Assert.Null(cart.LastOrderNumber);
        }

        [Fact]
        public async Task CheckedOut_ClearsTheItemsAndKeepsTheOrderNumber()
        {
            var cart = new CartAggregate(customerID, new MemoryEngine());
            await cart.Append(Added(lampID, "Desk Lamp", 59.00m, 1));

            await cart.Append(new CartCheckedOutEvent() { CustomerID = customerID, OrderID = Guid.NewGuid(), OrderNumber = "SO-1", Total = 59.00m });

            Assert.Empty(cart.Items);
            Assert.Equal("SO-1", cart.LastOrderNumber);
        }

        [Fact]
        public async Task Append_ValidatingAgainstAStaleCart_Throws()
        {
            IEventStoreEngine eventStore = new MemoryEngine();
            var first = new CartAggregate(customerID, eventStore);
            await first.Append(Added(lampID, "Desk Lamp", 59.00m, 1), true);
            var stale = new CartAggregate(customerID, eventStore);
            _ = await stale.Rebuild();

            //another command appends after the stale copy was rebuilt
            await first.Append(Added(mouseID, "Mouse", 49.99m, 1), true);

            _ = await Assert.ThrowsAnyAsync<Exception>(() => stale.Append(new CartEmptiedEvent() { CustomerID = customerID }, true));
            Assert.Single(stale.Items);
        }
    }
}
