using System.Diagnostics;
using Store.Carts.Domain;
using Store.Catalog.Domain;
using Store.Catalog.Service.Data;
using Store.Catalog.Service.Handlers;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.PostgreSql;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Catalog Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as a failed database read
log.Info("Starting Catalog service");

//Data store: this service's own database when it's running, checked here first like the message brokers, schema from the data models, then seed data
var databaseSetup = Stopwatch.StartNew();
var usePostgreSql = !StoreSettings.InMemoryOnly && PostgreSqlDataContext.TestConnection(StoreSettings.CatalogPostgreSql, log);
ITransactStoreEngine engine = usePostgreSql ? PostgreSqlDataContext.GetEngine(StoreSettings.CatalogPostgreSql) : MemoryDataContext.GetEngine();
var dataStore = DataStoreSetup.Prepare(engine, "PostgreSQL", [typeof(CategoryDataModel), typeof(ProductDataModel)], log);
databaseSetup.Stop();
log.Info($"Database setup done in {databaseSetup.ElapsedMilliseconds} ms");

var repo = Repo.New();
repo.AddProvider(new CatalogStoreProvider<CategoryDataModel>(engine));
repo.AddProvider(new CatalogStoreProvider<ProductDataModel>(engine));
var seeding = Stopwatch.StartNew();
await CatalogSeeder.SeedAsync(repo, log);
seeding.Stop();
log.Info($"Seed data done in {seeding.ElapsedMilliseconds} ms");

//Message brokers: RabbitMQ carries the product events when it's running, checked here first so the choice can be reported like the data store
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnectionTest.Test(StoreSettings.RabbitMQHost, log: log);
//Catalog receives queries and commands from the gateway over TCP, its RabbitMQ use is outbound
IMessagingInfo messaging = new MessagingInfo("Direct TCP");
log.Info($"Publishing product events over {(useRabbitMQ ? "RabbitMQ" : "direct TCP")}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle the catalog queries and commands, received over TCP from the gateway and the other services
var bus = Bus.New("Catalog", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<ICatalogQueryHandler>(new CatalogQueryHandler());
bus.AddHandler<ICatalogCommandHandler>(new CatalogCommandHandler());

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();
var compressor = StoreSettings.CreateCompressor();

var server = new TcpCqrsServer(StoreSettings.CatalogServiceUrl, serializer, encryptor, compressor, log);
bus.AddQueryServer<ICatalogQueryHandler>(server);
bus.AddCommandConsumer<ICatalogCommandHandler>(server);

//Catalog tells Carts to reprice when a price changes, so a cart never shows a price the store no longer charges. A command and not an
//event: an event would reach every Carts replica and have them all reprice the same carts, a command is handled by one of them.
var cartsClient = new TcpCqrsClient(StoreSettings.CartsServiceUrl, serializer, encryptor, null, log);
bus.AddCommandProducer<ICartRepricingHandler>(cartsClient);

//...and the product events that go with it, which Carts and Reviews both subscribe to. Each one caches products in its own memory, so
//each replica of each has to hear about the change and drop its own copy. That is what an event is for, and RabbitMQ's fanout exchange
//delivers it to every subscriber and every replica. Without RabbitMQ it goes direct, one producer per subscriber.
if (useRabbitMQ)
{
    bus.AddEventProducer<ICatalogEventHandler>(new RabbitMQProducer(StoreSettings.RabbitMQHost, serializer, encryptor, null, log, null));
}
else
{
    bus.AddEventProducer<ICatalogEventHandler>(cartsClient);
    bus.AddEventProducer<ICatalogEventHandler>(new TcpCqrsClient(StoreSettings.ReviewsServiceUrl, serializer, encryptor, null, log));
}

log.Info($"Catalog service listening on {StoreSettings.CatalogServiceUrl}, started in {startup.ElapsedMilliseconds} ms ({startup.ElapsedMilliseconds - databaseSetup.ElapsedMilliseconds - seeding.ElapsedMilliseconds} ms excluding database setup and seed data), press Ctrl+C to stop");

await bus.WaitForExitAsync();
