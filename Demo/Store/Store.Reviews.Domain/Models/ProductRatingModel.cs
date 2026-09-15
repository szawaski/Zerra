namespace Store.Reviews.Domain.Models
{
    /// <summary>
    /// The rating summary for one product, computed from its reviews.
    /// </summary>
    public sealed class ProductRatingModel
    {
        public Guid ProductID { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
