# Upgrading from Zerra 5 to Zerra 6

This guide is written for an AI agent (or a developer) moving a solution from Zerra 5.x (`release/5.4.0`) to Zerra 6. The goal is a solution that builds and runs on v6. Native AOT is **not** part of this upgrade; leave it for later (see [AOT](AOT.md)).

Before changing code, read [Agents.md](Agents.md), at least "Building an Application" and "Command or Event?". The v6 shape to aim for is in `Demo/Store` (several services, a web gateway, brokers, databases) and `Demo/Pets` (one service).

## What Changed

Zerra 5 set itself up by scanning assemblies. `Config.LoadConfiguration` turned on discovery, `cqrssettings.json` said which service hosts which interfaces, `Bus.StartServices` built the servers and clients and found handler classes by scanning, the static `Repo` found data providers by scanning, and the static `Log` found an `ILoggingProvider` by scanning.

Zerra 6 does no scanning. Each service's `Program.cs` builds everything itself: a logger, a repo with its providers, a bus, the handler instances, and the servers, clients, producers, and consumers. Handlers derive from `BaseHandler` or `BaseHandlerWithRepo` and use the `Bus`, `Log`, and `Repo` members they inherit instead of the static classes.

| Area | Zerra 5 | Zerra 6 |
|---|---|---|
| Target framework | netstandard2.0 through net10.0 | net10.0 and netstandard2.0 for `Zerra`, `Zerra.Web`, `Zerra.CQRS.*`; net10.0 only for `Zerra.Repository.*` |
| Routing config | `cqrssettings.json` + `CQRSSettings.Get` + `Bus.StartServices(settings, serviceCreator)` | Code in `Program.cs`: `Bus.New(...)`, `AddHandler`, `AddQueryServer`/`AddQueryClient`, `AddCommandConsumer`/`AddCommandProducer`, `AddEventConsumer`/`AddEventProducer` |
| Handlers | Found by discovery | Created with `new` and registered with `bus.AddHandler<IInterface>(handler)`. Must derive from `BaseHandler` |
| Bus | Static `Bus` class | `IBusSetup` instance from `Bus.New`. A static `Bus` wrapper remains but should be avoided |
| Repository | Static `Repo.QueryAsync(new QueryMany<T>(...))`, providers discovered | `IRepo` instance from `Repo.New()`, providers added with `repo.AddProvider`, methods like `ManyAsync<T>(...)` |
| Logging | Static `Log.InfoAsync`, discovered `ILoggingProvider`, `Zerra.Logger` package | `Zerra.Logging.ILogger` you implement, passed to `Bus.New` and set with `Log.SetLog` |
| Configuration | `Zerra.Config` | Removed. Bring your own (see [Configuration](#9-configuration)) |
| Network encryption | `SymmetricConfig` built from an `EncryptionKey` string | `IEncryptor`, normally `new ZerraEncryptor(key, SymmetricAlgorithmType.AESwithPrefix)` |
| Serialization choice | `ContentType` passed to clients and servers | `ISerializer` instance: `ZerraByteSerializer` or `ZerraJsonSerializer` |

v5 and v6 services can't talk to each other, since the wire format and the encryption changed. Upgrade every service that shares messages at the same time.

## Checklist

Work through these steps in order and build after each one:

1. [Inventory](#1-inventory) every v5 API the solution uses.
2. [Projects and packages](#2-projects-and-packages).
3. [Contracts](#3-contracts-domain-projects) in the `*.Domain` projects.
4. [Handlers](#4-handlers).
5. [Repository](#5-repository).
6. [Replace `cqrssettings.json`](#6-replace-cqrssettingsjson-with-programcs) with bus setup in code.
7. [Logging](#7-logging).
8. [ASP.NET projects](#8-aspnet-projects).
9. [Configuration](#9-configuration).
10. [Other removed APIs](#10-other-removed-apis).
11. [Static Bus and Log](#11-static-bus-and-log): remove the ones that are left where you can.
12. [Verify](#12-verify).

## 1. Inventory

Search the solution (skip `bin`/`obj`) and write down which of these appear. Each one points to the section that handles it.

| Search for | Section |
|---|---|
| `cqrssettings`, `CQRSSettings`, `Zerra.CQRS.Settings`, `ServiceCreator`, `Bus.StartServices` | [6](#6-replace-cqrssettingsjson-with-programcs) |
| `ServiceExposed`, `ServiceBlocked`, `ServiceSecure`, `NetworkType` | [3](#3-contracts-domain-projects) |
| `using Zerra.Providers` | [3](#3-contracts-domain-projects), [5](#5-repository) |
| Classes implementing `ICommandHandler`, `IEventHandler`, or a query interface | [4](#4-handlers) |
| `Repo.Query`, `Repo.Persist`, `new QueryMany<`, `new QuerySingle<`, `new QueryFirst<`, `new QueryAny<`, `new QueryCount<`, `new Create<`, `new Update<`, `new Delete<`, `new DeleteByID<`, `EventQuery`, `TemporalQuery` | [5](#5-repository) |
| `MsSqlDataContext`, `PostgreSqlDataContext`, `MySqlDataContext`, `DataStoreGenerationType`, `BaseTransactStore*Provider` | [5](#5-repository) |
| `Log.` with `Async(`, `ILoggingProvider`, `Zerra.Logger`, `BusLoggingProvider`, `IBusLogger`, `Bus.AddLogger`, `AddZerraLogger` | [7](#7-logging) |
| `UseCqrsApiGateway`, `ICqrsAuthorizer`, `ApiClient`, `KestrelServiceCreator` | [8](#8-aspnet-projects) |
| `Config.` | [9](#9-configuration) |
| `Resolver.`, `Discovery.`, `Instantiator.`, `.ForEach(`, `AppendAnd`, `AppendOr`, `ToLinqString`, `WaitAsync`, `Zerra.Threading`, `Zerra.Mathematics`, `Zerra.Identity`, `SymmetricConfig`, `AESwithShift`, `TcpRawCqrs`, `QueryStringSerializer`, `MapperWithLog`, `using Zerra.Linq` | [10](#10-other-removed-apis) |
| `Bus.Call`, `Bus.Dispatch`, static `Log.` calls outside handlers | [11](#11-static-bus-and-log) |
| `MemberDetails`, `SerializableMemberDetails`, `MethodDetailsBoxed`, `ParameterDetails`, `TypeDetailBoxed`, `GetMethodBoxed`, `GetGenericMethodDetail`, `GetGenericTypeDetail`, `GetNiceName`, `CoreTypeLookup`, `IsTask` | [10](#reflection) |
| `JsonConverter<`, `JsonValueType`, `JsonSerializer.AddConverter`, `Mapper.Copy` on types without a public parameterless constructor | [10](#serialization-and-mapping) |
| `[ServiceLog]` on a method | [3](#3-contracts-domain-projects) |
| `EnumName.Parse`, `EnumName.TryParse`, `.ToEnum`, `.ToEnumNullable` (now case-sensitive) | [10](#10-other-removed-apis) |
| `RemoteServiceException`, or code that catches an exception type thrown by another service | [10](#remote-exceptions) |
| `Discovery.DefineClassByInterface` (tests that swap in mock handlers) | [4](#4-handlers) |

## 2. Projects and Packages

- **Target `net10.0`** in services and in every project that references `Zerra.Repository.*`, which builds only for net10.0. Libraries (such as `*.Domain` projects) and .NET Framework apps that reference only `Zerra`, `Zerra.Web`, or `Zerra.CQRS.*` can stay on `netstandard2.0` or .NET Framework 4.7.2+; check the limits in [.NET Standard 2.0](Agents.md#net-standard-20) (sync `ApiClient` and `KestrelCqrsClient` query calls throw there).
- **Zerra 6 isn't on NuGet yet.** Replace the `Zerra.*` `PackageReference`s with `ProjectReference`s to a local Zerra checkout on `master`. Adjust the relative path:
  ```xml
  <ProjectReference Include="..\..\Zerra\Framework\Zerra\Zerra.csproj" />
  <ProjectReference Include="..\..\Zerra\Framework\Zerra.Repository\Zerra.Repository.csproj" />
  <ProjectReference Include="..\..\Zerra\Framework\Zerra.Repository.MsSql\Zerra.Repository.MsSql.csproj" />
  <ProjectReference Include="..\..\Zerra\Framework\Zerra.Web\Zerra.Web.csproj" />
  <ProjectReference Include="..\..\Zerra\Framework\Zerra.CQRS.RabbitMQ\Zerra.CQRS.RabbitMQ.csproj" />
  ```
  Also add the source generator to projects that declare or implement commands, events, query interfaces, handlers, or data models. The NuGet package will bring it in automatically once v6 is published:
  ```xml
  <ProjectReference Include="..\..\Zerra\Framework\Zerra.SourceGeneration\Zerra.SourceGeneration.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  ```
  Without AOT the generator is optional. When a type isn't generated, Zerra builds what it needs at runtime.
- **Don't add `<PublishAot>true</PublishAot>`** in this upgrade. It turns off runtime code generation even under `dotnet run`, so any type the generator missed fails right away.
- **These projects were removed**, so delete their references: `Zerra.Logger` (see [Logging](#7-logging)), `Zerra.CQRS.AzureEventHub` (no replacement; use Kafka, RabbitMQ, or Azure Service Bus), `Zerra.Repository.EventStoreDB` (use `Zerra.Repository.KurrentDB`), `Misc/Zerra.Identity`, and `Misc/Zerra.Tools` (see [Other removed APIs](#10-other-removed-apis)).
- **Packages you used to get through Zerra.** Zerra 5 brought in `Microsoft.Extensions.Configuration`, `.Binder`, `.CommandLine`, `.EnvironmentVariables`, `.Json`, and `.UserSecrets`. Zerra 6 doesn't. Add them directly to any project that still uses them.
- **SQL Server driver.** `Zerra.Repository.MsSql` now uses `Microsoft.Data.SqlClient` instead of `System.Data.SqlClient`. That driver encrypts by default, so a local server without a trusted certificate needs `TrustServerCertificate=True` in its connection string. Don't set `InvariantGlobalization` in a service that uses it, because it can't connect in that mode.
- **Delete `cqrssettings.json`** (and any `cqrssettings.*.json`), plus the `<None Update="cqrssettings.json">` item in the `.csproj`.
- **Azure package versions.** `Zerra.CQRS.AzureServiceBus` brings in `Azure.Core` 1.61 or later, which has two knock-on effects. `Azure.Identity` below 1.18 fails to compile with CS0433 (`ChainedTokenCredential`, `ManagedIdentityCredential` and others are in both assemblies), so update it (1.21.0 works). `Azure.Core` also depends on `Microsoft.Extensions.Logging.Abstractions` 10.0.9 or later, so a project that pins an older version gets `NU1605` (package downgrade); raise just that package.

## 3. Contracts (`*.Domain` Projects)

- **Query interfaces inherit `IQueryHandler`:**
  ```csharp
  //v5
  [ServiceExposed]
  public interface ILogNoteQueryProvider { ... }
  //v6
  public interface ILogNoteQueryProvider : IQueryHandler { ... }
  ```
- **Remove `[ServiceExposed]`** from interfaces, commands, and events. In v6, what a service exposes is decided by what its bus registers, and the web gateway forwards only interfaces its bus has a route for.
- **`[ServiceExposed(NetworkType.Internal)]` and `[ServiceBlocked]`**: move anything that must not reach browsers onto its own interface (for a single query method, move it to a new query interface), and don't register that interface on the gateway's bus. In `Demo/Store`, `IStockReservationHandler` and `ICartRepricingHandler` work this way.
- **`[ServiceSecure(roles)]`** was removed. Move the check into the handler method: check the claims on `Thread.CurrentPrincipal` and throw `System.Security.SecurityException`, which the gateway returns as HTTP 401. See [Security](Security.md).
- **Remove `using Zerra.Providers;`**. That namespace no longer exists.
- **`[ServiceExposed(NetworkType.Local)]`** meant the interface was only ever handled in its own process. Register its handler with `AddHandler` in that process and give it no server, client, consumer or producer.
- **`[ServiceLog]` is allowed only on classes and interfaces.** Move a method-level `[ServiceLog(BusLogging.None)]` to the interface; it then applies to every method.
- **Every implementation of a query interface must be an `IHandler`**, including classes built with `new` and called directly rather than through the bus (and test fakes), because `IQueryHandler` extends `IHandler`. Derive them from `BaseHandler` as well. Such a class never gets a bus context, so inside it use the static `Zerra.CQRS.Bus`, not the inherited `Bus`.
- **Commands without `[ServiceExposed]`** were only handled locally in v5. In v6 any command on a routed interface is sent to that route, so check for commands that must stay in-process.
- `ICommand`, `ICommand<TResult>`, `IEvent`, `ICommandHandler<T>`, `ICommandHandler<T, TResult>`, and `IEventHandler<T>` keep their v5 signatures. The handler interfaces now extend `IHandler`.
- Query methods can still return `IEnumerable<T>`. Changing return types to arrays is optional.
- Optionally put `CancellationToken cancellationToken` last on query methods. Callers don't pass it; the server supplies it.

## 4. Handlers

- **Every class passed to `AddHandler` must implement `IHandler`**, or registration throws `"<Type> does not implement IHandler"`. Derive from `BaseHandler`, or from `BaseHandlerWithRepo` (in `Zerra.Repository`) when the class uses the repository:
  ```csharp
  public class LogNoteQueryProvider : BaseHandlerWithRepo, ILogNoteQueryProvider
  public class EmailProvider : BaseHandler, IEmailCommandProvider
  ```
- **Inside a handler, `Bus`, `Log`, and `Repo` now bind to the inherited instance members** instead of the static classes, because C# looks up members of the class before types. So:
  - `Bus.Call<IX>().Method(...)`, `Bus.DispatchAsync(...)`, and `Bus.DispatchAwaitAsync(...)` compile unchanged and use this service's bus. `IBus.Call<T>()` has no `CancellationToken` or `TimeSpan` overload, so remove those arguments. A timeout comes from `Bus.New(defaultCallTimeout: ...)`.
  - `Log` is `ILogger?`. `Log.InfoAsync(...)` won't compile, so change it to `Log?.Info(...)` (same for `Trace`, `Debug`, `Warn`, `Error`, `Critical`).
  - `Repo` is `IRepo`. Rewrite the query and persist calls as described in [Repository](#5-repository).
  - **Static members of a handler class** (static methods, static lambdas and field initializers, and `: this(...)` constructor initializers) that use `Bus` or `Log` now fail with CS0120, because the name binds to the instance member. Pass the handler's `Bus`/`Log` in, or write `Zerra.CQRS.Bus` / `Zerra.Logging.Log` to keep the static ones. The compiler reports each location, so this can be scripted from the build output.
  - **Constructors can't use `Bus` or `Log`** either: the context is set by `AddHandler`, after construction, and using them earlier throws `Handler not initialized`.
- **Don't start background work from a handler's constructor or static constructor.** v5's discovery created handlers only after the bus was running. In v6 they are created with `new` in `Program.cs`, often before `Bus.New` and always before `AddHandler`. A static constructor that starts a loop runs as soon as the first instance is created, and the loop's first static `Bus` call throws `Bus not initialized`. Even after `Bus.New`, calls fail with `No handler registered` until the handlers and clients the loop uses are registered. Start such work from `Program.cs` once the bus is fully set up: after every `AddHandler`, server, consumer, client and producer.
- **Handlers are singletons.** One instance is created in `Program.cs` and serves every concurrent message. Remove instance fields that hold per-request state.
- **Dependencies.** v5 `Resolver.GetSingle<T>()` is gone. Either pass the dependency to the handler's constructor in `Program.cs`, or register it with `busServices.AddService<T>(instance)` and read it with `Context.GetService<T>()`. `Context` isn't available in the constructor. Use it inside handler methods, or override `InitializeBaseHandler()` (`InitializeBaseHandlerWithRepo()` for `BaseHandlerWithRepo`).
- **Static helpers that used the static `Repo` or `Bus`** get an `IRepo repo` or `IBus bus` parameter, and the handler passes its own `Repo` or `Bus`:
  ```csharp
  //v5
  public static async Task<BlockSetModel> LoadBlockSetForWorkout(Guid logWorkoutID) { var item = await Repo.QueryAsync(new QuerySingle<LogWorkoutBlocksDTO>(x => x.LogWorkoutID == logWorkoutID)); ... }
  //v6
  public static async Task<BlockSetModel> LoadBlockSetForWorkout(IRepo repo, Guid logWorkoutID) { var item = await repo.SingleAsync<LogWorkoutBlocksDTO>(x => x.LogWorkoutID == logWorkoutID); ... }
  ```
- **Tests.** v5 tests often swapped in mock handlers with `Discovery.DefineClassByInterface<IX>(typeof(MockX))`. The v6 bus doesn't use discovery. Create a bus for the tests with `Bus.New` and `AddHandler` the mocks. A handler a test builds directly (with `new` or `RuntimeHelpers.GetUninitializedObject`) has no context and throws `Handler not initialized` on its first `Bus` or `Log` use. `BusContext` can't be created outside Zerra, so add that handler to a bus of its own with `AddHandler` as well.
- **Command or event.** Read [Command or Event?](Agents.md#command-or-event-read-this-first). A v5 event handler that writes shared state, sends mail, or charges money must have its consumer registered with `EventConsumerMode.PerService` (see [section 6](#6-replace-cqrssettingsjson-with-programcs)).

## 5. Repository

### Setup

v5 found every provider in the loaded assemblies. v6 registers them one by one, and a service registers only the models it uses:

```csharp
var repo = Repo.New();
repo.AddProvider<AccountDTO>(new AccountEncryptionProvider());
repo.AddProvider<LogNoteDTO>(new LogNoteProvider());
//...one line per data model this service reads or writes

var busServices = new BusServices();
busServices.AddRepo(repo);                  //BaseHandlerWithRepo handlers get it as Repo
var bus = Bus.New("MyApp.Service.Domain", log, busLog, busServices);
```

Register providers before `Bus.New`, and create handlers after it: `AddHandler` hands each handler its context, and `BaseHandlerWithRepo` reads `IRepo` from that context right then. A service without data access can skip `BusServices`. Tools and tests that run without a bus use the `repo` instance directly.

### Query and Persist Calls

The arguments keep the same order as the v5 model constructors. Only the wrapper changes:

| Zerra 5 | Zerra 6 |
|---|---|
| `Repo.QueryAsync(new QueryMany<T>(args))` | `Repo.ManyAsync<T>(args)` |
| `Repo.QueryAsync(new QueryFirst<T>(args))` | `Repo.FirstAsync<T>(args)` |
| `Repo.QueryAsync(new QuerySingle<T>(args))` | `Repo.SingleAsync<T>(args)` |
| `Repo.QueryAsync(new QueryAny<T>(args))` | `Repo.AnyAsync<T>(args)` |
| `Repo.QueryAsync(new QueryCount<T>(args))` | `Repo.CountAsync<T>(args)` |
| `Repo.QueryAsync(new EventQueryMany<T>(args))` | `Repo.EventManyAsync<T>(args)` (also `EventFirstAsync`, `EventSingleAsync`, `EventAnyAsync`, `EventCountAsync`) |
| `Repo.QueryAsync(new TemporalQueryMany<T>(args))` | `Repo.TemporalManyAsync<T>(args)` (also `TemporalFirstAsync`, `TemporalAnyAsync`, `TemporalCountAsync`) |
| `EventQueryNewest`/`Oldest`, `TemporalQueryNewest`/`Oldest` | `EventFirstAsync`/`TemporalFirstAsync` with `TemporalOrder.Newest` or `TemporalOrder.Oldest` |
| `Repo.PersistAsync(new Create<T>(args))` | `Repo.CreateAsync<T>(args)` |
| `Repo.PersistAsync(new Update<T>(args))` | `Repo.UpdateAsync<T>(args)` |
| `Repo.PersistAsync(new Delete<T>(args))` | `Repo.DeleteAsync<T>(args)` |
| `Repo.PersistAsync(new DeleteByID<T>(args))` | `Repo.DeleteByIDAsync<T>(args)` |
| `Repo.Query(...)` / `Repo.Persist(...)` (sync) | `Repo.Many`, `Repo.Single`, `Repo.Create`, ... |

Watch for these:

- **Always write the type argument** when passing a collection: `Repo.CreateAsync<LogNoteDTO>(items)`. Without it, a `List<T>` binds to the single-model overload with `TModel = List<T>`.
- **Many queries return `IReadOnlyCollection<T>`** (v5 returned `ICollection<T>`). Fix declared variable types, `Mapper.Map<ICollection<X>, Y[]>(...)` calls, and any `.Add` on the result.
- **Relation properties** declared as `ICollection<T>` on data models: change them to `List<T>` or `T[]`. Arrays, `List<T>`, and `IReadOnlyList<T>` are the types documented for v6.
- LINQ support in `Where` is listed in [Agents.md](Agents.md#zerrarepository).

### Data Contexts and Providers

- **Connection string.** `public override string ConnectionString => ...` becomes `public override string GetConnectionString() => ...`.
- **`DataStoreGenerationType` override** was removed from the data context. Delete it. If the app relied on it to create or update the schema, call `CodeFirstGeneration.Generate<TContext>(...)` at startup instead (see [Repository Generation](RepositoryGeneration.md)).
- `TransactStoreProvider<TContext, TModel>` (the plain provider base class) keeps its name and its `QueryLinking`/`PersistLinking`/`EventLinking` overrides.
- **Layer providers (encryption, compression, rules)** used to find the provider below them through discovery. Now the constructor passes it in, and the outermost layer is what you register:
  ```csharp
  public class LogSessionCompressionProvider : BaseTransactStoreCompressionProvider<ITransactStoreProvider<LogSessionDTO>, LogSessionDTO>
  {
      public LogSessionCompressionProvider() : base(new LogSessionProvider()) { }
      ...
  }
  //Program.cs
  repo.AddProvider<LogSessionDTO>(new LogSessionCompressionProvider());
  ```
- **Encryption providers must override `EncryptionAlgorithm`.** v5 always used `AESwithShift`. Keep that for existing data or it can't be decrypted. `AESwithShift` is marked `[Obsolete]`, so suppress the warning:
  ```csharp
  public AccountEncryptionProvider() : base(new AccountProvider()) { }

  #pragma warning disable CS0612 // Type or member is obsolete
  public override SymmetricAlgorithmType EncryptionAlgorithm => SymmetricAlgorithmType.AESwithShift;
  #pragma warning restore CS0612 // Type or member is obsolete
  ```
- **Cache providers** (`ICacheProvider`) and `IDualBaseProvider` were removed. Drop the cache layer, or rewrite it as a `BaseTransactStoreLayerProvider`.

## 6. Replace `cqrssettings.json` with Program.cs

### How v5 Read the File

`Bus.StartServices` compared each entry's `Service` name with the running service's name (`Config.EntryAssemblyName`, the entry assembly's name):

- **`Queries` entry for this service**: one query server at `BindingUrl` hosts every interface in `Types`, and the handler implementation was found by discovery.
- **`Queries` entry for another service**: one query client to `ExternalUrl` for every interface in `Types`.
- **`Messages` entry for this service**: a consumer at `MessageHost` for every command and event interface in `Types`. Event interfaces show up in the `Messages` of every service that subscribes to them.
- **`Messages` entry for another service**: a producer to its `MessageHost` for those commands and events.
- **`EncryptionKey`**: encrypted traffic with `AESwithShift`.
- **Overrides**: `BindingUrl` came from settings or environment variables (`BindingUrl`, `urls`, `ASPNETCORE_URLS`, `DOTNET_URLS`), and each `ExternalUrl` could be overridden by a setting named after the service.
- **Service creator**: the `IServiceCreator` passed in chose the transport, such as `TcpServiceCreator` or `KafkaServiceCreator`.

v6 reads none of this. Write each service's registrations in code. The mapping for one service:

| cqrssettings.json | Zerra 6 in that service's Program.cs |
|---|---|
| Interface in this service's `Queries.Types` | `bus.AddHandler<IX>(new XImpl());` and `bus.AddQueryServer<IX>(server);` |
| Interface in another service's `Queries.Types` that this service calls | `bus.AddQueryClient<IX>(clientForThatService);` |
| Command interface in this service's `Messages.Types` | `bus.AddHandler<IX>(new XImpl());` and `bus.AddCommandConsumer<IX>(serverOrBrokerConsumer);` |
| Command interface in another service's `Messages.Types` that this service sends | `bus.AddCommandProducer<IX>(clientOrBrokerProducer);` |
| Event interface in this service's `Messages.Types` | `bus.AddHandler<IX>(new XImpl());` and `bus.AddEventConsumer<IX>(serverOrBrokerConsumer, EventConsumerMode.PerService /* or PerReplica */);` |
| Event interface this service publishes | Direct TCP/HTTP: `bus.AddEventProducer<IX>(client)` once per subscribing service. Broker: one `AddEventProducer<IX>(brokerProducer)` |
| `BindingUrl` / `ExternalUrl` / `MessageHost` | The URL passed to the server or client constructor. Read it from your own settings (see [Configuration](#9-configuration)) |
| `EncryptionKey` | `new ZerraEncryptor(key, SymmetricAlgorithmType.AESwithPrefix)`, the same key on both ends. An encryptor is optional, like the key was |

**Only register what this service actually uses.** A service no longer needs to know about interfaces it neither hosts nor calls.

`AddEventConsumer` has no default mode, so choose one for each registration. Use `PerService` when the handler writes shared state or must run once. Use `PerReplica` only when every running copy should act on the event, for example to clear its own in-memory cache. See [Events](Events.md#choosing-per-replica-or-per-service).

**Watch out:** registering an event interface with `AddCommandConsumer`, or a command interface with `AddEventConsumer`, doesn't throw. The bus only logs `Cannot add Command Consumer: no command types found in interface ...` or the event equivalent, and only when it was given a logger.

**A local handler now wins over producers.** In v5, when a service had both an event producer and a discovered implementation of the same event interface, the producers were used and the local implementation was ignored. In v6, if the bus has a handler for the interface, the event is handled locally and **not** sent to any producer. A service that publishes an event must not also `AddHandler` for that event interface, or its subscribers stop receiving it. The same applies to commands.

**Handlers are the only way a message is handled locally.** v5 found an implementation for any interface it could load, so interfaces missing from `cqrssettings.json` (process-internal handlers) still worked. In v6 every interface a service calls needs an `AddHandler` or a client/producer, otherwise the call throws `No handler or client registered for ...`. Search each service for `Bus.Call<`, `DispatchAsync` and `DispatchAwaitAsync`, including code in the libraries it references.

### Service Creators

Transport types from v5 and what replaces them:

| Zerra 5 | Zerra 6 |
|---|---|
| `TcpServiceCreator` (TCP, bytes) | `new TcpCqrsServer(url, serializer, encryptor, log)` / `new TcpCqrsClient(url, serializer, encryptor, log)` with `new ZerraByteSerializer()` |
| `HttpServiceCreator(contentType, authorizer, allowOrigins)` | `new HttpCqrsServer(url, serializer, encryptor, authorizer, allowOrigins, log)` / `new HttpCqrsClient(url, serializer, encryptor, authorizer, log)` |
| `KestrelServiceCreator(app, route, contentType, authorizer)` | `KestrelCqrsServerQueryServer` / `KestrelCqrsServerCommandConsumer` / `KestrelCqrsServerEventConsumer` sharing one `KestrelCqrsServerLinkedSettings`, plus `app.UseKestrelCqrsServer(serializer, encryptor, log, settings)`. Callers use `KestrelCqrsClient`. See [Agents.md](Agents.md#hosting-a-service-in-aspnet-core-instead-of-tcp) |
| `KafkaServiceCreator(queryCreator, env, user, pwd)` | Queries: the TCP/HTTP types above. Messages: `new KafkaProducer(host, serializer, encryptor, log, env, user, pwd)` / `new KafkaConsumer(...)` |
| `RabbitMQServiceCreator(queryCreator, env)` | `new RabbitMQProducer(host, serializer, encryptor, log, env)` / `new RabbitMQConsumer(...)` |
| `AzureServiceBusServiceCreator(queryCreator, env)` | `new AzureServiceBusProducer(connectionString, serializer, encryptor, log, env)` / `new AzureServiceBusConsumer(...)` |
| `AzureEventHubServiceCreator` | Removed |
| `TcpRawCqrsServer(contentType, url, symmetricConfig)` | `TcpCqrsServer(url, serializer, encryptor, log)` |

A single server object can be the query server, command consumer, and event consumer for one URL, which is how v5's creators shared servers. Create one per URL and reuse it. Use the same kind of serializer on both ends; v5's TCP creator used bytes.

### Program.cs Order

```csharp
Config.LoadConfiguration(args);                        //only if you kept Config, see section 9

ILogger log = new ConsoleLogger();                     //your Zerra.Logging.ILogger
Log.SetLog(log);                                       //framework messages and any remaining static Log calls

var repo = Repo.New();                                 //skip if this service has no data access
repo.AddProvider<LogNoteDTO>(new LogNoteProvider());
var busServices = new BusServices();
busServices.AddRepo(repo);

var bus = Bus.New("MyApp.Service.Domain", log, new BusLogger(), busServices);

//1. handlers
bus.AddHandler<ILogNoteQueryProvider>(new LogNoteQueryProvider());
bus.AddHandler<ILogNoteCommandProvider>(new LogNoteCommandProvider());

//2. what this service hosts
var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AESwithPrefix);
var server = new TcpCqrsServer(domainServiceUrl, serializer, encryptor, log);
bus.AddQueryServer<ILogNoteQueryProvider>(server);
bus.AddCommandConsumer<ILogNoteCommandProvider>(server);

//3. what this service calls
var emailClient = new TcpCqrsClient(emailServiceUrl, serializer, encryptor, log);
bus.AddCommandProducer<IEmailCommandProvider>(emailClient);

bus.WaitForExit();                                     //or await bus.WaitForExitAsync(token)
```

Also replace:

- Static `Bus.WaitForExit()` / `Bus.StopServices()` → the same methods on `bus`.
- `Bus.AddLogger(x)` → the `busLog` argument of `Bus.New`.
- `Bus.MaxConcurrentQueries`, `MaxConcurrentCommandsPerTopic`, `MaxConcurrentEventsPerTopic`, `DefaultCallTimeout`, `DefaultDispatchTimeout`, `DefaultDispatchAwaitTimeout`, `ReceiveCommandsBeforeExit` → the matching `Bus.New` arguments (`commandToReceiveUntilExit` for the last one).
- The service name → pass it to `Bus.New` directly. v5 used the entry assembly name, and many solutions keep that string.
- `Bus.ReceiveCommandsBeforeExit = n` set during startup → `Bus.New(..., commandToReceiveUntilExit: n)`. If that value was decided in a startup callback, decide it before `Bus.New`.
- **Event consumer mode that matches v5.** v5's broker consumers gave every consumer instance its own event subscription: a new Kafka group id, an exclusive RabbitMQ queue, or an `EVT-{guid}` Azure Service Bus subscription. So `EventConsumerMode.PerReplica` reproduces v5 exactly. Switch a subscriber to `PerService` only after checking its handlers.

### Many Services with One Shared Setup Method

v5 solutions often had one shared `cqrssettings.json`. To keep one place that describes the whole topology, write a shared method that builds the bus for any service and branches on the service name. Each service's `Program.cs` then adds only its own handlers:

```csharp
public static IBusSetup StartServices(string serviceName, IRepo? repo)
{
    var logger = new Logger();
    Log.SetLog(logger);
    var busServices = new BusServices();
    if (repo is not null)
        busServices.AddRepo(repo);
    var bus = Bus.New(serviceName, logger, new BusLogger(), busServices);

    var serializer = new ZerraByteSerializer();
    var encryptor = new ZerraEncryptor(sharedKey, SymmetricAlgorithmType.AESwithPrefix);

    if (serviceName == "MyApp.Service.Email")
    {
        var server = new TcpCqrsServer(emailUrl, serializer, encryptor, logger);
        bus.AddCommandConsumer<IEmailCommandProvider>(server);
    }
    else
    {
        var client = new TcpCqrsClient(emailUrl, serializer, encryptor, logger);
        bus.AddCommandProducer<IEmailCommandProvider>(client);
    }
    //...one block like this for each service

    return bus;
}

//MyApp.Service.Email Program.cs
var bus = ServiceManager.StartServices("MyApp.Service.Email", null);
bus.AddHandler<IEmailCommandProvider>(new EmailProvider());
bus.WaitForExit();
```

A handler must be added before a message for it arrives. The server starts listening when it's registered, so a request that comes in before `AddHandler` runs fails with `No handler registered for ...`. For services that start taking traffic right away, register handlers before servers, as in the Program.cs above.

Startup order for a service:

1. Create the `BusServices` providers (see [Repository](#5-repository)), then `Bus.New`.
2. `AddHandler` for every handler the service hosts.
3. Add the servers and consumers, then the clients and producers.
4. Start background work (timers, polling loops, queue processors) that calls the bus. If handlers are created before `Bus.New`, for example collected in a registration list first, their constructors and static constructors must not call the bus or start such work (see [Handlers](#4-handlers)).

## 7. Logging

- **`ILoggingProvider` → `Zerra.Logging.ILogger`.** The methods are now synchronous: `Trace`, `Debug`, `Info`, `Warn`, `Error(string?, Exception?)`, `Error(Exception?)`, `Critical(string?, Exception?)`, `Critical(Exception?)`. Nothing discovers it. Pass it to `Bus.New`, to servers and clients, and to `Log.SetLog`.
- **`Zerra.Logger` was removed** (`LoggingProvider`, the file logger that used `LogFileDirectory`, and `BusLoggingProvider`). Write your own. `Demo/Store/Store.Common/Logging/ConsoleLogger.cs` and `ConsoleBusLogger.cs` are short examples, and any logging library can sit behind the interface.
- **`IBusLogger`**: all six methods gained a `string service` parameter before `source`, for example `EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)`. Register it as `Bus.New`'s `busLog` argument instead of calling `Bus.AddLogger`.
- **Always pass a logger to `Bus.New`.** Registration mistakes (wrong interface kind, a duplicate client, and so on) are only reported through that logger.
- **Static `Log`** still exists, but it logs nothing until `Log.SetLog(log)` is called. Its `...Async` methods are marked `[Obsolete]`, so they fail to build in projects with `TreatWarningsAsErrors`. Rewrite `await Log.XAsync(...)` and `_ = Log.XAsync(...)` to `Log.X(...)`, a regex job; inside handlers use `Log?.X(...)` (see [Handlers](#4-handlers)). See [Static Bus and Log](#11-static-bus-and-log).

## 8. ASP.NET Projects

- **Gateway.** `UseCqrsApiGateway` now resolves `IBus`, `ISerializer`, and optionally `Zerra.Logging.ILogger` and `ICqrsAuthorizer` from dependency injection. The overloads that took an authorizer were removed:
  ```csharp
  var bus = ServiceManager.StartServices("MyApp.Web", null);   //a bus with query clients and command producers, no handlers
  builder.Services.AddSingleton<IBus>(bus);
  builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());   //browsers send JSON
  builder.Services.AddSingleton<Zerra.Logging.ILogger>(log);
  builder.Services.AddSingleton<ICqrsAuthorizer>(authorizer);              //if v5 passed one to UseCqrsApiGateway
  ...
  app.Lifetime.ApplicationStopping.Register(bus.StopServices);
  app.UseCqrsApiGateway("/CQRS", allowOrigins);
  ```
  With the older `IHostBuilder`/`Startup` pattern, register the same services in `.ConfigureServices((context, services) => ...)`.
- **`loggerFactory.AddZerraLogger()`** now needs the logger, `AddZerraLogger(log)`. Delete the call if you don't want ASP.NET logs routed to Zerra.
- **`ICqrsAuthorizer` implementations** must add a synchronous method. Implement it for real; don't block on the async one:
  ```csharp
  public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken) { ...build the headers... }
  public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken)
      => ValueTask.FromResult(GetAuthorizationHeaders(cancellationToken));
  ```
- **`ApiClient`**: `new ApiClient(endpoint, ContentType.Json, authorizer)` → `new ApiClient(endpoint, new ZerraJsonSerializer(), log, authorizer)`. `ApiCqrsCookieAuthorizer` takes a serializer as its first argument.
- **Controllers and middleware**: take `IBus` through the constructor instead of calling the static `Bus`.
- **In ASP.NET projects `ILogger` is ambiguous** with Microsoft's, so write `Zerra.Logging.ILogger`.
- Cookie or token protectors that used `SymmetricAlgorithmType.AESwithShift` can switch to `AESwithPrefix`. Values issued before the switch stop decrypting, so users sign in again. Keep `AESwithShift` if that isn't acceptable.

## 9. Configuration

`Zerra.Config` was removed (`LoadConfiguration`, `GetSetting`, `Bind<T>`, `EnvironmentName`, `EntryAssemblyName`, `IsDebugBuild`, and the rest). Choose one option:

- **Keep it.** Copy `Framework/Zerra/Config.cs` from the `release/5.4.0` branch into a shared project of the solution, following [Copying removed code](#copying-removed-code). Delete the discovery members (`DiscoveryEnabled`, `AssemblyLoaderEnabled`, `AddDiscoveryAssemblies`, `AddDiscoveryAssemblyNameStartsWiths`, `SetDiscoveryAssemblies`, `SetDiscoveryAssemblyNameStartsWiths`) and the discovery calls inside `LoadConfiguration`. Then add the `Microsoft.Extensions.Configuration` packages from [section 2](#2-projects-and-packages). `LoadConfiguration` in v5 also started discovery, which is why code that still scans (see [Other Removed APIs](#10-other-removed-apis)) now has to call `Discovery.Initialize` itself.
- **Replace it.** Read settings with `Microsoft.Extensions.Configuration` or environment variables, as `Demo/Store/Store.Common/StoreSettings.cs` does.

The URLs and keys that used to live in `cqrssettings.json` go wherever you keep other settings (`appsettings.json`, environment variables, secrets), and `Program.cs` reads them.

### Copying removed code

A solution that relies heavily on a removed helper can keep it. Examples are `Config`, `Instantiator.GetSingle`, `LinqExtensions.ForEach` and the `Misc/Zerra.Tools` types. Copy the source from `release/5.4.0` into a shared project of the solution, then:

1. **Use the solution's own namespace**, never a `Zerra` one. A type in a `Zerra` namespace that Zerra no longer ships misleads readers into looking for it in Zerra, and it would clash if Zerra later adds a type with the same name. Rewrite the file's comments so they describe the code, not the Zerra version it came from.
2. **Keep only what the solution uses.** Drop members that depend on v5 machinery, such as the discovery members of `Config`.
3. **Point the call sites at the new namespace.** Add a `using` to each calling file, or `@using` in Razor files. Rewrite fully qualified references too: `Zerra.Config.GetSetting(...)` becomes `Config.GetSetting(...)`, and `using Config = Zerra.Config;` needs the new namespace. A file needs no `using` for its own namespace or a parent of it.
4. **Import extension methods with `using static`**, for example `using static MyApp.Common.EnumerableExtensions;` for `ForEach`. Importing a whole extensions namespace can clash with same-named types the calling project already has.
5. **Remove the v5 usings that are now unused.** For example, delete `using Zerra.Linq;` from files that used it only for `ForEach`.

## 10. Other Removed APIs

| Zerra 5 | Zerra 6 |
|---|---|
| `Resolver.GetSingle<T>()`, `Resolver.GetNew<T>()`, `Instantiator` | Construct objects directly, or use `BusServices.AddService<T>` + `Context.GetService<T>()` in handlers. A static helper class is fine for stateless code (see [Copying removed code](#copying-removed-code)) |
| `Discovery.GetTypeFromName(name)` | `Zerra.Reflection.TypeFinder.GetTypeFromName(name)`, or `Type.GetType(name)` |
| Other `Discovery.*` | `Zerra.Reflection.Dynamic.Discovery` (runtime only, not AOT). Call `Discovery.Initialize(true)` once before the first lookup; v5's `Config.LoadConfiguration` did this implicitly. `true` loads the `.dll` files in the base directory, as v5 did. `DefineClassByInterface` no longer affects the bus (see [Handlers](#4-handlers)). Prefer registering types explicitly |
| `TypeAnalyzer`, `TypeDetail`, `MemberDetail` | Still in `Zerra.Reflection`, but several members were renamed or removed, see [Reflection](#reflection) |
| `LinqExtensions.ForEach` (`Zerra.Linq`) | A `foreach` loop, or a copy (see [Copying removed code](#copying-removed-code)) |
| `LinqChunkExtensions.Chunk` | .NET `Enumerable.Chunk` (returns arrays) |
| `AppendAnd`, `AppendOr`, `AppendExpressionOnMember`, `ToLinqString`, `ReadMemberName`, `WhereBuilder`, `LinqStringConverter`, `LinqExtensions.Contains(IEnumerable, IEnumerable)` | Removed. Rewrite with plain LINQ/expressions |
| `Zerra.Threading` `WaitAsync` extension, `MethodWait` | .NET `Task.WaitAsync(TimeSpan / CancellationToken)` |
| `Misc/Zerra.Tools`: `Locker<T>`, `TaskThrottler`, `ZerraThreadPool`, `Concurrent`, `MathParser`, `Secret` | Removed. If used, copy the source from `release/5.4.0` `Misc/Zerra.Tools` into the solution, following [Copying removed code](#copying-removed-code) |
| `Misc/Zerra.Identity` | Removed |
| `QueryStringSerializer` | Removed |
| `MapperWithLog`, `IMapLogger` | Removed. `Mapper.Map`/`MapTo`/`Copy` remain |
| `SymmetricConfig` for network encryption | `IEncryptor` (`ZerraEncryptor`) |
| `SymmetricAlgorithmType.AESwithShift` (and DES/TripleDES/RC2 `withShift`) | `[Obsolete]`, use `AESwithPrefix` for new data. The enum's numbers changed (`AESwithShift` was 4 and is now 8), so remap any stored enum numbers |
| `NetworkType`, `IServiceCreator`, `ServiceSettings`, `ServiceQuerySetting`, `ServiceMessageSetting` | Removed with `cqrssettings.json` |
| `StringExtensions` (`ToInt32`, `ToGuid`, `Truncate`, ...) | Unchanged, still in the global namespace. Only the file moved |
| `EnumName.Parse`, `EnumName.TryParse`, `ToEnum`, `ToEnumNullable` | Unchanged API, but names are now matched case-sensitively; v5 ignored case. `TryParse` returns false and `Parse` throws for a name that differs only in case. Values the application wrote itself with `EnumName()` still match. Check calls that parse text from outside the application (provider data, user input, files), and match case-insensitively there yourself where needed |
| `StreamExtensions` (`stream.ToArray()`, `stream.ToArrayAsync()`, ...) | Moved from `namespace System.IO` to `Zerra.IO`. Add `using Zerra.IO;`. Without it, `ToArrayAsync()` on a `Stream` binds to `System.Linq.AsyncEnumerable` and fails with CS0411 |
| `SymmetricConfig`, `SymmetricEncryptor` with `AESwithShift` for stored data | Still available for data at rest. Keep `AESwithShift` for existing data (suppress CS0612); use `AESwithPrefix` for data that only lives in memory |

### Reflection

Mechanical renames, safe to apply with a regex:

| Zerra 5 | Zerra 6 |
|---|---|
| `TypeDetail.MemberDetails` / `SerializableMemberDetails` | `Members` / `SerializableMembers` |
| `TypeDetail.MethodDetailsBoxed` | `Methods` |
| `TypeDetail.GetMethodBoxed` / `TryGetMethodBoxed` / `GetMemberBoxed` / `TryGetMemberBoxed` | `GetMethod` / `TryGetMethod` / `GetMember` / `TryGetMember` |
| `MemberDetail.TypeDetailBoxed`, `MethodDetail.ReturnTypeDetailBoxed` | `TypeDetail`, `ReturnTypeDetail` |
| `MemberDetail.HasGetterBoxed` / `HasSetterBoxed` | `HasGetter` / `HasSetter` |
| `MemberDetail<T>.GetterTyped` / `SetterTyped` | `Getter` / `Setter` |
| `MethodDetail.ParameterDetails` | `Parameters` |
| `TypeLookup.CoreTypeLookup(type, out coreType)` | `TypeLookup.GetCoreType(type, out coreType)` |
| `TypeDetail.IsTask` | `TypeDetail.SpecialType == SpecialType.Task` (`TaskResultGetter` was removed; read `Result` through reflection) |
| `Type.GetNiceName()` | Removed. Use `Type.Name`, or write a formatter if the generic arguments matter |

Other changes:

- **`type.GetTypeDetail()`** is an extension in `Zerra.Reflection`. Files that reached it through another namespace in v5 need `using Zerra.Reflection;`.
- **Nullable annotations.** `GetterBoxed`, `SetterBoxed`, `CallerBoxed` and `CreatorBoxed` are nullable delegates, and `InnerType`, `InnerTypeDetail`, `IEnumerableGenericInnerType`, `IEnumerableGenericInnerTypeDetail`, `DictionaryInnerType` and `DictionaryInnerTypeDetail` are nullable. Code that relied on v5 treating them as non-null gets CS8602/CS8604, which are errors under `TreatWarningsAsErrors`. Add `!` where the type guarantees them.
- **`TypeDetail.Methods` holds only public methods.** v5 included non-public ones. Code that looked up a private generic helper by name (`typeof(X).GetTypeDetail().GetMethod(nameof(Helper))`) throws `method not found`; use `typeof(X).GetMethod(nameof(Helper), BindingFlags.NonPublic | BindingFlags.Static)`.
- **`GetGenericMethodDetail` and `GetGenericTypeDetail` were removed** (on `MethodDetail`, on `TypeDetail`, and `TypeAnalyzer.GetGenericMethodDetail`). Use `methodDetail.MethodInfo.MakeGenericMethod(types)` and invoke it, and `typeDetail.Type.MakeGenericType(types).GetTypeDetail()`. `MethodInfo.Invoke` rejects a null target for an instance method, which v5's compiled callers allowed; make such helpers `static`.
- **`Instantiator`** is gone. `Instantiator.Create(type)` → `Activator.CreateInstance(type, true)`. `Instantiator.GetSingle(() => new X(...))` shared one instance per type; keep that with a static `Lazy<T>` or a small cache.

### Serialization and Mapping

- **Custom JSON converters** derive from `JsonConverter<TValue>` (the `TParent` type parameter is gone). `TryReadValue` takes a `JsonToken` instead of a `JsonValueType`, and `Drain(ref reader, ref state, token)` takes the token. Register one converter per closed type with `JsonSerializer.AddConverter(typeof(TValue), () => new MyConverter())`; open generic registrations (`AddConverter(typeof(MyConverter<,>), typeof(Secret<>))`) are gone. Child converters come from `JsonConverterFactory.Get(typeDetail, memberKey, getter, setter)`, where the getter is `Func<object, TMember?>` and the setter is `Action<object, TMember?>`.
- **`Mapper.Copy` and `Map`** create a target only through a public parameterless constructor or one whose parameters match public members. Otherwise they throw `Cannot create instance of X`, including for a member of the object being copied; v5 left such members uncopied instead. Register a converter for the type with `Mapper.AddConverter(typeof(X), typeof(X), () => new XConverter())`, deriving from `MapConverter<X, X>`. To keep v5's behaviour, return `null` from `Map`. Returning the source instance copies it by sharing, which only suits immutable types and changes behaviour for code that expects such members to come back empty.
- `Mapper.Copy(object source, Type sourceType)` replaces generic `Copy<T>` calls made through reflection.

### Remote Exceptions

v5 rebuilt an exception thrown in another service as its original type when the caller referenced that type, wrapped in `RemoteServiceException` with the original as `InnerException`. v6 does not rebuild it. The caller gets a `RemoteServiceException` whose `ErrorType` holds the original exception's type **name** (kept across several hops), with the original message and no `InnerException`.

- Code that catches the original type, or tests `ex.GetBaseException() is InvalidOperationException`, around a call to another service now falls through. Match `remote.ErrorType == nameof(InvalidOperationException)` as well, and list any derived types by name (`ArgumentNullException` for `ArgumentException`).
- The constructors changed: `RemoteServiceException(string? source, string? message)` and `RemoteServiceException(string errorType, string? message, string? source, string? stackTrace)`. v5's `(message)` and `(message, innerException)` are gone. A two-string call still compiles, now with the message as `source`, so check each one.

## 11. Static Bus and Log

v6 keeps a static `Bus` (`Call`, `DispatchAsync`, `DispatchAwaitAsync`) and a static `Log` so upgrades compile, but new code shouldn't use them:

- The static `Bus` points at the bus from the most recent `Bus.New`, and it throws `Bus not initialized. Call Bus.New to initialize.` before that.
- The static `Log` drops messages until `Log.SetLog` is called.

Replace them in this order and stop where the change would spread through too much code:

1. **Handlers**: nothing to do. Inside `BaseHandler` classes, `Bus` and `Log` already bind to the instance members (see [Handlers](#4-handlers)).
2. **ASP.NET controllers, filters, middleware**: inject `IBus` and `Zerra.Logging.ILogger`.
3. **Helpers called from handlers**: add an `IBus` / `ILogger` / `IRepo` parameter and pass the handler's own.
4. **Everything else**, such as background loops started from `Program.cs`, static initializers, or authorization helpers used everywhere: pass the `bus`/`log` from `Program.cs` through constructors where you can. Where you can't, leave the static call and list it in the upgrade notes. Don't add new sync-over-async (`.GetAwaiter().GetResult()`) while doing this.

## 12. Verify

1. Build the whole solution with no errors. Fix obsolete warnings for `AESwithShift` only where the old algorithm is still needed.
2. Run each service and the web app, and pass a logger to every `Bus.New`. Look for:
   - `Cannot add ...` messages: wrong registration kind, duplicates, or a type registered as both client and server.
   - `No handler registered for ...` / `No handler or client registered for ...`: missing `AddHandler`, `AddQueryClient`, or `AddCommandProducer` for something that is called.
   - `No dependency registered for type Zerra.Repository.IRepo`: a `BaseHandlerWithRepo` handler was added to a bus whose `BusServices` has no repo.
   - `does not implement IHandler`: a handler class still missing `BaseHandler`.
   - `Bus not initialized. Call Bus.New to initialize.`: a static `Bus` call ran before `Bus.New`, often in a static field initializer or in background work started from a handler's static constructor.
3. Exercise one query, one command, and one event across each pair of services, and check the event consumer mode chosen for each subscriber.
4. Read existing encrypted data through the repository to confirm the encryption providers still use `AESwithShift`.
5. Make a call that fails in another service and check the caller still handles it, since the exception now arrives as a `RemoteServiceException` (see [Remote Exceptions](#remote-exceptions)).
6. Run the unit tests. Failures such as `Handler not initialized`, `No handler or client registered for ...`, `Cannot create instance of ...` and `method not found` in a type initializer point back to [Handlers](#4-handlers), [Serialization and Mapping](#serialization-and-mapping) and [Reflection](#reflection).
