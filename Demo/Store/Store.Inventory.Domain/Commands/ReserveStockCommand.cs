using Store.Inventory.Domain.Models;
using Zerra.CQRS;

namespace Store.Inventory.Domain.Commands
{
    /// <summary>
    /// Reserves stock for every line of an order, or reserves nothing and fails if any line is short.
    /// </summary>
    public sealed class ReserveStockCommand : ICommand
    {
        public required Guid OrderID { get; set; }
        public required string OrderNumber { get; set; }
        public required StockReservationLine[] Lines { get; set; }
    }
}
