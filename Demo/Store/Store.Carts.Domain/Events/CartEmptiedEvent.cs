using Zerra.CQRS;

namespace Store.Carts.Domain.Events
{
    public sealed class CartEmptiedEvent : IEvent
    {
        public required Guid CustomerID { get; init; }
    }
}
