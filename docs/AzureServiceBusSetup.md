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
- Required NuGet packages: `Zerra`, `Zerra.CQRS.AzureServiceBus`
- Examples use the sample `ConsoleLogger` / `ConsoleBusLogger` implementations from [Logging](Logging.md); substitute your own

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    // Stops the bus and disposes its producers
    await bus.StopServicesAsync();
}
```

## Server Setup (Consumer)

A server application uses `AzureServiceBusConsumer` to receive and process commands and events from Azure Service Bus.

### Queues and subscriptions

Commands get a queue, events get a topic, and how the topic is subscribed decides how many replicas of the service handle each event:

| | Entity | With several replicas of the service |
|---|---|---|
| Commands | one queue named for the topic, shared by every replica | they compete, and **one** replica handles each command |
| Events, `EventConsumerMode.PerReplica` | a topic, with a new `EVT-{guid}` subscription per consumer instance | **every** replica gets its own copy |
| Events, `EventConsumerMode.PerService` | the same topic, with one `EVT-{serviceName}` subscription shared by every replica | they compete, and **one** replica handles each event |

`AddEventConsumer` takes the mode on every registration, there is no default. It is the subscriber's own choice and changes nothing for the publisher or for the other services subscribed to the same topic, each of which has its own subscription and still gets every event. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

A `PerReplica` subscription belongs to the one consumer that made it, so it is created with `AutoDeleteOnIdle` and deleted when the consumer stops. A `PerService` subscription is shared by the replicas, so it never auto-deletes and stays when they stop, holding events published while the whole service is down.

The service name comes from `Bus.New(serviceName, ...)`.

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

Console.WriteLine($"Azure Service Bus Server started on {serviceName}");
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
Console.WriteLine("Azure Service Bus Server stopped");
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

## Checking the Connection

`AzureServiceBusConnection.TestAsync` reads the namespace's properties through the administration endpoint, the same one that creates queues and topics, and returns whether it answered, waiting five seconds unless given another timeout. Use it at startup to fall back to a direct transport when Service Bus isn't reachable. The receiver makes the same check and registers the matching consumer, so both ends pick the same route:

```csharp
if (await AzureServiceBusConnection.TestAsync(connectionString, log: logger))
    bus.AddCommandProducer<IReviewsCommandHandler>(new AzureServiceBusProducer(connectionString, serializer, encryptor, logger, null));
else
    bus.AddCommandProducer<IReviewsCommandHandler>(new TcpCqrsClient("localhost:9104", serializer, encryptor, logger));
```

The logger, if given, is told why the connection failed. The Store demo uses this for review commands, see `Demo/Store/Store.Web/Program.cs` and `Demo/Store/Store.Reviews.Service/Program.cs`.

### The Service Bus Emulator

The emulator serves AMQP on its endpoint's port and the administration API only on port 5300. When the connection string has `UseDevelopmentEmulator=true`, Zerra sends administration calls, including the connection test, to port 5300 on the same host, so a single connection string works for both.

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same Azure Service Bus namespace by prefixing queue and topic names:

```csharp
// Development environment
var devProducer = new AzureServiceBusProducer(connectionString, serializer, encryptor, logger, "dev");

// Production environment
var prodProducer = new AzureServiceBusProducer(connectionString, serializer, encryptor, logger, "prod");
```

Queue (commands) and topic (events) names are the handler interface name (e.g. `IUserCommandHandler`) prefixed with the environment:
- `dev_IUserCommandHandler`
- `prod_IUserCommandHandler`

Azure Service Bus caps an entity name at 50 characters, which is short enough to hit with an environment prefix and a long interface name, and a `PerService` subscription is `EVT-` plus the service name. Both are shortened to fit, and the producer and consumer log a warning naming the shortened result when that happens, since a different entity or service shortening to the same name would share it.

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Kafka Setup](KafkaSetup.md) - Kafka messaging implementation
- [RabbitMQ Setup](RabbitMQSetup.md) - RabbitMQ messaging implementation
