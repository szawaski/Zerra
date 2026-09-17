using Zerra.CQRS;

namespace Store.Carts.Domain.Events
{
    /// <summary>
    /// Units of a product were added to the cart. Adding a product that's already in the cart adds to its quantity.
    /// </summary>
    public sealed class CartItemAddedEvent : IEvent
    {
        public required Guid CustomerID { get; init; }
        public required Guid ProductID { get; init; }
        //the name and price are a snapshot of the catalog when the item was added
        public required string ProductName { get; init; }
        public required decimal UnitPrice { get; init; }
        public required int Quantity { get; init; }
    }
}
