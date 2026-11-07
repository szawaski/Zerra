using Store.Wishlists.Domain.Commands;
using Zerra.CQRS;

namespace Store.Wishlists.Domain
{
    public interface IWishlistsCommandHandler :
        ICommandHandler<AddToWishlistCommand>,
        ICommandHandler<RemoveFromWishlistCommand>
    {
    }
}
