namespace Store.Carts.Domain.Models
{
    /// <summary>
    /// One event from a cart's stream and the cart as it stood right after it.
    /// </summary>
    public sealed class CartHistoryModel
    {
        public ulong EventNumber { get; set; }
        public string? EventName { get; set; }
        public DateTime OccurredOn { get; set; }
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
    }
}
