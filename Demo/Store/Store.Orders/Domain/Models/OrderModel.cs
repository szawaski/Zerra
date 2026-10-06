namespace Store.Orders.Domain.Models
{
    public sealed class OrderModel
    {
        public Guid ID { get; set; }
        public string? OrderNumber { get; set; }
        public Guid CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public DateTime PlacedOn { get; set; }
        public string? Status { get; set; }
        public decimal Total { get; set; }
        public OrderItemModel[]? Items { get; set; }
    }
}
