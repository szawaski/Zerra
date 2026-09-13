using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    [Entity("SalesOrder")]
    public sealed class OrderDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        [StoreProperties(true, 32)]
        public string? OrderNumber { get; set; }

        public Guid CustomerID { get; set; }

        [StoreProperties(true, 6)]
        public DateTime PlacedOn { get; set; }

        [StoreProperties(true, 16)]
        public string? Status { get; set; }

        [StoreProperties(false, 6)]
        public DateTime? ClosedOn { get; set; }

        [Relation(nameof(CustomerID))]
        public CustomerDataModel? Customer { get; set; }

        [Relation(nameof(OrderLineDataModel.OrderID))]
        public OrderLineDataModel[]? Lines { get; set; }
    }
}
