using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Zerra.CQRS;

namespace Store.Carts.Test
{
    public sealed class FakeCatalogQueryHandler : BaseHandler, ICatalogQueryHandler
    {
        public List<ProductModel> Products { get; } = new();
        public int GetProductsByIDsCalls { get; private set; }

        public Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
        {
            GetProductsByIDsCalls++;
            return Task.FromResult(Products.Where(x => productIDs.Contains(x.ID)).ToArray());
        }

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProducts(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
