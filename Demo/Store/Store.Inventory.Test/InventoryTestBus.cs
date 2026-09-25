using Store.Common.Data;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Inventory.Service.Data;
using Store.Inventory.Service.Handlers;
using Store.Orders.Domain;
using Zerra.CQRS;
using Zerra.Repository;

namespace Store.Inventory.Test
{
    /// <summary>
    /// A bus with the Inventory handlers on the service's own store providers, in-memory. Inventory calls no other service.
    /// </summary>
    public sealed class InventoryTestBus
    {
        public IRepo Repo { get; }

        public InventoryQueryHandler Queries { get; } = new();
        public InventoryCommandHandler Commands { get; } = new();

        public InventoryTestBus()
        {
            //the service's store providers skip MySQL and use the in-memory store, the same as the demo's In Memory launch profile
            Environment.SetEnvironmentVariable("STORE_IN_MEMORY", "true");
            //a context of its own is an in-memory store of its own, so no other test sees this one's rows
            var context = new InventoryDataContext();
            var repo = Zerra.Repository.Repo.New();
            repo.AddProvider(new InventoryStoreProvider<StockItemDataModel>(context));
            repo.AddProvider(new InventoryStoreProvider<StockReservationDataModel>(context));
            repo.AddProvider(new InventoryStoreProvider<StockMovementDataModel>(context));
            Repo = repo;

            var busServices = new BusServices();
            busServices.AddRepo(repo);
            busServices.AddService<IDataStoreInfo>(new DataStoreInfo("Test data store"));
            busServices.AddService<IMessagingInfo>(new MessagingInfo("Test messaging"));

            var bus = Bus.New("Inventory", busServices: busServices);
            bus.AddHandler<IInventoryQueryHandler>(Queries);
            bus.AddHandler<IInventoryCommandHandler>(Commands);
            bus.AddHandler<IStockReservationHandler>(Commands);
            bus.AddHandler<IOrdersEventHandler>(Commands);
        }

        public async Task<StockItemDataModel?> GetStockItem(Guid productID)
            => await Repo.SingleAsync<StockItemDataModel>(x => x.ProductID == productID);

        public async Task<StockMovementDataModel[]> GetMovements(Guid productID)
            => (await Repo.ManyAsync<StockMovementDataModel>(x => x.ProductID == productID)).OrderBy(x => x.OccurredOn).ToArray();
    }
}
