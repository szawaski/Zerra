namespace Store.Carts.Domain.Models
{
    public sealed class CartModel
    {
        public Guid CustomerID { get; set; }
        public CartItemModel[] Items { get; set; } = [];
        public int ItemCount { get; set; }
        public decimal Total { get; set; }

        /// <summary>
        /// The number of the last event in the cart's stream, or null when the cart has no events yet.
        /// </summary>
        public ulong? LastEventNumber { get; set; }
        public DateTime? UpdatedOn { get; set; }

        /// <summary>
        /// The order number of the most recent checkout, if there has been one.
        /// </summary>
        public string? LastOrderNumber { get; set; }
    }
}
