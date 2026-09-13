namespace Store.Orders.Domain.Models
{
    public sealed class OrderItemRequest
    {
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
