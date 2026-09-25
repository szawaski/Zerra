using Store.Shipping.Service.Data;
using Xunit;
using Zerra.Repository;

namespace Store.Shipping.Test
{
    public class ShippingQueryHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static async Task<ShipmentDataModel> AddShipment(ShippingTestBus test, DateTime shippedOn, ShipmentStatus status = ShipmentStatus.InTransit)
        {
            var shipment = new ShipmentDataModel()
            {
                ID = Guid.NewGuid(),
                OrderID = Guid.NewGuid(),
                OrderNumber = "SO-TEST-1",
                Carrier = "Ground Express",
                TrackingNumber = "TRK-123456",
                Status = status.ToString(),
                ShippedOn = shippedOn,
                DeliveredOn = status == ShipmentStatus.Delivered ? shippedOn.AddDays(2) : null
            };
            await test.Repo.CreateAsync(shipment);
            return shipment;
        }

        [Fact]
        public async Task GetDataStoreAndMessagingNames_ReportTheServices()
        {
            var test = new ShippingTestBus();

            Assert.Equal("Test data store", await test.Queries.GetDataStoreName(Token));
            Assert.Equal("Test messaging", await test.Queries.GetMessagingName(Token));
        }

        [Fact]
        public async Task GetShipments_NewestFirst()
        {
            var test = new ShippingTestBus();
            var now = DateTime.UtcNow;
            var older = await AddShipment(test, now.AddSeconds(1), ShipmentStatus.Delivered);
            var newer = await AddShipment(test, now.AddSeconds(2));

            var shipments = await test.Queries.GetShipments(Token);

            Assert.Equal([newer.OrderID, older.OrderID], shipments.Select(x => x.OrderID).ToArray());
            Assert.Equal("Ground Express", shipments[0].Carrier);
            Assert.Equal("TRK-123456", shipments[0].TrackingNumber);
            Assert.Equal(nameof(ShipmentStatus.InTransit), shipments[0].Status);
            Assert.Equal(nameof(ShipmentStatus.Delivered), shipments[1].Status);
            Assert.NotNull(shipments[1].DeliveredOn);
        }

        [Fact]
        public async Task GetShipments_AtMostFifty()
        {
            var test = new ShippingTestBus();
            var now = DateTime.UtcNow;
            for (var i = 0; i < 51; i++)
                _ = await AddShipment(test, now.AddSeconds(-i));

            var shipments = await test.Queries.GetShipments(Token);

            Assert.Equal(50, shipments.Length);
        }
    }
}
