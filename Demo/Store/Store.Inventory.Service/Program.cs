using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Inventory.Service.Data;
using Store.Inventory.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
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

//Message brokers: each is used when it's running, checked here first so the choice can be reported like the data store
var useKafka = !StoreSettings.DirectMessagingOnly && await KafkaConnection.TestAsync(StoreSettings.KafkaHost, null, null, log: log);
IMessagingInfo messaging = new MessagingInfo($"Stock commands: {(useKafka ? "Kafka" : "Direct TCP")}.");
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

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.InventoryServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<IInventoryQueryHandler>(server);
bus.AddCommandConsumer<IInventoryCommandHandler>(server);

//Orders sends every stock change here as a command on IStockReservationHandler: reserving when an order is placed, and settling
//the reservation when it ships or is cancelled. They are commands and not events because each one moves stock, which has to happen
//once: an event is delivered to every Inventory replica and each one would move the same units again. Kafka gives a command one
//shared consumer group so exactly one replica handles it. Orders makes the same check, so it uses whichever route this service listens on.
if (useKafka)
    bus.AddCommandConsumer<IStockReservationHandler>(new KafkaConsumer(StoreSettings.KafkaHost, serializer, encryptor, log, null, null, null));
else
    bus.AddCommandConsumer<IStockReservationHandler>(server);

log.Info($"Inventory service listening on {StoreSettings.InventoryServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
