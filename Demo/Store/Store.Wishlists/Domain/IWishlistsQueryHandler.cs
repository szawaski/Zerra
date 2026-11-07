using Store.Wishlists.Domain.Models;
using Zerra.CQRS;

namespace Store.Wishlists.Domain
{
    public interface IWishlistsQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        /// <summary>
        /// The products a customer saved, newest first, with each product's current name and price from the Catalog.
        /// </summary>
        Task<WishlistItemModel[]> GetWishlist(Guid customerID, CancellationToken cancellationToken);

        /// <summary>
        /// The products on the most wishlists, with how many customers saved each.
        /// </summary>
        Task<WishedProductModel[]> GetMostWished(int count, CancellationToken cancellationToken);
    }
}
