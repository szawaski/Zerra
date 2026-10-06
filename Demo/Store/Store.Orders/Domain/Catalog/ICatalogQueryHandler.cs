using Zerra.CQRS;

namespace Store.Orders.Domain.Catalog
{
    public interface ICatalogQueryHandler : IQueryHandler
    {
        Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);
    }
}
