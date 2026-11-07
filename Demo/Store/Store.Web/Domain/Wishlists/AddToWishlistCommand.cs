using Zerra.CQRS;

namespace Store.Web.Domain.Wishlists
{
    public sealed class AddToWishlistCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
        public string? Note { get; set; }
    }
}
