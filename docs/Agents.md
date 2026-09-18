# Zerra CQRS Framework - AI Agent Context

This document provides architectural context for AI agents working with the Zerra framework.

## Overview

Zerra is a CQRS (Command Query Responsibility Segregation) framework for .NET 10 that enables distributed message-driven architecture. It routes commands, events, and queries locally or remotely via message brokers (Kafka, RabbitMQ, Azure Service Bus) or HTTP.

## Building an Application (Start Here)

Working samples to copy from:

- `Demo/Store`: six microservices (Catalog, Inventory, Orders, Shipping, Reviews, Carts) behind an ASP.NET CQRS gateway, static pages calling the gateway with `Bus.js`, a database per service (PostgreSQL, MySQL, SQL Server, MariaDB, KurrentDB) with in-memory fallback, and seeding on startup. Shipping is hosted in ASP.NET Core/Kestrel instead of raw TCP and needs no database at all. Carts is event sourced with `AggregateRoot` on KurrentDB. Its `README.md` maps each feature to the file that shows it.
- `Demo/Pets.Domain` and `Demo/Pets.Service`: a single service.

Keep samples focused on Zerra: handlers read and write data models through `IRepo` and check business rules inline. Don't add aggregate or repository layers on top.

### Solution Layout

| Project | Contains | References |
|---|---|---|
| `X.Domain` | Contracts: query interfaces, commands, events, the models they carry, and handler interfaces | `Zerra`. Set `IsAotCompatible` |
| `X.Service` | Handler classes, data models, and `Program.cs` that builds the bus | its own `X.Domain`, plus the `X.Domain` of each service it calls, `Zerra`, `Zerra.Repository.*` |
| `X.Web` (optional) | ASP.NET app hosting the CQRS API gateway and the static pages | every `X.Domain` it forwards, `Zerra.Web` |

Services share only `*.Domain` projects, never each other's data models or handlers.

### Project Setup

- Target `net10.0`. The `Zerra` NuGet package brings the source generator (see [AOT](AOT.md)). Inside this repository, reference the projects directly and add the generator to every project that declares or implements CQRS types or data models:
  ```xml
  <ProjectReference Include="..\..\Framework\Zerra\Zerra.csproj" />
  <ProjectReference Include="..\..\Framework\Zerra.SourceGeneration\Zerra.SourceGeneration.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  ```
- `<PublishAot>true</PublishAot>` also turns off dynamic code under `dotnet run`, so a type the generator missed fails in development, not first in production.
- The "Source Generation Startup - ..." console lines at startup are expected.
- Don't set `InvariantGlobalization` in a service that uses `Microsoft.Data.SqlClient`, it can't connect in that mode.

### Contracts

```csharp
public interface ICatalogQueryHandler : IQueryHandler
{
    Task<ProductModel[]> GetProducts(CancellationToken cancellationToken);
    Task<ProductModel[]> GetProductsByIDs(Guid[] productIDs, CancellationToken cancellationToken);
}

public interface ICatalogCommandHandler :
    ICommandHandler<AddProductCommand, AddProductResult>, //command with a result
    ICommandHandler<DiscontinueProductCommand>             //command without one
{
}

public sealed class AddProductCommand : ICommand<AddProductResult>
{
    public required string Sku { get; set; }
    public required decimal Price { get; set; }
}

public interface IOrderEventHandler : IEventHandler<OrderShippedEvent> { }
```

- Put `CancellationToken` last on query methods. Clients don't send it, the server passes its own in its place.
- Command handler methods are `Handle(TCommand command, CancellationToken cancellationToken)`, event handler methods are `Handle(TEvent @event)`.
- Models are plain classes with public properties. `required` members work with the serializers and the generated clients, but a value missing from a browser's JSON arrives as its default, so handlers still validate their input.

### Service Program.cs

