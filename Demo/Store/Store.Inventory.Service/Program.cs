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

//Message brokers: each is used when it's running, checked here first so the choice can be reported like the data store
var useKafka = !StoreSettings.DirectMessagingOnly && await KafkaConnection.TestAsync(StoreSettings.KafkaHost, null, null, log: log);
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnection.Test(StoreSettings.RabbitMQHost, log: log);
IMessagingInfo messaging = new MessagingInfo($"{(useKafka ? "Kafka" : "Direct TCP")}. Order events: {(useRabbitMQ ? "RabbitMQ" : "Direct TCP")}.{(StoreSettings.DirectMessagingOnly ? " (STORE_DIRECT_MESSAGING=true)" : null)}");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: inventory queries and commands from the gateway, reservations from the Orders service, and the Orders service's events
var bus = Bus.New("Inventory", log, new ConsoleBusLogger(), busServices);
var commandHandler = new InventoryCommandHandler();
bus.AddHandler<IInventoryQueryHandler>(new InventoryQueryHandler());
bus.AddHandler<IInventoryCommandHandler>(commandHandler);
bus.AddHandler<IStockReservationHandler>(commandHandler);
bus.AddHandler<IOrderEventHandler>(new OrderEventHandler());

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.InventoryServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<IInventoryQueryHandler>(server);
bus.AddCommandConsumer<IInventoryCommandHandler>(server);

//Orders sends stock reservations through Kafka and order events through RabbitMQ when they're running, otherwise directly over TCP.
//Orders makes the same checks, so this service listens on whichever route Orders will use.
if (useKafka)
    bus.AddCommandConsumer<IStockReservationHandler>(new KafkaConsumer(StoreSettings.KafkaHost, serializer, encryptor, log, null, null, null));
else
    bus.AddCommandConsumer<IStockReservationHandler>(server);

if (useRabbitMQ)
    bus.AddEventConsumer<IOrderEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, log, null));
else
    bus.AddEventConsumer<IOrderEventHandler>(server);

log.Info($"Inventory service listening on {StoreSettings.InventoryServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
