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
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Carts Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Carts service");

//Data store: an event store instead of tables, each cart is a stream of events that the cart aggregate replays
var dataStore = DataStoreSetup.PrepareEventStore<CartsDataContext>("KurrentDB", log, out var eventStore);

//Carts receives queries and commands from the gateway and a reprice command from Catalog over TCP, and Catalog's product events over
//RabbitMQ when it's running. Its checkout command to Orders is outbound.
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnection.Test(StoreSettings.RabbitMQHost, log: log);
IMessagingInfo messaging = new MessagingInfo($"Commands: Direct TCP. Product events: {(useRabbitMQ ? "RabbitMQ" : "Direct TCP")}.");
log.Info($"Messaging: {messaging.Description}");

//the handlers build aggregates on the event store themselves, there's no repo
var busServices = new BusServices();
busServices.AddService<IEventStoreEngine>(eventStore);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);
//this instance's own cache of Catalog products, dropped entry by entry when the Catalog says a product changed
busServices.AddService<ICatalogProductCache>(new CatalogProductCache());

//Bus: handle cart queries and commands from the gateway and the Catalog. The cart's own events go to its stream only, nothing subscribes to them
var bus = Bus.New("Carts", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<ICartsQueryHandler>(new CartsQueryHandler());
var commandHandler = new CartsCommandHandler();
bus.AddHandler<ICartsCommandHandler>(commandHandler);
bus.AddHandler<ICartRepricingHandler>(commandHandler);
bus.AddHandler<ICatalogEventHandler>(new CatalogEventHandler());

await CartsSeeder.SeedAsync(eventStore, log);

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.CartsServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<ICartsQueryHandler>(server);
bus.AddCommandConsumer<ICartsCommandHandler>(server);
//Catalog sends price changes here, over the same TCP server the gateway uses. A command and not an event, so with several Carts
//replicas running one of them reprices the carts instead of all of them repricing the same ones.
bus.AddCommandConsumer<ICartRepricingHandler>(server);
//...and publishes its changes as events, through RabbitMQ when it's running and straight here over TCP when it isn't. Every Carts
//instance gets a copy, which is what dropping a per-instance cache needs: a command would reach one of them and leave the rest stale.
if (useRabbitMQ)
    bus.AddEventConsumer<ICatalogEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, log, null));
else
    bus.AddEventConsumer<ICatalogEventHandler>(server);

//Downstream services: product names and prices from Catalog, the customer list and checkout from Orders
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);
bus.AddCommandProducer<IOrdersCommandHandler>(ordersClient);

log.Info($"Carts service listening on {StoreSettings.CartsServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
