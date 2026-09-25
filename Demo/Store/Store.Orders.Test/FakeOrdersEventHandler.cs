using Store.Orders.Domain;
using Store.Orders.Domain.Events;
using Zerra.CQRS;

namespace Store.Orders.Test
{
    public sealed class FakeOrdersEventHandler : BaseHandler, IOrdersEventHandler
    {
        public List<OrderShippedEvent> Shipped { get; } = new();

        public Task Handle(OrderShippedEvent @event)
        {
            Shipped.Add(@event);
            return Task.CompletedTask;
        }
    }
}
