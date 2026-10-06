using Zerra.Repository;

namespace Store.Shipping.Service.Data
{
    [Entity("Shipment")]
    public sealed class ShipmentDataModel
    {
        [Identity(false)]
        public Guid ID { get; set; }

        public Guid OrderID { get; set; }

        [StoreProperties(true, 32)]
        public string? OrderNumber { get; set; }

        [StoreProperties(true, 32)]
        public string? Carrier { get; set; }

        [StoreProperties(true, 24)]
        public string? TrackingNumber { get; set; }

        [StoreProperties(true, 16)]
        public string? Status { get; set; }

        public DateTime ShippedOn { get; set; }

        public DateTime? DeliveredOn { get; set; }
    }
}
