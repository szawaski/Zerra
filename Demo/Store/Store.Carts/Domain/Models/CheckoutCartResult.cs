namespace Store.Carts.Domain.Models
{
    public sealed class CheckoutCartResult
    {
        public Guid OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Total { get; set; }
    }
}
