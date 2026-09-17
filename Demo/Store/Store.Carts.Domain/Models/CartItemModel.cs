namespace Store.Carts.Domain.Models
{
    public sealed class CartItemModel
    {
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
