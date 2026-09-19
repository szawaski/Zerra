using Zerra.Repository;

namespace Store.Carts.Service.Aggregates
{
    /// <summary>
    /// The Catalog changed a product's price while it was in the cart, so the cart took the new price. The event carries both prices,
    /// which keeps the stream a record of what the customer saw and when it changed.
    /// </summary>
    public sealed class CartItemRepricedEvent : IAggregateEvent
    {
        public required Guid CustomerID { get; init; }
        public required Guid ProductID { get; init; }
        public required string ProductName { get; init; }
        public required decimal OldUnitPrice { get; init; }
        public required decimal NewUnitPrice { get; init; }
    }
}
