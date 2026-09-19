using Zerra.CQRS;

namespace Store.Inventory.Domain.Commands
{
    /// <summary>
    /// The order was cancelled, so its reserved units go back on the shelf. Sent by the Orders service.
    /// </summary>
    /// <remarks>
    /// A command and not an event, for the same reason as <see cref="ShipReservedStockCommand"/>: releasing stock has to happen once.
    /// </remarks>
    public sealed class ReleaseReservedStockCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
    }
}
