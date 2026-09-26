[← Back to Documentation](Index.md)

# Server Setup

This guide covers configuring a server-side application using Zerra CQRS framework in `Program.cs` or your application entry point.

## Overview

A server application in Zerra:
- Creates a bus instance for message routing
- Configures serialization and encryption
- Registers command handlers for processing commands
- Registers query handlers for responding to queries
- Registers event handlers for processing events
- Configures command consumers to receive commands from message brokers or clients
- Configures query servers to respond to query requests
- Runs continuously to process incoming messages

## Basic Server Setup

### Minimal Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Handlers;
using MyApp.Services;

// Configure components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();          // your ILogger implementation (see Logging.md)
IBusLogger busLogger = new ConsoleBusLogger(); // your IBusLogger implementation (optional)

// Configure services
var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository(connectionString));
busServices.AddService<IEmailService>(new EmailService(smtpConfig));

// Create the bus
var bus = Bus.New(
    serviceName: "UserService",
    log: logger,
    busLog: busLogger,
    busServices: busServices
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Create TCP server
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);

// Register consumers and servers
bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);

// Wait for shutdown signal
await bus.WaitForExitAsync(cancellationToken);
```

## Complete Program.cs Example

### Console Application Server

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain;
using MyApp.Handlers;
using MyApp.Services;
using MyApp.Repositories;
using System.Threading;

Console.WriteLine("Starting User Service...");

// Load configuration
var serverAddress = Environment.GetEnvironmentVariable("SERVER_ADDRESS") ?? "localhost:9001";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? throw new InvalidOperationException("ENCRYPTION_KEY not set");
var serviceName = "UserService";
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? throw new InvalidOperationException("CONNECTION_STRING not set");

// Create serializer and encryptor
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);

// Create loggers
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();

// Create and configure services
var userRepository = new UserRepository(connectionString);
var emailService = new EmailService(GetSmtpConfig());
var cacheService = new RedisCacheService(GetRedisConfig());

var busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);
busServices.AddService<ICacheService>(cacheService);

// Create the bus
var bus = Bus.New(
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
);

// Create and register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Create network server
var server = new TcpCqrsServer(serverAddress, serializer, encryptor, logger);

// Register consumers and servers
bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);

Console.WriteLine($"User Service started on {serverAddress}");
Console.WriteLine("Press Ctrl+C to stop...");

// Waits for Ctrl+C, SIGTERM, or process exit, then stops the bus and disposes all producers, consumers, clients, and servers
await bus.WaitForExitAsync();
Console.WriteLine("User Service stopped");
```

### ASP.NET Core Worker Service

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain;
using MyApp.Handlers;
using MyApp.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register repositories and services
builder.Services.AddSingleton<IUserRepository>(sp => 
    new UserRepository(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Configure Zerra Bus
// Bus.New returns IBusSetup (which extends IBus); register it as IBusSetup so the hosted service can
// call WaitForExitAsync, and also expose it as IBus for components that only dispatch and call.
builder.Services.AddSingleton<IBusSetup>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    // Get configuration
    var serverAddress = configuration["Zerra:ServerAddress"];
    var encryptionKey = configuration["Zerra:EncryptionKey"];
    var serviceName = configuration["Zerra:ServiceName"];

    // Create components
    ISerializer serializer = new ZerraByteSerializer();
    IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
    // Your Zerra.Logging.ILogger adapter over Microsoft.Extensions.Logging (fully qualified to avoid ambiguity)
    Zerra.Logging.ILogger logger = new AspNetCoreLogger(serviceProvider.GetRequiredService<ILogger<Program>>());
    IBusLogger busLogger = new AspNetCoreBusLogger();

    // Create services
    var busServices = new BusServices();
    busServices.AddService<IUserRepository>(serviceProvider.GetRequiredService<IUserRepository>());
    busServices.AddService<IEmailService>(serviceProvider.GetRequiredService<IEmailService>());
    busServices.AddService<ICacheService>(serviceProvider.GetRequiredService<ICacheService>());

    // Create bus
    var bus = Bus.New(
        serviceName: serviceName,
        log: logger,
        busLog: busLogger,
        busServices: busServices
    );

    // Register handlers
    bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
    bus.AddHandler<IUserQueries>(new UserQueryHandler());
    bus.AddHandler<IUserEventHandler>(new UserEventHandler());

    // Configure network server
    var server = new TcpCqrsServer(serverAddress, serializer, encryptor, logger);
    bus.AddCommandConsumer<IUserCommandHandler>(server);
    bus.AddQueryServer<IUserQueries>(server);
    bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);

    return bus;
});
builder.Services.AddSingleton<IBus>(sp => sp.GetRequiredService<IBusSetup>());

// Add background service to keep bus running
builder.Services.AddHostedService<BusHostedService>();

var host = builder.Build();
await host.RunAsync();

