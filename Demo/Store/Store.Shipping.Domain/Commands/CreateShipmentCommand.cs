using Zerra.CQRS;

namespace Store.Shipping.Domain.Commands
{
    /// <summary>
    /// The order shipped, so a shipment is created for it with a carrier and a tracking number. Sent by the Orders service.
    /// </summary>
    /// <remarks>
    /// A command and not an event: it creates a record, so it has to happen once. An event would be delivered to every Shipping
    /// replica and each one would create its own shipment with its own tracking number.
    /// </remarks>
    public sealed class CreateShipmentCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
        public required DateTime ShippedOn { get; set; }
    }
}
