[← Back to Documentation](Index.md)

# Production Checklist

What to decide and verify before a Zerra service goes to production. Each item links to the page with the details.

## Delivery and Failures

Commands and events are delivered at most once, and Zerra doesn't retry a failed handler. [Delivery and Failure Handling](Reliability.md) explains what that means for each transport, and how to make handlers safe to retry.

- [ ] Work that must happen once is a command, or an event registered `PerService` ([Events](Events.md#the-rule))
- [ ] Commands a caller may retry are idempotent ([Commands](Commands.md#idempotency))
- [ ] State is saved before the event announcing it is dispatched, with an outbox where a lost notification matters
- [ ] Timeouts are set on `Bus.New` or per call; none are set by default ([Client Setup](ClientSetup.md#timeout-configuration))
- [ ] `shutdownTimeout` is a few seconds shorter than the orchestrator's grace period ([Server Setup](ServerSetup.md#shutdown))

## Security

### Trust Boundaries

Services talking directly over TCP, HTTP, or a broker trust each other: they accept the claims a caller sends without authenticating it. Only the [Zerra.Web](ZerraWeb.md) API gateway is meant to face outside callers. See [Security](Security.md#trust-model).

- [ ] Internal servers and brokers are reachable only from a private network
- [ ] The gateway has an `ICqrsAuthorizer`, and registers only the interfaces outside callers may use ([Zerra.Web](ZerraWeb.md#production-checklist))
- [ ] Handlers check the claims they depend on ([Security](Security.md#authorization))

### Encryption in Transit

| Connection | Transport encryption |
|---|---|
| `TcpCqrsServer`, `HttpCqrsServer` | none; use message encryption and a private network |
| Kestrel (`Zerra.Web`) | HTTPS, configured in ASP.NET Core |
| RabbitMQ | TLS with an `amqps://` URI ([RabbitMQ Setup](RabbitMQSetup.md#connection-settings)) |
| Azure Service Bus | always TLS |
| Kafka | none; `PLAINTEXT`, or `SASL_PLAINTEXT` with a user name and password |

Message encryption with an `IEncryptor` protects message bodies on every transport, including on a broker's disk, but not connection metadata, and not the Kafka SASL password. See [Encryptors](Encryptors.md).

### Keys

- [ ] The key comes from configuration or a secret store, never source control, with one key per environment ([Encryptors](Encryptors.md#configuration))
- [ ] There's a plan for rotation. Each endpoint has one key and both ends must match, so rotate by updating every service on an endpoint together, or by bringing up the new key under a new environment prefix or port and moving callers over.

### Error Details

A handler's exception is returned to a caller awaiting it, including through the gateway, with its type name, message, and stack trace. That makes failures easy to diagnose across services, and means exception messages must be safe for whoever can call:

- [ ] Exception messages never contain secrets, connection strings, or personal data
- [ ] Validation failures throw exceptions whose messages are meant for the caller

### Request Limits

- [ ] The gateway has a global rate limiter ([Zerra.Web](ZerraWeb.md#production-checklist)). Kestrel's request size limit applies, except to stream uploads, which the gateway lets through.
- [ ] Internal servers, which have no request size limit of their own, aren't reachable from outside
- [ ] `maxConcurrentQueries` and the per-topic command and event limits on `Bus.New` suit the service's resources ([Client Setup](ClientSetup.md#bus-options))

## Versioning and Rolling Deployments

Services that deploy independently run different versions of a contract for a while. Plan for it:

- [ ] **Contract identity is stable.** Messages and query interfaces are identified by their assembly-qualified type name, so renaming a contract, its namespace, or its assembly is a breaking change. Add a new contract and retire the old one instead.
- [ ] **Binary contracts are versioned.** `ZerraByteSerializer` matches members by declaration order unless they have `[SerializerIndex]`, so give every contract that will change an index per member, and only add members ([ByteSerializer](ByteSerializer.md#versioning)). `ZerraJsonSerializer` matches by name.
- [ ] **Receivers deploy before senders.** A receiver on the new version reads an old sender's messages, giving members it left out their defaults. A receiver on the old version can't read a member it doesn't know with the default byte format, and throws, so roll out the handling service before the services that send the new member. With `ByteSerializerIndexType.MemberNames`, `UseTypes = true`, or `ZerraJsonSerializer`, unknown members are skipped and either order works.
- [ ] **Major versions upgrade together.** Zerra 5 and 6 services can't talk to each other ([Upgrade Guide](UpgradeV5ToV6.md)), so pin the package version and upgrade all services that share messages at once.
- [ ] Old and new versions of a service have been run side by side against the same broker before rollout.

## Observability

Zerra has no built-in tracing or metrics. It gives you two hooks, and you connect them to your own tools ([Logging](Logging.md)):

- **`ILogger`** for framework errors, such as a lost broker connection or a failed handler on a message nobody awaited. Without one these are silent.
- **`IBusLogger`** for every query, command, and event, on both the sending and the handling side, with the source service, duration, and exception. It is the place to start OpenTelemetry activities or record metrics.

- [ ] An `ILogger` is passed to `Bus.New` and to every server, client, producer, and consumer
- [ ] An `IBusLogger` records messages, or at least failures
- [ ] Alerts exist for repeated broker connection errors and handler exceptions

## Testing

- [ ] Handlers are tested in memory, by registering them on a bus with `AddHandler` and an in-memory repository ([Store tests](../Demo/Store/README.md))
- [ ] The production transport has been tested with crashes, broker outages, duplicate commands, and shutdown under load ([Testing Failures](Reliability.md#testing-failures))
- [ ] A Native AOT build has been published and run, if you use AOT ([AOT](AOT.md))
- [ ] Throughput and latency were measured on your own workload, with the brokers and network you'll deploy on

## Repository

`Zerra.Repository` is experimental. The CQRS packages don't depend on it, so it can be adopted, or not, separately. Before relying on it, test its SQL translation, relations, and schema generation against your own models and database. See [Repository](Repository.md).

## See Also

- [Delivery and Failure Handling](Reliability.md)
- [Security](Security.md)
- [Server Setup](ServerSetup.md) and [Client Setup](ClientSetup.md)
- [Store demo](../Demo/Store/README.md) - A complete multi-service application
