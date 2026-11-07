using Store.Common;
using Store.Wishlists.Domain;
using Store.Wishlists.Domain.Catalog;
using Store.Wishlists.Domain.Commands;
using Store.Wishlists.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Wishlists.Service.Handlers
{
    public sealed class WishlistsCommandHandler : BaseHandlerWithRepo, IWishlistsCommandHandler
    {
        public async Task Handle(AddToWishlistCommand command, CancellationToken cancellationToken)
        {
            var note = String.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
            if (note is not null && note.Length > 200)
                throw new DomainException("Note must be 200 characters or less.");

            if (await Repo.Query<WishlistItemDataModel>().AnyAsync(x => x.CustomerID == command.CustomerID && x.ProductID == command.ProductID, cancellationToken))
                throw new DomainException("That product is already on the wishlist.");

            var product = (await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs([command.ProductID], cancellationToken)).FirstOrDefault() ?? throw new DomainException("Product not found.");
            if (!product.IsActive)
                throw new DomainException("Discontinued products can't be added to a wishlist.");

            await Repo.CreateAsync(new WishlistItemDataModel()
            {
                ID = Guid.NewGuid(),
                CustomerID = command.CustomerID,
                ProductID = command.ProductID,
                Note = note,
                AddedOn = DateTime.UtcNow
            });

            Log?.Info($"{product.Name} saved to a wishlist");
        }

        public async Task Handle(RemoveFromWishlistCommand command, CancellationToken cancellationToken)
        {
            //removed by condition in the store, without reading the document first
            var removed = await Repo.DeleteAsync<WishlistItemDataModel>(x => x.CustomerID == command.CustomerID && x.ProductID == command.ProductID, cancellationToken);
            if (removed == 0)
                throw new DomainException("That product isn't on the wishlist.");
        }
    }
}
