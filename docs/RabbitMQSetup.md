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
- Required NuGet packages: `Zerra`, `Zerra.CQRS.RabbitMQ`
- Examples use the sample `ConsoleLogger` / `ConsoleBusLogger` implementations from [Logging](Logging.md); substitute your own

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
    // Stops the bus and disposes its producers
    await bus.StopServicesAsync();
}
```

## Server Setup (Consumer)

A server application uses `RabbitMQConsumer` to receive and process commands and events from RabbitMQ exchanges.

### Commands compete, events fan out

The consumer declares a different topology for each, which is what makes a command happen once and an event reach everyone:

| | Exchange | Queue | With several replicas of the service |
|---|---|---|---|
| Commands | Direct | **one queue named for the topic, shared by every replica** | they compete, and **one** replica handles each command |
| Events | Fanout | a server-named exclusive queue per replica | **every** replica gets its own copy |
| Events, `PerService` | Fanout | **one queue named for the topic and the service, shared by every replica** | they compete, and **one** replica handles each event |

Design handlers to match. A `PerReplica` event handler has to be correct when every replica runs it at the same time: dropping a cache the replica holds in its own memory is fine, moving stock is not. Work that must happen once belongs in a command handler, or in a consumer registered `PerService`, which is the last row. See [Events](Events.md#events-are-fanned-out-to-every-replica).

The last row is what `bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerService)` declares instead. It is the subscriber's own choice and changes nothing for the publisher or for the other services bound to the same Fanout exchange, which still get their own copy of every event. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

The command queue and the `PerService` queue are not auto-deleted, so messages sent while no replica is connected, including while one reconnects, wait for the next one (messages are transient, so they don't survive a broker restart). A `PerReplica` queue is exclusive to its replica's connection, so events sent while that replica is disconnected are not held for it.

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

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
    serviceName: serviceName,
    log: logger,
    busLog: busLogger,
    busServices: busServices
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
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

Console.WriteLine($"RabbitMQ Server started on {serviceName}");
Console.WriteLine("Press Ctrl+C to stop...");

// Waits for Ctrl+C, SIGTERM, or process exit, then stops the bus and disposes its consumers
await bus.WaitForExitAsync();
Console.WriteLine("RabbitMQ Server stopped");
```

## Configuration Options

### RabbitMQProducer Constructor

```csharp
public RabbitMQProducer(
    string host,              // RabbitMQ host name, or an AMQP URI (see below)
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message encryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for exchanges
```

### RabbitMQConsumer Constructor

```csharp
public RabbitMQConsumer(
    string host,              // RabbitMQ host name, or an AMQP URI (see below)
    ISerializer serializer,   // Message serializer
    IEncryptor? encryptor,   // Optional message decryptor
    ILogger? log,            // Optional logger
    string? environment)     // Optional environment prefix for exchanges
```

## Connection Configuration (Credentials, Port, Virtual Host, TLS)

The `host` parameter accepts either a plain host name or an AMQP URI:

- **Host name** (e.g. `"localhost"`): the RabbitMQ client defaults apply for everything else: port `5672`, the `guest`/`guest` credentials, the default virtual host `/`, and no TLS.
- **AMQP URI**: configures credentials, port, virtual host, and TLS in one value. Use `amqps://` for TLS (default port `5671`). URL-encode special characters in the user name or password (e.g. `@` as `%40`).

```csharp
// Credentials, custom port, and virtual host
var producer = new RabbitMQProducer("amqp://myUser:myPassword@rabbit.example.com:5672/myVhost", serializer, encryptor, logger, "prod");

// TLS
var consumer = new RabbitMQConsumer("amqps://myUser:myPassword@rabbit.example.com/myVhost", serializer, encryptor, logger, "prod");
```

The URI contains secrets, so load it from configuration or a secret store rather than hard-coding it. Zerra never logs the host value.

## Checking the Connection

`RabbitMQConnection.Test` opens and closes a connection and returns whether it opened, waiting five seconds unless given another timeout. It's synchronous because the RabbitMQ client only connects synchronously. Use it at startup to fall back to a direct transport when RabbitMQ isn't running. Subscribers make the same check and register the matching consumer, so both ends pick the same route:

```csharp
if (RabbitMQConnection.Test(rabbitMQHost, log: logger))
    bus.AddEventProducer<IOrderEventHandler>(new RabbitMQProducer(rabbitMQHost, serializer, encryptor, logger, null));
else
    bus.AddEventProducer<IOrderEventHandler>(new TcpCqrsClient("localhost:9102", serializer, encryptor, logger));
```

The logger, if given, is told why the connection failed. The Store demo uses this for order events, see `Demo/Store/Store.Orders.Service/Program.cs`, `Demo/Store/Store.Inventory.Service/Program.cs`, and `Demo/Store/Store.Shipping.Service/Program.cs`.

## Environment Isolation

The optional `environment` parameter allows multiple environments (dev, staging, production) to share the same RabbitMQ server by prefixing exchange names:

```csharp
// Development environment
var devProducer = new RabbitMQProducer("localhost", serializer, encryptor, logger, "dev");

// Production environment
var prodProducer = new RabbitMQProducer("localhost", serializer, encryptor, logger, "prod");
```

Names are the handler interface name (e.g. `IUserCommandHandler`) prefixed with the environment:
- `dev_IUserCommandHandler`
- `prod_IUserCommandHandler`

RabbitMQ caps a name at 255 characters, and a `PerService` queue is the exchange name plus the service name. Both are shortened to fit, and the producer and consumer log a warning naming the shortened result when that happens, since a different exchange or service shortening to the same name would share it.

## See Also

- [Client Setup](ClientSetup.md) - General client configuration patterns
- [Server Setup](ServerSetup.md) - General server configuration patterns
- [Commands](Commands.md) - Working with commands
- [Events](Events.md) - Working with events
- [Azure Service Bus Setup](AzureServiceBusSetup.md) - Azure Service Bus messaging implementation
- [Kafka Setup](KafkaSetup.md) - Kafka messaging implementation
