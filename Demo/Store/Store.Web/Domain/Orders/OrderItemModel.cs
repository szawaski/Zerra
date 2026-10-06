namespace Store.Web.Domain.Orders
{
    public sealed class OrderItemModel
    {
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