```csharp
ILogger log = new ConsoleLogger();          //your Zerra.Logging.ILogger implementation
Log.SetLog(log);                            //framework messages too, such as why a database was skipped

var repo = Repo.New();
repo.AddProvider(new CatalogStoreProvider<ProductDataModel>());
var busServices = new BusServices();
busServices.AddRepo(repo);                  //handlers deriving from BaseHandlerWithRepo get it as Repo

var bus = Bus.New("Catalog", log, busLog, busServices);
bus.AddHandler<ICatalogQueryHandler>(new CatalogQueryHandler());
bus.AddHandler<ICatalogCommandHandler>(new CatalogCommandHandler());

var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AESwithPrefix);
var server = new TcpCqrsServer("localhost:9101", serializer, encryptor, log);
bus.AddQueryServer<ICatalogQueryHandler>(server);
bus.AddCommandConsumer<ICatalogCommandHandler>(server);

//calling another service: a client, then register what this service may use
var inventory = new TcpCqrsClient("localhost:9102", serializer, encryptor, log);
bus.AddCommandProducer<IStockReservationHandler>(inventory);
bus.AddEventProducer<IOrderEventHandler>(inventory);

await bus.WaitForExitAsync(exitToken);
```

Handler instances are created once and shared by concurrent messages, so keep them stateless. Inside a handler, use `Bus.Call<IOtherQueryHandler>().Method(...)`, `Bus.DispatchAsync(...)`, `Bus.DispatchAwaitAsync(...)`, `Repo`, `Log`, and `Context.GetService<T>()`. More in [Server Setup](ServerSetup.md), [Client Setup](ClientSetup.md), and [Service Injection](ServiceInjection.md).

### Hosting a Service in ASP.NET Core Instead of TCP

A service doesn't have to use `TcpCqrsServer`. Hosting it in ASP.NET Core/Kestrel over HTTP is a `ProjectReference` to `Zerra.Web` plus one shared settings object instead of one server object:

```csharp
var builder = WebApplication.CreateBuilder(args);
//... build repo, busServices, bus, and add handlers exactly as with any other service

var settings = new KestrelCqrsServerLinkedSettings(route: null, authorizer: null, contentType: ContentType.Bytes);
bus.AddQueryServer<IShippingQueryHandler>(new KestrelCqrsServerQueryServer(settings));
bus.AddCommandConsumer<IShippingCommandHandler>(new KestrelCqrsServerCommandConsumer(settings));
bus.AddEventConsumer<IOrderEventHandler>(new KestrelCqrsServerEventConsumer(settings));

var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(bus.StopServices);
app.UseKestrelCqrsServer(serializer, encryptor, log, settings);
app.Run();
```

A caller reaches it with `KestrelCqrsClient` (also in `Zerra.Web`) instead of `TcpCqrsClient`/`HttpCqrsClient`, same constructor shape plus an `authorizer` and `route` (both `null` if the server used `null`). Query and command handling work exactly like any other service; only the transport differs, so a gateway or another service can mix TCP and HTTP clients for different downstream services without anything else changing.

### Two Services Subscribing to the Same Event

An event type can have more than one producer. Register an event producer for each downstream service and the bus sends every event to all of them. Adding the same producer instance twice for one event type is ignored (logged) so it isn't sent twice:

```csharp
bus.AddEventProducer<IOrderEventHandler>(inventoryClient);   //TcpCqrsClient
bus.AddEventProducer<IOrderEventHandler>(shippingClient);    //KestrelCqrsClient
```

See `Store.Orders.Service/Program.cs`. A message broker producer (Kafka/RabbitMQ/AzureServiceBus) only needs registering once, the broker delivers each event to every subscribed consumer on its own.

### Web Gateway for Browsers

```csharp
var bus = Bus.New("Web", log, busLog);
bus.AddQueryClient<ICatalogQueryHandler>(catalogClient);
bus.AddCommandProducer<ICatalogCommandHandler>(catalogClient);

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());
builder.Services.AddSingleton(log);
...
app.UseCqrsApiGateway("/CQRS");
```

