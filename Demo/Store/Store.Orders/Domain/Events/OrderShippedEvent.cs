using Zerra.CQRS;

namespace Store.Orders.Domain.Events
{
    /// <summary>
    /// An order shipped. Inventory takes its reserved units off the shelf and Shipping creates the shipment.
    /// </summary>
    /// <remarks>
    /// Both of those write once, which is normally what makes something a command. They subscribe with
    /// <see cref="EventConsumerMode.PerService"/>, so the replicas of each service compete for the event and only one of them
    /// handles it, the same as a command consumer. That lets Orders announce the fact once and stay out of deciding who acts on it.
    /// </remarks>
    public sealed class OrderShippedEvent : IEvent
    {
        public required Guid OrderID { get; init; }
        public required string OrderNumber { get; init; }
        public required DateTime ShippedOn { get; init; }
    }
}
