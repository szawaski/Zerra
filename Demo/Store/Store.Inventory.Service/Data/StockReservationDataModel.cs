using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    [Entity("StockReservation")]
    public sealed class StockReservationDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid OrderID { get; set; }

        [StoreProperties(true, 32)]
        public string? OrderNumber { get; set; }

        public Guid ProductID { get; set; }

        public int Quantity { get; set; }
    }
}
