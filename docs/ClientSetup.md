[← Back to Documentation](README.md)

# Client Setup

This guide covers configuring a client-side application using Zerra CQRS framework in `Program.cs` or your application entry point.

## Overview

A client application in Zerra:
- Creates a bus instance for message routing
- Configures serialization and encryption
- Registers command producers for remote command dispatching
- Registers query clients for remote query calling
- Optionally registers event producers for remote event publishing
- Typically does not register handlers (unless running hybrid mode)

## Basic Client Setup

### Minimal Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configure components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new Logger();
IBusLogger busLogger = new BusLogger();
var busServices = new BusServices();

// Create the bus
var bus = Bus.New(
    service: "ClientApp",
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Create TCP client
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);

// Register command producer
bus.AddCommandProducer<IUserCommandHandler>(client);

// Register query client
bus.AddQueryClient<IUserQueries>(client);

// Now dispatch commands and call queries
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
var users = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
```

## Complete Program.cs Example

### Console Application

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain.Commands;
using MyApp.Domain.Queries;
using System.Threading;

// Setup configuration (use your configuration system)
var serverAddress = "localhost:9001";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyClientApp";

// Create serializer
ISerializer serializer = new ZerraByteSerializer();

// Create encryptor
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);

// Create loggers
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();

// Create bus services (typically empty for pure clients)
var busServices = new BusServices();

// Create the bus
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Create and configure network client
var client = new TcpCqrsClient(serverAddress, serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandler>(client);
bus.AddQueryClient<IUserQueries>(client);

// Application logic
try
{
    Console.WriteLine("Client started. Calling remote services...");

    // Dispatch command
    await bus.DispatchAwaitAsync(new CreateUserCommand 
    { 
        Email = "newuser@example.com" 
    });
    Console.WriteLine("User created successfully");

    // Call query
    var users = await bus.Call<IUserQueries>().GetActiveUsers(CancellationToken.None);
    Console.WriteLine($"Found {users.Count} active users");

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    logger.Error("Application error", ex);
}
finally
{
    // Cleanup
    (client as IDisposable)?.Dispose();
}
```

