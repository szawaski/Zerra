using Zerra.CQRS;

namespace Store.Orders.Domain.Events
{
    public sealed class OrderShippedEvent : IEvent
    {
        public required Guid OrderID { get; init; }
        public required string OrderNumber { get; init; }
        public required DateTime ShippedOn { get; init; }
    }
}
