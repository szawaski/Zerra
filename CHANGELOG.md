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
- Fixed `HttpCqrsClient` leaving the cause out of the error when sending a command with a result fails.
- `WriteStreamContent` made with an async delegate throws `NotSupportedException` when sent synchronously, instead of blocking a thread on the delegate. Use the synchronous delegate constructor for synchronous sends.

### Serialization

- `ZerraJsonSerializer` is faster than System.Text.Json on every benchmarked model, and `ZerraByteSerializer` is faster than before, both with less memory ([Benchmarks](docs/Benchmarks.md)).
- `ZerraJsonSerializer` writes dictionaries with enum keys as objects, like System.Text.Json (`{"Friday":1}`). The old array form is still read.
- `ZerraJsonSerializer` reads enums from numeric strings too.
- `ZerraJsonSerializer` deserializes `IEnumerable<T>`, `IReadOnlyCollection<T>`, `IEnumerable`, and `ICollection` members as a `List` instead of an array.
- `ZerraJsonSerializer` reads a JSON object into a value typed as `object` as a `JsonObject` instead of an empty `object`, numbers into `object` as `double` instead of `decimal`, writes and reads `JsonObject` members, and supports non-generic dictionaries such as `Hashtable`, which threw before.
- `ZerraByteSerializer` throws a `NotSupportedException` when a value typed as `object`, including the items of a non-generic collection, is serialized without `UseTypes`. Before, it came back as an empty `object`.
- Fixed `ZerraJsonSerializer` using the current culture for numbers, which wrote invalid JSON in some cultures.
- Fixed `ZerraJsonSerializer` losing date dictionary keys on the round trip.
- Fixed `ZerraJsonSerializer` dictionary keys that need escaping.
- Fixed `ZerraJsonSerializer` writing some negative UTC offsets.
- Fixed `ZerraJsonSerializer` enum numbers: negative and large `ulong` values failed, and out-of-range values weren't rejected.
- Fixed `ZerraJsonSerializer` `DeserializeJsonObject` failing on streams.
- Fixed `JsonObject` using the current culture for numbers and dates, and casting a JSON null to `string` throwing instead of returning null.
- Fixed `JsonObject` reading numbers with exponents, such as `1e5`, and numbers too large or too small for `decimal` as 0. They keep their value and convert with the `double` cast.
- `ZerraByteSerializer` throws on bytes after the value, like `ZerraJsonSerializer`. Before, they were ignored or rejected depending on where the stream's read buffer ended.
- Fixed `ZerraByteSerializer` losing `Type` values with `UseTypes` on.
- Fixed `ZerraByteSerializer` failing to skip members missing from the target type when they hold custom collections, hash sets, or records.
- Fixed `ZerraByteSerializer` without `UseTypes` failing to read a value declared as an interface when its class has members the interface doesn't. The value is written with the interface's members.
- Fixed `ZerraJsonSerializer` dates: out-of-range values such as month 13 or February 30 rolled over instead of being treated as a mismatch, offsets past 14 hours, month 0, and year 0 threw, and more than 7 fraction digits weren't read.
- Fixed `ZerraJsonSerializer` `char?`, which failed on empty strings and didn't decode escapes or non-ASCII characters.
- Fixed `ZerraJsonSerializer` `byte[]` failing on escaped base64.
- Fixed `ZerraJsonSerializer` throwing `ArgumentOutOfRangeException` instead of a `FormatException` for some invalid JSON at the end of the input.
- Fixed `ZerraJsonSerializer` throwing `NotSupportedException` or `NotImplementedException` instead of a `FormatException` for a `,` or `:` where a value belongs, such as `[,1]`.
- Fixed `ZerraJsonSerializer` reading a dictionary written as an array of key and value pairs when an item isn't a pair, such as `[1]`. It threw `ArgumentNullException` or added an empty entry, and now skips the item.
- Fixed `ZerraJsonSerializer` rejecting streams with long trailing whitespace.
- Fixed `ZerraJsonSerializer` leaving out an `sbyte` from -100 to -128 when it landed at the end of the write buffer, writing invalid JSON.
- Fixed `ZerraJsonSerializer` reading numbers differently from `string` input than from UTF-8 bytes. From a `string`, `decimal` didn't accept exponents like `1e5`, and spaces, thousands separators, or a `+` on unsigned types were accepted. Both now read the same.
- Fixed the synchronous `ZerraJsonSerializer.Serialize` stream overloads writing nothing for a null value. They write `null`, like the async overloads and the string and byte overloads.
- Fixed `ZerraJsonSerializer` ignoring `[JsonIgnore]` placed after `[JsonPropertyName]` on the same member.
- Fixed `ZerraJsonSerializer` throwing on an empty property name (`{"":1}`), which is valid JSON. It's skipped like any unknown member.
- Fixed `ZerraJsonSerializer` failing on dictionaries with `string` or number keys written as an array of key-value pairs.
- Fixed `ZerraJsonSerializer` and `ZerraByteSerializer` throwing when a record or other type built through its constructor is missing a value-type constructor argument, such as an empty nameless array.
- `ZerraJsonSerializer` throws on anything after the JSON value other than whitespace, and every type follows one rule for values that don't match it ([Mismatched Values](docs/JsonSerializer.md#mismatched-values)).
- `JsonSerializerOptions.ErrorOnTypeMismatch` is renamed `ErrorOnReadMismatchedData`. With it off, the default, valid JSON that doesn't fit the type no longer throws: `1.5` for an `int`, an unparseable date, an unknown enum name, or extra values in a nameless array read as the default or are skipped, and a repeated dictionary key keeps the last value. Invalid JSON still throws.