// Background service implementation
public class BusHostedService : BackgroundService
{
    private readonly IBusSetup bus;
    private readonly ILogger<BusHostedService> logger;

    public BusHostedService(IBusSetup bus, ILogger<BusHostedService> logger)
    {
        this.bus = bus;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bus service starting...");

        // Returns when the host stops (stoppingToken is cancelled) or the process exits,
        // after stopping and disposing all producers, consumers, clients, and servers
        await bus.WaitForExitAsync(stoppingToken);

        logger.LogInformation("Bus service stopped");
    }
}
```

## Network Transport Options

### TCP CQRS Server

High-performance binary protocol over TCP:

```csharp
var server = new TcpCqrsServer(
    serverUrl: "localhost:9001",
    serializer: serializer,
    encryptor: encryptor,
    log: logger
);

bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);
```

### HTTP CQRS Server

HTTP-based protocol for firewall-friendly communication:

```csharp
var server = new HttpCqrsServer(
    serverUrl: "localhost:8080",
    serializer: serializer,
    encryptor: encryptor,
    authorizer: null,       // optional ICqrsAuthorizer to validate request headers
    allowOrigins: null,     // optional CORS origins
    log: logger
);

bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);
```

### Multiple Protocols

Support both TCP and HTTP simultaneously:

```csharp
// TCP server for high-performance clients
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(tcpServer);
bus.AddQueryServer<IUserQueries>(tcpServer);

// HTTP server for web clients
var httpServer = new HttpCqrsServer("localhost:8080", serializer, encryptor, null, null, logger);
bus.AddCommandConsumer<IUserCommandHandler>(httpServer);
bus.AddQueryServer<IUserQueries>(httpServer);
```

## Message Broker Integration

Each broker consumer handles both commands and events. Topics/queues are named after the handler interface (e.g. `IUserCommandHandler`), prefixed with the optional `environment`.

`AddEventConsumer` requires an `EventConsumerMode`, there is no default. `PerReplica` gives every replica of the service its own copy of each event, which is what the samples above use. `PerService` has the replicas compete for them instead, the way they do for commands - it is the subscriber's own choice and changes nothing for the publisher or for the other subscribers. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

```csharp
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerService);
```

### Kafka Consumer

```csharp
using Zerra.CQRS.Kafka;

var kafkaConsumer = new KafkaConsumer(
    host: "localhost:9092",   // bootstrap servers
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: "dev",       // optional topic prefix
    userName: null,           // optional SASL username
    password: null            // optional SASL password
);
bus.AddCommandConsumer<IUserCommandHandler>(kafkaConsumer);
bus.AddEventConsumer<IUserEventHandler>(kafkaConsumer, EventConsumerMode.PerReplica);
```

### RabbitMQ Consumer

```csharp
using Zerra.CQRS.RabbitMQ;

var rabbitConsumer = new RabbitMQConsumer(
    host: "localhost",        // RabbitMQ host name
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: "dev"        // optional exchange/queue prefix
);
bus.AddCommandConsumer<IUserCommandHandler>(rabbitConsumer);
bus.AddEventConsumer<IUserEventHandler>(rabbitConsumer, EventConsumerMode.PerReplica);
```

### Azure Service Bus Consumer

```csharp
using Zerra.CQRS.AzureServiceBus;

var asbConsumer = new AzureServiceBusConsumer(
    host: configuration["AzureServiceBus:ConnectionString"],
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: "dev"        // optional queue/topic prefix
);
bus.AddCommandConsumer<IUserCommandHandler>(asbConsumer);
bus.AddEventConsumer<IUserEventHandler>(asbConsumer, EventConsumerMode.PerReplica);
```

See [Kafka Setup](KafkaSetup.md), [RabbitMQ Setup](RabbitMQSetup.md), and [Azure Service Bus Setup](AzureServiceBusSetup.md) for details.

## Configuration Options

### appsettings.json

```json
{
  "Zerra": {
    "ServiceName": "UserService",
    "ServerAddress": "localhost:9001",
    "EncryptionKey": "your-secure-key-here",
    "UseEncryption": true,
    "UseBinarySerializer": true,
    "CommandToReceiveUntilExit": null,
    "Timeouts": {
      "DefaultCall": 30000,
      "DefaultDispatch": 5000,
      "DefaultDispatchAwait": 60000
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Users;Trusted_Connection=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Zerra": "Debug"
    }
  }
}
```

### Loading Configuration

```csharp
var configuration = builder.Configuration;
var zerraConfig = configuration.GetSection("Zerra");

// Load settings
var serviceName = zerraConfig["ServiceName"];
var serverAddress = zerraConfig["ServerAddress"];
var encryptionKey = zerraConfig["EncryptionKey"];
var useEncryption = zerraConfig.GetValue<bool>("UseEncryption");
var useBinarySerializer = zerraConfig.GetValue<bool>("UseBinarySerializer");
var commandsUntilExit = zerraConfig.GetValue<int?>("CommandToReceiveUntilExit");

