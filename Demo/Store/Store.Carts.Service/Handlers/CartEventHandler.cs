using Store.Carts.Domain;
using Store.Carts.Domain.Events;
using Zerra.CQRS;

namespace Store.Carts.Service.Handlers
{
    /// <summary>
    /// The cart aggregate dispatches every event it appends, so something has to handle them. Nothing else in the store reacts to carts yet,
    /// so this service handles its own events and logs them. Another service could subscribe with an event producer, the way Orders publishes to Inventory and Shipping.
    /// </summary>
    public sealed class CartEventHandler : BaseHandler, ICartEventHandler
    {
        public Task Handle(CartItemAddedEvent @event)
        {
            Log?.Info($"Event: {@event.Quantity} x {@event.ProductName} added to cart {@event.CustomerID}");
            return Task.CompletedTask;
        }

        public Task Handle(CartItemRemovedEvent @event)
        {
            Log?.Info($"Event: product {@event.ProductID} removed from cart {@event.CustomerID}");
            return Task.CompletedTask;
        }

        public Task Handle(CartEmptiedEvent @event)
        {
            Log?.Info($"Event: cart {@event.CustomerID} emptied");
            return Task.CompletedTask;
        }

        public Task Handle(CartCheckedOutEvent @event)
        {
            Log?.Info($"Event: cart {@event.CustomerID} checked out as order {@event.OrderNumber}");
            return Task.CompletedTask;
        }
    }
}
