[← Back to Documentation](Index.md)

# Zerra.Web - ASP.NET Integration

`Zerra.Web` provides ASP.NET Core integration for Zerra CQRS, enabling your CQRS bus to be hosted within ASP.NET applications. This is essential for IIS-hosted environments (Azure App Services) and for exposing CQRS functionality as an HTTP API Gateway.

## Overview

`Zerra.Web` provides:
- **IIS/Kestrel Hosting** - Run CQRS bus within ASP.NET Core applications
- **API Gateway** - Expose CQRS commands and queries as HTTP endpoints (events are intentionally not accepted from external callers)
- **Azure App Services** - Compatible with IIS-hosted Azure App Services
- **Custom Authorization** - Integrate with ASP.NET authentication/authorization
- **Logging Integration** - Bridge Zerra logging with Microsoft.Extensions.Logging
- **Multiple Content Types** - Support JSON and binary serialization
- **CORS Support** - Built-in cross-origin resource sharing

## Installation

```bash
dotnet add package Zerra.Web
```

## Key Components

### 1. CQRS API Gateway (Main Feature)

The API Gateway exposes your CQRS bus to external HTTP clients, allowing browsers, mobile apps, and other services to invoke commands and queries without knowledge of your internal architecture. Events are not accepted through the gateway; a request whose `MessageType` is not a command is rejected.

#### Basic Setup

```csharp
using Zerra.CQRS;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Web;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Configure CQRS components
ISerializer serializer = new ZerraJsonSerializer(); // Use JSON for external clients
// Fully qualified: ASP.NET implicit usings also bring Microsoft.Extensions.Logging.ILogger into scope
Zerra.Logging.ILogger log = new ConsoleLogger(); // your ILogger implementation (see Logging.md)
IBusLogger busLog = new ConsoleBusLogger();

// Create the CQRS bus
var bus = Bus.New(
    serviceName: "MyService",
    log: log,
    busLog: busLog
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

// Add Bus and components to DI container - the gateway resolves IBus, ISerializer,
// and optionally Zerra.Logging.ILogger and ICqrsAuthorizer from DI.
// Bus.New returns IBusSetup, so register it explicitly as IBus.
builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);
builder.Services.AddSingleton(log);

var app = builder.Build();

// Enable CQRS API Gateway
app.UseCqrsApiGateway(route: "/api/cqrs");

await app.RunAsync();
```

#### How It Works

The API Gateway:
1. Listens for HTTP POST (and CORS preflight OPTIONS) requests at the specified route (default: `/CQRS`)
2. Calls the registered `ICqrsAuthorizer`, if any
3. Deserializes the request body (`ApiRequestData`) to determine which query or command to invoke
4. Dispatches the message to the CQRS bus
5. Serializes the response and returns it to the client

The request body is an `ApiRequestData` object. The front end scripts and `ApiClient` build it for you; the shapes are shown here for reference.

**Query request** (`ProviderArguments` holds each argument serialized on its own; the arguments are byte arrays, so JSON carries each one as base64 of its JSON, here `"123"`):
```json
POST /api/cqrs
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
POST /api/cqrs
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

### 2. Custom Authorization

Implement `ICqrsAuthorizer` to add custom authentication/authorization:

```csharp
using Zerra.CQRS.Network;
using System.Security;

public class ApiKeyAuthorizer : ICqrsAuthorizer
{
    private readonly string _validApiKey;

    public ApiKeyAuthorizer(string validApiKey)
    {
        _validApiKey = validApiKey;
    }

    // Server-side: Validate incoming requests
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("X-API-Key", out var apiKeys) || 
            !apiKeys.Contains(_validApiKey))
        {
            throw new SecurityException("Invalid API Key");
        }
    }

    // Client-side: Add authorization headers
    public Dictionary<string, List<string?>> GetAuthorizationHeaders(
        CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _validApiKey }
        };
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
        => new(GetAuthorizationHeaders(cancellationToken));
}
```

#### Using Authorization with API Gateway

```csharp
var authorizer = new ApiKeyAuthorizer("my-secret-api-key");

// Add to DI container
builder.Services.AddSingleton<ICqrsAuthorizer>(authorizer);

