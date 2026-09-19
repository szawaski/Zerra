using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Shipping.Domain;
using Store.Shipping.Service.Data;
using Store.Shipping.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Repository;
using Zerra.Web;

Console.Title = "Store - Shipping Service";
//Zerra.Logging.ILogger, ASP.NET's implicit usings also bring in Microsoft.Extensions.Logging.ILogger
Zerra.Logging.ILogger log = new ConsoleLogger();
Zerra.Logging.Log.SetLog(log); //framework messages too, such as why a database was skipped
log.Info("Starting Shipping service");

var builder = WebApplication.CreateBuilder(args);

//No database to reach here, see ShippingDataContext for why
CodeFirstGeneration.Generate<ShippingDataContext>(DataStoreGenerationType.CodeFirst | DataStoreGenerationType.NoDelete, [typeof(ShipmentDataModel)], log);
IDataStoreInfo dataStore = new DataStoreInfo("In-memory (by design, this service needs no database)");
log.Info($"Data store: {dataStore.Description}");

var repo = Repo.New();
repo.AddProvider(new ShippingStoreProvider<ShipmentDataModel>());

//No message broker here: Shipping takes commands from the gateway and from Orders over HTTP/Kestrel
IMessagingInfo messaging = new MessagingInfo("Direct HTTP");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle shipment queries and commands from the gateway, and the create-shipment command from the Orders service
var bus = Bus.New("Shipping", log, new ConsoleBusLogger(), busServices);
var commandHandler = new ShippingCommandHandler();
bus.AddHandler<IShippingQueryHandler>(new ShippingQueryHandler());
bus.AddHandler<IShippingCommandHandler>(commandHandler);
bus.AddHandler<IShipmentHandler>(commandHandler);

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

//Hosted inside ASP.NET Core over HTTP/Kestrel instead of the raw TcpCqrsServer the other services use, same serializer and encryption either way
var kestrelSettings = new KestrelCqrsServerLinkedSettings(route: null, authorizer: null, contentType: ContentType.Bytes);
bus.AddQueryServer<IShippingQueryHandler>(new KestrelCqrsServerQueryServer(kestrelSettings));
bus.AddCommandConsumer<IShippingCommandHandler>(new KestrelCqrsServerCommandConsumer(kestrelSettings));

//Orders sends CreateShipmentCommand here when an order ships. A command and not an event because it creates the shipment, which has to
//happen once: an event would reach every Shipping replica and each would create its own shipment with its own tracking number.
bus.AddCommandConsumer<IShipmentHandler>(new KestrelCqrsServerCommandConsumer(kestrelSettings));

var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(bus.StopServices);
app.UseKestrelCqrsServer(serializer, encryptor, log, kestrelSettings);

log.Info($"Shipping service listening on {StoreSettings.ShippingServiceUrl}, press Ctrl+C to stop");
app.Run();
