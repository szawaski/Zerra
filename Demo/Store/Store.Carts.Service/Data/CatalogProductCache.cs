using System.Collections.Concurrent;
using Store.Catalog.Domain.Models;

namespace Store.Carts.Service.Data
{
    /// <summary>
    /// What this Carts instance remembers about the Catalog's products, so adding to a cart does not query the Catalog service every time.
    /// </summary>
    /// <remarks>
    /// Every replica has its own copy of this, which is why the Catalog sends its changes as events: the bus fans an event out to every
    /// replica, so all of them drop the stale product. A command would reach only one replica and leave the others serving the old price.
    /// </remarks>
    public interface ICatalogProductCache
    {
        bool TryGet(Guid productID, out ProductModel? product);
        void Set(ProductModel product);
        /// <summary>Drops the product so the next cart that wants it reads the Catalog again.</summary>
        bool Drop(Guid productID);
    }

    public sealed class CatalogProductCache : ICatalogProductCache
    {
        private readonly ConcurrentDictionary<Guid, ProductModel> products = new();

        public bool TryGet(Guid productID, out ProductModel? product) => products.TryGetValue(productID, out product);

        public void Set(ProductModel product) => products[product.ID] = product;

        public bool Drop(Guid productID) => products.TryRemove(productID, out _);
    }
}
