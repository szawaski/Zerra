using System.Diagnostics;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Reviews.Domain;
using Store.Reviews.Domain.Catalog;
using Store.Reviews.Domain.Orders;
using Store.Reviews.Service.Data;
using Store.Reviews.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.MariaDb;
using Zerra.Repository.Memory;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Reviews Service";
ILogger log = new ConsoleLogger();
log.Info("Starting Reviews service");

//Data store: this service's own database when it's running, checked here first like the message brokers, schema from the data models, then seed data
var databaseSetup = Stopwatch.StartNew();
var useMariaDb = !StoreSettings.InMemoryOnly && MariaDbConnectionTest.Test(StoreSettings.ReviewsMariaDb, log);
ITransactStoreEngine engine = useMariaDb ? new MariaDbEngine(StoreSettings.ReviewsMariaDb) : new MemoryEngine();
var dataStore = DataStoreSetup.Prepare(engine, "MariaDB", [typeof(ReviewDataModel)], log);
databaseSetup.Stop();
log.Info($"Database setup done in {databaseSetup.ElapsedMilliseconds} ms");

var repo = Repo.New();
repo.AddProvider(new ReviewsStoreProvider<ReviewDataModel>(engine));
var seeding = Stopwatch.StartNew();
await ReviewsSeeder.SeedAsync(repo, log);
seeding.Stop();
log.Info($"Seed data done in {seeding.ElapsedMilliseconds} ms");

//Message brokers: Azure Service Bus is used when it's running, checked here first so the choice can be reported like the data store
var useServiceBus = !StoreSettings.DirectMessagingOnly && await AzureServiceBusConnectionTest.TestAsync(StoreSettings.AzureServiceBusConnectionString, log: log);
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && await RabbitMQConnectionTest.TestAsync(StoreSettings.RabbitMQHost, log: log);
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
var compressor = StoreSettings.CreateCompressor();

var server = new TcpCqrsServer(StoreSettings.ReviewsServiceUrl, serializer, encryptor, null, log);
bus.AddQueryServer<IReviewsQueryHandler>(server);

//The gateway sends review commands through Azure Service Bus when it's running, otherwise directly over TCP. The gateway makes the same check,
//so this service listens on whichever route the gateway will use. Queries always come directly over TCP.
if (useServiceBus)
    bus.AddCommandConsumer<IReviewsCommandHandler>(new AzureServiceBusConsumer(StoreSettings.AzureServiceBusConnectionString, serializer, encryptor, null, log, null));
else
    bus.AddCommandConsumer<IReviewsCommandHandler>(server);

//Catalog publishes its product events through RabbitMQ when it's running, otherwise straight here over TCP. Catalog makes the same check,
//so this service listens on whichever route it will use. Every Reviews replica gets a copy and drops its own cached product.
if (useRabbitMQ)
    bus.AddEventConsumer<ICatalogEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, null, log, null), EventConsumerMode.PerReplica);
else
    bus.AddEventConsumer<ICatalogEventHandler>(server, EventConsumerMode.PerReplica);

//Downstream services: the product's name from Catalog, purchase history from Orders to mark a review Verified
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, compressor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);

log.Info($"Reviews service listening on {StoreSettings.ReviewsServiceUrl}, started in {startup.ElapsedMilliseconds} ms ({startup.ElapsedMilliseconds - databaseSetup.ElapsedMilliseconds - seeding.ElapsedMilliseconds} ms excluding database setup and seed data), press Ctrl+C to stop");

await bus.WaitForExitAsync();
