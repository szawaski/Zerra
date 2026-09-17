using Store.Carts.Domain.Events;
using Zerra.CQRS;

namespace Store.Carts.Domain
{
    /// <summary>
    /// The events the cart aggregate appends to its stream. Appending also dispatches each one on the bus, so any service can subscribe.
    /// </summary>
    public interface ICartEventHandler :
        IEventHandler<CartItemAddedEvent>,
        IEventHandler<CartItemRemovedEvent>,
        IEventHandler<CartEmptiedEvent>,
        IEventHandler<CartCheckedOutEvent>
    {
    }
}
