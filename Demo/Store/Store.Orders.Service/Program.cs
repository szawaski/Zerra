using Store.Catalog.Domain;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Inventory.Domain;
using Store.Orders.Domain;
using Store.Orders.Service.Data;
using Store.Orders.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;
using Zerra.Web;

Console.Title = "Store - Orders Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Orders service");

//Data store: this service's own database, schema from the data models, then seed data
var dataStore = DataStoreSetup.Prepare<OrdersDataContext>("SQL Server", [typeof(CustomerDataModel), typeof(OrderDataModel), typeof(OrderItemDataModel)], log);

var repo = Repo.New();
repo.AddProvider(new OrdersStoreProvider<CustomerDataModel>());
repo.AddProvider(new OrdersStoreProvider<OrderDataModel>());
repo.AddProvider(new OrdersStoreProvider<OrderItemDataModel>());
await OrdersSeeder.SeedAsync(repo, log);

//Message brokers: each is used when it's running, checked here first so the choice can be reported like the data store
var useKafka = !StoreSettings.DirectMessagingOnly && await KafkaConnection.TestAsync(StoreSettings.KafkaHost, null, null, log: log);
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnection.Test(StoreSettings.RabbitMQHost, log: log);
//Orders only receives commands from the gateway over TCP, its Kafka and RabbitMQ use is outbound
IMessagingInfo messaging = new MessagingInfo("Direct TCP");
log.Info($"Messaging: {messaging.Description}");
log.Info($"Sending stock reservations over {(useKafka ? "Kafka" : "direct TCP")}, order events over {(useRabbitMQ ? "RabbitMQ" : "direct TCP and HTTP")}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

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

//Stock reservations go through Kafka when it's running, otherwise straight to Inventory over TCP. The bus takes one producer per command,
//so the choice is made here at startup, and Inventory makes the same check.
if (useKafka)
    bus.AddCommandProducer<IStockReservationHandler>(new KafkaProducer(StoreSettings.KafkaHost, serializer, encryptor, log, null, null, null));
else
    bus.AddCommandProducer<IStockReservationHandler>(inventoryClient);

//Order events go to Inventory and Shipping. RabbitMQ delivers each event to every subscriber on its own. Without it they're sent directly
//by one producer per subscriber, the bus sends each event to every producer registered for it, to Shipping over HTTP/Kestrel instead of TCP.
if (useRabbitMQ)
{
    bus.AddEventProducer<IOrderEventHandler>(new RabbitMQProducer(StoreSettings.RabbitMQHost, serializer, encryptor, log, null));
}
else
{
    bus.AddEventProducer<IOrderEventHandler>(inventoryClient);
    bus.AddEventProducer<IOrderEventHandler>(new KestrelCqrsClient(StoreSettings.ShippingServiceUrl, serializer, encryptor, log, null, null));
}

log.Info($"Orders service listening on {StoreSettings.OrdersServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
