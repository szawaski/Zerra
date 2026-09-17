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
        private const int recentReviewCount = 50;

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<ReviewModel[]> GetRecentReviews(int count, CancellationToken cancellationToken)
        {
            count = Math.Clamp(count, 1, 100);
            var items = await Repo.ManyAsync(QueryOrder<ReviewDataModel>.Create(x => x.CreatedOn, true), 0, count);
            return items.Select(ToModel).ToArray();
        }

        public async Task<ReviewModel[]> GetReviewsForProduct(Guid productID, CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(x => x.ProductID == productID, QueryOrder<ReviewDataModel>.Create(x => x.CreatedOn, true));
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductRatingModel[]> GetProductRatings(CancellationToken cancellationToken)
        {
            //a handful of reviews at a time, computing the average here keeps every page that wants a rating from redoing it
            var items = await Repo.ManyAsync<ReviewDataModel>(0, recentReviewCount * 20);
            return items
                .GroupBy(x => x.ProductID)
                .Select(g => new ProductRatingModel()
                {
                    ProductID = g.Key,
                    AverageRating = Math.Round(g.Average(x => x.Rating), 1),
                    ReviewCount = g.Count()
                })
                .ToArray();
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
