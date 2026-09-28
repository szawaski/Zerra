[← Back to Documentation](Index.md)

# Zerra.Web - ASP.NET Integration

`Zerra.Web` hosts Zerra in ASP.NET Core, on Kestrel or IIS (including Azure App Services). It provides:

- **The CQRS API gateway**, which exposes a bus's queries and commands to browsers, mobile apps, and other outside callers over HTTP. Events are never accepted from outside.
- **Kestrel hosting for a service**, as an alternative to `TcpCqrsServer`.
- **Authorization, CORS, and logging** that fit into ASP.NET Core.

## Installation

```bash
dotnet add package Zerra.Web
```

## The CQRS API Gateway

The gateway exposes a bus's queries and commands to outside callers, who don't need to know which service handles what. It resolves `IBus` and `ISerializer` from DI, and optionally a `Zerra.Logging.ILogger` and an `ICqrsAuthorizer`.

```csharp
using Zerra.CQRS;
using Zerra.Serialization;
using Zerra.Web;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET's implicit usings also bring in Microsoft's ILogger, so qualify Zerra's
Zerra.Logging.ILogger log = new ConsoleLogger();   // your implementation, see Logging.md

var bus = Bus.New("MyService", log, busLog);
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

builder.Services.AddSingleton<IBus>(bus);   // Bus.New returns IBusSetup, so register it as IBus
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());
builder.Services.AddSingleton(log);

var app = builder.Build();
app.UseCqrsApiGateway("/CQRS");
await app.RunAsync();
```
### How It Works

The API Gateway:
1. Listens for HTTP POST (and CORS preflight OPTIONS) requests at the specified route (default: `/CQRS`)
2. Calls the registered `ICqrsAuthorizer`, if any
3. Deserializes the request body (`ApiRequestData`) to determine which query or command to invoke
4. Dispatches the message to the CQRS bus
5. Serializes the response and returns it to the client

The request body is an `ApiRequestData` object. The front end scripts and `ApiClient` build it for you; the shapes are shown here for reference.

**Query request** (`ProviderArguments` holds each argument serialized on its own; the arguments are byte arrays, so JSON carries each one as base64 of its JSON, here `"123"`):
```json
POST /CQRS
Content-Type: application/json

{
  "ProviderType": "IUserQueryHandler",
  "ProviderMethod": "GetUserById",
  "ProviderArguments": ["IjEyMyI="],
  "Source": "JavaScript"
}
```

**Command request** (`MessageData` is the command serialized as a JSON string):
```json
POST /CQRS
Content-Type: application/json

{
  "MessageType": "CreateUserCommand",
  "MessageData": "{\"Email\":\"user@example.com\",\"Name\":\"John Doe\"}",
  "MessageAwait": true,
  "MessageResult": true,
  "Source": "JavaScript"
}
```

- `MessageAwait` - `false` for fire-and-forget, `true` to wait for the handler to complete
- `MessageResult` - `true` for `ICommand<TResult>` commands; the response body is the serialized result

**Response:** the serialized query result or command result, an empty `200` for commands without a result, or a raw stream for queries that return `Stream`.

**Upload request** (a query with a `Stream` parameter): send the header `Upload-Stream: true`, and a body of the request JSON's length as a 4-byte little-endian integer, then the request JSON with `null` in the stream argument's place, then a 4-byte `0` marking the end of the request, then the stream's bytes until the body ends. (The request may be split into several length-prefixed pieces before the `0`, which is how the .NET clients write it without buffering.) The gateway lifts Kestrel's request size limit for these requests and passes the rest of the body to the query as it arrives. `Bus.js` and `Bus.ts` do this for you when an argument is a `Blob` or `File`.

### Authorization

Register an `ICqrsAuthorizer` in DI and the gateway calls its `Authorize(headers)` for every request, before the message is dispatched:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICqrsAuthorizer, JwtCqrsAuthorizer>();

var app = builder.Build();
app.UseAuthentication();   // when the authorizer relies on ASP.NET authentication
app.UseAuthorization();
app.UseCqrsApiGateway("/CQRS");
```

- A `SecurityException` from `Authorize()` or from a handler returns `401 Unauthorized`. Any other exception returns `500`.
- An authorizer can check headers itself, such as an API key, or read `HttpContext.User` after ASP.NET authentication has run.

[Security](Security.md) has complete API key and JWT authorizers, and explains how claims reach the handlers.
### Logging Integration

Bridge Zerra logging with ASP.NET Core logging:

```csharp
using Zerra.Logging;
using Zerra.Web;
using Microsoft.Extensions.Logging;

