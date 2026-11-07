using System.Diagnostics;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Wishlists.Domain;
using Store.Wishlists.Domain.Catalog;
using Store.Wishlists.Service.Data;
using Store.Wishlists.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.MongoDB;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Wishlists Service";
ILogger log = new ConsoleLogger();
log.Info("Starting Wishlists service");

//Data store: MongoDB, a document store, when it's running, each saved product a document. The handlers use the same IRepo queries,
//commands, and code first generation as the services on SQL databases. Without MongoDB the wishlists are kept in memory
var databaseSetup = Stopwatch.StartNew();
var useMongoDB = !StoreSettings.InMemoryOnly && MongoDBConnectionTest.Test(StoreSettings.WishlistsMongoDB, log);
ITransactStoreEngine engine = useMongoDB ? new MongoDBEngine(StoreSettings.WishlistsMongoDB) : new MemoryEngine();
var dataStore = DataStoreSetup.Prepare(engine, "MongoDB", [typeof(WishlistItemDataModel)], log);
databaseSetup.Stop();
log.Info($"Database setup done in {databaseSetup.ElapsedMilliseconds} ms");

var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<WishlistItemDataModel>(engine));
var seeding = Stopwatch.StartNew();
await WishlistsSeeder.SeedAsync(repo, log);
seeding.Stop();
log.Info($"Seed data done in {seeding.ElapsedMilliseconds} ms");

//Wishlists takes no events, its queries and commands come from the gateway over TCP
IMessagingInfo messaging = new MessagingInfo("Direct TCP");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle wishlist queries and commands from the gateway
var bus = Bus.New("Wishlists", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<IWishlistsQueryHandler>(new WishlistsQueryHandler());
bus.AddHandler<IWishlistsCommandHandler>(new WishlistsCommandHandler());

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();
var compressor = StoreSettings.CreateCompressor();

var server = new TcpCqrsServer(StoreSettings.WishlistsServiceUrl, serializer, encryptor, null, log);
bus.AddQueryServer<IWishlistsQueryHandler>(server);
bus.AddCommandConsumer<IWishlistsCommandHandler>(server);

//Downstream service: the products' current names and prices from Catalog, whose traffic is compressed
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, compressor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

log.Info($"Wishlists service listening on {StoreSettings.WishlistsServiceUrl}, started in {startup.ElapsedMilliseconds} ms ({startup.ElapsedMilliseconds - databaseSetup.ElapsedMilliseconds - seeding.ElapsedMilliseconds} ms excluding database setup and seed data), press Ctrl+C to stop");

await bus.WaitForExitAsync();
