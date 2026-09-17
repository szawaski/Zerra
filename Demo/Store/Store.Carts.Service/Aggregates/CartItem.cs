namespace Store.Carts.Service.Aggregates
{
    public sealed class CartItem
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
