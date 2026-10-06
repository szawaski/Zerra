namespace Store.Web.Domain.Carts
{
    public sealed class CheckoutCartResult
    {
        public Guid OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Total { get; set; }
    }
}
