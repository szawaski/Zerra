namespace Store.Web.Domain.Inventory
{
    public sealed class StockLevelModel
    {
        public Guid ProductID { get; set; }
        public int OnHand { get; set; }
        public int Reserved { get; set; }
        public int Available { get; set; }
    }
}
