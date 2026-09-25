using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Inventory.Domain.Commands;
using Store.Orders.Domain;
using Store.Orders.Domain.Events;
using Store.Orders.Service.Data;
using Store.Orders.Service.Handlers;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Orders.Test
{
    /// <summary>
    /// A bus with the Orders handlers on the service's own store providers, in-memory, and fakes of the Catalog and Inventory services
    /// and the order event subscribers.
    /// </summary>
    public sealed class OrdersTestBus
    {
        public IRepo Repo { get; }
        public FakeCatalogQueryHandler Catalog { get; } = new();
        public FakeStockReservationHandler Inventory { get; } = new();
        public FakeOrdersEventHandler OrderEvents { get; } = new();

        public OrdersQueryHandler Queries { get; } = new();
        public OrdersCommandHandler Commands { get; } = new();

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

            var bus = Bus.New("Orders", busServices: busServices);
            bus.AddHandler<IOrdersQueryHandler>(Queries);
            bus.AddHandler<IOrdersCommandHandler>(Commands);

            //the other services, answered in process
            bus.AddHandler<ICatalogQueryHandler>(Catalog);
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
            Catalog.Products.Add(product);
            return product;
        }

        public async Task<OrderDataModel?> GetOrder(Guid orderID)
            => await Repo.SingleAsync<OrderDataModel>(x => x.ID == orderID);
    }

    public sealed class FakeCatalogQueryHandler : BaseHandler, ICatalogQueryHandler
    {
        public List<ProductModel> Products { get; } = new();

        public Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
            => Task.FromResult(Products.Where(x => productIDs.Contains(x.ID)).ToArray());

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProducts(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public sealed class FakeStockReservationHandler : BaseHandler, IStockReservationHandler
    {
        public List<ReserveStockCommand> Reserved { get; } = new();
        public List<ReleaseReservedStockCommand> Released { get; } = new();
        /// <summary>When set, reserving fails with this, the way Inventory refuses an order it's short for.</summary>
        public Exception? ReserveFailure { get; set; }

        public Task Handle(ReserveStockCommand command, CancellationToken cancellationToken)
        {
            if (ReserveFailure is not null)
                return Task.FromException(ReserveFailure);
            Reserved.Add(command);
            return Task.CompletedTask;
        }

        public Task Handle(ReleaseReservedStockCommand command, CancellationToken cancellationToken)
        {
            Released.Add(command);
            return Task.CompletedTask;
        }
    }

    public sealed class FakeOrdersEventHandler : BaseHandler, IOrdersEventHandler
    {
        public List<OrderShippedEvent> Shipped { get; } = new();

        public Task Handle(OrderShippedEvent @event)
        {
            Shipped.Add(@event);
            return Task.CompletedTask;
        }
    }
}
