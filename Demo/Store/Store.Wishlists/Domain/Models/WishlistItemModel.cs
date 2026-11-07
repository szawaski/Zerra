namespace Store.Wishlists.Domain.Models
{
    public sealed class WishlistItemModel
    {
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public string? Note { get; set; }
        public DateTime AddedOn { get; set; }
    }
}
