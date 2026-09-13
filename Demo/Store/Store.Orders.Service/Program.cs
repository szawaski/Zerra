using Store.Catalog.Domain;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Inventory.Domain;
using Store.Orders.Domain;
using Store.Orders.Service.Data;
using Store.Orders.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Orders Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Orders service");

//Data store: this service's own database, schema from the data models, then seed data
var dataStore = DataStoreSetup.Prepare<OrdersDataContext>("SQL Server", [typeof(CustomerDataModel), typeof(OrderDataModel), typeof(OrderLineDataModel)], log);

var repo = Repo.New();
repo.AddProvider(new OrdersStoreProvider<CustomerDataModel>());
repo.AddProvider(new OrdersStoreProvider<OrderDataModel>());
repo.AddProvider(new OrdersStoreProvider<OrderLineDataModel>());
await OrdersSeeder.SeedAsync(repo, log);

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);

//Bus: handle the order queries and commands from the gateway
var bus = Bus.New("Orders", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<IOrdersQueryHandler>(new OrdersQueryHandler());
bus.AddHandler<IOrdersCommandHandler>(new OrdersCommandHandler());

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<IOrdersQueryHandler>(server);
bus.AddCommandConsumer<IOrdersCommandHandler>(server);

//Downstream services: prices from Catalog, stock reservations and order events to Inventory
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var inventoryClient = new TcpCqrsClient(StoreSettings.InventoryServiceUrl, serializer, encryptor, log);
bus.AddCommandProducer<IStockReservationHandler>(inventoryClient);
bus.AddEventProducer<IOrderEventHandler>(inventoryClient);

log.Info($"Orders service listening on {StoreSettings.OrdersServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
