using Zerra.CQRS;

namespace Store.Web.Domain.Catalog
{
    public interface ICatalogQueryHandler : IQueryHandler
    {
        Task<string> GetDataStoreName(CancellationToken cancellationToken);
        Task<string> GetMessagingName(CancellationToken cancellationToken);

        Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken);
        Task<ProductModel[]> GetProducts(CancellationToken cancellationToken);
        Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken);
        Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);

        //A Stream result is streamed back to the caller instead of serialized
        Task<Stream> ExportProductsCsv(CancellationToken cancellationToken);
        //A Stream argument can sit among the others, the rest are sent first and the stream follows in the same request, the handler reads it as it arrives
        Task<ProductImportPreviewModel> PreviewProductImport(string fileName, Stream csv, decimal maxPriceChangePercent, CancellationToken cancellationToken);
    }
}
