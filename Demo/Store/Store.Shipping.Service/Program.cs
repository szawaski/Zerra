using Store.Common;
using Store.Common.Data;
using Store.Common.Logging;
using Store.Common.Messaging;
using Store.Orders.Domain;
using Store.Shipping.Domain;
using Store.Shipping.Service.Data;
using Store.Shipping.Service.Handlers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.RabbitMQ;
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

//Commands come from the gateway over HTTP/Kestrel. The order events from Orders take RabbitMQ when it's running, HTTP/Kestrel when it isn't
var useRabbitMQ = !StoreSettings.DirectMessagingOnly && RabbitMQConnection.Test(StoreSettings.RabbitMQHost, log: log);
IMessagingInfo messaging = new MessagingInfo($"Shipment commands: Direct HTTP. Order events: {(useRabbitMQ ? "RabbitMQ" : "Direct HTTP")}.");
log.Info($"Messaging: {messaging.Description}");

var busServices = new BusServices();
busServices.AddRepo(repo);
busServices.AddService<IDataStoreInfo>(dataStore);
busServices.AddService<IMessagingInfo>(messaging);

//Bus: handle shipment queries and commands from the gateway, and the order events from the Orders service
var bus = Bus.New("Shipping", log, new ConsoleBusLogger(), busServices);
var commandHandler = new ShippingCommandHandler();
bus.AddHandler<IShippingQueryHandler>(new ShippingQueryHandler());
bus.AddHandler<IShippingCommandHandler>(commandHandler);
bus.AddHandler<IOrdersEventHandler>(commandHandler);

var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

//Hosted inside ASP.NET Core over HTTP/Kestrel instead of the raw TcpCqrsServer the other services use, same serializer and encryption either way
var kestrelSettings = new KestrelCqrsServerLinkedSettings(route: null, authorizer: null, contentType: ContentType.Bytes);
bus.AddQueryServer<IShippingQueryHandler>(new KestrelCqrsServerQueryServer(kestrelSettings));
bus.AddCommandConsumer<IShippingCommandHandler>(new KestrelCqrsServerCommandConsumer(kestrelSettings));

//Orders announces OrderShippedEvent when an order ships, and this service creates the shipment for it. EventConsumerMode.PerService, not the
//default PerReplica: the shipment is created once, so the replicas of this service have to compete for the event rather than each get a copy,
//which would give the same order a shipment per replica, each with its own carrier and tracking number. The Inventory service subscribes the
//same way and still gets every event, because the two services have separate subscriptions.
if (useRabbitMQ)
    bus.AddEventConsumer<IOrdersEventHandler>(new RabbitMQConsumer(StoreSettings.RabbitMQHost, serializer, encryptor, log, null), EventConsumerMode.PerService);
else
    bus.AddEventConsumer<IOrdersEventHandler>(new KestrelCqrsServerEventConsumer(kestrelSettings), EventConsumerMode.PerService);

var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(bus.StopServices);
app.UseKestrelCqrsServer(serializer, encryptor, log, kestrelSettings);

log.Info($"Shipping service listening on {StoreSettings.ShippingServiceUrl}, press Ctrl+C to stop");
app.Run();
