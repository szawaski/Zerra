[← Back to Documentation](Index.md)

# Getting Started

This guide builds one service and one client that calls it, then shows how the same code moves between in-process calls, TCP, HTTP, and message brokers. For a complete multi-service application, see the [Store demo](../Demo/Store/README.md).

- [Install](#install)
- [Solution Layout](#solution-layout)
- [Define the Contracts](#define-the-contracts)
- [Write the Handlers](#write-the-handlers)
- [Host the Service](#host-the-service)
- [Call the Service](#call-the-service)
- [Routing](#routing)
- [Configuration](#configuration)
- [Lifecycle](#lifecycle)
- [Best Practices](#best-practices)
- [Next Steps](#next-steps)

## Install

```bash
dotnet add package Zerra
```

The package includes the source generator, which creates the query proxies and handler invocation code at compile time. There is nothing else to configure. Add a transport package only when you use a message broker:

```bash
dotnet add package Zerra.CQRS.Kafka            # or Zerra.CQRS.RabbitMQ, Zerra.CQRS.AzureServiceBus
dotnet add package Zerra.Web                   # ASP.NET Core hosting and the browser API gateway
```

## Solution Layout

| Project | Contains | References |
|---|---|---|
| `Users.Domain` | Contracts: query interfaces, commands, events, the models they carry, and handler interfaces | `Zerra` |
| `Users.Service` | Handler classes and `Program.cs` that builds the bus | `Users.Domain`, `Zerra` |
| `Client` | Anything that calls the service: another service, a web app, a console app | `Users.Domain`, `Zerra` |

Services and clients share only the `*.Domain` project, never each other's handlers or data models.

Sharing the project keeps each contract in one place, and the compiler catches every caller a change breaks. A caller can instead keep its own copy of the contracts it uses, since services match contracts by type name, not namespace or assembly. Copies let each service build and deploy from its own repository and pipeline, at the cost of making a contract change in every copy. A copy keeps its members in the same order as the original. `Demo/Store` uses copies; see [Agents](Agents.md#solution-layout).

## Define the Contracts

The contracts live in `Users.Domain` and describe everything a caller can do.

### Queries

A query is a method on an interface that derives from `IQueryHandler`. Callers get a type-safe proxy, so a remote query reads like a local method call.

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
    Task<List<User>> GetActiveUsers(CancellationToken cancellationToken);
    Task<Stream> ExportUsers(CancellationToken cancellationToken); // streamed live from the service
}
```

Put the `CancellationToken` last. Cancelling the caller's token cancels the call, including the handler's token on the remote service.

### Commands

A command asks for a change and is handled **once**, by one replica of the service. It may return a result.

```csharp
public class CreateUserCommand : ICommand
{
    public string Email { get; set; }
}

public class UpdateUserCommand : ICommand<User>
{
    public int Id { get; set; }
    public string Email { get; set; }
}

public interface IUserCommandHandler :
    ICommandHandler<CreateUserCommand>,
    ICommandHandler<UpdateUserCommand, User>
{
}
```

### Events

An event announces something that already happened. It is delivered to every subscriber.

```csharp
public class UserCreatedEvent : IEvent
{
    public int UserId { get; set; }
    public string Email { get; set; }
}

public interface IEmailEventHandler : IEventHandler<UserCreatedEvent>
{
}
```

> **Command or event?** A command is handled once. An event reaches every replica of every subscriber, unless the subscriber registers its consumer with `EventConsumerMode.PerService`. Work that must happen once, like sending an email or charging a card, belongs in a command or in a `PerService` event handler. See [Command or Event?](Agents.md#command-or-event-read-this-first).

## Write the Handlers

Handlers live in `Users.Service` and derive from `BaseHandler`, which gives them `Bus`, `Log`, and `Context`. Handler instances are shared by concurrent messages, so keep them stateless and get dependencies from `Context`.

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public Task<User> GetUserById(int id, CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().GetByIdAsync(id, cancellationToken);

    public Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().GetActiveAsync(cancellationToken);

    public Task<Stream> ExportUsers(CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().ExportStreamAsync(cancellationToken);
}

public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        var user = await repository.CreateAsync(command.Email, cancellationToken);

        // Tell any subscribers
        await Bus.DispatchAsync(new UserCreatedEvent { UserId = user.Id, Email = user.Email });
    }

    public Task<User> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().UpdateAsync(command.Id, command.Email, cancellationToken);
}

public class EmailEventHandler : BaseHandler, IEmailEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        // Registered PerService below, so one replica sends the email
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmail(@event.Email);
    }
}
```

An exception thrown in a handler comes back to the caller with its type name and message, including across several service hops. Write the message for the user.

## Host the Service

`Program.cs` in `Users.Service` creates the bus, registers the handlers, and exposes them over a transport.

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

ILogger log = new ConsoleLogger();          // your ILogger implementation (see Logging.md)
IBusLogger busLog = new ConsoleBusLogger(); // optional, see Logging.md

var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository());
busServices.AddService<IEmailService>(new EmailService());

var bus = Bus.New("UserService", log, busLog, busServices);

// Local handlers
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IEmailEventHandler>(new EmailEventHandler());

// Expose them over TCP, serialized in binary and encrypted with a shared key
var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AESwithPrefix);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);   // null: no compressor, see Compressors.md
bus.AddQueryServer<IUserQueryHandler>(server);
bus.AddCommandConsumer<IUserCommandHandler>(server);

// Runs until the process is asked to exit, then finishes the work in progress
await bus.WaitForExitAsync(cancellationToken);
```

More in [Server Setup](ServerSetup.md).

## Call the Service

The client builds its own bus and registers where each interface lives. The serializer and encryptor must match the server's.

```csharp
var bus = Bus.New("ClientService", log, busLog);

var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AESwithPrefix);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);
bus.AddQueryClient<IUserQueryHandler>(client);
bus.AddCommandProducer<IUserCommandHandler>(client);

// Queries
var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
var activeUsers = await bus.Call<IUserQueryHandler>().GetActiveUsers(cancellationToken);

// Commands
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com" });      // sent, not awaited
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" }); // waits until handled
var updated = await bus.DispatchAwaitAsync(new UpdateUserCommand { Id = 1, Email = "new@example.com" }); // returns the result
```

Inside a handler, the same calls go through `Bus`: `Bus.Call<IOtherQueryHandler>()`, `Bus.DispatchAsync(...)`, `Bus.DispatchAwaitAsync(...)`. More in [Client Setup](ClientSetup.md).

## Routing

Callers never know where a handler runs. `bus.Call<T>()` and `bus.DispatchAsync(...)` look the same whether the handler is in the same process, across TCP, or behind a message broker. Only the registrations in `Program.cs` change.

### In Process

Register the handler on the caller's own bus and the message goes straight to it. This is how a modular monolith runs, and how tests exercise handlers.

```csharp
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
```

### TCP

```csharp
// Server
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);
bus.AddQueryServer<IUserQueryHandler>(tcpServer);
bus.AddCommandConsumer<IUserCommandHandler>(tcpServer);
bus.AddEventConsumer<IEmailEventHandler>(tcpServer, EventConsumerMode.PerService);

// Client
var tcpClient = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);
bus.AddQueryClient<IUserQueryHandler>(tcpClient);
bus.AddCommandProducer<IUserCommandHandler>(tcpClient);
bus.AddEventProducer<IEmailEventHandler>(tcpClient);
```

### HTTP

```csharp
// Server (authorizer and allowOrigins are optional)
var httpServer = new HttpCqrsServer("localhost:9001", serializer, encryptor, null, authorizer: null, allowOrigins: null, log: log);
bus.AddQueryServer<IUserQueryHandler>(httpServer);
bus.AddCommandConsumer<IUserCommandHandler>(httpServer);

// Client
var httpClient = new HttpCqrsClient("localhost:9001", serializer, encryptor, null, authorizer: null, log: log);
bus.AddQueryClient<IUserQueryHandler>(httpClient);
bus.AddCommandProducer<IUserCommandHandler>(httpClient);
```

To host a service inside ASP.NET Core/Kestrel instead, use the `KestrelCqrsServer*` types from [Zerra.Web](ZerraWeb.md).

### Message Brokers

Commands and events can travel through a broker. Queries are request-response and use TCP or HTTP. Each broker package has one producer class and one consumer class for both commands and events:

| Package | Producer | Consumer |
|---------|----------|----------|
| `Zerra.CQRS.Kafka` | `KafkaProducer` | `KafkaConsumer` |
| `Zerra.CQRS.RabbitMQ` | `RabbitMQProducer` | `RabbitMQConsumer` |
| `Zerra.CQRS.AzureServiceBus` | `AzureServiceBusProducer` | `AzureServiceBusConsumer` |

```csharp
// Server (Kafka shown; RabbitMQ and Azure Service Bus follow the same pattern)
var kafkaConsumer = new KafkaConsumer("localhost:9092", serializer, encryptor, null, log, environment: "dev", userName: null, password: null);
bus.AddCommandConsumer<IUserCommandHandler>(kafkaConsumer);
bus.AddEventConsumer<IEmailEventHandler>(kafkaConsumer, EventConsumerMode.PerService);

// Client
var kafkaProducer = new KafkaProducer("localhost:9092", serializer, encryptor, null, log, environment: "dev", userName: null, password: null);
bus.AddCommandProducer<IUserCommandHandler>(kafkaProducer);
bus.AddEventProducer<IEmailEventHandler>(kafkaProducer);

// RabbitMQ:          new RabbitMQProducer(host, serializer, encryptor, compressor, log, environment)
// Azure Service Bus: new AzureServiceBusProducer(connectionString, serializer, encryptor, compressor, log, environment)
```

`AddEventConsumer` always states a mode. `PerService` means the service's replicas compete, so one of them handles each event. `PerReplica` means every replica gets its own copy, which suits work like dropping a replica's in-memory cache. See [Events](Events.md#choosing-per-replica-or-per-service).

Setup for each broker: [Kafka](KafkaSetup.md), [RabbitMQ](RabbitMQSetup.md), [Azure Service Bus](AzureServiceBusSetup.md).

### Browsers

[Zerra.Web](ZerraWeb.md) hosts a CQRS API gateway in ASP.NET Core. Browsers call it with the front end scripts in `Front End Scripts/`, using JavaScript or TypeScript clients generated from the contracts, and the gateway forwards to the services over whatever route its bus has.

```csharp
app.UseCqrsApiGateway("/CQRS");
```

## Configuration

- **Timeouts, concurrency limits, and shutdown:** the [Bus options](ClientSetup.md#bus-options).
- **Services for handlers:** register them by interface in `BusServices` and get them with `Context.GetService<T>()`. See [Service Injection](ServiceInjection.md).
- **Logging:** `ILogger` for messages, and `IBusLogger` for the start and end of every command, event, and query. See [Logging](Logging.md).
- **Serialization, encryption, and compression:** `ZerraByteSerializer` between services, `ZerraJsonSerializer` for browsers, `ZerraEncryptor` with a shared key, and optionally `ZerraCompressor` for large payloads. See [Serializers](Serializers.md), [Encryptors](Encryptors.md), and [Compressors](Compressors.md).
## Lifecycle

Servers, consumers, clients, and producers start when they are added to the bus.

```csharp
// Wait for the process to be asked to exit (recommended for services)
await bus.WaitForExitAsync(cancellationToken);

// Or stop explicitly
await bus.StopServicesAsync();
```

Stopping closes the servers and consumers so nothing new arrives, waits for the handlers already running, then disposes the producers and clients. No handler is cancelled. See [Finishing Work in Progress](ServerSetup.md#finishing-work-in-progress).

## Best Practices

1. **Choose command or event deliberately.** Exactly-once work goes in a command or a `PerService` event handler, never a `PerReplica` one.
2. **Group related commands and events** on one handler interface per service.
3. **Keep query interfaces focused** on one area.
4. **Keep handlers stateless** and get dependencies from `Context`.
5. **Throw exceptions with messages for the user.** They reach the caller across service hops.
6. **Make handlers idempotent** where a message may be redelivered.
7. **Put service-to-service commands on their own interface** that the web gateway doesn't register, so browsers can't send them.

## Next Steps

- [Store demo](../Demo/Store/README.md): seven services, five data stores, three brokers, a browser gateway, and event sourcing
- [Queries](Queries.md), [Commands](Commands.md), [Events](Events.md): the details of each message type
- [Security](Security.md): claims from the caller to the handler
- [AOT](AOT.md): source generation and Native AOT
- [Repository](Repository.md): data store agnostic LINQ data access (experimental)
- [Documentation Index](Index.md)
