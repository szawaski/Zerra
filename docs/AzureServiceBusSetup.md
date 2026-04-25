[← Back to Documentation](Index.md)

# Azure Service Bus Setup

This guide covers using Azure Service Bus as a messaging transport for the Zerra CQRS framework.

## Overview

The Azure Service Bus implementation provides:
- Reliable message delivery using Azure Service Bus queues and topics
- Command producers for dispatching commands to remote handlers
- Event producers for publishing events to subscribers
- Command consumers for receiving and processing commands
- Event consumers for receiving and processing events
- Support for command acknowledgements with automatic retry logic
- Optional message encryption
- Environment-based isolation for multi-environment deployments

## Installation

Add the NuGet package to your project:

```bash
dotnet add package Zerra.CQRS.AzureServiceBus
```

## Prerequisites

- An Azure Service Bus namespace with connection string
- Required NuGet packages: `Zerra.CQRS`, `Zerra.CQRS.AzureServiceBus`

## Client Setup (Producer)

A client application uses `AzureServiceBusProducer` to send commands and publish events to Azure Service Bus.

### Basic Producer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configuration
var connectionString = "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...";
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

// Create Azure Service Bus producer
var producer = new AzureServiceBusProducer(
    host: connectionString,
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
using Zerra.CQRS.AzureServiceBus;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain.Commands;
using MyApp.Domain.Events;

Console.WriteLine("Azure Service Bus Client starting...");

// Configuration
var connectionString = Environment.GetEnvironmentVariable("AZURE_SERVICEBUS_CONNECTION") 
    ?? throw new InvalidOperationException("AZURE_SERVICEBUS_CONNECTION not set");
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyAzureClient";
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

// Create and configure Azure Service Bus producer
var producer = new AzureServiceBusProducer(
    host: connectionString,
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
    await producer.DisposeAsync();
}
```

## Server Setup (Consumer)

A server application uses `AzureServiceBusConsumer` to receive and process commands and events from Azure Service Bus.

### Basic Consumer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.AzureServiceBus;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Handlers;

// Configuration
var connectionString = "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...";
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

// Create Azure Service Bus consumer
var consumer = new AzureServiceBusConsumer(
    host: connectionString,
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
using Zerra.CQRS.AzureServiceBus;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain;
using MyApp.Handlers;
using MyApp.Services;
using MyApp.Repositories;

Console.WriteLine("Starting Azure Service Bus Server...");

// Configuration
var connectionString = Environment.GetEnvironmentVariable("AZURE_SERVICEBUS_CONNECTION") 
    ?? throw new InvalidOperationException("AZURE_SERVICEBUS_CONNECTION not set");
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

// Create Azure Service Bus consumer
var consumer = new AzureServiceBusConsumer(
    host: connectionString,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment
);

// Register consumers
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer);

Console.WriteLine($"Azure Service Bus Server started on {serviceName}");
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
    await consumer.DisposeAsync();
    Console.WriteLine("Azure Service Bus Server stopped");
}
```

## Configuration Options

### AzureServiceBusProducer Constructor

```csharp
public AzureServiceBusProducer(
    string host,              // Azure Service Bus connection string
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message encryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for queues/topics
```

### AzureServiceBusConsumer Constructor

```csharp
public AzureServiceBusConsumer(
    string host,              // Azure Service Bus connection string
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message decryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for queues/topics
```

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same Azure Service Bus namespace by prefixing queue and topic names:

```csharp
// Development environment
var devProducer = new AzureServiceBusProducer(connectionString, serializer, encryptor, logger, "dev");

// Production environment
var prodProducer = new AzureServiceBusProducer(connectionString, serializer, encryptor, logger, "prod");
```

Queues/topics will be named like:
- `dev_UserCommandQueue`
- `prod_UserCommandQueue`

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Kafka Setup](KafkaSetup.md) - Kafka messaging implementation
- [RabbitMQ Setup](RabbitMQSetup.md) - RabbitMQ messaging implementation
