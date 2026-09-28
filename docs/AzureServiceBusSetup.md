[← Back to Documentation](Index.md)

# Azure Service Bus Setup

`Zerra.CQRS.AzureServiceBus` carries commands and events through Azure Service Bus. Queries always go directly over TCP or HTTP.

```bash
dotnet add package Zerra.CQRS.AzureServiceBus
```

## Producer

The sending side registers an `AzureServiceBusProducer` for the interfaces whose commands and events it sends:

```csharp
using Zerra.CQRS.AzureServiceBus;

var producer = new AzureServiceBusProducer(
    host: connectionString,   // the namespace's connection string
    serializer: serializer,
    encryptor: encryptor,     // optional
    log: log,                 // optional
    environment: "dev");      // optional queue and topic prefix

bus.AddCommandProducer<IUserCommandHandler>(producer);
bus.AddEventProducer<IUserEventHandler>(producer);
```

One producer serves any number of interfaces, and Service Bus delivers each event to every subscription, so it is registered once per event interface.

## Consumer

The handling side registers its handlers and an `AzureServiceBusConsumer`, which takes the same constructor arguments:

```csharp
var consumer = new AzureServiceBusConsumer(connectionString, serializer, encryptor, log, environment: "dev");

bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddCommandConsumer<IUserCommandHandler>(consumer);
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

### Queues and Subscriptions

Commands get a queue and events get a topic. How the topic is subscribed decides how many replicas handle each event:

| | Entity | With several replicas |
|---|---|---|
| Commands | one queue named for the topic, shared | they compete; **one** replica handles each command |
| Events, `PerReplica` | the topic, with a new `EVT-{guid}` subscription per consumer instance | **every** replica gets a copy |
| Events, `PerService` | the topic, with one `EVT-{serviceName}` subscription, shared | they compete; **one** replica handles each event |

The service name is the one passed to `Bus.New`. The mode is each subscriber's own choice and changes nothing for the publisher or other subscribers. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).

- A `PerReplica` subscription is created with `AutoDeleteOnIdle` and deleted when its consumer stops.
- A `PerService` subscription never auto-deletes, so it holds events published while the whole service is down.

## Checking the Connection

`AzureServiceBusConnection.TestAsync` reads the namespace's properties through the administration endpoint, the same one that creates queues and topics, and returns whether it answered, waiting five seconds unless given another timeout. The logger, if given, is told why it failed. Use it at startup to fall back to a direct connection when Service Bus isn't reachable. The receiving side makes the same check and registers the matching consumer, so both ends pick the same route:

```csharp
if (await AzureServiceBusConnection.TestAsync(connectionString, log: log))
    bus.AddCommandProducer<IReviewsCommandHandler>(new AzureServiceBusProducer(connectionString, serializer, encryptor, log, null));
else
    bus.AddCommandProducer<IReviewsCommandHandler>(new TcpCqrsClient("localhost:9104", serializer, encryptor, log));
```

`Demo/Store` does this for review commands, in `Store.Web/Program.cs` and `Store.Reviews.Service/Program.cs`.

### The Emulator

The Service Bus emulator serves AMQP on its endpoint's port but the administration API only on port 5300. When the connection string has `UseDevelopmentEmulator=true`, Zerra sends administration calls, including the connection test, to port 5300 on the same host, so one connection string works for both.

## Names

Queues and topics are named after the handler interface, prefixed with the `environment` when one is given, such as `dev_IUserCommandHandler`. Environments can share a namespace this way.

Service Bus limits entity names to 50 characters, short enough to hit with an environment prefix and a long interface name, and a `PerService` subscription is `EVT-` plus the service name. Longer names are shortened to fit, and the producer or consumer logs a warning with the result, since another entity or service shortening to the same name would share it.

## See Also

- [Events](Events.md) - Per-replica and per-service delivery
- [Kafka Setup](KafkaSetup.md) and [RabbitMQ Setup](RabbitMQSetup.md)
