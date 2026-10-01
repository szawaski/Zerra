using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Orders.Domain;
using Store.Orders.Service.Data;
using Store.Orders.Service.Handlers;
using Zerra.CQRS;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Orders.Test
{
    /// <summary>
    /// A bus with the Orders handlers on the service's own store providers, in-memory, and fakes of the Catalog and Inventory services
    /// and the order event subscribers.
    /// Tests send their commands, events, and queries through <see cref="Bus"/>, the way the gateway and the other services reach the handlers.
    /// </summary>
    public sealed class OrdersTestBus
    {
        private readonly FakeCatalogQueryHandler catalog = new();

        public IBus Bus { get; }
        public IRepo Repo { get; }
        public FakeStockReservationHandler Inventory { get; } = new();
        public FakeOrdersEventHandler OrderEvents { get; } = new();

        public OrdersTestBus()
        {
            //an engine of its own is an in-memory store of its own, so no other test sees this one's rows
            var engine = new MemoryEngine();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new OrdersStoreProvider<CustomerDataModel>(engine));
            repo.AddProvider(new OrdersStoreProvider<OrderDataModel>(engine));
            repo.AddProvider(new OrdersStoreProvider<OrderItemDataModel>(engine));
            Repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));

            var bus = Zerra.CQRS.Bus.New("Orders", busServices: busServices);
            Bus = bus;
            bus.AddHandler<IOrdersQueryHandler>(new OrdersQueryHandler());
            bus.AddHandler<IOrdersCommandHandler>(new OrdersCommandHandler());

            //the other services, answered in process
            bus.AddHandler<ICatalogQueryHandler>(catalog);
            bus.AddHandler<IStockReservationHandler>(Inventory);
            bus.AddHandler<IOrdersEventHandler>(OrderEvents);
        }

        public async Task<CustomerDataModel> AddCustomer(string name)
        {
            var customer = new CustomerDataModel() { ID = Guid.NewGuid(), Name = name, Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com" };
            await Repo.CreateAsync(customer);
            return customer;
        }

        public ProductModel AddProduct(string name, decimal price, bool isActive = true)
        {
            var product = new ProductModel() { ID = Guid.NewGuid(), Name = name, Price = price, IsActive = isActive };
            catalog.Products.Add(product);
            return product;
        }

        public async Task<OrderDataModel?> GetOrder(Guid orderID)
            => await Repo.SingleAsync<OrderDataModel>(x => x.ID == orderID);
    }
}
