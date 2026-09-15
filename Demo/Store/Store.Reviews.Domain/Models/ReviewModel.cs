namespace Store.Reviews.Domain.Models
{
    public sealed class ReviewModel
    {
        public Guid ID { get; set; }
        public Guid ProductID { get; set; }
        public string? ProductName { get; set; }
        public Guid CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool VerifiedPurchase { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