- The gateway only exposes interfaces its bus has a route for, so leave out anything meant only for service-to-service use.
- Public sites need an `ICqrsAuthorizer` and `allowOrigins`. See [Zerra.Web](ZerraWeb.md) and [Security](Security.md).
- In ASP.NET projects `ILogger` is ambiguous with Microsoft's, so write `Zerra.Logging.ILogger`.

### Browser Clients

- Copy `Front End Scripts/JavaScript/Bus.js` (jQuery) or `Front End Scripts/TypeScript/Bus.ts` (fetch) into the site.
- Generate the typed client from the `*.Domain` sources with a T4 template that calls `Zerra.T4.CQRSClientDomain.GenerateJavaScript(folder)` or `GenerateTypeScript(folder)`, using `Front End Scripts/Binaries/Zerra.T4.dll`. See `Demo/Store/Store.Web/wwwroot/js/JavaScriptModels.tt`. Scan only your own folder, and regenerate after changing contracts.
- Generated query functions omit the trailing `CancellationToken`.

### Errors

Throw an exception whose message is written for the user. It comes back to the caller with its type name and message, including across several service hops. A relayed `SecurityException` becomes HTTP 401 at the gateway. `Demo/Store` uses a `DomainException` for rule violations.

### Zerra.Repository

Each service owns its data store. Details in [Repository](Repository.md), [Repository Generation](RepositoryGeneration.md), and [Graph](Graph.md).

```csharp
[Entity("SalesOrder")]
public sealed class OrderDataModel
{
    [Identity(false)]                        //false: the key is assigned by the code, not the database
    public Guid ID { get; set; }

    [StoreProperties(true, 32)]              //not null, max length 32
    public string? OrderNumber { get; set; }

    public Guid CustomerID { get; set; }

    [Relation(nameof(CustomerID))]           //many-to-one: this model's foreign key
    public CustomerDataModel? Customer { get; set; }

    [Relation(nameof(OrderLineDataModel.OrderID))] //one-to-many: the related model's foreign key
    public OrderLineDataModel[]? Lines { get; set; }
}
```

- `DataContextSelector` uses the first context that validates, so list the real database first and a `MemoryDataContext` last as the fallback.
- Create the schema on startup with `CodeFirstGeneration.Generate<TContext>(DataStoreGenerationType.CodeFirst | DataStoreGenerationType.NoDelete, modelTypes, log)`, then seed only when the store is empty. Data models need a parameterless constructor.
- Handlers derive from `BaseHandlerWithRepo` and use the async `IRepo` methods. LINQ `Where` expressions support comparisons, `&&`, `||`, `!`, bool members, `string.Contains`, `array.Contains(x.Prop)`, `Any`/`All`/`Count` on related collections, and date parts such as `x.PlacedOn.Year`. `StartsWith` and `EndsWith` aren't translated.
- Relations load only when named in a graph: `Repo.ManyAsync<OrderDataModel>(new Graph<OrderDataModel>(true, x => x.Customer, x => x.Lines))`. One-to-many properties can be arrays, `List<T>`, or interfaces like `IReadOnlyList<T>`.
- Update only some columns by passing a graph: `Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status))`.
- With `PersistLinking => false`, related models aren't saved with their parent. Create the order and then its lines.
- Pass collections to `CreateAsync`/`UpdateAsync`/`DeleteAsync` as arrays or with an explicit type argument. A `List<T>` binds to the single-model overload.
- Without a precision, PostgreSQL, MySQL, and MariaDB store date and time columns to the microsecond, and SQL Server stores `DateTime` as `datetime` (about 3 ms). Set `[StoreProperties(notNull, precision)]` to choose.

## Core Concepts

### Commands (`ICommand`)
- Represent actions that modify state
- Fire-and-forget or with acknowledgment from remote service
- **Simple Command** (`ICommand`): No return value
- **Command with Result** (`ICommand<TResult>`): Returns a typed result and automatically awaits remote completion

### Events (`IEvent`)
- Represent state changes that have occurred
- Published to zero or more subscribers
- Multiple handlers can respond to the same event
- Used for event sourcing and eventual consistency