var app = builder.Build();

// Gateway will automatically use the authorizer from DI
app.UseCqrsApiGateway(route: "/api/cqrs");
```

The middleware will:
- Call `Authorize()` for every incoming POST request, before the message is dispatched
- Return `401 Unauthorized` if `Authorize()` or a handler throws `SecurityException`
- Return `500 Internal Server Error` for other exceptions

### 3. ASP.NET Authentication Integration

Integrate with ASP.NET Core authentication:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

public class JwtCqrsAuthorizer : ICqrsAuthorizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCqrsAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        var context = _httpContextAccessor.HttpContext;

        // Check if user is authenticated
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            throw new SecurityException("User not authenticated");
        }

        // Check claims/roles
        if (!context.User.IsInRole("ApiUser"))
        {
            throw new SecurityException("Insufficient permissions");
        }
    }

    public Dictionary<string, List<string?>> GetAuthorizationHeaders(
        CancellationToken cancellationToken = default)
    {
        // Get JWT token from current context
        var context = _httpContextAccessor.HttpContext;
        var token = context?.Request.Headers.Authorization.ToString();

        return new Dictionary<string, List<string?>>
        {
            ["Authorization"] = new List<string?> { token }
        };
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
        => new(GetAuthorizationHeaders(cancellationToken));
}

// Configure in Startup
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });
builder.Services.AddSingleton<ICqrsAuthorizer, JwtCqrsAuthorizer>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCqrsApiGateway(route: "/api/cqrs");
```

### 4. Logging Integration

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

This allows:
- ASP.NET middleware to log through Zerra
- Unified logging across CQRS and ASP.NET components
- Consistent log format and destination

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

## Usage Examples

### IIS/Azure App Services Hosting

For Azure App Services (which use IIS):

```csharp
using Zerra.CQRS;
using Zerra.Serialization;
using Zerra.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure CQRS
ISerializer serializer = new ZerraJsonSerializer();
var bus = Bus.New("MyService");
bus.AddHandler<IMyCommandHandler>(new MyCommandHandler());
bus.AddHandler<IMyQueryHandler>(new MyQueryHandler());

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);

var app = builder.Build();

// Expose CQRS via HTTP
app.UseCqrsApiGateway(route: "/api/cqrs");

// IIS/Azure App Services will manage the Kestrel lifetime
await app.RunAsync();
```

**Azure App Service Configuration:**
- Set `ASPNETCORE_ENVIRONMENT` to `Production`
- Configure application settings in Azure Portal
- The app runs in IIS with Kestrel as the web server

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
### Microservices Gateway

Use as a gateway for microservices communication:

```csharp
// Gateway Service (ASP.NET)
var builder = WebApplication.CreateBuilder(args);

ISerializer serializer = new ZerraJsonSerializer();
var bus = Bus.New("GatewayService");

// Connect to backend services
var userServiceClient = new TcpCqrsClient("user-service:9001", serializer, null, null);
bus.AddCommandProducer<IUserCommandHandler>(userServiceClient);
bus.AddQueryClient<IUserQueryHandler>(userServiceClient);

var orderServiceClient = new TcpCqrsClient("order-service:9002", serializer, null, null);
bus.AddCommandProducer<IOrderCommandHandler>(orderServiceClient);
bus.AddQueryClient<IOrderQueryHandler>(orderServiceClient);

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);

var app = builder.Build();

// Expose unified API to external clients
app.UseCqrsApiGateway(route: "/api");

await app.RunAsync();
```

Clients call the gateway, which routes to appropriate backend services.

## Best Practices

### 1. Use JSON for External Clients

```csharp
// ✅ Good - JSON is interoperable with browsers/mobile
ISerializer serializer = new ZerraJsonSerializer();
app.UseCqrsApiGateway();
```

### 2. Always Use Authorization for Public APIs

```csharp
// ✅ Good - protect your API
builder.Services.AddSingleton<ICqrsAuthorizer>(new ApiKeyAuthorizer("secret"));
app.UseCqrsApiGateway();

// ❌ Bad - no authorization
app.UseCqrsApiGateway(); // Anyone can call any command!
```

### 3. Restrict Browser Origins in Production

