# Changelog

## 6.0.0 (in development)

Zerra 6 replaces assembly scanning and configuration files with explicit setup in code. [Upgrading from Zerra 5](docs/UpgradeV5ToV6.md) walks through moving a solution step by step. Zerra 5 and 6 services use different wire formats, so services that share messages upgrade together.

### Setup and Hosting

- Each service builds its bus in `Program.cs` with `Bus.New` and registers handlers, servers, clients, producers, and consumers explicitly. `cqrssettings.json`, discovery, and service creators are gone.
- Handlers derive from `BaseHandler` or `BaseHandlerWithRepo` and use the `Bus`, `Log`, `Repo`, and `Context` they inherit. Services are injected through `BusServices`.
- The static `Bus` and `Log` are obsolete. Use the `IBus` from `Bus.New` and pass your `ILogger` where it's needed, including `CodeFirstGeneration.Generate`, which logs a failed database read to it.
- What a service exposes is what its bus registers; `[ServiceExposed]`, `[ServiceBlocked]`, and `[ServiceSecure]` are removed.
- Graceful shutdown: stopping the bus finishes every message already received, and `WaitForExitAsync` handles SIGTERM and SIGINT for containers. `shutdownTimeout` limits the wait without cancelling handlers.
- `commandToReceiveUntilExit` for batch jobs and KEDA-style scaling.
- Services can be hosted in ASP.NET Core/Kestrel with `KestrelCqrsServer*` and called with `KestrelCqrsClient`.

### Messaging

