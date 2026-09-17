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
IMessagingInfo messaging = new MessagingInfo($"{(useServiceBus ? "Azure Service Bus" : "Direct TCP")}.{(StoreSettings.DirectMessagingOnly ? " (STORE_DIRECT_MESSAGING=true)" : null)}");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle review queries and commands from the gateway
var bus = Bus.New("Reviews", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<IReviewsQueryHandler>(new ReviewsQueryHandler());
bus.AddHandler<IReviewsCommandHandler>(new ReviewsCommandHandler());

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

//Downstream services: the product's name from Catalog, purchase history from Orders to mark a review Verified
var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);

log.Info($"Reviews service listening on {StoreSettings.ReviewsServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
