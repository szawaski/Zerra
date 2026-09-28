[← Back to Documentation](Index.md)

# RabbitMQ Setup

`Zerra.CQRS.RabbitMQ` carries commands and events through RabbitMQ. Queries always go directly over TCP or HTTP.

```bash
dotnet add package Zerra.CQRS.RabbitMQ
```

## Producer

The sending side registers a `RabbitMQProducer` for the interfaces whose commands and events it sends:

```csharp
using Zerra.CQRS.RabbitMQ;

var producer = new RabbitMQProducer(
    host: "localhost",        // host name or AMQP URI, see below
    serializer: serializer,
    encryptor: encryptor,     // optional
    log: log,                 // optional
    environment: "dev");      // optional exchange prefix

bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEventHandler>(producer);
```

One producer serves any number of interfaces, and RabbitMQ delivers each event to every subscriber, so it is registered once per event interface.

## Consumer

The handling side registers its handlers and a `RabbitMQConsumer`, which takes the same constructor arguments:

```csharp
var consumer = new RabbitMQConsumer("localhost", serializer, encryptor, log, environment: "dev");

bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

### Exchanges and Queues

The consumer declares a different topology for each kind of message:

| | Exchange | Queue | With several replicas |
|---|---|---|---|
| Commands | Direct | one named for the topic, shared | they compete; **one** replica handles each command |
| Events, `PerReplica` | Fanout | a server-named exclusive queue per replica | **every** replica gets a copy |
| Events, `PerService` | Fanout | one named for the topic and the service, shared | they compete; **one** replica handles each event |

The mode is each subscriber's own choice and changes nothing for the publisher or for the other services bound to the same Fanout exchange. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

- The command queue and `PerService` queues aren't auto-deleted, so messages sent while no replica is connected, including during a reconnect, wait for the next one. Messages are transient, so they don't survive a broker restart.
- A `PerReplica` queue is exclusive to its replica's connection, so events sent while that replica is disconnected aren't held for it.

## Connection Settings

`host` is a plain host name or an AMQP URI:

- **Host name**, such as `"localhost"`: port `5672`, the `guest`/`guest` credentials, virtual host `/`, and no TLS.
- **AMQP URI**: sets the credentials, port, virtual host, and TLS in one value. Use `amqps://` for TLS (default port `5671`), and URL-encode special characters in the user name or password, such as `@` as `%40`.

```csharp
var producer = new RabbitMQProducer("amqp://myUser:myPassword@rabbit.example.com:5672/myVhost", serializer, encryptor, log, "prod");
var consumer = new RabbitMQConsumer("amqps://myUser:myPassword@rabbit.example.com/myVhost", serializer, encryptor, log, "prod");
```

The URI contains secrets, so load it from configuration or a secret store. Zerra never logs it.

## Checking the Connection

`RabbitMQConnection.Test` opens and closes a connection and returns whether it opened, waiting five seconds unless given another timeout. It's synchronous because the RabbitMQ client only connects synchronously. The logger, if given, is told why it failed. Use it at startup to fall back to a direct connection when RabbitMQ isn't running. Subscribers make the same check and register the matching consumer, so both ends pick the same route:

```csharp
if (RabbitMQConnection.Test("localhost", log: log))
    bus.AddEventProducer<IOrderEventHandler>(new RabbitMQProducer("localhost", serializer, encryptor, log, null));
else
    bus.AddEventProducer<IOrderEventHandler>(new TcpCqrsClient("localhost:9102", serializer, encryptor, log));
```

`Demo/Store` does this for order events, in `Store.Orders.Service`, `Store.Inventory.Service`, and `Store.Shipping.Service`.

## Names

Exchanges and queues are named after the handler interface, prefixed with the `environment` when one is given, such as `dev_IUserCommandHandler`. Environments can share a server this way.

RabbitMQ limits names to 255 characters, and a `PerService` queue is the exchange name plus the service name. Longer names are shortened to fit, and the producer or consumer logs a warning with the result, since another exchange or service shortening to the same name would share it.

## See Also

- [Events](Events.md) - Per-replica and per-service delivery
- [Kafka Setup](KafkaSetup.md) and [Azure Service Bus Setup](AzureServiceBusSetup.md)
