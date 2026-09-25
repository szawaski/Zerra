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
            //the service's store providers skip SQL Server and use the in-memory store, the same as the demo's In Memory launch profile
            Environment.SetEnvironmentVariable("STORE_IN_MEMORY", "true");
            //a context of its own is an in-memory store of its own, so no other test sees this one's rows
            var context = new OrdersDataContext();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new OrdersStoreProvider<CustomerDataModel>(context));
            repo.AddProvider(new OrdersStoreProvider<OrderDataModel>(context));
            repo.AddProvider(new OrdersStoreProvider<OrderItemDataModel>(context));
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