### Queries
- Represent read operations that return data
- Synchronous request-response pattern
- Implemented via interface methods (not explicit types)
- Defined in handler interfaces like `IUserQueries : IQueryHandler`

## The Bus

Central message router created via `Bus.New()`:

**Key Parameters:**
- `serviceName`: Service identifier (required)
- `log`: Optional `ILogger` instance
- `busLog`: Optional `IBusLogger` for cross-service logging
- `busServices`: Optional `BusServices` containing registered dependencies
- `commandToReceiveUntilExit`: Optional count for graceful shutdown
- `defaultCallTimeout`, `defaultDispatchTimeout`, `defaultDispatchAwaitTimeout`: Optional `TimeSpan` timeouts
- `maxConcurrentQueries`, `maxConcurrentCommandsPerTopic`, `maxConcurrentEventsPerTopic`: Optional concurrency limits

`Bus.New()` returns `IBusSetup` (which extends `IBus`) and also sets the static `Bus` instance.

**Responsibilities:**
- Routes commands/events/queries to local handlers or remote producers
- Manages lifecycle of consumers, producers, clients, servers
- Provides `BusContext` to handlers
- Counts received commands with `CommandCounter` to support exit-after-N-commands

## Handlers

All handlers inherit from `BaseHandler` (implements `IHandler`).

### Handler Types

| Type | Interface | Purpose |
|------|-----------|---------|
| Command Handler | `ICommandHandler<T>` where `T : ICommand` | `Task Handle(T command, CancellationToken cancellationToken)` |
| Command Result Handler | `ICommandHandler<T, TResult>` where `T : ICommand<TResult>` | `Task<TResult> Handle(T command, CancellationToken cancellationToken)` |
| Event Handler | `IEventHandler<T>` where `T : IEvent` | `Task Handle(T @event)` (no `CancellationToken`) |
| Query Handler | `IQueryHandler` (marker) | Base for query interfaces |

### BusContext Access

Handlers receive `BusContext` via `this.Context`:
- `this.Bus`: Access to bus for dispatching (same as `Context.Bus`)
- `this.Log`: Optional logger (same as `Context.Log`)
- `Context.GetService<TInterface>()`: Retrieve registered dependencies (throws if not registered)
- `Context.TryGetService<TInterface>(out var service)`: Retrieve optional dependencies
- `Context.ServiceName`: Current service name

## Routing Modes

### Local Processing
Handler registered locally → invoked in-process immediately

### Remote Processing
- **Commands**: Producer sends to broker or server → Consumer receives and processes
  - `DispatchAsync()`: Fire-and-forget
  - `DispatchAwaitAsync()`: Wait for completion signal
- **Events**: Producer publishes → Multiple consumers subscribe (parallel processing)
- **Queries**: Client sends HTTP/TCP request → Server processes and responds

## Message Flow

### Command Dispatch
1. `bus.DispatchAsync(command)` called
2. Bus finds handler (local or remote producer)
3. If local: invoke immediately
4. If remote: send to broker
5. If awaiting: wait for completion or result
6. Return/throw result

**Variants:**
- `DispatchAsync(ICommand)` - Fire-and-forget
- `DispatchAwaitAsync(ICommand)` - Wait for remote service completion signal
- `DispatchAwaitAsync<TResult>(ICommand<TResult>)` - Wait for result (auto-awaits remote)

### Event Dispatch
1. `bus.DispatchAsync(event)` called
2. Bus finds all handlers/producers
3. Invoke locally or send to brokers in parallel
4. Wait for all to complete (or immediate if local)

**Characteristics:**
- Multiple handlers per event
- No return value
- Parallel remote processing

### Query Call
1. `bus.Call<IQueryInterface>().QueryMethod(...)` called
2. Bus finds handler (local or remote client)
3. Invoke locally or send HTTP/TCP request
4. Return result

**Characteristics:**
- Single handler per interface
- Type-safe methods
- Synchronous request-response

## Concurrency & Throttling

```csharp
maxConcurrentQueries = Environment.ProcessorCount * 32
maxConcurrentCommandsPerTopic = Environment.ProcessorCount * 8
maxConcurrentEventsPerTopic = Environment.ProcessorCount * 16
```

