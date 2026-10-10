# Zerra.CQRS.RabbitMQ

RabbitMQ transport for [Zerra](https://www.nuget.org/packages/Zerra) commands and events. Queries always go directly over TCP or HTTP.

## Installation

```bash
dotnet add package Zerra.CQRS.RabbitMQ
```

## Producer

The sending side registers a `RabbitMQProducer` for the interfaces whose commands and events it sends:

```csharp
using Zerra.CQRS;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.CQRS.RabbitMQ;

ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AES_GCM);

var bus = Bus.New("OrderService");

var producer = new RabbitMQProducer("localhost", serializer, encryptor, null, log: null, environment: "dev");
bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEventHandler>(producer);

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
```

## Consumer

The handling side registers its handlers and a `RabbitMQConsumer`, which takes the same constructor arguments:

```csharp
var bus = Bus.New("UserService");

var consumer = new RabbitMQConsumer("localhost", serializer, encryptor, null, log: null, environment: "dev");
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

- Commands compete: **one** replica of the service handles each command.
- Events with `EventConsumerMode.PerReplica` reach **every** replica; with `PerService` one replica of each subscribing service handles each event.
- `RabbitMQConnectionTest` checks the connection at startup, so a service can fall back to a direct `TcpCqrsClient` when the broker isn't running.

## Documentation

- [RabbitMQ Setup](https://github.com/szawaski/Zerra/blob/master/docs/RabbitMQSetup.md) - Topology, connection settings, naming, and connection testing
- [Events](https://github.com/szawaski/Zerra/blob/master/docs/Events.md) - Per-replica and per-service delivery
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