- `EventConsumerMode.PerReplica` and `PerService` on every event subscription.
- `resilientCommands` on the Kafka, RabbitMQ, and Azure Service Bus consumers acknowledges a command after its handler finishes, so a command being handled when the process dies goes to another replica. See [Resilient Commands](docs/Reliability.md#resilient-commands).
- Optional message compression with `ICompressor`, applied before encryption.
- `IEncryptor` replaces `SymmetricConfig`, with `ZerraEncryptor` built in.
- Streams as query arguments (uploads) as well as results.
- Connection tests for each broker (`KafkaConnectionTest`, `RabbitMQConnectionTest`, `AzureServiceBusConnectionTest`) to fall back to direct TCP or HTTP.
- RabbitMQ accepts AMQP URIs, including TLS with `amqps://`. Kafka connects with TLS through `useTls`.
- `Zerra.CQRS.RabbitMQ` uses RabbitMQ.Client 7, and `RabbitMQConnectionTest.Test` is now `TestAsync`. Creating a RabbitMQ consumer or producer no longer throws when the broker is down: the consumer logs it and keeps retrying, and sends fail until the broker is back.
- `Zerra.CQRS.AzureEventHub` is removed; use Kafka, RabbitMQ, or Azure Service Bus.
- Kafka and Azure Service Bus recover when a producer's acknowledgement topic or queue is deleted: the producer creates it again and the consumer retries the reply, so awaited commands no longer hang.
- `WriteStreamContent` made with an async delegate throws `NotSupportedException` when sent synchronously, instead of blocking a thread on the delegate. Use the synchronous delegate constructor for synchronous sends.

### Serialization

- `ZerraJsonSerializer` is faster than System.Text.Json on every benchmarked model, and `ZerraByteSerializer` is faster than before, both with less memory ([Benchmarks](docs/Benchmarks.md)).
- `ZerraJsonSerializer` writes dictionaries with enum keys as objects, like System.Text.Json (`{"Friday":1}`). The old array form is still read.
- `ZerraJsonSerializer` reads enums from numeric strings too.
- `ZerraJsonSerializer` deserializes `IEnumerable<T>`, `IReadOnlyCollection<T>`, `IEnumerable`, and `ICollection` members as a `List` instead of an array.
- `ZerraJsonSerializer` reads a JSON object into a value typed as `object` as a `JsonObject` instead of an empty `object`, numbers into `object` as `double` instead of `decimal`, writes and reads `JsonObject` members, and supports non-generic dictionaries such as `Hashtable`, which threw before.
- `ZerraByteSerializer` throws a `NotSupportedException` when a value typed as `object`, including the items of a non-generic collection, is serialized without `UseTypes`. Before, it came back as an empty `object`.
- `ZerraJsonSerializer` rejects enum numbers out of the enum's range.
- `ZerraByteSerializer` throws on bytes after the value, like `ZerraJsonSerializer`. Before, they were ignored or rejected depending on where the stream's read buffer ended.
- Fixed `ZerraByteSerializer` failing to skip members missing from the target type when they hold custom collections, hash sets, or records.
- Fixed `ZerraByteSerializer` without `UseTypes` failing to read a value declared as an interface when its class has members the interface doesn't. The value is written with the interface's members.
- Fixed `ZerraJsonSerializer` throwing `ArgumentOutOfRangeException` instead of a `FormatException` for some invalid JSON at the end of the input.
- Fixed `ZerraJsonSerializer` reading a dictionary written as an array of key and value pairs when an item isn't a pair, such as `[1]`. It threw `ArgumentNullException` or added an empty entry, and now skips the item.
- Fixed `ZerraJsonSerializer` failing on dictionaries with `string` or number keys written as an array of key-value pairs.
- `ZerraJsonSerializer` throws on anything after the JSON value other than whitespace, and every type follows one rule for values that don't match it ([Mismatched Values](docs/JsonSerializer.md#mismatched-values)).
- `JsonSerializerOptions.ErrorOnTypeMismatch` is renamed `ErrorOnReadMismatchedData`. With it off, the default, valid JSON that doesn't fit the type no longer throws: `1.5` for an `int`, an unparseable date, an unknown enum name, or extra values in a nameless array read as the default or are skipped, and a repeated dictionary key keeps the last value. Invalid JSON still throws.

### Mapping

- `Mapper` is faster and allocates less when mapping collections.
- `Mapper` and repository type conversions use the invariant culture.
- `Mapper` and the serializers throw the actual error, such as a `NotSupportedException` for types that can't be mapped, instead of wrapping it in a `TargetInvocationException`.

### Platform

- Targets .NET 10. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0, including .NET Framework 4.7.2+.
- Source generation for every CQRS type, model, and enum, and Native AOT compatibility throughout.
- No external dependencies on .NET 10.
- `StringExtensions` parses numbers and dates with the invariant culture, with an optional `provider` for other cultures.
- Fixed `TypeDetail.GetConstructor()` failing on types with several constructors.
- For types that aren't source generated, members read and write through the property unless it's an auto-property, and only public fields are listed, like generated types. Before, a private field with the property's name was used instead, so a computed property such as `Dictionary.Count` read the wrong value. `MemberDetail.IsBacked` is removed.
- `AsynmmetricEncryptor` is renamed `AsymmetricEncryptor`.
- The concurrent collections follow the standard `IList` and `IDictionary` rules, like `List<T>` and `ConcurrentDictionary`. For example, `Add` throws on a duplicate key and `Insert` at the end works.

### Repository (Experimental)

- `IRepo` from `Repo.New()` with providers added explicitly, replacing the static `Repo` and data contexts.
- Engines for SQL Server (`Microsoft.Data.SqlClient`), PostgreSQL, MySQL, MariaDB, in-memory, and KurrentDB (replacing EventStoreDB), each with a connection test.
- The MySQL engine uses MySqlConnector instead of MySql.Data, the same driver as MariaDB, so async calls are truly async.
- `AggregateRoot` for event-sourced aggregates.
- SQL queries compare null columns the way C# does, so `x.Maybe != 4` and `!(x.Maybe > 1)` include rows where `Maybe` is null, matching the in-memory engine.
- `saveStateEvery` sets how often the event store saves a model's state, or 0 to never save it.

## 5.x

The current NuGet release. Its source is on the `release/5.4.0` branch.
