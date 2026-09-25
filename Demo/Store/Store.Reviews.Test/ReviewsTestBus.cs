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
    /// Tests send their commands, events, and queries through <see cref="Bus"/>, the way the gateway and the other services reach the handlers.
    /// </summary>
    public sealed class ReviewsTestBus
    {
        public IBus Bus { get; }
        public IRepo Repo { get; }
        public FakeCatalogQueryHandler Catalog { get; } = new();
        public FakeOrdersQueryHandler Orders { get; } = new();

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
            busServices.AddService<ICatalogProductCache>(new CatalogProductCache());

            var bus = Zerra.CQRS.Bus.New("Reviews", busServices: busServices);
            Bus = bus;
            bus.AddHandler<IReviewsQueryHandler>(new ReviewsQueryHandler());
            bus.AddHandler<IReviewsCommandHandler>(new ReviewsCommandHandler());
            bus.AddHandler<ICatalogEventHandler>(new CatalogEventHandler());

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
}
