[← Back to Documentation](Index.md)

# RabbitMQ Setup

This guide covers using RabbitMQ as a messaging transport for the Zerra CQRS framework.

## Overview

The RabbitMQ implementation provides:
- Reliable message delivery using RabbitMQ exchanges and queues
- Command producers for dispatching commands to remote handlers
- Event producers for publishing events to subscribers
- Command consumers for receiving and processing commands
- Event consumers for receiving and processing events
- Support for command acknowledgements with automatic retry logic
- Automatic connection recovery
- Optional message encryption
- Environment-based isolation for multi-environment deployments

## Installation

Add the NuGet package to your project:

```bash
dotnet add package Zerra.CQRS.RabbitMQ
```

## Prerequisites

- A running RabbitMQ server
- Required NuGet packages: `Zerra.CQRS`, `Zerra.CQRS.RabbitMQ`

## Client Setup (Producer)

A client application uses `RabbitMQProducer` to send commands and publish events to RabbitMQ exchanges.

### Basic Producer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configuration
var rabbitMQHost = "localhost";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyClientApp";
var environment = "dev"; // Optional: for environment isolation

// Create components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();
var busServices = new BusServices();

// Create the bus
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Create RabbitMQ producer
var producer = new RabbitMQProducer(
    host: rabbitMQHost,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment
);

// Register command producer
bus.AddCommandProducer<IUserCommandHandler>(producer);

// Register event producer (optional)
bus.AddEventProducer<IUserEvents>(producer);

// Now you can dispatch commands and events
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
await bus.DispatchAsync(new UserCreatedEvent { UserId = 123 });
```

### Complete Console Client Example

```csharp
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain.Commands;
using MyApp.Domain.Events;

Console.WriteLine("RabbitMQ Client starting...");

// Configuration
var rabbitMQHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyRabbitMQClient";
var environment = "dev";

// Create components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();
var busServices = new BusServices();

// Create bus
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Create and configure RabbitMQ producer
var producer = new RabbitMQProducer(
    host: rabbitMQHost,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment
);

bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEvents>(producer);

try
{
    Console.WriteLine("Client connected. Sending commands...");

    // Dispatch command with acknowledgement
    await bus.DispatchAwaitAsync(new CreateUserCommand 
    { 
        Email = "newuser@example.com",
        Name = "John Doe"
    });
    Console.WriteLine("User creation command sent successfully");

    // Dispatch event
    await bus.DispatchAsync(new UserCreatedEvent { UserId = 123, Email = "newuser@example.com" });
    Console.WriteLine("Event dispatched successfully");

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    logger.Error("Client error", ex);
}
finally
{
    // Cleanup
    producer.Dispose();
}
```

## Server Setup (Consumer)

A server application uses `RabbitMQConsumer` to receive and process commands and events from RabbitMQ exchanges.

### Basic Consumer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Handlers;

// Configuration
var rabbitMQHost = "localhost";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyServerApp";
var environment = "dev"; // Optional: for environment isolation

// Create components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();

// Configure services
var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository(connectionString));

// Create the bus
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Create RabbitMQ consumer
var consumer = new RabbitMQConsumer(
    host: rabbitMQHost,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment
);

// Register consumers
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer);

// Wait for shutdown signal
await bus.WaitForExitAsync(cancellationToken);
```

### Complete Console Server Example

```csharp
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain;
using MyApp.Handlers;
using MyApp.Services;
using MyApp.Repositories;

Console.WriteLine("Starting RabbitMQ Server...");

// Configuration
var rabbitMQHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "UserService";
var environment = "dev";
var dbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? throw new InvalidOperationException("CONNECTION_STRING not set");

// Create components
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();

// Configure services
var userRepository = new UserRepository(dbConnectionString);
var emailService = new EmailService(GetSmtpConfig());

var busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);

// Create bus
var bus = Bus.New(
    service: serviceName,
    log: logger,
    busLog: busLogger,
    busScopes: busServices
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Create RabbitMQ consumer
var consumer = new RabbitMQConsumer(
    host: rabbitMQHost,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment
);

// Register consumers
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer);

Console.WriteLine($"RabbitMQ Server started on {serviceName}");
Console.WriteLine("Press Ctrl+C to stop...");

// Setup graceful shutdown
using var cts = new CancellationTokenSource();
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
    Console.WriteLine("Shutdown requested...");
}
finally
{
    consumer.Dispose();
    Console.WriteLine("RabbitMQ Server stopped");
}
```

## Configuration Options

### RabbitMQProducer Constructor

```csharp
public RabbitMQProducer(
    string host,              // RabbitMQ server hostname or IP address
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message encryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for exchanges
```

### RabbitMQConsumer Constructor

```csharp
public RabbitMQConsumer(
    string host,              // RabbitMQ server hostname or IP address
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message decryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for exchanges
```

## Advanced Connection Configuration

For advanced RabbitMQ connection settings (username, password, virtual host, port, SSL), you can configure the connection factory before creating the producer/consumer. The basic implementation uses default settings connecting to `localhost:5672` with guest credentials.

For production use, consider creating your own connection string format or extending the implementation to support additional parameters.

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same RabbitMQ server by prefixing exchange names:

```csharp
// Development environment
var devProducer = new RabbitMQProducer("localhost", serializer, encryptor, logger, "dev");

// Production environment
var prodProducer = new RabbitMQProducer("localhost", serializer, encryptor, logger, "prod");
```

Exchanges will be named like:
- `dev_UserCommandExchange`
- `prod_UserCommandExchange`

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Azure Service Bus Setup](AzureServiceBusSetup.md) - Azure Service Bus messaging implementation
- [Kafka Setup](KafkaSetup.md) - Kafka messaging implementation
