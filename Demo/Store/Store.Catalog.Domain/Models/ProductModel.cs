namespace Store.Catalog.Domain.Models
{
    public sealed class ProductModel
    {
        public Guid ID { get; set; }
        public Guid CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
