using Store.Common.Data;
using Store.Common.Messaging;
using Store.Wishlists.Domain;
using Store.Wishlists.Domain.Catalog;
using Store.Wishlists.Service.Data;
using Store.Wishlists.Service.Handlers;
using Zerra.CQRS;
using Zerra.Repository;
using Zerra.Repository.Memory;

namespace Store.Wishlists.Test
{
    /// <summary>
    /// A bus with the Wishlists handlers on the service's own store provider, in-memory as MongoDB's fallback is, and a fake of the Catalog service they call.
    /// Tests send their commands and queries through <see cref="Bus"/>, the way the gateway reaches the handlers.
    /// </summary>
    public sealed class WishlistsTestBus
    {
        public IBus Bus { get; }
        public IRepo Repo { get; }
        public FakeCatalogQueryHandler Catalog { get; } = new();

        public WishlistsTestBus()
        {
            //an engine of its own is an in-memory store of its own, so no other test sees this one's documents
            var engine = new MemoryEngine();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new TransactStoreProvider<WishlistItemDataModel>(engine));
            Repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));

            var bus = Zerra.CQRS.Bus.New("Wishlists", busServices: busServices);
            Bus = bus;
            bus.AddHandler<IWishlistsQueryHandler>(new WishlistsQueryHandler());
            bus.AddHandler<IWishlistsCommandHandler>(new WishlistsCommandHandler());

            //the Catalog service, answered in process
            bus.AddHandler<ICatalogQueryHandler>(Catalog);
        }

        public ProductModel AddProduct(string name, decimal price = 10.00m, bool isActive = true)
        {
            var product = new ProductModel() { ID = Guid.NewGuid(), Name = name, Price = price, IsActive = isActive };
            Catalog.Products.Add(product);
            return product;
        }
    }
}
