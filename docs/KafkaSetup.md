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
- Required NuGet packages: `Zerra`, `Zerra.CQRS.Kafka`
- Examples use the sample `ConsoleLogger` / `ConsoleBusLogger` implementations from [Logging](Logging.md); substitute your own

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    // Stops the bus and disposes its producers
    await bus.StopServicesAsync();
}
```

## Server Setup (Consumer)

A server application uses `KafkaConsumer` to receive and process commands and events from Kafka topics.

### Consumer groups

The group id decides how many replicas of the service handle each message, and it is the whole difference between a command and an event here:

| | Consumer group | With several replicas of the service |
|---|---|---|
| Commands | one named for the topic, shared by every replica | they compete, and **one** replica handles each command |
| Events, `EventConsumerMode.PerReplica` | a new group id per consumer instance | **every** replica gets its own copy |
| Events, `EventConsumerMode.PerService` | one named for the topic and the service, shared by every replica | they compete, and **one** replica handles each event |

`AddEventConsumer` takes the mode on every registration, there is no default. It is the subscriber's own choice and changes nothing for the publisher or for the other services subscribed to the same topic, which still get their own copy of every event. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

A `PerReplica` group belongs to the one consumer that made it, so the consumer deletes it when it stops. A `PerService` group is shared by the replicas, so it stays, and its committed offsets mean events published while the whole service is down are waiting when it comes back.

A topic is created with one partition, so `PerService` means one replica receives everything and the rest stand by to take over. That is what a Kafka command consumer already does.

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

Console.WriteLine($"Kafka Server started on {serviceName}");
Console.WriteLine("Press Ctrl+C to stop...");

// Setup graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Waits for process exit or cancellation, then stops the bus and disposes its consumers
await bus.WaitForExitAsync(cts.Token);
Console.WriteLine("Kafka Server stopped");
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

## Checking the Connection

`KafkaConnection.TestAsync` asks the cluster to describe itself and returns whether it answered, waiting five seconds unless given another timeout. Use it at startup to fall back to a direct transport when Kafka isn't running. The receiver makes the same check and registers the matching consumer, so both ends pick the same route:

```csharp
if (await KafkaConnection.TestAsync(bootstrapServers, userName: null, password: null, log: logger))
    bus.AddCommandProducer<IStockReservationHandler>(new KafkaProducer(bootstrapServers, serializer, encryptor, logger, null, null, null));
else
    bus.AddCommandProducer<IStockReservationHandler>(new TcpCqrsClient("localhost:9102", serializer, encryptor, logger));
```

The logger, if given, is told why the connection failed. The Store demo uses this for stock reservations, see `Demo/Store/Store.Orders.Service/Program.cs` and `Demo/Store/Store.Inventory.Service/Program.cs`.

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same Kafka cluster by prefixing topic names:

```csharp
// Development environment
var devProducer = new KafkaProducer(bootstrapServers, serializer, encryptor, logger, "dev", null, null);

// Production environment
var prodProducer = new KafkaProducer(bootstrapServers, serializer, encryptor, logger, "prod", null, null);
```

Topic names are the handler interface name (e.g. `IUserCommandHandler`) prefixed with the environment:
- `dev_IUserCommandHandler`
- `prod_IUserCommandHandler`

Kafka caps a topic name at 249 characters, and a `PerService` consumer group is the topic name plus the service name. Both are shortened to fit, and the producer and consumer log a warning naming the shortened result when that happens, since a different topic or service shortening to the same name would share it.

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Azure Service Bus Setup](AzureServiceBusSetup.md) - Azure Service Bus messaging implementation
- [RabbitMQ Setup](RabbitMQSetup.md) - RabbitMQ messaging implementation
