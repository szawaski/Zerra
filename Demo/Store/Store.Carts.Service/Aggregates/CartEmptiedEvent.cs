using Zerra.Repository;

namespace Store.Carts.Service.Aggregates
{
    public sealed class CartEmptiedEvent : IAggregateEvent
    {
        public required Guid CustomerID { get; init; }
    }
}
