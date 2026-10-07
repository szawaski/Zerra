# Changelog

## 6.0.0 (in development)

Zerra 6 replaces assembly scanning and configuration files with explicit setup in code. [Upgrading from Zerra 5](docs/UpgradeV5ToV6.md) walks through moving a solution step by step. Zerra 5 and 6 services use different wire formats, so services that share messages upgrade together.

### Setup and Hosting

- Each service builds its bus in `Program.cs` with `Bus.New` and registers handlers, servers, clients, producers, and consumers explicitly. `cqrssettings.json`, discovery, and service creators are gone.
- Handlers derive from `BaseHandler` or `BaseHandlerWithRepo` and use the `Bus`, `Log`, `Repo`, and `Context` they inherit. Services are injected through `BusServices`.
- What a service exposes is what its bus registers; `[ServiceExposed]`, `[ServiceBlocked]`, and `[ServiceSecure]` are removed.
- Graceful shutdown: stopping the bus finishes every message already received, and `WaitForExitAsync` handles SIGTERM and SIGINT for containers. `shutdownTimeout` limits the wait without cancelling handlers.
- `commandToReceiveUntilExit` for batch jobs and KEDA-style scaling.
- Services can be hosted in ASP.NET Core/Kestrel with `KestrelCqrsServer*` and called with `KestrelCqrsClient`.

### Messaging

- `EventConsumerMode.PerReplica` and `PerService` on every event subscription.
- Optional message compression with `ICompressor`, applied before encryption.
- `IEncryptor` replaces `SymmetricConfig`, with `ZerraEncryptor` built in.
- Streams as query arguments (uploads) as well as results.
- Connection tests for each broker (`KafkaConnectionTest`, `RabbitMQConnectionTest`, `AzureServiceBusConnectionTest`) to fall back to direct TCP or HTTP.
- RabbitMQ accepts AMQP URIs, including TLS with `amqps://`. Kafka connects with TLS through `useTls`.
- `Zerra.CQRS.AzureEventHub` is removed; use Kafka, RabbitMQ, or Azure Service Bus.

### Serialization

- `ZerraJsonSerializer` is faster than System.Text.Json ([Benchmarks](docs/Benchmarks.md)): serializing to UTF-8 no longer grows its buffer on every call, arrays are read in one pass instead of being scanned for their length first, dates are written with the runtime's round-trip formatter, strings are checked for escaping with vectorized search, property names are copied without pinning, strings with nothing to escape are read and written on an inlined path, and reading handles punctuation, numbers, and `true`/`false`/`null` with less overhead.
- `ZerraJsonSerializer` and `ZerraByteSerializer` clear only the bytes they wrote when returning a pooled buffer, instead of the whole buffer on every call, and write each property with less overhead.
- `ZerraJsonSerializer` reads and writes every collection and dictionary type element by element without per-item delegates, and without enumerator allocations for arrays, lists, hash sets, and dictionaries, writes string dictionary keys without allocating, transcodes ASCII strings to and from UTF-8 on a faster path, writes `TimeSpan`, `DateOnly`, and `TimeOnly` with the runtime's formatters, and parses `TimeSpan`, `TimeOnly`, and integers from strings faster, and reads and writes enum names from per-converter caches without boxing or, for cached names, allocating.
- `ZerraByteSerializer` reads and writes every collection and dictionary type without per-item delegates, and writes arrays, lists, hash sets, and dictionaries without allocating an enumerator or accessor, and transcodes ASCII strings on a faster path, and reads and writes enums without boxing.
- `ZerraJsonSerializer` deserializes `IEnumerable<T>`, `IReadOnlyCollection<T>`, `IEnumerable`, and `ICollection` members as a `List` instead of copying it into an array.
- `ZerraJsonSerializer` and `ZerraByteSerializer` find the converter for each call with one cached lookup, or none for generic calls, cutting per-call overhead so small payloads are faster too.
- Fixed `ZerraJsonSerializer` writing dictionary keys that need escaping: they threw when serializing to UTF-8 and were corrupted when serializing to a string.
- Fixed `ZerraJsonSerializer` writing a negative UTC offset of 10 hours or more, or with minutes, on `DateTimeOffset` and local `DateTime` values.
- Fixed `ZerraJsonSerializer` using the current culture for numbers and numeric dictionary keys: in cultures with a different decimal separator or minus sign it wrote invalid JSON and misread numbers.
- Fixed `ZerraJsonSerializer` reading enum numbers: negative values failed from a string, and `ulong` values above `long.MaxValue` failed from a string or UTF-8, and values outside the enum's underlying type were truncated instead of rejected.
- Fixed `ZerraJsonSerializer` writing `DateTime`, `DateTimeOffset`, `DateOnly`, and `TimeOnly` dictionary keys in a culture format that dropped fractions and read back as default values; they are written in ISO 8601.
- `ZerraJsonSerializer` writes dictionaries with enum keys as JSON objects keyed by the enum name, the same as System.Text.Json, or by the number with `EnumAsNumber`, instead of an array of key and value pairs. The array form is still read, and enums are also read from numeric strings.

### Mapping

- `Mapper` maps arrays, lists, sets, and dictionaries element by element without per-item delegates or accessor allocations, and finds the converter for each call with one cached lookup, or none for generic calls.
- Fixed `Mapper` throwing when mapping a dictionary, or another type built through its constructor, with value-type members such as `Dictionary<string, int>`.
- Fixed `Mapper` throwing when mapping to a custom dictionary type such as `SortedDictionary<TKey, TValue>`.
- Fixed `TypeAnalyzer.Convert`, used by `Mapper` for core types and by the repository for generated identities: numbers, dates, and strings used the current culture, `DateTimeOffset?` targets returned a `DateTime`, and converting between date types such as `DateTime` to `DateOnly` failed.

### Platform

- Targets .NET 10. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0, including .NET Framework 4.7.2+.
- Source generation for every CQRS type, model, and enum, and Native AOT compatibility throughout.
- No external dependencies on .NET 10.

### Repository (Experimental)

- `IRepo` from `Repo.New()` with providers added explicitly, replacing the static `Repo` and data contexts.
- Engines for SQL Server (`Microsoft.Data.SqlClient`), PostgreSQL, MySQL, MariaDB, in-memory, and KurrentDB (replacing EventStoreDB), each with a connection test.
- `AggregateRoot` for event-sourced aggregates.

## 5.x

The current NuGet release. Its source is on the `release/5.4.0` branch.
