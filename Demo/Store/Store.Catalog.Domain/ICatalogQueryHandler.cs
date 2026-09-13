using Store.Catalog.Domain.Models;
using Zerra.CQRS;

namespace Store.Catalog.Domain
{
    public interface ICatalogQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);

        Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken);
        Task<ProductModel[]> GetProducts(CancellationToken cancellationToken);
        Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken);
        Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);
    }
}