```csharp
// ✅ Good - only your front ends can call the gateway from a browser, plus ApiClient callers (the gateway's host)
app.UseCqrsApiGateway(route: "/api/cqrs", allowOrigins: ["https://myapp.com", "https://mobile.myapp.com", "api.myapp.com"]);
```

CORS restricts browsers only (see [CORS Configuration](#cors-configuration)), so still use `ICqrsAuthorizer` and authentication to control access.

### 4. Rate Limit with a Global Limiter

The gateway is middleware rather than an endpoint, so ASP.NET rate limiting applies through `options.GlobalLimiter`, not a named policy, followed by `app.UseRateLimiter()` before `app.UseCqrsApiGateway()`.

### 5. Log Gateway Activity

```csharp
Zerra.Logging.ILogger log = new ConsoleLogger();
IBusLogger busLog = new ConsoleBusLogger();

var bus = Bus.New("MyService", log: log, busLog: busLog);

// The bus logger tracks the commands and queries received through the gateway
```

## When to Use Zerra.Web

Choose `Zerra.Web` when:

- ✅ **IIS Hosting** - Deploying to Azure App Services or IIS
- ✅ **External Clients** - Browsers, mobile apps, or third-party services need access
- ✅ **API Gateway Pattern** - Single entry point for multiple backend services
- ✅ **ASP.NET Integration** - Using ASP.NET authentication/authorization
- ✅ **Existing ASP.NET App** - Adding CQRS to an existing web application
- ✅ **HTTP/HTTPS Required** - Standard web protocols for compatibility

Don't use when:

- ❌ The gateway for internal service-to-service calls - Services call each other directly over TCP, HTTP, or a broker. A service can still be hosted in ASP.NET Core, see [Hosting a Service in ASP.NET Core](#hosting-a-service-in-aspnet-core)

## Troubleshooting

### Gateway Returns 400 or 500

**Problem**: API Gateway rejects requests

**Solutions**:
- A `400` means the `Content-Type` is missing or doesn't match the registered serializer, the `Accept` type can't be produced (see [Content Type Support](#content-type-support)), or the body had neither `ProviderType` (query) nor `MessageType` (command)
- A `401` without an authorizer failure means the request's `Origin` is missing or isn't in `allowOrigins` (for `ApiClient`, add the gateway's host name)
- Verify `MessageType` names a command type the gateway can resolve (events are rejected) and that the bus has a handler or producer for it
- Verify `ProviderType` names a query interface registered with the bus

### Authorization Fails

**Problem**: Requests fail authorization

**Solutions**:
- Verify `ICqrsAuthorizer` is registered in DI container
- Check authorization headers are included in client requests
- Ensure `Authorize()` method doesn't throw exceptions for valid requests
- Throw `SecurityException` (not other exception types) from `Authorize()` so the gateway responds with `401` rather than `500`
- Use a debugger to inspect the headers received

### CORS Errors in Browser

**Problem**: Browser shows CORS policy errors

**Solutions**:
- If you set `allowOrigins`, verify the page's origin (or its host name) is in the list
- Verify the request path matches the gateway `route`; requests to other paths are passed on and don't get the gateway's CORS headers
- Check that another CORS middleware isn't adding a conflicting `Access-Control-Allow-Origin` header
- Check that preflight OPTIONS requests reach the gateway (it answers them automatically)

### Performance Issues

**Problem**: Gateway is slow under load

**Solutions**:
- Use `ZerraByteSerializer` if clients support binary (faster than JSON)
- Enable response compression in ASP.NET
- Configure Kestrel limits appropriately
- Consider using message brokers for high-volume scenarios
- Add rate limiting to prevent abuse

## See Also

- [Front End Scripts](FrontEndScripts.md) - Browser clients for the gateway
- [ApiClient](ApiClient.md) - .NET clients for the gateway
- [Server Setup](ServerSetup.md) - Configure CQRS servers
- [Client Setup](ClientSetup.md) - Configure CQRS clients
- [JsonSerializer](JsonSerializer.md) - JSON serialization for external clients
- [Encryptors](Encryptors.md) - Secure message encryption
- [Logging](Logging.md) - Implement logging for gateway activity
