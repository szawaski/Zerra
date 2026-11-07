using Zerra.CQRS;

namespace Store.Web.Domain.Wishlists
{
    public interface IWishlistsCommandHandler :
        ICommandHandler<AddToWishlistCommand>,
        ICommandHandler<RemoveFromWishlistCommand>
    {
    }
}
