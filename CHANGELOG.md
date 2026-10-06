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
