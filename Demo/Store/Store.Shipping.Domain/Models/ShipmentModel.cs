namespace Store.Shipping.Domain.Models
{
    public sealed class ShipmentModel
    {
        public Guid OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Status { get; set; }
        public DateTime ShippedOn { get; set; }
        public DateTime? DeliveredOn { get; set; }
    }
}
