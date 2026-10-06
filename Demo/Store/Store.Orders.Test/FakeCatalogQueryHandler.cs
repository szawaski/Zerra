using Store.Orders.Domain.Catalog;
using Zerra.CQRS;

namespace Store.Orders.Test
{
    public sealed class FakeCatalogQueryHandler : BaseHandler, ICatalogQueryHandler
    {
        public List<ProductModel> Products { get; } = new();

        public Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
            => Task.FromResult(Products.Where(x => productIDs.Contains(x.ID)).ToArray());
    }
}