### Mapping

- `Mapper` is faster and allocates less when mapping collections.
- Fixed `Mapper` throwing on dictionaries with value-type values, such as `Dictionary<string, int>`, and on custom dictionaries such as `SortedDictionary`.
- Fixed `Mapper` and repository type conversions using the current culture and failing between date types.
- `Mapper` and the serializers throw the actual error, such as a `NotSupportedException` for types that can't be mapped, instead of wrapping it in a `TargetInvocationException`.
- Fixed a second map definition for the same pair of types being ignored. The rules of both now apply.

### Platform

- Targets .NET 10. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0, including .NET Framework 4.7.2+.
- Source generation for every CQRS type, model, and enum, and Native AOT compatibility throughout.
- No external dependencies on .NET 10.
- `StringExtensions` parses numbers and dates with the invariant culture, with an optional `provider` for other cultures.
- Fixed `MatchWildcard` failing to match when part of the pattern after a wildcard repeats, such as `"aab"` against `"*ab"`, or when the pattern's end appears earlier in the text. A wildcard at the end now also matches nothing, as documented.
- Fixed `ToEnumNullable` returning the enum's zero value instead of `null` for a missing or unknown name.
- Fixed `EnumName.GetName` for `[Flags]` enums not being thread-safe, a new combination named on one thread could break reading names on others.
- Fixed `TypeDetail.GetConstructor()` failing on types with several constructors, and `GetMethod` and `GetConstructor` returning a cached result from an earlier lookup with a different generic argument or parameter count.
- Fixed the runtime accessor generator crashing the process for typed getters, setters, and method callers on structs.
- Fixed reading the members of a `ref struct` that isn't source generated, such as `Utf8JsonReader`, failing with `InvalidProgramException`.
- For types that aren't source generated, members read and write through the property unless it's an auto-property, and only public fields are listed, like generated types. Before, a private field with the property's name was used instead, so a computed property such as `Dictionary.Count` read the wrong value. `MemberDetail.IsBacked` is removed.
- Fixed a `Graph` member that was removed and then added again still showing in the signature as removed.
- Fixed `Graph.AddChildGraph` throwing when the child graph has instance graphs and the member already had a child graph.
- Fixed `Discovery` not finding a class that derives from a closed generic base, such as `Base<int>`, when looking up the open one, `Base<>`.
- `AsynmmetricEncryptor` is renamed `AsymmetricEncryptor`.
- Fixed `SymmetricKey.GetHashCode` differing for equal keys, so a key couldn't be found in a dictionary or set by an equal copy.
- Fixed `CryptoFlushStream.FlushFinalBlockAsync` returning before the final block was written for the prefix algorithms, such as `AESwithPrefix`. Encrypted CQRS requests could end before the last block was sent.
- Fixed the read-write collections (`ConcurrentReadWriteList`, `ConcurrentReadWriteHashSet`, `ConcurrentSortedReadWriteDictionary`) locking up for good after an exception inside them, and `ConcurrentSortedReadWriteDictionary.GetOrAdd` throwing when adding.
- The concurrent collections follow the standard `IList` and `IDictionary` rules, like `List<T>` and `ConcurrentDictionary`. For example, `Add` throws on a duplicate key and `Insert` at the end works.
- Fixed `ConcurrentSortedDictionary.GetOrAdd(key, value)` throwing `KeyNotFoundException` instead of adding the value, and `TryUpdate` on both sorted dictionaries updating when only one of the current and comparison values was null.
- Fixed `LinqRebinder` breaking `is` type checks and array creation (`new[] { ... }`, `new T[n]`) in the expressions it rebinds.
- Fixed `LinqRebinder` failing when a parameter is replaced with an expression containing a nested lambda, block, `catch`, assignment, array index, or negation.
- Fixed `LinqRebinder` turning `x++` into `x--` and `--x` into `x--` in the expressions it rebinds.

