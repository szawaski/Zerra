using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Inventory.Domain;
using Store.Inventory.Service.Data;
using Store.Inventory.Service.Handlers;
using Store.Orders.Domain;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Inventory Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Inventory service");

//Data store: this service's own database, schema from the data models, then seed data
var dataStore = DataStoreSetup.Prepare<InventoryDataContext>("MySQL", [typeof(StockItemDataModel), typeof(StockReservationDataModel), typeof(StockMovementDataModel)], log);

var repo = Repo.New();
repo.AddProvider(new InventoryStoreProvider<StockItemDataModel>());
repo.AddProvider(new InventoryStoreProvider<StockReservationDataModel>());
repo.AddProvider(new InventoryStoreProvider<StockMovementDataModel>());
await InventorySeeder.SeedAsync(repo, log);

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);

//Bus: inventory queries and commands from the gateway, reservations from the Orders service, and the Orders service's events
var bus = Bus.New("Inventory", log, new ConsoleBusLogger(), busServices);
var commandHandler = new InventoryCommandHandler();
bus.AddHandler<IInventoryQueryHandler>(new InventoryQueryHandler());
bus.AddHandler<IInventoryCommandHandler>(commandHandler);
bus.AddHandler<IStockReservationHandler>(commandHandler);
bus.AddHandler<IOrderEventHandler>(new OrderEventHandler());

var server = new TcpCqrsServer(StoreSettings.InventoryServiceUrl, StoreSettings.CreateServiceSerializer(), StoreSettings.CreateServiceEncryptor(), log);
bus.AddQueryServer<IInventoryQueryHandler>(server);
bus.AddCommandConsumer<IInventoryCommandHandler>(server);
bus.AddCommandConsumer<IStockReservationHandler>(server);
bus.AddEventConsumer<IOrderEventHandler>(server);

log.Info($"Inventory service listening on {StoreSettings.InventoryServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