// Create Zerra logger (your implementation)
Zerra.Logging.ILogger zerraLogger = new ConsoleLogger();

// Add Zerra logger to ASP.NET logging
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ZerraLoggerProvider(zerraLogger));

// Alternatively, on an existing ILoggerFactory: loggerFactory.AddZerraLogger(zerraLogger);

// Now ASP.NET components log through Zerra
var app = builder.Build();
```

ASP.NET Core's own logging then goes to the same destination as Zerra's.

## Configuration Options

### API Gateway Route

Customize the endpoint where the gateway listens:

```csharp
// Default route
app.UseCqrsApiGateway(); // Listens at /CQRS

// Custom route
app.UseCqrsApiGateway(route: "/api/v1/gateway");

// Handle POST requests on any path
app.UseCqrsApiGateway(route: null);
```

Every gateway resolves the same `IBus` and `ISerializer` from DI, so multiple routes expose the same bus.

### Content Type Support

The request `Content-Type` must match the `ContentType` of the `ISerializer` registered in DI; a missing or different content type gets `400 Bad Request`:

| Registered serializer | Required `Content-Type` |
|---|---|
| `ZerraJsonSerializer` | `application/json` |
| `ZerraJsonSerializer` with `Nameless = true` | `application/jsonnameless` |
| `ZerraByteSerializer` | `application/octet-stream` |

With a standard JSON serializer, clients may also send `Accept: application/jsonnameless` to receive nameless JSON responses (the front end scripts do this automatically when a model type is supplied). An `Accept` type the gateway can't produce also gets `400 Bad Request`.

### CORS Configuration

The gateway handles CORS itself, including `OPTIONS` preflight requests. By default it allows every origin:

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: *
Access-Control-Allow-Headers: *
```

To restrict browser access, pass `allowOrigins`. Each value can be a full origin (`scheme://host[:port]`) or just a host name; matching is case-insensitive:

```csharp
app.UseCqrsApiGateway(route: "/api/cqrs", allowOrigins: ["https://myapp.com", "mobile.myapp.com"]);
```

With `allowOrigins` set:
- An allowed request `Origin` is echoed back in `Access-Control-Allow-Origin` (with `Vary: Origin`)
- A request whose `Origin` is missing or not allowed gets `401 Unauthorized`, and a disallowed preflight gets no `Access-Control-Allow-Origin` header
- `ApiClient` sends the gateway's host name as its `Origin` (as `HttpCqrsClient` and `KestrelCqrsClient` do for their servers), so include that host to keep .NET clients working, e.g. `allowOrigins: ["https://myapp.com", "api.myapp.com"]`
- Passing `null`, an empty array, or `"*"` allows all origins

`HttpCqrsServer` and `KestrelCqrsServerMiddleware` apply the same rules to their `allowOrigins`.

CORS only constrains browsers; any other client can send any `Origin`. Use `ICqrsAuthorizer` and authentication to control who can call the gateway. Because the gateway writes its own CORS headers, configure origins with `allowOrigins` rather than an ASP.NET CORS policy.

## Usage

### Gateway in Front of Several Services

The usual gateway has no handlers of its own. It registers a client for each backend service and forwards what outside callers send, so it exposes exactly the interfaces it registers. Leave out anything meant only for service-to-service use.

```csharp
var bus = Bus.New("Gateway", log, busLog);

var users = new TcpCqrsClient("user-service:9001", new ZerraByteSerializer(), encryptor, log);
bus.AddCommandProducer<IUserCommandHandler>(users);
bus.AddQueryClient<IUserQueryHandler>(users);

var orders = new TcpCqrsClient("order-service:9002", new ZerraByteSerializer(), encryptor, log);
bus.AddCommandProducer<IOrderCommandHandler>(orders);
bus.AddQueryClient<IOrderQueryHandler>(orders);

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer()); // what outside callers speak
builder.Services.AddSingleton(log);

var app = builder.Build();
app.UseCqrsApiGateway("/CQRS");
await app.RunAsync();
```

