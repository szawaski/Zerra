[← Back to Documentation](Index.md)

# Server Setup

A service's `Program.cs` creates a bus, registers its handlers, exposes them through a server or broker consumer, and waits for the process to exit.

## Program.cs

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

ILogger log = new ConsoleLogger();          // your ILogger implementation, see Logging.md
IBusLogger busLog = new ConsoleBusLogger(); // optional

var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository(connectionString));

var bus = Bus.New("UserService", log, busLog, busServices);

bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AES_GCM);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);   // null: no compressor, see Compressors.md
bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueryHandler>(server);
bus.AddEventConsumer<IUserEventHandler>(server, EventConsumerMode.PerReplica);

await bus.WaitForExitAsync();
```

- Callers must use the same serializer, encryption key, and compressor. Read the key from configuration or a secret store, never source code.
- Handler instances are shared by concurrent messages, so keep them stateless.
- `AddEventConsumer` always takes an `EventConsumerMode`: `PerReplica` gives every replica of the service a copy of each event, and `PerService` has the replicas compete. See [Choosing per replica or per service](Events.md#choosing-per-replica-or-per-service).
- `Bus.New` also sets the process-wide static `Bus`, so a process normally has one bus.

The [Bus options](ClientSetup.md#bus-options) (timeouts, concurrency limits, `shutdownTimeout`) apply to services as well.

## Servers

| Server | Package | Use |
|---|---|---|
| `TcpCqrsServer` | `Zerra` | Fastest. Service-to-service traffic |
| `HttpCqrsServer` | `Zerra` | HTTP without ASP.NET Core, for networks that only pass HTTP |
| Kestrel (`KestrelCqrsServer*`) | `Zerra.Web` | Inside an ASP.NET Core app. See [Hosting a Service in ASP.NET Core](ZerraWeb.md#hosting-a-service-in-aspnet-core) |

Every server is a query server, a command consumer, and an event consumer at once.

```csharp
var tcpServer = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);

var httpServer = new HttpCqrsServer("localhost:8080", serializer, encryptor, null,
    authorizer: null,     // optional ICqrsAuthorizer to validate request headers
    allowOrigins: null,   // optional CORS origins
    log: log);
```

A service can register more than one server, for example TCP for other services and HTTP for callers that need it.

Servers take the mode passed to `AddEventConsumer` but ignore it. On a direct connection the publisher decides which replicas get an event through the client URLs it registers.

## Message Brokers

A broker consumer handles both commands and events. Topics and queues are named after the handler interface, such as `IUserCommandHandler`, prefixed with the optional `environment`.

```csharp
using Zerra.CQRS.Kafka;
var kafka = new KafkaConsumer("localhost:9092", serializer, encryptor, null, log,
    environment: "dev", userName: null, password: null);   // SASL credentials are optional

using Zerra.CQRS.RabbitMQ;
var rabbit = new RabbitMQConsumer("localhost", serializer, encryptor, null, log, environment: "dev");

using Zerra.CQRS.AzureServiceBus;
var asb = new AzureServiceBusConsumer(connectionString, serializer, encryptor, null, log, environment: "dev");

bus.AddCommandConsumer<IUserCommandHandler>(kafka);
bus.AddEventConsumer<IUserEventHandler>(kafka, EventConsumerMode.PerReplica);
```

A service can mix brokers and servers, for example commands through Kafka and queries over TCP. Queries never go through a broker. See [Kafka](KafkaSetup.md), [RabbitMQ](RabbitMQSetup.md), and [Azure Service Bus](AzureServiceBusSetup.md).

## Generic Host

In a `Host.CreateApplicationBuilder` worker, build the bus in a hosted service and wait on the host's stopping token:

```csharp
builder.Services.AddSingleton<IBusSetup>(sp =>
{
    var bus = Bus.New("UserService", log, busLog, busServices);
    // add handlers, servers, and consumers as above
    return bus;
});
builder.Services.AddSingleton<IBus>(sp => sp.GetRequiredService<IBusSetup>());
builder.Services.AddHostedService<BusHostedService>();

public class BusHostedService(IBusSetup bus) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => bus.WaitForExitAsync(stoppingToken);
}
```

In ASP.NET Core, see [Zerra.Web](ZerraWeb.md).

## Shutdown

### Waiting for Exit

`WaitForExitAsync` returns when the process is exiting or its token is cancelled, without throwing on cancellation. Either way it stops the bus and disposes every producer, consumer, client, and server before returning.

While waiting, it registers for SIGTERM and SIGINT (Ctrl+C), cancels the default termination, stops the bus, and returns, so `Main` ends normally. No extra handler is needed in Docker or Kubernetes. A second signal while the bus is stopping ends the process immediately. On Windows, SIGTERM is the system shutdown or log off event.

Other exits, such as `Environment.Exit`, are held in `AppDomain.CurrentDomain.ProcessExit` until the bus has stopped, for up to the shutdown timeout plus 10 seconds. On .NET Standard 2.0 there is no signal registration, so only that `ProcessExit` path applies.

`StopServicesAsync()` (or `StopServices()`) stops the bus without waiting for an exit signal.

### Finishing Work in Progress

Stopping the bus stops receiving new queries, commands, and events, then waits for the ones already received to finish, including fire-and-forget commands and events. Handlers are never cancelled.

`shutdownTimeout` (default 30 seconds) limits how long stopping waits. Set it a few seconds shorter than the orchestrator's grace period: 10 seconds for `docker stop`, 30 seconds by default in Kubernetes.

```csharp
var bus = Bus.New("UserService", log, shutdownTimeout: TimeSpan.FromSeconds(20));
```

In ASP.NET Core, register `bus.StopServices` on `ApplicationStopped` so Kestrel's requests finish first.

### Exiting After N Commands

`commandToReceiveUntilExit` on `AddCommandConsumer` makes the service exit after that consumer has handled that many commands, for batch jobs and KEDA-style scaling where a short-lived container processes a batch and exits:

```csharp
var bus = Bus.New("UserService", log, busLog, busServices);
bus.AddCommandConsumer<IUserJobHandler>(consumer, commandToReceiveUntilExit: 1);
await bus.WaitForExitAsync();
```

The count belongs to that consumer, so a consumer added with one serves only that interface; adding it again throws. If several consumers are added with a count, the service exits when the first of them finishes.

Once a replica has received that many commands it stops listening while it finishes handling them, so other replicas pick up the commands still waiting. On Kafka it leaves the consumer group and hands over the partition, on RabbitMQ it cancels its consumer, and on Azure Service Bus it closes its receiver.

## See Also

- [Client Setup](ClientSetup.md) - Calling services, and the bus options
- [Service Injection](ServiceInjection.md) - Services for handlers
- [Serializers](Serializers.md), [Encryptors](Encryptors.md), and [Compressors](Compressors.md) - What goes over the wire
- [Logging](Logging.md) - `ILogger` and `IBusLogger`
