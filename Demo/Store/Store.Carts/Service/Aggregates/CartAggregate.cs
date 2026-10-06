using Zerra.Repository;

namespace Store.Carts.Service.Aggregates
{
    /// <summary>
    /// A customer's shopping cart, event sourced. The cart has no table: its state is only ever the result of replaying the events in its stream,
    /// one stream per customer, named from this type and the customer's ID.
    /// </summary>
    /// <remarks>
    /// <see cref="AggregateRoot.Append"/> calls the public method here that takes the event's type, so the same method changes the state
    /// when a command appends a new event and when <see cref="AggregateRoot.Rebuild"/> replays an old one.
    /// </remarks>
    public sealed class CartAggregate : AggregateRoot
    {
        private readonly List<CartItem> items = new();


        public CartAggregate(Guid customerID, IEventStoreEngine eventStore)
            : base(customerID, eventStore)
        {
        }

        public IReadOnlyList<CartItem> Items => items;
        public int ItemCount => items.Sum(x => x.Quantity);
        public decimal Total => items.Sum(x => x.UnitPrice * x.Quantity);
        public string? LastOrderNumber { get; private set; }

        public Task On(CartItemAddedEvent @event)
        {
            var item = items.FirstOrDefault(x => x.ProductID == @event.ProductID);
            if (item is null)
            {
                items.Add(new CartItem() { ProductID = @event.ProductID, ProductName = @event.ProductName, UnitPrice = @event.UnitPrice, Quantity = @event.Quantity });
            }
            else
            {
                //adding the same product again keeps the price it was first added at
                item.Quantity += @event.Quantity;
            }
            return Task.CompletedTask;
        }

        public Task On(CartItemRemovedEvent @event)
        {
            _ = items.RemoveAll(x => x.ProductID == @event.ProductID);
            return Task.CompletedTask;
        }

        public Task On(CartItemRepricedEvent @event)
        {
            //the product may have left the cart between the Catalog price change and this event, and a replay then has nothing to reprice
            var item = items.FirstOrDefault(x => x.ProductID == @event.ProductID);
            if (item is not null)
                item.UnitPrice = @event.NewUnitPrice;
            return Task.CompletedTask;
        }

        public Task On(CartEmptiedEvent @event)
        {
            items.Clear();
            return Task.CompletedTask;
        }

        public Task On(CartCheckedOutEvent @event)
        {
            items.Clear();
            LastOrderNumber = @event.OrderNumber;
            return Task.CompletedTask;
        }
    }
}
