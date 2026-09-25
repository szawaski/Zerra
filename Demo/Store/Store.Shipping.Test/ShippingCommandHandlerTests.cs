using Store.Common;
using Store.Orders.Domain.Events;
using Store.Shipping.Domain.Commands;
using Store.Shipping.Service.Data;
using Xunit;

namespace Store.Shipping.Test
{
    public class ShippingCommandHandlerTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static OrderShippedEvent Shipped() => new() { OrderID = Guid.NewGuid(), OrderNumber = "SO-TEST-1", ShippedOn = DateTime.UtcNow };

        [Fact]
        public async Task OrderShipped_CreatesAShipmentInTransit()
        {
            var test = new ShippingTestBus();
            var shipped = Shipped();

            await test.Bus.DispatchAsync(shipped);

            var shipment = Assert.Single(await test.GetShipments(shipped.OrderID));
            Assert.Equal("SO-TEST-1", shipment.OrderNumber);
            Assert.Equal(nameof(ShipmentStatus.InTransit), shipment.Status);
            Assert.Equal(shipped.ShippedOn, shipment.ShippedOn);
            Assert.Null(shipment.DeliveredOn);
            Assert.False(String.IsNullOrEmpty(shipment.Carrier));
            Assert.StartsWith("TRK-", shipment.TrackingNumber);
        }

        [Fact]
        public async Task OrderShipped_DeliveredTwice_CreatesOneShipment()
        {
            var test = new ShippingTestBus();
            var shipped = Shipped();

            await test.Bus.DispatchAsync(shipped);
            await test.Bus.DispatchAsync(shipped);

            Assert.Single(await test.GetShipments(shipped.OrderID));
        }

        [Fact]
        public async Task MarkDelivered_MarksTheShipmentDelivered()
        {
            var test = new ShippingTestBus();
            var shipped = Shipped();
            await test.Bus.DispatchAsync(shipped);

            await test.Bus.DispatchAwaitAsync(new MarkDeliveredCommand() { OrderID = shipped.OrderID }, Token);

            var shipment = Assert.Single(await test.GetShipments(shipped.OrderID));
            Assert.Equal(nameof(ShipmentStatus.Delivered), shipment.Status);
            Assert.NotNull(shipment.DeliveredOn);
        }

        [Fact]
        public async Task MarkDelivered_AlreadyDelivered_Throws()
        {
            var test = new ShippingTestBus();
            var shipped = Shipped();
            await test.Bus.DispatchAsync(shipped);
            await test.Bus.DispatchAwaitAsync(new MarkDeliveredCommand() { OrderID = shipped.OrderID }, Token);

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new MarkDeliveredCommand() { OrderID = shipped.OrderID }, Token));
            Assert.Contains("already marked delivered", ex.Message);
        }

        [Fact]
        public async Task MarkDelivered_NoShipment_Throws()
        {
            var test = new ShippingTestBus();

            var ex = await Assert.ThrowsAsync<DomainException>(() => test.Bus.DispatchAwaitAsync(new MarkDeliveredCommand() { OrderID = Guid.NewGuid() }, Token));
            Assert.Equal("Shipment not found.", ex.Message);
        }
    }
}
