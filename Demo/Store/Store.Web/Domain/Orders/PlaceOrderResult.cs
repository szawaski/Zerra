namespace Store.Web.Domain.Orders
{
    public sealed class PlaceOrderResult
    {
        public Guid OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Total { get; set; }
    }
}
