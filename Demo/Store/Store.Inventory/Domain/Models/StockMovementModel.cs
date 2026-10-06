namespace Store.Inventory.Domain.Models
{
    public sealed class StockMovementModel
    {
        public Guid ProductID { get; set; }
        public string? Kind { get; set; }
        public int Quantity { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime OccurredOn { get; set; }
    }
}
