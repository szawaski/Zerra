using Zerra.Repository;

namespace Store.Wishlists.Service.Data
{
    //a product a customer saved, one document in MongoDB; the product's name and price stay in the Catalog and are read from it with the wishlist
    [Entity("WishlistItem")]
    public sealed class WishlistItemDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        [StoreIndex("CustomerProduct", Unique = true)]
        public Guid CustomerID { get; set; }

        [StoreIndex("CustomerProduct", Unique = true, Order = 1)]
        public Guid ProductID { get; set; }

        [StoreProperties(false, 200)]
        public string? Note { get; set; }

        [StoreProperties(true, 6)]
        public DateTime AddedOn { get; set; }
    }
}
