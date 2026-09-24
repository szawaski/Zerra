# Zerra CQRS Framework

> **Upgrading from version 5?** Version 6 changes how services are set up. Follow the [upgrade guide](docs/UpgradeV5ToV6.md) to move a v5 solution to v6 step by step.

A high-performance, distributed CQRS (Command Query Responsibility Segregation) framework. Zerra enables message-driven architecture with unified, caller-agnostic routing that abstracts local and remote service boundaries for commands, queries, and events, supporting multiple transport services including Kafka, RabbitMQ, and Azure Service Bus.

## 📚 Documentation

For comprehensive guides, see the [Documentation Index](docs/Index.md)

## Features

⚡ **Pure CQRS Pattern** - Clear separation between commands (write), events (notifications), and queries (read)

🚀 **High Performance** - Source-generated proxy code eliminates reflection overhead; optimized for AOT compilation

🔌 **Multiple Transports** - Built-in support for Kafka, RabbitMQ, and Azure Service Bus message brokers

🔀 **Local and Remote Routing** - Seamlessly route messages to local handlers or remote services via configurable brokers

🔒 **Type-Safe Queries** - Query interfaces with automatic proxy generation for type-safe remote calls

⏱️ **Async-First** - Fully async/await support with configurable timeout and concurrency management

🎛️ **Dependency Injection** - Built-in scoped dependency management via `BusContext`

📊 **Observable** - Optional `IBusLogger` for cross-service message lifecycle tracking

📦 **Built-in Serialization** - High-performance ZerraByteSerializer for compact binary serialization and flexible ZerraJsonSerializer for human-readable JSON format

🔐 **Message Encryption** - Transparent symmetric encryption supporting AES, DES, TripleDES, RC2, and custom algorithms

✨ **Zero Dependencies** - No external package dependencies on .NET 10; the .NET Standard 2.0 build adds only Microsoft's System.* compatibility packages

🧩 **.NET 10 and .NET Standard 2.0** - `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also run on .NET Framework 4.7.2+ and other .NET Standard 2.0 platforms

## Quick Start

### Install

```bash
dotnet add package Zerra
```

### Basic Setup - Server Side

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configure serialization, encryption, and logging
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();          // your ILogger implementation (see docs/Logging.md)
IBusLogger busLogger = new ConsoleBusLogger(); // your IBusLogger implementation (optional)
BusServices busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);

// Create the bus
var bus = Bus.New(
    serviceName: "UserService",
    log: logger,
    busLog: busLogger,
    busServices: busServices
);

// Register local handlers and services
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddHandler<IUserQueries>(userQueryHandler);

// Create TCP CQRS server with configured serializer, encryptor, and logger
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandlers>(server);
bus.AddQueryServer<IUserQueries>(server);

// Start processing messages
await bus.WaitForExitAsync(cancellationToken);
```

### Basic Setup - Client Side

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Configure serialization, encryption, and logging (must match server)
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();
BusServices busServices = new BusServices();

// Create the bus
var bus = Bus.New(
    serviceName: "ClientService",
    log: logger,
    busLog: busLogger,
    busServices: busServices
);

// Create TCP CQRS client with configured serializer, encryptor, and logger
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandlers>(client);
bus.AddQueryClient<IUserQueries>(client);

// Now dispatch commands and queries to remote UserService
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });

var user = await bus.DispatchAwaitAsync(new UpdateUserCommand { Id = 1, Email = "updated@example.com" });

var activeUsers = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
```

## Core Concepts

### Queries

Execute read operations:

```csharp
// Shared domain - Define query interface
public interface IUserQueries : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
    Task<List<User>> GetActiveUsers(CancellationToken cancellationToken);
    Task<Stream> ExportUsers(CancellationToken cancellationToken); // Live-streamed
}

// Server side - Implement the query handler
public class UserQueryHandler : BaseHandler, IUserQueries
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.GetActiveAsync(cancellationToken);
    }

    public async Task<Stream> ExportUsers(CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.ExportStreamAsync(cancellationToken);
    }
}

// Client side - Call queries (type-safe)
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
var activeUsers = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
```

### Commands

Execute operations that modify state:

```csharp
// Shared domain - Define command classes
public class CreateUserCommand : ICommand
{
    public String Email { get; set; }
}

public class UpdateUserCommand : ICommand<User>
{
    public int Id { get; set; }
    public String Email { get; set; }
}

// Server side - Implement the command handler
public class UserCommandHandler : BaseHandler, 
    ICommandHandler<CreateUserCommand>,
    ICommandHandler<UpdateUserCommand, User>
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        var repository = Context.GetService<IUserRepository>();
        await repository.CreateAsync(command.Email, ct);
    }

    public async Task<User> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.UpdateAsync(command.Id, command.Email, ct);
    }
}

