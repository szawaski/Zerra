[← Back to Documentation](Index.md)

# Zerra.Web - ASP.NET Integration

`Zerra.Web` provides ASP.NET Core integration for Zerra CQRS, enabling your CQRS bus to be hosted within ASP.NET applications. This is essential for IIS-hosted environments (Azure App Services) and for exposing CQRS functionality as an HTTP API Gateway.

## Overview

`Zerra.Web` provides:
- **IIS/Kestrel Hosting** - Run CQRS bus within ASP.NET Core applications
- **API Gateway** - Expose CQRS commands, queries, and events as HTTP endpoints
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

The API Gateway exposes your CQRS bus to external HTTP clients, allowing browsers, mobile apps, and other services to invoke commands, queries, and events without knowledge of your internal architecture.

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
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger log = new Logger();
IBusLogger busLog = new BusLogger();

// Create the CQRS bus
var bus = Bus.New(
    service: "MyService",
    log: log,
    busLog: busLog
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());

// Add Bus and components to DI container
builder.Services.AddSingleton(bus);
builder.Services.AddSingleton(serializer);
builder.Services.AddSingleton(log);

var app = builder.Build();

// Enable CQRS API Gateway
app.UseCqrsApiGateway(route: "/api/cqrs");

await app.RunAsync();
```

#### How It Works

The API Gateway:
1. Listens for HTTP POST requests at the specified route (default: `/CQRS`)
2. Deserializes the request body to determine which command/query/event to invoke
3. Dispatches the message to the CQRS bus
4. Serializes the response and returns it to the client

**Request Format:**
```json
POST /api/cqrs
Content-Type: application/json

{
  "MessageType": "MyApp.Commands.CreateUserCommand",
  "Message": {
    "Email": "user@example.com",
    "Name": "John Doe"
  }
}
```

**Response Format:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "UserId": "12345"
}
```

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
    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _validApiKey }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
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
- Call `Authorize()` for every incoming request
- Return `401 Unauthorized` if `SecurityException` is thrown
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

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        // Get JWT token from current context
        var context = _httpContextAccessor.HttpContext;
        var token = context?.Request.Headers.Authorization.ToString();

        var headers = new Dictionary<string, List<string?>>
        {
            ["Authorization"] = new List<string?> { token }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
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

// Create Zerra logger
ILogger zerraLogger = new Logger();

// Add Zerra logger to ASP.NET logging
builder.Logging.ClearProviders();
builder.Logging.AddZerraLogger(zerraLogger);

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

// Multiple gateways (different buses)
app.UseCqrsApiGateway(route: "/api/users");
app.UseCqrsApiGateway(route: "/api/orders");
```

### Content Type Support

The API Gateway automatically supports multiple content types:

```csharp
// Client specifies content type
Content-Type: application/json              // Standard JSON
Content-Type: application/jsonnameless     // Compact nameless JSON
Content-Type: application/octet-stream     // Binary (ZerraByteSerializer)

// Gateway uses the same serializer for request and response
```

### CORS Configuration

CORS is automatically enabled with permissive defaults:

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: *
Access-Control-Allow-Headers: *
```

For production, configure ASP.NET CORS middleware before the gateway:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins("https://myapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("MyPolicy");
app.UseCqrsApiGateway(route: "/api/cqrs");
```

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
bus.AddHandler<IMyCommands>(new MyCommandHandler());
bus.AddHandler<IMyQueries>(new MyQueryHandler());

builder.Services.AddSingleton(bus);
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

### Browser Client (JavaScript)

```javascript
// Call a query
async function getUser(userId) {
    const response = await fetch('https://myapp.azurewebsites.net/api/cqrs', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': 'my-secret-key'
        },
        body: JSON.stringify({
            MessageType: 'MyApp.Queries.GetUserQuery',
            Message: {
                UserId: userId
            }
        })
    });

    if (!response.ok) {
        throw new Error('Query failed');
    }

    return await response.json();
}

// Dispatch a command
async function createUser(email, name) {
    const response = await fetch('https://myapp.azurewebsites.net/api/cqrs', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': 'my-secret-key'
        },
        body: JSON.stringify({
            MessageType: 'MyApp.Commands.CreateUserCommand',
            Message: {
                Email: email,
                Name: name
            }
        })
    });

    if (!response.ok) {
        throw new Error('Command failed');
    }

    return await response.json();
}

