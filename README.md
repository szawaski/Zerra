# Zerra CQRS Framework

A high-performance, distributed CQRS (Command Query Responsibility Segregation) framework. Zerra enables message-driven architecture with unified, caller-agnostic routing that abstracts local and remote service boundaries for commands, queries, and events, supporting multiple transport services including Kafka, RabbitMQ, and Azure Service Bus.

## Features

✨ **Pure CQRS Pattern** - Clear separation between commands (write), events (notifications), and queries (read)

🚀 **High Performance** - Source-generated proxy code eliminates reflection overhead; optimized for AOT compilation

📦 **Multiple Transports** - Built-in support for Kafka, RabbitMQ, and Azure Service Bus message brokers

🔄 **Local and Remote Routing** - Seamlessly route messages to local handlers or remote services via configurable brokers

📝 **Type-Safe Queries** - Query interfaces with automatic proxy generation for type-safe remote calls

⚡ **Async-First** - Fully async/await support with configurable timeout and concurrency management

🔌 **Dependency Injection** - Built-in scoped dependency management via `BusContext`

📊 **Observable** - Optional `IBusLogger` for cross-service message lifecycle tracking

📦 **Built-in Serialization** - High-performance ZerraByteSerializer for compact binary serialization and flexible ZerraJsonSerializer for human-readable JSON format

🔐 **Message Encryption** - Transparent symmetric encryption supporting AES, DES, TripleDES, RC2, and custom algorithms

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
ILogger logger = new Logger();
IBusLogger busLogger = new BusLogger();
BusScopes busScopes = new BusScopes();
busScopes.AddScope<IUserRepository>(userRepository);

// Create the bus
var bus = Bus.New(
    service: "UserService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
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
ILogger logger = new Logger();
IBusLogger busLogger = new BusLogger();
BusScopes busScopes = new BusScopes();

// Create the bus
var bus = Bus.New(
    service: "ClientService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);

// Create TCP CQRS client with configured serializer, encryptor, and logger
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandlers>(client);
bus.AddQueryClient<IUserQueries>(client);

// Now dispatch commands and queries to remote UserService
var user = await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });

await bus.DispatchAwaitAsync(new UpdateUserCommand { Id = user.Id, Email = "updated@example.com" });

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
        var repository = this.Context.Get<IUserRepository>();
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
    {
        var repository = this.Context.Get<IUserRepository>();
        return await repository.GetActiveAsync(cancellationToken);
    }

    public async Task<Stream> ExportUsers(CancellationToken cancellationToken)
    {
        var repository = this.Context.Get<IUserRepository>();
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
        var repository = this.Context.Get<IUserRepository>();
        await repository.CreateAsync(command.Email, ct);
    }

    public async Task<User> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var repository = this.Context.Get<IUserRepository>();
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
        var emailService = this.Context.Get<IEmailService>();
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
// Register local handler
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);

// Messages routed to local handler immediately
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com" });
```

### Remote Query Processing

#### TCP CQRS

```csharp
// Server side
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
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
// Server side
var httpServer = new HttpCqrsServer("localhost:9001", serializer, encryptor, log);
bus.AddQueryServer<IUserQueries>(httpServer);
bus.AddCommandConsumer<IUserCommandHandlers>(httpServer);

// Client side
var httpClient = new HttpCqrsClient("localhost:9001", serializer, encryptor, log);
bus.AddQueryClient<IUserQueries>(httpClient);
bus.AddCommandProducer<IUserCommandHandlers>(httpClient);

// Use as before - type-safe queries
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
```

### Remote Command/Event Processing

#### TCP CQRS

```csharp
// Server side
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
bus.AddCommandConsumer<IUserCommandHandlers, IUserQueries>(tcpServer);

// Client side
var tcpClient = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
bus.AddCommandProducer<IUserCommandHandlers, IUserQueries>(tcpClient);
```

#### HTTP CQRS

```csharp
// Server side
var httpServer = new HttpCqrsServer("localhost:9001", serializer, encryptor, log);
bus.AddCommandConsumer<IUserCommandHandlers, IUserQueries>(httpServer);

// Client side
var httpClient = new HttpCqrsClient("localhost:9001", serializer, encryptor, log);
bus.AddCommandProducer<IUserCommandHandlers, IUserQueries>(httpClient);
```

#### Kafka

```csharp
// Server side
var kafkaConsumer = new KafkaCommandConsumer(kafkaConfig);
bus.AddCommandConsumer<IUserCommandHandlers>(kafkaConsumer);

