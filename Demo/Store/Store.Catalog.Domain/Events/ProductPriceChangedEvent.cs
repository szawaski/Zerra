using Zerra.CQRS;

namespace Store.Catalog.Domain.Events
{
    /// <summary>
    /// A product's price changed. Anything holding a copy of the old price drops it.
    /// </summary>
    public sealed class ProductPriceChangedEvent : IEvent
    {
        public required Guid ProductID { get; init; }
        public required string Name { get; init; }
        public required decimal OldPrice { get; init; }
        public required decimal NewPrice { get; init; }
    }
}
