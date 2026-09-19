using Store.Orders.Domain.Events;
using Zerra.CQRS;

namespace Store.Orders.Domain
{
    /// <summary>
    /// The events the Orders service publishes. A downstream service implements this interface to subscribe.
    /// </summary>
    /// <remarks>
    /// Subscribers register this one with <see cref="EventConsumerMode.PerService"/>, not the default
    /// <see cref="EventConsumerMode.PerReplica"/>: each handler writes to its service's own store, so it has to run once per
    /// service rather than once per replica. Compare <c>ICatalogEventHandler</c>, which is the default because every replica
    /// there has its own cache to drop.
    /// </remarks>
    public interface IOrdersEventHandler :
        IEventHandler<OrderShippedEvent>
    {
    }
}
