using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    [Entity("SalesOrderItem")]
    public sealed class OrderItemDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        [StoreIndex("OrderProduct")]
        public Guid OrderID { get; set; }

        [StoreIndex("OrderProduct", Order = 1)]
        public Guid ProductID { get; set; }

        [StoreProperties(true, 128)]
        public string? ProductName { get; set; }

        [StoreProperties(true, 18, 2)]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }
}
