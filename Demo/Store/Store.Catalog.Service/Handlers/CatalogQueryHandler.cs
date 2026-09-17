using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Catalog.Service.Data;
using Store.Common.Data;
using Store.Common.Messaging;
using Zerra;
using Zerra.Repository;

namespace Store.Catalog.Service.Handlers
{
    public sealed class CatalogQueryHandler : BaseHandlerWithRepo, ICatalogQueryHandler
    {
        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IDataStoreInfo>().Description);
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => Task.FromResult(Context.GetService<IMessagingInfo>().Description);

        public async Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync<CategoryDataModel>(QueryOrder<CategoryDataModel>.Create(x => x.Name));
            return items.Select(x => new CategoryModel() { ID = x.ID, Name = x.Name }).ToArray();
        }

        public async Task<ProductModel[]> GetProducts(CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(QueryOrder<ProductDataModel>.Create(x => x.Name), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken)
        {
            var items = await Repo.ManyAsync(x => x.CategoryID == categoryID, QueryOrder<ProductDataModel>.Create(x => x.Name), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        public async Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
        {
            if (productIDs.Length == 0)
                return [];
            var items = await Repo.ManyAsync(x => productIDs.Contains(x.ID), WithCategory());
            return items.Select(ToModel).ToArray();
        }

        //all product columns plus the related category for its name
        private static Graph<ProductDataModel> WithCategory() => new(true, x => x.Category);

        private static ProductModel ToModel(ProductDataModel item) => new()
        {
            ID = item.ID,
            CategoryID = item.CategoryID,
            CategoryName = item.Category?.Name,
            Sku = item.Sku,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            IsActive = item.Status == nameof(ProductStatus.Active)
        };
    }
}
