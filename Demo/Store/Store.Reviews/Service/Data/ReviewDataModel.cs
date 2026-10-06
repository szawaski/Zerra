using Zerra.Repository;

namespace Store.Reviews.Service.Data
{
    [Entity("Review")]
    public sealed class ReviewDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid ProductID { get; set; }

        //a snapshot of the name at review time, the same pattern OrderItemDataModel uses for its product name
        [StoreProperties(true, 128)]
        public string? ProductName { get; set; }

        public Guid CustomerID { get; set; }

        [StoreProperties(true, 128)]
        public string? CustomerName { get; set; }

        public int Rating { get; set; }

        [StoreProperties(false, 1000)]
        public string? Comment { get; set; }

        public bool VerifiedPurchase { get; set; }

        [StoreProperties(true, 6)]
        public DateTime CreatedOn { get; set; }
    }
}
