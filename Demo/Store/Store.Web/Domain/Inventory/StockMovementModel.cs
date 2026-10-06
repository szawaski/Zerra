namespace Store.Web.Domain.Inventory
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
