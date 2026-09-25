using Store.Carts.Domain;
using Store.Carts.Service.Data;
using Store.Carts.Service.Handlers;
using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Store.Orders.Domain.Commands;
using Store.Orders.Domain.Models;
using Zerra.CQRS;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Carts.Test
{
    /// <summary>
    /// A bus with the Carts handlers on their own in-memory event store, and fakes of the Catalog and Orders services they call.
    /// Each test builds a new one, so no test sees another's carts.
    /// </summary>
    public sealed class CartsTestBus
    {
        public IEventStoreEngine EventStore { get; } = new MemoryEngine();
        public CatalogProductCache ProductCache { get; } = new();
        public FakeCatalogQueryHandler Catalog { get; } = new();
        public FakeOrdersHandler Orders { get; } = new();

        public CartsCommandHandler Commands { get; } = new();
        public CartsQueryHandler Queries { get; } = new();
        public CatalogEventHandler CatalogEvents { get; } = new();

        public CartsTestBus()
        {
            var busServices = new BusServices();
            busServices.AddService<IEventStoreEngine>(EventStore);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test event store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));
            busServices.AddService<ICatalogProductCache>(ProductCache);

            var bus = Bus.New("Carts", busServices: busServices);
            bus.AddHandler<ICartsCommandHandler>(Commands);
            bus.AddHandler<ICartRepricingHandler>(Commands);
            bus.AddHandler<ICartsQueryHandler>(Queries);
            bus.AddHandler<ICatalogEventHandler>(CatalogEvents);

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

    public sealed class FakeCatalogQueryHandler : BaseHandler, ICatalogQueryHandler
    {
        public List<ProductModel> Products { get; } = new();
        public int GetProductsByIDsCalls { get; private set; }

        public Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken)
        {
            GetProductsByIDsCalls++;
            return Task.FromResult(Products.Where(x => productIDs.Contains(x.ID)).ToArray());
        }

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CategoryModel[]> GetCategories(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProducts(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductModel[]> GetProductsByCategory(Guid categoryID, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public sealed class FakeOrdersHandler : BaseHandler, IOrdersQueryHandler, IOrdersCommandHandler
    {
        public List<CustomerModel> Customers { get; } = new();
        public List<PlaceOrderCommand> PlacedOrders { get; } = new();
        /// <summary>When set, placing an order fails with this, the way Orders refuses an order it can't reserve stock for.</summary>
        public Exception? PlaceOrderFailure { get; set; }

        public Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken) => Task.FromResult(Customers.ToArray());

        public Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
        {
            if (PlaceOrderFailure is not null)
                return Task.FromException<PlaceOrderResult>(PlaceOrderFailure);
            PlacedOrders.Add(command);
            return Task.FromResult(new PlaceOrderResult() { OrderID = Guid.NewGuid(), OrderNumber = $"SO-TEST-{PlacedOrders.Count}", Total = 100m * PlacedOrders.Count });
        }

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel[]> GetOrders(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task Handle(CancelOrderCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task Handle(ShipOrderCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
