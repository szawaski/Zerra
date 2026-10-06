using System.Diagnostics;
using Store.Common;
using Store.Common.Logging;
using Store.Web.Domain.Carts;
using Store.Web.Domain.Catalog;
using Store.Web.Domain.Inventory;
using Store.Web.Domain.Orders;
using Store.Web.Domain.Reviews;
using Store.Web.Domain.Shipping;
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Web;

var startup = Stopwatch.StartNew();

Console.Title = "Store - Web Gateway";
var builder = WebApplication.CreateBuilder(args);

//Zerra.Logging.ILogger, ASP.NET's implicit usings also bring in Microsoft.Extensions.Logging.ILogger
Zerra.Logging.ILogger log = new ConsoleLogger();

//The gateway's bus has no handlers of its own, each query and command is forwarded over TCP to the service that owns it
var bus = Bus.New("Web", log, new ConsoleBusLogger());
var serializer = StoreSettings.CreateServiceSerializer();
var encryptor = StoreSettings.CreateServiceEncryptor();
var compressor = StoreSettings.CreateCompressor();

var catalogClient = new TcpCqrsClient(StoreSettings.CatalogServiceUrl, serializer, encryptor, compressor, log);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);
bus.AddCommandProducer<ICatalogCommandHandler>(catalogClient);

var inventoryClient = new TcpCqrsClient(StoreSettings.InventoryServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<IInventoryQueryHandler>(inventoryClient);
bus.AddCommandProducer<IInventoryCommandHandler>(inventoryClient);
//IStockReservationHandler is deliberately left out, only the Orders service may reserve stock

var ordersClient = new TcpCqrsClient(StoreSettings.OrdersServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<IOrdersQueryHandler>(ordersClient);
bus.AddCommandProducer<IOrdersCommandHandler>(ordersClient);

var reviewsClient = new TcpCqrsClient(StoreSettings.ReviewsServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<IReviewsQueryHandler>(reviewsClient);
//Review commands go through Azure Service Bus when it's running, otherwise straight to Reviews over TCP. Queries always go directly,
//a broker only carries commands and events. The bus takes one producer per command, so the choice is made here at startup, and Reviews makes the same check.
if (!StoreSettings.DirectMessagingOnly && await AzureServiceBusConnectionTest.TestAsync(StoreSettings.AzureServiceBusConnectionString, log: log))
{
    bus.AddCommandProducer<IReviewsCommandHandler>(new AzureServiceBusProducer(StoreSettings.AzureServiceBusConnectionString, serializer, encryptor, null, log, null));
    log.Info("Review commands to Reviews: Azure Service Bus");
}
else
{
    bus.AddCommandProducer<IReviewsCommandHandler>(reviewsClient);
    log.Info("Review commands to Reviews: direct TCP");
}

var cartsClient = new TcpCqrsClient(StoreSettings.CartsServiceUrl, serializer, encryptor, null, log);
bus.AddQueryClient<ICartsQueryHandler>(cartsClient);
bus.AddCommandProducer<ICartsCommandHandler>(cartsClient);
//ICartRepricingHandler is left out too, only the Catalog service sends it. The cart aggregate events never reach the bus at all

//Shipping is hosted in ASP.NET Core, so it's an HTTP client here instead of the TCP clients above; the gateway doesn't care which transport a service uses
var shippingClient = new KestrelCqrsClient(StoreSettings.ShippingServiceUrl, serializer, encryptor, null, log, null, null);
bus.AddQueryClient<IShippingQueryHandler>(shippingClient);
bus.AddCommandProducer<IShippingCommandHandler>(shippingClient);

//The gateway middleware resolves the bus, serializer, and logger from DI, browsers send and receive JSON
builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());
builder.Services.AddSingleton(log);

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(bus.StopServices); //after Kestrel has finished the requests in progress, which may still use the bus

//The pages are static HTML and JavaScript, the browser does the work and calls the gateway
app.UseDefaultFiles();
app.UseStaticFiles();

//Bus.js posts every query and command here. A real site would register an ICqrsAuthorizer and pass allowOrigins.
app.UseCqrsApiGateway("/CQRS");

//Kestrel starts in Run, so the time is logged once it's listening
app.Lifetime.ApplicationStarted.Register(() => log.Info($"Web gateway listening on {String.Join(", ", app.Urls)}, started in {startup.ElapsedMilliseconds} ms"));
app.Run();
