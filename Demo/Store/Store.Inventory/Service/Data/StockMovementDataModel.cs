using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    [Entity("StockMovement")]
    public sealed class StockMovementDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid ProductID { get; set; }

        [StoreProperties(true, 16)]
        public string? Kind { get; set; }

        public int Quantity { get; set; }

        [StoreProperties(false, 32)]
        public string? OrderNumber { get; set; }

        //fractional seconds so movements in the same second still sort in order
        [StoreProperties(true, 6)]
        public DateTime OccurredOn { get; set; }
    }
}
