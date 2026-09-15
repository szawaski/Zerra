using Store.Common;
using Store.Shipping.Domain;
using Store.Shipping.Domain.Commands;
using Store.Shipping.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Shipping.Service.Handlers
{
    public sealed class ShippingCommandHandler : BaseHandlerWithRepo, IShippingCommandHandler
    {
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
