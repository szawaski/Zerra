using Store.Carts.Domain;
using Store.Carts.Service.Data;
using Store.Carts.Service.Handlers;
using Store.Catalog.Domain;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Carts Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Carts service");

//Data store: an event store instead of tables, each cart is a stream of events that the cart aggregate replays
var dataStore = DataStoreSetup.PrepareEventStore<CartsDataContext>("KurrentDB", log, out var eventStore);

//Carts only receives queries and commands from the gateway over TCP, its checkout command to Orders is outbound
IMessagingInfo messaging = new MessagingInfo("Direct TCP");
log.Info($"Messaging: {messaging.Description}");

//the handlers build aggregates on the event store themselves, there's no repo
var busServices = new BusServices();
busServices.AddService<IEventStoreEngine>(eventStore);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle cart queries and commands from the gateway, and the events the cart aggregate dispatches when it appends
var bus = Bus.New("Carts", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<ICartsQueryHandler>(new CartsQueryHandler());
bus.AddHandler<ICartsCommandHandler>(new CartsCommandHandler());
bus.AddHandler<ICartEventHandler>(new CartEventHandler());

await CartsSeeder.SeedAsync(eventStore, log);

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.CartsServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<ICartsQueryHandler>(server);
bus.AddCommandConsumer<ICartsCommandHandler>(server);

//Downstream services: product names and prices from Catalog, checkout places the order in Orders
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddCommandProducer<IOrdersCommandHandler>(ordersClient);

log.Info($"Carts service listening on {StoreSettings.CartsServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