### Repository (Experimental)

- `IRepo` from `Repo.New()` with providers added explicitly, replacing the static `Repo` and data contexts.
- Engines for SQL Server (`Microsoft.Data.SqlClient`), PostgreSQL, MySQL, MariaDB, in-memory, and KurrentDB (replacing EventStoreDB), each with a connection test.
- `AggregateRoot` for event-sourced aggregates.
- Fixed SQL generation writing numbers in the current culture, which broke queries in some cultures.
- SQL queries compare null columns the way C# does, so `x.Maybe != 4` and `!(x.Maybe > 1)` include rows where `Maybe` is null, matching the in-memory engine.
- Fixed integer division in MySQL and MariaDB queries giving a decimal result, and SQL Server queries with a bool column as the test of `?:`.
- Fixed code-first generation planning the same column change on every run for a property whose `[StoreProperties]` makes the column nullable.
- Fixed members with a `[StoreName]` reading back empty from SQL databases.
- Fixed `Many(order, skip, take, graph)` ignoring the graph.
- Fixed `FirstAsync` with a graph naming relations not loading them unless the graph also named the keys.
- Fixed creating models with more than one generated identity, and the in-memory engine with a generated identity next to one that isn't.
- Fixed SQL date values with offsets of -10:00 or less, or between -01:00 and 00:00, and a trailing space after MySQL and MariaDB dates.
- Fixed the encryption provider never decrypting strings it encrypted.
- Fixed the encryption and compression providers changing the caller's model on create and update, and leaving `EventMany` results encrypted or compressed.
- Fixed create, update, and delete with a graph that names relations, which threw or skipped the related models, including deletes through a rule provider.
- Fixed deleting a multiple of 1028 models throwing "No rows affected".
- Fixed the in-memory engine's update writing related models into the stored ones, which left removed related models behind.
- Fixed event store queries on models with more than one identity property, which always threw.
- Fixed event store history queries: "as of" and number ranges gave the wrong versions, `EventMany` ignored `take` when reading oldest first, and results were wrong or empty once a stream had over 100 events. Saved states now also speed up normal reads, and `saveStateEvery` sets how often they are saved.
- Fixed event store queries that failed when the where had `array.Contains(x.ID)`, a new array or list, `??`, an index, a static member, an identity written as `a ? b : c` or `a + 1`, or a filter on other members such as `x.Name.StartsWith(...)`.
- `LinqStringConverter` and `WhereBuilder.ToString()` group nested operations in parentheses and no longer depend on the current culture. Fixed strings shown as character lists, and crashes on `as`, array indexes, invoked delegates, and a null partway through a captured value.

## 5.x

The current NuGet release. Its source is on the `release/5.4.0` branch.
