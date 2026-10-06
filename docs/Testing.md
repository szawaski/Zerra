[← Back to Documentation](Index.md)

# Testing

Zerra is covered by about 1,700 test methods across the framework and the Store demo. The transport and data store tests run against real Kafka, RabbitMQ, Azure Service Bus, SQL Server, PostgreSQL, MySQL, MariaDB, and KurrentDB instances, not mocks.

## Test Projects

| Project | Tests | Covers | Needs |
|---|---|---|---|
| `Tests/Zerra.Test` | ~1,350 | the bus, TCP and HTTP transports, serializers, encryption, compression, reflection, mapping, collections, and the `Zerra.Web` gateway and Kestrel hosting | nothing |
| `Tests/Zerra.CQRS.Test` | ~40 | Kafka, RabbitMQ, and Azure Service Bus producers and consumers | the brokers |
| `Tests/Zerra.Repository.Test` | ~170 | LINQ to SQL translation, every data store engine, event-sourced aggregates | the databases |
| `Tests/Zerra.SourceGeneration.Test` | ~15 | source generator output for handlers, models, enums, and edge-case type shapes | nothing |
| `Tests/Zerra.T4.Test` | ~30 | the JavaScript and TypeScript client generators | nothing |
| `Demo/Store/Store.*.Test` | ~120 | each demo service's handlers on an in-memory bus and store | nothing |

## What's Covered

| Area | Where |
|---|---|
| **Dispatch** | `CQRS/BusTests` covers local and remote calls, commands, and events over TCP and HTTP, encrypted and unencrypted, with timeouts and `IBusLogger`. `BusContextTests` and `BusScopesTests` cover handler context and services. |
| **Serialization** | `ByteSerializerDataTests` and `JsonSerializerDataTests` (about 200 tests) cover every supported type, nulls, nested and large models, streams, nameless JSON, and graphs. |
| **Contract compatibility** | `IndexAttribute` reads data written by one version of a model into a different version through `[SerializerIndex]`. `DrainBytes` and the JSON `Drain` tests skip members the reader doesn't have. `IndexTypeMemberNames` covers matching by name. |
| **TCP and HTTP** | `TcpCqrsClientTests`, `TcpCqrsServerTests`, `HttpCqrsClientTests`, and `HttpCqrsServerTests` cover cancellation during a request and during a response, a server closing the connection, an invalid response on a pooled connection not being resent, content type mismatches, unregistered interfaces, and the receive limit. The protocol stream tests cover framing, chunking, and streams that end early. |
| **Connections** | `SocketClientPoolTests` covers connection reuse, discarding a connection with unread data, and failed connects. `SocketAbortMonitorTests` covers the cancellation handshake and invalid abort messages. |
| **Brokers** | Each broker runs the shared `MessageTest` sequence: fire-and-forget, awaited, and result commands, handler errors, concurrency, cancellation, claims, and events. Also `TestSustainedLoad` (two replicas under sustained traffic, every command handled once and every event once per replica, within the concurrency limit), `TestReceiveLimitHandsOff`, `TestEventConsumerModePerService`, `TestCommandSentBeforeConsumer`, `TestConsumesAgainAfterTopicDeleted` (recovery when the topic or queue is deleted under a running consumer), `TestHandlerErrorNotReceivedAgain`, and `TestReplicasEnsureAtOnce`. |
| **Shutdown** | `BusShutdownTests` covers finishing a query and a fire-and-forget command in progress over TCP and HTTP, and stopping after the timeout without cancelling the handler. Each broker's `TestFinishesProcessingOnClose` covers a command and an event still being handled when the consumer closes. |
| **Security** | The servers' `Query_WithClaims_SetsThreadPrincipalForHandler` and the broker claims test cover claims propagation. `CqrsApiGatewayMiddlewareTests` covers authorizer rejection (401), a relayed `SecurityException` (401), allowed and disallowed origins, preflight, and content type checks. `HttpCqrsServerTests` covers its authorizer and origins. |
| **Errors** | `ExceptionSerializerTests` covers exceptions crossing the wire, relayed exceptions keeping their original type, and responses that can't be read. |
| **Native AOT** | `Zerra.SourceGeneration.Test` checks the generated code. The Store demo services set `PublishAot`, which also disables dynamic code under `dotnet run`, so running the demo runs the generated paths. |
| **Data stores** | Each engine's `TestSequence` creates the schema with code-first generation, asserts the generated plan is then empty, and runs queries, relations, and updates. |

## Running the Tests

The test projects use xunit v3. Build the solution, then run a project's test executable, filtering by class if needed:

```bash
dotnet build Zerra.slnx
Tests/Zerra.Test/bin/Debug/net10.0/Zerra.Test.exe
Tests/Zerra.CQRS.Test/bin/Debug/net10.0/Zerra.CQRS.Test.exe -class "Zerra.CQRS.Test.Kafka.KafkaMessageTests"
```

`Demo/Infrastructure/start-infrastructure.ps1` starts the brokers and databases the transport and data store tests need, in Docker, skipping any already running. Each broker test uses its own topics and deletes them afterwards, and each data store test recreates its database.

## Benchmarks

Serializer and mapper results are in [Benchmarks](Benchmarks.md), and the repository's comparison with Entity Framework is in [Repository](Repository.md#compared-with-entity-framework).

## See Also

- [Delivery and Failure Handling](Reliability.md)
- [Running in Production](Production.md)
