using Store.Common.Data;
using Store.Common.Messaging;
using Store.Shipping.Domain;
using Store.Shipping.Domain.Orders;
using Store.Shipping.Service.Data;
using Store.Shipping.Service.Handlers;
using Zerra.CQRS;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Shipping.Test
{
    /// <summary>
    /// A bus with the Shipping handlers on the service's own store provider, which is always in-memory. Shipping calls no other service.
    /// Tests send their commands, events, and queries through <see cref="Bus"/>, the way the gateway and the other services reach the handlers.
    /// </summary>
    public sealed class ShippingTestBus
    {
        public IBus Bus { get; }
        public IRepo Repo { get; }

        public ShippingTestBus()
        {
            //an engine of its own is an in-memory store of its own, so no other test sees this one's rows
            var engine = new MemoryEngine();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new ShippingStoreProvider<ShipmentDataModel>(engine));
            Repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));

            var bus = Zerra.CQRS.Bus.New("Shipping", busServices: busServices);
            Bus = bus;
            var commands = new ShippingCommandHandler();
            bus.AddHandler<IShippingQueryHandler>(new ShippingQueryHandler());
            bus.AddHandler<IShippingCommandHandler>(commands);
            bus.AddHandler<IOrdersEventHandler>(commands);
        }

        public async Task<ShipmentDataModel[]> GetShipments(Guid orderID)
            => (await Repo.ManyAsync<ShipmentDataModel>(x => x.OrderID == orderID)).ToArray();
    }
}
