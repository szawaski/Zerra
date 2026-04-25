[← Back to Documentation](Index.md)

# Kafka Setup

This guide covers using Apache Kafka as a messaging transport for the Zerra CQRS framework.

## Overview

The Kafka implementation provides:
- High-throughput message delivery using Kafka topics
- Command producers for dispatching commands to remote handlers
- Event producers for publishing events to subscribers
- Command consumers for receiving and processing commands
- Event consumers for receiving and processing events
- Support for command acknowledgements with automatic retry logic
- Optional message encryption
- SASL authentication support
- Environment-based isolation for multi-environment deployments

## Installation

Add the NuGet package to your project:

```bash
dotnet add package Zerra.CQRS.Kafka
```

## Prerequisites

- A running Kafka cluster with accessible bootstrap servers
- Required NuGet packages: `Zerra.CQRS`, `Zerra.CQRS.Kafka`

## Client Setup (Producer)

A client application uses `KafkaProducer` to send commands and publish events to Kafka topics.

### Basic Producer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configuration
var bootstrapServers = "localhost:9092";
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

// Create Kafka producer
var producer = new KafkaProducer(
    host: bootstrapServers,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment,
    userName: null,  // Optional: SASL username
    password: null   // Optional: SASL password
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
using Zerra.CQRS.Kafka;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain.Commands;
using MyApp.Domain.Events;

Console.WriteLine("Kafka Client starting...");

// Configuration
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "MyKafkaClient";
var environment = "dev";

// Optional SASL authentication
var userName = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
var password = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");

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

// Create and configure Kafka producer
var producer = new KafkaProducer(
    host: bootstrapServers,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment,
    userName: userName,
    password: password
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

A server application uses `KafkaConsumer` to receive and process commands and events from Kafka topics.

### Basic Consumer Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Kafka;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Handlers;

// Configuration
var bootstrapServers = "localhost:9092";
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

// Create Kafka consumer
var consumer = new KafkaConsumer(
    host: bootstrapServers,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment,
    userName: null,  // Optional: SASL username
    password: null   // Optional: SASL password
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
using Zerra.CQRS.Kafka;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Domain;
using MyApp.Handlers;
using MyApp.Services;
using MyApp.Repositories;

Console.WriteLine("Starting Kafka Server...");

// Configuration
var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "devKey123";
var serviceName = "UserService";
var environment = "dev";
var dbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? throw new InvalidOperationException("CONNECTION_STRING not set");

// Optional SASL authentication
var userName = Environment.GetEnvironmentVariable("KAFKA_USERNAME");
var password = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");

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

// Create Kafka consumer
var consumer = new KafkaConsumer(
    host: bootstrapServers,
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: environment,
    userName: userName,
    password: password
);

// Register consumers
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer);

Console.WriteLine($"Kafka Server started on {serviceName}");
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
    Console.WriteLine("Kafka Server stopped");
}
```

## Configuration Options

### KafkaProducer Constructor

```csharp
public KafkaProducer(
    string host,              // Kafka bootstrap servers (e.g., "localhost:9092")
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message encryptor
    ILogger? log,            // Optional logger
    string? environment,     // Optional environment prefix for topics
    string? userName,        // Optional SASL username
    string? password)        // Optional SASL password
```

### KafkaConsumer Constructor

```csharp
public KafkaConsumer(
    string host,              // Kafka bootstrap servers (e.g., "localhost:9092")
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message decryptor
    ILogger? log,            // Optional logger
    string? environment,     // Optional environment prefix for topics
    string? userName,        // Optional SASL username
    string? password)        // Optional SASL password
```

## SASL Authentication

When your Kafka cluster requires authentication, provide the username and password:

```csharp
var producer = new KafkaProducer(
    host: "kafka.example.com:9092",
    serializer: serializer,
    encryptor: encryptor,
    log: logger,
    environment: "prod",
    userName: "myServiceUser",
    password: "securePassword123"
);
```

The implementation uses SASL/PLAIN authentication mechanism.

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same Kafka cluster by prefixing topic names:

```csharp
// Development environment
var devProducer = new KafkaProducer(bootstrapServers, serializer, encryptor, logger, "dev", null, null);

// Production environment
var prodProducer = new KafkaProducer(bootstrapServers, serializer, encryptor, logger, "prod", null, null);
```

Topics will be named like:
- `dev_UserCommandTopic`
- `prod_UserCommandTopic`

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Azure Service Bus Setup](AzureServiceBusSetup.md) - Azure Service Bus messaging implementation
- [RabbitMQ Setup](RabbitMQSetup.md) - RabbitMQ messaging implementation
