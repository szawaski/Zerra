using Store.Common;
using Store.Orders.Domain;
using Store.Orders.Domain.Events;
using Store.Shipping.Domain;
using Store.Shipping.Domain.Commands;
using Store.Shipping.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Shipping.Service.Handlers
{
    public sealed class ShippingCommandHandler : BaseHandlerWithRepo, IShippingCommandHandler, IOrdersEventHandler
    {
        private static readonly string[] carriers = ["Ground Express", "Sky Freight", "QuickShip"];

        /// <summary>
        /// Published by the Orders service when an order ships, and a shipment is created for it.
        /// </summary>
        /// <remarks>
        /// An event, but the service registers it with <see cref="Zerra.CQRS.EventConsumerMode.PerService"/>, so the replicas compete
        /// and one of them creates the shipment. Under the default <see cref="Zerra.CQRS.EventConsumerMode.PerReplica"/> every replica
        /// would create its own shipment with its own carrier and tracking number. That is why this used to be a command.
        /// </remarks>
        public async Task Handle(OrderShippedEvent @event)
        {
            //an event can be delivered twice, a shipment that already exists means there's nothing left to do
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

        public async Task Handle(MarkDeliveredCommand command, CancellationToken cancellationToken)
        {
            var shipment = await Repo.SingleAsync<ShipmentDataModel>(x => x.OrderID == command.OrderID) ?? throw new DomainException("Shipment not found.");
            if (shipment.Status == nameof(ShipmentStatus.Delivered))
                throw new DomainException($"Order {shipment.OrderNumber} was already marked delivered.");

            shipment.Status = nameof(ShipmentStatus.Delivered);
            shipment.DeliveredOn = DateTime.UtcNow;
            //the graph limits the update to the status columns
            await Repo.UpdateAsync(shipment, new Graph<ShipmentDataModel>(x => x.Status, x => x.DeliveredOn));

            Log?.Info($"Order {shipment.OrderNumber} marked delivered");
        }
    }
}
