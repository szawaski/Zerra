using Zerra.Repository;

namespace Store.Carts.Service.Aggregates
{
    /// <summary>
    /// The cart became an order in the Orders service, which leaves the cart empty for the customer's next order.
    /// </summary>
    public sealed class CartCheckedOutEvent : IAggregateEvent
    {
        public required Guid CustomerID { get; init; }
        public required Guid OrderID { get; init; }
        public required string OrderNumber { get; init; }
        public required decimal Total { get; init; }
    }
}
