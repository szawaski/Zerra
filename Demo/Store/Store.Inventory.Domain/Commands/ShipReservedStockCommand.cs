using Zerra.CQRS;

namespace Store.Inventory.Domain.Commands
{
    /// <summary>
    /// The order shipped, so its reserved units leave the shelf. Sent by the Orders service.
    /// </summary>
    /// <remarks>
    /// A command and not an event: it moves stock, so it has to happen once. An event would be delivered to every Inventory
    /// replica and each one would take the units off the shelf again.
    /// </remarks>
    public sealed class ShipReservedStockCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
    }
}
