namespace Store.Web.Domain.Wishlists
{
    public sealed class WishedProductModel
    {
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public int Customers { get; set; }
    }
}
