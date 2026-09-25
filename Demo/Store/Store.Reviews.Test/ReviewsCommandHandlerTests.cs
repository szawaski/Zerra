using Store.Catalog.Domain.Events;
using Store.Common;
using Store.Reviews.Domain.Commands;
using Store.Reviews.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Reviews.Test
{
    public class ReviewsCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task SubmitReview_SavesTheReviewWithTheProductAndCustomerNames()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");

            var result = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 4, Comment = "  Bright enough.  " }, Token);

            Assert.False(result.VerifiedPurchase);
            var review = await test.Repo.SingleAsync<ReviewDataModel>(x => x.ID == result.ReviewID);
            Assert.NotNull(review);
            Assert.Equal(lamp.ID, review.ProductID);
            Assert.Equal("Desk Lamp", review.ProductName);
            Assert.Equal(ada.ID, review.CustomerID);
            Assert.Equal("Ada Lovelace", review.CustomerName);
            Assert.Equal(4, review.Rating);
            Assert.Equal("Bright enough.", review.Comment);
        }

        [Fact]
        public async Task SubmitReview_CustomerReceivedTheProduct_IsVerified()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");
            _ = test.Orders.Purchases.Add((ada.ID, lamp.ID));

            var result = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 5 }, Token);

            Assert.True(result.VerifiedPurchase);
            Assert.True((await test.Repo.SingleAsync<ReviewDataModel>(x => x.ID == result.ReviewID))!.VerifiedPurchase);
        }

        [Fact]
        public async Task SubmitReview_BlankComment_IsNotStored()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");

            var result = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 3, Comment = "   " }, Token);

            Assert.Null((await test.Repo.SingleAsync<ReviewDataModel>(x => x.ID == result.ReviewID))!.Comment);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task SubmitReview_RatingOutOfRange_Throws(int rating)
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = rating }, Token));
        }

        [Fact]
        public async Task SubmitReview_CommentTooLong_Throws()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");

            _ = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 3, Comment = new string('a', 1001) }, Token));
        }

        [Fact]
        public async Task SubmitReview_SecondReviewOfTheSameProduct_Throws()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");
            _ = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 4 }, Token);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 1 }, Token));
            Assert.Contains("already reviewed", ex.Message);
            Assert.Single(await test.Repo.ManyAsync<ReviewDataModel>(x => x.ProductID == lamp.ID));
        }

        [Fact]
        public async Task SubmitReview_ProductNotInCatalog_Throws()
        {
            var test = new ReviewsTestBus();
            var ada = test.AddCustomer("Ada Lovelace");

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = Guid.NewGuid(), Rating = 4 }, Token));
            Assert.Equal("Product not found.", ex.Message);
        }

        [Fact]
        public async Task SubmitReview_CustomerNotFound_Throws()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Commands.Handle(new SubmitReviewCommand() { CustomerID = Guid.NewGuid(), ProductID = lamp.ID, Rating = 4 }, Token));
            Assert.Equal("Customer not found.", ex.Message);
            Assert.Empty(await test.Repo.ManyAsync<ReviewDataModel>(x => x.ProductID == lamp.ID));
        }

        [Fact]
        public async Task SubmitReview_CachesTheProductUntilTheCatalogSaysItChanged()
        {
            var test = new ReviewsTestBus();
            var lamp = test.AddProduct("Desk Lamp");
            var ada = test.AddCustomer("Ada Lovelace");
            var grace = test.AddCustomer("Grace Hopper");
            var alan = test.AddCustomer("Alan Turing");

            _ = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = ada.ID, ProductID = lamp.ID, Rating = 4 }, Token);
            _ = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = grace.ID, ProductID = lamp.ID, Rating = 5 }, Token);
            Assert.Equal(1, test.Catalog.GetProductsByIDsCalls);

            lamp.Name = "LED Desk Lamp";
            await test.CatalogEvents.Handle(new ProductDiscontinuedEvent() { ProductID = lamp.ID, Name = "LED Desk Lamp" });
            var result = await test.Commands.Handle(new SubmitReviewCommand() { CustomerID = alan.ID, ProductID = lamp.ID, Rating = 3 }, Token);

            Assert.Equal(2, test.Catalog.GetProductsByIDsCalls);
            Assert.Equal("LED Desk Lamp", (await test.Repo.SingleAsync<ReviewDataModel>(x => x.ID == result.ReviewID))!.ProductName);
        }
    }
}
