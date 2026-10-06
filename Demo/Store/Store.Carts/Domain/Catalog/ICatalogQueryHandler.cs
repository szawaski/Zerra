using Zerra.CQRS;

namespace Store.Carts.Domain.Catalog
{
    public interface ICatalogQueryHandler : IQueryHandler
    {
        Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);
    }
}