The gateway talks JSON to outside callers and binary to the services. `Demo/Store/Store.Web` is a complete example. The same app runs unchanged on IIS or Azure App Services.

### Hosting a Service in ASP.NET Core

A service can be hosted in ASP.NET Core/Kestrel over HTTP instead of `TcpCqrsServer`. One settings object is shared by the query server, command consumer, and event consumer:

```csharp
var builder = WebApplication.CreateBuilder(args);
// build the bus and add handlers as with any other service

var settings = new KestrelCqrsServerLinkedSettings(route: null, authorizer: null, contentType: ContentType.Bytes);
bus.AddQueryServer<IShippingQueryHandler>(new KestrelCqrsServerQueryServer(settings));
bus.AddCommandConsumer<IShippingCommandHandler>(new KestrelCqrsServerCommandConsumer(settings));
bus.AddEventConsumer<IOrderEventHandler>(new KestrelCqrsServerEventConsumer(settings), EventConsumerMode.PerService);

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(bus.StopServices); // after Kestrel finishes the requests in progress
app.UseKestrelCqrsServer(serializer, encryptor, log, settings);
app.Run();
```

Callers use `KestrelCqrsClient` instead of `TcpCqrsClient`, with the same constructor shape plus an `authorizer` and `route`. Nothing else changes for the caller or the handlers. `Demo/Store/Store.Shipping.Service` is a complete example.

### Browser Clients

Browsers call the gateway with `Bus.js` or `Bus.ts`, using JavaScript or TypeScript models generated from your `*.Domain` projects. See [Front End Scripts](FrontEndScripts.md).

### .NET Clients

.NET applications call the gateway with `ApiClient`, registered on their own bus like any other query client and command producer. See [ApiClient](ApiClient.md).

## Production Checklist

- **Authorize every request.** Register an `ICqrsAuthorizer`; without one, anyone who can reach the gateway can call every command it exposes. See [Security](Security.md).
- **Restrict browser origins** with `allowOrigins`, and include the gateway's own host if `ApiClient` callers use it. CORS only constrains browsers, so it doesn't replace authorization.
- **Rate limit with a global limiter.** The gateway is middleware rather than an endpoint, so ASP.NET rate limiting applies through `options.GlobalLimiter`, not a named policy, with `app.UseRateLimiter()` before `app.UseCqrsApiGateway()`.
- **Pass an `IBusLogger`** to the gateway's bus to record every query and command it forwards. See [Logging](Logging.md).

## Troubleshooting

**`400 Bad Request`:**
- The `Content-Type` is missing or doesn't match the registered serializer, or the `Accept` type can't be produced (see [Content Type Support](#content-type-support)).
- The body has neither `ProviderType` (a query) nor `MessageType` (a command).

**`401 Unauthorized` without an authorizer failure:** the request's `Origin` is missing or isn't in `allowOrigins`. For `ApiClient`, add the gateway's host name.

**`500` for a message:** check that `MessageType` names a command (events are rejected), that `ProviderType` names a query interface, and that the bus has a handler, client, or producer for it.

**Authorization fails:**
- Check the `ICqrsAuthorizer` is registered in DI and the client sends its headers.
- Throw `SecurityException` from `Authorize()`, so the gateway answers `401` instead of `500`.

**CORS errors in the browser:**
- Check the page's origin, or its host name, is in `allowOrigins`.
- Check the request path matches the gateway's `route`, since other paths don't get its CORS headers.
- Check no other CORS middleware adds a conflicting `Access-Control-Allow-Origin`.

**Slow under load:** check Kestrel's limits and response compression. .NET callers can use a separate gateway app registered with `ZerraByteSerializer`, since one gateway accepts only its registered serializer's content type.

## See Also

- [Front End Scripts](FrontEndScripts.md) - Browser clients for the gateway
- [ApiClient](ApiClient.md) - .NET clients for the gateway
- [Server Setup](ServerSetup.md) - Configure CQRS servers
- [Client Setup](ClientSetup.md) - Configure CQRS clients
- [JsonSerializer](JsonSerializer.md) - JSON serialization for external clients
- [Encryptors](Encryptors.md) - Secure message encryption
- [Logging](Logging.md) - Implement logging for gateway activity
