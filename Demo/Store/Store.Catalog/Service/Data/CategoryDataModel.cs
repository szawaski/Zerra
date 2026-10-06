using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    [Entity("Category")]
    public sealed class CategoryDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        [StoreProperties(true, 64)]
        public string? Name { get; set; }
    }
}
