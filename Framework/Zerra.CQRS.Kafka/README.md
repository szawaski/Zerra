# Zerra.CQRS.Kafka

Kafka transport for [Zerra](https://www.nuget.org/packages/Zerra) commands and events. Queries always go directly over TCP or HTTP.

## Installation

```bash
dotnet add package Zerra.CQRS.Kafka
```

## Producer

The sending side registers a `KafkaProducer` for the interfaces whose commands and events it sends:

```csharp
using Zerra.CQRS;
using Zerra.Encryption;
using Zerra.Serialization;
using Zerra.CQRS.Kafka;

ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);

var bus = Bus.New("OrderService");

var producer = new KafkaProducer("localhost:9092", serializer, encryptor, null, log: null, environment: "dev", userName: null, password: null);
bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEventHandler>(producer);

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
```

## Consumer

The handling side registers its handlers and a `KafkaConsumer`, which takes the same constructor arguments:

```csharp
var bus = Bus.New("UserService");

var consumer = new KafkaConsumer("localhost:9092", serializer, encryptor, null, log: null, environment: "dev", userName: null, password: null);
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

- Commands compete: **one** replica of the service handles each command.
- Events with `EventConsumerMode.PerReplica` reach **every** replica; with `PerService` one replica of each subscribing service handles each event.
- `KafkaConnectionTest` checks the connection at startup, so a service can fall back to a direct `TcpCqrsClient` when the broker isn't running.

## Documentation

- [Kafka Setup](https://github.com/szawaski/Zerra/blob/master/docs/KafkaSetup.md) - Topology, connection settings, naming, and connection testing
- [Events](https://github.com/szawaski/Zerra/blob/master/docs/Events.md) - Per-replica and per-service delivery
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
