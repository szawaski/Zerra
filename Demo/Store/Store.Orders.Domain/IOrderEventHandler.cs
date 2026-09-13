using Store.Orders.Domain.Events;
using Zerra.CQRS;

namespace Store.Orders.Domain
{
    /// <summary>
    /// The events the Orders service publishes. A downstream service implements this interface to subscribe.
    /// </summary>
    public interface IOrderEventHandler :
        IEventHandler<OrderCancelledEvent>,
        IEventHandler<OrderShippedEvent>
    {
    }
}
