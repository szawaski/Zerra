using Zerra.CQRS;

namespace Store.Catalog.Domain.Events
{
    /// <summary>
    /// A product was discontinued. Anything holding a copy of it drops it, so it is not still offered as active.
    /// </summary>
    public sealed class ProductDiscontinuedEvent : IEvent
    {
        public required Guid ProductID { get; init; }
        public required string Name { get; init; }
    }
}
