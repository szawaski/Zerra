using System.Diagnostics;
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
using Zerra.Repository.Memory;
using Zerra.Repository.KurrentDB;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Carts Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as a failed database read
log.Info("Starting Carts service");

//Data store: an event store instead of tables, each cart is a stream of events that the cart aggregate replays. KurrentDB when it's running, checked here first like the message brokers
var databaseSetup = Stopwatch.StartNew();
var useKurrentDB = !StoreSettings.InMemoryOnly && KurrentDBDataContext.TestConnection(StoreSettings.CartsKurrentDB, true, log: log);
//the in-memory engine keeps its events in the instance, the handlers share this one
IEventStoreEngine eventStore = useKurrentDB ? KurrentDBDataContext.GetEngine(StoreSettings.CartsKurrentDB, true) : MemoryDataContext.GetEngine();
var dataStore = DataStoreSetup.PrepareEventStore(eventStore, "KurrentDB", log);
databaseSetup.Stop();
log.Info($"Database setup done in {databaseSetup.ElapsedMilliseconds} ms");

//Carts receives queries and commands from the gateway and a reprice command from Catalog over TCP, and Catalog's product events over
//RabbitMQ when it's running. Its checkout command to Orders is outbound.
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnectionTest.Test(StoreSettings.RabbitMQHost, log: log);
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

var seeding = Stopwatch.StartNew();
await CartsSeeder.SeedAsync(eventStore, log);
seeding.Stop();
log.Info($"Seed data done in {seeding.ElapsedMilliseconds} ms");

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();
var compressor = StoreSettings.CreateCompressor();

var server = new TcpCqrsServer(StoreSettings.CartsServiceUrl, serializer, encryptor, null, log);
bus.AddQueryServer<ICartsQueryHandler>(server);
bus.AddCommandConsumer<ICartsCommandHandler>(server);
//Catalog sends price changes here, over the same TCP server the gateway uses. A command and not an event, so with several Carts
//replicas running one of them reprices the carts instead of all of them repricing the same ones.
bus.AddCommandConsumer<ICartRepricingHandler>(server);
//...and publishes its changes as events, through RabbitMQ when it's running and straight here over TCP when it isn't. Every Carts
//instance gets a copy, which is what dropping a per-instance cache needs: a command would reach one of them and leave the rest stale.
if (useRabbitMQ)
    bus.AddEventConsumer<ICatalogEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, null, log, null), EventConsumerMode.PerReplica);
else
    bus.AddEventConsumer<ICatalogEventHandler>(server, EventConsumerMode.PerReplica);

//Downstream services: product names and prices from Catalog, the customer list and checkout from Orders
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, compressor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);
bus.AddCommandProducer<IOrdersCommandHandler>(ordersClient);

log.Info($"Carts service listening on {StoreSettings.CartsServiceUrl}, started in {startup.ElapsedMilliseconds} ms ({startup.ElapsedMilliseconds - databaseSetup.ElapsedMilliseconds - seeding.ElapsedMilliseconds} ms excluding database setup and seed data), press Ctrl+C to stop");

await bus.WaitForExitAsync();
