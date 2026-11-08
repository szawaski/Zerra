# Zerra

[![NuGet](https://img.shields.io/nuget/vpre/Zerra.svg)](https://www.nuget.org/packages/Zerra/absoluteLatest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**A high-performance CQRS framework for .NET that cuts the code a distributed system needs. Write services once and decide later where they run.**

Handlers are called the same way whether they live in the same process, across TCP or HTTP, or behind Kafka, RabbitMQ, or Azure Service Bus. Moving a handler from a monolith into its own service changes a few lines of registration, not the code that calls it.

## Less Code to Write and Maintain

Most of the plumbing a distributed .NET system needs is never written, so there is less to build, review, test, and keep in sync as the system grows.

- **No API layer.** No controllers, routes, request classes, or HTTP clients to write and keep matched up. The interface is the contract, and the compiler checks every caller, so renaming a method or changing a parameter is an ordinary refactor instead of a breaking change found at runtime.
- **No hand-written browser clients.** The API gateway serves its own JavaScript and TypeScript clients, with the models, queries, and commands already in them, so the front end follows the services without extra work.
- **No rewrite to scale out.** Splitting a monolith into services, or moving a handler from TCP to a broker, changes startup registration only. Handlers and the code that calls them stay as they are.
- **Unit tests without infrastructure.** Handlers run on an in-memory bus against an in-memory store, so unit tests and local development need no servers, brokers, or databases. Integration tests against the real ones still matter, and the same handlers run there unchanged.
- **One way to read and write data.** The same LINQ works on SQL Server, PostgreSQL, MySQL, MariaDB, MongoDB, and in memory, and moving a model to another store is one line at startup. Code First creates and updates the schema, and Database First writes models for an existing database.
- **Fewer packages to keep up with.** The core has no external dependencies, so there are no third-party versions to track, patch, or reconcile.

> **Upgrading from Zerra 5?** Follow the [upgrade guide](docs/UpgradeV5ToV6.md). Zerra 5 and 6 use different wire formats, so services that share messages upgrade together.

## Why Zerra

- **Location-transparent calls.** `bus.Call<IUserQueryHandler>().GetUserById(id)` is a typed method call wherever the handler runs. A remote call can still time out or fail, so timeouts, cancellation that reaches the remote handler, and typed failures are built in and [documented](docs/Reliability.md#remote-calls-look-local).
- **Deploy as you need.** Start as one process, split into microservices later, or unit test every handler in memory, all with the same handlers and callers.
- **Clear message semantics.** Queries read, commands change state and are handled once, events notify every subscriber. Each subscriber chooses per-replica or per-service delivery, and the [delivery guarantees](docs/Reliability.md) are documented for every transport.
- **Fast by design.** Source generators replace runtime reflection, the binary serializer is compact, and everything is [Native AOT](docs/AOT.md) compatible. [Benchmarks](docs/Benchmarks.md) cover the serializers, the bus, and encryption.
- **No external dependencies.** Nothing beyond .NET itself on .NET 10 and 11. The .NET Standard 2.0 build, which also runs on .NET Framework 4.7.2+, adds only Microsoft's System.* packages.
- **Complete toolkit.** Binary and JSON serializers, compression, tamper-proof encryption with [key rotation](docs/Encryptors.md#rotating-keys), claims propagation, a browser API gateway, and a LINQ repository across SQL Server, PostgreSQL, MySQL, MariaDB, MongoDB, and KurrentDB that runs [up to ~8× faster than Entity Framework](docs/Repository.md#compared-with-entity-framework).

## At a Glance

Define the contract in a shared project:

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
}

public class CreateUserCommand : ICommand
{
    public string Email { get; set; }
}
```

Implement it in the service:

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public Task<User> GetUserById(int id, CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().GetByIdAsync(id, cancellationToken);
}
```

Call it from anywhere:

```csharp
var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
```

Where the handler runs is decided only at startup:

```csharp
// In the same process
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

// Or in another service, over TCP
bus.AddQueryClient<IUserQueryHandler>(new TcpCqrsClient("users:9001", serializer, encryptor, null, log));
```

## Get Started

```bash
dotnet add package Zerra
```

- **[Getting Started](docs/GettingStarted.md)**: build a service and a client, then move between in-process, TCP, HTTP, and brokers
- **[Store demo](Demo/Store/README.md)**: eight services, six data stores, three message brokers, a browser gateway, and an event-sourced aggregate
- **[Documentation Index](docs/Index.md)**: every guide, from queries and events to serializers and AOT
- **[Running in Production](docs/Production.md)**: delivery, security, versioning, and observability
- **[Testing](docs/Testing.md)**: line coverage per library and what the tests cover, including transport tests against real brokers and databases

## When to Use Zerra

- **Adding services to an existing system.** Build a new service with Zerra, and an existing app consumes it by adding just the client side: reference the service's contracts, register a client, and call it as a typed interface, from .NET or .NET Framework 4.7.2+. Existing ASP.NET Core apps can also host the [API gateway](docs/ZerraWeb.md) beside their controllers.
- **Starting as a monolith and splitting later.** Write handlers once in one process and move any of them into its own service when the time comes.
- **Choosing the transport per interface.** Direct TCP for fast calls between services, HTTP inside ASP.NET Core, and a broker for work that should be queued, mixed freely within one system.
- **Small, fast services.** Source generation, Native AOT, no external dependencies, and a [binary serializer](docs/Benchmarks.md) that's faster and smaller than JSON keep services quick to start and cheap to run.

## In Production

Zerra runs in production in private commercial applications, across in-process, TCP, and broker deployments. Changes are in the [changelog](CHANGELOG.md), security reports go through the [security policy](SECURITY.md), and the [trust model](docs/Security.md#trust-model) says what encryption and claims protect and what they don't.

## Packages

| Package | Purpose |
|---|---|
| `Zerra` | Bus, handlers, TCP/HTTP transports, serializers, encryption, compression, source generation |
| `Zerra.Web` | ASP.NET Core hosting and the browser CQRS API gateway |
| `Zerra.CQRS.Kafka` | Kafka transport |
| `Zerra.CQRS.RabbitMQ` | RabbitMQ transport |
| `Zerra.CQRS.AzureServiceBus` | Azure Service Bus transport |
| `Zerra.Repository.*` | LINQ data access for SQL Server, PostgreSQL, MySQL, MariaDB, MongoDB, in-memory, and KurrentDB ([Repository](docs/Repository.md)) |

Zerra targets .NET 10 and .NET 11. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0.

## License

[MIT](LICENSE)