// Client side - Dispatch commands
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com" });

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });

var user = await bus.DispatchAwaitAsync(new UpdateUserCommand { Id = 1, Email = "new@example.com" });
```

### Events

Publish state change notifications:

```csharp
// Shared domain - Define event class
public class UserCreatedEvent : IEvent
{
    public int UserId { get; set; }
    public String Email { get; set; }
}

// Server side - Implement event handler
public class UserEventHandler : BaseHandler,
    IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var emailService = this.Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmail(@event.Email);
    }
}

// Client side - Dispatch events
await bus.DispatchAsync(new UserCreatedEvent 
{ 
    UserId = 123, 
    Email = "user@example.com" 
});
```

## Routing

Zerra's routing mechanism is agnostic to whether services are local or remote. Callers dispatch messages without needing to change code based on deployment topology.

### Local Processing

```csharp
// Register local handlers
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddHandler<IUserQueries>(userQueryHandler);

// Commands routed to local handler immediately
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com" });

// Queries routed to local handler immediately
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
```

### Remote Query Processing

#### TCP CQRS

```csharp
// Server side
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
bus.AddHandler<IUserQueries>(userQueryHandler);
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddQueryServer<IUserQueries>(tcpServer);
bus.AddCommandConsumer<IUserCommandHandlers>(tcpServer);

// Client side
var tcpClient = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
bus.AddQueryClient<IUserQueries>(tcpClient);
bus.AddCommandProducer<IUserCommandHandlers>(tcpClient);

// Use as before - type-safe queries
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
```

#### HTTP CQRS

```csharp
// Server side (authorizer and allowOrigins are optional)
var httpServer = new HttpCqrsServer("localhost:9001", serializer, encryptor, authorizer: null, allowOrigins: null, log: log);
bus.AddHandler<IUserQueries>(userQueryHandler);
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddQueryServer<IUserQueries>(httpServer);
bus.AddCommandConsumer<IUserCommandHandlers>(httpServer);

// Client side
var httpClient = new HttpCqrsClient("localhost:9001", serializer, encryptor, authorizer: null, log: log);
bus.AddQueryClient<IUserQueries>(httpClient);
bus.AddCommandProducer<IUserCommandHandlers>(httpClient);

// Use as before - type-safe queries
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
```

### Remote Command/Event Processing

#### TCP / HTTP CQRS

The TCP and HTTP servers are also command and event consumers, and the clients are command and event producers:

```csharp
// Server side
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddHandler<IUserEvents>(userEventHandler);
bus.AddCommandConsumer<IUserCommandHandlers>(tcpServer);
bus.AddEventConsumer<IUserEvents>(tcpServer, EventConsumerMode.PerReplica);

// Client side
var tcpClient = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
bus.AddCommandProducer<IUserCommandHandlers>(tcpClient);
bus.AddEventProducer<IUserEvents>(tcpClient);
```

#### Message Brokers

Each broker package provides a single producer class (commands and events) and a single consumer class (commands and events):

| Package | Producer | Consumer |
|---------|----------|----------|
| `Zerra.CQRS.Kafka` | `KafkaProducer` | `KafkaConsumer` |
| `Zerra.CQRS.RabbitMQ` | `RabbitMQProducer` | `RabbitMQConsumer` |
| `Zerra.CQRS.AzureServiceBus` | `AzureServiceBusProducer` | `AzureServiceBusConsumer` |

```csharp
// Server side (Kafka shown; RabbitMQ and Azure Service Bus follow the same pattern)
var kafkaConsumer = new KafkaConsumer("localhost:9092", serializer, encryptor, log, environment: "dev", userName: null, password: null);
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddHandler<IUserEvents>(userEventHandler);
bus.AddCommandConsumer<IUserCommandHandlers>(kafkaConsumer);
bus.AddEventConsumer<IUserEvents>(kafkaConsumer, EventConsumerMode.PerReplica);

// Client side
var kafkaProducer = new KafkaProducer("localhost:9092", serializer, encryptor, log, environment: "dev", userName: null, password: null);
bus.AddCommandProducer<IUserCommandHandlers>(kafkaProducer);
bus.AddEventProducer<IUserEvents>(kafkaProducer);

