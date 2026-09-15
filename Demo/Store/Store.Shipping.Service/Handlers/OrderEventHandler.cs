using Store.Orders.Domain;
using Store.Orders.Domain.Events;
using Store.Shipping.Service.Data;
using Zerra.Repository;

namespace Store.Shipping.Service.Handlers
{
    /// <summary>
    /// Subscribes to the Orders service's events, the same interface Inventory subscribes to. The Orders service
    /// publishes once and both services receive it, each doing its own thing with the notification.
    /// </summary>
    public sealed class OrderEventHandler : BaseHandlerWithRepo, IOrderEventHandler
    {
        private static readonly string[] carriers = ["Ground Express", "Sky Freight", "QuickShip"];

        public async Task Handle(OrderShippedEvent @event)
        {
            //events can be delivered more than once, a shipment already exists means there's nothing left to do
            if (await Repo.AnyAsync<ShipmentDataModel>(x => x.OrderID == @event.OrderID))
                return;

            var shipment = new ShipmentDataModel()
            {
                ID = Guid.NewGuid(),
                OrderID = @event.OrderID,
                OrderNumber = @event.OrderNumber,
                Carrier = carriers[Random.Shared.Next(carriers.Length)],
                TrackingNumber = $"TRK-{Random.Shared.Next(100000, 1000000)}",
                Status = nameof(ShipmentStatus.InTransit),
                ShippedOn = @event.ShippedOn
            };
            await Repo.CreateAsync(shipment);

            Log?.Info($"Created shipment {shipment.TrackingNumber} for order {@event.OrderNumber} via {shipment.Carrier}");
        }

        //an order can only be cancelled while it's still Placed, so a cancelled order was never shipped and Shipping has nothing to do
        public Task Handle(OrderCancelledEvent @event) => Task.CompletedTask;
    }
}
