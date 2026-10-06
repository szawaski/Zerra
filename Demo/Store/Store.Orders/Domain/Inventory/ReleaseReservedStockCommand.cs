using Zerra.CQRS;

namespace Store.Orders.Domain.Inventory
{
    /// <summary>
    /// The order was cancelled, so its reserved units go back on the shelf. Sent by the Orders service.
    /// </summary>
    /// <remarks>
    /// A command and not an event: releasing stock has to happen once, and Orders knows it wants the reservation undone.
    /// </remarks>
    public sealed class ReleaseReservedStockCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
    }
}
