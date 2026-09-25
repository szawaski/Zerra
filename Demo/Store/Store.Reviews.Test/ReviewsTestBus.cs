using Store.Catalog.Domain;
using Store.Catalog.Domain.Models;
using Store.Common.Data;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Store.Orders.Domain.Models;
using Store.Reviews.Domain;
using Store.Reviews.Service.Data;
using Store.Reviews.Service.Handlers;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Reviews.Test
{
    /// <summary>
    /// A bus with the Reviews handlers on the service's own store providers, in-memory, and fakes of the Catalog and Orders services they call.
    /// </summary>
    public sealed class ReviewsTestBus
    {
        public IRepo Repo { get; }
        public CatalogProductCache ProductCache { get; } = new();
        public FakeCatalogQueryHandler Catalog { get; } = new();
        public FakeOrdersQueryHandler Orders { get; } = new();

        public ReviewsQueryHandler Queries { get; } = new();
        public ReviewsCommandHandler Commands { get; } = new();
        public CatalogEventHandler CatalogEvents { get; } = new();

        public ReviewsTestBus()
        {
            //the service's store providers skip MariaDB and use the in-memory store, the same as the demo's In Memory launch profile
            Environment.SetEnvironmentVariable("STORE_IN_MEMORY", "true");
            //a context of its own is an in-memory store of its own, so no other test sees this one's rows
            var context = new ReviewsDataContext();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new ReviewsStoreProvider<ReviewDataModel>(context));
            Repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));
            busServices.AddService<ICatalogProductCache>(ProductCache);

            var bus = Bus.New("Reviews", busServices: busServices);
            bus.AddHandler<IReviewsQueryHandler>(Queries);
            bus.AddHandler<IReviewsCommandHandler>(Commands);
            bus.AddHandler<ICatalogEventHandler>(CatalogEvents);

            //the other services, answered in process
            bus.AddHandler<ICatalogQueryHandler>(Catalog);
            bus.AddHandler<IOrdersQueryHandler>(Orders);
        }

        public ProductModel AddProduct(string name)
        {
            var product = new ProductModel() { ID = Guid.NewGuid(), Name = name, Price = 10.00m, IsActive = true };
            Catalog.Products.Add(product);
            return product;
        }

        public CustomerModel AddCustomer(string name)
        {
            var customer = new CustomerModel() { ID = Guid.NewGuid(), Name = name };
            Orders.Customers.Add(customer);
            return customer;
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

    public sealed class FakeOrdersQueryHandler : BaseHandler, IOrdersQueryHandler
    {
        public List<CustomerModel> Customers { get; } = new();
        /// <summary>The customer and product pairs that were shipped, a verified purchase.</summary>
        public HashSet<(Guid CustomerID, Guid ProductID)> Purchases { get; } = new();

        public Task<CustomerModel[]> GetCustomers(CancellationToken cancellationToken) => Task.FromResult(Customers.ToArray());
        public Task<bool> HasPurchased(Guid customerID, Guid productID, CancellationToken cancellationToken) => Task.FromResult(Purchases.Contains((customerID, productID)));

        public Task<string> GetDataStoreName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetMessagingName(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel[]> GetOrders(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OrderModel> GetOrder(Guid orderID, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
