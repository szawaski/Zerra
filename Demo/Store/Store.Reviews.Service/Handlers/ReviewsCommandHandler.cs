using Store.Catalog.Domain;
using Store.Common;
using Store.Orders.Domain;
using Store.Reviews.Domain;
using Store.Reviews.Domain.Commands;
using Store.Reviews.Domain.Models;
using Store.Reviews.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Reviews.Service.Handlers
{
    public sealed class ReviewsCommandHandler : BaseHandlerWithRepo, IReviewsCommandHandler
    {
        public async Task<SubmitReviewResult> Handle(SubmitReviewCommand command, CancellationToken cancellationToken)
        {
            if (command.Rating < 1 || command.Rating > 5)
                throw new DomainException("Rating must be between 1 and 5.");
            var comment = String.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim();
            if (comment is not null && comment.Length > 1000)
                throw new DomainException("Comment must be 1000 characters or less.");

            if (await Repo.AnyAsync<ReviewDataModel>(x => x.CustomerID == command.CustomerID && x.ProductID == command.ProductID))
                throw new DomainException("You've already reviewed this product.");

            //the product's name comes from the Catalog service, same as OrdersCommandHandler does for order lines
            var products = await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([command.ProductID], cancellationToken);
            var product = products.FirstOrDefault() ?? throw new DomainException("Product not found.");

            //asking the Orders service whether this customer actually received the product marks the review Verified, without Reviews needing its own copy of order history
            var verifiedPurchase = await Bus.Call<IOrdersQueryHandler>().HasPurchased(command.CustomerID, command.ProductID, cancellationToken);

            var review = new ReviewDataModel()
            {
                ID = Guid.NewGuid(),
                ProductID = product.ID,
                ProductName = product.Name,
                CustomerID = command.CustomerID,
                Rating = command.Rating,
                Comment = comment,
                VerifiedPurchase = verifiedPurchase,
                CreatedOn = DateTime.UtcNow
            };

            //the customer's name comes from Orders too, Reviews only knows the ID until now
            var customers = await Bus.Call<IOrdersQueryHandler>().GetCustomers(cancellationToken);
            var customer = customers.FirstOrDefault(x => x.ID == command.CustomerID) ?? throw new DomainException("Customer not found.");
            review.CustomerName = customer.Name;

            await Repo.CreateAsync(review);

            Log?.Info($"{customer.Name} rated {product.Name} {command.Rating}/5{(verifiedPurchase ? " (verified purchase)" : "")}");
            return new SubmitReviewResult() { ReviewID = review.ID, VerifiedPurchase = verifiedPurchase };
        }
    }
}
