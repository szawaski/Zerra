[← Back to Documentation](README.md)

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
ILogger logger = new Logger();
IBusLogger busLogger = new BusLogger();

// Configure services
var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository(connectionString));
busServices.AddService<IEmailService>(new EmailService(smtpConfig));

// Create the bus
var bus = Bus.New(
    service: "UserService",
    log: logger,
    busLog: busLogger,
    busScopes: busServices
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
bus.AddEventConsumer<IUserEventHandler>(server);

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
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
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
bus.AddEventConsumer<IUserEventHandler>(server);

Console.WriteLine($"User Service started on {serverAddress}");
Console.WriteLine("Press Ctrl+C to stop...");

// Setup cancellation token for graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    // Wait for shutdown signal
    await bus.WaitForExitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutdown requested...");
}
finally
{
    // Cleanup
    (server as IDisposable)?.Dispose();
    Console.WriteLine("User Service stopped");
}
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
builder.Services.AddSingleton<IBus>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    // Get configuration
    var serverAddress = configuration["Zerra:ServerAddress"];
    var encryptionKey = configuration["Zerra:EncryptionKey"];
    var serviceName = configuration["Zerra:ServiceName"];

    // Create components
    ISerializer serializer = new ZerraByteSerializer();
    IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
    ILogger logger = new AspNetCoreLogger(serviceProvider.GetRequiredService<ILogger<Program>>());
    IBusLogger busLogger = new AspNetCoreBusLogger();

    // Create services
    var busServices = new BusServices();
    busServices.AddService<IUserRepository>(serviceProvider.GetRequiredService<IUserRepository>());
    busServices.AddService<IEmailService>(serviceProvider.GetRequiredService<IEmailService>());
    busServices.AddService<ICacheService>(serviceProvider.GetRequiredService<ICacheService>());

    // Create bus
    var bus = Bus.New(
        service: serviceName,
        log: logger,
        busLog: busLogger,
        busScopes: busServices
    );

    // Register handlers
    bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
    bus.AddHandler<IUserQueries>(new UserQueryHandler());
    bus.AddHandler<IUserEventHandler>(new UserEventHandler());

    // Configure network server
    var server = new TcpCqrsServer(serverAddress, serializer, encryptor, logger);
    bus.AddCommandConsumer<IUserCommandHandler>(server);
    bus.AddQueryServer<IUserQueries>(server);
    bus.AddEventConsumer<IUserEventHandler>(server);

    return bus;
});

// Add background service to keep bus running
builder.Services.AddHostedService<BusHostedService>();

var host = builder.Build();
await host.RunAsync();

// Background service implementation
public class BusHostedService : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<BusHostedService> logger;

    public BusHostedService(IBus bus, ILogger<BusHostedService> logger)
    {
        this.bus = bus;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bus service starting...");

        try
        {
            await bus.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Bus service stopping...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bus service error");
            throw;
        }
    }
}
```

## Network Transport Options

### TCP CQRS Server

High-performance binary protocol over TCP:

```csharp
var server = new TcpCqrsServer(
    address: "localhost:9001",
    serializer: serializer,
    encryptor: encryptor,
    log: logger
);

bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
bus.AddEventConsumer<IUserEventHandler>(server);
```

### HTTP CQRS Server

HTTP-based protocol for firewall-friendly communication:

```csharp
var server = new HttpCqrsServer(
    address: "http://localhost:8080",
    serializer: serializer,
    encryptor: encryptor,
    log: logger
);

bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);
```

### Multiple Protocols

Support both TCP and HTTP simultaneously:

```csharp
// TCP server for high-performance clients
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(tcpServer);
bus.AddQueryServer<IUserQueries>(tcpServer);

// HTTP server for web clients
var httpServer = new HttpCqrsServer("http://localhost:8080", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(httpServer);
bus.AddQueryServer<IUserQueries>(httpServer);
```

## Message Broker Integration

### Kafka Consumer

```csharp
using Zerra.CQRS.Kafka;

// Configure Kafka consumer
var kafkaConfig = new KafkaConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "user-service-group",
    Topic = "user-commands"
};

var kafkaConsumer = new KafkaConsumer(kafkaConfig, serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(kafkaConsumer);
bus.AddEventConsumer<IUserEventHandler>(kafkaConsumer);
```

### RabbitMQ Consumer

```csharp
using Zerra.CQRS.RabbitMQ;

// Configure RabbitMQ consumer
var rabbitConfig = new RabbitMQConsumerConfig
{
    HostName = "localhost",
    Port = 5672,
    QueueName = "user-commands",
    UserName = "guest",
    Password = "guest"
};

var rabbitConsumer = new RabbitMQConsumer(rabbitConfig, serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(rabbitConsumer);
bus.AddEventConsumer<IUserEventHandler>(rabbitConsumer);
```

### Azure Service Bus Consumer

```csharp
using Zerra.CQRS.AzureServiceBus;

// Configure Azure Service Bus consumer
var asbConfig = new AzureServiceBusConsumerConfig
{
    ConnectionString = configuration["AzureServiceBus:ConnectionString"],
    QueueName = "user-commands"
};

var asbConsumer = new AzureServiceBusConsumer(asbConfig, serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(asbConsumer);
bus.AddEventConsumer<IUserEventHandler>(asbConsumer);
```

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

// Timeout settings
var timeoutConfig = zerraConfig.GetSection("Timeouts");
var defaultCallTimeout = timeoutConfig.GetValue<int>("DefaultCall");
var defaultDispatchTimeout = timeoutConfig.GetValue<int>("DefaultDispatch");
var defaultDispatchAwaitTimeout = timeoutConfig.GetValue<int>("DefaultDispatchAwait");

// Create bus with configuration
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices,
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
    service: "UserService",
    log: logger,
    busLog: busLogger,
    busScopes: busServices,
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

Use cancellation token for graceful shutdown:

```csharp
using var cts = new CancellationTokenSource();

// Handle Ctrl+C
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await bus.WaitForExitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    logger.Info("Shutdown initiated");
}
```

### SIGTERM Handler (Docker/Kubernetes)

```csharp
using var cts = new CancellationTokenSource();

// Handle SIGTERM for containerized environments
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    logger.Info("SIGTERM received, initiating shutdown");
    cts.Cancel();
};

await bus.WaitForExitAsync(cts.Token);
```

## Microservices Architecture

### Service per Domain

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
