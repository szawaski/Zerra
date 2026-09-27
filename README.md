# Zerra

[![NuGet](https://img.shields.io/nuget/v/Zerra.svg)](https://www.nuget.org/packages/Zerra)
[![Next: v6.0.0 in development](https://img.shields.io/badge/next-v6.0.0%20in%20development-orange.svg)](docs/UpgradeV5ToV6.md)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**A high-performance CQRS framework for .NET that lets you write services once and decide later where they run.**

Handlers are called the same way whether they live in the same process, across TCP or HTTP, or behind Kafka, RabbitMQ, or Azure Service Bus. Moving a handler from a monolith into its own service changes a few lines of registration, not the code that calls it.

> **Version 6.0.0 is in development.** This branch and its documentation describe v6; the current NuGet release is v5. Moving a v5 solution to v6? Follow the [upgrade guide](docs/UpgradeV5ToV6.md).

## Why Zerra

- **Location-transparent calls.** `bus.Call<IUserQueryHandler>().GetUserById(id)` is a typed method call, local or remote. No controllers, routes, or hand-written HTTP clients.
- **Deploy as you need.** Start as one process, split into microservices later, or test every handler in memory, all with the same handlers and callers.
- **Clear message semantics.** Queries read, commands change state and are handled once, events notify every subscriber. Per-replica or per-service delivery is an explicit choice for each subscriber.
- **Fast by design.** Source generators replace runtime reflection, the binary serializer is compact, and everything is Native AOT compatible.
- **No external dependencies.** Nothing beyond .NET itself on .NET 10. The .NET Standard 2.0 build, which also runs on .NET Framework 4.7.2+, adds only Microsoft's System.* compatibility packages.
- **Complete toolkit.** Built-in binary and JSON serializers, message encryption, claims propagation, a browser API gateway with generated JavaScript and TypeScript clients, and an experimental LINQ repository across SQL Server, PostgreSQL, MySQL, MariaDB, and KurrentDB.

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
bus.AddQueryClient<IUserQueryHandler>(new TcpCqrsClient("users:9001", serializer, encryptor, log));
```

## Get Started

```bash
dotnet add package Zerra
```

- **[Getting Started](docs/GettingStarted.md)**: build a service and a client, then move between in-process, TCP, HTTP, and brokers
- **[Store demo](Demo/Store/README.md)**: seven services, five data stores, three message brokers, a browser gateway, and an event-sourced aggregate
- **[Documentation Index](docs/Index.md)**: every guide, from queries and events to serializers and AOT

## Packages

| Package | Purpose |
|---|---|
| `Zerra` | Bus, handlers, TCP/HTTP transports, serializers, encryption, source generation |
| `Zerra.Web` | ASP.NET Core hosting and the browser CQRS API gateway |
| `Zerra.CQRS.Kafka` | Kafka transport |
| `Zerra.CQRS.RabbitMQ` | RabbitMQ transport |
| `Zerra.CQRS.AzureServiceBus` | Azure Service Bus transport |
| `Zerra.Repository.*` (experimental) | LINQ data access for SQL Server, PostgreSQL, MySQL, MariaDB, in-memory, and KurrentDB ([Repository](docs/Repository.md)) |

Zerra targets .NET 10. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0.

## License

[MIT](LICENSE)
