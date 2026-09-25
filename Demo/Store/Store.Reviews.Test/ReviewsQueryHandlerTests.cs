using Store.Reviews.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Reviews.Test
{
    public class ReviewsQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static async Task<ReviewDataModel> AddReview(ReviewsTestBus test, Guid productID, int rating, DateTime createdOn)
        {
            var review = new ReviewDataModel()
            {
                ID = Guid.NewGuid(),
                ProductID = productID,
                ProductName = "Desk Lamp",
                CustomerID = Guid.NewGuid(),
                CustomerName = "Ada Lovelace",
                Rating = rating,
                Comment = $"{rating} stars",
                VerifiedPurchase = rating > 3,
                CreatedOn = createdOn
            };
            await test.Repo.CreateAsync(review);
            return review;
        }

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new ReviewsTestBus();

            Assert.Equal("Test data store", await test.Queries.GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Queries.GetMessagingName(Token));
        }

        [Fact]
        public async Task GetReviewsForProduct_OnlyThatProductNewestFirst()
        {
            var test = new ReviewsTestBus();
            var productID = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var older = await AddReview(test, productID, 2, now.AddDays(-1));
            var newer = await AddReview(test, productID, 5, now);
            _ = await AddReview(test, Guid.NewGuid(), 4, now);

            var reviews = await test.Queries.GetReviewsForProduct(productID, Token);

            Assert.Equal([newer.ID, older.ID], reviews.Select(x => x.ID).ToArray());
            Assert.Equal(5, reviews[0].Rating);
            Assert.Equal("5 stars", reviews[0].Comment);
            Assert.Equal("Ada Lovelace", reviews[0].CustomerName);
            Assert.True(reviews[0].VerifiedPurchase);
        }

        [Fact]
        public async Task GetRecentReviews_NewestFirst()
        {
            var test = new ReviewsTestBus();
            var productID = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var older = await AddReview(test, productID, 2, now.AddSeconds(1));
            var newer = await AddReview(test, productID, 5, now.AddSeconds(2));

            var reviews = await test.Queries.GetRecentReviews(2, Token);

            Assert.Equal([newer.ID, older.ID], reviews.Select(x => x.ID).ToArray());
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1000, 100)]
        public async Task GetRecentReviews_CountIsKeptBetweenOneAndOneHundred(int count, int expected)
        {
            var test = new ReviewsTestBus();
            var now = DateTime.UtcNow;
            for (var i = 0; i < 101; i++)
                _ = await AddReview(test, Guid.NewGuid(), 3, now.AddSeconds(-i));

            var reviews = await test.Queries.GetRecentReviews(count, Token);

            Assert.Equal(expected, reviews.Length);
        }

        [Fact]
        public async Task GetProductRatings_AverageAndCountPerProduct()
        {
            var test = new ReviewsTestBus();
            var lampID = Guid.NewGuid();
            var mouseID = Guid.NewGuid();
            var now = DateTime.UtcNow;
            _ = await AddReview(test, lampID, 5, now);
            _ = await AddReview(test, lampID, 4, now);
            _ = await AddReview(test, lampID, 4, now);
            _ = await AddReview(test, mouseID, 2, now);

            var ratings = await test.Queries.GetProductRatings(Token);

            Assert.Equal(2, ratings.Length);
            var lamp = Assert.Single(ratings, x => x.ProductID == lampID);
            //4.333 rounded to one place
            Assert.Equal(4.3, lamp.AverageRating);
            Assert.Equal(3, lamp.ReviewCount);
            var mouse = Assert.Single(ratings, x => x.ProductID == mouseID);
            Assert.Equal(2.0, mouse.AverageRating);
            Assert.Equal(1, mouse.ReviewCount);
        }
    }
}
