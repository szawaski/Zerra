using Store.Carts.Domain.Catalog;
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
    }
}
