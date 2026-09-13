namespace Store.Orders.Domain.Models
{
    public sealed class OrderLineModel
    {
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
