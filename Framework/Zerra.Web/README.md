# Zerra.Web

ASP.NET Core hosting for [Zerra](https://www.nuget.org/packages/Zerra), on Kestrel or IIS (including Azure App Services):

- **The CQRS API gateway**, which exposes a bus's queries and commands to browsers, mobile apps, and other outside callers over HTTP. Events are never accepted from outside.
- **Kestrel hosting for a service**, as an alternative to `TcpCqrsServer`.
- **Authorization, CORS, and logging** that fit into ASP.NET Core.

## Installation

```bash
dotnet add package Zerra.Web
```

## The CQRS API Gateway

The gateway resolves `IBus` and `ISerializer` from DI, and optionally a `Zerra.Logging.ILogger` and an `ICqrsAuthorizer`. A typical gateway has no handlers of its own and forwards to the backend services:

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Web;

var builder = WebApplication.CreateBuilder(args);

var bus = Bus.New("Gateway");
var users = new TcpCqrsClient("user-service:9001", new ZerraByteSerializer(), encryptor, null, log: null);
bus.AddCommandProducer<IUserCommandHandler>(users);
bus.AddQueryClient<IUserQueryHandler>(users);

builder.Services.AddSingleton<IBus>(bus);   // Bus.New returns IBusSetup, so register it as IBus
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());   // what outside callers speak
builder.Services.AddSingleton<ICqrsAuthorizer, MyAuthorizer>();

var app = builder.Build();
app.UseCqrsApiGateway("/CQRS", allowOrigins: ["https://myapp.com"]);
await app.RunAsync();
```

Browsers call the gateway with the `Bus.js` or `Bus.ts` front end scripts, and .NET applications with `ApiClient`.

## Hosting a Service in ASP.NET Core

```csharp
var settings = new KestrelCqrsServerLinkedSettings(route: null, authorizer: null, contentType: ContentType.Bytes);
bus.AddQueryServer<IShippingQueryHandler>(new KestrelCqrsServerQueryServer(settings));
bus.AddCommandConsumer<IShippingCommandHandler>(new KestrelCqrsServerCommandConsumer(settings));

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(bus.StopServices);   // after Kestrel finishes the requests in progress
app.UseKestrelCqrsServer(serializer, encryptor, null, log, settings);
app.Run();
```

Callers use `KestrelCqrsClient` in place of `TcpCqrsClient`.

## Documentation

- [Zerra.Web](https://github.com/szawaski/Zerra/blob/master/docs/ZerraWeb.md) - Gateway, CORS, content types, and Kestrel hosting
- [Security](https://github.com/szawaski/Zerra/blob/master/docs/Security.md) - API key and JWT authorizers
- [Front End Scripts](https://github.com/szawaski/Zerra/blob/master/docs/FrontEndScripts.md) and [ApiClient](https://github.com/szawaski/Zerra/blob/master/docs/ApiClient.md) - Calling the gateway
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
