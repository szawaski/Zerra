namespace Store.Web.Domain.Reviews
{
    public sealed class SubmitReviewResult
    {
        public Guid ReviewID { get; set; }
        public bool VerifiedPurchase { get; set; }
    }
}
