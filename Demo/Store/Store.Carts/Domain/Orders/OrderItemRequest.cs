namespace Store.Carts.Domain.Orders
{
    public sealed class OrderItemRequest
    {
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
