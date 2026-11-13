# Changelog

## 6.0.0

Zerra 6 replaces assembly scanning and configuration files with explicit setup in code. [Upgrading from Zerra 5](docs/UpgradeV5ToV6.md) walks through moving a solution step by step. Zerra 5 and 6 services use different wire formats, so services that share messages upgrade together.

### Setup and Hosting

- Each service builds its bus in `Program.cs` with `Bus.New` and registers handlers, servers, clients, producers, and consumers explicitly. `cqrssettings.json`, discovery, and service creators are gone.
- Handlers derive from `BaseHandler` or `BaseHandlerWithRepo` and use the `Bus`, `Log`, `Repo`, and `Context` they inherit. Services are injected through `BusServices`.
- The static `Bus` and `Log` are obsolete. Use the `IBus` from `Bus.New` and pass your `ILogger` where it's needed, including `CodeFirstGeneration.Generate`, which logs a failed database read to it.
- What a service exposes is what its bus registers; `[ServiceExposed]`, `[ServiceBlocked]`, and `[ServiceSecure]` are removed.
- Graceful shutdown: stopping the bus finishes every message already received, and `WaitForExitAsync` handles SIGTERM and SIGINT for containers. `shutdownTimeout` limits the wait without cancelling handlers.
- `commandToReceiveUntilExit` for batch jobs and KEDA-style scaling.
- Services can be hosted in ASP.NET Core/Kestrel with `KestrelCqrsServer*` and called with `KestrelCqrsClient`.
- Concurrency limits are set on each client, server, producer, and consumer (`maxConcurrentQueries`, `maxConcurrentCommands`, `maxConcurrentEvents`) instead of on `Bus.New`, so each can have its own. `TcpCqrsServer` and `HttpCqrsServer` limit queries, commands, and events separately. See [Concurrency Limits](docs/ClientSetup.md#concurrency-limits).
- The web gateway serves the browser clients with `app.UseCqrsClientScripts()`: `Bus.js` and `Bus.ts`, already pointed at the gateway, and their models, built from what its bus routes. They replace the copied scripts and the T4 templates, so there's nothing to copy or regenerate after changing a contract. See [Front End Scripts](docs/FrontEndScripts.md).
- `BusRoutes.js` and `BusConfig.ts` are gone. Set routes, headers, and a global error handler in code with `Bus.setRoute`, `Bus.setHeader`, and `Bus.onFail`. The `Bus` methods are camelCase in both scripts (`Bus.call`, `Bus.dispatch`, `Bus.dispatchAwait`, `Bus.dispatchAsync`, `Bus.dispatchAwaitAsync`), and `Bus.js` no longer needs jQuery.

### Messaging

- `EventConsumerMode.PerReplica` and `PerService` on every event subscription.
- `resilientCommands` on the Kafka, RabbitMQ, and Azure Service Bus consumers acknowledges a command after its handler finishes, so a command being handled when the process dies goes to another replica. See [Resilient Commands](docs/Reliability.md#resilient-commands).
- Optional message compression with `ICompressor`, applied before encryption.
- `IEncryptor` replaces `SymmetricConfig`, with `ZerraEncryptor` built in.
- New encryption modes: `AES_GCM` and `AES_CBC_HMAC` reject messages that were changed, reordered, or cut short, and `AES_CBC` only keeps them private. Every encryption is different, even of the same message. `DES`, `TripleDES`, and `RC2` are removed. Keys are `byte[]` (`SymmetricKey` and `SymmetricConfig` are gone), and Zerra 5's `AES` and `AESwithShift` moved to `ZerraEncryptorOld` and `SymmetricEncryptorOld` for data Zerra 5 stored. They and `HasherOld` are obsolete, so using them warns. Repository encryption providers take an `IEncryptor`.
- Keys can rotate: `ZerraEncryptor` takes an `ISymmetricKeySource` that supplies the current key and when it expires, and data from the previous key still decrypts. See [Rotating Keys](docs/Encryptors.md#rotating-keys). Repository encryption providers refuse one, since stored data outlives the previous key. Every message starts with a key check, so the right key is picked without trying each one, and with `AES_CBC` a wrong key fails instead of decrypting to garbage.
- `AsymmetricEncryptor` writes standard JWE, so it encrypts data of any size that any JOSE library can read, with `AsymmetricAlgorithmType.RSA_OAEP_256_A256GCM`, or `RSA_OAEP_A256CBC_HS512` on .NET Framework. Keys are PEM, 2048-bit by default, and `Encrypt`/`Decrypt` replace `RSAEncrypt`/`RSADecrypt`.
- `Hasher.PBKDF2GenerateHash` stores passwords as PHC strings with modern settings (SHA-256, 600,000 iterations), `PBKDF2NeedsRehash` says when to upgrade one, and hashes are compared in constant time. `HashAlgoritmType` is renamed `HashAlgorithmType` without MD5 and SHA-1, and Zerra 5 hashes are checked with `HasherOld`. PBKDF2 with SHA-256 and up works on .NET Framework too.
- `Password` is removed; use `RandomNumberGenerator.GetString`.
- A handler's exception reaches the caller as a `CqrsServiceException` (renamed from `RemoteServiceException`) whether the handler is local or remote, with the original type name in `ErrorType`. A remote handler's stack trace isn't sent. See [Handler Exceptions](docs/UpgradeV5ToV6.md#handler-exceptions).
- Streams as query arguments (uploads) as well as results.
- Connection tests for each broker (`KafkaConnectionTest`, `RabbitMQConnectionTest`, `AzureServiceBusConnectionTest`) to fall back to direct TCP or HTTP.
- RabbitMQ accepts AMQP URIs, including TLS with `amqps://`. Kafka connects with TLS through `useTls`.
- Kafka topics use the broker's `default.replication.factor` instead of 1, which needs Kafka 2.4 or later.
- `Zerra.CQRS.RabbitMQ` uses RabbitMQ.Client 7, and `RabbitMQConnectionTest.Test` is now `TestAsync`. Creating a RabbitMQ consumer or producer no longer throws when the broker is down: the consumer logs it and keeps retrying, and sends fail until the broker is back.
- `Zerra.CQRS.AzureEventHub` is removed; use Kafka, RabbitMQ, or Azure Service Bus.
- Kafka and Azure Service Bus recover when a producer's acknowledgement topic or queue is deleted: the producer creates it again and the consumer retries the reply, so awaited commands no longer hang.
- Azure Service Bus sends wait out throttling instead of failing once the client's retries run out.
- Kafka waits for a topic it creates to be ready before using it, so consumers no longer log an unknown topic error when they first start.
- `WriteStreamContent` made with an async delegate throws `NotSupportedException` when sent synchronously, instead of blocking a thread on the delegate. Use the synchronous delegate constructor for synchronous sends.
- `bus.Call<T>(serviceUrl)` sends a query to a specific replica's address, optionally with a different encryptor. See [Calling a Specific Replica](docs/ClientSetup.md#calling-a-specific-replica).
- Requests no longer send a `Provider-Type` header. Servers read the type from the request body, and the type name isn't sent unencrypted when the body is encrypted.
- Remote queries and commands over TCP and HTTP are about 20% faster. The server no longer throws and catches an exception on every request when it stops watching for the caller to cancel.

### Serialization

- `JsonSerializer` is faster than System.Text.Json on every benchmarked model, and `ByteSerializer` is faster than before, both with less memory ([Benchmarks](docs/Benchmarks.md)).
- `JsonSerializer` writes dictionaries with enum keys as objects, like System.Text.Json (`{"Friday":1}`). The old array form is still read.
- `JsonSerializer` reads enums from numeric strings too.
- `JsonSerializer` deserializes `IEnumerable<T>`, `IReadOnlyCollection<T>`, `IEnumerable`, and `ICollection` members as a `List` instead of an array.
- `JsonSerializer` reads a JSON object into a value typed as `object` as a `JsonObject` instead of an empty `object`, numbers into `object` as `double` instead of `decimal`, writes and reads `JsonObject` members, and supports non-generic dictionaries such as `Hashtable`, which threw before.
- `ByteSerializer` throws a `NotSupportedException` when a value typed as `object`, including the items of a non-generic collection, is serialized without `UseTypes`. Before, it came back as an empty `object`.
- `JsonSerializer` rejects enum numbers out of the enum's range.
- `ByteSerializer` throws on bytes after the value, like `JsonSerializer`. Before, they were ignored or rejected depending on where the stream's read buffer ended.
- Fixed `ByteSerializer` failing to skip members missing from the target type when they hold custom collections, hash sets, or records.
- Fixed `ByteSerializer` without `UseTypes` failing to read a value declared as an interface when its class has members the interface doesn't. The value is written with the interface's members.
- Fixed `JsonSerializer` throwing `ArgumentOutOfRangeException` instead of a `FormatException` for some invalid JSON at the end of the input.
- Fixed `JsonSerializer` reading a dictionary written as an array of key and value pairs when an item isn't a pair, such as `[1]`. It threw `ArgumentNullException` or added an empty entry, and now skips the item.
- Fixed `JsonSerializer` failing on dictionaries with `string` or number keys written as an array of key-value pairs.
- Fixed `JsonSerializer` throwing `NullReferenceException` when writing a dictionary to a stream if its closing `}` or `]` landed exactly at the end of the write buffer.
- Fixed on .NET Framework: reading records and `init` properties, `float` and `double` values that didn't round trip, numbers too large for `float` or `double` throwing instead of reading as infinity, and `[JsonIgnore(Condition = ...)]` being treated as always ignored.
- `JsonSerializer` throws on anything after the JSON value other than whitespace, and every type follows one rule for values that don't match it ([Mismatched Values](docs/JsonSerializer.md#mismatched-values)).
- `JsonSerializerOptions.ErrorOnTypeMismatch` is renamed `ErrorOnReadMismatchedData`. With it off, the default, valid JSON that doesn't fit the type no longer throws: `1.5` for an `int`, an unparseable date, an unknown enum name, or extra values in a nameless array read as the default or are skipped, and a repeated dictionary key keeps the last value. Invalid JSON still throws.
- `MaxDepth` on `JsonSerializerOptions` and `ByteSerializerOptions` sets how deeply objects and collections can nest. The default is 64, the same as System.Text.Json; before, both serializers stopped at 31 with no way to change it.
- Fixed source-generated types leaving out public properties and fields inherited from a base class, so the serializers and the mapper skipped them.

### Mapping

- `Mapper` is faster and allocates less when mapping collections.
- `Mapper` and repository type conversions use the invariant culture.
- `Mapper` and the serializers throw the actual error, such as a `NotSupportedException` for types that can't be mapped, instead of wrapping it in a `TargetInvocationException`.

### Platform

- Targets .NET 10 and .NET 11. `Zerra`, `Zerra.Web`, and the `Zerra.CQRS.*` transports also target .NET Standard 2.0, including .NET Framework 4.7.2+.
- Source generation for every CQRS type, model, and enum, and Native AOT compatibility throughout.
- No external dependencies on .NET 10 and 11.
- `StringExtensions` parses numbers and dates with the invariant culture, with an optional `provider` for other cultures.
- Fixed `TypeDetail.GetConstructor()` failing on types with several constructors.
- For types that aren't source generated, members read and write through the property unless it's an auto-property, and only public fields are listed, like generated types. Before, a private field with the property's name was used instead, so a computed property such as `Dictionary.Count` read the wrong value. `MemberDetail.IsBacked` is removed.
- `AsynmmetricEncryptor` is renamed `AsymmetricEncryptor`.
- The concurrent collections follow the standard `IList` and `IDictionary` rules, like `List<T>` and `ConcurrentDictionary`. For example, `Add` throws on a duplicate key and `Insert` at the end works.

### Repository

- `IRepo` from `Repo.New()` with providers added explicitly, replacing the static `Repo` and data contexts.
- Engines for SQL Server (`Microsoft.Data.SqlClient`), PostgreSQL, MySQL, MariaDB, MongoDB, in-memory, and KurrentDB (replacing EventStoreDB), each with a connection test.
- The MySQL engine uses MySqlConnector instead of MySql.Data, the same driver as MariaDB, so async calls are truly async.
- `AggregateRoot` for event-sourced aggregates.
- SQL queries compare null columns the way C# does, so `x.Maybe != 4` and `!(x.Maybe > 1)` include rows where `Maybe` is null, matching the in-memory engine.
- `saveStateEvery` sets how often the event store saves a model's state, or 0 to never save it.
- `DatabaseFirstGeneration.Generate(engine, namespace)` writes a model for each table of an existing SQL Server, PostgreSQL, MySQL, or MariaDB database, with relations in both directions, keys, and indexes. Each is a `partial` class in its own file, and code-first finds nothing to change with them. It replaces the T4 `MsSqlFirst` templates and `Zerra.T4`. See [Database First](docs/RepositoryGeneration.md#database-first).
- `[Entity("schema", "Name")]` puts a table in another schema. Code First creates the schema, and Database First reads every schema.
- `DataStoreGenerationType` is now `CodeFirstMode`, and nothing runs unless you name it: `CreateDataStore`, `Columns`, `Constraints`, and `DropColumns`, with `Preview` to only log the plan. `Columns` makes the changes that can't fail on existing rows and `Constraints` the ones that can, so data can be moved between them. `DropColumns` drops the columns a model no longer has, with their data, instead of making them nullable, and never drops tables. A column can't change to an unrelated type, such as text to a number, and defaults and constraints added outside the models are kept when a column changes. A missing database throws without `CreateDataStore`. See [Code First](docs/RepositoryGeneration.md#code-first).
- On SQL Server, a `DateTime` with a precision, stored as `datetime2`, keeps every digit when saved and compared. Before, it was cut to milliseconds.
- A column is NOT NULL when its member can't be null: `string`, `byte[]`, and `int` can't, `string?` and `int?` can. Before, only value types were, and any `[StoreProperties]` made the column nullable unless it said otherwise, so `[StoreProperties(18, 2)] public decimal Price` was nullable. `[StoreProperties]` still decides when it's given `notNull`, or the now settable `NotNull`. Code-first changes existing columns to match, so run it with `CodeFirstMode.Preview` first.
- `[StoreIndex]` adds a column to an index in the SQL engines. Members with the same name form one combined index, with `Unique`, `Order`, and `Descending` options.
- `Update<T>(where, model, graph)`, `Update<T>(where, x => new T { Price = x.Price * 1.1m })`, and `Delete<T>(where)` change every matching row in one statement without loading them, and return how many. See [Changing Many Rows](docs/Repository.md#changing-many-rows).
- A custom provider implements one `Query(LinqQuery)`, and `LinqModelQuery` is the query a provider uses to load models. A history query is a `LinqQuery` with `Temporal` set.
- The types custom providers and engines use, such as `LinqQuery`, `LinqModelQuery`, and `ProjectionMember`, are in the `Zerra.Repository.Linq` namespace, and `RepoTransaction` is in `Zerra.Repository.Transactions`. `LinqExtensions` is removed, and `LinqAppender` and `LinqStringConverter` are no longer public.
- A `where` through a relation whose model is on another engine now works: the related part runs on its engine first and the identities it matched filter the query, aggregate, projection, or update and delete by condition. An aggregate key or value, a projected member, or an order through such a relation loads the matching models with what it reads and runs in memory.
- `WhereBuilder` is removed. Add a `Where` to the query for each condition.
- A condition, aggregate value, projected member, or order can read a member of one row of a related collection, picked with `First`, `Last`, or `ElementAt` after filtering, ordering, and skipping, such as `x.Lines.OrderByDescending(l => l.PlacedOn).First().Status`. The row is a subquery of one row in the store, and a row that isn't there reads as null in memory as well.
- `IRepo.BeginTransaction` groups work across operations, on SQL Server, PostgreSQL, MySQL, MariaDB, and the in-memory engine. Across engines the commit runs one engine at a time and throws `RepoPartialCommitException` if it fails partway.
- Enum members can be stored in the SQL engines. They are stored as text, using `[EnumName]` when a value has one, and compare and sort by their numbers as in C#.
- `Convert.ToInt32` and the other number conversions round a midpoint to the even number in the store, as in C#.
- `Math.Round` rounds as C# does with every `MidpointRounding`, to even by default, where it used to round midpoints away from zero. Flags enums can be cast, sorted, and checked with `HasFlag` in a query, and their `ToString` is the `EnumName` text.
- `string.Join` in a query is no longer cut off at 1024 characters on MySQL.
- Queries combine with `Union`, `Concat`, `Intersect`, and `Except`, in one statement when both are on the same engine, and the combined elements can be filtered, ordered, paged, and selected from.
- Queries join any two models with `Join`, `GroupJoin`, `LeftJoin`, `RightJoin`, or `SelectMany` of another query, including cross joins, with no relation needed, in one statement when both are on the same engine. A member of a row a left join doesn't have reads as null on every store and in memory.
- Queries also support `Cast`, `OfType`, `Order`, `MinBy`, `MaxBy`, `CountBy`, `DefaultIfEmpty`, `Chunk`, the `GroupBy` and `FirstOrDefault` overloads, and operations by position: `Index`, `TakeWhile`, `SkipWhile`, `TakeLast`, `SkipLast`, `DistinctBy`, `UnionBy`, `IntersectBy`, `ExceptBy`, `Append`, `Prepend`, `Zip`, and `SequenceEqual`.
- `[Relation]` takes several foreign key members, so a relation can refer to a model identified by more than one member. `RelationAttribute.ForeignIdentity` is now `ForeignIdentities`.
- SQL Server receives values as parameters, so a query or change run with different values reuses one plan instead of compiling a new one each time.
- Creating or updating many models at once sends them in a few commands in one transaction, so 1,000 models take tens of milliseconds instead of seconds. If one fails, none of them are saved, unless a transaction from `IRepo.BeginTransaction` is open.
- `AsAsyncEnumerable()` and `AsEnumerable()` on a repository query give each element as it's read. A plain `foreach` still reads everything first. A custom provider or engine implements `QueryStream` and `QueryStreamAsync`, or `ExecuteLinqStream` and `ExecuteLinqStreamAsync`, for it.
- An event query includes a deleted model's delete as its last event, with `Deleted` set, the delete's event name and source, and the state it deleted as `Model`. The delete used to be recorded as `Delete` without its name or source, and left out of event queries. `AggregateRoot.Rebuild` applies the delete event as `Delete` did. A custom `IEventStoreEngine` stores the data `Terminate` and `TerminateAsync` now take.
- History is read with `IRepo.TemporalQuery<T>()` and `IRepo.EventQuery<T>()`, which take the order and the range by date or event number, and compose like `Query<T>()`: `Where`, `OrderBy`, `Skip`, `Take`, `Select`, `GroupBy`, the aggregates, and streaming. An event query can filter and order on the event, such as `x.EventName`. With `TemporalOrder.Newest` the newest state or event comes first.
- Comparing a `char` member with a `char` literal, such as `x.Code == 'A'`, now works in the SQL engines. It used to give wrong results, or an error on PostgreSQL.
- The byte store (`ByteStoreProvider`, `IByteStoreEngine`, and its layers) is removed. Call your storage's own client, such as a blob or file SDK, directly.
- A condition on an aggregate of filtered related rows, such as `x.Lines.Where(l => l.Quantity > 1).Sum(l => l.Quantity) > 3`, no longer fails on PostgreSQL.
- `QueryOrder` is removed. Use `OrderBy` and `ThenBy` on the query.
- All reads go through `IRepo.Query<T>()`, a LINQ query the store runs in one pass on every engine, with `Async` endings that take a `CancellationToken`. Pass a `Graph` to read models with their relations. What the store can't do is refused instead of run in memory. See [Repository Queries](docs/RepositoryQueries.md).

## 5.x

The previous release. Its source is on the `release/5.4.0` branch.
