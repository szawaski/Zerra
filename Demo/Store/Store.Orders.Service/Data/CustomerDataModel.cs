using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    [Entity("Customer")]
    public sealed class CustomerDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        [StoreProperties(true, 128)]
        public string? Name { get; set; }

        [StoreProperties(true, 256)]
        public string? Email { get; set; }
    }
}
