namespace Store.Reviews.Domain.Models
{
    public sealed class SubmitReviewResult
    {
        public Guid ReviewID { get; set; }
        public bool VerifiedPurchase { get; set; }
    }
}
