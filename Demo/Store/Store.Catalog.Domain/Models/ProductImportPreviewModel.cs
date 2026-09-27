namespace Store.Catalog.Domain.Models
{
    public sealed class ProductImportPreviewModel
    {
        public string? FileName { get; set; }
        public int NewCount { get; set; }
        public int PriceChangeCount { get; set; }
        public int UnchangedCount { get; set; }
        public int ErrorCount { get; set; }
        public ProductImportRowModel[]? Rows { get; set; }
    }
}
