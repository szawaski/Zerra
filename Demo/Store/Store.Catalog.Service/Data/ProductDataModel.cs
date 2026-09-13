using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    [Entity("Product")]
    public sealed class ProductDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid CategoryID { get; set; }

        [StoreProperties(true, 32)]
        public string? Sku { get; set; }

        [StoreProperties(true, 128)]
        public string? Name { get; set; }

        [StoreProperties(false, 512)]
        public string? Description { get; set; }

        [StoreProperties(true, 18, 2)]
        public decimal Price { get; set; }

        [StoreProperties(true, 16)]
        public string? Status { get; set; }

        [Relation(nameof(CategoryID))]
        public CategoryDataModel? Category { get; set; }
    }
}
