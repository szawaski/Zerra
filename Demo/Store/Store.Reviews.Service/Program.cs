using Store.Catalog.Domain;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Store.Reviews.Domain;
using Store.Reviews.Service.Data;
using Store.Reviews.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Reviews Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Reviews service");

//Data store: this service's own database, schema from the data models, then seed data
var dataStore = DataStoreSetup.Prepare<ReviewsDataContext>("MariaDB", [typeof(ReviewDataModel)], log);

var repo = Repo.New();
repo.AddProvider(new ReviewsStoreProvider<ReviewDataModel>());
await ReviewsSeeder.SeedAsync(repo, log);

//Message brokers: Azure Service Bus is used when it's running, checked here first so the choice can be reported like the data store
var useServiceBus = !StoreSettings.DirectMessagingOnly && await AzureServiceBusConnection.TestAsync(StoreSettings.AzureServiceBusConnectionString, log: log);
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnection.Test(StoreSettings.RabbitMQHost, log: log);
IMessagingInfo messaging = new MessagingInfo($"Review commands: {(useServiceBus ? "Azure Service Bus" : "Direct TCP")}. Product events: {(useRabbitMQ ? "RabbitMQ" : "Direct TCP")}.");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);
//this instance's own cache of Catalog products, dropped entry by entry when the Catalog publishes a change
busServices.AddService<ICatalogProductCache>(new CatalogProductCache());

//Bus: handle review queries and commands from the gateway
var bus = Bus.New("Reviews", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<IReviewsQueryHandler>(new ReviewsQueryHandler());
bus.AddHandler<IReviewsCommandHandler>(new ReviewsCommandHandler());
bus.AddHandler<ICatalogEventHandler>(new CatalogEventHandler());

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var server = new TcpCqrsServer(StoreSettings.ReviewsServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<IReviewsQueryHandler>(server);

//The gateway sends review commands through Azure Service Bus when it's running, otherwise directly over TCP. The gateway makes the same check,
//so this service listens on whichever route the gateway will use. Queries always come directly over TCP.
if (useServiceBus)
    bus.AddCommandConsumer<IReviewsCommandHandler>(new AzureServiceBusConsumer(StoreSettings.AzureServiceBusConnectionString, serializer, encryptor, log, null));
else
    bus.AddCommandConsumer<IReviewsCommandHandler>(server);

//Catalog publishes its product events through RabbitMQ when it's running, otherwise straight here over TCP. Catalog makes the same check,
//so this service listens on whichever route it will use. Every Reviews replica gets a copy and drops its own cached product.
if (useRabbitMQ)
    bus.AddEventConsumer<ICatalogEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, log, null), EventConsumerMode.PerReplica);
else
    bus.AddEventConsumer<ICatalogEventHandler>(server, EventConsumerMode.PerReplica);

//Downstream services: the product's name from Catalog, purchase history from Orders to mark a review Verified
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);

log.Info($"Reviews service listening on {StoreSettings.ReviewsServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
