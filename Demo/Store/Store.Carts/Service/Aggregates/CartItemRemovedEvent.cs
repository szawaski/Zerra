using Zerra.Repository;

namespace Store.Carts.Service.Aggregates
{
    public sealed class CartItemRemovedEvent : IAggregateEvent
    {
        public required Guid CustomerID { get; init; }
        public required Guid ProductID { get; init; }
    }
}
