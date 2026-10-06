using Zerra.CQRS;

namespace Store.Reviews.Domain.Catalog
{
    public interface ICatalogQueryHandler : IQueryHandler
    {
        Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);
    }
}