**CommandCounter** enables graceful shutdown after N commands:
```csharp
var bus = Bus.New("MyService", commandToReceiveUntilExit: 100);
```

## Logging

`IBusLogger` interface provides message lifecycle hooks:

**Methods:**
- `BeginCommand/Event/Call`: Invoked at message start
- `EndCommand/Event/Call`: Invoked at message completion

**Parameters:**
- `service`: Current processing service
- `source`: Originating service
- `handled`: True if local, false if remote
- `milliseconds`: Processing duration
- `ex`: Exception if failed

## Timeout Handling

Three configurable levels:
- `defaultCallTimeout`: Query operations
- `defaultDispatchTimeout`: Dispatch without await
- `defaultDispatchAwaitTimeout`: Dispatch with await

Override per-call with a `TimeSpan` or a `CancellationToken`:
```csharp
await bus.DispatchAsync(command, TimeSpan.FromSeconds(5));
await bus.DispatchAsync(command, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
```

Timeouts surface as `TimeoutException`.

## Lifecycle

**Startup:**
```csharp
var bus = Bus.New(...);
bus.AddHandler<IHandlers>(handler);
bus.AddCommandProducer/Consumer/EventProducer/Consumer/QueryClient/Server(...);
// Producers/consumers/servers start automatically
```

**Shutdown:**
```csharp
await bus.StopServicesAsync();  // Explicit shutdown
// OR
await bus.WaitForExitAsync(cancellationToken);  // Wait for exit signal
```

Both close consumers/servers, dispose all producers/consumers/clients/servers, and stop processing new messages.

## Integration Points

### Message Broker Implementations
- `Zerra.CQRS.Kafka`: Kafka producer/consumer, `KafkaConnection.TestAsync` to check the cluster is reachable
- `Zerra.CQRS.RabbitMQ`: RabbitMQ producer/consumer, `RabbitMQConnection.Test` to check the server is reachable
- `Zerra.CQRS.AzureServiceBus`: Azure Service Bus producer/consumer, `AzureServiceBusConnection.TestAsync` to check the namespace is reachable

The Store demo checks each broker at startup and falls back to direct TCP/HTTP when it isn't running: the sender registers either the broker's producer or the direct client, and the receiver makes the same check and registers either the broker's consumer or its direct consumer, so both ends pick the same route. See "Message brokers" in `Demo/Store/README.md`.

Types the brokers serialize must work without dynamic code (an app with `PublishAot` disables it even under `dotnet run`): messages are serialized by their runtime type, the envelopes carry the message type as an assembly qualified name resolved by `TypeFinder`, and the envelope and `Acknowledgement` classes have `[GenerateTypeDetail]` with public setters, since the source generator only sets public properties.

### HTTP/Network
- `TcpCqrsServer` / `HttpCqrsServer` act as query servers and command/event consumers
- `TcpCqrsClient` / `HttpCqrsClient` act as query clients and command/event producers
- `Zerra.Web` hosts the bus in ASP.NET Core and provides the external API gateway

## Best Practices

1. **Group handlers by interface**: One interface for related commands/events
2. **Keep query interfaces focused**: One responsibility per query interface
3. **Register dependencies**: Register in `BusServices` for handler access
4. **Handle exceptions**: Propagate from handlers to callers; use `IBusLogger` for tracking
5. **Eventual consistency**: Events for loose coupling, commands for intent, handle duplicates
6. **Dependency injection**: Handlers receive dependencies via `BusContext.GetService<T>()`

## Architecture Summary

```
Application → Bus (Router) → Handlers/Producers/Consumers/Clients/Servers
                    ↓
                Logging (IBusLogger)
                Command Counter (Exit after N commands)
                Bus Context (Dependency Access)
                    ↓
            Message Broker / TCP / HTTP Network
                    ↓
            Remote Services (same pattern)
```

## Key Implementation Details

