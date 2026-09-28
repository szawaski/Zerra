[← Back to Documentation](Index.md)

# Kafka Setup

`Zerra.CQRS.Kafka` carries commands and events through Apache Kafka. Queries always go directly over TCP or HTTP.

```bash
dotnet add package Zerra.CQRS.Kafka
```

## Producer

The sending side registers a `KafkaProducer` for the interfaces whose commands and events it sends:

```csharp
using Zerra.CQRS.Kafka;

var producer = new KafkaProducer(
    host: "localhost:9092",   // bootstrap servers
    serializer: serializer,
    encryptor: encryptor,     // optional
    log: log,                 // optional
    environment: "dev",       // optional topic prefix
    userName: null,           // optional SASL/PLAIN user name
    password: null);          // optional SASL/PLAIN password

bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEventHandler>(producer);

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
```

One producer serves any number of interfaces, and Kafka delivers each event to every subscriber, so it is registered once per event interface.

## Consumer

The handling side registers its handlers and a `KafkaConsumer`, which takes the same constructor arguments:

```csharp
var consumer = new KafkaConsumer("localhost:9092", serializer, encryptor, log, environment: "dev", userName: null, password: null);

bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

### Consumer Groups

The consumer group decides how many replicas of the service handle each message:

| | Consumer group | With several replicas |
|---|---|---|
| Commands | one named for the topic, shared | they compete; **one** replica handles each command |
| Events, `PerReplica` | a new group id per consumer instance | **every** replica gets a copy |
| Events, `PerService` | one named for the topic and the service, shared | they compete; **one** replica handles each event |

The mode is each subscriber's own choice and changes nothing for the publisher or other subscribers. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

- A `PerReplica` group belongs to one consumer, which deletes it when it stops.
- A `PerService` group is shared and stays, so its committed offsets hold events published while the whole service is down.
- Topics are created with one partition, so under `PerService`, as for commands, one replica receives everything and the others stand by to take over.

## Checking the Connection

`KafkaConnection.TestAsync` asks the cluster to describe itself and returns whether it answered, waiting five seconds unless given another timeout. The logger, if given, is told why it failed. Use it at startup to fall back to a direct connection when Kafka isn't running. The receiving side makes the same check and registers the matching consumer, so both ends pick the same route:

```csharp
if (await KafkaConnection.TestAsync("localhost:9092", userName: null, password: null, log: log))
    bus.AddCommandProducer<IStockReservationHandler>(new KafkaProducer("localhost:9092", serializer, encryptor, log, null, null, null));
else
    bus.AddCommandProducer<IStockReservationHandler>(new TcpCqrsClient("localhost:9102", serializer, encryptor, log));
```

`Demo/Store` does this for stock reservations, in `Store.Orders.Service/Program.cs` and `Store.Inventory.Service/Program.cs`.

## Topic Names

A topic is the handler interface's name, prefixed with the `environment` when one is given, such as `dev_IUserCommandHandler`. Environments can share a cluster this way.

Kafka limits names to 249 characters, and a `PerService` group is the topic name plus the service name. Longer names are shortened to fit, and the producer or consumer logs a warning with the result, since another topic or service shortening to the same name would share it.

## See Also

- [Events](Events.md) - Per-replica and per-service delivery
- [RabbitMQ Setup](RabbitMQSetup.md) and [Azure Service Bus Setup](AzureServiceBusSetup.md)
