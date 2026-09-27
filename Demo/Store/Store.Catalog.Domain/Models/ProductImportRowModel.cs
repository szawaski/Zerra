namespace Store.Catalog.Domain.Models
{
    public sealed class ProductImportRowModel
    {
        public int Line { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? CurrentPrice { get; set; }
        public ProductImportChange Change { get; set; }
        public string? Error { get; set; }
    }
}