### ASP.NET Core Web Application

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain.Commands;
using MyApp.Domain.Queries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Zerra Bus as singleton
builder.Services.AddSingleton<IBus>(serviceProvider =>
{
    // Get configuration
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var serverAddress = configuration["Zerra:ServerAddress"] ?? "localhost:9001";
    var encryptionKey = configuration["Zerra:EncryptionKey"];
    var serviceName = configuration["Zerra:ServiceName"] ?? "WebClient";

    // Create components
    ISerializer serializer = new ZerraByteSerializer();
    IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
    ILogger logger = new AspNetCoreLogger(serviceProvider.GetRequiredService<ILogger<Program>>());
    IBusLogger busLogger = new AspNetCoreBusLogger();
    var busServices = new BusServices();

    // Create bus
    var bus = Bus.New(
        service: serviceName,
        log: logger,
        busLog: busLogger,
        busScopes: busServices
    );

    // Configure network client
    var client = new TcpCqrsClient(serverAddress, serializer, encryptor, logger);
    bus.AddCommandProducer<IUserCommandHandler>(client);
    bus.AddQueryClient<IUserQueries>(client);

    return bus;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Controller Example

```csharp
using Microsoft.AspNetCore.Mvc;
using Zerra.CQRS;
using MyApp.Domain.Commands;
using MyApp.Domain.Queries;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IBus bus;

    public UsersController(IBus bus)
    {
        this.bus = bus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        await bus.DispatchAwaitAsync(new CreateUserCommand 
        { 
            Email = request.Email 
        });

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await bus.Call<IUserQueries>().GetUserById(id, cancellationToken);
        if (user == null)
            return NotFound();

        return Ok(user);
    }
}
```

## Network Transport Options

### TCP CQRS Client

High-performance binary protocol over TCP:

```csharp
var client = new TcpCqrsClient(
    address: "localhost:9001",
    serializer: serializer,
    encryptor: encryptor,
    log: logger
);

bus.AddCommandProducer<IUserCommandHandler>(client);
bus.AddQueryClient<IUserQueries>(client);
```

### HTTP CQRS Client

HTTP-based protocol for firewall-friendly communication:

```csharp
var client = new HttpCqrsClient(
    address: "http://localhost:8080",
    serializer: serializer,
    encryptor: encryptor,
    log: logger
);

bus.AddCommandProducer<IUserCommandHandler>(client);
bus.AddQueryClient<IUserQueries>(client);
```

### Multiple Servers

Connect to multiple backend services:

```csharp
// User service
var userClient = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandler>(userClient);
bus.AddQueryClient<IUserQueries>(userClient);

// Order service
var orderClient = new TcpCqrsClient("localhost:9002", serializer, encryptor, logger);
bus.AddCommandProducer<IOrderCommandHandler>(orderClient);
bus.AddQueryClient<IOrderQueries>(orderClient);

// Product service
var productClient = new HttpCqrsClient("http://productservice:8080", serializer, encryptor, logger);
bus.AddQueryClient<IProductQueries>(productClient);
```

## Configuration Options

### appsettings.json

```json
{
  "Zerra": {
    "ServiceName": "MyClientApp",
    "ServerAddress": "localhost:9001",
    "EncryptionKey": "your-secure-key-here",
    "UseEncryption": true,
    "UseBinarySerializer": true,
    "DefaultTimeout": 30000,
    "LogLevel": "Information"
  }
}
```

### Loading Configuration

```csharp
// Read from configuration
var configuration = builder.Configuration;
var zerraConfig = configuration.GetSection("Zerra");

var serviceName = zerraConfig["ServiceName"];
var serverAddress = zerraConfig["ServerAddress"];
var encryptionKey = zerraConfig["EncryptionKey"];
var useEncryption = zerraConfig.GetValue<bool>("UseEncryption");
var useBinarySerializer = zerraConfig.GetValue<bool>("UseBinarySerializer");
var defaultTimeout = zerraConfig.GetValue<int>("DefaultTimeout");

// Create components based on configuration
ISerializer serializer = useBinarySerializer 
    ? new ZerraByteSerializer() 
    : new ZerraJsonSerializer();

IEncryptor? encryptor = useEncryption 
    ? new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix)
    : null;

var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices,
    defaultCallTimeout: defaultTimeout
);
```

## Hybrid Client-Server Setup

A hybrid application acts as both client and server:

```csharp
// Create bus
var bus = Bus.New("HybridApp", logger, busLogger, busServices);

// Register local handlers
bus.AddHandler<ILocalCommandHandler>(new LocalCommandHandler());
bus.AddHandler<ILocalQueries>(new LocalQueryHandler());

// Register remote producers/clients
var remoteClient = new TcpCqrsClient("remoteserver:9001", serializer, encryptor, logger);
bus.AddCommandProducer<IRemoteCommandHandler>(remoteClient);
bus.AddQueryClient<IRemoteQueries>(remoteClient);

// Now you can:
// - Handle local commands/queries with local handlers
await bus.DispatchAwaitAsync(new LocalCommand());
var localData = await bus.Call<ILocalQueries>().GetLocalData();

// - Dispatch remote commands/queries to remote server
await bus.DispatchAwaitAsync(new RemoteCommand());
var remoteData = await bus.Call<IRemoteQueries>().GetRemoteData();
```

## Timeout Configuration

Configure timeouts for remote calls:

```csharp
var bus = Bus.New(
    service: "ClientApp",
    log: logger,
    busLog: busLogger,
    busScopes: busServices,
    defaultCallTimeout: 30000,              // 30 seconds for queries
    defaultDispatchTimeout: 5000,           // 5 seconds for fire-and-forget commands
    defaultDispatchAwaitTimeout: 60000      // 60 seconds for commands awaiting completion
);
```

## Error Handling

### Connection Failures

```csharp
try
{
    var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
    bus.AddCommandProducer<IUserCommandHandler>(client);

    await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "test@example.com" });
}
catch (TimeoutException ex)
{
    logger.Error("Server did not respond in time", ex);
}
catch (Exception ex)
{
    logger.Error("Failed to connect to server", ex);
}
```

### Query Failures

```csharp
try
{
    var users = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
}
catch (TimeoutException ex)
{
    logger.Error("Query timed out", ex);
    // Return cached data or default
}
catch (Exception ex)
{
    logger.Error("Query failed", ex);
    // Handle error
}
```

## See Also

- [Server Setup](ServerSetup.md) - Configure server-side applications
- [Serializers](Serializers.md) - Choose and configure serializers
- [Encryptors](Encryptors.md) - Configure encryption
- [Logging](Logging.md) - Implement logging
- [Service Injection](ServiceInjection.md) - Manage dependencies
- [Queries](Queries.md) - Call remote queries
- [Commands](Commands.md) - Dispatch remote commands
- [Events](Events.md) - Publish remote events
