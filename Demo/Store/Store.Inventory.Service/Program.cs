using System.Diagnostics;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Inventory.Service.Data;
using Store.Inventory.Service.Handlers;
using Store.Orders.Domain;
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.MySql;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Inventory Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as a failed database read
log.Info("Starting Inventory service");

//Data store: this service's own database when it's running, checked here first like the message brokers, schema from the data models, then seed data
var databaseSetup = Stopwatch.StartNew();
var useMySql = !StoreSettings.InMemoryOnly && MySqlDataContext.TestConnection(StoreSettings.InventoryMySql, log);
ITransactStoreEngine engine = useMySql ? MySqlDataContext.GetEngine(StoreSettings.InventoryMySql) : MemoryDataContext.GetEngine();
var dataStore = DataStoreSetup.Prepare(engine, "MySQL", [typeof(StockItemDataModel), typeof(StockReservationDataModel), typeof(StockMovementDataModel)], log);
databaseSetup.Stop();
log.Info($"Database setup done in {databaseSetup.ElapsedMilliseconds} ms");

var repo = Repo.New();
repo.AddProvider(new InventoryStoreProvider<StockItemDataModel>(engine));
repo.AddProvider(new InventoryStoreProvider<StockReservationDataModel>(engine));
repo.AddProvider(new InventoryStoreProvider<StockMovementDataModel>(engine));
var seeding = Stopwatch.StartNew();
await InventorySeeder.SeedAsync(repo, log);
seeding.Stop();
log.Info($"Seed data done in {seeding.ElapsedMilliseconds} ms");

//Message brokers: each is used when it's running, checked here first so the choice can be reported like the data store
var useKafka = !StoreSettings.DirectMessagingOnly && await KafkaConnectionTest.TestAsync(StoreSettings.KafkaHost, null, null, log: log);
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnectionTest.Test(StoreSettings.RabbitMQHost, log: log);
IMessagingInfo messaging = new MessagingInfo($"Stock commands: {(useKafka ? "Kafka" : "Direct TCP")}. Order events: {(useRabbitMQ ? "RabbitMQ" : "Direct TCP")}.");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: inventory queries and commands from the gateway, and the stock reservation and settlement commands from the Orders service
var bus = Bus.New("Inventory", log, new ConsoleBusLogger(), busServices);
var commandHandler = new InventoryCommandHandler();
bus.AddHandler<IInventoryQueryHandler>(new InventoryQueryHandler());
bus.AddHandler<IInventoryCommandHandler>(commandHandler);
bus.AddHandler<IStockReservationHandler>(commandHandler);
bus.AddHandler<IOrdersEventHandler>(commandHandler);

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.InventoryServiceUrl, serializer, encryptor, null, log);
bus.AddQueryServer<IInventoryQueryHandler>(server);
bus.AddCommandConsumer<IInventoryCommandHandler>(server);

//Orders sends every stock change here as a command on IStockReservationHandler: reserving when an order is placed, and settling
//the reservation when it ships or is cancelled. They are commands and not events because each one moves stock, which has to happen
//once: an event is delivered to every Inventory replica and each one would move the same units again. Kafka gives a command one
//shared consumer group so exactly one replica handles it. Orders makes the same check, so it uses whichever route this service listens on.
if (useKafka)
    bus.AddCommandConsumer<IStockReservationHandler>(new KafkaConsumer(StoreSettings.KafkaHost, serializer, encryptor, null, log, null, null, null));
else
    bus.AddCommandConsumer<IStockReservationHandler>(server);

//Shipping an order arrives as OrderShippedEvent instead, and this service settles the reservation for it. EventConsumerMode.PerService,
//not the default PerReplica: the units leave the shelf once, so the replicas of this service have to compete for the event rather than
//each get a copy. On RabbitMQ that is one queue named for this service on the Fanout exchange, shared by the replicas; the Shipping
//service has its own queue on the same exchange and still gets every event.
if (useRabbitMQ)
    bus.AddEventConsumer<IOrdersEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, null, log, null), EventConsumerMode.PerService);
else
    bus.AddEventConsumer<IOrdersEventHandler>(server, EventConsumerMode.PerService);

log.Info($"Inventory service listening on {StoreSettings.InventoryServiceUrl}, started in {startup.ElapsedMilliseconds} ms ({startup.ElapsedMilliseconds - databaseSetup.ElapsedMilliseconds - seeding.ElapsedMilliseconds} ms excluding database setup and seed data), press Ctrl+C to stop");

await bus.WaitForExitAsync();