// Timeout settings (milliseconds in config, TimeSpan for Bus.New)
var timeoutConfig = zerraConfig.GetSection("Timeouts");
var defaultCallTimeout = TimeSpan.FromMilliseconds(timeoutConfig.GetValue<int>("DefaultCall"));
var defaultDispatchTimeout = TimeSpan.FromMilliseconds(timeoutConfig.GetValue<int>("DefaultDispatch"));
var defaultDispatchAwaitTimeout = TimeSpan.FromMilliseconds(timeoutConfig.GetValue<int>("DefaultDispatchAwait"));

// Create bus with configuration
var bus = Bus.New(
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices,
    commandToReceiveUntilExit: commandsUntilExit,
    defaultCallTimeout: defaultCallTimeout,
    defaultDispatchTimeout: defaultDispatchTimeout,
    defaultDispatchAwaitTimeout: defaultDispatchAwaitTimeout
);
```

## Graceful Shutdown

### Command Limit Shutdown

Process a specific number of commands before exiting (useful for container environments with short-lived services or one-time processing, often with Kubernetes KEDA):

```csharp
var bus = Bus.New(
    serviceName: "UserService",
    log: logger,
    busLog: busLogger,
    busServices: busServices,
    commandToReceiveUntilExit: 100  // Exit after processing 100 commands
);

await bus.WaitForExitAsync(cancellationToken);
```

**Use cases:**
- **KEDA autoscaling** - Process a batch of messages then exit, allowing Kubernetes to scale down
- **Job processing** - One-time batch jobs that process N messages and terminate
- **Cost optimization** - Short-lived containers that process a specific workload
- **Testing** - Controlled test scenarios with predictable exit conditions

### Cancellation Token Shutdown

`WaitForExitAsync` returns when the process is exiting or the token is cancelled; it does not throw on cancellation. In both cases it stops the bus and disposes all producers, consumers, clients, and servers before returning.

```csharp
await bus.WaitForExitAsync(cancellationToken);
logger.Info("Shutdown complete");
```

### SIGTERM and Ctrl+C (Docker/Kubernetes)

No extra handler is needed. While waiting, `WaitForExitAsync` registers for SIGTERM and SIGINT (Ctrl+C), cancels the default termination, stops the bus, and returns, so `Main` ends normally. A second signal while the bus is stopping terminates the process right away. On Windows, SIGTERM is the system shutdown or log off event.

```csharp
await bus.WaitForExitAsync();
```

Other process exits, such as `Environment.Exit`, are held in `AppDomain.CurrentDomain.ProcessExit` until the bus has stopped, for up to 30 seconds. On `netstandard2.0` there is no signal registration, so only that `ProcessExit` path applies.

The stop has to finish inside the orchestrator's grace period before it kills the process: 10 seconds for `docker stop`, 30 seconds by default in Kubernetes (`terminationGracePeriodSeconds`).

## Microservices Architecture

### Service per Domain

Each service below typically runs in its own process (`Bus.New` also sets the process-wide static `Bus`):

```csharp
// User Service
var userBus = Bus.New("UserService", logger, busLogger, userServices);
userBus.AddHandler<IUserCommandHandler>(userCommandHandler);
userBus.AddHandler<IUserQueries>(userQueryHandler);
var userServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
userBus.AddCommandConsumer<IUserCommandHandler>(userServer);
userBus.AddQueryServer<IUserQueries>(userServer);

// Order Service
var orderBus = Bus.New("OrderService", logger, busLogger, orderServices);
orderBus.AddHandler<IOrderCommandHandler>(orderCommandHandler);
orderBus.AddHandler<IOrderQueries>(orderQueryHandler);
var orderServer = new TcpCqrsServer("localhost:9002", serializer, encryptor, logger);
orderBus.AddCommandConsumer<IOrderCommandHandler>(orderServer);
orderBus.AddQueryServer<IOrderQueries>(orderServer);

// Product Service
var productBus = Bus.New("ProductService", logger, busLogger, productServices);
productBus.AddHandler<IProductQueries>(productQueryHandler);
var productServer = new TcpCqrsServer("localhost:9003", serializer, encryptor, logger);
productBus.AddQueryServer<IProductQueries>(productServer);
```

## See Also

- [Client Setup](ClientSetup.md) - Configure client-side applications
- [Serializers](Serializers.md) - Choose and configure serializers
- [Encryptors](Encryptors.md) - Configure encryption
- [Logging](Logging.md) - Implement logging
- [Service Injection](ServiceInjection.md) - Manage dependencies
- [Queries](Queries.md) - Implement query handlers
- [Commands](Commands.md) - Implement command handlers
- [Events](Events.md) - Implement event handlers
