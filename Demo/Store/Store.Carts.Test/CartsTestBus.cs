using Store.Carts.Domain;
using Store.Carts.Service.Data;
using Store.Carts.Service.Handlers;
using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Zerra.CQRS;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Carts.Test
{
    /// <summary>
    /// A bus with the Carts handlers on their own in-memory event store, and fakes of the Catalog and Orders services they call.
    /// Each test builds a new one, so no test sees another's carts.
    /// Tests send their commands, events, and queries through <see cref="Bus"/>, the way the gateway and the other services reach the handlers.
    /// </summary>
    public sealed class CartsTestBus
    {
        public IBus Bus { get; }
        public FakeCatalogQueryHandler Catalog { get; } = new();
        public FakeOrdersHandler Orders { get; } = new();

        public CartsTestBus()
        {
            var busServices = new BusServices();
            busServices.AddService<IEventStoreEngine>(new MemoryEngine());
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test event store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));
            busServices.AddService<ICatalogProductCache>(new CatalogProductCache());

            var bus = Zerra.CQRS.Bus.New("Carts", busServices: busServices);
            Bus = bus;
            var commands = new CartsCommandHandler();
            bus.AddHandler<ICartsCommandHandler>(commands);
            bus.AddHandler<ICartRepricingHandler>(commands);
            bus.AddHandler<ICartsQueryHandler>(new CartsQueryHandler());
            bus.AddHandler<ICatalogEventHandler>(new CatalogEventHandler());

            //the other services, answered in process
            bus.AddHandler<ICatalogQueryHandler>(Catalog);
            bus.AddHandler<IOrdersQueryHandler>(Orders);
            bus.AddHandler<IOrdersCommandHandler>(Orders);
        }

        public ProductModel AddProduct(string name, decimal price, bool isActive = true)
        {
            var product = new ProductModel() { ID = Guid.NewGuid(), Name = name, Price = price, IsActive = isActive };
            Catalog.Products.Add(product);
            return product;
        }
    }
}
