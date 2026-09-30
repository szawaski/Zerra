[← Back to Documentation](Index.md)

# Client Setup

A caller, whether another service, a web app, or a console app, creates a bus and registers where each interface it uses lives. It then calls queries and dispatches commands and events without knowing where they are handled.

## Program.cs

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

ILogger log = new ConsoleLogger();          // your ILogger implementation, see Logging.md
IBusLogger busLog = new ConsoleBusLogger(); // optional

var bus = Bus.New("ClientApp", log, busLog);

var serializer = new ZerraByteSerializer();                                               // must match the server
var encryptor = new ZerraEncryptor(encryptionKey, SymmetricAlgorithmType.AESwithPrefix);  // must match the server
var users = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);   // null: no compressor, see Compressors.md
bus.AddQueryClient<IUserQueryHandler>(users);
bus.AddCommandProducer<IUserCommandHandler>(users);
bus.AddEventProducer<IUserEventHandler>(users);

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
var active = await bus.Call<IUserQueryHandler>().GetActiveUsers(cancellationToken);

await bus.StopServicesAsync(); // on exit: disposes the clients and producers
```

In ASP.NET Core, register the bus as a singleton `IBus` and inject it where needed. Register `bus.StopServices` on `ApplicationStopped`.

## Clients and Producers

| Client | Package | Reaches |
|---|---|---|
| `TcpCqrsClient(url, serializer, encryptor, compressor, log)` | `Zerra` | a `TcpCqrsServer` |
| `HttpCqrsClient(url, serializer, encryptor, compressor, authorizer, log)` | `Zerra` | an `HttpCqrsServer` |
| `KestrelCqrsClient(url, serializer, encryptor, compressor, log, authorizer, route)` | `Zerra.Web` | a service hosted in ASP.NET Core |
| `ApiClient` | `Zerra` | the CQRS API gateway, see [ApiClient](ApiClient.md) |
| `KafkaProducer`, `RabbitMQProducer`, `AzureServiceBusProducer` | `Zerra.CQRS.*` | a broker; commands and events only |

Each client is a query client, a command producer, and an event producer. The optional `authorizer` supplies request headers, see [Security](Security.md).

Register one client per backend service. A caller can mix transports freely:

```csharp
var users = new TcpCqrsClient("user-service:9001", serializer, encryptor, null, log);
bus.AddQueryClient<IUserQueryHandler>(users);
bus.AddCommandProducer<IUserCommandHandler>(users);

var shipping = new KestrelCqrsClient("http://shipping-service:9105", serializer, encryptor, null, log, authorizer: null, route: null);
bus.AddQueryClient<IShippingQueryHandler>(shipping);

var kafka = new KafkaProducer("localhost:9092", serializer, encryptor, null, log, environment: "dev", userName: null, password: null);
bus.AddCommandProducer<IOrderCommandHandler>(kafka);
```

To send an event to several services over direct connections, register an event producer for each. The bus sends every event to all of them. A broker producer only needs registering once, since the broker delivers to every subscriber.

## Local and Remote Together

A service can handle some interfaces itself and call others remotely. The calling code looks the same either way:

```csharp
bus.AddHandler<IOrderQueryHandler>(new OrderQueryHandler());      // local
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);          // remote

var orders = await bus.Call<IOrderQueryHandler>().GetOrders(cancellationToken);
var products = await bus.Call<ICatalogQueryHandler>().GetProducts(cancellationToken);
```

## Bus Options

```csharp
var bus = Bus.New(
    serviceName: "ClientApp",
    log: log,
    busLog: busLog,
    busServices: busServices,
    defaultCallTimeout: TimeSpan.FromSeconds(30),
    defaultDispatchTimeout: TimeSpan.FromSeconds(5),
    defaultDispatchAwaitTimeout: TimeSpan.FromSeconds(60),
    maxConcurrentQueries: Environment.ProcessorCount * 32,
    maxConcurrentCommandsPerTopic: Environment.ProcessorCount * 8,
    maxConcurrentEventsPerTopic: Environment.ProcessorCount * 16,
    shutdownTimeout: TimeSpan.FromSeconds(30),
    commandToReceiveUntilExit: null
);
```

The concurrency limits and `shutdownTimeout` shown are the defaults. `shutdownTimeout` and `commandToReceiveUntilExit` are described in [Server Setup](ServerSetup.md#shutdown).

### Timeout Configuration

| Option | Applies to | Default |
|---|---|---|
| `defaultCallTimeout` | queries | none |
| `defaultDispatchTimeout` | `DispatchAsync` of commands and events | none |
| `defaultDispatchAwaitTimeout` | `DispatchAwaitAsync` | none |

A dispatch can override them with its own `TimeSpan` or `CancellationToken`. A timeout throws `TimeoutException`.

## Errors

| Exception | Meaning |
|---|---|
| `RemoteServiceException` | the remote handler threw; see `ErrorType` and `Message` |
| `TimeoutException` | no response in time; an awaited command may or may not have run |
| `OperationCanceledException` | the caller's token was cancelled |
| `IOException` or other | the connection failed |

See [Commands](Commands.md#errors) and [Queries](Queries.md#errors).

## See Also

- [Server Setup](ServerSetup.md) - The other end
- [Serializers](Serializers.md), [Encryptors](Encryptors.md), and [Compressors](Compressors.md) - What goes over the wire
- [Logging](Logging.md) - `ILogger` and `IBusLogger`
