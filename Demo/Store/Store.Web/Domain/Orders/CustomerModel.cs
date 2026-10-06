namespace Store.Web.Domain.Orders
{
    public sealed class CustomerModel
    {
        public Guid ID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
