using Store.Carts.Domain;
using Store.Catalog.Domain;
using Store.Catalog.Service.Data;
using Store.Catalog.Service.Handlers;
using Store.Common.Data;
using Store.Common.Messaging;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Catalog.Test
{
    /// <summary>
    /// A bus with the Catalog handlers on the service's own store providers, in-memory, and fakes of the services the Catalog sends messages to.
    /// Tests send their commands, events, and queries through <see cref="Bus"/>, the way the gateway and the other services reach the handlers.
    /// </summary>
    public sealed class CatalogTestBus
    {
        private readonly IRepo repo;

        public IBus Bus { get; }
        public FakeCatalogEventHandler CatalogEvents { get; } = new();
        public FakeCartRepricingHandler CartRepricing { get; } = new();

        public CatalogTestBus()
        {
            //the service's store providers skip PostgreSQL and use the in-memory store, the same as the demo's In Memory launch profile
            Environment.SetEnvironmentVariable("STORE_IN_MEMORY", "true");
            //a context of its own is an in-memory store of its own, so no other test sees this one's rows
            var context = new CatalogDataContext();
            var repo = Repo.New();
            repo.AddProvider(new CatalogStoreProvider<CategoryDataModel>(context));
            repo.AddProvider(new CatalogStoreProvider<ProductDataModel>(context));
            this.repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));

            var bus = Zerra.CQRS.Bus.New("Catalog", busServices: busServices);
            Bus = bus;
            bus.AddHandler<ICatalogQueryHandler>(new CatalogQueryHandler());
            bus.AddHandler<ICatalogCommandHandler>(new CatalogCommandHandler());

            //the subscribers, answered in process
            bus.AddHandler<ICatalogEventHandler>(CatalogEvents);
            bus.AddHandler<ICartRepricingHandler>(CartRepricing);
        }

        public async Task<CategoryDataModel> AddCategory(string name)
        {
            var category = new CategoryDataModel() { ID = Guid.NewGuid(), Name = name };
            await repo.CreateAsync(category);
            return category;
        }

        public async Task<ProductDataModel> AddProduct(Guid categoryID, string name, decimal price, ProductStatus status = ProductStatus.Active)
        {
            var product = new ProductDataModel()
            {
                ID = Guid.NewGuid(),
                CategoryID = categoryID,
                Sku = NewSku(),
                Name = name,
                Price = price,
                Status = status.ToString()
            };
            await repo.CreateAsync(product);
            return product;
        }

        /// <summary>A new valid SKU, SKUs are unique across the catalog.</summary>
        public static string NewSku() => $"T-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }
}
