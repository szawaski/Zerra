using Store.Common.Data;
using Store.Common.Messaging;
using Store.Wishlists.Domain;
using Store.Wishlists.Domain.Catalog;
using Store.Wishlists.Domain.Models;
using Store.Wishlists.Service.Data;
using Zerra;
using Zerra.Repository;

namespace Store.Wishlists.Service.Handlers
{
    public sealed class WishlistsQueryHandler : BaseHandlerWithRepo, IWishlistsQueryHandler
    {
        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<WishlistItemModel[]> GetWishlist(Guid customerID, CancellationToken cancellationToken)
        {
            var items = await Repo.Query<WishlistItemDataModel>().Where(x => x.CustomerID == customerID).OrderByDescending(x => x.AddedOn).ToArrayAsync(cancellationToken);
            if (items.Length == 0)
                return [];

            //the wishlist keeps only the product's ID, its name and price are the Catalog's current ones
            var products = (await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs(items.Select(x => x.ProductID).ToArray(), cancellationToken)).ToDictionary(x => x.ID);
            return items.Select(item =>
            {
                _ = products.TryGetValue(item.ProductID, out var product);
                return new WishlistItemModel()
                {
                    ProductID = item.ProductID,
                    ProductName = product?.Name ?? "No longer in the catalog",
                    Price = product?.Price ?? 0,
                    IsActive = product?.IsActive ?? false,
                    Note = item.Note,
                    AddedOn = item.AddedOn
                };
            }).ToArray();
        }

        public async Task<WishedProductModel[]> GetMostWished(int count, CancellationToken cancellationToken)
        {
            count = Math.Clamp(count, 1, 20);
            //the store groups the saved products, counts each, and keeps the top ones, so one row per product comes back
            var wished = await Repo.Query<WishlistItemDataModel>()
                .GroupBy(x => x.ProductID)
                .Select(g => new WishedProductModel() { ProductID = g.Key, Customers = g.Count() })
                .OrderByDescending(x => x.Customers)
                .Take(count)
                .ToArrayAsync(cancellationToken);
            if (wished.Length == 0)
                return [];

            var products = (await Bus.Call<ICatalogQueryHandler>().GetProductsByIDs(wished.Select(x => x.ProductID).ToArray(), cancellationToken)).ToDictionary(x => x.ID);
            foreach (var item in wished)
                item.ProductName = products.TryGetValue(item.ProductID, out var product) ? product.Name : "No longer in the catalog";
            //products saved by as many customers are listed by name
            return wished.OrderByDescending(x => x.Customers).ThenBy(x => x.ProductName, StringComparer.Ordinal).ToArray();
        }
    }
}