var kafkaEventConsumer = new KafkaEventConsumer(kafkaConfig);
bus.AddEventConsumer<IUserEvents>(kafkaEventConsumer);

// Client side
var kafkaProducer = new KafkaCommandProducer(kafkaConfig);
bus.AddCommandProducer<IUserCommandHandlers>(kafkaProducer);

var kafkaEventProducer = new KafkaEventProducer(kafkaConfig);
bus.AddEventProducer<IUserEvents>(kafkaEventProducer);
```

#### RabbitMQ

```csharp
// Server side
var rabbitmqConsumer = new RabbitMQCommandConsumer(rabbitmqConfig);
bus.AddCommandConsumer<IUserCommandHandlers>(rabbitmqConsumer);

var rabbitmqEventConsumer = new RabbitMQEventConsumer(rabbitmqConfig);
bus.AddEventConsumer<IUserEvents>(rabbitmqEventConsumer);

// Client side
var rabbitmqProducer = new RabbitMQCommandProducer(rabbitmqConfig);
bus.AddCommandProducer<IUserCommandHandlers>(rabbitmqProducer);

var rabbitmqEventProducer = new RabbitMQEventProducer(rabbitmqConfig);
bus.AddEventProducer<IUserEvents>(rabbitmqEventProducer);
```

#### Azure Service Bus

```csharp
// Server side
var asbConsumer = new AzureServiceBusCommandConsumer(asbConfig);
bus.AddCommandConsumer<IUserCommandHandlers>(asbConsumer);

var asbEventConsumer = new AzureServiceBusEventConsumer(asbConfig);
bus.AddEventConsumer<IUserEvents>(asbEventConsumer);

// Client side
var asbProducer = new AzureServiceBusCommandProducer(asbConfig);
bus.AddCommandProducer<IUserCommandHandlers>(asbProducer);

var asbEventProducer = new AzureServiceBusEventProducer(asbConfig);
bus.AddEventProducer<IUserEvents>(asbEventProducer);
```

## Configuration

### Bus Creation

```csharp
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: dependencyScopes,
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
// Register dependencies in BusScopes
var scopes = new BusScopes();
scopes.AddScope<IUserRepository>(userRepository);
scopes.AddScope<IEmailService>(emailService);

var bus = Bus.New(
    service: "MyService",
    busScopes: scopes,
    // ...
);

// Access dependencies in handlers via BusContext
public class UserCommandHandler : BaseHandler, 
    ICommandHandler<CreateUserCommand>,
    ICommandHandler<UpdateUserCommand, User>
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Get scoped dependencies from context
        var repository = this.Context.Get<IUserRepository>();
        var emailService = this.Context.Get<IEmailService>();
        
        // Use dependencies
        this.Context.Log.Info($"Creating user: {command.Email}");
        var user = await repository.CreateAsync(command.Email, ct);
        await emailService.SendWelcomeEmail(user.Email, ct);
    }

    public async Task<User> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var repository = this.Context.Get<IUserRepository>();
        return await repository.UpdateAsync(command.Id, command.Email, ct);
    }
}

// Same pattern works in query handlers and event handlers
public class UserQueryHandler : BaseHandler, IUserQueries
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var repository = this.Context.Get<IUserRepository>();
        return await repository.GetByIdAsync(id, cancellationToken);
    }
}

public class UserEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var emailService = this.Context.Get<IEmailService>();
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
    service: "MyService",
    busLog: new MyBusLogger(),
    // ...
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
// - Dispose all producers/consumers/servers
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
3. **Use scoped dependencies** - Register in `BusScopes` for handler access
4. **Handle exceptions properly** - They propagate from handlers to callers
5. **Design for eventual consistency** - Events represent completed changes, not intents
6. **Make handlers idempotent** - Especially for commands/events that might be retried

## Framework Packages

- **Zerra** - Core CQRS framework (this package)
- **Zerra.CQRS.Kafka** - Kafka message broker support
- **Zerra.CQRS.RabbitMQ** - RabbitMQ message broker support
- **Zerra.CQRS.AzureServiceBus** - Azure Service Bus support
- **Zerra.Web** - ASP.NET Core integration, including API gateway

## License

MIT - See LICENSE file

## Repository

[GitHub - Zerra CQRS Framework](https://github.com/szawaski/Zerra)
