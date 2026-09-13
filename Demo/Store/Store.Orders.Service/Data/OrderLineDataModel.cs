using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    [Entity("SalesOrderLine")]
    public sealed class OrderLineDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid OrderID { get; set; }

        public Guid ProductID { get; set; }

        [StoreProperties(true, 128)]
        public string? ProductName { get; set; }

        [StoreProperties(true, 18, 2)]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }
}
