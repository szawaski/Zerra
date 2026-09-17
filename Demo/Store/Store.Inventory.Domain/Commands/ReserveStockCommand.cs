using Store.Inventory.Domain.Models;
using Zerra.CQRS;

namespace Store.Inventory.Domain.Commands
{
    /// <summary>
    /// Reserves stock for every item of an order, or reserves nothing and fails if any item is short.
    /// </summary>
    public sealed class ReserveStockCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
        public required StockReservationItem[] Items { get; set; }
    }
}
