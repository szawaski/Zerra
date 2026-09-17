using Store.Catalog.Domain;
using Store.Common;
using Store.Common.Logging;
using Store.Inventory.Domain;
using Store.Orders.Domain;
using Store.Reviews.Domain;
using Store.Shipping.Domain;
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Web;

Console.Title = "Store - Web Gateway";
var builder = WebApplication.CreateBuilder(args);

//Zerra.Logging.ILogger, ASP.NET's implicit usings also bring in Microsoft.Extensions.Logging.ILogger
Zerra.Logging.ILogger log = new ConsoleLogger();

//The gateway's bus has no handlers of its own, each query and command is forwarded over TCP to the service that owns it
var bus = Bus.New("Web", log, new ConsoleBusLogger());
var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();

var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);
bus.AddCommandProducer<ICatalogCommandHandler>(catalogClient);

var inventoryClient = new TcpCqrsClient(StoreSettings.InventoryServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IInventoryQueryHandler>(inventoryClient);
bus.AddCommandProducer<IInventoryCommandHandler>(inventoryClient);
//IStockReservationHandler is deliberately left out, only the Orders service may reserve stock

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);
bus.AddCommandProducer<IOrdersCommandHandler>(ordersClient);

var reviewsClient = new TcpCqrsClient(StoreSettings.ReviewsServiceUrl, serializer, encryptor, log);
bus.AddQueryClient<IReviewsQueryHandler>(reviewsClient);
//Review commands go through Azure Service Bus when it's running, otherwise straight to Reviews over TCP. Queries always go directly,
//a broker only carries commands and events. The bus takes one producer per command, so the choice is made here at startup, and Reviews makes the same check.
if (!StoreSettings.DirectMessagingOnly && await AzureServiceBusConnection.TestAsync(StoreSettings.AzureServiceBusConnectionString, log: log))
{
    bus.AddCommandProducer<IReviewsCommandHandler>(new AzureServiceBusProducer(StoreSettings.AzureServiceBusConnectionString, serializer, encryptor, log, null));
    log.Info("Review commands to Reviews: Azure Service Bus");
}
else
{
    bus.AddCommandProducer<IReviewsCommandHandler>(reviewsClient);
    log.Info("Review commands to Reviews: direct TCP");
}

//Shipping is hosted in ASP.NET Core, so it's an HTTP client here instead of the TCP clients above; the gateway doesn't care which transport a service uses
var shippingClient = new KestrelCqrsClient(StoreSettings.ShippingServiceUrl, serializer, encryptor, log, null, null);
bus.AddQueryClient<IShippingQueryHandler>(shippingClient);
bus.AddCommandProducer<IShippingCommandHandler>(shippingClient);

//The gateway middleware resolves the bus, serializer, and logger from DI, browsers send and receive JSON
builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());
builder.Services.AddSingleton(log);

var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(bus.StopServices);

//The pages are static HTML and JavaScript, the browser does the work and calls the gateway
app.UseDefaultFiles();
app.UseStaticFiles();

//Bus.js posts every query and command here. A real site would register an ICqrsAuthorizer and pass allowOrigins.
app.UseCqrsApiGateway("/CQRS");

app.Run();