// RabbitMQ:          new RabbitMQProducer(host, serializer, encryptor, log, environment)
// Azure Service Bus: new AzureServiceBusProducer(connectionString, serializer, encryptor, log, environment)
```

See [Kafka Setup](docs/KafkaSetup.md), [RabbitMQ Setup](docs/RabbitMQSetup.md), and [Azure Service Bus Setup](docs/AzureServiceBusSetup.md) for details.

## Configuration

### Bus Creation

```csharp
var bus = Bus.New(
    serviceName: "MyService",
    log: logger,
    busLog: busLogger,
    busServices: busServices,
    commandToReceiveUntilExit: 100,                    // Optional: graceful shutdown after N commands
    defaultCallTimeout: TimeSpan.FromSeconds(30),      // Query timeout
    defaultDispatchTimeout: TimeSpan.FromSeconds(5),   // Command/event dispatch timeout
    defaultDispatchAwaitTimeout: TimeSpan.FromSeconds(10), // Await timeout
    maxConcurrentQueries: Environment.ProcessorCount * 32,
    maxConcurrentCommandsPerTopic: Environment.ProcessorCount * 8,
    maxConcurrentEventsPerTopic: Environment.ProcessorCount * 16
);
```

### Dependency Injection

```csharp
// Register dependencies in BusServices (registration is by interface type)
var busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);

var bus = Bus.New(
    serviceName: "MyService",
    busServices: busServices
);

// Access dependencies in handlers via BusContext
public class UserCommandHandler : BaseHandler, 
    ICommandHandler<CreateUserCommand>,
    ICommandHandler<UpdateUserCommand, User>
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Get scoped dependencies from context
        var repository = Context.GetService<IUserRepository>();
        var emailService = Context.GetService<IEmailService>();
        
        // Use dependencies
        Log?.Info($"Creating user: {command.Email}");
        var user = await repository.CreateAsync(command.Email, ct);
        await emailService.SendWelcomeEmail(user.Email, ct);
    }

    public async Task<User> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.UpdateAsync(command.Id, command.Email, ct);
    }
}

// Same pattern works in query handlers and event handlers
public class UserQueryHandler : BaseHandler, IUserQueries
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var repository = this.Context.GetService<IUserRepository>();
        return await repository.GetByIdAsync(id, cancellationToken);
    }
}

public class UserEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var emailService = this.Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmail(@event.Email);
    }
}
```

### Logging

```csharp
public class MyBusLogger : IBusLogger
{
    public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled)
    {
        // Log command start
    }

    public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
    {
        // Log command completion
    }

    // Similar for events and queries...
}

var bus = Bus.New(
    serviceName: "MyService",
    busLog: new MyBusLogger()
);
```

## Lifecycle Management

### Startup and Shutdown

```csharp
// Option 1: Manual shutdown
await bus.StopServicesAsync();

// Option 2: Wait for process exit (recommended for services)
await bus.WaitForExitAsync(cancellationToken);

// Both will:
// - Close all consumers/servers
// - Dispose all producers/consumers/clients/servers
// - Stop processing new messages
```

## Source Generation

Zerra uses C# source generators to create:
- Proxy implementations of query interfaces
- Message type routing metadata
- Handler invocation code

This eliminates reflection overhead and enables AOT compilation.

## Performance Characteristics

- **Local Handler Invocation**: Direct delegate call (~microseconds)
- **Remote Command Dispatch**: Broker-dependent (typically < 100ms)
- **Query Calls**: HTTP/TCP round-trip (typically < 50ms)
- **Concurrency**: Configurable limits per message type

## Best Practices

1. **Group handlers by interface** - One interface for related commands/events
2. **Keep query interfaces focused** - Single responsibility per interface
3. **Use scoped dependencies** - Register in `BusServices` for handler access
4. **Handle exceptions properly** - They propagate from handlers to callers
5. **Design for eventual consistency** - Events represent completed changes, not intents
6. **Make handlers idempotent** - Especially for commands/events that might be retried

## Framework Packages

- **Zerra** - Core CQRS framework (this package)
- **Zerra.CQRS.Kafka** - Kafka message broker support
- **Zerra.CQRS.RabbitMQ** - RabbitMQ message broker support
- **Zerra.CQRS.AzureServiceBus** - Azure Service Bus support
- **Zerra.Web** - ASP.NET Core integration, including API gateway
- **Zerra.Repository** (experimental) - Data store agnostic LINQ-based repository, with providers in `Zerra.Repository.MsSql`, `Zerra.Repository.MySql`, `Zerra.Repository.MariaDb`, `Zerra.Repository.PostgreSql`, `Zerra.Repository.Memory`, and `Zerra.Repository.KurrentDB` (see [Repository](docs/Repository.md))

## License

MIT - See LICENSE file

## Repository

[GitHub - Zerra CQRS Framework](https://github.com/szawaski/Zerra)
