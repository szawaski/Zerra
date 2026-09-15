using Store.Reviews.Domain.Models;
using Zerra.CQRS;

namespace Store.Reviews.Domain
{
    public interface IReviewsQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);

        Task<ReviewModel[]> GetRecentReviews(int count, CancellationToken cancellationToken);
        Task<ReviewModel[]> GetReviewsForProduct(Guid productID, CancellationToken cancellationToken);

        /// <summary>
        /// The average rating and review count for every product that has at least one review.
        /// Computed here so pages like the Catalog can show a rating without recomputing it themselves.
        /// </summary>
        Task<ProductRatingModel[]> GetProductRatings(CancellationToken cancellationToken);
    }
}
