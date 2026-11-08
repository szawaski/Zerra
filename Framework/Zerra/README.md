# Zerra

A high-performance CQRS framework for .NET that cuts the code a distributed system needs. Write services once and decide later where they run.

Handlers are called the same way whether they live in the same process, across TCP or HTTP, or behind Kafka, RabbitMQ, or Azure Service Bus. Moving a handler into its own service changes a few lines of registration, not the code that calls it.

> **Upgrading from Zerra 5?** Follow the [upgrade guide](https://github.com/szawaski/Zerra/blob/master/docs/UpgradeV5ToV6.md). Zerra 5 and 6 use different wire formats, so services that share messages upgrade together.

## Why Zerra

- **No API layer.** No controllers, routes, request classes, or HTTP clients. The interface is the contract, and the compiler checks every caller.
- **Deploy as you need.** Start as one process, split into services later, or unit test every handler in memory, with the same handlers and callers.
- **Clear message semantics.** Queries read, commands change state and are handled once, events notify every subscriber. Delivery guarantees are [documented](https://github.com/szawaski/Zerra/blob/master/docs/Reliability.md) per transport.
- **Fast by design.** Source generators replace runtime reflection, the binary serializer is compact, and everything is [Native AOT](https://github.com/szawaski/Zerra/blob/master/docs/AOT.md) compatible. See the [benchmarks](https://github.com/szawaski/Zerra/blob/master/docs/Benchmarks.md).
- **No external dependencies** on .NET 10 and 11. The .NET Standard 2.0 build, which also runs on .NET Framework 4.7.2+, adds only Microsoft's System.* packages.
- **Complete toolkit.** Binary and JSON serializers, compression, tamper-proof encryption with key rotation, claims propagation, and a browser API gateway that serves its own JavaScript and TypeScript clients.

## Installation

```bash
dotnet add package Zerra
```

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

Where the handler runs is decided at startup:

```csharp
// Service
var bus = Bus.New("UserService", log, busLog, busServices);
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

var serializer = new NetworkByteSerializer();
var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AES_GCM);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);
bus.AddQueryServer<IUserQueryHandler>(server);
bus.AddCommandConsumer<IUserCommandHandler>(server);
await bus.WaitForExitAsync(cancellationToken);

// Caller
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);
bus.AddQueryClient<IUserQueryHandler>(client);
bus.AddCommandProducer<IUserCommandHandler>(client);
```

Swap the server and client for a `KafkaConsumer` and `KafkaProducer`, or register the handler on the caller's own bus, and nothing else changes.

## Packages

| Package | Purpose |
|---|---|
| `Zerra` | Bus, handlers, TCP/HTTP transports, serializers, encryption, compression, source generation |
| `Zerra.Web` | ASP.NET Core hosting and the browser API gateway |
| `Zerra.CQRS.Kafka`, `Zerra.CQRS.RabbitMQ`, `Zerra.CQRS.AzureServiceBus` | Broker transports |
| `Zerra.Repository.*` | LINQ data access for SQL Server, PostgreSQL, MySQL, MariaDB, MongoDB, in-memory, and KurrentDB |

## Documentation

- [Getting Started](https://github.com/szawaski/Zerra/blob/master/docs/GettingStarted.md) - Build a service and a client, then move between transports
- [Store demo](https://github.com/szawaski/Zerra/blob/master/Demo/Store/README.md) - Eight services, six data stores, three brokers, and a browser gateway
- [Documentation index](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
