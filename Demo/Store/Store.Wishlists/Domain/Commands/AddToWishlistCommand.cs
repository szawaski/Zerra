using Zerra.CQRS;

namespace Store.Wishlists.Domain.Commands
{
    public sealed class AddToWishlistCommand : ICommand
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
        public string? Note { get; set; }
    }
}
