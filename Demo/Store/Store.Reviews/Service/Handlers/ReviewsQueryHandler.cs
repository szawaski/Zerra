using Store.Common.Data;
using Store.Common.Messaging;
using Store.Reviews.Domain;
using Store.Reviews.Domain.Models;
using Store.Reviews.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Reviews.Service.Handlers
{
    public sealed class ReviewsQueryHandler : BaseHandlerWithRepo, IReviewsQueryHandler
    {
        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<ReviewModel[]> GetRecentReviews(int count, CancellationToken cancellationToken)
        {
            count = Math.Clamp(count, 1, 100);
            var items = await Repo.Query<ReviewDataModel>().OrderByDescending(x => x.CreatedOn).Skip(0).Take(count).ToArrayAsync(cancellationToken);
            return items.Select(ToModel).ToArray();
        }

        public async Task<ReviewModel[]> GetReviewsForProduct(Guid productID, CancellationToken cancellationToken)
        {
            var items = await Repo.Query<ReviewDataModel>().Where(x => x.ProductID == productID).OrderByDescending(x => x.CreatedOn).ToArrayAsync(cancellationToken);
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductRatingModel[]> GetProductRatings(CancellationToken cancellationToken)
        {
            //the store groups the reviews and averages them, so one row per product comes back instead of every review
            var ratings = await Repo.Query<ReviewDataModel>().GroupBy(x => x.ProductID).Select(g => new ProductRatingModel()
            {
                ProductID = g.Key,
                AverageRating = g.Average(x => x.Rating),
                ReviewCount = g.Count()
            }).ToArrayAsync(cancellationToken);
            foreach (var rating in ratings)
                rating.AverageRating = Math.Round(rating.AverageRating, 1);
            return ratings.ToArray();
        }

        private static ReviewModel ToModel(ReviewDataModel item) => new()
        {
            ID = item.ID,
            ProductID = item.ProductID,
            ProductName = item.ProductName,
            CustomerID = item.CustomerID,
            CustomerName = item.CustomerName,
            Rating = item.Rating,
            Comment = item.Comment,
            VerifiedPurchase = item.VerifiedPurchase,
            CreatedOn = item.CreatedOn
        };
    }
}
