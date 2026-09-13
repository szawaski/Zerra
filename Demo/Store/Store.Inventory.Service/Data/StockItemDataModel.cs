using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    [Entity("StockItem")]
    public sealed class StockItemDataModel
    {
        [Identity(false)]
        public Guid ProductID { get; set; }

        public int OnHand { get; set; }

        public int Reserved { get; set; }
    }
}
