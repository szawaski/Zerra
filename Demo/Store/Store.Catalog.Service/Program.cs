using Store.Catalog.Domain;
using Store.Catalog.Service.Data;
using Store.Catalog.Service.Handlers;
using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Repository;

Console.Title = "Store - Catalog Service";
ILogger log = new ConsoleLogger();
Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Catalog service");

//Data store: this service's own database, schema from the data models, then seed data
var dataStore = DataStoreSetup.Prepare<CatalogDataContext>("PostgreSQL", [typeof(CategoryDataModel), typeof(ProductDataModel)], log);

var repo = Repo.New();
repo.AddProvider(new CatalogStoreProvider<CategoryDataModel>());
repo.AddProvider(new CatalogStoreProvider<ProductDataModel>());
await CatalogSeeder.SeedAsync(repo, log);

//No message broker here, Catalog only answers queries and takes commands from the gateway over TCP
IMessagingInfo messaging = new MessagingInfo("Direct TCP");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle the catalog queries and commands, received over TCP from the gateway and the other services
var bus = Bus.New("Catalog", log, new ConsoleBusLogger(), busServices);
bus.AddHandler<ICatalogQueryHandler>(new CatalogQueryHandler());
bus.AddHandler<ICatalogCommandHandler>(new CatalogCommandHandler());

var server = new TcpCqrsServer(StoreSettings.CatalogServiceUrl, StoreSettings.CreateServiceSerializer(), StoreSettings.CreateServiceEncryptor(), log);
bus.AddQueryServer<ICatalogQueryHandler>(server);
bus.AddCommandConsumer<ICatalogCommandHandler>(server);

log.Info($"Catalog service listening on {StoreSettings.CatalogServiceUrl}, press Ctrl+C to stop");

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) => { e.Cancel = true; exit.Cancel(); };
await bus.WaitForExitAsync(exit.Token);
