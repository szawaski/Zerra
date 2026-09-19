using Store.Common;
using Store.Shipping.Domain;
using Store.Shipping.Domain.Commands;
using Store.Shipping.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Shipping.Service.Handlers
{
    public sealed class ShippingCommandHandler : BaseHandlerWithRepo, IShippingCommandHandler, IShipmentHandler
    {
        private static readonly string[] carriers = ["Ground Express", "Sky Freight", "QuickShip"];

        /// <summary>
        /// Sent by the Orders service when an order ships. A command and not an event: it creates the shipment, which has to happen
        /// once. An event reaches every replica and each would pick its own carrier and tracking number for the same order.
        /// </summary>
        public async Task Handle(CreateShipmentCommand command, CancellationToken cancellationToken)
        {
            //a command can be delivered twice, a shipment that already exists means there's nothing left to do
            if (await Repo.AnyAsync<ShipmentDataModel>(x => x.OrderID == command.OrderID))
                return;

            var shipment = new ShipmentDataModel()
            {
                ID = Guid.NewGuid(),
                OrderID = command.OrderID,
                OrderNumber = command.OrderNumber,
                Carrier = carriers[Random.Shared.Next(carriers.Length)],
                TrackingNumber = $"TRK-{Random.Shared.Next(100000, 1000000)}",
                Status = nameof(ShipmentStatus.InTransit),
                ShippedOn = command.ShippedOn
            };
            await Repo.CreateAsync(shipment);

            Log?.Info($"Created shipment {shipment.TrackingNumber} for order {command.OrderNumber} via {shipment.Carrier}");
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