### IBusInternal (Generated Proxy Support)
- `_CallMethod<TReturn>()`: Sync query calls
- `_CallMethodTask()`: Async task queries
- `_CallMethodTaskGeneric<TReturn>()`: Async generic queries
- `_DispatchCommandInternalAsync()`: Command dispatch
- `_DispatchEventInternalAsync()`: Event dispatch

### Source Generation
Creates proxy implementations of query interfaces, routing metadata, and handler invocation code.

### Async Pattern
- Commands: `Task` or `Task<TResult>`
- Events: `Task`
- Queries: Sync, `Task`, or `Task<TResult>`

## AI Agent Guidelines

When working with Zerra code:

### When Creating Handlers
- Always inherit from `BaseHandler`, or `BaseHandlerWithRepo` when the handler uses `IRepo`
- Implement the appropriate handler interface (`ICommandHandler<T>`, `IEventHandler<T>`, etc.)
- Access dependencies via `this.Context.GetService<TInterface>()`
- Use `this.Context.Bus` to dispatch additional commands/events

### When Adding Bus Routes
- Check if handler is local (performance priority) or remote (scalability priority)
- Use `AddHandler<TInterface>()` for local processing
- Use `AddCommandProducer/Consumer`, `AddEventProducer/Consumer`, `AddQueryClient/Server` for remote
- Consider concurrency limits when routing high-volume operations

### When Implementing Commands
- Use `ICommand` for fire-and-forget operations
- Use `ICommand<TResult>` when caller needs a response
- Simple commands should be idempotent when used with `DispatchAwaitAsync()`

### When Implementing Events
- Events should represent completed state changes, not intents
- Design for multiple subscribers
- Handle duplicate events gracefully (idempotent handlers)

### When Creating Query Interfaces
- Keep interfaces focused (single responsibility)
- Include a `CancellationToken` as the last parameter for cancellation support
- Return `Task<T>` for async, or sync if needed
- Query calls are type-safe and routed via proxy generation
- **Special case**: If return type is `Stream`, the response will be live-streamed from the remote service

## Working on the Framework Itself

- Zerra is built for maximum runtime performance. In runtime code (serializers, the bus, network clients and servers, repository engines), make the smallest in-place change and match the existing style, even when that repeats a few lines. Don't extract shared helpers or add layers.
- Never block on async code with `.GetAwaiter().GetResult()`, `.Result`, or `.Wait()`. Add a real synchronous path instead.
- Runtime code must stay AOT compatible (`IsAotCompatible` is on). Get type information from `TypeDetail` (source generated) instead of generating it at runtime. Where a call is flagged with `RequiresDynamicCode` but is safe, suppress `IL3050` with a comment explaining why, as the existing code does.
- Tests: `Tests/Zerra.Test` (core, serializers, CQRS network, plus `Web/` for the Kestrel middleware and clients), `Tests/Zerra.SourceGeneration.Test`, and `Tests/Zerra.Repository.Test`. The repository engine tests need local SQL Server, PostgreSQL (5432), MySQL (3306), and MariaDB (3307); the connection strings are in `Tests/Zerra.Repository.Test/*/*TestSqlDataContext.cs`. Each run drops and recreates the test database, and after code-first generation the tests assert the generated plan is empty. The messaging tests in the same project (`Kafka/`, `RabbitMQ/`, `AzureServiceBus/`) run the shared `MessageTest` sequence through the producer and consumer interfaces and need Kafka (9092), RabbitMQ (5672), and the Service Bus emulator (AMQP 5673, management 5300); each run uses its own topics and deletes them afterwards. `Demo/Infrastructure/start-infrastructure.ps1` starts any of these data stores and messaging services in Docker that aren't already running.
- `Framework/Zerra.T4` targets net48 and copies its build to `Front End Scripts/Binaries`. Rebuild it after changing the JavaScript or TypeScript generators, and keep `Bus.js` and `Bus.ts` in step with each other.
- The repository uses CRLF line endings in the working tree. Some command-line tools strip the CRs (Git Bash `sed -i`, for one), so check with `git ls-files --eol`.
