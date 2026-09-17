using Zerra.CQRS;

namespace Store.Carts.Domain.Events
{
    public sealed class CartItemRemovedEvent : IEvent
    {
        public required Guid CustomerID { get; init; }
        public required Guid ProductID { get; init; }
    }
}
