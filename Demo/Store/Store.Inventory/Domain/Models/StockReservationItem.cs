namespace Store.Inventory.Domain.Models
{
    public sealed class StockReservationItem
    {
        public Guid ProductID { get; set; }
        //only used to word the error when the product is short
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
    }
}
