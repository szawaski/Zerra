# Zerra Framework

> **⚠️ Breaking Changes Notice:** Version 6 introduces many breaking changes from version 5. Please review the documentation and examples carefully before upgrading.

A high-performance, distributed CQRS (Command Query Responsibility Segregation) framework for .NET. Zerra enables message-driven architecture with unified, caller-agnostic routing that abstracts local and remote service boundaries for commands, queries, and events, supporting multiple transport services including Kafka, RabbitMQ, and Azure Service Bus.

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

✨ **Zero Dependencies** - No external package dependencies, only .NET standard libraries

## Installation

```bash
dotnet add package Zerra
```

## Quick Start

### Server Setup

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;

// Configure services
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
BusScopes busScopes = new BusScopes();
busScopes.AddService<IUserRepository>(userRepository);

// Create the bus
var bus = Bus.New(
    service: "UserService",
    busScopes: busScopes
);

// Register local handlers
bus.AddHandler<IUserCommandHandlers>(userCommandHandler);
bus.AddHandler<IUserQueries>(userQueryHandler);

// Create TCP CQRS server
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor);
bus.AddCommandConsumer<IUserCommandHandlers>(server);
bus.AddQueryServer<IUserQueries>(server);

// Start processing messages
await bus.WaitForExitAsync(cancellationToken);
```

### Client Setup

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;

// Configure services (must match server)
ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);

// Create the bus
var bus = Bus.New(service: "ClientService");

// Create TCP CQRS client
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor);
bus.AddCommandProducer<IUserCommandHandlers>(client);
bus.AddQueryClient<IUserQueries>(client);

// Dispatch commands and queries
var user = await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
var activeUsers = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
```

## Core Concepts

### Queries

Execute read operations with type-safe interfaces:

```csharp
// Define query interface
public interface IUserQueries : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
    Task<List<User>> GetActiveUsers(CancellationToken cancellationToken);
}

// Implement query handler
public class UserQueryHandler : BaseHandler, IUserQueries
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var repository = GetService<IUserRepository>();
        return await repository.GetByIdAsync(id, cancellationToken);
    }
}

// Call queries
var user = await bus.Call<IUserQueries>().GetUserById(123, cancellationToken);
```

### Commands

Execute state-changing operations:

```csharp
// Define command
public class CreateUserCommand : ICommand
{
    public string Email { get; set; }
}

// Implement handler
public class UserCommandHandler : BaseHandler, ICommandHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        var repository = GetService<IUserRepository>();
        await repository.CreateAsync(command.Email, ct);
    }
}

// Dispatch commands
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com" });
```

### Events

Publish state change notifications:

```csharp
// Define event
public class UserCreatedEvent : IEvent
{
    public int UserId { get; set; }
    public string Email { get; set; }
}

// Implement handler
public class UserEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var emailService = GetService<IEmailService>();
        await emailService.SendWelcomeEmail(@event.Email);
    }
}

// Dispatch events
await bus.DispatchAsync(new UserCreatedEvent { UserId = 123, Email = "user@example.com" });
```

## Transport Options

Zerra supports multiple message transports:

- **TCP/HTTP CQRS** - Built-in lightweight transport (included in this package)
- **Kafka** - Install `Zerra.CQRS.Kafka` for Apache Kafka support
- **RabbitMQ** - Install `Zerra.CQRS.RabbitMQ` for RabbitMQ support
- **Azure Service Bus** - Install `Zerra.CQRS.AzureServiceBus` for Azure messaging

## Additional Features

### Serialization
- **ZerraByteSerializer** - High-performance binary serialization
- **ZerraJsonSerializer** - JSON serialization with Graph-based property control

### Utilities
- **TypeAnalyzer** - Fast reflection and type inspection
- **Mapper** - Object mapping and type conversion with AOT support
- **Graph** - Selective member inclusion/exclusion
- **Collections** - Thread-safe collection implementations
- **Stream Wrappers** - Stream interception and transformation

### AOT Support
Built-in source generators provide precompiled reflection metadata for Native AOT compilation.

## Documentation

For comprehensive guides and examples, visit the [GitHub repository](https://github.com/szawaski/Zerra).

**Key Topics:**
- [Agents](https://github.com/szawaski/Zerra/blob/master/docs/Agents.md) - Architectural context
- [AOT Support](https://github.com/szawaski/Zerra/blob/master/docs/AOT.md) - Native AOT configuration
- [Serializers](https://github.com/szawaski/Zerra/blob/master/docs/Serializers.md) - Serialization options
- [Server Setup](https://github.com/szawaski/Zerra/blob/master/docs/ServerSetup.md) - Server configuration
- [Client Setup](https://github.com/szawaski/Zerra/blob/master/docs/ClientSetup.md) - Client configuration
- [Zerra.Web](https://github.com/szawaski/Zerra/blob/master/docs/ZerraWeb.md) - ASP.NET integration

## Framework Packages

- **Zerra** - Core CQRS framework (this package)
- **Zerra.CQRS.Kafka** - Kafka message broker support
- **Zerra.CQRS.RabbitMQ** - RabbitMQ message broker support
- **Zerra.CQRS.AzureServiceBus** - Azure Service Bus support
- **Zerra.Web** - ASP.NET Core integration and API gateway

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)

## Repository

[GitHub - Zerra Framework](https://github.com/szawaski/Zerra)