// Usage
const user = await getUser('12345');
console.log('User:', user);

const result = await createUser('user@example.com', 'John Doe');
console.log('Created user:', result);
```

### Mobile App Integration

```csharp
// Xamarin/MAUI client
using System.Net.Http;
using System.Text.Json;

public class CqrsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public CqrsClient(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl;
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<TResult> CallAsync<TResult>(string messageType, object message)
    {
        var request = new
        {
            MessageType = messageType,
            Message = message
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/cqrs", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResult>(json);
    }
}

// Usage
var client = new CqrsClient("https://myapp.azurewebsites.net", "my-secret-key");
var user = await client.CallAsync<User>(
    "MyApp.Queries.GetUserQuery",
    new { UserId = "12345" }
);
```

### Front End Scripts (JavaScript/TypeScript)

Zerra provides pre-built JavaScript and TypeScript utilities in the **Front End Scripts** solution folder to simplify browser integration with the CQRS API Gateway.

#### Included Files

**JavaScript:**
- [`Bus.js`](../Front%20End%20Scripts/JavaScript/Bus.js) - CQRS client with JSON and Nameless JSON deserialization
- [`BusRoutes.js`](../Front%20End%20Scripts/JavaScript/BusRoutes.js) - Route configuration
- [`JavaScriptModels.tt`](../Front%20End%20Scripts/JavaScript/JavaScriptModels.tt) - T4 template to generate client-side models from .NET types

**TypeScript:**
- [`Bus.ts`](../Front%20End%20Scripts/TypeScript/Bus.ts) - Typed CQRS client with full IntelliSense support
- [`BusConfig.ts`](../Front%20End%20Scripts/TypeScript/BusConfig.ts) - Type-safe route configuration
- [`TypeScriptModels.tt`](../Front%20End%20Scripts/TypeScript/TypeScriptModels.tt) - T4 template to generate TypeScript interfaces

**Binaries (Required for T4):**
- [`Zerra.T4.dll`](../Front%20End%20Scripts/Binaries/Zerra.T4.dll) - Pre-built T4 code generation library
- [`Zerra.T4.pdb`](../Front%20End%20Scripts/Binaries/Zerra.T4.pdb) - Debug symbols (optional)
- [`Zerra.T4.xml`](../Front%20End%20Scripts/Binaries/Zerra.T4.xml) - XML documentation (optional)

> **Note**: The `Zerra.T4.dll` is automatically built from the `Zerra.T4` project and copied to this folder. These binaries are always up-to-date with the latest framework build. Copy the entire `Binaries` folder to your solution to use the T4 templates.

#### Auto-Generate Client Models with T4

The T4 templates automatically generate JavaScript/TypeScript models from your .NET CQRS types.

**Step 1: Copy Files to Your Solution**

Copy the Front End Scripts files to your solution:
```
YourSolution/
├── Scripts/
│   ├── Binaries/
│   │   ├── Zerra.T4.dll
│   │   ├── Zerra.T4.pdb
│   │   └── Zerra.T4.xml
│   └── TypeScriptModels.tt (or JavaScriptModels.tt)
└── YourWebProject/
    └── src/services/ (output location)
```

**Step 2: Update T4 Template Assembly Path**

Edit the `.tt` file to reference the correct path to `Zerra.T4.dll`:

```csharp
<#@ assembly name="Scripts\Binaries\Zerra.T4.dll" #>
```

Or use an absolute path if needed:
```csharp
<#@ assembly name="C:\MyProject\Scripts\Binaries\Zerra.T4.dll" #>
```

**Original template reference (example):**
```csharp
<#@ assembly name="Framework\Zerra.T4.TestDev\bin\Release\net48\Zerra.T4.dll" #>
```

**Step 3: Configure Project Build**

Add to your web project `.csproj` (see [`Help-ProjectBuildT4.txt`](../Front%20End%20Scripts/JavaScript/Help-ProjectBuildT4.txt)):

```xml
<!--Section to Build T4 into the UI project-->
<Import Project="$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v17.0\TextTemplating\Microsoft.TextTemplating.targets" />
<PropertyGroup>
    <TransformOnBuild>true</TransformOnBuild>
    <OverwriteReadOnlyOutputFiles>true</OverwriteReadOnlyOutputFiles>
    <TransformOutOfDateOnly>false</TransformOutOfDateOnly>
</PropertyGroup>
<ItemGroup>
    <None Include="..\Scripts\TypeScriptModels.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <OutputFilePath>..\MyWebApp\src\services</OutputFilePath>
        <LastGenOutput>TypeScriptModels.ts</LastGenOutput>
    </None>
</ItemGroup>
```

**Step 4: Build Your Solution**

The T4 template will automatically run during build and generate the TypeScript/JavaScript models based on your CQRS types (queries, commands, events, and DTOs).

#### Using the JavaScript Bus

```html
<!-- Include jQuery (required dependency) -->
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>

<!-- Include generated models and Bus -->
<script src="JavaScriptModels.js"></script>
<script src="Bus.js"></script>
<script src="BusRoutes.js"></script>

<script>
// Configure routes
BusRoutes["Gateway"] = "https://myapp.azurewebsites.net/api/cqrs";

// Set custom headers (e.g., API key)
Bus.setHeader("X-API-Key", "my-secret-key");

// Call a query with standard JSON
Bus.Call(
    "MyApp.Queries.IUserQueries",           // Provider type
    "GetUser",                               // Method name
    [{ UserId: "12345" }],                   // Arguments array
    null,                                    // Model type (null = standard JSON)
    false,                                   // Has many (array result)
    function(result) {                       // Success callback
        console.log("User:", result);
        document.getElementById("userName").innerText = result.Name;
    },
    function(error) {                        // Error callback
        console.error("Error:", error);
    }
);

// Call a query with Nameless JSON (compact)
Bus.Call(
    "MyApp.Queries.IUserQueries",
    "GetAllUsers",
    [],
    ModelTypeDictionary["User"],             // Model type for deserialization
    true,                                    // Has many (array result)
    function(users) {
        console.log("Users:", users);
        // Result automatically deserialized from compact JSON arrays
        users.forEach(u => console.log(u.Name));
    },
    function(error) {
        console.error("Error:", error);
    }
);

// Dispatch a command
const command = {
    CommandType: "MyApp.Commands.CreateUserCommand",
    CommandWithResult: true,
    Email: "user@example.com",
    Name: "John Doe"
};

Bus.Dispatch(
    command,
    function(result) {
        console.log("User created:", result);
    },
    function(error) {
        console.error("Error:", error);
    }
);

// Global error handler
BusFail = function(message, url) {
    alert("CQRS Error: " + message);
};
</script>
```

#### Using the TypeScript Bus

```typescript
import { Bus } from "./Bus";
import { SetBusRoute, SetBusFailCallback } from "./BusConfig";
import { User, GetUserQuery, CreateUserCommand } from "./TypeScriptModels";

// Configure routes
SetBusRoute("Gateway", "https://myapp.azurewebsites.net/api/cqrs");

// Set global error handler
SetBusFailCallback((message) => {
    console.error("Bus error:", message);
    alert(message);
});

// Call a query with full type safety
const query = new GetUserQuery();
query.UserId = "12345";

const user = await Bus.Call<User>(
    "MyApp.Queries.IUserQueries",
    "GetUser",
    [query],
    User,           // Type for IntelliSense and deserialization
    false
);

console.log(user.Name); // Full IntelliSense support

// Call query returning array
const users = await Bus.Call<User[]>(
    "MyApp.Queries.IUserQueries",
    "GetAllUsers",
    [],
    User,
    true            // Has many = true for arrays
);

users.forEach(u => console.log(u.Email));

// Dispatch command
const command = new CreateUserCommand();
command.Email = "user@example.com";
command.Name = "John Doe";

const result = await Bus.Dispatch(command);
console.log("Created user:", result);
```

#### Nameless JSON Support

The Bus utilities automatically detect and deserialize Nameless JSON responses:

**Server Configuration:**
```csharp
var options = new JsonSerializerOptions { Nameless = true };
var serializer = new ZerraJsonSerializer(options);
app.UseCqrsApiGateway();
```

**Client Code:**
```javascript
// Bus automatically detects "application/jsonnameless" content type
// and deserializes compact arrays back to objects
Bus.Call(
    "MyApp.Queries.IUserQueries",
    "GetUsers",
    [],
    ModelTypeDictionary["User"],  // Required for nameless deserialization
    true,
    function(users) {
        // Compact JSON: [[1,"John","john@example.com"],[2,"Jane","jane@example.com"]]
        // Deserialized to: [{ Id: 1, Name: "John", Email: "john@example.com" }, ...]
        console.log(users);
    }
);
```

The Bus utilities handle:
- ✅ Date serialization/deserialization (ISO 8601 with timezone)
- ✅ Nested object deserialization
- ✅ Array property deserialization
- ✅ Automatic Nameless JSON detection and decoding
- ✅ Custom header support (authentication, API keys)
- ✅ Global error handling

### Microservices Gateway

Use as a gateway for microservices communication:

```csharp
// Gateway Service (ASP.NET)
var builder = WebApplication.CreateBuilder(args);

ISerializer serializer = new ZerraJsonSerializer();
var bus = Bus.New("GatewayService");

// Connect to backend services
var userServiceClient = new TcpCqrsClient("user-service:9001", serializer, null, null);
bus.AddCommandProducer<IUserCommands>(userServiceClient);
bus.AddQueryClient<IUserQueries>(userServiceClient);

var orderServiceClient = new TcpCqrsClient("order-service:9002", serializer, null, null);
bus.AddCommandProducer<IOrderCommands>(orderServiceClient);
bus.AddQueryClient<IOrderQueries>(orderServiceClient);

builder.Services.AddSingleton(bus);
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

### 3. Use HTTPS in Production

```csharp
// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443"
      }
    }
  }
}
```

### 4. Configure CORS Properly

```csharp
// ✅ Good - specific origins in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://myapp.com", "https://mobile.myapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 5. Use Rate Limiting

```csharp
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

var app = builder.Build();
app.UseRateLimiter();
app.UseCqrsApiGateway();
```

### 6. Log Gateway Activity

```csharp
ILogger log = new Logger();
IBusLogger busLog = new BusLogger();

var bus = Bus.New("MyService", log: log, busLog: busLog);

// BusLogger will track all commands/queries/events through the gateway
```

### 7. Use Front End Scripts for Browser Clients

```csharp
// ✅ Good - use provided Bus.js/Bus.ts utilities
// Located in solution folder: "Front End Scripts"
// - Handles date serialization
// - Supports Nameless JSON
// - T4 templates generate type-safe models
// - Simplifies error handling

// Copy Bus.js/Bus.ts and generated models to your web project
// See examples in "Front End Scripts (JavaScript/TypeScript)" section
```
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

- ❌ Internal services only - Use `TcpCqrsServer` or message brokers instead
- ❌ High-performance internal communication - Binary transports are faster
- ❌ Message broker patterns - Use Kafka, RabbitMQ, or Azure Service Bus

## Troubleshooting

### Gateway Returns 400 Bad Request

**Problem**: API Gateway returns 400 for valid requests

**Solutions**:
- Verify `MessageType` is the fully-qualified type name (e.g., `MyApp.Commands.CreateUserCommand`)
- Check that the message type is registered with the bus
- Ensure request body matches the command/query/event structure
- Verify `Content-Type` header matches the serializer

### Authorization Fails

**Problem**: API Gateway returns 401 Unauthorized

**Solutions**:
- Verify `ICqrsAuthorizer` is registered in DI container
- Check authorization headers are included in client requests
- Ensure `Authorize()` method doesn't throw exceptions for valid requests
- Use a debugger to inspect the headers received

### CORS Errors in Browser

**Problem**: Browser shows CORS policy errors

**Solutions**:
- Configure ASP.NET CORS middleware before the gateway
- Verify `Access-Control-Allow-Origin` includes your client domain
- Check that preflight OPTIONS requests are handled (gateway does this automatically)
- Ensure client sends correct `Origin` header

### Performance Issues

**Problem**: Gateway is slow under load

**Solutions**:
- Use `ZerraByteSerializer` if clients support binary (faster than JSON)
- Enable response compression in ASP.NET
- Configure Kestrel limits appropriately
- Consider using message brokers for high-volume scenarios
- Add rate limiting to prevent abuse

## See Also

- [Server Setup](ServerSetup.md) - Configure CQRS servers
- [Client Setup](ClientSetup.md) - Configure CQRS clients
- [JsonSerializer](JsonSerializer.md) - JSON serialization for external clients
- [Encryptors](Encryptors.md) - Secure message encryption
- [Logging](Logging.md) - Implement logging for gateway activity
